# BackgammonDiagram_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon
**After committing here, return to the Backgammon Umbrella project to update hashes and instructions doc.**

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib
**Branch:** main
**Current commit:** `084d0e3`

## Raw URLs (current commit)

* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/Enums.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/DiagramSize.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/DiagramRequest.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/DiagramOptions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/DiagramRequestExtensions.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/PlayCandidate.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Models/AnalysisDepthEntry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Themes/ITheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Themes/DefaultTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Themes/GreyscaleTheme.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Themes/ThemeRegistry.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Rendering/BoardLayout.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Rendering/ISvgRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Rendering/SkiaSharpRasterizer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Rendering/DiagramRenderer.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib/Rendering/PptxBuilder.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib.Tests/DiagramRendererTests.cs
* https://raw.githack.com/halheinrich/BackgammonDiagram_Lib/084d0e3/BackgammonDiagram_Lib.Tests/TestPaths.cs

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
* `svg/` subfolder: SVG output from tests for visual inspection
* `png/` subfolder: PNG output from tests for visual inspection
* `pptx/` subfolder: PPTX output from tests for visual inspection
* Shared across all projects — do NOT put TestData inside individual project directories
* `TestPaths._root` resolves via 5 levels up from `AppContext.BaseDirectory`

---

## Spec summary

### Diagram types

* **Checker play** — board with checkers and dice; play list panel in Solution mode
* **Cube decision** — board with checkers and cube indicator (no dice); cube analysis panel in Solution mode
* Type determined by `DiagramRequest.IsCube`

### DiagramMode

* `Problem` — board only, no analysis panel
* `Solution` — board + analysis panel always shown

### DiagramRequest
`DiagramRequest` is a **class** (not a record) with a builder pattern. Use `DiagramRequest.Builder`
to construct instances; `.Build()` validates and returns the request. Invalid state cannot be
constructed.

Validation rules enforced by `.Build()`:
- `Mop` must be length 26
- `Dice` must be length 2
- `IsCube = true` → `Dice` must be `[0, 0]`
- `IsCube = false` → `Dice[0]` and `Dice[1]` must be 1–6
- `CubeSize` must be a power of 2 from 1 to 4096
```
public class DiagramRequest
{
    public int[] Mop { get; }          // Men on Point — 26-element board array
                                        // [0]=opponent bar, [1-24]=points, [25]=on-roll bar
                                        // Positive=on-roll; negative=opponent
    public int OnRollNeeds { get; }
    public int OpponentNeeds { get; }
    public int OnRollPipCount { get; }
    public int OpponentPipCount { get; }
    public string OnRollName { get; }
    public string OpponentName { get; }
    public int CubeSize { get; }
    public CubeOwner CubeOwner { get; }
    public bool IsCube { get; }
    public int[] Dice { get; }          // Always length 2; [0,0] when IsCube=true
    public DiagramMode Mode { get; }
    public DiagramOrientation Orientation { get; }
    public bool OnRollAtBottom { get; }
    public PanelPosition AnalysisPanelPosition { get; }
    public string? Title { get; }
    public int BestPlayIndex { get; }
    public int UserPlayIndex { get; }
    public List<PlayCandidate> Plays { get; }
    public List<AnalysisDepthEntry> AnalysisDepths { get; }
    // ... cube solution fields omitted for brevity

    public class Builder { ... }        // inner builder class
}
```

### DiagramOptions
```
public class DiagramOptions
{
    public ITheme Theme { get; init; } = ThemeRegistry.Default;
    public bool ShowPipCount { get; init; }
    public DiagramSize Size { get; init; }
    public string? WatermarkText { get; init; }
}
```

`ThemeName` string has been removed. Pass an `ITheme` instance directly. Built-in themes available
as `ThemeRegistry.Default` and `ThemeRegistry.Greyscale`. Custom themes implement `ITheme`.

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
* `ThemeRegistry.Default` — returns `DefaultTheme` instance
* `ThemeRegistry.Greyscale` — returns `GreyscaleTheme` instance
* Custom themes: implement `ITheme` and pass directly to `DiagramOptions.Theme`

