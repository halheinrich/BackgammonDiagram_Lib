using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// <see cref="DiagramRequest.RailNames"/> (halheinrich/backgammon#448): the
/// names a decision's rails show in place of the record's. Absent, every
/// request draws what it drew before; stated, each rail reads the stated name
/// with the side's score, in either mode and on a working board, and nothing
/// else drawn changes. Refused on a board request, whose display facts state
/// its names; a blank name is refused where the names are made.
/// </summary>
public class RailNamesTests
{
    private static readonly RailNames Anonymous = new("On Roll", "Opponent");

    public static TheoryData<string, DiagramMode> KindsAndModes => new()
    {
        { "checker play", DiagramMode.Problem },
        { "checker play", DiagramMode.Solution },
        { "cube", DiagramMode.Problem },
        { "cube", DiagramMode.Solution },
    };

    /// <summary>A record of <paramref name="kind"/>, on its default board, in <paramref name="session"/> (by default a 7-point match at 0-0).</summary>
    private static BgDecisionData RecordOf(string kind, Session? session = null) => kind == "cube"
        ? TestRecords.Cube(position: TestRecords.Position(mop: TestRecords.Cube().Position.Mop, session: session))
        : TestRecords.CheckerPlay(position: TestRecords.Position(session: session));

    // -----------------------------------------------------------------------
    //  Absent — the record's names, as before
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(KindsAndModes))]
    public void Absent_TheRailsShowTheRecordsNames(string kind, DiagramMode mode)
    {
        var request = TestFixtures.RequestFor(RecordOf(kind), mode);
        var presentation = DiagramPresentation.Of(request);

        Assert.Null(request.RailNames);
        Assert.Equal("Alice needs 7", presentation.OnRollLabel);
        Assert.Equal("Bob needs 7", presentation.OpponentLabel);
    }

    // -----------------------------------------------------------------------
    //  Stated — the stated names, with each side's score
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(KindsAndModes))]
    public void Stated_InAMatch_TheRailsShowTheStatedNamesWithEachSidesScore(string kind, DiagramMode mode)
    {
        var record = RecordOf(kind, TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 5));
        var request = TestFixtures.RequestFor(record, mode) with { RailNames = Anonymous };
        var presentation = DiagramPresentation.Of(request);

        Assert.Equal("On Roll needs 3", presentation.OnRollLabel);
        Assert.Equal("Opponent needs 5", presentation.OpponentLabel);
    }

    [Theory]
    [MemberData(nameof(KindsAndModes))]
    public void Stated_InMoney_TheRailsShowTheStatedNamesWithTheMoneyLabel(string kind, DiagramMode mode)
    {
        var record = RecordOf(kind, TestRecords.MoneySession(isJacoby: true));
        var request = TestFixtures.RequestFor(record, mode) with { RailNames = Anonymous };
        var presentation = DiagramPresentation.Of(request);

        Assert.Equal("On Roll (Money Game, Jacoby)", presentation.OnRollLabel);
        Assert.Equal("Opponent (Money Game, Jacoby)", presentation.OpponentLabel);
    }

    [Theory]
    [MemberData(nameof(KindsAndModes))]
    public void Stated_OnlyTheNamesChange_AndTheRecordIsUntouched(string kind, DiagramMode mode)
    {
        // The cube's face, the title and the score come out as without the
        // option; the record keeps its own names.
        var record = RecordOf(kind, TestRecords.MatchSession(length: 7, onRollNeeds: 4, opponentNeeds: 2));
        var plain = DiagramPresentation.Of(TestFixtures.RequestFor(record, mode));
        var renamed = DiagramPresentation.Of(TestFixtures.RequestFor(record, mode) with { RailNames = Anonymous });

        Assert.Equal(
            plain with { OnRollLabel = "On Roll needs 4", OpponentLabel = "Opponent needs 2" },
            renamed);
        Assert.Equal("Alice", record.Descriptive.OnRollName);
        Assert.Equal("Bob", record.Descriptive.OpponentName);
    }

    [Fact]
    public void Stated_TheRenderedRailsDrawTheStatedNames()
    {
        var svg = TestFixtures.Render(TestFixtures.MinimalRequest() with { RailNames = Anonymous });

        Assert.Contains(">On Roll needs 7</text>", svg);
        Assert.Contains(">Opponent needs 7</text>", svg);
        Assert.DoesNotContain("Alice", svg);
        Assert.DoesNotContain(">Bob ", svg);
    }

    [Fact]
    public void Stated_RidesBothSidesOfAProblemSolutionPair()
    {
        var (problem, solution) = (TestFixtures.MinimalRequest() with { RailNames = Anonymous }).ToProblemSolutionPair();

        Assert.Equal(Anonymous, problem.RailNames);
        Assert.Equal(Anonymous, solution.RailNames);
    }

    [Theory]
    [InlineData(DiagramMode.Problem)]
    [InlineData(DiagramMode.Solution)]
    public void Stated_AWorkingBoardKeepsThem(DiagramMode mode)
    {
        // WithWorkingBoard keeps every option but Mode and ShownPlayIndex.
        var working = (TestFixtures.RequestFor(TestRecords.CheckerPlay(), mode) with { RailNames = Anonymous })
            .WithWorkingBoard(BoardPosition.Standard, DiceOrder.Reversed);
        var presentation = DiagramPresentation.Of(working);

        Assert.Equal(Anonymous, working.RailNames);
        Assert.Equal("On Roll needs 7", presentation.OnRollLabel);
        Assert.Equal("Opponent needs 7", presentation.OpponentLabel);
    }

    [Fact]
    public void Stated_OnAWorkingBoard_IsAccepted()
    {
        var working = TestFixtures.MinimalRequest().WithWorkingBoard(BoardPosition.Standard) with { RailNames = Anonymous };

        Assert.Equal("On Roll needs 7", DiagramPresentation.Of(working).OnRollLabel);
    }

    // -----------------------------------------------------------------------
    //  Refused
    // -----------------------------------------------------------------------

    [Fact]
    public void RefusedOnABoard()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()) with { RailNames = Anonymous });
        Assert.Equal(nameof(DiagramRequest.RailNames), ex.ParamName);
    }

    [Fact]
    public void ABoard_AcceptsNone() =>
        Assert.Null((DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()) with { RailNames = null }).RailNames);

    [Theory]
    [InlineData(null, "Opponent", "onRollName")]
    [InlineData("On Roll", null, "opponentName")]
    public void AMissingName_IsRefused(string? onRollName, string? opponentName, string parameter)
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RailNames(onRollName!, opponentName!));
        Assert.Equal(parameter, ex.ParamName);
    }

    [Theory]
    [InlineData("", "Opponent", "onRollName")]
    [InlineData("   ", "Opponent", "onRollName")]
    [InlineData("On Roll", "", "opponentName")]
    [InlineData("On Roll", "\t", "opponentName")]
    public void ABlankName_IsRefused(string onRollName, string opponentName, string parameter)
    {
        var ex = Assert.Throws<ArgumentException>(() => new RailNames(onRollName, opponentName));
        Assert.Equal(parameter, ex.ParamName);
    }

    // -----------------------------------------------------------------------
    //  Equality
    // -----------------------------------------------------------------------

    [Fact]
    public void Equality_EqualNames_ConstructedIndependently_MakeEqualRequests()
    {
        // The same record instance and options; two RailNames built apart.
        var request = TestFixtures.MinimalRequest();
        var first = request with { RailNames = new RailNames("On Roll", "Opponent") };
        var second = request with { RailNames = new RailNames(new string("On Roll"), new string("Opponent")) };

        Assert.NotSame(first.RailNames, second.RailNames);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentNames_OrStatedAgainstAbsent_MakeUnequalRequests()
    {
        var request = TestFixtures.MinimalRequest();
        var stated = request with { RailNames = Anonymous };

        Assert.NotEqual(stated, request with { RailNames = new RailNames("On Roll", "Them") });
        Assert.NotEqual(stated, request with { RailNames = new RailNames("Me", "Opponent") });
        Assert.NotEqual(stated, request with { RailNames = new RailNames("Opponent", "On Roll") });
        Assert.NotEqual(stated, request);
    }
}
