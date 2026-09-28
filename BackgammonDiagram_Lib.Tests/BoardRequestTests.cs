using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Boards drawn through the board path: a board that is not a decision
/// position, drawn with the display facts its request states, and a working
/// board during play entry, drawn with its decision's presentation. In both,
/// the checkers and the pip counts are the board's, and the presentation is
/// worded by the diagram.
/// </summary>
public class BoardRequestTests
{
    // -----------------------------------------------------------------------
    //  A game's final position: a side borne off
    // -----------------------------------------------------------------------

    /// <summary>
    /// The position a final bear-off leaves: the player on roll has borne off
    /// every checker; the opponent has thirteen left, two borne off.
    /// </summary>
    private static readonly BoardPosition FinalPosition = TestFixtures.Board(
        (19, -3), (20, -3), (21, -3), (22, -2), (23, -1), (24, -1));

    private static DisplayFacts FinalFacts() => new()
    {
        OnRollName = "Engine One",
        OpponentName = "Engine Two",
        Title = "Game 5, final position",
        CubeValue = 2,
        CubeOwner = CubeOwner.Opponent,
        Score = new MatchRailScore(onRollNeeds: 0, opponentNeeds: 3, isCrawford: false),
    };

    private static string RenderFinal(DiagramOptions? options = null) =>
        DiagramRenderer.RenderSvg(DiagramRequest.ForBoard(FinalPosition, FinalFacts()),
            options ?? new DiagramOptions { Aspect = AspectPreset.Natural });

    [Fact]
    public void FinalPosition_IsNotADecisionPosition()
    {
        // The premise: no record could stand on this board.
        Assert.False(PositionData.IsDecisionPosition(FinalPosition));
    }

    [Fact]
    public void FinalPosition_RailsShowTheStatedNamesAndScore()
    {
        var svg = RenderFinal();

        Assert.Contains(">Engine One needs 0</text>", svg);
        Assert.Contains(">Engine Two needs 3</text>", svg);
        Assert.DoesNotContain("Crawford", svg);
    }

    [Fact]
    public void FinalPosition_PipCountsAreTheBoards()
    {
        // Read off the board by the producer's one pip rule: the side borne
        // off has none left.
        var svg = RenderFinal();
        var pips = new BoardState(FinalPosition);

        Assert.Equal(0, pips.PipCount);
        Assert.Contains($">Pip: {pips.PipCount}</text>", svg);
        Assert.Contains($">Pip: {pips.OpponentPipCount.ToString(CultureInfo.InvariantCulture)}</text>", svg);
    }

    [Fact]
    public void FinalPosition_TitleStripCarriesTheTitle_AndNoPrompt()
    {
        // No dice shown and no decision kind: the action cell is empty. The
        // stated title alone brings the strip.
        var svg = RenderFinal();

        Assert.Contains(">Game 5, final position</text>", svg);
        Assert.DoesNotContain("to play", svg);
        Assert.DoesNotContain(DiagramPresentation.CubeActionPrompt, svg);
        Assert.Contains("<g transform=\"translate(0,", svg);
    }

    [Fact]
    public void FinalPosition_CubeShowsItsValue_WhereItSits()
    {
        var svg = RenderFinal();

        Assert.Equal("2", TestFixtures.CubeFace(svg));
        // Opponent-owned with the player on roll at the bottom: the cube is
        // turned to the top of the rail, its edge margin (8) from the top.
        Assert.Matches("""<rect x="[0-9.]+" y="8" [^>]*rx="3" """, svg);
    }

    [Fact]
    public void FinalPosition_DrawsNoDiceAndNoPanelContent()
    {
        var request = DiagramRequest.ForBoard(FinalPosition, FinalFacts());
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Empty(DieFaces(svg));
        Assert.Null(DiagramRenderer.GetHitRegions(request, TestFixtures.DefaultOptions()).Dice);
        Assert.DoesNotContain($">{DiagramRenderer.PlayPanelEquityHeader}</text>", svg);
    }

