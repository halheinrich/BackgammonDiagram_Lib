# BackgammonDiagram_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon
**After committing here, return to the Backgammon Umbrella project to update hashes and instructions doc.**

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib
**Branch:** main
**Current commit:** `dbe4387`

## Raw URLs (current commit)

* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/Enums.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/DiagramSize.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/DiagramRequest.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/DiagramOptions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/DiagramRequestExtensions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/PlayCandidate.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/AnalysisDepthEntry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Models/BoardHitRegions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Themes/ITheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Themes/DefaultTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Themes/GreyscaleTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Themes/ThemeRegistry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Rendering/BoardLayout.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Rendering/ISvgRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Rendering/SkiaSharpRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Rendering/DiagramRenderer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Rendering/PptxBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib/Rendering/PdfBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/BoardLayoutTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/SvgStructureTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/VisualOutputTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/DiagramRequestBuilderTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/HitRegionsTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/PptxConformanceTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/TestFixtures.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/dbe4387/BackgammonDiagram_Lib.Tests/TestPaths.cs

## Stack

C# / .NET 10 / Class Library / Visual Studio 2026 / Windows

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BackgammonDiagram_Lib\BackgammonDiagram_Lib.slnx`

## Purpose

A pure rendering library. Takes a `DiagramRequest` and returns a board diagram as SVG, PNG, PDF, or
PowerPoint. Has no knowledge of user interaction, game state, or decision flow — the caller owns all
of that.

## Dependencies

* **ConvertXgToJson_Lib** (commit `d5c3ed6`) — board array convention reference only; `DecisionRow`
  is a caller-side mapping type, not consumed by the renderer directly
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
* Type determined by `DiagramRequest.IsCube`

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

Key fields:
```
int[] Mop              // 26-element board array; [0]=opponent bar, [1-24]=points, [25]=on-roll bar
int OnRollNeeds
int OpponentNeeds
int OnRollPipCount
int OpponentPipCount
string OnRollName
string OpponentName
int CubeSize
CubeOwner CubeOwner
bool IsCube
int[] Dice             // Always length 2; [0,0] when IsCube=true
DiagramMode Mode
bool HomeBoardOnRight  // true = on-roll player's home board on right
bool OnRollAtBottom    // true = on-roll player shown at bottom
PanelPosition AnalysisPanelPosition
string? Title          // Optional slide title for PowerPoint/PDF output
int BestPlayIndex
int UserPlayIndex      // -1 if not applicable
List<PlayCandidate> Plays
List<AnalysisDepthEntry> AnalysisDepths
```

### DiagramOptions
```
public class DiagramOptions
{
    public bool ShowPipCount { get; init; }
    public DiagramSize Size { get; init; }
    public string? WatermarkText { get; init; }
    public ITheme Theme { get; init; }   // default: ThemeRegistry.Default
}
```

### Supporting types
```
public enum PanelPosition { Left, Right }

public class PlayCandidate
{
    public string MoveNotation { get; init; } = string.Empty;
    public double Equity { get; init; }
    public double? EquityLoss { get; init; }
    public bool IsUserPlay { get; init; }
}

