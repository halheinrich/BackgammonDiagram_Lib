using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the SVG title strip and the top/bottom rail labels of a decision's
/// diagram: the strip composed from the decision (its roll or the cube
/// prompt, its source file, the request's position number), and the rails
/// worded from the record's session — a match's away scores with the
/// Crawford game marked, or a money session with its Jacoby rule. A board's
/// strip and rails, from its display facts, are pinned in
/// <see cref="BoardRequestTests"/>.
/// </summary>
public class RendererTitleAndRailTests
{
    private static string Render(BgDecisionData record) => TestFixtures.Render(TestFixtures.RequestFor(record));

    private static CheckerPlayDecision InFile(string filename) => TestRecords.CheckerPlay(
        id: filename.EndsWith(".xgp", StringComparison.Ordinal)
            ? new XgpDecisionId(filename)
            : new XgDecisionId(filename, Game: 1, MoveNumber: 1, IsCube: false));

    // -----------------------------------------------------------------------
    //  Title strip — composed from the roll / the cube + the source + PositionNumber
    // -----------------------------------------------------------------------

    [Fact]
    public void Title_PlayDecision_ShowsDiceToPlay()
    {
        var svg = Render(TestRecords.CheckerPlay());

        Assert.Contains(">3-1 to play</text>", svg);
        // Translate-group wrapping the board is added under the strip.
        Assert.Contains("<g transform=\"translate(0,", svg);
    }

    [Fact]
    public void Title_PlayDecision_ShowsTheRollInRolledOrder()
    {
        // The record keeps the dice as rolled, the order the diagram draws.
        var svg = Render(TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(dice: [2, 6])));

