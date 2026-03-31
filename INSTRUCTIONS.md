# BackgammonDiagram_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon
**After committing here, return to the Backgammon Umbrella project to update hashes and instructions doc.**

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib
**Branch:** main
**Current commit:** `e036b1d`

## Raw URLs (current commit)

* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/Enums.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/DiagramSize.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/DiagramRequest.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/DiagramOptions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/DiagramRequestExtensions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/PlayCandidate.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/AnalysisDepthEntry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Models/BoardHitRegions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Themes/ITheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Themes/DefaultTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Themes/GreyscaleTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Themes/ThemeRegistry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Rendering/BoardLayout.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Rendering/ISvgRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Rendering/SkiaSharpRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Rendering/DiagramRenderer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Rendering/PptxBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib/Rendering/PdfBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib.Tests/DiagramRendererTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib.Tests/PptxConformanceTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib.Tests/HitRegionsTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/e036b1d/BackgammonDiagram_Lib.Tests/TestPaths.cs

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
* **QuestPDF 2026.2.3** — PDF rendering (Community license)
* **DocumentFormat.OpenXml 3.5.1** — PowerPoint output

## Related projects

* **BgDiag_Blazor** — thin Blazor class library wrapper; references this lib and exposes
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

### DiagramRequest (key fields)
```
public record DiagramRequest
{
    public int[] Mop { get; init; }        // Men on Point — 26-element board array
                                            // [0]=opponent bar, [1-24]=points, [25]=on-roll bar
                                            // Positive=on-roll; negative=opponent
    public int OnRollNeeds { get; init; }
    public int OpponentNeeds { get; init; }
    public int OnRollPipCount { get; init; }
    public int OpponentPipCount { get; init; }
    public string OnRollName { get; init; }
    public string OpponentName { get; init; }
    public int CubeSize { get; init; }
    public CubeOwner CubeOwner { get; init; }
    public bool IsCube { get; init; }
    public int[] Dice { get; init; }        // Always length 2; ignored when IsCube=true
    public DiagramMode Mode { get; init; }
    public bool OnRollAtBottom { get; init; } = true;
    public PanelPosition AnalysisPanelPosition { get; init; }
    public string? Title { get; init; }    // Optional title for PowerPoint/PDF output
    // ... cube and play solution fields omitted for brevity
    public int BestPlayIndex { get; init; }
    public int UserPlayIndex { get; init; } = -1;
    public List<PlayCandidate> Plays { get; init; }
    public List<AnalysisDepthEntry> AnalysisDepths { get; init; }
}
```

### DiagramOptions
```
public class DiagramOptions
{
    public bool ShowPipCount { get; init; }
    public DiagramSize Size { get; init; }
    public string? WatermarkText { get; init; }
    public string? ThemeName { get; init; }   // "Default" or "Greyscale"
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

### Hit region types
```
public class BoardHitRegions
{
    public required SvgViewBox ViewBox { get; init; }
    public required Dictionary<int, HitRect> Points { get; init; }  // 1–24
    public required HitRect Bar { get; init; }
    public HitRect? Cube { get; init; }
    public HitRect? OnRollTray { get; init; }
}

public record SvgViewBox(double X, double Y, double Width, double Height);
public record HitRect(double X, double Y, double Width, double Height);
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
* `ThemeRegistry.Resolve(string? name)` — falls back to `DefaultTheme`

### Rendering API
```
string RenderSvg(DiagramRequest request, DiagramOptions options);
byte[] RenderPng(DiagramRequest request, DiagramOptions options);
byte[] RenderPdf(DiagramRequest request, DiagramOptions options);
byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options);
byte[] RenderPptx(DiagramRequest request, DiagramOptions options);
byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options);
BoardHitRegions GetHitRegions(DiagramOptions options);
```

### Hit regions API (GetHitRegions)

