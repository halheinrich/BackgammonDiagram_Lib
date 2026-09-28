using System.Diagnostics;
using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

/// <summary>
/// An immutable, validated rendering request: what a diagram draws — a
/// decision, or any other board — and the diagram's own options for drawing
/// it. There is no public constructor; a request comes from one of three
/// entry points, so one in hand is always one the renderer can draw:
/// <list type="bullet">
/// <item><description><see cref="ForDecision"/> — a decision's diagram, built
/// from the validated record (<see cref="BgDecisionData"/>, either kind). The
/// request holds the record and no copy of anything it states or derives:
/// the board, the presentation (the names, the source, the roll, the cube,
/// the score) and, in <see cref="DiagramMode.Solution"/>, the analysis are
/// read from the record when the diagram is drawn, and so is the
/// XGID.</description></item>
/// <item><description><see cref="ForBoard"/> — any other board: a
/// <see cref="BoardPosition"/> and the <see cref="DisplayFacts"/> the
/// presentation owns. A board that is not a decision position (a game's
/// final position), a board with no analysis (a replayed turn). It is never a
/// record, a <see cref="PositionData"/> or a <see cref="Session"/>, so no
/// rule of the game is applied to it.</description></item>
/// <item><description><see cref="WithWorkingBoard"/> — the working board of
/// a checker play being entered, drawn with its decision's
/// presentation.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// <b>Options.</b> Every option is init-only and validated when it is set, so
/// a request is varied with <c>request with { Mode = DiagramMode.Solution }</c>
/// and a variation the request cannot draw is refused where it is made. The
/// options a decision's candidates are drawn by — <see cref="Ranking"/>,
/// <see cref="MaximumHiddenCandidateAnalysisLevel"/>,
/// <see cref="SecondaryPlayIndex"/> — belong to a request that presents a
/// decision; a board has none.
/// </para>
/// <para>
/// <b>Equality.</b> Value equality, as a record: two requests are equal when
/// they draw the same thing — the same record instance, or equal boards and
/// display facts — with equal options.
/// </para>
/// </remarks>
public sealed record DiagramRequest
{
    // -----------------------------------------------------------------------
    //  What the request draws: exactly one of three shapes
    // -----------------------------------------------------------------------

    /// <summary>What a request draws; the three shapes below are the only ones.</summary>
    private abstract record Content;

    /// <summary>A decision's diagram: everything is read from the record.</summary>
    private sealed record DecisionContent(BgDecisionData Decision) : Content;

    /// <summary>Any other board, with the display facts the caller states.</summary>
    private sealed record BoardContent(BoardPosition Board, DisplayFacts Facts) : Content;

    /// <summary>A checker play's working board, with the decision's presentation.</summary>
    private sealed record WorkingBoardContent(CheckerPlayDecision Decision, BoardPosition Board, DiceOrder DiceOrder) : Content;

    private readonly DiagramMode _mode;
    private readonly PanelPosition _analysisPanelPosition;
    private readonly PlayRanking? _ranking;
    private readonly AnalysisLevel? _maximumHiddenCandidateAnalysisLevel;
    private readonly int? _secondaryPlayIndex;

    private DiagramRequest(Content content) => Drawn = content;

    /// <summary>
    /// What this request draws. Set by the constructor, and changed only by
    /// <see cref="WithWorkingBoard"/>'s <c>with</c> inside this type: no
    /// caller can reach it.
    /// </summary>
    private Content Drawn { get; init; }

    // -----------------------------------------------------------------------
    //  Entry points
    // -----------------------------------------------------------------------

    /// <summary>
    /// A decision's diagram, from its validated record: the record's board
    /// and presentation, and — in <see cref="DiagramMode.Solution"/> — its
    /// analysis, a checker play's candidates ordered, numbered and scored by
    /// <paramref name="ranking"/>. <see cref="Mode"/> starts at
    /// <see cref="DiagramMode.Problem"/>.
    /// </summary>
    /// <param name="decision">The record, either kind. Held, never copied.</param>
    /// <param name="ranking">
    /// The ranking the candidates are drawn by (SPEC-scoring §2a): it decides
    /// which play is best, the order and the rank numbers, and every error is
    /// measured against its best play. None is assumed, so an app with a
    /// play-sorting setting states it here and cannot draw a best play it did
    /// not score with; an app without one states the default,
    /// <see cref="PlayRanking.Equity"/>. A cube decision has no candidates;
    /// its request states a ranking all the same, so that a caller holding
    /// either kind of record states one the same way.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="decision"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ranking"/> is not a defined ranking.</exception>
    public static DiagramRequest ForDecision(BgDecisionData decision, PlayRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return new DiagramRequest(new DecisionContent(decision)) { Ranking = ranking };
    }

