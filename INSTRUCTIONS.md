# BackgammonDiagram_Lib

> Session conventions: [`../CLAUDE.md`](../CLAUDE.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)

## Stack

C# / .NET 10 / Class Library / xUnit. Pure rendering — no user interaction, no game state.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BackgammonDiagram_Lib\BackgammonDiagram_Lib.slnx`

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib — branch `main`.

## Depends on

- **BgDataTypes_Lib** — `PositionData`, `DecisionData`, `DescriptiveData`,
  `CubeOwner`, `PlayCandidate`, `AnalysisDepthEntry`, `BgDecisionData`. The
  whole shared type layer this library renders from.
- **SkiaSharp** — PNG rasterization backend.
- **Svg.Skia** — SVG parse/draw path used by the PNG pipeline.
- **QuestPDF** — PDF layout and output (MIT licensed; license set by caller,
  not this library).
- **DocumentFormat.OpenXml** — PPTX generation.

## Directory tree

```
BackgammonDiagram_Lib.slnx
BackgammonDiagram_Lib/
  BackgammonDiagram_Lib.csproj
  Models/
    BoardHitRegions.cs        — point/bar/cube/tray hit regions
    DiagramOptions.cs         — record: ShowPipCount, Size, WatermarkText, Theme
    DiagramRequest.cs         — immutable class + inner Builder
    DiagramRequestExtensions.cs
    DiagramSize.cs
    Enums.cs                  — DiagramMode, AnalysisPanelPosition
    MathUtils.cs
  Rendering/
    BoardLayout.cs            — geometry derived from CheckerRadius
    DiagramRenderer.cs        — public entry points (RenderSvg/Png/Pdf/Pptx, GetHitRegions)
    ISvgRasterizer.cs         — PNG backend abstraction
    PdfBuilder.cs             — internal, QuestPDF-based
    PptxBuilder.cs            — internal, OpenXml-based
    SkiaSharpRasterizer.cs    — default ISvgRasterizer implementation
  Themes/
    CustomTheme.cs
    DefaultTheme.cs
    GreyscaleTheme.cs
    ITheme.cs
    ThemeRegistry.cs          — static Default / Greyscale
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
```

## Architecture

### DiagramRequest

Immutable class with a nested `Builder`. Callers set flat fields on the
Builder; `Build()` constructs the nested `PositionData` / `DecisionData` /
`DescriptiveData` internally, then validates:

- `Mop` must be length 26.
- `Dice` must be length 2.
- `IsCube == true` → `Dice` must be `[0, 0]`.
- `IsCube == false` → each die in `1..6`.
- `CubeSize` must be a power of 2 in `1..4096`.

Exposed properties: `Position` (PositionData), `Decision` (DecisionData),
`Descriptive` (DescriptiveData), plus rendering-shape fields `HomeBoardOnRight`
(bool, default `true`), `Mode` (DiagramMode), `AnalysisPanelPosition`.

`Mop`, `Dice`, `Plays`, `AnalysisDepths` are defensively copied in and
exposed as `IReadOnlyList<T>`. `BoardHitRegions.Points` is exposed as
`IReadOnlyDictionary`. Builder's `CubeOwner` defaults to `CubeOwner.Centered`.

`DiagramRequest` is constructed by clients from `BgDecisionData` plus
rendering options — it is intentionally *not* produced by `ConvertXgToJson_Lib`.

### Diagram types

Selected by `Decision.IsCube`:

- **Checker play** — board with dice; play list panel in Solution mode.
- **Cube decision** — board with cube indicator (no dice); cube analysis
  panel in Solution mode.

### DiagramMode

- `Problem` — board only.
- `Solution` — board plus analysis panel.

Problem and Solution diagrams have **identical overall dimensions**: the
analysis panel region is always allocated, so swapping modes never reflows
surrounding content.

### BoardLayout

All geometry constants derive from `CheckerRadius` (default 14 px). The SVG
viewBox is derived from layout totals. `HomeBoardOnRight` is a purely
geometric reflection applied in `ColumnCentreX` — no data is flipped. Hot-path
formatting uses `InvariantCulture` throughout `DiagramRenderer` to stay
locale-safe.

### Title and analysis panel

The diagram title is rendered into the SVG itself as a title strip — a
single source of truth. Neither `PdfBuilder` nor `PptxBuilder` stamps a
title on top of the rendered page; they consume the SVG/PNG as-is.

The strip has three cells, composed from context (not from
`Descriptive.Title`):

- **Col 1** (left edge, left-anchored): action text —
  `"{d1}-{d2} to play"` for checker decisions, `"Cube Action?"` for cube
  decisions. Empty for malformed inputs.
- **Col 2** (strip centre, centre-anchored): `Descriptive.SourceFile`
  stem — the filename minus its final dot-extension
  (`mochy-falafel.xg` → `mochy-falafel`,
  `abc.weird.xg` → `abc.weird`). Null / empty SourceFile emits no text.
- **Col 3** (right edge, right-anchored): `"Position {N}"` when
  `DiagramRequest.PositionNumber` is set.

Strip visibility is keyed off cols 1 and 3: col 2 alone never forces the
strip on, preserving the pre-SourceFile contract for synthetic-test
requests that set only SourceFile.

`PanelBackgroundColor` is part of `ITheme`; `DefaultTheme` uses white.

### Rail text

The bottom/top rail shows away scores, the Crawford indicator when
applicable, and "Money game" labels.