    [Fact]
    public void FinalPosition_DrawsOnlyTheCheckersLeft_AndNoTrayForTheSideBorneOff()
    {
        var request = DiagramRequest.ForBoard(FinalPosition, FinalFacts());
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());
        var regions = DiagramRenderer.GetHitRegions(request, TestFixtures.DefaultOptions());

        // Thirteen checkers, each one circle; the pips on dice would be
        // circles too, and there are no dice.
        Assert.Equal(13, TestFixtures.CountOccurrences(svg, "<circle"));
        // Every checker off is outside the tray's band; the opponent's two off
        // are inside it.
        Assert.Null(regions.OnRollTray);
        Assert.NotNull(regions.OpponentTray);
    }

    // -----------------------------------------------------------------------
    //  Board facts: what is stated is drawn, and nothing else
    // -----------------------------------------------------------------------

    [Fact]
    public void Dice_AreDrawnInTheStatedOrder_AndHeadTheTitle()
    {
        var request = DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts { Dice = new DiceFaces(6, 2) });
        var svg = TestFixtures.Render(request);

        Assert.Contains(">6-2 to play</text>", svg);
        Assert.Equal([6, 2], DieFaces(svg));
        Assert.NotNull(DiagramRenderer.GetHitRegions(request, TestFixtures.DefaultOptions()).Dice);
    }

    [Fact]
    public void Money_WithNoJacobyRuleStated_RailsShowTheBareMoneyLabel()
    {
        // A board's source may state no Jacoby rule: the bare label, never a
        // guessed rule.
        var svg = TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts
        {
            OnRollName = "One",
            OpponentName = "Two",
            Score = new MoneyRailScore(isJacoby: null),
        }));

        Assert.Contains(">One (Money Game)</text>", svg);
        Assert.Contains(">Two (Money Game)</text>", svg);
        Assert.DoesNotContain("Jacoby", svg);
    }

    [Fact]
    public void AbsentNamesAndScore_DrawNothingInTheirPlace()
    {
        // A name alone, a score alone, or neither: each part is drawn only
        // where stated, with no stray separator.
        var svg = TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts
        {
            OnRollName = "One",
            Score = null,
        }));

        Assert.Contains(">One</text>", svg);
        var scoreOnly = TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts
        {
            Score = new MatchRailScore(2, 4, isCrawford: false),
        }));
        Assert.Contains(">needs 2</text>", scoreOnly);
        Assert.Contains(">needs 4</text>", scoreOnly);
    }

    [Fact]
    public void BareBoard_HasNoTitleStrip()
    {
        // Nothing to put in any cell: no strip, so the canvas is the board
        // and the panel allocation.
        var svg = TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()));

        Assert.DoesNotContain("transform=\"translate(", svg);
    }

    [Fact]
    public void PositionNumber_IsTheRequestsOption_OnABoardToo()
    {
        var request = DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()) with { PositionNumber = 3 };

        Assert.Contains(">Position 3</text>", TestFixtures.Render(request));
    }

    // -----------------------------------------------------------------------
    //  A working board mid-entry: its decision's presentation
    // -----------------------------------------------------------------------

    /// <summary>The opening 3-1 after its first half, 8/5: a checker on the 5-point.</summary>
    private static readonly BoardPosition AfterEightFive = TestFixtures.Board(
        (1, -2), (12, -5), (17, -3), (19, -5), (24, 2), (13, 5), (8, 2), (6, 5), (5, 1));

    [Fact]
    public void WorkingBoard_KeepsTheDecisionsPresentation()
    {
        // The default record: Alice on roll against Bob at 0-0 in a 7-point
        // match, in match.xg, the cube centred at 1, a 3-1 to play.
        var decisionSvg = TestFixtures.Render(TestFixtures.MinimalRequest());
        var workingSvg = TestFixtures.Render(TestFixtures.MinimalRequest().WithWorkingBoard(AfterEightFive));

        foreach (string text in new[] { ">Alice needs 7</text>", ">Bob needs 7</text>", ">3-1 to play</text>", ">match</text>" })
        {
            Assert.Contains(text, decisionSvg);
            Assert.Contains(text, workingSvg);
        }
        Assert.Equal("64", TestFixtures.CubeFace(workingSvg));
        Assert.Equal([3, 1], DieFaces(workingSvg));
    }

    [Fact]
    public void WorkingBoard_DrawsTheWorkingBoardsCheckersAndPips()
    {
        var working = TestFixtures.MinimalRequest().WithWorkingBoard(AfterEightFive);
        var svg = TestFixtures.Render(working);

        // 8/5 moves three pips: 167 → 164 for the player on roll.
        Assert.Contains(">Pip: 164</text>", svg);
        Assert.Contains(">Pip: 167</text>", svg);
        Assert.NotEqual(TestFixtures.Render(TestFixtures.MinimalRequest()), svg);
        // A checker on the 5-point, which the decision's board does not have.
        var layout = BoardLayout.Default with { PanelWidthOverride = ViewBoxWidth(svg) - BoardLayout.Default.BoardWidth };
        string onFive = $"<circle cx=\"{SvgFormat.Number(layout.ColumnCentreX(5, working.PanelOnLeft, working.HomeBoardOnRight))}\"";
        Assert.Contains(onFive, svg);
        Assert.DoesNotContain(onFive, TestFixtures.Render(TestFixtures.MinimalRequest()));
    }

    [Fact]
    public void WorkingBoard_DiceReversed_DrawsTheSecondDieFirst()
    {
        var svg = TestFixtures.Render(TestFixtures.MinimalRequest().WithWorkingBoard(AfterEightFive, DiceOrder.Reversed));

        Assert.Equal([1, 3], DieFaces(svg));
        Assert.Contains(">1-3 to play</text>", svg);
    }

    [Fact]
    public void WorkingBoard_KeepsACrawfordDecisionsMarks()
    {
        // The decision's own session words its rails and its cube: a working
        // board keeps them from the record, with no display fact restated.
        var session = TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 4, isCrawford: true);
        var record = TestFixtures.CheckerPlayOn(BoardPosition.Standard, session: session);

        var svg = TestFixtures.Render(TestFixtures.RequestFor(record).WithWorkingBoard(BoardPosition.Standard));

        Assert.Contains(">Alice needs 1 Crawford</text>", svg);
        Assert.Contains(">Bob needs 4 Crawford</text>", svg);
        Assert.Equal(DiagramPresentation.CrawfordFace, TestFixtures.CubeFace(svg));
    }

    [Fact]
    public void WorkingBoard_DrawsNoAnalysis_AndNoXgid()
    {
        var solution = TestFixtures.MinimalRequest() with { Mode = DiagramMode.Solution };
        var working = solution.WithWorkingBoard(AfterEightFive);
        var svg = TestFixtures.Render(working, TestFixtures.DefaultOptions() with { ShowXgid = true });

        Assert.DoesNotContain($">{DiagramRenderer.PlayPanelEquityHeader}</text>", svg);
        Assert.DoesNotContain("XGID=", svg);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// The faces of the two dice drawn, left die first: each die is a square
    /// with a 0.75 stroke, and its pips the circles drawn inside it before the
    /// next die.
    /// </summary>
    private static List<int> DieFaces(string svg)
    {
        var faces = new List<int>();
        var dice = Regex.Matches(svg,
            """<rect x="([0-9.]+)" y="[0-9.]+" width="([0-9.]+)" height="[0-9.]+" rx="[0-9.]+" fill="[^"]*" stroke="#888" stroke-width="0.75"/>((?:\s*<circle [^>]*/>)*)""");
        foreach (Match die in dice)
            faces.Add(Regex.Matches(die.Groups[3].Value, "<circle ").Count);
        return faces;
    }

    private static double ViewBoxWidth(string svg) => double.Parse(
        Regex.Match(svg, """viewBox="0 0 ([0-9.]+) """).Groups[1].Value, CultureInfo.InvariantCulture);
}
