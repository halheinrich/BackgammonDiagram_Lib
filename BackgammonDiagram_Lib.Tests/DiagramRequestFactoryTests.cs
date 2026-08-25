using BackgammonDiagram_Lib;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the <see cref="DiagramRequest.FromDecisionData"/> factory and the
/// <see cref="DiagramRequest.Builder.From(DiagramRequest)"/> round-trip —
/// the single-site field mapping that protects against drift when new
/// DecisionData/PositionData/DescriptiveData fields are added.
/// </summary>
public class DiagramRequestFactoryTests
{
    // -----------------------------------------------------------------------
    //  FromDecisionData — data-layer record → DiagramRequest
    // -----------------------------------------------------------------------

    [Fact]
    public void FromDecisionData_PreservesPositionFields()
    {
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data);

        Assert.Equal(data.Position.Mop, req.Position.Mop);
        Assert.Equal(data.Position.OnRollNeeds, req.Position.OnRollNeeds);
        Assert.Equal(data.Position.OpponentNeeds, req.Position.OpponentNeeds);
        Assert.Equal(data.Position.OnRollPipCount, req.Position.OnRollPipCount);
        Assert.Equal(data.Position.OpponentPipCount, req.Position.OpponentPipCount);
        Assert.Equal(data.Position.CubeSize, req.Position.CubeSize);
        Assert.Equal(data.Position.CubeOwner, req.Position.CubeOwner);
        Assert.Equal(data.Position.IsCrawford, req.Position.IsCrawford);
        Assert.Equal(data.Position.IsJacoby, req.Position.IsJacoby);
    }

    [Fact]
    public void FromDecisionData_PreservesDecisionFields()
    {
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data);

        Assert.Equal(data.Decision.IsCube, req.Decision.IsCube);
        Assert.Equal(data.Decision.Dice, req.Decision.Dice);
        Assert.Equal(data.Decision.CubeDepth, req.Decision.CubeDepth);
        Assert.Equal(data.Decision.CubeDepthAbbreviation, req.Decision.CubeDepthAbbreviation);
        Assert.Equal(data.Decision.CubeDepthRank, req.Decision.CubeDepthRank);
        Assert.Equal(data.Decision.BestPlayIndex, req.Decision.BestPlayIndex);
        Assert.Equal(data.Decision.UserPlayIndex, req.Decision.UserPlayIndex);
        Assert.Equal(data.Decision.NoDoubleEquity, req.Decision.NoDoubleEquity);
        Assert.Equal(data.Decision.DoubleTakeEquity, req.Decision.DoubleTakeEquity);
        Assert.Equal(data.Decision.WinPctAfterNoDouble, req.Decision.WinPctAfterNoDouble);
        Assert.Equal(data.Decision.GammonPctAfterNoDouble, req.Decision.GammonPctAfterNoDouble);
        Assert.Equal(data.Decision.BgPctAfterNoDouble, req.Decision.BgPctAfterNoDouble);
        Assert.Equal(data.Decision.LosePctAfterNoDouble, req.Decision.LosePctAfterNoDouble);
        Assert.Equal(data.Decision.LoseGammonPctAfterNoDouble, req.Decision.LoseGammonPctAfterNoDouble);
        Assert.Equal(data.Decision.LoseBgPctAfterNoDouble, req.Decision.LoseBgPctAfterNoDouble);
        Assert.Equal(data.Decision.WinPctAfterDoubleTake, req.Decision.WinPctAfterDoubleTake);
        Assert.Equal(data.Decision.GammonPctAfterDoubleTake, req.Decision.GammonPctAfterDoubleTake);
        Assert.Equal(data.Decision.BgPctAfterDoubleTake, req.Decision.BgPctAfterDoubleTake);
        Assert.Equal(data.Decision.LosePctAfterDoubleTake, req.Decision.LosePctAfterDoubleTake);
        Assert.Equal(data.Decision.LoseGammonPctAfterDoubleTake, req.Decision.LoseGammonPctAfterDoubleTake);
        Assert.Equal(data.Decision.LoseBgPctAfterDoubleTake, req.Decision.LoseBgPctAfterDoubleTake);
        Assert.Equal(data.Decision.ProbOfOpponentErrorJustifyingDouble, req.Decision.ProbOfOpponentErrorJustifyingDouble);
        Assert.Equal(data.Decision.UserDoubleError, req.Decision.UserDoubleError);
        Assert.Equal(data.Decision.UserTakeError, req.Decision.UserTakeError);
        Assert.Equal(data.Decision.UserDoublerAction, req.Decision.UserDoublerAction);
        Assert.Equal(data.Decision.UserTakerAction, req.Decision.UserTakerAction);
        Assert.Equal(data.Decision.UserPlayError, req.Decision.UserPlayError);
        Assert.Equal(data.Decision.CubeAnalysisMode, req.Decision.CubeAnalysisMode);
        Assert.Equal(data.Decision.CubeAnalysisLevel, req.Decision.CubeAnalysisLevel);
        Assert.Equal(data.Decision.CubelessNoDoubleEquity, req.Decision.CubelessNoDoubleEquity);
        Assert.Equal(data.Decision.CubelessDoubleTakeEquity, req.Decision.CubelessDoubleTakeEquity);
    }

    [Fact]
    public void FromDecisionData_PreservesDescriptiveFields()
    {
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data);

        Assert.Equal(data.Descriptive.OnRollName, req.Descriptive.OnRollName);
        Assert.Equal(data.Descriptive.OpponentName, req.Descriptive.OpponentName);
        Assert.Equal(data.Descriptive.Title, req.Descriptive.Title);
        Assert.Equal(data.Descriptive.MatchLength, req.Descriptive.MatchLength);
        Assert.Equal(data.Descriptive.Date, req.Descriptive.Date);
        Assert.Equal(data.Descriptive.Event, req.Descriptive.Event);
        Assert.Equal(data.Descriptive.SourceFile, req.Descriptive.SourceFile);
        Assert.Equal(data.Descriptive.Game, req.Descriptive.Game);
        Assert.Equal(data.Descriptive.MoveNumber, req.Descriptive.MoveNumber);
        Assert.Equal(data.Descriptive.IsStandardStart, req.Descriptive.IsStandardStart);
        Assert.Equal(data.Descriptive.Comment, req.Descriptive.Comment);
        Assert.Equal(data.Descriptive.Flagged, req.Descriptive.Flagged);
    }

    [Fact]
    public void FromDecisionData_PreservesXgid()
    {
        // Xgid is top-level on BgDecisionData (not in the three records), so
        // it rides the dedicated b.Xgid assignment rather than Builder.From's
        // record copy — assert that wiring survives.
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data);

        Assert.Equal(data.Xgid, req.Xgid);
    }

    [Fact]
    public void FromDecisionData_LeavesSecondaryPlayIndexUnset()
    {
        // SecondaryPlayIndex is a render-only overlay the consumer sets
        // directly — it is NOT sourced from the data layer, so the data-record
        // factory must leave it at the default −1. This is the exports-untouched
        // guard: every export path builds through here (or FromDecisionData),
        // so a stray mapping would silently start marking a second play.
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data);

        Assert.Equal(-1, req.SecondaryPlayIndex);
    }

    [Fact]
    public void BuilderFrom_ExistingRequest_PreservesSecondaryPlayIndex()
    {
        // Builder.From(DiagramRequest) reproduces a request faithfully, so the
        // overlay must ride across the round-trip (like Xgid / PositionNumber),
        // even though the three-record From does not carry it.
        var b = TestFixtures.MinimalBuilder();
        b.SecondaryPlayIndex = 3;
        var original = b.Build();

        var rebuilt = DiagramRequest.Builder.From(original).Build();

        Assert.Equal(3, rebuilt.SecondaryPlayIndex);
    }

    [Fact]
    public void ToProblemSolutionPair_PreservesSecondaryPlayIndex()
    {
        // The quiz consumer sets the overlay then expands to a Problem/Solution
        // pair — the overlay must survive on both sides so the solution view
        // can mark the second play.
        var b = TestFixtures.MinimalBuilder();
        b.SecondaryPlayIndex = 2;
        var (problem, solution) = b.Build().ToProblemSolutionPair();

        Assert.Equal(2, problem.SecondaryPlayIndex);
        Assert.Equal(2, solution.SecondaryPlayIndex);
    }

    [Fact]
    public void FromDecisionData_AppliesRendererParameters()
    {
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data,
            mode: DiagramMode.Problem,
            homeBoardOnRight: false,
            onRollAtBottom: false,
            analysisPanelPosition: PanelPosition.Right);

        Assert.Equal(DiagramMode.Problem, req.Mode);
        Assert.False(req.HomeBoardOnRight);
        Assert.False(req.OnRollAtBottom);
        Assert.Equal(PanelPosition.Right, req.AnalysisPanelPosition);
    }

    // -----------------------------------------------------------------------
    //  Builder.From(DiagramRequest) — round-trip preserves every field
    // -----------------------------------------------------------------------

    [Fact]
    public void BuilderFrom_ExistingRequest_RoundTripsAllFields()
    {
        // Start from a FromDecisionData result, then Builder.From(request)
        // and rebuild — the two DiagramRequests should be value-equal across
        // every public field.
        var original = DiagramRequest.FromDecisionData(
            FullyPopulatedData(),
            mode: DiagramMode.Problem,
            homeBoardOnRight: false,
            analysisPanelPosition: PanelPosition.Right);

        var rebuilt = DiagramRequest.Builder.From(original).Build();

        // Position
        Assert.Equal(original.Position.Mop, rebuilt.Position.Mop);
        Assert.Equal(original.Position.OnRollNeeds, rebuilt.Position.OnRollNeeds);
        Assert.Equal(original.Position.CubeSize, rebuilt.Position.CubeSize);
        Assert.Equal(original.Position.CubeOwner, rebuilt.Position.CubeOwner);

        // Decision — spot check the fields most likely to drift
        Assert.Equal(original.Decision.IsCube, rebuilt.Decision.IsCube);
        Assert.Equal(original.Decision.NoDoubleEquity, rebuilt.Decision.NoDoubleEquity);
        Assert.Equal(original.Decision.UserDoubleError, rebuilt.Decision.UserDoubleError);
        Assert.Equal(original.Decision.UserTakeError, rebuilt.Decision.UserTakeError);
        Assert.Equal(original.Decision.UserDoublerAction, rebuilt.Decision.UserDoublerAction);
        Assert.Equal(original.Decision.UserTakerAction, rebuilt.Decision.UserTakerAction);
        Assert.Equal(original.Decision.CubeDepth, rebuilt.Decision.CubeDepth);
        Assert.Equal(original.Decision.CubeDepthAbbreviation, rebuilt.Decision.CubeDepthAbbreviation);
        Assert.Equal(original.Decision.CubeDepthRank, rebuilt.Decision.CubeDepthRank);

        // Descriptive — SourceFile is the newest drift-prone mapping;
        // assert it survives a round-trip so the single field-mapping
        // site (Builder.From) can't silently drop it.
        Assert.Equal(original.Descriptive.SourceFile, rebuilt.Descriptive.SourceFile);

        // Renderer-specific
        Assert.Equal(original.Mode, rebuilt.Mode);
        Assert.Equal(original.HomeBoardOnRight, rebuilt.HomeBoardOnRight);
        Assert.Equal(original.OnRollAtBottom, rebuilt.OnRollAtBottom);
        Assert.Equal(original.AnalysisPanelPosition, rebuilt.AnalysisPanelPosition);
    }

    // -----------------------------------------------------------------------
    //  ToProblemSolutionPair — still correct after refactor to Builder.From
    // -----------------------------------------------------------------------

    [Fact]
    public void ToProblemSolutionPair_PreservesUserCubeFieldsAcrossExpansion()
    {
        // This is the specific drift that bit us in the Best/Actual banner
        // commit: UserDoubleError was dropped by the old open-coded
        // ToProblemSolutionPair. The refactor through Builder.From means
        // the pair expansion now can't skip a field silently. The played
        // actions matter most here — they are what the Actual line reads.
        var data = FullyPopulatedData();
        var source = DiagramRequest.FromDecisionData(data);

        var (problem, solution) = source.ToProblemSolutionPair();

        Assert.Equal(data.Decision.UserDoubleError, problem.Decision.UserDoubleError);
        Assert.Equal(data.Decision.UserTakeError, problem.Decision.UserTakeError);
        Assert.Equal(data.Decision.UserDoublerAction, problem.Decision.UserDoublerAction);
        Assert.Equal(data.Decision.UserTakerAction, problem.Decision.UserTakerAction);
        Assert.Equal(data.Decision.UserDoubleError, solution.Decision.UserDoubleError);
        Assert.Equal(data.Decision.UserTakeError, solution.Decision.UserTakeError);
        Assert.Equal(data.Decision.UserDoublerAction, solution.Decision.UserDoublerAction);
        Assert.Equal(data.Decision.UserTakerAction, solution.Decision.UserTakerAction);

        Assert.Equal(DiagramMode.Problem, problem.Mode);
        Assert.Equal(DiagramMode.Solution, solution.Mode);
    }

    [Fact]
    public void ToProblemSolutionPair_DoesNotManipulateTitle()
    {
        // Title is no longer composed by pair expansion; the renderer builds
        // the title strip from context (dice + PositionNumber) directly.
        // Whatever the source Title was passes through untouched to both sides.
        var b = TestFixtures.MinimalBuilder();
        b.Title = "Arbitrary source title";
        var (problem, solution) = b.Build().ToProblemSolutionPair();

        Assert.Equal("Arbitrary source title", problem.Descriptive.Title);
        Assert.Equal("Arbitrary source title", solution.Descriptive.Title);
        Assert.Equal(DiagramMode.Problem, problem.Mode);
        Assert.Equal(DiagramMode.Solution, solution.Mode);
    }

    [Fact]
    public void ToProblemSolutionPair_PreservesPositionNumber()
    {
        var b = TestFixtures.MinimalBuilder();
        b.PositionNumber = 7;
        var (problem, solution) = b.Build().ToProblemSolutionPair();

        Assert.Equal(7, problem.PositionNumber);
        Assert.Equal(7, solution.PositionNumber);
    }

    [Fact]
    public void ToProblemSolutionPair_PreservesXgid()
    {
        const string xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10";
        var b = TestFixtures.MinimalBuilder();
        b.Xgid = xgid;
        var (problem, solution) = b.Build().ToProblemSolutionPair();

        Assert.Equal(xgid, problem.Xgid);
        Assert.Equal(xgid, solution.Xgid);
    }

    // -----------------------------------------------------------------------
    //  Fixture builder — every mappable field set to a distinct non-default
    //  value so any dropped mapping shows up as a failed equality assertion.
    // -----------------------------------------------------------------------

    private static BgDecisionData FullyPopulatedData() => new()
    {
        Id = new XgpDecisionId("test.xgp"),
        Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10",
        Position = new PositionData
        {
            Mop = MakeDistinctMop(),
            OnRollNeeds = 7,
            OpponentNeeds = 9,
            OnRollPipCount = 131,
            OpponentPipCount = 142,
            CubeSize = 4,
            CubeOwner = CubeOwner.OnRoll,
            IsCrawford = true,
            IsJacoby = true,
        },
        Decision = new DecisionData
        {
            IsCube = true,
            Dice = [0, 0],
            Plays = [],
            CubeDepth = "XG Roller+",
            CubeDepthAbbreviation = "R+",
            CubeDepthRank = 21,
            CubeAnalysisMode = AnalysisMode.Rollout,
            CubeAnalysisLevel = AnalysisLevel.XgRollerPlus,
            BestPlayIndex = 2,
            UserPlayIndex = 1,
            UserPlayError = 0.0123,
            NoDoubleEquity = 0.4321,
            DoubleTakeEquity = 0.6543,
            CubelessNoDoubleEquity = 0.4111,
            CubelessDoubleTakeEquity = 0.6222,
            WinPctAfterNoDouble = 0.71,
            GammonPctAfterNoDouble = 0.12,
            BgPctAfterNoDouble = 0.013,
            LosePctAfterNoDouble = 0.29,
            LoseGammonPctAfterNoDouble = 0.09,
            LoseBgPctAfterNoDouble = 0.003,
            WinPctAfterDoubleTake = 0.72,
            GammonPctAfterDoubleTake = 0.13,
            BgPctAfterDoubleTake = 0.014,
            LosePctAfterDoubleTake = 0.28,
            LoseGammonPctAfterDoubleTake = 0.08,
            LoseBgPctAfterDoubleTake = 0.002,
            ProbOfOpponentErrorJustifyingDouble = 0.05,
            UserDoubleError = 0.006,
            UserTakeError = 0.0,
            UserDoublerAction = CubeAction.Double,
            UserTakerAction = CubeAction.Take,
        },
        Descriptive = new DescriptiveData
        {
            OnRollName = "Alice",
            OpponentName = "Bob",
            Title = "Pivotal decision",
            MatchLength = 11,
            Date = new DateOnly(2026, 4, 16),
            Event = "Club championship",
            SourceFile = "mochy-falafel.xg",
            Game = 3,
            MoveNumber = 17,
            IsStandardStart = true,
            Comment = "Blitz or prime?",
            Flagged = true,
        },
    };

    private static int[] MakeDistinctMop()
    {
        // A valid 26-element board with a handful of non-zero points so any
        // accidental array-truncation / array-sharing shows up immediately.
        var mop = new int[26];
        mop[6] = 5; mop[8] = 3; mop[13] = 5; mop[24] = 2;
        mop[1] = -2; mop[12] = -5; mop[17] = -3; mop[19] = -5;
        return mop;
    }
}