    /// <summary>
    /// Any other board: <paramref name="board"/> drawn with the display facts
    /// <paramref name="facts"/> state — a board that is not a decision
    /// position, such as a game's final position, or a board with no
    /// analysis, such as a replayed turn. No rule of the game is applied to
    /// either (see <see cref="DisplayFacts"/>). It has no analysis to show, so
    /// its <see cref="Mode"/> stays <see cref="DiagramMode.Problem"/>.
    /// </summary>
    /// <param name="board">The board, in the frame <see cref="BoardPosition"/> states; any well-formed position, the empty one and a side borne off included.</param>
    /// <param name="facts">What the diagram draws and prints for it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="facts"/> is <see langword="null"/>.</exception>
    public static DiagramRequest ForBoard(BoardPosition board, DisplayFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new DiagramRequest(new BoardContent(board, facts));
    }

    /// <summary>
    /// The working board of the checker play this request presents, while
    /// its play is being entered: <paramref name="board"/> — any well-formed
    /// position, since a bear-off can leave a side with no checker — drawn
    /// with the decision's own presentation, which this library derives from
    /// the record as it does for the decision's diagram, so an entry
    /// component never restates the record's facts. The names, the source,
    /// the roll, the cube and the score are the decision's; the checkers and
    /// the pip counts are the working board's; the dice are drawn in
    /// <paramref name="diceOrder"/>. Every option is kept but
    /// <see cref="Mode"/>, which is <see cref="DiagramMode.Problem"/>: a
    /// working board has no analysis.
    /// </summary>
    /// <param name="board">The working board, in the decision's frame.</param>
    /// <param name="diceOrder">The order the decision's dice are drawn in.</param>
    /// <returns>A request drawing the working board; it has no <see cref="Decision"/> and no XGID.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="diceOrder"/> is not a defined order.</exception>
    /// <exception cref="InvalidOperationException">This request presents no checker play — it is a cube decision's or a board's.</exception>
    public DiagramRequest WithWorkingBoard(BoardPosition board, DiceOrder diceOrder = DiceOrder.AsRolled)
    {
        if (!Enum.IsDefined(diceOrder))
            throw new ArgumentOutOfRangeException(nameof(diceOrder), diceOrder, "Not a defined dice order.");
        if (PresentedDecision is not CheckerPlayDecision decision)
            throw new InvalidOperationException("A working board is a checker play's, during its entry; this request presents none.");
        return this with
        {
            Drawn = new WorkingBoardContent(decision, board, diceOrder),
            Mode = DiagramMode.Problem,
        };
    }

    // -----------------------------------------------------------------------
    //  What is drawn
    // -----------------------------------------------------------------------

    /// <summary>
    /// The board drawn: a decision's own board (<see cref="BgDecisionData.Board"/>,
    /// read from the record), or the board a board request or a working
    /// board states.
    /// </summary>
    public BoardPosition Board => Drawn switch
    {
        DecisionContent content => content.Decision.Board,
        BoardContent content => content.Board,
        WorkingBoardContent content => content.Board,
        _ => throw new UnreachableException(),
    };

    /// <summary>
    /// The decision this request draws — its board, its presentation and, in
    /// <see cref="DiagramMode.Solution"/>, its analysis — or
    /// <see langword="null"/> for a board, a working board included.
    /// </summary>
    public BgDecisionData? Decision => (Drawn as DecisionContent)?.Decision;

    /// <summary>
    /// The display facts a board request states, or <see langword="null"/>
    /// for a decision's diagram and for a working board, whose presentation is
    /// its decision's.
    /// </summary>
    public DisplayFacts? Display => (Drawn as BoardContent)?.Facts;

    /// <summary>
    /// The decision's XGID, derived by the record on each read
    /// (<see cref="BgDecisionData.Xgid"/>), or <see langword="null"/> for a
    /// board, a working board included: an XGID states a decision. Consumed as
    /// an upper-right label — real selectable text in PDF and PPTX, and baked
    /// pixels in the SVG and PNG when <see cref="DiagramOptions.ShowXgid"/> is
    /// set.
    /// </summary>
    public string? Xgid => (Drawn as DecisionContent)?.Decision.Xgid;

    /// <summary>
    /// The decision whose presentation the diagram draws — a decision's
    /// diagram's record or a working board's decision — or
    /// <see langword="null"/> for a board request.
    /// </summary>
    private BgDecisionData? PresentedDecision => Drawn switch
    {
        DecisionContent content => content.Decision,
        WorkingBoardContent content => content.Decision,
        _ => null,
    };

