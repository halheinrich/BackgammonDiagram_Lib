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

- **BgDataTypes_Lib** — the validated record a decision's diagram is built
  from (`BgDecisionData`, its two kinds `CheckerPlayDecision` and
  `CubeDecision`, their categories and the session kinds), the ranking of a
  checker play's candidates (`PlayRanking`, `RankedPlays`), each cube
  action's equity and error (`CubeDecisionData.ActionEquity` and the error
  methods), a play's notation (`Play.ToNotation`, through
  `PlayCandidate.Notation`), and the board value any diagram draws
  (`BoardPosition`) with the one pip rule (`BoardState`). The whole shared
  type layer this library renders from. **This is core's only dependency** —
  no native packages.

ExportRaster (`BackgammonDiagram_Lib.ExportRaster`), in addition to a project
reference to core:

- **Svg.Skia** — SVG parse/draw backend for the PNG pipeline (its `SKSvg` is
  what the rasterizer loads); brings `SkiaSharp` transitively, which the
  rasterizer consumes directly.
- **QuestPDF** — PDF layout and output (MIT licensed; license set by caller,
  not this library).
- **DocumentFormat.OpenXml** — PPTX generation.

Test-only:

- **BgDataTypes_Lib.TestSupport** — `TestRecords`, the producer's record
  builders: the one way the tests build decision records, so no test here
  restates a record's construction.
- **ConvertXgToJson_Lib** — referenced by `BackgammonDiagram_Lib.Tests` for
  real-`.xg`-file fixtures used by the visual tests (local-only; they read
  `TestData/` and gate nothing). The library itself does not depend on it;
  standalone test builds outside the umbrella checkout will not have the
  sibling submodule path available.

## Layout

Three projects under `BackgammonDiagram_Lib.slnx`, governed by repo-root
`Directory.Build.props` (TFM, nullable, implicit usings,
`TreatWarningsAsErrors`, XML doc generation) and `Directory.Packages.props`
(Central Package Management — no inline `Version=` anywhere). Two of them
ship; the split between those two is the native-free core invariant (see
Architecture).

**`BackgammonDiagram_Lib/`** — the core: native-free, SVG only, and what
WebAssembly / SVG-only consumers reference. It declares itself
trim-compatible and runs the trim analyzer in its own build; its csproj
states why. Four areas:

- **Rendering** — `Rendering/`: `DiagramRenderer`, the SVG entry points
  (`RenderSvg`, `GetHitRegions`) and every drawing rule behind them —
  board, checkers, dice, cube, watermark placement, title strip, rail text,
  both analysis panels; the internal `DiagramPresentation`, what a request
  presents beyond its checkers, worded once for both of its sources; and
  `BoardLayout`, the internal geometry, every constant derived from
  `CheckerRadius`.
- **The request and options model** — `Models/`, whose types sit in the
  root `BackgammonDiagram_Lib` namespace rather than a `.Models` one:
  `DiagramRequest` (immutable and validated, from its three entry points)
  and `DiagramRequestExtensions` (`ToProblemSolutionPair`); a board's
  display facts, `DisplayFacts`, with the values they state — `DiceFaces`
  and the closed pair `RailScore` (`MatchRailScore`, `MoneyRailScore`);
  `DiagramOptions` with the `AspectPreset` canvas enum beside it;
  `DiagramSize`; the remaining display enums in `Enums.cs`
  (`DiagramMode`, `DiceOrder`, `PanelPosition`, `DiagramSizePreset`);
  `BoardHitRegions` — the viewBox and the point, bar, cube, tray and dice
  rectangles — with the `SvgViewBox` and `HitRect` records it is expressed
  in.
- **Themes** — `Themes/`: `ITheme`, the palette contract; `ThemeRegistry`,
  which exposes the two internal built-ins (`DefaultTheme`,
  `GreyscaleTheme`) only as `ITheme`; and `CustomTheme`, the public
  caller-supplied palette.
- **Single sources at the root** — `CubeLabels` (the wording of every cube
  answer), `SvgFormat` (invariant number formatting for SVG attributes),
  and `Watermarks`, the loader for `Assets/board-watermark.png`, the
  pre-baked watermark shipped as an `EmbeddedResource`.

