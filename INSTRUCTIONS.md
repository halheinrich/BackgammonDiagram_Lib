# BackgammonDiagram_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib
**Branch:** main

## Stack

C# / .NET 10 / Class Library / Visual Studio 2026 / Windows

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BackgammonDiagram_Lib\BackgammonDiagram_Lib.slnx`

## Purpose

Pure rendering library. Takes a `DiagramRequest` and returns a board diagram as SVG, PNG, PDF,
or PowerPoint. No user interaction, no game state — the caller owns all of that.

## Depends on

* **BgDataTypes_Lib** — PositionData, DecisionData, DescriptiveData, CubeOwner, PlayCandidate, AnalysisDepthEntry
* **SkiaSharp 3.119.2** — PNG rendering
* **Svg.Skia 3.6.0** — SVG parsing/drawing for PNG pipeline
* **QuestPDF 2026.2.3** — PDF rendering (MIT licensed)
* **DocumentFormat.OpenXml 3.5.1** — PowerPoint output

## Dependency files

### BgDataTypes_Lib
* BgDataTypes_Lib/PositionData.cs
* BgDataTypes_Lib/DecisionData.cs
* BgDataTypes_Lib/DescriptiveData.cs
* BgDataTypes_Lib/CubeOwner.cs
* BgDataTypes_Lib/PlayCandidate.cs
* BgDataTypes_Lib/AnalysisDepthEntry.cs
* BgDataTypes_Lib/BgDecisionData.cs

## Directory tree

```
BackgammonDiagram_Lib/
  BackgammonDiagram_Lib/
    BackgammonDiagram_Lib.csproj
    Models/
      BoardHitRegions.cs
      DiagramOptions.cs
      DiagramRequest.cs
      DiagramRequestExtensions.cs
      DiagramSize.cs
      Enums.cs
      MathUtils.cs
    Rendering/
      BoardLayout.cs
      DiagramRenderer.cs
      ISvgRasterizer.cs
      PdfBuilder.cs
      PptxBuilder.cs
      SkiaSharpRasterizer.cs
    Themes/
      CustomTheme.cs
      DefaultTheme.cs
      GreyscaleTheme.cs
      ITheme.cs
      ThemeRegistry.cs
  BackgammonDiagram_Lib.Tests/
    BackgammonDiagram_Lib.Tests.csproj
    BoardLayoutTests.cs
    ColourSchemeTests.cs
    DiagramRequestBuilderTests.cs
    HitRegionsTests.cs
    PptxConformanceTests.cs
    SvgStructureTests.cs
    TestFixtures.cs
    TestPaths.cs
    VisualOutputTests.cs
  BackgammonDiagram_Lib.slnx
  CodeReview.md
```

## Architecture

### DiagramRequest

Immutable class with inner `Builder`. Callers set flat fields on Builder; `Build()` constructs
nested `PositionData`/`DecisionData`/`DescriptiveData` internally.

Validation at `Build()`:
- Mop must be length 26
- Dice must be length 2
- IsCube=true → Dice must be [0,0]; IsCube=false → each die 1–6
- CubeSize must be power of 2, 1–4096

Exposes: `Position` (PositionData), `Decision` (DecisionData), `Descriptive` (DescriptiveData),
plus rendering fields: `HomeBoardOnRight`, `Mode`, `AnalysisPanelPosition`.

### DiagramOptions

Record: `ShowPipCount`, `DiagramSize Size`, `WatermarkText`, `ITheme Theme` (direct reference,
no string lookup).

### Diagram types

- **Checker play** — board with dice; play list panel in Solution mode
- **Cube decision** — board with cube indicator (no dice); cube analysis panel in Solution mode
- Determined by `Decision.IsCube`

### DiagramMode

- `Problem` — board only
- `Solution` — board + analysis panel always shown

### Rendering API

`RenderSvg`, `RenderPng`, `RenderPdf`, `RenderPptx` — each takes `(DiagramRequest, DiagramOptions)`.
PDF and PPTX also accept `IEnumerable<DiagramRequest>` for multi-page/slide output.

### Board layout (BoardLayout struct)

All constants derive from `CheckerRadius` (default 14px). ViewBox derived from layout totals.
`HomeBoardOnRight` (bool, default true) — geometric reflection in `ColumnCentreX`.

### Hit regions

`GetHitRegions(DiagramRequest, DiagramOptions)` — returns `BoardHitRegions` with points,
bar, cube, tray regions. DiagramRequest required for correct orientation mapping.

### Themes

- `DefaultTheme`, `GreyscaleTheme`, `CustomTheme`
- `ThemeRegistry`: static instances `Default` and `Greyscale`
- `ITheme` interface

### PNG rasterization

- `ISvgRasterizer` interface isolates SkiaSharp
- `Svg.Skia`'s `Drawable.Bounds` unreliable — use `ParseViewBox` + `ClipRect`
- `SKSvg` not IDisposable — never use `using`
- `dominant-baseline` ignored — use `textY = centreY + fontSize * 0.35`

### PPTX

- `sldLayoutId` must be ≥ 2147483648 per OOXML spec
- Post-processing fixes five OpenXml SDK quirks; six regression tests

### PDF

- Each DiagramRequest → one page (PNG embedded via QuestPDF `FitArea()`)
- Widescreen landscape 13.33" × 7.5" matching PPTX
- `PdfBuilder` is internal static; callers set QuestPDF license themselves

### TestData

- Shared at `backgammon\TestData`; `TestPaths._root` resolves 5 × `..`
- SVG/PNG output to `TestData\svg\`, PPTX to `TestData\pptx\`, PDF to `TestData\pdf\`
- Visual tests tagged `[Trait("Category", "Visual")]`

## Current status

🔧 In progress — SVG, PNG, PDF, PPTX rendering functional; BgDataTypes_Lib refactor complete;
hit regions implemented; Builder pattern adopted

## Deferred

- Additional themes beyond Default and Greyscale
- `FromBoard` / `FromXgid` factory methods on DiagramRequest
- Animation

## Key decisions

* SVG is hand-rolled (no SVG library)
* PNG uses SkiaSharp + Svg.Skia (NOT SkiaSharp.Svg)
* `DiagramRequest` converted from `record` to immutable `class` with inner `Builder`
* `DiagramOptions` is a record; `ITheme Theme` direct reference (no string lookup)
* `ThemeRegistry` simplified to static instances; `Resolve(string)` removed
* `HomeBoardOnRight` (bool) replaced `DiagramOrientation` enum
* `HomeBoardOnRight` mirror is purely geometric reflection in `ColumnCentreX`
* `Builder.CubeOwner` defaults to `CubeOwner.Centered` (enum zero `OnRoll` was wrong default)
* `Mop`, `Dice`, `Plays`, `AnalysisDepths` defensively copied, exposed as `IReadOnlyList<T>`
* `BoardHitRegions.Points` → `IReadOnlyDictionary`
* `GetHitRegions` takes `(DiagramRequest, DiagramOptions)` — DiagramRequest needed for orientation
* `EnsureLicense` removed from PdfBuilder; `IsPdfSupported()` added to DiagramRenderer
* `F()` locale-safe via `InvariantCulture` throughout DiagramRenderer
* `AppendCheckers` passes `request.HomeBoardOnRight` to `ColumnCentreX`
* DiagramRequest is not produced by ConvertXgToJson_Lib — constructed by clients from BgDecisionData + rendering options
* Avoid CSS stylesheets and complex SVG filters — Svg.Skia has limited support