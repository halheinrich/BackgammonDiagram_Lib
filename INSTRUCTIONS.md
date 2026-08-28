# BackgammonDiagram_Lib

> Collaboration contract: [`../AGENTS.md`](../AGENTS.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)

## Stack

C# / .NET 10 / Class Library / xUnit. Pure rendering — no user interaction, no game state.

Ships as **two assemblies** (one submodule):

- **`BackgammonDiagram_Lib`** (core) — provably **native-free**. Holds the SVG
  renderer (`RenderSvg`), the model/layout/theme types, and the pre-baked
  watermark. Safe for Blazor WASM / SVG-only consumers (BgDiag_Razor, BgQuiz).
- **`BackgammonDiagram_Lib.ExportRaster`** — the raster/export sibling. Owns all
  native deps (SkiaSharp, Svg.Skia, QuestPDF, OpenXml) and the PNG/PDF/PPTX
  output. References core; nothing in core references it.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BackgammonDiagram_Lib\BackgammonDiagram_Lib.slnx`

## Repo

https://github.com/halheinrich/BackgammonDiagram_Lib — branch `main`.

## Depends on

Core (`BackgammonDiagram_Lib`):

- **BgDataTypes_Lib** — `PositionData`, `DecisionData` (incl. `CubeDepth` /
  `CubeDepthAbbreviation` / `CubeDepthRank`), `DescriptiveData`, `CubeOwner`,
  `PlayCandidate` (incl. per-play `Depth` / `DepthAbbreviation` / `DepthRank`),
  `BgDecisionData`. The whole shared type layer this library renders from.
  **This is core's only dependency** — no native packages.

ExportRaster (`BackgammonDiagram_Lib.ExportRaster`), in addition to a project
reference to core:

- **Svg.Skia** — SVG parse/draw backend for the PNG pipeline (its `SKSvg` is
  what the rasterizer loads); brings `SkiaSharp` transitively, which the
  rasterizer consumes directly.
- **QuestPDF** — PDF layout and output (MIT licensed; license set by caller,
  not this library).
- **DocumentFormat.OpenXml** — PPTX generation.

Test-only:

- **ConvertXgToJson_Lib** — referenced by `BackgammonDiagram_Lib.Tests` for
  real-`.xg`-file fixtures used by visual and decision-data round-trip
  tests. The library itself does not depend on it; standalone test builds
  outside the umbrella checkout will not have the sibling submodule path
  available.

## Directory tree

```
BackgammonDiagram_Lib.slnx
Directory.Build.props         — repo-wide build policy (TFM, nullable, warnings-as-errors, doc file)
Directory.Packages.props
BackgammonDiagram_Lib/                    (core — native-free)
  BackgammonDiagram_Lib.csproj
  Watermarks.cs               — public static Watermarks.Default byte[] accessor
  Assets/
    board-watermark.png       — pre-baked transparent watermark (EmbeddedResource, SSOT)
  Models/
    BoardHitRegions.cs        — point/bar/cube/tray hit regions
    DiagramOptions.cs         — record: Size, WatermarkImage, Theme, Aspect, ShowXgid
    DiagramRequest.cs         — immutable class + inner Builder
    DiagramRequestExtensions.cs
    DiagramSize.cs
    Enums.cs                  — DiagramMode, PanelPosition, DiagramSizePreset
    MathUtils.cs
  Rendering/
    BoardLayout.cs            — internal geometry derived from CheckerRadius
    DiagramRenderer.cs        — SVG entry points (RenderSvg, GetHitRegions)
  Themes/
    CustomTheme.cs            — public: caller-supplied palette
    DefaultTheme.cs           — internal ITheme impl (reached via ThemeRegistry.Default)
    GreyscaleTheme.cs         — internal ITheme impl (reached via ThemeRegistry.Greyscale)
    ITheme.cs
    ThemeRegistry.cs          — static Default / Greyscale
BackgammonDiagram_Lib.ExportRaster/       (raster/export — native deps)
  BackgammonDiagram_Lib.ExportRaster.csproj
  DiagramRasterRenderer.cs    — public entry points (RenderPng/Pdf/Pptx)
  Rendering/
    ISvgRasterizer.cs         — PNG backend abstraction
    PdfBuilder.cs             — internal, QuestPDF-based
    PptxBuilder.cs            — internal, OpenXml-based
    SkiaSharpRasterizer.cs    — internal default ISvgRasterizer implementation
BackgammonDiagram_Lib.Tests/              (refs core + ExportRaster)
  BackgammonDiagram_Lib.Tests.csproj
  BearOffTests.cs
  BoardLayoutTests.cs
  BuilderFieldCarriageTests.cs  — guard: Builder carries every record field
  ColourSchemeTests.cs
  CoreNativeFreeTests.cs        — guard: core references no native package
  DecisionDataDiagramTests.cs
  DiagramRequestBuilderTests.cs
  DiagramRequestFactoryTests.cs
  DualPlayMarkerTests.cs
  HitRegionsTests.cs
  PptxConformanceTests.cs
  PptxSizingTests.cs
  RealFileCheckerDecisionTests.cs
  RealFileCubeDecisionTests.cs
  RendererPanelContentTests.cs
  RendererPlayPanelTests.cs
  RendererTitleAndRailTests.cs
  SvgStructureTests.cs
  TestFixtures.cs
  TestPaths.cs
  VisualOutputTests.cs
  WatermarksTests.cs            — incl. Default_MatchesPreBakedBytes (byte pin)
  XgidLabelTests.cs
```

## Architecture

### Assembly structure (native-free core invariant)

The submodule is split so that an SVG-only consumer never drags in native
raster libraries:

- **`BackgammonDiagram_Lib`** (core) renders to SVG and holds all the data,
  layout, and theme types. It is **provably native-free** — its only dependency
  is `BgDataTypes_Lib`. This is what Blazor WASM / SVG-only callers reference.
  The invariant is enforced at build time by `CoreNativeFreeTests`, which
  reflects over the core assembly's referenced assemblies and fails if any
  SkiaSharp / Svg.Skia / QuestPDF / OpenXml reference leaks in.
- **`BackgammonDiagram_Lib.ExportRaster`** turns that SVG into PNG / PDF / PPTX.
  It owns the four native packages and references core. `DiagramRasterRenderer`
  is its public entry point; it calls `DiagramRenderer.RenderSvg`, then
  rasterizes (`SkiaSharpRasterizer`) and packages (`PdfBuilder` / `PptxBuilder`).

The dependency arrow points one way: ExportRaster → core. Anything that needs
to rasterize lives in ExportRaster; core must never gain a native package
reference (see Pitfalls).

### DiagramRequest

Immutable class with a nested `Builder`. Callers set flat fields on the
Builder; `Build()` constructs the nested `PositionData` / `DecisionData` /
`DescriptiveData` internally, then validates:

- `Mop` must be length 26.
- `Dice` must be length 2.
- `IsCube == true` → `Dice` must be `[0, 0]`.
- `IsCube == false` → each die in `1..6`.
- `CubeSize` must be a power of 2 in `1..4096`.
- `CandidateOrdering` and a non-null `MinimumCandidateAnalysisLevel` must be
  defined enum values, and the floor must not be `AnalysisLevel.Unknown` —
  Unknown means "level not recorded", not a depth; null is the show-all
  state. The display options are caller configuration, so they get the
  validate-don't-tolerate register (unlike producer-stamped data facts).

Exposed properties:

- `Position` (PositionData), `Decision` (DecisionData),
  `Descriptive` (DescriptiveData) — the three nested data records.
- `Mode` (DiagramMode) — Problem vs Solution.
- `HomeBoardOnRight` (bool, default `true`) — geometric reflection.
- `OnRollAtBottom` (bool, default `true`) — vertical orientation of the
  on-roll half.
- `AnalysisPanelPosition` (PanelPosition) — Left or Right of the board.
- `PanelOnLeft` (bool, derived) — true ⇔
  `AnalysisPanelPosition == PanelPosition.Left`. Single declaration site
  for this derivation; both `RenderSvg` and `GetHitRegions` read it to
  share one coordinate-system rule.
- `PositionNumber` (int?, default `null`) — optional counter rendered
  right-justified in the title strip as `"Position {N}"`. Callers
  emitting a deck typically set this to a 1-based running counter so
  readers can cross-reference back to the source list.
- `CandidateOrdering` (CandidateOrdering, default `Equity`) — row order of
  the Solution-mode play panel's candidate list. `Equity` renders the
  caller's (assumed equity-sorted) order unchanged; `DepthFirst` orders by
  analysis depth, deepest first (halheinrich/backgammon#150). See the
  Analysis panel section for the ordering rule.
- `MinimumCandidateAnalysisLevel` (AnalysisLevel?, default `null`) —
  optional display floor for the play panel: hides candidates analysed
  below this level (halheinrich/backgammon#66); null shows all. See the
  Analysis panel section for the hiding rule and the never-hidden contract.
  Both depth-treatment options are consumer-set display options on the
  `SecondaryPlayIndex` model: the data-sourcing factories leave them at
  their defaults, so every export path renders unchanged.

`Mop`, `Dice`, and `Plays` live on the nested `BgDataTypes_Lib` records
(`Position.Mop`, `Decision.Dice`, `Decision.Plays`) — they are not
top-level `DiagramRequest` properties. `Builder.Build()` defensively
copies them when constructing the nested records, so external mutation
of caller-owned arrays/lists after `Build()` cannot affect a built
`DiagramRequest`. `BoardHitRegions.Points` is exposed as
`IReadOnlyDictionary`.

`DiagramRequest` is constructed by clients from `BgDecisionData` plus
rendering options — it is intentionally *not* produced by `ConvertXgToJson_Lib`.

### Diagram types

Selected by `Decision.IsCube`:

- **Checker decision** — board with dice; play list panel in Solution mode.
- **Cube decision** — board with cube indicator (no dice); cube analysis
  panel in Solution mode.

### DiagramMode

- `Problem` — board only.
- `Solution` — board plus analysis panel.

Problem and Solution diagrams have **identical overall dimensions**: the
analysis panel region is always allocated, so swapping modes never reflows
surrounding content.

### BoardLayout

`BoardLayout` is `internal` — it is a geometry-derivation detail of the
renderer, not consumer surface. Core's own tests reach it through the
existing `InternalsVisibleTo`. All geometry constants derive from
`CheckerRadius` (default 14 px). The SVG
viewBox is derived from layout totals. `HomeBoardOnRight` is a purely
geometric reflection applied in `ColumnCentreX` — no data is flipped. Hot-path
formatting uses `InvariantCulture` throughout `DiagramRenderer` to stay
locale-safe, single-sourced in the public `SvgFormat.Number`.

### Title and analysis panel

The diagram title is rendered into the SVG itself as a title strip — a
single source of truth. Neither `PdfBuilder` nor `PptxBuilder` stamps a
title on top of the rendered page; they consume the SVG/PNG as-is.

The strip has three cells, composed from context (not from
`Descriptive.Title`):

- **Col 1** (left edge, left-anchored): action text —
  `"{d1}-{d2} to play"` for checker decisions, `"Cube Action?"` for cube
  decisions. Empty for malformed inputs.
- **Col 2** (left-anchored at a fixed offset just right of col 1's
  reserved action column): `Descriptive.SourceFile`
  stem — the filename minus its final dot-extension
  (`mochy-falafel.xg` → `mochy-falafel`,
  `abc.weird.xg` → `abc.weird`). Null / empty SourceFile emits no text.
- **Col 3** (right edge, right-anchored): `"Position {N}"` when
  `DiagramRequest.PositionNumber` is set.

Strip visibility is keyed off cols 1 and 3: col 2 alone never forces the
strip on, preserving the pre-SourceFile contract for synthetic-test
requests that set only SourceFile.

`AspectPreset.BoardOnly` renders **no strip at all** — no cells are
composed for it, so the canvas is the board proper alone and its viewBox
height is the board's (ruled 2026-08-13, halheinrich/backgammon#98: the
strip's height is board budget for the quiz page's maximized answering
view, where the drawn dice carry the roll and the match name returns at
review). Consumers that need the strip's texts render a panel-bearing
preset.

`PanelBackgroundColor` is part of `ITheme`; `DefaultTheme` uses white.

### Rail text

`DiagramRenderer.FormatPlayerLabel` composes each side's rail label — away
scores, the Crawford indicator, or the money-game label. Both players' labels
are composed the same way: the Jacoby rule is a per-session fact, so both
rails say the same thing even though the label itself is per-player.

- **Match play** (`Descriptive.MatchLength != 0`) — `{name} needs {n}`, plus
  ` Crawford` when `Position.IsCrawford`. A match record carrying a non-null
  `Position.IsJacoby` is tolerated, not rejected: Jacoby is a money-game fact
  and never reaches a match label.
- **Money play** (`MatchLength == 0`, the money-game sentinel) — three
  states, tracking `Position.IsJacoby`'s three states:

  | `Position.IsJacoby` | Label                            |
  | ------------------- | -------------------------------- |
  | `true`              | `{name} (Money Game, Jacoby)`    |
  | `false`             | `{name} (Money Game, No Jacoby)` |
  | `null`              | `{name} (Money Game)`            |

  `null` means **the source did not stamp the fact**, never "off" — this
  renderer serves surfaces whose producers may legitimately not carry it, so
  an unstamped money position keeps the bare pre-#143 label. It degrades; it
  never guesses (halheinrich/backgammon#143), mirroring the
  tolerate-don't-reject register of `PositionData.IsJacoby` itself.

The fact only reaches the renderer because the Builder carries it — see the
full-copy invariant under `DiagramRequest.Builder`.

### Analysis panel

Rendered in Solution mode only. Two shapes:

- **Play panel** (`Decision.IsCube == false`). One row per visible
  `PlayCandidate`; display order and visibility are the request's
  depth-treatment options (`CandidateOrdering` /
  `MinimumCandidateAnalysisLevel`), whose defaults render every candidate in
  caller order, assumed equity-sorted — byte-identical to the pre-#150
  rendering. Columns: user's play marker, rank, move notation, equity,
  equity loss, depth. Invariants:
  - The Depth column renders `PlayCandidate.DepthAbbreviation`, not
    `PlayCandidate.Depth`. Rows with empty `DepthAbbreviation` omit the
    Depth cell entirely (the column header still renders).
  - Bold `font-weight` on the Equity and Eq Loss **values** — the figures a
    reader scans the panel for. Their column headers, the rank and move-
    notation cells, and the Depth column all keep the normal weight; the
    marker cell's bold is a separate, older rule. The weight lives
    in `RenderSvg`, so the ExportRaster sibling (PNG / PDF / PPTX) inherits it
    through the normal pipeline rather than re-encoding the style. Bold and
    the rank-inversion italic are independent attributes: an inverted row
    reads bold-italic.
  - Italic `font-style` flags a rank inversion: for row `i > 0`, italic
    is applied to the row's Equity, Eq Loss, and Depth cells when
    `plays[i].DepthRank > plays[i-1].DepthRank` — a deeper analysis sits
    below a shallower one in the equity-sorted list. The marker, rank,
    and move-notation cells stay upright. Row 0 never italic (no
    predecessor). Check is keyed off source-list position, not display
    slot — a user's play rescued into the last displayed row carries the
    italic state from its original index.
  - `CandidateOrdering.DepthFirst` (halheinrich/backgammon#150) orders rows
    by the producer-stamped `PlayCandidate.DepthRank`, descending — the data
    layer's designated ordering surface for depth comparisons, and the same
    field the rank-inversion italic compares; the renderer ranks nothing
    itself. The sort is stable, so candidates within a depth tier (equal
    rank) keep their caller (equity) order.
  - `MinimumCandidateAnalysisLevel` (halheinrich/backgammon#66) hides a
    candidate iff its numbers came from a direct evaluation
    (`AnalysisMode.Evaluation`) whose stamped `AnalysisLevel` sits strictly
    below the floor on the level axis's declared ascending-rigor order (the
    floor is inclusive: "4-ply and lower hidden" is `Ply5`). Rollout-family
    rows are never hidden — their `AnalysisLevel` is the rollout's *inner*
    level, not the analysis's own depth — and unstamped rows (`Unknown`
    mode or level) are never hidden: Unknown means "not recorded", never
    "shallow". **The best-play row and both marked rows are never hidden,
    whatever their depth** — review must always show what was best and what
    was played.
  - Every per-row treatment — rank number, the * / † marks, the
    rank-inversion italics — is keyed to the play's **source index**, so
    marks follow candidates, not row positions, under reordering, and each
    row shows its true equity rank wherever it lands.
  - When the panel runs out of vertical space, marked plays are "rescued"
    into the last visible slots with their real rank numbers, displacing
    the rows that would otherwise have been last. When a depth-treatment
    option is active, the best play is rescue-eligible too — the options
    must never push what was best out of view; under default options the
    legacy marked-rows-only window is preserved byte-for-byte.

- **Cube panel** (`Decision.IsCube == true`). Best/Actual banner,
  Equity/Loss table (No Double / Double / Take / Pass), two percentage
  tables (No Double and Take played-out stats), footer lines. Invariants:
  - The Best/Actual banner is atomic: both lines are a pair of per-half
    cube actions run through one builder (`CubeDecisionLine`). Best reads
    `DecisionData.BestDoublerAction` / `BestTakerAction`; Actual reads the
    stamped `DecisionData.UserDoublerAction` / `UserTakerAction`.
    Actual is **not** inferred from `UserDoubleError` / `UserTakeError`,
    and there is no legacy fallback to that inference: a zero error does
    not identify the action when the two cube equities tie, which used to
    misreport an equity-tie double as "No Double". A null half means the
    producer recorded no action for it — that half is omitted, and a cube
    decision with neither half stamped (a resignation-terminal record, or
    JSON written before the fields existed) drops the Actual line
    entirely.
    When both halves are present they form a complete decision and are
    classified as a `BgDataTypes_Lib.CubeDecisionPair`: the too-good pair
    (NoDouble, Pass) renders as `"Too Good"` instead of its two halves.
    That rule lives in `CubeDecisionPair.IsTooGood`, not here — the
    renderer must not re-encode "NoDouble + Pass means too good".
    Otherwise `CubeDecisionLine` renders every half that is present and
    suppresses none: presence is the only thing that decides whether a
    half appears, and no half is dropped on account of the other's value.
    A (NoDouble, Take) best therefore renders `"No Double / Take"` in
    full — the halves are analysis, the taker half states what a double
    would meet, and "no double, take" is a verdict distinct from
    "Too Good". The quiz scores both halves, so suppressing one hid the
    half the user was asked about.
  - The **stale-taker rule belongs to the Actual line's stamped-data
    boundary**, not to `CubeDecisionLine`: `DiagramRenderer
    .StampedTakerAction` drops the taker half of a stamped
    `CubeDecisionPair.NoDoubleTake` before the line is built. This is
    defence-in-depth against a producer contract violation — `DecisionData`
    validates each played half only against its own action domain and
    leaves cross-half consistency (a recorded taker response implies the
    doubler doubled) to the producer — so an opponent cannot appear to
    have taken a cube that was never offered. Only that one pair is
    filtered; (NoDouble, Pass) passes through to the Too Good
    classification. The Best line gets no such pass, because halves
    derived from the cube equities can never be out of contract.
  - `"Actual: Too Good"` is unreachable from real data **by design** — the
    Actual line reports the game's actions, and on a too-good decline the
    action played was No Double with no taker decision in existence, so
    the Too Good pair never gets stamped. Too Good is a Best-line verdict.
    Don't "fix" this by stamping a fabricated Pass: it would violate the
    producer's cross-half contract (a recorded taker response implies a
    double) to make the renderer print a decision nobody made.
  - The Analysis Level footer renders `Decision.CubeDepth` (the full
    string, e.g. `"Rollout: 1296 trials. 3-ply"`), not
    `Decision.CubeDepthAbbreviation`. The cube panel has one analysis
    depth and column space to spare, so the long form fits and is more
    informative; the play panel still uses `DepthAbbreviation` per row
    because per-play column space is tight. An empty `CubeDepth`
    suppresses the entire footer line — a non-empty abbreviation alone
    does not force the line on.
  - No italic treatment — a single analysis depth value has no adjacent
    rank to compare against.

### Watermark

On-by-default board watermark driven by `DiagramOptions.WatermarkImage`
(defaults to `Watermarks.Default`; set explicitly to `null` to opt out).
When non-null, the renderer emits two SVG `<image>` elements per diagram —
one in each half-board, rotated 90° so their tops face each other
across the bar — with:

- Size = `min(MiddleGap × 0.9, halfWidth/2 − dicePairHalf − 2×padding)`.
  The first term keeps the watermark inside the middle-gap band between
  triangle rows; the second keeps it within the bar-to-dice strip in the
  on-roll half. Applied uniformly to both halves for visual symmetry
  (dice are on one side only, but both watermarks are sized identically).
- Vertical centre at `MiddleY + MiddleGap / 2`.
- Horizontal position: each copy sits bar-adjacent, offset from the bar
  by a small padding, with its far edge short of where the dice pair
  would land on the on-roll side. Neither watermark overlaps the dice,
  so dice-layer painting doesn't hide it.
- Rotation: 90° CW for the outer (left-of-bar) half, 90° CCW for the
  inner (right-of-bar) half.
- Image bytes embedded as a `data:image/...;base64,` URI on each `<image>`
  element. MIME is sniffed from the first bytes (PNG magic → `image/png`;
  anything else defaults to `image/jpeg`). Base64 computed once per
  render, emitted twice.
- SVG-level `opacity` of `DiagramRenderer.WatermarkOpacity` (currently
  `0.22`) on top of the per-pixel alpha baked into the asset (see
  `Watermarks.Default` below).

Emitted between points and checkers in `AppendBoard`, so checkers, dice,
cube, and analysis panel all paint cleanly on top.

`Watermarks.Default` exposes the built-in asset via a cached `byte[]`
accessor. The asset is a **pre-baked transparent PNG** shipped as an
`EmbeddedResource` (`Assets/board-watermark.png`) and is the single source
of truth — `Watermarks` is a pure embedded-resource loader with no native
code, which is what keeps core WASM-clean.

The PNG was produced once from the original `board-watermark.jpg` by a
SkiaSharp transform: per-pixel luminance became inverse alpha (dark pixels
opaque, light pixels transparent, near-white pixels above a threshold of 200
forced fully transparent to kill JPEG noise) with RGB forced to pure black,
re-encoded as PNG. That transform pulled SkiaSharp into core, so it was
removed; the JPG + transform remain recoverable in git history if the
silhouette ever needs regenerating. `WatermarksTests.Default_MatchesPreBakedBytes`
pins the exact bytes (SHA-256 + length) so an accidental re-encode can't
silently shift every rendered diagram's watermark base64.

### Themes

`ITheme` interface with three concrete implementations. `DefaultTheme` and
`GreyscaleTheme` are `internal` — the built-in palettes are reached only as
`ITheme` through `ThemeRegistry.Default` / `ThemeRegistry.Greyscale`, never
by their concrete type. `CustomTheme` is `public`: it is the supported way
for a caller to supply its own palette. `DiagramOptions.Theme` is a direct
`ITheme` reference — there is no string-based lookup.

### PNG rasterization

These three sections (PNG / PDF / PPTX) all live in the
**`BackgammonDiagram_Lib.ExportRaster`** assembly, behind
`DiagramRasterRenderer`. Core is not involved beyond producing the SVG.

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
- `PdfBuilder` is `internal static` (internal to ExportRaster). Callers own the
  QuestPDF license and must configure `QuestPDF.Settings.License` themselves
  before invoking `RenderPdf`.

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

Consumers rendering overlays from these rectangles must format the
coordinates with `SvgFormat.Number` (and the viewBox with
`SvgViewBox.ToAttributeString()`) — never culture-sensitive interpolation.
See `SvgFormat` under Public API.

### TestData

Shared at `backgammon\TestData`. `TestPaths._root` resolves with five `..`
segments from the test assembly. Output layout used by visual tests:
`TestData\svg\`, `TestData\png\`, `TestData\pptx\`, `TestData\pdf\`. Visual
tests carry `[Trait("Category", "Visual")]`.

## Public API

### `DiagramRenderer` (core — `BackgammonDiagram_Lib.Rendering`)

`DiagramRenderer` is a native-free `static class`. Every method is
`public static`. There is no constructor; no instance state is held.

```csharp
static string RenderSvg(DiagramRequest request, DiagramOptions options);
static BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options);
```

### `SvgFormat` (core — `BackgammonDiagram_Lib`)

Single source of truth for the "SVG numbers are formatted invariantly"
convention. Consumers that assemble their own SVG fragments (hit-region
overlays, custom annotations) must format every number through it — culture-
sensitive interpolation emits comma decimals in locales like `nb-NO`, which
browsers parse as 0.

```csharp
static string Number(double value);   // invariant, "0.##"; throws on non-finite
```

`SvgViewBox.ToAttributeString()` builds on it: a valid `viewBox` attribute
value, identical to what `RenderSvg` emits for the same dimensions. The
renderer's internal formatting delegates to `SvgFormat.Number` — one rule,
one home.

### `DiagramRasterRenderer` (export — `BackgammonDiagram_Lib.ExportRaster`)

The raster/export entry point, also a `static class`. Lives in the
`BackgammonDiagram_Lib.ExportRaster` assembly + namespace — consumers of the
raster formats add `using BackgammonDiagram_Lib.ExportRaster;` and reference
that project. The pluggable-backend abstraction `ISvgRasterizer` is `public`
and lives here too; its default implementation `SkiaSharpRasterizer` is
`internal` — callers pass their own `ISvgRasterizer` to substitute a backend,
they never name the built-in one.

```csharp
// Rasterization-backed formats take an optional ISvgRasterizer. When null
// (the default), a shared SkiaSharpRasterizer is used.
static byte[] RenderPng(DiagramRequest request, DiagramOptions options,
                        ISvgRasterizer? rasterizer = null);
static byte[] RenderPdf(DiagramRequest request, DiagramOptions options,
                        ISvgRasterizer? rasterizer = null);
static byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options,
                        ISvgRasterizer? rasterizer = null);
static byte[] RenderPptx(DiagramRequest request, DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
static byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options,
                         ISvgRasterizer? rasterizer = null);
```

PDF and PPTX accept `IEnumerable<DiagramRequest>` for multi-page / multi-slide
output; a single request is handled by the scalar overload.

### `DiagramRequest` factory methods

```csharp
static DiagramRequest FromDecisionData(
    BgDecisionData data,
    DiagramMode    mode                  = DiagramMode.Solution,
    bool           homeBoardOnRight      = true,
    bool           onRollAtBottom        = true,
    PanelPosition  analysisPanelPosition = PanelPosition.Left);
```

Convenience entry point for the `BgDecisionData` → `DiagramRequest`
mapping. Takes the renderer-specific parameters (display mode, board
orientation, panel side) that aren't carried by the data layer. Delegates
to `Builder.From(...)` which holds the field-by-field copy logic;
callers (tests, Blazor apps, PPTX exporters) should use this entry point
rather than open-code the mapping.

`Builder.From(...)` is itself public for advanced "tweak an existing
request" scenarios — both a three-record overload
(`From(PositionData, DecisionData, DescriptiveData, …)`) and a
request-cloning overload (`From(DiagramRequest existing)`) are
available. `DiagramRequestExtensions.ToProblemSolutionPair` is the
canonical consumer of the latter.

### `DiagramRequest.Builder`

Flat property setters for position, dice, cube, scores, plays, `CubeDepth`
/ `CubeDepthAbbreviation` / `CubeDepthRank`, plus `HomeBoardOnRight`,
`OnRollAtBottom`, `Mode`, `AnalysisPanelPosition`, `PositionNumber`,
`CandidateOrdering`, and `MinimumCandidateAnalysisLevel`.
`Build()` constructs the nested `BgDataTypes_Lib` records and validates.
Throws on validation failure. `CubeOwner` defaults to `CubeOwner.Centered`.

`PlayCandidate` values flow through unchanged (Builder stores them as
`List<PlayCandidate>`, so `DepthAbbreviation` / `DepthRank` survive the
round-trip without per-field plumbing). The cube equivalents need
explicit Builder fields because the Builder models `DecisionData`
field-for-field, not as a held record.

**Full-copy invariant.** The Builder carries **every** carriable member of
`PositionData`, `DecisionData`, and `DescriptiveData`; a new field added to
any of those records joins the copy — `Builder.From` *and* `Builder.Build` —
in the same change. Because the Builder re-spells each record field-by-field
instead of holding the instance, it is a second enumeration of the data
layer's facts, and a stale one fails silently: the field simply arrives
default-valued at the renderer. That is what
`halheinrich/backgammon#122` existed to demand.

The rule is **enforced, not merely stated**: `BuilderFieldCarriageTests`
reflects over the three record types and asserts that every carriable member
survives both `Builder.From` overloads, so a new field fails the build's
tests until it is carried. Do not restate the rule as prose elsewhere —
point at that test. "Carriable" is drawn at `PropertyInfo.CanWrite == true`:
init-only accessors report `true` (reflection ignores the `init` modreq) and
are included as producer-supplied state; computed get-only properties report
`false` and are excluded, since carrying their inputs carries them.

The test needs two fixtures — a checker decision and a cube decision —
because `IsCube` and `Dice` cannot both be held away from their defaults at
once: `Build()` requires a cube decision to carry `[0, 0]` dice, which is
`Dice`'s own default.

The renderer-specific members of `DiagramRequest` itself (`Mode`,
orientation flags, `PositionNumber`, `Xgid`, `SecondaryPlayIndex`, the
depth-treatment options — everything outside the three records) are
hand-carried by `Builder.From(DiagramRequest)` only; the three-record
overload deliberately leaves them at their defaults. They get the same
reflection net in `Builder_CarriesEveryRendererSpecificField`: a new
top-level field fails that test until it is set away from default in the
fixture and survives the `From(DiagramRequest).Build()` round-trip.

### `DiagramOptions`

