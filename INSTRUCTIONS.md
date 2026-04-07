# BackgammonDiagram_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon
**After committing here, return to the Backgammon Umbrella project to update hashes and instructions doc.**

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib
**Branch:** main
**Current commit:** `8689489`

## Raw URLs (current commit)

* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/Enums.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/DiagramSize.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/DiagramRequest.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/DiagramOptions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/DiagramRequestExtensions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/BoardHitRegions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Models/MathUtils.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Themes/ITheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Themes/DefaultTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Themes/GreyscaleTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Themes/ThemeRegistry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Themes/CustomTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Rendering/BoardLayout.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Rendering/ISvgRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Rendering/SkiaSharpRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Rendering/DiagramRenderer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Rendering/PptxBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib/Rendering/PdfBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/BoardLayoutTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/SvgStructureTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/VisualOutputTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/DiagramRequestBuilderTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/HitRegionsTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/PptxConformanceTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/TestFixtures.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/TestPaths.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/8689489/BackgammonDiagram_Lib.Tests/ColourSchemeTests.cs

## Stack

C# / .NET 10 / Class Library / Visual Studio 2026 / Windows

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BackgammonDiagram_Lib\BackgammonDiagram_Lib.slnx`

## Purpose

A pure rendering library. Takes a `DiagramRequest` and returns a board diagram as SVG, PNG, PDF, or
PowerPoint. Has no knowledge of user interaction, game state, or decision flow — the caller owns all
of that.

## Dependencies

* **BgDataTypes_Lib** (commit `bcffabf`) — shared domain types: `PositionData`, `DecisionData`,
  `DescriptiveData`, `CubeOwner`, `PlayCandidate`, `AnalysisDepthEntry`
* **SkiaSharp 3.119.2** — PNG rendering surface and canvas
* **Svg.Skia 3.6.0** — SVG parsing and drawing (replaces SkiaSharp.Svg)
* **SVG — hand-rolled** — no SVG library needed for SVG output
* **QuestPDF 2026.2.3** — PDF rendering (MIT licensed)
* **DocumentFormat.OpenXml 3.5.1** — PowerPoint output

## Related projects

* **BackgammonDiagram_Blazor** — thin Blazor class library wrapper; references this lib and exposes
  a `BackgammonDiagram.razor` component that calls `RenderSvg` and injects the result as
  `MarkupString`. Kept separate so the core lib has no Blazor dependency.

## Shared infrastructure

### TestData

* Location: `D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\TestData`
* `BothWays/` subfolder: `ThisWay.xg` and `ThatWay.xg` — same match, perspectives reversed
* `svg/` subfolder: SVG and PNG output from tests for visual inspection
* `pptx/` subfolder: PPTX output from tests for visual inspection
* `pdf/` subfolder: PDF output from tests for visual inspection
* Shared across all projects — do NOT put TestData inside individual project directories
* `TestPaths._root` resolves via 5 levels up from `AppContext.BaseDirectory`

---

## Spec summary

### Diagram types

* **Checker play** — board with dice inside; play list panel in Solution mode
* **Cube decision** — board with cube indicator (no dice); cube analysis panel in Solution mode
* Type determined by `DiagramRequest.Decision.IsCube`

### DiagramMode

* `Problem` — board only, no analysis panel
* `Solution` — board + analysis panel always shown

### DiagramRequest

`DiagramRequest` is an immutable class constructed exclusively via its inner `Builder`:
```csharp
var request = new DiagramRequest.Builder
{
    Mop = new int[26],
    IsCube = false,
    Dice = [3, 1],
    CubeSize = 2,
    // ... other fields
}.Build();
```

`Build()` enforces:
- `Mop` must be length 26
- `Dice` must be length 2
- `IsCube = true` → `Dice` must be `[0, 0]`
- `IsCube = false` → each die value must be 1–6
- `CubeSize` must be a power of 2 from 1 to 4096

The `Builder` is flat — callers set all fields directly on the builder. `Build()` constructs
the nested objects internally. The built `DiagramRequest` exposes: