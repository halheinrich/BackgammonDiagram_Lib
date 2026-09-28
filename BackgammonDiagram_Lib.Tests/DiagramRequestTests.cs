using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// <see cref="DiagramRequest"/>'s three entry points and its options: a
/// decision's request holds its record and copies nothing out of it; a
/// board's holds a board and its display facts and applies no rule of the
/// game; a working board keeps its decision's presentation; every option is
/// validated where it is set, the ranking stated wherever a decision is
/// presented and never assumed.
/// </summary>
public class DiagramRequestTests
{
    // -----------------------------------------------------------------------
    //  ForDecision — the record, held, never copied
    // -----------------------------------------------------------------------

    public static TheoryData<string> DecisionKinds => new() { "checker play", "cube" };

    private static BgDecisionData RecordOf(string kind) =>
        kind == "cube" ? TestRecords.Cube() : TestRecords.CheckerPlay();

    [Theory]
    [MemberData(nameof(DecisionKinds))]
    public void ForDecision_HoldsTheRecordItself(string kind)
    {
        // The request is built from the validated record and restates none
        // of it: it holds the instance, and everything drawn is read from it.
        var record = RecordOf(kind);
        var request = DiagramRequest.ForDecision(record, PlayRanking.Equity);

        Assert.Same(record, request.Decision);
        Assert.Null(request.Display);
    }

    [Theory]
    [MemberData(nameof(DecisionKinds))]
    public void ForDecision_BoardAndXgid_AreTheRecords(string kind)
    {
        // No copy of anything the record states or derives, the XGID
        // included: each reads through to the record.
        var record = RecordOf(kind);
        var request = DiagramRequest.ForDecision(record, PlayRanking.Equity);

        Assert.Equal(record.Board, request.Board);
        Assert.Equal(record.Xgid, request.Xgid);
    }

    [Fact]
    public void ForDecision_StartsAsAProblem_WithEveryOptionAtItsDefault()
    {
        var request = DiagramRequest.ForDecision(TestRecords.CheckerPlay(), PlayRanking.DepthFirst);

        Assert.Equal(DiagramMode.Problem, request.Mode);
        Assert.True(request.HomeBoardOnRight);
        Assert.True(request.OnRollAtBottom);
        Assert.Equal(PanelPosition.Left, request.AnalysisPanelPosition);
        Assert.Null(request.PositionNumber);
        Assert.Equal(PlayRanking.DepthFirst, request.Ranking);
        Assert.Null(request.MaximumHiddenCandidateAnalysisLevel);
        Assert.Null(request.SecondaryPlayIndex);
    }

    [Fact]
    public void ForDecision_CubeDecision_StatesItsRankingToo()
    {
        // A caller holding either kind states a ranking the same way; a cube
        // decision's is carried, though it draws no candidates.
        var request = DiagramRequest.ForDecision(TestRecords.Cube(), PlayRanking.DepthFirst);

        Assert.Equal(PlayRanking.DepthFirst, request.Ranking);
    }

    [Fact]
    public void ForDecision_RefusesANullRecord() =>
        Assert.Throws<ArgumentNullException>(() => DiagramRequest.ForDecision(null!, PlayRanking.Equity));

