# BackgammonDiagram_Lib

> Collaboration contract: [`../AGENTS.md`](../AGENTS.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)
> Open work: the [tracker](https://github.com/halheinrich/backgammon/issues), label `BackgammonDiagram_Lib`.
> Repo: https://github.com/halheinrich/BackgammonDiagram_Lib, branch `main`.

## What it is for

Draws backgammon positions. Give it a decision from a match file, as a
problem or with its analysis panel, or any other board with the facts to
print beside it, and it returns the diagram as SVG, with the rectangles that
make the SVG clickable, or as PNG, PDF or PPTX. It is pure rendering: no user
interaction and no game state. A host owns the clicks and the state, and the
data comes from `BgDataTypes_Lib`.

C# / .NET 10 class libraries, tested with xUnit. Two assemblies ship from one
submodule, split so that an SVG-only consumer never drags in a native library:

- **`BackgammonDiagram_Lib`** (core) — the SVG renderer, the request, option,
  layout and theme types, and the pre-baked watermark. Native-free, and what
  Blazor WebAssembly and other SVG-only consumers (BgDiag_Razor, BgQuiz's
  client) reference.
- **`BackgammonDiagram_Lib.ExportRaster`** — PNG, PDF and PPTX output. Owns
  every native and export package. References core; nothing in core references
  it.

## Using it

```csharp
using BackgammonDiagram_Lib;                // requests and options
using BackgammonDiagram_Lib.Rendering;      // DiagramRenderer
using BackgammonDiagram_Lib.ExportRaster;   // PNG, PDF, PPTX only
using BgDataTypes_Lib;

// A decision with its analysis panel. The record states everything shown.
var request = DiagramRequest.ForDecision(record, PlayRanking.Equity) with { Mode = DiagramMode.Solution };
var options = new DiagramOptions();         // 16:9, default theme, watermark on

string svg = DiagramRenderer.RenderSvg(request, options);
BoardHitRegions regions = DiagramRenderer.GetHitRegions(request, options);  // the same canvas's rectangles
byte[] png = DiagramRasterRenderer.RenderPng(request, options);

// Any other board: the caller states what the diagram prints beside it.
var final = DiagramRequest.ForBoard(board, new DisplayFacts { OnRollName = "Alice", Title = "Final position" });
```

Themes are in `BackgammonDiagram_Lib.Themes`. Each public member's XML doc
states its contract; `GenerateDocumentationFile` builds them on every build.
This file holds what those cannot: why it is built this way, and what to avoid.

## Build and test

The solution is `BackgammonDiagram_Lib.slnx`. It needs the umbrella checkout:
three of its six projects are siblings reached by relative path
(`BgDataTypes_Lib`, its `TestSupport` project, `ConvertXgToJson_Lib`).

```bash
dotnet build BackgammonDiagram_Lib.slnx
```

```bash
dotnet test BackgammonDiagram_Lib.slnx
```

Four test classes carry `[Trait("Category", "Visual")]`. They work against the
umbrella's `TestData/`, which `TestPaths` reaches five `..` above the test
assembly and whose contents are gitignored (AGENTS § Tests), so what they prove
depends on what is there:

| Class | Input | With no fixtures |
|---|---|---|
| `DecisionDataDiagramTests` | every `DiagramRequest/*.json` | passes on a sentinel row, having drawn nothing |
| `RealFileCheckerDecisionTests`, `RealFileCubeDecisionTests` | the first `xg/*.xg`, then `xgp/*.xgp` | fails: each asserts that `xg/` holds a file |
| `VisualOutputTests` | synthesized | runs in full; it only asserts that each diagram rendered |

The Visual classes write to `TestData`'s `svg/`, `png/`, `pptx/` and `pdf/` for
a person to look at, and one `ColourSchemeTests` test writes an SVG there too.
On a checkout without match files, leave the Visual classes out; every test
that remains synthesizes its input:

```bash
dotnet test BackgammonDiagram_Lib.slnx --filter "Category!=Visual"
```

## Depends on

**Core: `BgDataTypes_Lib`, its only dependency.** Every input comes from it:
the decision records (`BgDecisionData`, either kind) and the boards
(`BoardPosition`) a diagram draws, and the rule behind each thing shown beside
them, namely the ranking of a checker play, the cube answers and their costs,
the display of every equity and loss, play notation and the pip count. This
library reads those through the producer's members and restates none; each
rule is that library's `INSTRUCTIONS.md`'s.

**ExportRaster**, beyond core:

- **Svg.Skia** — SVG parse and draw for the PNG pipeline. Brings `SkiaSharp`
  transitively, which the rasterizer consumes directly.
- **QuestPDF** — PDF layout and output, licensed by tier (Community MIT,
  Professional, Enterprise). The caller chooses and sets the license; see
  Pitfalls.
- **DocumentFormat.OpenXml** — PPTX generation.

The three are held at their current versions: `Directory.Packages.props` says
why, and the umbrella's `INSTRUCTIONS.md` records the hold.

**Test-only:**

- **BgDataTypes_Lib.TestSupport** — `TestRecords`, the producer's record
  builders: the one way the tests build decision records, so no test here
  restates a record's construction.
- **ConvertXgToJson_Lib** — reads real `.xg` and `.xgp` files for the two
  `RealFile…` classes. The library itself does not depend on it.

## Layout

Repo-root `Directory.Build.props` governs the three projects that live here
(TFM, nullable, implicit usings, `TreatWarningsAsErrors`, XML doc generation)
and `Directory.Packages.props` their packages (Central Package Management: no
inline `Version=`).

**`BackgammonDiagram_Lib/`** — the core. It declares itself trim-compatible and
runs the trim analyzer in its own build; its csproj states why, and
`CoreTrimPostureTests` pins it. Four areas:

- **Rendering** — `Rendering/`: `DiagramRenderer`, the SVG entry points
  (`RenderSvg`, `GetHitRegions`) and every drawing rule behind them; the
  internal `DiagramPresentation`, what a request presents beyond its checkers,
  worded once for both of its sources; and `BoardLayout`, the internal
  geometry.
- **The request and options model** — `Models/`, whose types sit in the root
  `BackgammonDiagram_Lib` namespace rather than a `.Models` one:
  `DiagramRequest` and `DiagramRequestExtensions` (`ToProblemSolutionPair`);
  `RailNames`; `DisplayFacts` with the values it states, `DiceFaces` and the
  closed pair `RailScore` (`MatchRailScore`, `MoneyRailScore`);
  `DiagramOptions` with the `AspectPreset` enum beside it; `DiagramSize`; the
  remaining display enums in `Enums.cs`; and `BoardHitRegions` with the
  `SvgViewBox` and `HitRect` records it is expressed in.
- **Themes** — `Themes/`: `ITheme`, the palette contract; `ThemeRegistry`,
  which exposes the two internal built-ins (`DefaultTheme`, `GreyscaleTheme`);
  and `CustomTheme`, the public caller-supplied palette.
- **Single sources at the root** — `CubeLabels` with its `CubeAnswerSpelling`,
  every word of every cube answer and action; `SvgFormat`, invariant number
  formatting for SVG attributes; and `Watermarks`, the loader for
  `Assets/board-watermark.png`.

**`BackgammonDiagram_Lib.ExportRaster/`** — the raster and export sibling, in
one namespace, `Rendering/` included. No WebAssembly client ships it, so it is
not trim-analyzed. `DiagramRasterRenderer` is the public entry point;
`Rendering/` holds the public backend seam `ISvgRasterizer` with its internal
default `SkiaSharpRasterizer`, and the internal packagers `PdfBuilder` and
`PptxBuilder`.

**`BackgammonDiagram_Lib.Tests/`** — xUnit, not packable. One class per
surface or behaviour area. Records come from `TestRecords` and boards are
`BoardPosition` values; `TestFixtures` holds the shared requests and boards,
`PlayPanelReader` reads a rendered play panel back into rows, `RendererSource`
reaches the renderer's source, and `TestPaths` resolves `TestData/`. Guards
enforce invariants this doc states: `CoreNativeFreeTests`,
`WatermarksTests.Default_MatchesPreBakedBytes`, `CollectionRiderTests`, and
three surveys of the renderer's source, which a rendered SVG cannot show:
`RailLabels_BoldIsSpeltOnce_InTheOneRailTextEmitter`,
`CubePanel_StatesNoPassValueAndNoDoublingRuleOfItsOwn` and
`Renderer_StatesNoPrecisionForAnEquityOrALoss`.

## Architecture

### Assembly structure (native-free core invariant)

The dependency arrow points one way: ExportRaster → core. Core renders to SVG
and holds all the data, layout and theme types, and its only dependency is
`BgDataTypes_Lib`, so a WebAssembly client can ship it. ExportRaster turns that
SVG into PNG, PDF and PPTX: `DiagramRasterRenderer` calls
`DiagramRenderer.RenderSvg`, then rasterizes (`SkiaSharpRasterizer`) and
packages (`PdfBuilder`, `PptxBuilder`). Anything that needs to rasterize lives
there. `CoreNativeFreeTests` reflects over the core assembly's references and
fails on any SkiaSharp, Svg.Skia, QuestPDF or OpenXml reference (see Pitfalls).

### DiagramRequest

What a diagram draws, and the diagram's own options for drawing it: a
`sealed record`, validated where each part is set, with no public constructor.
Three entry points (the umbrella's determination, approved by Hal 2026-09-28 on
halheinrich/backgammon#273):

- **`ForDecision(BgDecisionData decision, PlayRanking ranking)`** — a
  decision's diagram, built from the validated record, either kind. The
  request holds the record and **no copy of anything it states or derives**:
  the board, the presentation, the analysis and the XGID are read from the
  record when the diagram is drawn, so a field the records gain reaches the
  diagram with no edit here.
- **`ForBoard(BoardPosition board, DisplayFacts facts)`** — any other board: a
  game's final position (a side borne off, so no decision position), a
  replayed turn with no analysis. Drawn from the board and the display facts
  its caller states (below). Never a `PositionData`, a `Session` or a stand-in
  record: a match won 0-away is no valid `MatchSession`, and it need not be
  one. There is no request from an XGID string: the record derives its XGID
  and parses none, and `ForBoard` already covers any board.
- **`WithWorkingBoard(BoardPosition board, DiceOrder diceOrder)`** — on a
  request presenting a checker play: the working board of its entry
  (BgDiag_Razor's board mid-play), drawn with **its decision's presentation**,
  which this library derives from the record exactly as for the decision's own
  diagram, so an entry component never copies the record's facts into display
  facts. The checkers and pip counts are the working board's; the names,
  source, roll (in `diceOrder`, since entry lets its user swap the dice on
  screen), cube and score are the decision's. It keeps every option but `Mode`,
  which is `Problem`, a working board having no analysis and no XGID (an XGID
  states the decision's position); and `ShownPlayIndex`, which is null, a
  working board showing the play being entered. It applies to a working board's
  request again, for the next click. On a cube decision's request, or a board's,
  it throws `InvalidOperationException`: a run-time refusal, since a request's
  type does not carry its decision's kind (halheinrich/backgammon#306 asks
  whether it could).

`Board`, `Decision`, `Display` and `Xgid` read a request back. `Decision` and
`Xgid` are null for a board, a working board included.

**Display facts** (`DisplayFacts`; Hal, 2026-09-28: "presentation-owned facts
rather than copied domain-decision facts"). What the diagram draws and prints
for a board beyond its checkers, each validated only for what drawing it needs:

| Fact | Type (default) | Validated | Drawn as |
|---|---|---|---|
| `OnRollName`, `OpponentName` | `string?` | text or null; empty or white space refused (null is the one spelling of none) | each rail's player label |
| `Title` | `string?` | the same | the title strip's middle cell, verbatim |
| `Dice` | `DiceFaces?` | each face 1–6, the faces a die is drawn with | the dice, left first, and the strip's `"{left}-{right} to play"` |
| `CubeValue` | `int` (1) | nothing | the cube's face; 1 reads `64` |
| `CubeOwner` | `CubeOwner` (`Centered`) | a defined value, a place to draw it | where the cube sits |
| `Score` | `RailScore?` | nothing | `MatchRailScore(onRollNeeds, opponentNeeds, isCrawford)` or `MoneyRailScore(isJacoby)`, every argument required, worded under "Presentation and rail text"; null → names alone |

**The boundary** (Hal's rulings of 2026-09-28 on halheinrich/backgammon#273). A
board's presentation may carry the domain facts it needs to reproduce that
presentation: the Crawford status for a match score and the Jacoby rule for a
money score, each a required constructor argument, so a caller that leaves one
out fails to build rather than silently drawing none, and `MoneyRailScore(null)`
is how a source states no rule. The diagram consumes them and neither derives
nor validates their domain legality: a stated Crawford status draws whatever
the away scores beside it, as the needs numbers draw `needs 0`. Double match
point is not a supplied flag: it is worded from the match score by the one rule
a decision's session goes through, so the same score never renders differently
by request path. Nothing else joins display facts: no session beyond those
facts, no decision kind (so `"Cube Action?"` stays a decision's diagram's), no
cube legality, analysis, candidate, error or ranking; and the diagram enforces
no rule of the game on what is there, so the cube shows the value it is given.
The diagram keeps owning how it words what it shows: a caller states values,
never drawn text. Not display facts: the board (the request's); the
orientation, panel side and position number (the request's options, the same
for every entry point); and the pip counts, read off the drawn board by
`BgDataTypes_Lib`'s one pip rule through `BoardState`'s public counts, never
supplied and never computed by a rule of this library's.

**Options.** Each is init-only and validated on every set, so a request is
varied with `with`, `request with { Mode = DiagramMode.Solution }`, and a
variation the request cannot draw is refused where it is made, by an
`ArgumentException` (or its null or out-of-range kinds) naming the member:

- `Mode` (default `Problem`) — `Solution` is a decision's diagram's alone.
  `ToProblemSolutionPair` gives both for one decision, every other option
  riding both.
- `HomeBoardOnRight` (default `true`) — mirrors the points left to right.
- `OnRollAtBottom` (default `true`) — which end of the diagram each side's rail
  label, turned cube and tray take, and which half the dice sit in. The points
  and checkers do not move: points 1–12 stay at the bottom either way. As of
  2026-10-31 no in-tree consumer sets it `false` and no test draws with it.
- `AnalysisPanelPosition` (default `Left`) and the derived `PanelOnLeft`, the
  single declaration site `RenderSvg` and `GetHitRegions` both read.
- `PositionNumber` — "Position {N}" in the strip.
- `Ranking` — the ranking a checker play's candidates are drawn by (see
  "Analysis panel"). Stated for every request presenting a decision:
  `ForDecision` takes it, and unsetting it is refused. None is assumed, so an
  app with a play-sorting setting cannot draw a best play it did not score
  with, and an app without one states the default, `Equity`. A cube decision
  states one too, so a caller holding either kind states it the same way. Null
  for a board.
- `MaximumHiddenCandidateAnalysisLevel` — the ceiling (see "Analysis panel"): a
  defined level, never `Unknown`, which means "not recorded" and bounds
  nothing; null hides nothing. Accepted on any request presenting a decision
  (an app applies its setting to every solution it draws), refused on a board.
- `SecondaryPlayIndex` — the † mark: a candidate of a checker play, refused
  elsewhere and out of range; null for none. It rides a working board.
- `ShownPlayIndex` — showing a play (SPEC-quiz-view §1, 2026-10-25;
  halheinrich/backgammon#440): a candidate of a checker-play decision, refused
  out of range and on a cube decision's, a board's and a working board's
  request. In Solution, `Board` is the position the candidate leaves,
  `AfterBoardOf(i).Flipped()` in the decision's frame, so the checkers, pip
  counts and trays follow it, and its panel row is highlighted; the roll,
  names, cube, score and `Xgid` stay the record's (a baked `ShowXgid` label
  would name the decision's position). Inert in Problem, as the † is.
- `RailNames` — the names the rails show in place of the record's
  (halheinrich/backgammon#448): the on-roll side's and the opponent's, both
  required and neither blank. Each rail reads the stated name with the side's
  score, `"On Roll needs 3"`; the cube's face, the title, the score and the
  record are unchanged. Applies to a decision's diagram in either mode and to a
  working board; refused on a board request, whose display facts state its
  names. A caller's wording: this library holds none of its own.

**Equality.** Value equality: the same record instance (records compare by
reference) or equal boards and display facts, with equal options.

`DiagramRequest` is built by clients from a record or a board. It is
intentionally *not* produced by `ConvertXgToJson_Lib`.

**`DiagramOptions`** are the render's, apart from the request, so one request
renders at several sizes or themes:

- `Size` — the raster formats' pixel width. The SVG ignores it: it is emitted
  at `width="100%"` and takes its host's width.
- `Theme` — see "Themes".
- `Aspect` (default `Widescreen16x9`) — see "The canvas".
- `WatermarkImage` — see "Watermark".
- `ShowXgid` (default `false`) — bakes the request's `Xgid` into the SVG and
  PNG as an upper-right label; a board has none. PDF and PPTX force it off and
  overlay the XGID as real selectable text instead.

### The canvas

What a request draws:

- **Checker decision** — the board with the roll's dice; the play panel in
  Solution mode.
- **Cube decision** — the board with no dice and the cube prompt; the cube
  panel in Solution mode.
- **Board** (`ForBoard`, `WithWorkingBoard`) — the board; the panel region
  blank.

`Natural`, `Widescreen16x9` and `Standard4x3` are the panel-bearing presets.
The board's geometry is fixed in all of them, so checkers stay round: `Natural`
takes the layout's intrinsic panel width, and the other two widen the panel to
reach their aspect. Under them Problem and Solution have **identical overall
dimensions**: the panel region is always allocated, so swapping modes never
reflows surrounding content.

`AspectPreset.BoardOnly` drops the panel and the title strip: the canvas is the
board proper alone. It is for Problem only; `RenderSvg` and `GetHitRegions`
throw `ArgumentException` for a Solution request (the combination spans the
request and the options, so neither can refuse it alone).

`BoardLayout` is `internal`, a geometry-derivation detail of the renderer;
core's tests reach it through `InternalsVisibleTo`. The point, checker, bar and
side-rail dimensions derive from `CheckerRadius` (14 px, the one value
`BoardLayout.Default` states); the top and bottom rails and the point-number
strips are fixed heights. The SVG viewBox is derived from layout totals.

### Title strip

The diagram's title is rendered into the SVG itself as a title strip, a single
source of truth: neither `PdfBuilder` nor `PptxBuilder` stamps a title on the
page. Its background is the theme's `PanelBackgroundColor`. Three cells:

- **Col 1** (left edge, left-anchored): the action — `"{left}-{right} to play"`
  wherever dice are drawn (a checker play's roll in its rolled order, reversed
  on a working board whose user swapped them; a board's stated dice),
  `"Cube Action?"` for a cube decision, nothing otherwise.
- **Col 2** (left-anchored at a fixed offset just right of col 1's reserved
  action column): a decision's source file stem, the record's `SourceFile`
  minus its final dot-extension (`mochy-falafel.xg` → `mochy-falafel`,
  `abc.weird.xg` → `abc.weird`), or a board's stated `Title`, verbatim.
- **Col 3** (right edge, right-anchored): `"Position {N}"` when the request's
  `PositionNumber` is set.

The strip shows when any cell has content, so a decision's diagram always has
one and a board stating no title, dice or position number has none. The
canvas's height, and under an aspect preset its panel's width, follow the
strip: no space is reserved for it (halheinrich/backgammon#318 asks whether it
should be).

`BoardOnly` composes no cells at all (ruled 2026-08-13,
halheinrich/backgammon#98): the strip's height is board budget for the quiz
page's maximized answering view, where the drawn dice carry the roll and the
match name returns at review. Consumers that need the strip's texts render a
panel-bearing preset.

### Presentation and rail text

What a diagram draws beyond its checkers and its analysis (the title strip's
texts, the dice, the cube's face and place, the rail labels) is resolved once
per render by the internal `DiagramPresentation`, from one of two sources: a
decision's record, for a decision's diagram and a working board, or a board's
display facts. Both go through the same wording helpers, so a thing is worded
one way whichever source states it. The title's cube prompt stays a decision's:
it words a decision's kind. The pip counts are not presentation: the rails read
them off the drawn board.

Each rail carries a player label and `Pip: N`, both bold on every surface and
emitted by the one helper `DiagramRenderer.AppendRailLabel`. The player label
is `"{name} {score}"`, each part drawn only where stated (a record may state no
name). A decision's name is the record's, or the request's `RailNames` in its
place; a board's is its display facts'.

**One rule for the score, whichever path states it.** A decision's session is
read as the `RailScore` a board would state for it (the session's facts are the
producer's; see `BgDataTypes_Lib`'s "Money and match: the session kinds"), and
`DiagramPresentation.Scored` words either:

- a match reads `needs {n}` on each side, with ` Crawford` on both in the
  Crawford game;
- money reads `(Money Game, Jacoby)` or `(Money Game, No Jacoby)` on both
  rails, or the bare `(Money Game)` where a board's source states no rule;
- no score (a board's): the names alone.

The cube's face comes from the same method: `Dmp` at double match point (a
match with both players 1-away, where the cube is dead), taking precedence over
`Cr` in the Crawford game (played without the cube), and otherwise the cube's
value, `64` for 1. There is no second copy of the rule;
`PresentationPathAgreementTests` holds the two paths to one rendering for the
same score.

### Analysis panel

Drawn in Solution mode, on a decision's diagram. Two shapes, sharing the
producer's displays for every equity and loss figure: `BgDataTypes_Lib`'s
`EquityDisplay`, the one owner of the precision, the sign rule and the zero
rule (SPEC-scoring §3, "When a cost counts as zero"; halheinrich/backgammon#202),
so what a panel shows and what is scored cannot disagree. The cube panel's
percentages, which are not costs, keep their one decimal.

#### Play panel

One row per visible candidate. **The ranking is `BgDataTypes_Lib`'s**
(SPEC-scoring §2a; `CheckerPlayDecisionData.RankedBy`, stated once in that
library's "The ranking"): it decides the order, the rank numbers, the best play
and every error, and the renderer ranks nothing itself. Columns: user's play
marker, rank, move notation, equity, equity loss, depth.

- The move notation is the candidate's play's (`PlayCandidate.Notation`); a
  record stores none.
- The Eq Loss cell is the ranking's error: blank where it counts as zero (the
  best play, any play tying it, and any whose error shows as `0.0000`), so a
  blank cell and a correct play coincide; the error where it does not; and the
  not-scored mark `—` for a candidate the ranking does not score. Such a
  candidate (under depth first, one at another depth from the best that rates
  higher) has no error, so its row never shows one and never reads like the
  best play, whose cell is blank. It keeps its rank number and its place in the
  ranking.
- The Depth column renders the candidate's derived `DepthAbbreviation`; a
  candidate with no depth recorded omits the cell (the column header still
  renders).
- **Column placement and size** (halheinrich/backgammon#252). The numeric block
  hangs off a 15 em move-text reservation rather than the panel's right edge,
  so it stays close to the move notation however wide a preset stretches the
  panel. When long text would run past the panel, the reservation narrows
  first, then the panel's one font size shrinks from 14 toward a minimum of 11,
  and at the minimum the Depth column alone overruns, by the least amount: move
  text and Equity never overlap. The row pitch never changes.
  `LayOutPlayPanelColumns` states the steps. Widths are estimates, since SVG
  built server-side cannot measure text, from one owner, `EstimateTextWidth`:
  Helvetica/Arial advance-width tables for regular and bold, each cell measured
  at the weight it is drawn in, times a 1.10 safety factor. Each table's and
  the factor's own doc comment says what was measured and what is assumed. On
  Natural and 4:3, where not even an empty move text fits at 14, none of this
  applies and the layout is left as it was: that overflow is
  halheinrich/backgammon#253's.
- Bold on the Equity and Eq Loss **values**, the figures a reader scans the
  panel for; their column headers, the rank, move-notation and Depth cells keep
  the normal weight. The marker is bold too. The weight lives in the SVG, so
  PNG, PDF and PPTX inherit it. Bold and the rank-inversion italic are
  independent: an inverted row reads bold-italic.
- Italic flags a rank inversion: a row's Equity, Eq Loss and Depth cells are
  italic when its candidate's depth rank is higher than that of the candidate
  the ranking places immediately before it, a deeper analysis ranked below a
  shallower one. The ranking's first is never italic, and under depth first no
  row is; an unrecorded depth compares with nothing, being unrecorded rather
  than shallow. The check is keyed off the candidate's place in the ranking,
  not its display slot, so a rescued row, or a row after hidden ones, compares
  with its neighbour in the ranking.
- `MaximumHiddenCandidateAnalysisLevel` (halheinrich/backgammon#66) hides a
  candidate iff its numbers came from a direct evaluation
  (`AnalysisMode.Evaluation`) whose `AnalysisLevel` sits at or below the
  ceiling in `AnalysisLevel`'s declared order, the producer's contract, which
  states the order (the ply and XG Roller families interleave, so a ply ceiling
  also hides the Roller levels beneath it, and only naming `XgRollerPlusPlus`
  reaches that level). Inclusive on the hide side, so the ruled consumer
  selection, "show only rollouts" (the user's ruling of 2026-08-29), is the top
  level named, and a consumer passes its dropdown value verbatim. Rollout-family
  rows are never hidden (their level is the rollout's inner level), and neither
  are rows whose level is not recorded: since `Unknown = 0` would otherwise
  rank at or below every ceiling, an explicit guard keeps them. **The ranking's
  best play, both marked rows and the shown play are never hidden, whatever
  their depth**: review must always show what was best, what was played and
  what is shown.
- Every per-row treatment (rank number, the * and † marks, the rank-inversion
  italics) follows the candidate, not the row, so marks land on the same
  candidates whatever order the ranking draws them in.
- When the panel runs out of vertical space, the protected rows (the best play,
  both marks and the shown play) are reserved first and the rest fill the room
  left in the ranking's order, so a marked row beyond the cut is "rescued" with
  its real rank number and one on the last visible line is never displaced
  (halheinrich/backgammon#440). The rows read in the ranking's order. One row
  plan (`PlanPlayPanel`) is read by both the drawing and `GetHitRegions`.
- The shown play's row carries a highlight behind its text, its colour the
  panel background blended a little toward the panel's text colour, so it shows
  on any panel, black included, and a theme states no colour for it.

#### Cube panel

Best/Actual banner, Equity/Loss table (No double / Double / Take / Pass), two
percentage tables (No double and Take played-out stats), and an Analysis Level
footer. Every word of every cube label comes from `CubeLabels`. The banner names
cube answers, the four of SPEC-scoring §3, each labelled at this decision; the
table names actions.

- **Each row's equity and loss are the producer's**, from its one calculation:
  `CubeDecisionData.ActionEquity(action)`, each action's equity in the
  doubler's perspective, doubling's the taker's best response's, and the half's
  error method, the action's error against the best action of its half. The
  renderer states neither the pass's value nor the rule for doubling's equity
  (Hal's ruling of 2026-09-27 on halheinrich/backgammon#273), so each row's
  equity and error agree. **They are the analysis's action facts, not what the
  scoring charges** (see Pitfalls). The loss shows for every row, `0.0000` for
  the correct option.
- **The Best line lists every answer whose cost counts as zero** (SPEC-scoring
  §3, "The tie"): the producer's `CubeDecision.ZeroCostAnswers`, in its order,
  each labelled at this decision, joined by `", "`, a separator BgQuiz spells
  again (halheinrich/backgammon#330). Its size comes from the costs, not from an
  exact equity tie, and off a tie it can still hold two answers (where gammons
  are not possible and No double / Pass is right, No double costs nothing too).
  The line never names the quiz user's answer: the verdict below the board does.
- **The Best line wraps** (Hal, 2026-10-01: "Yes, wrap it"). Its lead, `Best:`,
  is drawn on its own, and its answers from one x past it, so every line of a
  wrapped list starts exactly under the first answer. The answers fill a line
  while it fits the panel's usable width by `EstimateTextWidth`, its trailing
  comma included; a line breaks only after a separator, between two answers,
  never inside a label, and every answer keeps its full label. Everything below
  moves down one line pitch per line added. A single label wider than the panel
  takes a line of its own and overruns it, on the narrow presets: that overflow
  is halheinrich/backgammon#253's. **Measured 2026-10-01 on `Widescreen16x9`**
  (recorded in 86bb111), the preset BgQuiz uses for every review: the answers
  start at x 52.1 and must end by 396.2. The four-answer list (about 353 px
  one-line with Too good, 405 px with No double / Pass) wraps into two lines,
  the first, `No double, Double / Take, Double / Pass,`, ending at 335.4. With
  an Actual line, the footer's baseline then sits at 354 in a panel 446 high. On
  Natural and 4:3 the same list takes four lines, footer at 394.
- **The Actual line reads what was played**: the record's `UserDoublerAction`
  and `UserTakerAction`, never inferred from an error (a zero error does not
  identify the action when the equities tie). Both halves present are the
  played answer `CubeAnswerExtensions.Of(doubler, taker)`, labelled at its
  decision like any answer. The doubler's action alone renders in its action
  label: `No double`, or `Double` where no response is recorded, a double
  answered by resigning among them. With no doubler action recorded the line is
  dropped. A take or pass without a stated double never reaches the renderer:
  the record's `RecordedCubeActions` refuses it (SPEC-scoring §3), so the switch
  has no arm for it.
- The Analysis Level footer renders the cube analysis's derived `Depth` label
  (e.g. `Rollout: 1296 trials. 3-ply`), not its abbreviation: the cube panel has
  one analysis depth and column space to spare, and so no rank to italicize. No
  depth recorded draws no footer.
- There is no "Pass Justifying Dbl" line: it read a stored field XG never
  stored, gone from the record (halheinrich/backgammon#273); deriving the
  figure is halheinrich/backgammon#288's.

### Cube wording: `CubeLabels`

The single source for the user-facing wording of a cube answer and a cube
action. Every surface that names one reads it here, this library's cube panel
and the consuming apps alike (halheinrich/backgammon#185): one case throughout,
sentence case (ruled 2026-09-02). Its class remarks list the labels. One table
in the type holds each answer's full and short label together, so neither can
drift from the other, and a full label naming a doubling action and a response
is the two actions' own labels joined by `" / "`, so each word has one spelling.

The answers are SPEC-scoring §3's four (amended 2026-09-30 and 2026-10-01). The
short labels are SPEC-quiz-view §4's ("The action row under quiz navigation"),
for a row that cannot fit the full ones; when a surface uses them is that
surface's to decide, and the full label stays the answer's accessible name.

**The fourth answer is labelled at its decision.** `NoDoublePass` reads Too good
or No double / Pass by the decision's reading of it, `CubeDecision.ClaimOf`.
Whether gammons are possible, and what the answer means, are BgDataTypes_Lib's;
this type renders the reading and re-checks no rule. So every answer is labelled
together with the decision it answers: no member labels an answer alone, or from
a claim a caller supplies apart from its decision, and no caller can pair an
answer with another decision's reading.
`PublicSurface_LabelsAnAnswerOnlyWithItsDecision_AndNeverAClaim` pins that shape.

**Two members take an answer without its decision, and neither labels one**;
that pin names them as its two exceptions.

- `Spellings(answer)` lists every spelling the answer can take, each a
  `CubeAnswerSpelling`, its full and short labels under one reading together. A
  host sizes its action row from one width budget, which must hold the cube
  pills at their widest before any cube decision is on screen (SPEC-quiz-view
  §4, "One budget from the outset"), and no label could tell it that, since the
  fourth answer's is the decision's reading. The fourth answer's two spellings
  come in the order its bucket name joins them, which says nothing about which
  reading a decision takes. A spelling carries no reading, so no caller can look
  a label up by a claim, and only `CubeLabels` makes one.
- `BreakdownBucketLabel(answer)` names the answer-type breakdown's bucket
  (SPEC-scoring §3, "The tie", its breakdown sub-bullet). A bucket gathers
  problems, each at its own decision, so its name takes none: it joins the full
  labels of its answer's spellings with `" or "`, which makes the fourth
  `Too good or No double / Pass` (Hal, 2026-10-01). BgQuiz_Blazor's breakdown is
  its consumer.

`Label(CubeAction)` keeps the actions' labels for what is an action, not an
answer: the cube panel's equity table, and a played half recorded without the
other.

There is no display fallback. An unlabelled value is a programming error, and
rendering a placeholder would ship it to the reader: every member is exhaustive
over its type and throws `ArgumentOutOfRangeException` outside it, and a decision
reading the fourth answer as a claim it cannot make, a broken producer contract,
is refused with `UnreachableException`.

### Watermark

On by default: `DiagramOptions.WatermarkImage` defaults to `Watermarks.Default`.
Set it to `null` to opt out, or to other immutable bytes to substitute; a
default (uninitialized) array is refused, since it holds no bytes at all.

The image is drawn twice, one copy in each half, bar-adjacent and rotated 90° so
their tops face each other across the bar, between the points and the checkers
so that everything else paints on top. Both copies take one size, though the dice
sit on one side only, chosen to stay inside the middle gap and short of where the
dice land; `AppendWatermark` has the arithmetic and the opacity lives beside it.
The box is square, which suits the built-in asset; a non-square image is not
catered for. The bytes are embedded as a `data:` URI; PNG is detected by its magic
number and anything else is declared JPEG.

`Watermarks.Default` is a **pre-baked transparent PNG** shipped as an
`EmbeddedResource`, the single source of truth, and `Watermarks` is a pure
embedded-resource loader. A SkiaSharp transform produced it once from the
original JPG, and that transform pulled a native package into core, so it was
removed; `Watermarks.Default`'s remarks describe it, and it and the JPG remain in
git history if the silhouette ever needs regenerating.
`WatermarksTests.Default_MatchesPreBakedBytes` pins the exact bytes, so an
accidental re-encode can't silently shift every rendered diagram's watermark.

### Themes

`DiagramOptions.Theme` is a direct `ITheme` reference; there is no string-based
lookup. The built-in palettes are `internal` and reached only through
`ThemeRegistry.Default` and `ThemeRegistry.Greyscale`. `CustomTheme` is the
supported way for a caller to supply its own palette.

A palette covers less than its member names suggest. `DiceColor` fills the cube;
the dice take `CheckerColorOnRoll`. `TextColor` colours only the point numbers:
title, rail and panel text take a contrast colour derived from the background
behind it.

**The spelling that works today is `#RRGGBB`.** `CustomTheme` also accepts `#RGB`
by its regex, but the renderer's colour helpers read six digits, so a short
colour that they shade or read for contrast passes the constructor and throws at
render (halheinrich/backgammon#441).

### Hit regions

`DiagramRenderer.GetHitRegions` returns what a host needs to lay clickable
overlays on the SVG: a `BoardHitRegions`, in the SVG's viewBox coordinates. It
takes the request as well as the options because the request decides the panel
side, the orientation, which trays show and whether dice are drawn. `RenderSvg`
and `GetHitRegions` share one prologue, `PlanCanvas`, so they describe the same
canvas by construction.

- `Points` is keyed by point number, 1–24 in the on-roll player's numbering. Each
  rect covers a full drawn stack, which is taller than the triangle, so the
  topmost checker stays clickable.
- `Cube` is always set and spans the whole left rail, every place the cube can
  sit. The trays lie inside it, so a host hit-testing both decides which wins.
- `OnRollTray` and `OpponentTray` are null where the tray is not drawn. The
  on-roll tray shows from 0 off, empty, so the first checker can be borne off by
  clicking it; the opponent's, which is display-only, from 1 off; neither with
  every checker off.
- `Dice` is one rect over the pair, null where none are drawn (a cube decision, a
  board showing none).
- `Candidates` (halheinrich/backgammon#440) holds one rect per row the play panel
  draws, keyed by the candidate's **stored index** into the decision's `Plays`,
  never its row position, so a click names the stored candidate under either
  ranking, the ceiling and a rescue. Each spans the panel's width and the row's
  height, shifted by the title strip like every region. Empty for a Problem
  request, a cube decision, a board and a board-only canvas.

### The collection rider

No public member hands out a live mutable collection or array, behind a
read-only interface or as a raw array (Hal, 2026-09-26, on
halheinrich/backgammon#273): a shared `byte[]` would let one caller corrupt every
later render. So `Watermarks.Default` and `DiagramOptions.WatermarkImage` are
immutable bytes, `BoardHitRegions.Points` and `Candidates` are immutable copies
taken on init, and `CubeLabels.Spellings` returns an immutable array of immutable
values. The renderers' `byte[]` results (`RenderPng`, `RenderPdf`, `RenderPptx`,
`ISvgRasterizer.Rasterize`) are fresh arrays each call, owned by the caller.
`CollectionRiderTests` pins these sites and sweeps the shipped assemblies' public
properties for an array type; `CubeLabelsTests.Spellings_CannotBeChangedByACaller`
pins the spellings.

### Export: PNG, PDF and PPTX

All of it lives in the `BackgammonDiagram_Lib.ExportRaster` assembly and
namespace, behind `DiagramRasterRenderer`. Core is not involved beyond producing
the SVG.

- **PNG.** `RenderPng` rasterizes `RenderSvg`'s output at the width
  `DiagramOptions.Size` selects: 600, 1000 or 1600 px, or a custom width. Every
  method takes an optional `ISvgRasterizer`, the pluggable backend; null uses a
  shared internal `SkiaSharpRasterizer`, which callers never name. That class
  holds no state, but no test establishes that native Skia is safe under
  concurrent renders: halheinrich/backgammon#426 records one native crash in a
  deck test, cause unknown.
- **PDF.** Each request becomes one page, widescreen landscape 13.33" × 7.5"
  whatever the canvas preset, embedding the PNG through QuestPDF's `FitArea()`.
  The XGID is overlaid as real selectable text; `XgidLabelTests` reads it back
  through the PDF's ToUnicode CMap, and that test's one unreproduced failure is
  halheinrich/backgammon#436.
- **PPTX.** Each request becomes one slide, with the XGID as a real text box.
  The slide's height is fixed at 7.5" and its width follows the first PNG's
  aspect, so it matches the PDF page only at 16:9. `PptxBuilder.Build`
  post-processes the package to correct a list of OpenXml SDK quirks, each named
  at its call; `PptxConformanceTests` guards against regressions.
- **Cancellation.** The multi-request `RenderPdf` and `RenderPptx` take a token,
  and a cancelled call throws `OperationCanceledException`. Their XML docs list
  where it is checked. A call that runs to its end once entered (one page's
  rasterization, the PPTX package save and each fix-up pass's ZIP rewrite) delays
  the stop until it returns, so the time from cancel to return is bounded by the
  longest of these, not by one page. Inside `GeneratePdf()`, each page carries a
  background element that draws nothing and checks the token; QuestPDF wraps that
  throw in `DocumentDrawingException`, and the builder unwraps only this call's
  own cancellation.

## Pitfalls

- **Core must never gain a native package reference.** SkiaSharp, Svg.Skia,
  QuestPDF and DocumentFormat.OpenXml belong in ExportRaster only; adding one to
  core breaks the WebAssembly-clean invariant BgDiag_Razor and BgQuiz depend on.
  `CoreNativeFreeTests` fails once core's compiled code references one, but a
  package added and unused leaves nothing for it to see. New raster or export
  work goes in the ExportRaster sibling behind `DiagramRasterRenderer`, never in
  `DiagramRenderer`.
- **The QuestPDF license is the caller's.** This library never sets it. Set
  `QuestPDF.Settings.License` in app startup before calling `RenderPdf`. The test
  classes that render PDF set the Community license in their static constructors,
  so a new one must too.
- **Don't trust `Svg.Skia`'s `Drawable.Bounds` for the SVG's visible extent.**
  `SkiaSharpRasterizer` has parsed the viewBox and used `ClipRect` instead since it
  was found unreliable (fbfdf4a, 2026-03-19); no test pins the cause, and it has
  not been re-checked since.
- **Vertical text placement in a raster is an open assumption.** This doc has said
  since 2026-03-26 (66e7fb7), with no test behind it, that Svg.Skia ignores
  `dominant-baseline`. The cube's face and a capped stack's count place their text
  by hand (`centreY + fontSize * 0.35`), which holds either way, so do that for new
  text. The title strip, rail labels and point numbers state
  `dominant-baseline="central"` with no offset: if the claim still holds at 3.6.0,
  they sit high in a raster.
- **Keep the generated SVG to inline attributes and primitive shapes.** Svg.Skia is
  taken to have limited support for CSS stylesheets and complex SVG filters
  (recorded here since 2026-03-26, not tested).
- **`sldLayoutId < 2147483648` produces invalid PPTX.** The OOXML spec requires
  values in the reserved range, the OpenXml SDK will not enforce it, and PowerPoint
  answers with its repair prompt.
- **A deck has one slide size.** `RenderPptx` throws `InvalidOperationException`
  when its pages rasterize to different pixel sizes. Render every request in a
  batch with the same options, and mind the strip: on `Natural`, which forces no
  aspect, a board with no strip comes out shorter than a request with one.
- **`HomeBoardOnRight` is geometry, not data.** The board array is never flipped;
  only `ColumnCentreX` mirrors the points, and the rails, bar, cube and dice stay
  where they are. Anything indexing `DiagramRequest.Board` or
  `BoardHitRegions.Points` uses the unflipped convention.
- **Format every SVG number through `SvgFormat.Number`,** in this renderer and in a
  consumer's overlay or annotation alike, and build a viewBox attribute with
  `SvgViewBox.ToAttributeString()`. Interpolation uses the thread's culture, and
  the comma decimals of a locale like `nb-NO` parse as 0 in a browser.
- **The play panel's best play, errors and order are a ranking's.** Draw a checker
  play's candidates only through `RankedBy(request.Ranking)`. Nothing here answers
  "best" without the request's ranking, and the stored order is not the drawn
  order.
- **No equity or loss is formatted here.** Every loss goes through
  `EquityDisplay.FormatLoss`, every equity through `EquityDisplay.FormatEquity`,
  and every "is it zero" through `EquityDisplay.CountsAsZero`. A format string,
  sign rule or threshold beside them would let what is shown and what is scored
  disagree; `Renderer_StatesNoPrecisionForAnEquityOrALoss` fails if a format
  string returns.
- **Which answers are best is the producer's, and the cube table's losses are not
  answer costs.** Each table row is an action's error; an answer's cost is
  `CubeDecision.CostOf`, which adds the response an answer commits to and charges
  SPEC-scoring §3's conventions, so it differs from the rows
  (halheinrich/backgammon#326). Anything asking which answers are correct reads
  `CubeDecision.ZeroCostAnswers`. Never rebuild that set here, from `CostOf` or
  from the table. `CubePanel_StatesNoPassValueAndNoDoublingRuleOfItsOwn` pins that
  no copy of the table's rule returns.
- **A cube answer is labelled at its decision, and two played actions are an answer
  only through the producer.** Label an answer with
  `CubeLabels.Label(answer, decision)` or `ShortLabel`, never by switching on a
  claim, and form an answer from two actions with `CubeAnswerExtensions.Of`, never
  by classifying the pair here.
- **`CubeLabels.Spellings` is never a labelling fallback.** Never label an answer
  with one of its spellings, the first or the only: for the fourth answer that
  guesses the decision's reading, and for the others it routes a label around the
  decision it belongs to. `BreakdownBucketLabel` is no fallback either.