**`BackgammonDiagram_Lib.ExportRaster/`** — the raster/export sibling. It
owns every native package (Svg.Skia and, through it, SkiaSharp; QuestPDF;
DocumentFormat.OpenXml) and references core; nothing in core references
it, and no WebAssembly client ships it, so it is not trim-analyzed.
`DiagramRasterRenderer` is the public entry point (PNG / PDF / PPTX);
`Rendering/` holds the public backend seam `ISvgRasterizer` with its
internal default `SkiaSharpRasterizer`, and the internal packagers
`PdfBuilder` (QuestPDF) and `PptxBuilder` (OpenXml). Every type sits in the
`BackgammonDiagram_Lib.ExportRaster` namespace, `Rendering/` included.

**`BackgammonDiagram_Lib.Tests/`** — xUnit, not packable. References both
shipped projects, `BgDataTypes_Lib` with its `TestSupport` project, and —
test-only — `ConvertXgToJson_Lib` (see "Depends on"). One class per
surface or behaviour area: the renderer's facets, the request and its
display facts, the board path, the ranking, hit regions, themes, labels and
formatting, the export formats. Every gating test synthesizes its input:
records through `TestRecords`, boards as `BoardPosition` values. Guards
enforce invariants this doc states: `CoreNativeFreeTests` (core references
no native package), `WatermarksTests.Default_MatchesPreBakedBytes` (the
watermark's exact bytes), `CollectionRiderTests` (no public member hands
out a live mutable collection or array), and two surveys of the renderer's
source, which a rendered SVG cannot show —
`RailLabels_BoldIsSpeltOnce_InTheOneRailTextEmitter` and
`CubePanel_StatesNoPassValueAndNoDoublingRuleOfItsOwn`. `TestFixtures`
holds the shared requests and boards, `PlayPanelReader` reads a rendered
play panel back into rows, and `RendererSource` reaches the renderer's
source; `TestPaths` resolves the umbrella's `TestData/`, which the
real-file and visual tests read and write — see "TestData" below.

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

What a diagram draws, and the diagram's own options for drawing it: a
`sealed record`, validated where each part is set, with no public
constructor. Three entry points (the umbrella's determination, approved by
Hal 2026-09-28 on halheinrich/backgammon#273):

- **`ForDecision(BgDecisionData decision, PlayRanking ranking)`** — a
  decision's diagram, built from the validated record, either kind. The
  request holds the record and **no copy of anything it states or
  derives**: the board, the presentation, the analysis and the XGID are read
  from the record when the diagram is drawn. The Builder that re-spelled the
  three records field by field is gone, with the test that enforced its
  full copy: holding the record leaves nothing to carry, so a field the
  records gain reaches the diagram with no edit here.
- **`ForBoard(BoardPosition board, DisplayFacts facts)`** — any other
  board: a game's final position (a side borne off, so no decision
  position), a replayed turn with no analysis. Drawn from the board and the
  display facts its caller states (below). Never a `PositionData`, a
  `Session` or a stand-in record: a match won 0-away is no valid
  `MatchSession`, and it need not be one.