        Assert.Contains(">2-6 to play</text>", svg);
    }

    [Fact]
    public void Title_CubeDecision_ShowsCubeAction()
    {
        Assert.Contains($">{DiagramPresentation.CubeActionPrompt}</text>", Render(TestRecords.Cube()));
    }

    [Fact]
    public void Title_PositionNumber_RendersAsSeparateCell()
    {
        var svg = TestFixtures.Render(TestFixtures.MinimalRequest() with { PositionNumber = 7 });

        // Action cell in col 1, position cell in col 3 — distinct <text>
        // elements, no em-dash concatenation.
        Assert.Contains(">3-1 to play</text>", svg);
        Assert.Contains(">Position 7</text>", svg);
    }

    [Fact]
    public void Title_PositionNumber_IsRightAnchored()
    {
        // Col 3 text-anchor="end" attribute ordering is the title-strip
        // signature (rails use dominant-baseline="central" text-anchor="end"
        // in the opposite order, so this string is unique to col 3).
        var svg = TestFixtures.Render(TestFixtures.MinimalRequest() with { PositionNumber = 7 });

        Assert.Contains(
            """text-anchor="end" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
        Assert.Contains(">Position 7</text>", svg);
    }

    [Fact]
    public void Title_NoPositionNumber_ActionStandsAlone()
    {
        var svg = TestFixtures.Render(TestFixtures.MinimalRequest());

        Assert.DoesNotContain(">Position ", svg);
        Assert.Contains(">3-1 to play</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_RendersStemInColumn2()
    {
        // The record's source file, the extension stripped — a slide
        // audience doesn't need the file type.
        var svg = Render(InFile("mochy-falafel.xg"));

        Assert.Contains(">mochy-falafel</text>", svg);
        Assert.DoesNotContain(">mochy-falafel.xg</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_IsLeftAnchoredAfterActionColumn()
    {
        // Col 2 (source) is left-anchored at a fixed x just right of the
        // reserved action column (edgeMargin 8 + ActionColumnWidth 110 = 118),
        // not centred — so it can't collide with the upper-right XGID label.
        var svg = Render(InFile("game.xg"));

        Assert.Contains(
            """<text x="118" y="11" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
        Assert.Contains(">game</text>", svg);
        Assert.DoesNotContain(
            """text-anchor="middle" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
    }

    [Fact]
    public void Title_SourceFile_XgpExtensionStripped()
    {
        var svg = Render(InFile("game.xgp"));

        Assert.Contains(">game</text>", svg);
        Assert.DoesNotContain(">game.xgp</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_OnlyLastExtensionStripped()
    {
        // "abc.weird.xg" must become "abc.weird", not "abc".
        var svg = Render(InFile("abc.weird.xg"));

        Assert.Contains(">abc.weird</text>", svg);
        Assert.DoesNotContain(">abc.weird.xg</text>", svg);
    }

    [Fact]
    public void Title_BoardTitle_DrawnVerbatimInColumn2_AndAbsentOtherwise()
    {
        // A board's stated title takes the source cell, drawn as given; with
        // none, the cell is not emitted at all — one <text> fewer.
        var facts = new DisplayFacts { Dice = new DiceFaces(3, 1) };
        var without = TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, facts));
        var with = TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, facts with { Title = "abc.xg" }));

        Assert.Contains(">abc.xg</text>", with);
        Assert.DoesNotContain(">abc.xg</text>", without);
        Assert.Equal(
            TestFixtures.CountOccurrences(without, "<text ") + 1,
            TestFixtures.CountOccurrences(with, "<text "));
    }

    // -----------------------------------------------------------------------
    //  Rail labels — the record's session
    // -----------------------------------------------------------------------

    [Fact]
    public void RailLabel_MatchPlay_ShowsNameAndNeeds()
    {
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 5)));

        Assert.Contains(">Alice needs 3</text>", svg);
        Assert.Contains(">Bob needs 5</text>", svg);
    }

    // A money session states its Jacoby rule; the rule is the session's, so
    // both rails carry the same wording.

    [Fact]
    public void RailLabel_MoneyGame_Jacoby_NamesTheRule()
    {
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard, session: TestRecords.MoneySession(isJacoby: true)));

        Assert.Contains(">Alice (Money Game, Jacoby)</text>", svg);
        Assert.Contains(">Bob (Money Game, Jacoby)</text>", svg);
    }

    [Fact]
    public void RailLabel_MoneyGame_NoJacoby_NamesTheRuleOff()
    {
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard, session: TestRecords.MoneySession(isJacoby: false)));

        Assert.Contains(">Alice (Money Game, No Jacoby)</text>", svg);
        Assert.Contains(">Bob (Money Game, No Jacoby)</text>", svg);
    }

    [Fact]
    public void RailLabel_Crawford_AppendedToMatchLabel()
    {
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 5, isCrawford: true)));

        Assert.Contains(">Alice needs 1 Crawford</text>", svg);
        Assert.Contains(">Bob needs 5 Crawford</text>", svg);
    }

    [Fact]
    public void RailLabel_UnnamedPlayer_ShowsTheScoreAlone()
    {
        // No name recorded (null): the rail shows the score, with no stray
        // leading space.
        var record = TestRecords.CheckerPlay(descriptive: TestRecords.Descriptive(onRollName: null, opponentName: "Bob"));

        Assert.Contains(">needs 7</text>", Render(record));
    }

    // -----------------------------------------------------------------------
    //  Cube face — the record's session
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1, "64")]
    [InlineData(4, "4")]
    public void CubeFace_ShowsTheCubesValue(int cubeSize, string face)
    {
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard, cubeSize: cubeSize, cubeOwner: CubeOwner.OnRoll));

        Assert.Equal(face, TestFixtures.CubeFace(svg));
    }

    [Fact]
    public void CubeFace_Crawford_RendersCr()
    {
        // The Crawford game is played without the cube: the face reads "Cr".
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 5, isCrawford: true)));

        Assert.Equal(DiagramPresentation.CrawfordFace, TestFixtures.CubeFace(svg));
    }

    [Fact]
    public void CubeFace_DoubleMatchPoint_RendersDmp()
    {
        // Both players 1-away (after the Crawford game): the cube is dead, so
        // the face reads "Dmp" whatever its value.
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard, cubeSize: 2,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 1)));

        Assert.Equal(DiagramPresentation.DoubleMatchPointFace, TestFixtures.CubeFace(svg));
    }

    [Fact]
    public void CubeFace_Money_ShowsItsValue()
    {
        // Money has no Crawford game and no match point: the value.
        var svg = Render(TestFixtures.CheckerPlayOn(BoardPosition.Standard, cubeSize: 2, session: TestRecords.MoneySession()));

        Assert.Equal("2", TestFixtures.CubeFace(svg));
    }

    // -----------------------------------------------------------------------
    //  Rail labels — weight, anchors, positions, fit (halheinrich/backgammon#229)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void RailLabels_AllFourAreBold_AtTheirAnchorsAndPositions(PanelPosition side)
    {
        // The standard start after 8/5: pip counts that differ (164 on roll,
        // 167 the opponent), so each rail's label is told apart.
        var afterEightFive = TestFixtures.Board(
            (1, -2), (12, -5), (17, -3), (19, -5), (24, 2), (13, 5), (8, 2), (6, 5), (5, 1));
        var record = TestFixtures.CheckerPlayOn(afterEightFive,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 5));
        var request = TestFixtures.RequestFor(record) with { AnalysisPanelPosition = side };
        // Natural: the intrinsic panel width, so the board's x offset derives
        // from BoardLayout alone.
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = AspectPreset.Natural });

        // Board-local coordinates (the board sits in the title strip's
        // translate group). The rails run between the side rails.
        var layout = BoardLayout.Default;
        double railX = layout.BoardOffsetX(side == PanelPosition.Left) + layout.LeftRailWidth;
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double nameX = railX + DiagramRenderer.RailLabelInset;
        double pipX = railX + railWidth - DiagramRenderer.RailLabelInset;
        double topY = layout.TopRailHeight / 2;
        double bottomY = layout.BottomRailY + layout.BottomRailHeight / 2;
        var pips = new BoardState(request.Board);

        // On roll at bottom: the bottom rail carries the on-roll player.
        Assert.True(request.OnRollAtBottom);
        AssertRailLabel(svg, "Bob needs 5", nameX, topY, anchorEnd: false);
        AssertRailLabel(svg, $"Pip: {pips.OpponentPipCount}", pipX, topY, anchorEnd: true);
        AssertRailLabel(svg, "Alice needs 3", nameX, bottomY, anchorEnd: false);
        AssertRailLabel(svg, $"Pip: {pips.PipCount}", pipX, bottomY, anchorEnd: true);
    }

    [Fact]
    public void RailLabels_BoldIsSpeltOnce_InTheOneRailTextEmitter()
    {
        // A survey of the renderer's source: the rails' only <text> markup is
        // AppendRailLabel's, so the weight (and family and size) is written
        // once. A rail method that emitted its own <text> would be a second
        // copy of the presentation rule.
        string source = RendererSource.Read();

        foreach (string caller in new[] { "AppendTopRail", "AppendBottomRail", "AppendRailLabels" })
        {
            string body = RendererSource.MethodBody(source, caller);
            Assert.DoesNotContain("<text", body);
            Assert.DoesNotContain("font-weight", body);
        }

        string emitter = RendererSource.MethodBody(source, "AppendRailLabel");
        Assert.Equal(1, TestFixtures.CountOccurrences(emitter, "<text"));
        Assert.Equal(1, TestFixtures.CountOccurrences(emitter, """font-weight="bold" """));
    }

    /// <summary>
    /// The longest labels the rails carry: a long real name with a two-digit
    /// away score and the Crawford suffix, and the same name with the longest
    /// money-game text; the pip label at the largest possible count (every
    /// checker on the bar, 25 pips each).
    /// </summary>
    public static TheoryData<AspectPreset, bool> RailFitCases()
    {
        var data = new TheoryData<AspectPreset, bool>();
        foreach (var preset in Enum.GetValues<AspectPreset>())
        {
            data.Add(preset, true);    // match: "... needs 11 Crawford"
            data.Add(preset, false);   // money: "... (Money Game, No Jacoby)"
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(RailFitCases))]
    public void RailLabels_LongestPlayerLabel_DoesNotReachThePipLabel(AspectPreset preset, bool match)
    {
        const string longName = "Mochizuki Masayuki";
        const int maxPip = 375;
        var allOnTheBar = TestFixtures.Board((0, -BoardPosition.CheckersPerSide), (25, BoardPosition.CheckersPerSide));
        Session session = match
            ? TestRecords.MatchSession(length: 11, onRollNeeds: 11, opponentNeeds: 1, isCrawford: true)
            : TestRecords.MoneySession(isJacoby: false);
        var record = TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: allOnTheBar, session: session),
            descriptive: TestRecords.Descriptive(onRollName: longName, opponentName: longName));
        var svg = DiagramRenderer.RenderSvg(TestFixtures.RequestFor(record), new DiagramOptions { Aspect = preset });

        string[] playerLabels = match
            ? [$"{longName} needs 11 Crawford", $"{longName} needs 1 Crawford"]
            : [$"{longName} (Money Game, No Jacoby)", $"{longName} (Money Game, No Jacoby)"];
        string pipLabel = $"Pip: {maxPip}";

        var pips = RailTexts(svg, pipLabel);
        Assert.Equal(2, pips.Count);
        double pipWidth = DiagramRenderer.EstimateTextWidth(
            pipLabel, DiagramRenderer.RailLabelFontSize, DiagramRenderer.TextWeight.Bold);
        double pipLeft = Num(pips[0]["x"]) - pipWidth;

        foreach (string playerLabel in playerLabels.Distinct())
        {
            var names = RailTexts(svg, playerLabel);
            Assert.NotEmpty(names);
            double nameWidth = DiagramRenderer.EstimateTextWidth(
                playerLabel, DiagramRenderer.RailLabelFontSize, DiagramRenderer.TextWeight.Bold);
            foreach (var name in names)
            {
                double nameRight = Num(name["x"]) + nameWidth;
                Assert.True(nameRight < pipLeft,
                    $"{preset}: \"{playerLabel}\" ends at {nameRight:F2}, \"{pipLabel}\" starts at {pipLeft:F2}.");
            }
        }
    }

    // -----------------------------------------------------------------------
    //  Hit-region offset correctness when a title is present
    // -----------------------------------------------------------------------

    [Fact]
    public void GetHitRegions_ViewBoxIncludesTitleStripHeight()
    {
        // A decision's diagram always has its strip (its roll or the cube
        // prompt, and its source file), so hit regions must account for the
        // ~22 px offset.
        var layout = BoardLayout.Default;
        var regions = DiagramRenderer.GetHitRegions(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());

        Assert.True(regions.ViewBox.Height > layout.BoardHeight,
            "ViewBox should include title-strip height above the board.");
        double offset = regions.ViewBox.Height - layout.BoardHeight;
        foreach (var (_, rect) in regions.Points)
            Assert.True(rect.Y >= offset - 1,
                $"Point Y {rect.Y} should be at or below title offset {offset}.");
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Asserts the one rail <c>&lt;text&gt;</c> carrying <paramref name="content"/>:
    /// bold, at today's family and size, vertically centred at
    /// (<paramref name="x"/>, <paramref name="y"/>), right-anchored when
    /// <paramref name="anchorEnd"/> and start-anchored (no attribute) otherwise.
    /// </summary>
    private static void AssertRailLabel(string svg, string content, double x, double y, bool anchorEnd)
    {
        var attrs = Assert.Single(RailTexts(svg, content));
        Assert.Equal("bold", attrs["font-weight"]);
        Assert.Equal("sans-serif", attrs["font-family"]);
        Assert.Equal(SvgFormat.Number(DiagramRenderer.RailLabelFontSize), attrs["font-size"]);
        Assert.Equal("central", attrs["dominant-baseline"]);
        Assert.Equal(SvgFormat.Number(x), attrs["x"]);
        Assert.Equal(SvgFormat.Number(y), attrs["y"]);
        if (anchorEnd)
            Assert.Equal("end", attrs["text-anchor"]);
        else
            Assert.False(attrs.ContainsKey("text-anchor"), $"\"{content}\" should be start-anchored.");
    }

    /// <summary>
    /// The attributes of every <c>&lt;text&gt;</c> element whose content is
    /// exactly <paramref name="content"/>, in document order.
    /// </summary>
    private static List<Dictionary<string, string>> RailTexts(string svg, string content) =>
        Regex.Matches(svg, $"<text ([^>]*)>{Regex.Escape(content)}</text>")
            .Select(m => Regex.Matches(m.Groups[1].Value, "([a-z-]+)=\"([^\"]*)\"")
                .ToDictionary(a => a.Groups[1].Value, a => a.Groups[2].Value))
            .ToList();

    private static double Num(string x) =>
        double.Parse(x, System.Globalization.CultureInfo.InvariantCulture);
}