    /// <summary>
    /// The presentation's source, one branch per kind: a decision's, with the
    /// order its dice are drawn in, or a board's stated facts. The renderer's
    /// one way in to what this request presents.
    /// </summary>
    internal TResult Present<TResult>(
        Func<BgDecisionData, DiceOrder, TResult> decision, Func<DisplayFacts, TResult> board) => Drawn switch
    {
        DecisionContent content => decision(content.Decision, DiceOrder.AsRolled),
        WorkingBoardContent content => decision(content.Decision, content.DiceOrder),
        BoardContent content => board(content.Facts),
        _ => throw new UnreachableException(),
    };

    // -----------------------------------------------------------------------
    //  Options
    // -----------------------------------------------------------------------

    /// <summary>
    /// Whether the analysis panel renders blank
    /// (<see cref="DiagramMode.Problem"/>, the default) or filled
    /// (<see cref="DiagramMode.Solution"/>). Under the panel-bearing canvas
    /// presets both modes share identical overall dimensions — the panel
    /// region is allocated either way — so swapping modes never reflows
    /// surrounding content. <see cref="AspectPreset.BoardOnly"/> is the ruled
    /// exception: it drops the panel allocation and the title strip for
    /// Problem renders (and is rejected for Solution renders), so a consumer
    /// opting into it accepts that its canvas differs from the Solution
    /// canvas.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a defined mode.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is <see cref="DiagramMode.Solution"/> and
    /// the request draws no decision: a board, a working board included, has
    /// no analysis to show.
    /// </exception>
    public DiagramMode Mode
    {
        get => _mode;
        init
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(Mode), value, "Not a defined diagram mode.");
            if (value == DiagramMode.Solution && Drawn is not DecisionContent)
                throw new ArgumentException(
                    "Only a decision's diagram has an analysis to show; a board, a working board included, is drawn as a problem.",
                    nameof(Mode));
            _mode = value;
        }
    }

    /// <summary>
    /// When <c>true</c> (the default), the on-roll player's home board is drawn
    /// on the right. Purely geometric: the board is never flipped, only
    /// <c>ColumnCentreX</c> mirrors, so anything indexing <see cref="Board"/>
    /// uses the unflipped convention regardless of this flag.
    /// </summary>
    public bool HomeBoardOnRight { get; init; } = true;

    /// <summary>
    /// When <c>true</c> (the default), the on-roll half of the board is drawn
    /// at the bottom. Controls only the vertical orientation of the two halves.
    /// </summary>
    public bool OnRollAtBottom { get; init; } = true;

    /// <summary>
    /// Which side of the board the analysis panel occupies. Read through the
    /// derived <see cref="PanelOnLeft"/> so both <c>RenderSvg</c> and
    /// <c>GetHitRegions</c> share one coordinate rule.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a defined panel position.</exception>
    public PanelPosition AnalysisPanelPosition
    {
        get => _analysisPanelPosition;
        init => _analysisPanelPosition = Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(AnalysisPanelPosition), value, "Not a defined panel position.");
    }

    /// <summary>
    /// True when the analysis panel sits to the left of the board. Single
    /// declaration site for this derivation — both <c>RenderSvg</c> and
    /// <c>GetHitRegions</c> read it so the two functions share one
    /// coordinate-system rule and cannot drift.
    /// </summary>
    public bool PanelOnLeft => AnalysisPanelPosition == PanelPosition.Left;

    /// <summary>
    /// Optional counter surfaced right-justified in the title strip as
    /// "Position {N}". Callers emitting a deck typically set this to a running
    /// 1-based counter so readers can cross-reference the slide back to a
    /// source list. Null hides the right title cell.
    /// </summary>
    public int? PositionNumber { get; init; }

    /// <summary>
    /// The ranking a checker play's candidates are drawn by: their order, their
    /// rank numbers, the best play and every error are
    /// <see cref="CheckerPlayDecisionData.RankedBy"/>'s for it (SPEC-scoring
    /// §2a). Stated for every request that presents a decision — none is
    /// assumed — and <see langword="null"/> for a board request, which has no
    /// candidates.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a defined ranking.</exception>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/> and the request presents a decision.</exception>
    /// <exception cref="ArgumentException">Thrown on init when the value is stated and the request draws a board.</exception>
    public PlayRanking? Ranking
    {
        get => _ranking;
        init
        {
            if (value is PlayRanking ranking && !Enum.IsDefined(ranking))
                throw new ArgumentOutOfRangeException(nameof(Ranking), ranking, "Not a defined play ranking.");
            if (PresentedDecision is null)
            {
                if (value is not null)
                    throw new ArgumentException("A board has no candidates to rank.", nameof(Ranking));
            }
            else if (value is null)
            {
                throw new ArgumentNullException(nameof(Ranking),
                    "A decision's diagram states the ranking its candidates are drawn by; none is assumed.");
            }
            _ranking = value;
        }
    }

    /// <summary>
    /// Optional display ceiling for the Solution-mode play panel: hides
    /// candidates analysed <em>at or below</em> this level
    /// (halheinrich/backgammon#66). Null (the default) hides nothing. The
    /// ceiling is inclusive on the hide side, so "hide 4-ply and lower" is
    /// <see cref="AnalysisLevel.Ply4"/>, and "show only rollouts" is
    /// <see cref="AnalysisLevel.XgRollerPlusPlus"/>, the top of the level axis
    /// (the user's ruling of 2026-08-29): stated as a hide-ceiling, every real
    /// level is a legal selection, so a consumer passes its dropdown value
    /// straight through.
    /// <para>
    /// A candidate is hidden exactly when its numbers came from a direct
    /// evaluation (<see cref="AnalysisMode.Evaluation"/>) whose level is at or
    /// below the ceiling in <see cref="AnalysisLevel"/>'s declared order —
    /// the producer's contract, which states that order. A rollout-family
    /// candidate is never hidden (its level is the rollout's inner one), nor
    /// is one whose level is not recorded: <see cref="AnalysisLevel.Unknown"/>
    /// sits outside the order, and its zero value would otherwise compare at
    /// or below every ceiling, so the renderer excludes it explicitly.
    /// </para>
    /// <para>
    /// <b>Contract: the ranking's best play, the user's play and an active
    /// secondary play are never hidden, whatever their depth</b> — review must
    /// always show what was best and what was played. At
    /// <see cref="AnalysisLevel.XgRollerPlusPlus"/> the panel drops every
    /// evaluation row and still draws the rollout family and these three.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a defined level.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is <see cref="AnalysisLevel.Unknown"/> —
    /// it means "level not recorded", not a depth, so it bounds none; null is
    /// the hide-nothing state — or when a value is stated and the request
    /// draws a board, which has no candidates.
    /// </exception>
    public AnalysisLevel? MaximumHiddenCandidateAnalysisLevel
    {
        get => _maximumHiddenCandidateAnalysisLevel;
        init
        {
            if (value is AnalysisLevel ceiling)
            {
                if (!Enum.IsDefined(ceiling))
                    throw new ArgumentOutOfRangeException(nameof(MaximumHiddenCandidateAnalysisLevel), ceiling, "Not a defined analysis level.");
                if (ceiling == AnalysisLevel.Unknown)
                    throw new ArgumentException(
                        "Unknown means \"level not recorded\", not a depth, so it bounds none; null hides nothing.",
                        nameof(MaximumHiddenCandidateAnalysisLevel));
                if (PresentedDecision is null)
                    throw new ArgumentException("A board has no candidates to hide.", nameof(MaximumHiddenCandidateAnalysisLevel));
            }
            _maximumHiddenCandidateAnalysisLevel = value;
        }
    }

    /// <summary>
    /// The candidate to mark a second time in the Solution-mode play panel,
    /// with a bold † beside the primary play's bold * (the record's
    /// <see cref="CheckerPlayDecisionData.UserPlayIndex"/>), or
    /// <see langword="null"/> — the default — for none. A quiz's review shows
    /// the play originally made (*) and its user's differing answer (†) side
    /// by side.
    /// <para>
    /// A consumer's overlay, not a record fact: nothing reads it from a
    /// record. It is drawn only when it names a candidate other than the
    /// user's play — a secondary that coincides with the primary collapses to
    /// one *, so a consumer passes both blindly. Both marked rows are always
    /// kept visible: one ranking beyond the panel's window is rescued into
    /// view with its real rank number.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentException">Thrown on init when a value is stated and the request presents no checker play.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value identifies no candidate of the checker play.</exception>
    public int? SecondaryPlayIndex
    {
        get => _secondaryPlayIndex;
        init
        {
            if (value is int index)
            {
                if (PresentedDecision is not CheckerPlayDecision play)
                    throw new ArgumentException(
                        "A secondary play mark names a checker play's candidate; this request presents none.",
                        nameof(SecondaryPlayIndex));
                ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(SecondaryPlayIndex));
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, play.Decision.Plays.Count, nameof(SecondaryPlayIndex));
            }
            _secondaryPlayIndex = value;
        }
    }
}