### Rendering API
```
string RenderSvg(DiagramRequest request, DiagramOptions options);
byte[] RenderPng(DiagramRequest request, DiagramOptions options);
byte[] RenderPdf(DiagramRequest request, DiagramOptions options);
byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options);
byte[] RenderPptx(DiagramRequest request, DiagramOptions options);
byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options);
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
* `BarCentreX(bool panelOnLeft)` convenience property

### Checker rendering

* Driven by `Mop[1..24]` for points, `Mop[25]` for on-roll bar, `Mop[0]` for opponent bar
* Positive values = on-roll checkers; negative = opponent checkers
* Stacks up to 6 deep; overflow shows count label on the boundary checker
* On-roll bar: anchored at 6th top-half checker position, grows upward, label at base
* Opponent bar: anchored at 6th bottom-half checker position, grows downward, label at base

### Dice rendering

* Shown only when `IsCube = false`
* Two dice in the middle gap, centred in the on-roll player's half
* `OnRollAtBottom = true` → right (inner) half; `OnRollAtBottom = false` → left (outer) half
* Die size = `CheckerRadius * 1.6`; rounded rect, `theme.DiceColor` fill, pip fill `#222`
* Standard pip layout (1–6) on a 3×3 grid

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
* **Known issue**: PowerPoint shows a one-time repair prompt on open — OpenXml SDK writes
  `Default Extension="xml"` with the presentation main content type instead of `application/xml`.
  Content is intact after repair. Fix deferred — candidate for Opus.

### Analysis panel (checker play, Solution mode)

* Best play (index == `BestPlayIndex`): crown icon, bold move notation, equity top-right
* Each play: move notation, primary equity top-right, equity loss stacked below
* Equity bar below each play entry
* User's play (index == `UserPlayIndex`): green checkmark
* Panel position (Left/Right) caller-specified via `AnalysisPanelPosition`

---

## Current status

✅ SVG rendering — complete (geometry, themes, rails, cube, point numbers, checkers, dice)
✅ PNG rendering — complete (SkiaSharp + Svg.Skia)
✅ PowerPoint rendering — complete (known issue: one-time repair prompt on open)
🔧 PDF rendering — stub only

## Known issues

* Point color alternation is wrong (dark/light swapped) — deferred
* Bar not visually distinct from board background — deferred
* Right rail color blends with board — deferred
* PPTX: PowerPoint shows one-time repair prompt on open — OpenXml SDK writes wrong
  `Default Extension="xml"` content type in `[Content_Types].xml`; fix deferred

## Next steps

1. Convert `DiagramRequest` from `record` to `class` with builder pattern and validation
2. Convert `DiagramOptions.ThemeName` string to `ITheme` direct reference; update `ThemeRegistry`
3. Fix PPTX repair prompt (Content_Types.xml Default xml content type) — candidate for Opus
4. PDF rendering (QuestPDF)
5. `BackgammonDiagram_Blazor` wrapper project
6. Fix point color alternation
7. Add watermark
8. Analysis panel

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
* `IsCube = true` → `Dice` must be `[0, 0]`; `IsCube = false` → `Dice` values must be 1–6
* Analysis panel always shown in Solution mode, never in Problem mode
* Panel position (Left/Right) is caller-specified
* Checker stack count shown when stack exceeds 6
* Crown icon on best play driven by `BestPlayIndex`, not a field on `PlayCandidate`
* `PlayCandidate` and `AnalysisDepthEntry` are defined in this library
* `DecisionRow` (from `ConvertXgToJson_Lib`) is a caller-side type
* `DiagramRequest` is a **class with a builder** (was a record) — invalid state unrepresentable
* `DiagramOptions.Theme` is `ITheme` (was `ThemeName` string) — custom themes just implement `ITheme`
* `ThemeRegistry` exposes static instances (`Default`, `Greyscale`) — no string lookup
* Valid cube sizes: powers of 2 from 1 to 4096
* `OnRollAtBottom` controls which player is shown at bottom; independent of `DiagramOrientation`
* All layout constants live in `BoardLayout` struct — change `CheckerRadius` and everything scales
* `effectivePanelOnLeft = hasPanel && panelOnLeft` — panel offset only applied when panel present
* Full canvas background rect drawn before board to prevent white edge artifacts in PNG
* `TestPaths._root` resolves 5 levels up from `AppContext.BaseDirectory` to reach `TestData`
* SVG test output written to `TestData\svg\` for manual inspection
* PNG test output written to `TestData\png\` for manual inspection
* PPTX test output written to `TestData\pptx\` for manual inspection
* Spec is expected to evolve during implementation

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