- **`WithWorkingBoard(BoardPosition board, DiceOrder diceOrder)`** — on a
  request presenting a checker play: the working board of its entry
  (BgDiag_Razor's board mid-play), drawn with **its decision's
  presentation**, which this library derives from the record exactly as for
  the decision's own diagram — so an entry component never copies the
  record's facts into display facts. The checkers and pip counts are the
  working board's; the names, source, roll (in `diceOrder`, since entry lets
  its user swap the dice on screen), cube and score are the decision's. It
  keeps every option but `Mode`, which is `Problem`: a working board has no
  analysis, and no XGID (an XGID states the decision's position). A cube
  decision's request, or a board's, has none (`InvalidOperationException`).
  It applies to a working board's request again, for the next click.

Read-back: `Board` (always), `Decision` (a decision's diagram; null for a
board, a working board included), `Display` (a board request's stated facts;
null otherwise), `Xgid` (a decision's, the record's derivation; null
otherwise).

**Display facts** (`DisplayFacts`; Hal, 2026-09-28: "presentation-owned
facts rather than copied domain-decision facts"). What the diagram draws
and prints for a board beyond its checkers, each validated only for what
drawing it needs:

| Fact | Type (default) | Validated | Drawn as |
|---|---|---|---|
| `OnRollName`, `OpponentName` | `string?` | text or null; empty refused (null is the one spelling of none) | each rail's player label |
| `Title` | `string?` | text or null | the title strip's middle cell, verbatim |
| `Dice` | `DiceFaces?` | each face 1–6, the faces a die is drawn with | the dice, left first, and the strip's `"{left}-{right} to play"` |
| `CubeValue` | `int` (1) | nothing | the cube's face; 1 reads `64` |
| `CubeOwner` | `CubeOwner` (`Centered`) | a defined value, a place to draw it | where the cube sits |
| `Score` | `RailScore?` | nothing | `MatchRailScore(onRollNeeds, opponentNeeds, isCrawford)` → `"{name} needs {n}"`, with ` Crawford` and a `Cr` cube when stated, and a `Dmp` cube at 1-away/1-away; `MoneyRailScore(isJacoby)` → `"{name} (Money Game, Jacoby)"`, `(… No Jacoby)`, or the bare `(Money Game)` for none stated; null → names alone |

**The boundary** (Hal's rulings of 2026-09-28 on
halheinrich/backgammon#273). A board's presentation may carry the domain
facts it needs to reproduce that presentation: the Crawford status for a
match score and the Jacoby rule for a money score. The diagram consumes
them and neither derives nor validates their domain legality — a stated
Crawford status draws whatever the away scores beside it, as the needs
numbers draw `needs 0`. Double match point is not a supplied flag: it is
worded from the match score by the one rule a decision's session goes
through (see "Rail text and the cube's face"), so the same score never
renders differently by request path. Nothing else joins display facts: no
session beyond those facts, no decision kind (so `"Cube Action?"` stays a
decision's diagram's), no cube legality, analysis, candidate, error or
ranking; and the diagram enforces no rule of the game on what is there — a
cube limit, a legal standing or a legal roll is the domain's rule, so the
cube shows the value it is given. The diagram keeps owning how it words
what it shows: a caller states values, never drawn text. Not display facts: the board (the
request's); the orientation, panel side and position number (the request's
options, the same for every entry point); and the pip counts, read off the
drawn board by `BgDataTypes_Lib`'s one pip rule through `BoardState`'s
public counts — never supplied, never computed by a rule of this library's.

**Options.** Each is init-only and validated on every set, so a request is
varied with `with` — `request with { Mode = DiagramMode.Solution }` — and a
variation the request cannot draw is refused where it is made, by an
`ArgumentException` (or its null / out-of-range kinds) naming the member:

- `Mode` (`DiagramMode`, default `Problem`) — `Solution` is a decision's
  diagram's alone.
- `HomeBoardOnRight`, `OnRollAtBottom` (default `true`) — the orientation.
- `AnalysisPanelPosition` (a defined value) and the derived `PanelOnLeft`,
  the single declaration site `RenderSvg` and `GetHitRegions` both read.
- `PositionNumber` (`int?`) — "Position {N}" in the strip.
- `Ranking` (`PlayRanking?`) — the ranking a checker play's candidates are
  drawn by (see "Analysis panel"). Stated for every request presenting a
  decision — `ForDecision` takes it, and unsetting it is refused: none is
  assumed, so an app with a play-sorting setting cannot draw a best play it
  did not score with, and an app without one states the default,
  `Equity`. A cube decision states one too, so a caller holding either kind
  states it the same way. Null for a board.
- `MaximumHiddenCandidateAnalysisLevel` (`AnalysisLevel?`) — the ceiling
  (see "Analysis panel"): a defined level, never `Unknown`, which means "not
  recorded" and bounds nothing; null hides nothing. Accepted on any request
  presenting a decision (an app applies its setting to every solution it
  draws), refused on a board.
- `SecondaryPlayIndex` (`int?`, default null) — the † mark: a candidate of
  a checker play, refused elsewhere and out of range. It was -1 for none;
  none is null now (the arc's rule).

**Equality.** Value equality: the same record instance (records compare by
reference) or equal boards and display facts, with equal options.

`DiagramRequest` is constructed by clients from a record or a board — it is
intentionally *not* produced by `ConvertXgToJson_Lib`.

### Diagram types

What a request draws:

- **Checker decision** — board with the roll's dice; play list panel in
  Solution mode.
- **Cube decision** — board with no dice and the cube prompt; cube
  analysis panel in Solution mode.
- **Board** (`ForBoard`, `WithWorkingBoard`) — board only; the panel region
  blank.

### DiagramMode

- `Problem` — board only.
- `Solution` — board plus analysis panel; a decision's diagram only.

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

### Presentation

What a diagram draws beyond its checkers and its analysis — the title
strip's texts, the dice, the cube's face and place, the rail labels — is
resolved once per render by the internal `DiagramPresentation`, from one of
two sources: a decision's record (for a decision's diagram and a working
board) or a board's display facts. Both go through the same wording helpers
there, so a thing is worded one way whichever source states it — the score
through one method (see "Rail text and the cube's face"). The title's cube
prompt stays a decision's: it words a decision's kind. The pip
counts are not presentation: the rails read them off the drawn board.

### Title strip

The diagram title is rendered into the SVG itself as a title strip — a
single source of truth. Neither `PdfBuilder` nor `PptxBuilder` stamps a
title on top of the rendered page; they consume the SVG/PNG as-is.

The strip has three cells:

- **Col 1** (left edge, left-anchored): the action — `"{left}-{right} to
  play"` wherever dice are drawn (a checker play's roll in its rolled order,
  reversed on a working board whose user swapped them; a board's stated
  dice), `"Cube Action?"` for a cube decision, nothing otherwise.
- **Col 2** (left-anchored at a fixed offset just right of col 1's reserved
  action column): a decision's source file stem — the record's `SourceFile`
  minus its final dot-extension (`mochy-falafel.xg` → `mochy-falafel`,
  `abc.weird.xg` → `abc.weird`) — or a board's stated `Title`, verbatim.
- **Col 3** (right edge, right-anchored): `"Position {N}"` when the
  request's `PositionNumber` is set.

The strip shows when any cell has content. (It used to key off cols 1 and
3 alone, so that a synthetic request stating only a source file stayed
stripless; every record has a source file now, and a board's stated title
must be drawn.) A decision's diagram always has cols 1 and 2.

`AspectPreset.BoardOnly` renders **no strip at all** — no cells are
composed for it, so the canvas is the board proper alone and its viewBox
height is the board's (ruled 2026-08-13, halheinrich/backgammon#98: the
strip's height is board budget for the quiz page's maximized answering
view, where the drawn dice carry the roll and the match name returns at
review). Consumers that need the strip's texts render a panel-bearing
preset.

`PanelBackgroundColor` is part of `ITheme`; `DefaultTheme` uses white.

### Rail text and the cube's face

Each rail carries a player label and `Pip: N`. Both are bold on every
surface, emitted by the one helper `DiagramRenderer.AppendRailLabel`. The
pip count is the drawn board's, by `BoardState`'s count. The player label is
`"{name} {score}"`, each part drawn only where stated (a record may state no
name).

**One rule for the score, whichever path states it.** A decision's session
is read as the `RailScore` a board would state for it — a match's away
scores and Crawford status, a money session's Jacoby rule (the session's
facts are the producer's; see `BgDataTypes_Lib`'s "Money and match: the
session kinds") — and `DiagramPresentation.Scored` words either:

- a match reads `needs {n}` on each side, with ` Crawford` on both in the
  Crawford game;
- money reads `(Money Game, Jacoby)` or `(Money Game, No Jacoby)` on both
  rails, or the bare `(Money Game)` where a board's source states no rule;
- no score (a board's): the names alone.

The cube's face comes from the same method: `Dmp` at double match point (a
match with both players 1-away, where the cube is dead), taking precedence
over `Cr` in the Crawford game (played without the cube), and otherwise the
cube's value — `64` for 1. Double match point is worded from the away
scores and never stated, so 1-away/1-away draws `Dmp` on either path, and
there is no second copy of the rule; `PresentationPathAgreementTests` holds
the two paths to one rendering for the same score.

### Analysis panel

Rendered in Solution mode only — a decision's diagram. Two shapes:

- **Play panel** (a checker play). One row per visible candidate. The
  candidates arrive in the analyser's stored order; the request's ranking
  orders them. **The ranking is `BgDataTypes_Lib`'s** (SPEC-scoring §2a;
  `CheckerPlayDecisionData.RankedBy`, stated once in that library's "The
  ranking"): it decides the order, the rank numbers, the best play and
  every error, and the renderer ranks nothing itself. The candidates are
  drawn in the ranking's order, each numbered with its rank; the best play
  is the ranking's first. Columns: user's play marker, rank, move notation,
  equity, equity loss, depth. Invariants:
  - The move notation is the candidate's play's
    (`PlayCandidate.Notation`, which is `Play.ToNotation()`); a record
    stores none.
  - The Eq Loss cell is the ranking's error: blank at 0 — the best play, and
    any play tying it — the error where it is positive, and the not-scored
    mark `—` (`DiagramRenderer.PlayPanelNotScoredMark`) for a candidate the
    ranking does not score. Such a candidate (under depth first, one at
    another depth from the best that rates higher) has no error, so its row
    never shows one and never reads like the best play, whose cell is blank.
    It keeps its rank number and its place in the ranking.
  - The Depth column renders the candidate's derived
    `DepthAbbreviation`; a candidate with no depth recorded (null) omits
    the cell (the column header still renders).
  - Column placement and size (halheinrich/backgammon#252). The numeric
    block (Equity, Eq Loss, Depth) hangs off a move-text reservation of
    15 em rather than the panel's right edge. *Room* is the reservation that
    ends the Depth column at the panel's right edge less the margin; the
    *floor* is the longest move text + the column gap + the widest Equity
    cell. One font size serves the whole panel, header row and play rows
    alike; the row pitch never changes. Three steps, in order
    (`LayOutPlayPanelColumns`):
    1. **Fit at 14.** If the floor fits in the room, the size is 14 and the
       reservation `min(15 em, room)` — the full 15 em whenever there is
       room for it, so every render that fits is unchanged. Guaranteed: no
       overlap, no overrun.
    2. **Shrink to fit.** Otherwise the size is the largest at which floor
       equals room, in closed form (every horizontal term but the fixed
       left inset and right margin scales with the size), not below the
       minimum of 11. Guaranteed: no overlap, no overrun.
    3. **At the minimum, the floor wins.** If even 11 cannot fit, the
       reservation is the floor. Guaranteed: move text and Equity never
       overlap; the Depth column overruns by the least amount (floor minus
       room).

    All three are `reserve = max(floor, min(15 em, room))` at the chosen
    size. Widths are estimates — SVG built server-side cannot measure text —
    from one owner, `EstimateTextWidth`: Helvetica/Arial advance-width
    tables for regular and bold (each cell measured at the weight it is
    drawn in) times a 1.10 safety factor. The same estimator serves the
    rail labels' fit check (halheinrich/backgammon#229). On Natural and 4:3,
    where not even an empty move text fits at 14, none of this applies and
    the layout is left as it was: that pre-existing overflow is
    halheinrich/backgammon#253's.
  - Bold `font-weight` on the Equity and Eq Loss **values** — the figures a
    reader scans the panel for. Their column headers, the rank and move-
    notation cells, and the Depth column all keep the normal weight; the
    marker cell's bold is a separate, older rule. The weight lives
    in `RenderSvg`, so the ExportRaster sibling (PNG / PDF / PPTX) inherits it
    through the normal pipeline rather than re-encoding the style. Bold and
    the rank-inversion italic are independent attributes: an inverted row
    reads bold-italic.
  - Italic `font-style` flags a rank inversion: a row's Equity, Eq Loss and
    Depth cells are italic when its candidate's depth rank is higher than
    that of the candidate the ranking places immediately before it — a
    deeper analysis ranked below a shallower one. The marker, rank, and
    move-notation cells stay upright. The ranking's first is never italic
    (no predecessor), and under depth first no row is (it orders by depth);
    an unrecorded depth (a null rank) compares with nothing, being
    unrecorded rather than shallow. The check is keyed off the candidate's
    place in the ranking, not its display slot — a user's play rescued into
    the last displayed row, or a row after hidden ones, compares with its
    neighbour in the ranking.
  - `MaximumHiddenCandidateAnalysisLevel` (halheinrich/backgammon#66) hides
    a candidate iff its numbers came from a direct evaluation
    (`AnalysisMode.Evaluation`) whose `AnalysisLevel` sits at or below the
    ceiling in `AnalysisLevel`'s declared order — the producer's contract,
    which states the order (the ply and XG Roller families interleave, so a
    ply ceiling also hides the Roller levels beneath it, and only naming
    `XgRollerPlusPlus` reaches that level). Inclusive on the hide side, so
    the ruled consumer selection — "show only rollouts", the user's ruling
    of 2026-08-29 — is the top level named, and a consumer passes its
    dropdown value verbatim. Rollout-family rows are never hidden (their
    level is the rollout's inner level), and neither are rows whose level is
    not recorded: `Unknown` sits outside the order, and since `Unknown = 0`
    would otherwise rank at or below every ceiling, an explicit guard keeps
    them. **The ranking's best play and both marked rows are never hidden,
    whatever their depth** — review must always show what was best and what
    was played.
  - Every per-row treatment — rank number, the * / † marks, the
    rank-inversion italics — follows the candidate, not the row, so marks
    land on the same candidates whatever order the ranking draws them in.
  - When the panel runs out of vertical space, the marked plays are
    "rescued" into the last visible slots with their real rank numbers,
    displacing the rows that would otherwise have been last. The best play
    needs no rescue: it heads the ranking.

- **Cube panel** (a cube decision). Best/Actual banner, Equity/Loss table
  (No double / Double / Take / Pass), two percentage tables (No double and
  Take played-out stats), and an Analysis Level footer. Every word of every
  cube label comes from `CubeLabels` (see Public API); the renderer holds
  no cube wording of its own. Invariants:
  - **Each row's equity and loss are the producer's**, from its one
    calculation: `CubeDecisionData.ActionEquity(action)` — each action's
    equity in the doubler's perspective, doubling's the taker's best
    response's — and the half's error method. The renderer states neither
    the pass's value nor the rule for doubling's equity (Hal's ruling of
    2026-09-27 on halheinrich/backgammon#273), so the numbers shown are the
    ones the scoring used; `CubePanel_StatesNoPassValueAndNoDoublingRuleOfItsOwn`
    pins that no copy returns. The loss shows for every row, `0.0000` for
    the correct option.
  - **The Best line is claim-level; the Actual line is action-level.**
    Best labels `CubeDecisionData.BestClaimPair` whole — the producer's one
    derivation of the verdict — through `CubeLabels`, and never composes
    itself from the two board actions: Too good and No double share a board
    action, so a composed line printed a too-good position as
    `"No double / Take"` (`halheinrich/backgammon#185`). At the producer's
    tie boundary where the pair is the incoherent `NoDoublePass`,
    `CubeLabels` reads it `Too good` (SPEC-scoring §3), never `No double`.
  - Actual reports what was played: the stamped `UserDoublerAction` /
    `UserTakerAction`, assembled by `CubeDecisionLine`, never inferred from
    an error (a zero error does not identify the action when the equities
    tie). A null half is omitted, and a decision with neither half stamped
    drops the line. Both halves present are classified as a
    `CubeDecisionPair`, whose `IsTooGood` names the too-good pair — the
    renderer does not re-encode that rule — spelled
    `CubeLabels.Label(CubeClaimPair.TooGoodPass)`, the Best banner's
    spelling. Otherwise every present half renders, and no half is dropped
    on account of the other's value.
  - The **stale-taker rule belongs to the Actual line's stamped-data
    boundary**, not to `CubeDecisionLine`: `DiagramRenderer
    .StampedTakerAction` drops the taker half of a stamped
    `CubeDecisionPair.NoDoubleTake` before the line is built — defence in
    depth, since the record leaves cross-half consistency to its producer
    (`BgDataTypes_Lib`'s "Played cube actions on CubeDecisionData"). Only
    that pair is filtered; (NoDouble, Pass) passes through to the too-good
    classification.
  - `"Actual: Too good"` is unreachable from real data **by design**: on a
    too-good decline no taker decision exists, so the too-good pair is
    never stamped. Don't "fix" this by stamping a fabricated Pass.
  - The Analysis Level footer renders the cube analysis's derived `Depth`
    label (e.g. `"Rollout: 1296 trials. 3-ply"`), not its abbreviation: the
    cube panel has one analysis depth and column space to spare. No depth
    recorded (null) draws no footer. There is no "Pass Justifying Dbl"
    line: it read a stored field XG never stored, gone from the record
    (halheinrich/backgammon#273); deriving the figure is
    halheinrich/backgammon#288's.
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

`Watermarks.Default` exposes the built-in asset as cached immutable bytes
(`ImmutableArray<byte>`). The asset is a **pre-baked transparent PNG** shipped as an
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
`BoardHitRegions` with point, bar, cube, tray, and dice rectangles. The
`DiagramRequest` is required (not just `DiagramOptions`) because
`HomeBoardOnRight` controls the orientation mapping, the drawn board decides
which trays show, and the presentation whether dice are drawn (the dice
region is null where none are: a cube decision, a board showing none).
`Points` is an immutable copy (see "The collection rider").

Consumers rendering overlays from these rectangles must format the
coordinates with `SvgFormat.Number` (and the viewBox with
`SvgViewBox.ToAttributeString()`) — never culture-sensitive interpolation.
See `SvgFormat` under Public API.

### The collection rider

No public member hands out a live mutable collection or array, behind a
read-only interface or as a raw array (Hal, 2026-09-26, on
halheinrich/backgammon#273). This repository's sweep found three sites,
each fixed: `Watermarks.Default` and `DiagramOptions.WatermarkImage`
(immutable bytes, above), and `BoardHitRegions.Points`, which handed out
the renderer's live `Dictionary` behind `IReadOnlyDictionary` and now holds
an immutable copy taken on init. The renderers' `byte[]` results
(`RenderPng`, `RenderPdf`, `RenderPptx`, `ISvgRasterizer.Rasterize`) are
fresh arrays each call, owned by the caller, so they hand out nothing
shared. `CollectionRiderTests` pins each site, and sweeps the shipped
assemblies' public properties for an array type.

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

### `CubeLabels` (core — `BackgammonDiagram_Lib`)

Single source of truth for the user-facing wording of a cube answer —
one case throughout, sentence case. Every surface that names a cube
answer reads it here: this library's cube panel, and the consuming apps
(`halheinrich/backgammon#185`).

```csharp
static string Label(CubeClaim claim);        // No double / Double / Too good
static string Label(CubeAction action);      // No double / Double / Take / Pass
static string Label(CubeClaimPair pair);     // the pair rule, below
```

**The pair rule, in two clauses.** A pair reads as its claim alone when
that claim has exactly one reachable pair, else claim and response joined
by `" / "` — so the four reachable verdicts read `No double`,
`Double / Take`, `Double / Pass`, `Too good` (ruled 2026-09-02 on
`halheinrich/backgammon#185`; reachability is SPEC-scoring §3 as amended
2026-09-02). And the incoherent cell `NoDoublePass` reads `Too good`:
§3's sixth-cell ruling buckets it with Too good / Pass as that posture's
degenerate point — derivable only at the exact tie boundary — and a
banner must not print a verdict the model itself calls incoherent.

`Label` is therefore **not injective** over `CubeClaimPair`, by design:
`NoDoublePass` and `TooGoodPass` share a label. The pair is not lossy,
only its spelling is; callers needing to tell the two apart hold the
pair. `CubeClaimPair` is a closed 3×2 and the function is total over it,
which leaves `TooGoodTake` — unreachable as a verdict since Too good came
to require the pass — as the one cell joining on its own account, because
its response is exactly what its claim does *not* imply.

Every member is exhaustive over its type and throws
`ArgumentOutOfRangeException` outside it, including on the non-meaningful
`default(CubeClaimPair)`, whose `Taker` escapes that type's half-guards.
There is no display fallback: an unlabelled value is a programming error,
and rendering a placeholder would ship it to the reader.

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

### `DiagramRequest` (core — `BackgammonDiagram_Lib`)

```csharp
sealed record DiagramRequest
{
    static DiagramRequest ForDecision(BgDecisionData decision, PlayRanking ranking);
    static DiagramRequest ForBoard(BoardPosition board, DisplayFacts facts);
    DiagramRequest WithWorkingBoard(BoardPosition board, DiceOrder diceOrder = DiceOrder.AsRolled);

    BoardPosition   Board    { get; }   // the board drawn
    BgDecisionData? Decision { get; }   // a decision's diagram's record
    DisplayFacts?   Display  { get; }   // a board request's stated facts
    string?         Xgid     { get; }   // the record's, derived

    DiagramMode    Mode                  { get; init; }   // Problem
    bool           HomeBoardOnRight      { get; init; }   // true
    bool           OnRollAtBottom        { get; init; }   // true
    PanelPosition  AnalysisPanelPosition { get; init; }   // Left
    bool           PanelOnLeft           { get; }
    int?           PositionNumber        { get; init; }
    PlayRanking?   Ranking               { get; init; }
    AnalysisLevel? MaximumHiddenCandidateAnalysisLevel { get; init; }
    int?           SecondaryPlayIndex    { get; init; }
}

static (DiagramRequest Problem, DiagramRequest Solution)
    ToProblemSolutionPair(this DiagramRequest request);   // DiagramRequestExtensions
```

What each entry point draws, the options' rules and the equality are under
Architecture, "DiagramRequest". `ToProblemSolutionPair` is the request
`with` each mode, every other option riding both; a board has no solution.

### `DisplayFacts` and its values (core — `BackgammonDiagram_Lib`)

```csharp
sealed record DisplayFacts
{
    string?    OnRollName   { get; init; }
    string?    OpponentName { get; init; }
    string?    Title        { get; init; }
    DiceFaces? Dice         { get; init; }
    int        CubeValue    { get; init; }   // 1
    CubeOwner  CubeOwner    { get; init; }   // Centered
    RailScore? Score        { get; init; }
}

sealed record DiceFaces(int left, int right) { int Left { get; } int Right { get; } }   // each 1–6
abstract class RailScore : IEquatable<RailScore>                                        // closed
sealed class MatchRailScore(int onRollNeeds, int opponentNeeds, bool isCrawford = false) : RailScore
sealed class MoneyRailScore(bool? isJacoby = null) : RailScore
enum DiceOrder { AsRolled, Reversed }
```

What each fact means and validates is the table under Architecture,
"DiagramRequest".

### `DiagramOptions`

```csharp
record DiagramOptions
{
    DiagramSize           Size           { get; init; } = DiagramSize.Medium;
    ImmutableArray<byte>? WatermarkImage { get; init; } = Watermarks.Default;
    ITheme                Theme          { get; init; } = ThemeRegistry.Default;
    AspectPreset          Aspect         { get; init; } = AspectPreset.Widescreen16x9;
    bool                  ShowXgid       { get; init; } = false;
}
```

`WatermarkImage` is immutable bytes, or null to opt out; a default
(uninitialized) array is refused, since it holds no bytes at all.

`ShowXgid` bakes the request's `Xgid` — a decision's; a board has none — into
the SVG as an upper-right label (off by default; the export path forces it
off and overlays the XGID as real selectable text instead — see
`DiagramRasterRenderer`).

### `Watermarks`

`Watermarks.Default` returns the built-in watermark as cached
`ImmutableArray<byte>` — a pre-baked transparent PNG shipped as an
`EmbeddedResource` under `Assets/` and the single source of truth (the
loader is pure managed code, no native deps). It's the default value of
`DiagramOptions.WatermarkImage`, so every rendered diagram carries the mark
unless the caller sets `WatermarkImage = null` to opt out. The same
immutable bytes are handed out on every call, and no caller can change them
(it used to hand out its cached `byte[]`, which a caller could corrupt for
every later render — see "The collection rider").

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
- **The play panel's best play, errors and order are a ranking's.** Draw
  a checker play's candidates only through `RankedBy(request.Ranking)`:
  the Eq Loss cell is blank exactly where the ranking's error is 0 (the
  best play and any tie with it), and a candidate the ranking does not
  score shows the not-scored mark, never a number and never the blank.
  Nothing here answers "best" without the request's ranking, and the
  stored order is not the drawn order. The cube panel's Equity/Loss table
  is governed independently — it always renders its loss values,
  including `0.0000` for the correct option.

## Subproject-internal next steps

- Additional themes beyond `Default` and `Greyscale`.
- A request from an XGID string. The record derives its XGID and parses
  none, so this needs a producer-side reader first; `ForBoard` already
  covers any board.
- Animation support.
- Single-source the per-checker stacking-Y formula. `AppendCheckerStack`
  computes each checker's centre Y inline (`base ± i·2·CheckerRadius`), and
  `HitRegionsTests` hand-copies that same formula to pin render/hit
  agreement. The stack *bound* is now single-sourced
  (`BoardLayout.MaxStackCheckers` / `MaxStackHeight`, the finding-4 fix),
  but the per-index position formula remains duplicated — so the test
  cross-checks against a copy rather than the real method, and the two
  could drift. Expose a `BoardLayout`-level checker-centre helper (e.g.
  `CheckerCentreY(stackIndex, bottomHalf, baseY)`) that both
  `AppendCheckerStack` and the test call. Small encapsulation cleanup; do at
  the next touch. Surfaced in the finding-4 hit-region fix review.
