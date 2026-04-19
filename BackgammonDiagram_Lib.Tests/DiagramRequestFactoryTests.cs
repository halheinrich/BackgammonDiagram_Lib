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
    }

    [Fact]
    public void FromDecisionData_PreservesDecisionFields()
    {
        var data = FullyPopulatedData();
        var req = DiagramRequest.FromDecisionData(data);

        Assert.Equal(data.Decision.IsCube, req.Decision.IsCube);
        Assert.Equal(data.Decision.Dice, req.Decision.Dice);
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
    public void ToProblemSolutionPair_PreservesUserErrorsAcrossExpansion()
    {
        // This is the specific drift that bit us in the Best/Actual banner
        // commit: UserDoubleError was dropped by the old open-coded
        // ToProblemSolutionPair. The refactor through Builder.From means
        // the pair expansion now can't skip a field silently.
        var data = FullyPopulatedData();
        var source = DiagramRequest.FromDecisionData(data);

        var (problem, solution) = source.ToProblemSolutionPair();

        Assert.Equal(data.Decision.UserDoubleError, problem.Decision.UserDoubleError);
        Assert.Equal(data.Decision.UserTakeError, problem.Decision.UserTakeError);
        Assert.Equal(data.Decision.UserDoubleError, solution.Decision.UserDoubleError);
        Assert.Equal(data.Decision.UserTakeError, solution.Decision.UserTakeError);

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

    // -----------------------------------------------------------------------
    //  Fixture builder — every mappable field set to a distinct non-default
    //  value so any dropped mapping shows up as a failed equality assertion.
    // -----------------------------------------------------------------------

    private static BgDecisionData FullyPopulatedData() => new()
    {
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
        },
        Decision = new DecisionData
        {
            IsCube = true,
            Dice = [0, 0],
            Plays = [],
            AnalysisDepths = [new AnalysisDepthEntry { Label = "XG Roller+" }],
            BestPlayIndex = 2,
            UserPlayIndex = 1,
            NoDoubleEquity = 0.4321,
            DoubleTakeEquity = 0.6543,
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