    [Fact]
    public void ForDecision_RefusesAnUndefinedRanking() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DiagramRequest.ForDecision(TestRecords.CheckerPlay(), (PlayRanking)99));

    // -----------------------------------------------------------------------
    //  ForBoard — a board and its display facts, no rule of the game
    // -----------------------------------------------------------------------

    [Fact]
    public void ForBoard_HoldsTheBoardAndTheFacts_AndPresentsNoDecision()
    {
        var board = BoardPosition.Standard;
        var facts = new DisplayFacts { OnRollName = "One", OpponentName = "Two" };
        var request = DiagramRequest.ForBoard(board, facts);

        Assert.Equal(board, request.Board);
        Assert.Same(facts, request.Display);
        Assert.Null(request.Decision);
        Assert.Null(request.Xgid);
        Assert.Null(request.Ranking);
        Assert.Equal(DiagramMode.Problem, request.Mode);
    }

    public static TheoryData<string> BoardsNoDecisionIsMadeOn => new() { "empty", "on roll borne off", "opponent borne off" };

    private static BoardPosition NotADecisionPosition(string name) => name switch
    {
        "empty" => BoardPosition.Empty,
        "on roll borne off" => TestFixtures.Board((19, -3), (20, -2)),
        _ => TestFixtures.Board((1, 2), (2, 1)),
    };

    [Theory]
    [MemberData(nameof(BoardsNoDecisionIsMadeOn))]
    public void ForBoard_DrawsABoardNoDecisionIsMadeOn(string name)
    {
        // A game's final position is a board, not a decision: the board path
        // takes it as it is, where no record could stand on it.
        var board = NotADecisionPosition(name);
        Assert.False(PositionData.IsDecisionPosition(board));

        var request = DiagramRequest.ForBoard(board, new DisplayFacts());

        Assert.Equal(board, request.Board);
        Assert.Contains("<svg", TestFixtures.Render(request));
    }

    [Fact]
    public void ForBoard_RefusesNullFacts() =>
        Assert.Throws<ArgumentNullException>(() => DiagramRequest.ForBoard(BoardPosition.Standard, null!));

    [Fact]
    public void ForBoard_HasNoSolution()
    {
        var request = DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts());

        var ex = Assert.Throws<ArgumentException>(() => request with { Mode = DiagramMode.Solution });
        Assert.Equal(nameof(DiagramRequest.Mode), ex.ParamName);
        Assert.Throws<ArgumentException>(() => request.ToProblemSolutionPair());
    }

    [Fact]
    public void ForBoard_HasNoCandidateOptions()
    {
        // A board has no candidates to rank, hide or mark.
        var request = DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts());

        Assert.Throws<ArgumentException>(() => request with { Ranking = PlayRanking.Equity });
        Assert.Throws<ArgumentException>(() => request with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply4 });
        Assert.Throws<ArgumentException>(() => request with { SecondaryPlayIndex = 0 });
    }

    // -----------------------------------------------------------------------
    //  The ranking — stated, never assumed
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(DecisionKinds))]
    public void Ranking_CannotBeUnstatedOnADecision(string kind)
    {
        var request = DiagramRequest.ForDecision(RecordOf(kind), PlayRanking.Equity);

        var ex = Assert.Throws<ArgumentNullException>(() => request with { Ranking = null });
        Assert.Equal(nameof(DiagramRequest.Ranking), ex.ParamName);
    }

    [Fact]
    public void Ranking_IsVariedWithWith()
    {
        var request = TestFixtures.MinimalRequest() with { Ranking = PlayRanking.DepthFirst };

        Assert.Equal(PlayRanking.DepthFirst, request.Ranking);
    }

    [Fact]
    public void Ranking_RefusesAnUndefinedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestFixtures.MinimalRequest() with { Ranking = (PlayRanking)99 });

    // -----------------------------------------------------------------------
    //  The other options
    // -----------------------------------------------------------------------

    [Fact]
    public void Mode_RefusesAnUndefinedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TestFixtures.MinimalRequest() with { Mode = (DiagramMode)99 });

    [Fact]
    public void AnalysisPanelPosition_RefusesAnUndefinedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestFixtures.MinimalRequest() with { AnalysisPanelPosition = (PanelPosition)99 });

    [Fact]
    public void Ceiling_RefusesUnknown()
    {
        // Unknown means "level not recorded", not a depth, so it bounds none;
        // null is the hide-nothing state.
        var ex = Assert.Throws<ArgumentException>(
            () => TestFixtures.MinimalRequest() with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Unknown });
        Assert.Contains("Unknown", ex.Message);
    }

    [Fact]
    public void Ceiling_RefusesAnUndefinedValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestFixtures.MinimalRequest() with { MaximumHiddenCandidateAnalysisLevel = (AnalysisLevel)99 });

    [Fact]
    public void Ceiling_IsAcceptedOnACubeDecision()
    {
        // A user setting a caller applies to every solution it draws, either
        // kind: a cube decision carries it and draws no candidates.
        var request = TestFixtures.RequestFor(TestRecords.Cube()) with
        {
            MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply4,
        };

        Assert.Equal(AnalysisLevel.Ply4, request.MaximumHiddenCandidateAnalysisLevel);
    }

    // -----------------------------------------------------------------------
    //  The secondary play mark — none is null
    // -----------------------------------------------------------------------

    [Fact]
    public void SecondaryPlayIndex_DefaultsToNull() =>
        Assert.Null(TestFixtures.MinimalRequest().SecondaryPlayIndex);

    [Fact]
    public void SecondaryPlayIndex_NamesACandidate()
    {
        var request = TestFixtures.MinimalRequest() with { SecondaryPlayIndex = 2 };

        Assert.Equal(2, request.SecondaryPlayIndex);
    }

    [Theory]
    [InlineData(-1)]    // the old "none"; none is null now
    [InlineData(3)]     // the default record holds three candidates
    public void SecondaryPlayIndex_RefusesAnIndexNamingNoCandidate(int index)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => TestFixtures.MinimalRequest() with { SecondaryPlayIndex = index });
        Assert.Equal(nameof(DiagramRequest.SecondaryPlayIndex), ex.ParamName);
    }

    [Fact]
    public void SecondaryPlayIndex_RefusedOnACubeDecision()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => TestFixtures.RequestFor(TestRecords.Cube()) with { SecondaryPlayIndex = 0 });
        Assert.Equal(nameof(DiagramRequest.SecondaryPlayIndex), ex.ParamName);
    }

    // -----------------------------------------------------------------------
    //  ToProblemSolutionPair
    // -----------------------------------------------------------------------

    [Fact]
    public void ToProblemSolutionPair_SetsTheModes_AndCarriesEveryOption()
    {
        var source = TestFixtures.MinimalRequest() with
        {
            Ranking = PlayRanking.DepthFirst,
            SecondaryPlayIndex = 2,
            MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply2,
            PositionNumber = 7,
            HomeBoardOnRight = false,
            OnRollAtBottom = false,
            AnalysisPanelPosition = PanelPosition.Right,
        };

        var (problem, solution) = source.ToProblemSolutionPair();

        Assert.Equal(DiagramMode.Problem, problem.Mode);
        Assert.Equal(DiagramMode.Solution, solution.Mode);
        // Everything but the mode is the source's, on both sides.
        Assert.Equal(source, problem);
        Assert.Equal(source with { Mode = DiagramMode.Solution }, solution);
        Assert.Same(source.Decision, solution.Decision);
    }

    // -----------------------------------------------------------------------
    //  WithWorkingBoard — the decision's presentation over a working board
    // -----------------------------------------------------------------------

    /// <summary>The opening 3-1's position after its first half, 8/5.</summary>
    private static BoardPosition AfterEightFive() => TestFixtures.Board(
        (1, -2), (12, -5), (17, -3), (19, -5), (24, 2), (13, 5), (8, 2), (6, 5), (5, 1));

    [Fact]
    public void WithWorkingBoard_DrawsTheWorkingBoard_AndPresentsNoDecision()
    {
        var request = TestFixtures.MinimalRequest().WithWorkingBoard(AfterEightFive());

        Assert.Equal(AfterEightFive(), request.Board);
        // A board: no analysis, no stated facts, no XGID (an XGID states a
        // decision's position, not a board mid-entry).
        Assert.Null(request.Decision);
        Assert.Null(request.Display);
        Assert.Null(request.Xgid);
    }

    [Fact]
    public void WithWorkingBoard_KeepsEveryOptionButTheMode()
    {
        var source = TestFixtures.MinimalRequest() with
        {
            Mode = DiagramMode.Solution,
            HomeBoardOnRight = false,
            OnRollAtBottom = false,
            AnalysisPanelPosition = PanelPosition.Right,
            PositionNumber = 4,
            Ranking = PlayRanking.DepthFirst,
            SecondaryPlayIndex = 1,
        };

        var working = source.WithWorkingBoard(AfterEightFive());

        Assert.Equal(DiagramMode.Problem, working.Mode);
        Assert.False(working.HomeBoardOnRight);
        Assert.False(working.OnRollAtBottom);
        Assert.Equal(PanelPosition.Right, working.AnalysisPanelPosition);
        Assert.Equal(4, working.PositionNumber);
        Assert.Equal(PlayRanking.DepthFirst, working.Ranking);
        Assert.Equal(1, working.SecondaryPlayIndex);
    }

    [Fact]
    public void WithWorkingBoard_HasNoSolution()
    {
        var working = TestFixtures.MinimalRequest().WithWorkingBoard(AfterEightFive());

        Assert.Throws<ArgumentException>(() => working with { Mode = DiagramMode.Solution });
    }

    [Fact]
    public void WithWorkingBoard_AppliesAgain_ToAWorkingBoard()
    {
        // An entry component redraws on every click from the request it
        // holds; a working board's request presents the same decision, so it
        // takes the next board as the decision's request does.
        var once = TestFixtures.MinimalRequest().WithWorkingBoard(AfterEightFive());
        var twice = once.WithWorkingBoard(BoardPosition.Standard, DiceOrder.Reversed);

        Assert.Equal(BoardPosition.Standard, twice.Board);
    }

    [Fact]
    public void WithWorkingBoard_RefusedForACubeDecision() =>
        Assert.Throws<InvalidOperationException>(
            () => TestFixtures.RequestFor(TestRecords.Cube()).WithWorkingBoard(BoardPosition.Standard));

    [Fact]
    public void WithWorkingBoard_RefusedForABoard() =>
        Assert.Throws<InvalidOperationException>(
            () => DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()).WithWorkingBoard(BoardPosition.Standard));

    [Fact]
    public void WithWorkingBoard_RefusesAnUndefinedDiceOrder() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestFixtures.MinimalRequest().WithWorkingBoard(BoardPosition.Standard, (DiceOrder)99));

    [Fact]
    public void WithWorkingBoard_TakesABoardWithASideBorneOff()
    {
        // A bear-off can leave the mover with no checker mid-entry: the
        // working board is a board, held to no decision's rule.
        var record = TestFixtures.CheckerPlayOn(TestFixtures.Board((1, 1), (24, -1)));
        var request = TestFixtures.RequestFor(record).WithWorkingBoard(TestFixtures.Board((24, -1)));

        Assert.False(PositionData.IsDecisionPosition(request.Board));
        Assert.Contains("<svg", TestFixtures.Render(request));
    }

    // -----------------------------------------------------------------------
    //  Equality — what is drawn, and how
    // -----------------------------------------------------------------------

    [Fact]
    public void Equality_SameRecordAndOptions_AreEqual()
    {
        var record = TestRecords.CheckerPlay();

        Assert.Equal(DiagramRequest.ForDecision(record, PlayRanking.Equity),
                     DiagramRequest.ForDecision(record, PlayRanking.Equity));
        Assert.NotEqual(DiagramRequest.ForDecision(record, PlayRanking.Equity),
                        DiagramRequest.ForDecision(record, PlayRanking.DepthFirst));
    }

    [Fact]
    public void Equality_BoardRequests_CompareBoardAndFacts()
    {
        var facts = new DisplayFacts { Score = new MatchRailScore(3, 5, isCrawford: false) };

        Assert.Equal(DiagramRequest.ForBoard(BoardPosition.Standard, facts),
                     DiagramRequest.ForBoard(BoardPosition.Standard, facts with { }));
        Assert.NotEqual(DiagramRequest.ForBoard(BoardPosition.Standard, facts),
                        DiagramRequest.ForBoard(BoardPosition.Standard, facts with { Score = new MatchRailScore(3, 4, isCrawford: false) }));
    }
}