```csharp
record DiagramOptions
{
    DiagramSize  Size             { get; init; } = DiagramSize.Medium;
    byte[]?      WatermarkImage   { get; init; } = Watermarks.Default;
    ITheme       Theme            { get; init; } = ThemeRegistry.Default;
    AspectPreset Aspect           { get; init; } = AspectPreset.Widescreen16x9;
    bool         ShowXgid         { get; init; } = false;
}
```

`ShowXgid` bakes the request's `Xgid` into the SVG as an upper-right label
(off by default; the export path forces it off and overlays the XGID as
real selectable text instead — see `DiagramRasterRenderer`).

### `Watermarks`

`Watermarks.Default` returns the built-in watermark as a cached `byte[]` —
a pre-baked transparent PNG shipped as an `EmbeddedResource` under `Assets/`
and the single source of truth (the loader is pure managed code, no native
deps). It's the default value of `DiagramOptions.WatermarkImage`, so every
rendered diagram carries the mark unless the caller sets `WatermarkImage =
null` to opt out. The returned array is shared across calls; callers must not
mutate it.

### `ITheme` and `ThemeRegistry`

`ITheme` exposes the palette consumed by `DiagramRenderer`, including
`PanelBackgroundColor`. `ThemeRegistry.Default` and `ThemeRegistry.Greyscale`
are singleton instances; `CustomTheme` is available for callers that want
to supply their own palette.