public class AnalysisDepthEntry
{
    public string Label { get; init; } = string.Empty;
}
```

### DiagramRequestExtensions
```csharp
(DiagramRequest Problem, DiagramRequest Solution) ToProblemSolutionPair(this DiagramRequest request)
```

Titles default to "Problem" / "Solution"; if `request.Title` is set they become
"{Title} — Problem" / "{Title} — Solution".

### Themes

* `DefaultTheme` — warm brown board, dark red / wheat points
* `GreyscaleTheme` — grey board, dark grey / light grey points
* `ThemeRegistry.Default` — static instance of DefaultTheme
* `ThemeRegistry.Greyscale` — static instance of GreyscaleTheme

### Rendering API
```
string RenderSvg(DiagramRequest request, DiagramOptions options);
byte[] RenderPng(DiagramRequest request, DiagramOptions options);
byte[] RenderPdf(DiagramRequest request, DiagramOptions options);
byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options);
byte[] RenderPptx(DiagramRequest request, DiagramOptions options);
byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options);
BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options);
static bool IsPdfSupported();
```

### Board layout (BoardLayout struct)

* All constants derive from `CheckerRadius` (default 14px)
* ViewBox: derived from `BoardLayout` totals — no hardcoded viewbox
* Two halves (outer/inner) 6 points each × 28px column = 168px per half
* Bar: 30.8px (bar checkers only — no dice)
* Left rail: 42px (cube), Right rail: 21px (decorative)
* Point height: 5 × diameter = 140px
* Middle gap: 2.5 × diameter = 70px (dice and watermark live here)
* Top/bottom rails: 28px each (name + pip + score)
* Point number strips: 20px each
* Full canvas background rect drawn first to prevent transparent edge artifacts in PNG

### PNG rasterization (SkiaSharpRasterizer)

* Implements `ISvgRasterizer` — swappable without touching `DiagramRenderer`
* Uses `Svg.Skia.SKSvg` to parse and draw SVG
* `Svg.Skia`'s `Drawable.Bounds` is unreliable (reports wider than actual content)
* Workaround: use `ParseViewBox` for dimensions; use `SKPictureRecorder` with `ClipRect`
  set to viewBox bounds before calling `svg.Draw()` — this clips overflow content
* `SKSvg` is not `IDisposable` — do not use `using`
* `dominant-baseline` is ignored by SkiaSharp — use explicit baseline offset instead:
  `textY = centreY + fontSize * 0.35`

### PowerPoint rendering (PptxBuilder)

* Internal static class — called only by `DiagramRenderer`
* Each `DiagramRequest` → one slide via `RenderPng` → embedded image
* Slide canvas: 13.33" × 7.5" (12192000 × 6858000 EMUs)
* PNG aspect ratio preserved; image centered horizontally, top-aligned vertically
* Optional `Title` rendered as text box below image when present
* `ToProblemSolutionPair()` extension method on `DiagramRequest` produces paired Problem/Solution requests
* PPTX output written to `TestData\pptx\` for manual inspection

### Hit regions (GetHitRegions)

* Returns `BoardHitRegions` with `SvgViewBox`, point hit rects, bar, cube
* Coordinates in SVG viewBox space matching `RenderSvg()` Problem-mode output
* `OnRollTray` is null — bearing-off tray not yet rendered
* 10 regression tests in `HitRegionsTests.cs`

### Analysis panel (checker play, Solution mode)

* Best play (index == `BestPlayIndex`): crown icon, bold move notation, equity top-right
* Each play: move notation, primary equity top-right, equity loss stacked below
* Equity bar below each play entry
* User's play (index == `UserPlayIndex`): green checkmark
* Panel position (Left/Right) caller-specified via `AnalysisPanelPosition`

### Test fixtures

* `TestFixtures` static class in test project — shared builder helpers
* `TestFixtures.MinimalBuilder()` — returns a pre-populated `DiagramRequest.Builder`
* `TestFixtures.MinimalRequest()` — calls `MinimalBuilder().Build()`
* `TestFixtures.DefaultOptions()` / `TestFixtures.GreyscaleOptions()`
* All tests must construct `DiagramRequest` via `Builder` — never directly

---

## Current status

✅ SVG rendering — complete (geometry, themes, rails, cube, point numbers, checkers, dice)
✅ PNG rendering — complete (SkiaSharp + Svg.Skia)
✅ PDF rendering — complete (QuestPDF)
✅ PowerPoint rendering — complete (known issue: one-time repair prompt on open)
✅ Hit regions — complete (`GetHitRegions`, 10 tests)
✅ DiagramRequest — immutable class with inner Builder + validation
✅ DiagramOptions.Theme — ITheme direct reference (ThemeRegistry.Default / .Greyscale)

## Known issues

* Point color alternation is wrong (dark/light swapped) — deferred
* Bar not visually distinct from board background — deferred
* Right rail color blends with board — deferred
* PPTX: PowerPoint shows one-time repair prompt on open — deferred

## Next steps

1. `BackgammonDiagram_Blazor` wrapper project
2. Fix point color alternation
3. Add analysis panel
4. Watermark
5. Fix PPTX repair prompt

## Deferred

* `FromBoard(int[] board)` and `FromXgid(string xgid)` factory methods on `DiagramRequest`
* Additional themes beyond Default and Greyscale
* Animation
* `BackgammonDiagram_Blazor` wrapper project

## Key decisions

* Pure rendering library — no user interaction, no game state
* Blazor support lives in separate `BackgammonDiagram_Blazor` wrapper project
* SVG is hand-rolled (no SVG library)
* PNG uses SkiaSharp 3.119.2 + Svg.Skia 3.6.0 (NOT SkiaSharp.Svg)
* `ISvgRasterizer` interface isolates SkiaSharp — swappable without touching `DiagramRenderer`
* `Svg.Skia`'s `Drawable.Bounds` is unreliable — use `ParseViewBox` + `ClipRect` workaround
* `SKSvg` is not `IDisposable` — never use `using` on it
* `dominant-baseline` ignored by SkiaSharp — use `textY = centreY + fontSize * 0.35`
* Avoid CSS stylesheets and complex SVG filters — Svg.Skia has limited SVG support
* QuestPDF for PDF output (MIT licensed at v2024+)
* OpenXml for PowerPoint output
* PPTX: each DiagramRequest → one slide (PNG embedded); caller assembles multi-slide decks
* PPTX: `Title` on `DiagramRequest` renders as text box below image on slide
* PPTX: `ToProblemSolutionPair()` extension produces paired Problem/Solution requests
* PPTX: `PptxBuilder` is internal static — not part of public API
* PPTX: slide canvas 13.33" × 7.5" widescreen (12192000 × 6858000 EMUs)
* `IsCube` drives diagram type and panel content
* Analysis panel always shown in Solution mode, never in Problem mode
* Panel position (Left/Right) is caller-specified
* Checker stack count shown when stack exceeds 6
* Crown icon on best play driven by `BestPlayIndex`, not a field on `PlayCandidate`
* `PlayCandidate` and `AnalysisDepthEntry` are defined in this library
* `DecisionRow` (from `ConvertXgToJson_Lib`) is a caller-side type
* `DiagramRequest` is an immutable class with inner `Builder`; all construction via `Builder.Build()`
* `DiagramRequest` constructor is `internal`; properties are `init`-only
* `Builder.Build()` uses object initializer on `new DiagramRequest()` — valid because `init` allows this from any context within the same assembly
* `DiagramOrientation` enum removed — replaced by `bool HomeBoardOnRight` on `DiagramRequest`
* `DiagramOptions.Theme` is `ITheme` direct reference — `ThemeName` string removed
* `ThemeRegistry` provides static instances `Default` and `Greyscale` — `Resolve(string)` removed
* `OnRollAtBottom` controls which player is shown at bottom
* `HomeBoardOnRight` controls which half is the home board; independent of `OnRollAtBottom`
* All layout constants live in `BoardLayout` struct — change `CheckerRadius` and everything scales
* `effectivePanelOnLeft = hasPanel && panelOnLeft` — panel offset only applied when panel present
* Full canvas background rect drawn before board to prevent white edge artifacts in PNG
* `TestPaths._root` resolves 5 levels up from `AppContext.BaseDirectory` to reach `TestData`
* SVG/PNG test output written to `TestData\svg\` for manual inspection
* PPTX test output written to `TestData\pptx\` for manual inspection
* PDF test output written to `TestData\pdf\` for manual inspection
* `TestFixtures` provides shared builder helpers — all tests use Builder, never direct construction
* Spec is expected to evolve during implementation
* INSTRUCTIONS.md must be updated and committed as the final act of every session
* Builder.CubeOwner defaults to CubeOwner.Centered — class property has no initializer (always set by Build())
* Code review findings: see [CodeReview.md](CodeReview.md)
* DiagramRenderer.F() uses InvariantCulture — SVG is locale-safe
* AppendCheckers passes request.HomeBoardOnRight to ColumnCentreX — checker columns match point triangles
* PdfBuilder does not set QuestPDF license — caller must set QuestPDF.Settings.License at startup; use DiagramRenderer.IsPdfSupported() to validate
* DiagramRenderer.IsPdfSupported() checks QuestPDF.Settings.License.HasValue; catches native dependency load failures; returns false on unsupported runtimes
* Test assemblies that call RenderPdf must set QuestPDF.Settings.License in a static constructor
* DiagramOptions is now a record; DiagramSize presets are static readonly fields; Mop/Dice exposed as IReadOnlyList<int>; Plays/AnalysisDepths as IReadOnlyList<T>; BoardHitRegions.Points as IReadOnlyDictionary; test categories: Visual tests in VisualOutputTests with [Trait("Category", "Visual")]

## Shared rules

See `AGENTS.md` in the umbrella repo — applies to all sub-projects.
`https://raw.githack.com/halheinrich/backgammon/main/AGENTS.md`

## Session handoff

After committing:

1. `git rev-parse --short HEAD` in this subproject dir — note the short hash
2. Update commit hash in this doc and all raw URLs
3. Add URLs for any new files created
4. Update Known issues / Next steps / Current status / Key decisions
5. Return to Backgammon Umbrella project — update umbrella instructions doc