* Returns axis-aligned `HitRect` rectangles in SVG viewBox coordinate space
* Depends on `DiagramOptions` only — not `DiagramRequest` (piece positions don't move the points)
* Assumes no panel (Problem mode) — coordinates match board-only `RenderSvg()` output
* Points 1–24: each rect is `ColumnWidth` wide, centered on `ColumnCentreX`, covering triangle area
* Bar: full bar strip, top to bottom
* Cube: full left-rail column (covers all possible cube vertical positions)
* OnRollTray: `null` (bearing-off tray not yet rendered)
* ViewBox: `(0, 0, BoardWidth, BoardHeight)` — no panel
* 10 tests in `HitRegionsTests.cs`

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
* PPTX repair prompt fixed — six regression tests in `PptxConformanceTests.cs` guard all
  post-processing fixes (content types, relative .rels targets, namespace hoisting,
  sequential rId format, XML declaration format, valid sldLayoutId)

### PDF rendering (PdfBuilder)

* Internal static class — called only by `DiagramRenderer`
* Each `DiagramRequest` → one page via `RenderPng` → embedded image
* Page dimensions: widescreen landscape 13.33" × 7.5" (matching PPTX)
* PNG embedded with `FitArea()` — aspect ratio preserved automatically
* Optional `Title` rendered in page header, centered above image
* QuestPDF Community license (free for < $1M revenue)
* PDF output written to `TestData\pdf\` for manual inspection

### Analysis panel (checker play, Solution mode)

* Best play (index == `BestPlayIndex`): crown icon, bold move notation, equity top-right
* Each play: move notation, primary equity top-right, equity loss stacked below
* Equity bar below each play entry
* User's play (index == `UserPlayIndex`): green checkmark
* Panel position (Left/Right) caller-specified via `AnalysisPanelPosition`

---

## Current status

✅ SVG rendering — complete (geometry, themes, rails, cube, point numbers)
✅ PNG rendering — complete (SkiaSharp + Svg.Skia)
✅ PowerPoint rendering — complete (repair prompt fixed, six regression tests in `PptxConformanceTests.cs`)
✅ PDF rendering — complete (QuestPDF, widescreen landscape, PNG embedding)
✅ Hit regions API — complete (`GetHitRegions`, 10 tests in `HitRegionsTests.cs`)

## Known issues

* Point color alternation is wrong (dark/light swapped) — deferred
* Bar not visually distinct from board background — deferred
* Right rail color blends with board — deferred

## Next steps

1. `BgDiag_Blazor` wrapper project
2. Fix point color alternation
3. Add checkers
4. Add dice
5. Watermark
6. Analysis panel

## Deferred

* `FromBoard(int[] board)` and `FromXgid(string xgid)` factory methods on `DiagramRequest`
* Additional themes beyond Default and Greyscale
* Animation
* `BgDiag_Blazor` wrapper project

## Key decisions

* Pure rendering library — no user interaction, no game state
* Blazor support lives in separate `BgDiag_Blazor` wrapper project
* SVG is hand-rolled (no SVG library)
* PNG uses SkiaSharp 3.119.2 + Svg.Skia 3.6.0 (NOT SkiaSharp.Svg)
* `ISvgRasterizer` interface isolates SkiaSharp — swappable without touching `DiagramRenderer`
* `Svg.Skia`'s `Drawable.Bounds` is unreliable — use `ParseViewBox` + `ClipRect` workaround
* `SKSvg` is not `IDisposable` — never use `using` on it
* `dominant-baseline` ignored by SkiaSharp — use `textY = centreY + fontSize * 0.35`
* Avoid CSS stylesheets and complex SVG filters — Svg.Skia has limited SVG support
* QuestPDF for PDF output (Community license at v2024+)
* OpenXml for PowerPoint output
* PPTX: each DiagramRequest → one slide (PNG embedded); caller assembles multi-slide decks
* PPTX: `Title` on `DiagramRequest` renders as text box below image on slide
* PPTX: `ToProblemSolutionPair()` extension produces paired Problem/Solution requests
* PPTX: `PptxBuilder` is internal static — not part of public API
* PPTX: slide canvas 13.33" × 7.5" widescreen (12192000 × 6858000 EMUs)
* PPTX: OOXML `sldLayoutId` must be ≥ 2147483648 (0x80000000)
* PDF: each DiagramRequest → one page (PNG embedded); caller assembles multi-page docs
* PDF: `Title` on `DiagramRequest` renders in page header, centered above image
* PDF: `PdfBuilder` is internal static — not part of public API
* PDF: widescreen landscape 13.33" × 7.5" matching PPTX dimensions
* PDF: QuestPDF Community license (free for < $1M revenue)
* Hit regions: `GetHitRegions(DiagramOptions)` — no `DiagramRequest` dependency
* Hit regions: assumes no panel (Problem mode) — interactive use case
* Hit regions: cube hit rect covers full left rail (all possible positions)
* Hit regions: `OnRollTray` is null until bearing-off tray is rendered
* `IsCube` drives diagram type and panel content
* Analysis panel always shown in Solution mode, never in Problem mode
* Panel position (Left/Right) is caller-specified
* Checker stack count shown when stack exceeds 6
* Crown icon on best play driven by `BestPlayIndex`, not a field on `PlayCandidate`
* `PlayCandidate` and `AnalysisDepthEntry` are defined in this library
* `DecisionRow` (from `ConvertXgToJson_Lib`) is a caller-side type
* `DiagramRequest` is a `record` (enables `with` expressions in tests)
* `OnRollAtBottom` controls which player is shown at bottom; independent of `HomeBoardOnRight`
* All layout constants live in `BoardLayout` struct — change `CheckerRadius` and everything scales
* `effectivePanelOnLeft = hasPanel && panelOnLeft` — panel offset only applied when panel present
* Full canvas background rect drawn before board to prevent white edge artifacts in PNG
* `TestPaths._root` resolves 5 levels up from `AppContext.BaseDirectory` to reach `TestData`
* SVG/PNG test output written to `TestData\svg\` for manual inspection
* PPTX test output written to `TestData\pptx\` for manual inspection
* PDF test output written to `TestData\pdf\` for manual inspection
* Spec is expected to evolve during implementation
* DiagramOrientation enum removed; replaced by bool HomeBoardOnRight (default true) on DiagramRequest
* HomeBoardOnRight = false mirrors board horizontally via 25-point substitution in ColumnCentreX and effectivePt in AppendPoints

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