## Pitfalls

- **Core must never gain a native package reference.** SkiaSharp, Svg.Skia,
  QuestPDF, and DocumentFormat.OpenXml belong in
  `BackgammonDiagram_Lib.ExportRaster` only — adding any of them (or a `using`
  that pulls one) to core breaks the WASM-clean invariant that BgDiag_Razor /
  BgQuiz depend on. `CoreNativeFreeTests` fails the build if one leaks in. New
  raster/export work goes in the ExportRaster sibling behind
  `DiagramRasterRenderer`, never in `DiagramRenderer`.
- **QuestPDF license is the caller's responsibility.** `PdfBuilder` does not
  call `EnsureLicense`. Configure `QuestPDF.Settings.License` in app startup
  before invoking `RenderPdf`; there is no library-side probe helper.
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
- **Play-panel Eq Loss column renders blank for best plays.** The cell is
  emitted only when `PlayCandidate.EquityLoss > 0`. `EquityLoss == 0.0`
  marks membership in the best-equity equivalence class (per
  `BgDataTypes_Lib`'s `PlayCandidate` XML doc); when multiple candidates tie
  at zero loss they all render with a blank Eq Loss cell uniformly. This
  keys off the equivalence class, not `DecisionData.BestPlayIndex` (which
  names a single canonical best). The cube panel's Equity/Loss table is
  governed independently — it always renders its loss values, including
  `0.0000` for the correct option.

## Subproject-internal next steps

- Additional themes beyond `Default` and `Greyscale`.
- `FromBoard` / `FromXgid` factory methods on `DiagramRequest`.
- Animation support.
- Single-source the per-checker stacking-Y formula. `AppendCheckerStack`
  computes each checker's centre Y inline (`base ± i·2·CheckerRadius`), and
  `HitRegionsTests` hand-copies that same formula to pin render/hit
  agreement. The stack *bound* is now single-sourced
  (`BoardLayout.MaxStackCheckers` / `MaxStackHeight`, the finding-#4 fix),
  but the per-index position formula remains duplicated — so the test
  cross-checks against a copy rather than the real method, and the two
  could drift. Expose a `BoardLayout`-level checker-centre helper (e.g.
  `CheckerCentreY(stackIndex, bottomHalf, baseY)`) that both
  `AppendCheckerStack` and the test call. Small encapsulation cleanup; do at
  the next touch. Surfaced in the #4 hit-region fix review.
- Defensive-copy paragraph polish in this doc: after the Pass C edit that
  relocated the CubeOwner-default sentence, the paragraph's remaining tail
  (the `BoardHitRegions.Points` exposure sentence) is topically
  off-paragraph. Future polish candidate.