### Themes

`ITheme` interface, concrete implementations `DefaultTheme`, `GreyscaleTheme`,
`CustomTheme`. `ThemeRegistry` exposes `Default` and `Greyscale` as static
instances. `DiagramOptions.Theme` is a direct `ITheme` reference — there is
no string-based lookup.

### PNG rasterization

- `ISvgRasterizer` is the pluggable backend; `SkiaSharpRasterizer` is the
  default implementation.
- `Svg.Skia`'s `Drawable.Bounds` is unreliable — the rasterizer parses the
  viewBox explicitly and uses `ClipRect` instead.
- Layout avoids CSS stylesheets and complex SVG filters because `Svg.Skia`
  has limited support for them.
- Text vertical placement uses `textY = centreY + fontSize * 0.35` because
  `dominant-baseline` is ignored by `Svg.Skia`.

### PDF

- Each `DiagramRequest` becomes one page; the page embeds the rendered PNG
  via QuestPDF `FitArea()`.
- Page size is widescreen landscape 13.33" × 7.5", matching the PPTX slide.
- `PdfBuilder` is `internal static`. Callers own the QuestPDF license.
  `DiagramRenderer.IsPdfSupported()` lets callers probe whether a license
  has been configured before invoking `RenderPdf`.

### PPTX

- Each `DiagramRequest` becomes one slide.
- `sldLayoutId` values must be `>= 2147483648` per the OOXML spec.
- The builder post-processes the file to correct a handful of OpenXml SDK
  quirks. Conformance regressions are guarded by `PptxConformanceTests`.

### Hit regions

`DiagramRenderer.GetHitRegions(DiagramRequest, DiagramOptions)` returns a
`BoardHitRegions` with point, bar, cube, and tray rectangles. The
`DiagramRequest` is required (not just `DiagramOptions`) because
`HomeBoardOnRight` controls the orientation mapping.

### TestData

Shared at `backgammon\TestData`. `TestPaths._root` resolves with five `..`
segments from the test assembly. Output layout used by visual tests:
`TestData\svg\`, `TestData\png\`, `TestData\pptx\`, `TestData\pdf\`. Visual
tests carry `[Trait("Category", "Visual")]`.

## Public API

### `DiagramRenderer`

`DiagramRenderer` is a `static class`. Every method is `public static`.
There is no constructor; no instance state is held.

```csharp
static string  RenderSvg   (DiagramRequest request, DiagramOptions options);
static BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options);
static bool    IsPdfSupported();

// Rasterization-backed formats take an optional ISvgRasterizer. When null
// (the default), a shared SkiaSharpRasterizer is used.
static byte[] RenderPng (DiagramRequest  request,  DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
static byte[] RenderPdf (DiagramRequest  request,  DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
static byte[] RenderPdf (IEnumerable<DiagramRequest> requests, DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
static byte[] RenderPptx(DiagramRequest  request,  DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
static byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
```

PDF and PPTX accept `IEnumerable<DiagramRequest>` for multi-page / multi-slide
output; a single request is handled by the scalar overload.

### `DiagramRequest.Builder`

Flat property setters for position, dice, cube, scores, plays, analysis
depths, plus `HomeBoardOnRight`, `Mode`, `AnalysisPanelPosition`. `Build()`
constructs the nested `BgDataTypes_Lib` records and validates. Throws on
validation failure.

### `DiagramOptions`

```csharp
record DiagramOptions(
    bool         ShowPipCount,
    DiagramSize  Size,
    string?      WatermarkText,
    ITheme       Theme);
```

### `ITheme` and `ThemeRegistry`

`ITheme` exposes the palette consumed by `DiagramRenderer`, including
`PanelBackgroundColor`. `ThemeRegistry.Default` and `ThemeRegistry.Greyscale`
are singleton instances; `CustomTheme` is available for callers that want
to supply their own palette.

## Pitfalls

- **QuestPDF license is the caller's responsibility.** `PdfBuilder` does not
  call `EnsureLicense`. Use `DiagramRenderer.IsPdfSupported()` to probe before
  `RenderPdf` in environments where the license may not be set.
- **`Svg.Skia.Drawable.Bounds` lies.** Anywhere you need the SVG's visible
  extent in the PNG path, parse the viewBox yourself and `ClipRect`.
- **`SKSvg` is not `IDisposable`.** Never wrap it in `using` — the compiler
  will not stop you, but disposal will break.
- **`dominant-baseline` is ignored by Svg.Skia.** Compute text vertical
  placement manually (`centreY + fontSize * 0.35`).
- **CSS stylesheets and complex SVG filters are unsupported by Svg.Skia.**
  Keep the generated SVG using inline attributes and primitive shapes.
- **`sldLayoutId < 2147483648` produces invalid PPTX.** The OOXML spec
  requires values in the reserved range; OpenXml SDK will not enforce it.
- **`HomeBoardOnRight` is geometry, not data.** The board array is never
  flipped; only `ColumnCentreX` mirrors. Anything that reaches into `Points`
  by index must use the unflipped convention.
- **Locale-dependent `ToString`.** All numeric formatting in the renderer
  must go through `InvariantCulture` — commas for decimals in some locales
  would produce broken SVG.

## Subproject-internal next steps

- Additional themes beyond `Default` and `Greyscale`.
- `FromBoard` / `FromXgid` factory methods on `DiagramRequest`.
- Animation support.
