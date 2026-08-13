using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Renderer-geometry tests for <see cref="AspectPreset.BoardOnly"/> — the
/// Problem-mode board-only canvas (halheinrich/backgammon#41 producer leg).
/// Pins the three contracts the preset introduces: the canvas is the board
/// proper plus its title strip; RenderSvg and GetHitRegions agree on that
/// canvas (every hit region lands on the board); and the crop is crop-only —
/// board geometry is identical to the panel-bearing presets, so checkers stay
/// round.
/// </summary>
public class BoardOnlyCanvasTests
{
    private static readonly DiagramOptions BoardOnlyOptions = new() { Aspect = AspectPreset.BoardOnly };
    private static readonly DiagramOptions NaturalOptions = new() { Aspect = AspectPreset.Natural };

    // -----------------------------------------------------------------------
    //  Canvas shape
    // -----------------------------------------------------------------------

    [Fact]
    public void BoardOnly_ViewBoxIsBoardProperPlusTitleStrip()
    {
        // MinimalRequest is a checker decision, so the title strip is always
        // composed ("3-1 to play") — the canvas is board width exactly, and
        // board height plus the strip.
        var layout = BoardLayout.Default;
        var regions = DiagramRenderer.GetHitRegions(TestFixtures.MinimalRequest(), BoardOnlyOptions);

        Assert.Equal(0, regions.ViewBox.X);
        Assert.Equal(0, regions.ViewBox.Y);
        Assert.Equal(layout.BoardWidth, regions.ViewBox.Width, 2);
        Assert.True(regions.ViewBox.Height > layout.BoardHeight,
            $"ViewBox height {regions.ViewBox.Height} should exceed board height " +
            $"{layout.BoardHeight} (title strip present).");
    }

    [Fact]
    public void BoardOnly_NoPanelRectEmitted()
    {
        // The analysis panel's background is the only element that is both
        // full board height and panel-coloured — the title strip is a short
        // band and the cube/dice are small squares. (A bare colour count can't
        // identify the panel: the default theme shares #FFFFFF between
        // DiceColor and PanelBackgroundColor, so the cube matches too.)
        string panelRectSignature =
            $"height=\"{SvgFormat.Number(BoardLayout.Default.BoardHeight)}\" " +
            $"fill=\"{BoardOnlyOptions.Theme.PanelBackgroundColor}\"";

        Assert.Contains(panelRectSignature, TestFixtures.Render(opts: NaturalOptions));
        Assert.DoesNotContain(panelRectSignature, TestFixtures.Render(opts: BoardOnlyOptions));
    }

    [Fact]
    public void BoardOnly_TitleStripRetainedAndSpansBoardWidth()
    {
        // The strip's cells survive the crop: the action prompt and the
        // right-anchored Position cell (which re-anchors to the narrower
        // canvas), plus the source stem. The strip's background rect spans
        // exactly the board-only canvas width.
        var b = TestFixtures.MinimalBuilder();
        b.PositionNumber = 7;
        b.SourceFile = "match.xg";
        string svg = DiagramRenderer.RenderSvg(b.Build(), BoardOnlyOptions);

        Assert.Contains(">3-1 to play<", svg);
        Assert.Contains(">match<", svg);
        Assert.Contains(">Position 7<", svg);

        double boardWidth = BoardLayout.Default.BoardWidth;
        Assert.Contains(
            $"""<rect x="0" y="0" width="{SvgFormat.Number(boardWidth)}" """,
            svg);
    }

    // -----------------------------------------------------------------------
    //  RenderSvg ↔ GetHitRegions agreement
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(PanelPosition.Left,  true)]
    [InlineData(PanelPosition.Left,  false)]
    [InlineData(PanelPosition.Right, true)]
    [InlineData(PanelPosition.Right, false)]
    public void BoardOnly_HitRegionsAlignWithRenderSvgCoordinateSystem(
        PanelPosition panelPosition, bool homeBoardOnRight)
    {
        // Same contract as HitRegionsTests' coordinate-system regression test,
        // under the board-only canvas: the hit-regions ViewBox matches the
        // rendered SVG's viewBox attribute, and every point centre matches
        // ColumnCentreX on the board-only layout. With no panel allocated the
        // panel side must be a geometric no-op.
        var b = TestFixtures.MinimalBuilder();
        b.AnalysisPanelPosition = panelPosition;
        b.HomeBoardOnRight = homeBoardOnRight;
        var request = b.Build();

        string svg = DiagramRenderer.RenderSvg(request, BoardOnlyOptions);
        var regions = DiagramRenderer.GetHitRegions(request, BoardOnlyOptions);

        var (svgWidth, svgHeight) = ParseSvgViewBox(svg);
        Assert.Equal(svgWidth, regions.ViewBox.Width, 2);
        Assert.Equal(svgHeight, regions.ViewBox.Height, 2);

        var layout = BoardLayout.Default with { BoardOnly = true };
        bool panelOnLeft = request.PanelOnLeft;

        for (int pt = 1; pt <= 24; pt++)
        {
            var rect = regions.Points[pt];
            double actualCx = rect.X + rect.Width / 2;
            double expectedCx = layout.ColumnCentreX(pt, panelOnLeft, homeBoardOnRight);
            Assert.Equal(expectedCx, actualCx, 2);
        }

        Assert.Equal(layout.BarX(panelOnLeft), regions.Bar.X, 2);
        Assert.NotNull(regions.Cube);
        Assert.Equal(layout.LeftRailX(panelOnLeft), regions.Cube!.X, 2);
    }

    [Fact]
    public void BoardOnly_AllHitRegionsLandOnTheBoard()
    {
        // A request that renders every region at once: a checker decision
        // (dice region), the on-roll player with all 15 on the board (empty
        // on-roll tray renders per the [0,14] band), and the opponent one
        // checker off (opponent tray renders per the [1,14] band). Every
        // region must lie inside the board-only viewBox — the whole point of
        // the preset is that clicks land on the board, not on cropped space.
        var b = TestFixtures.MinimalBuilder();
        var mop = TestFixtures.StartingMop();
        mop[19] = -4;   // opponent: 14 on board → 1 borne off
        b.Mop = mop;
        var regions = DiagramRenderer.GetHitRegions(b.Build(), BoardOnlyOptions);

        Assert.NotNull(regions.OnRollTray);
        Assert.NotNull(regions.OpponentTray);
        Assert.NotNull(regions.Dice);

        var all = new List<(string Name, HitRect Rect)>
        {
            ("Bar", regions.Bar),
            ("Cube", regions.Cube!),
            ("OnRollTray", regions.OnRollTray!),
            ("OpponentTray", regions.OpponentTray!),
            ("Dice", regions.Dice!),
        };
        foreach (var (pt, rect) in regions.Points)
            all.Add(($"Point {pt}", rect));

        foreach (var (name, rect) in all)
        {
            Assert.True(
                rect.X >= -0.01 &&
                rect.Y >= -0.01 &&
                rect.X + rect.Width <= regions.ViewBox.Width + 0.01 &&
                rect.Y + rect.Height <= regions.ViewBox.Height + 0.01,
                $"{name} rect [{rect.X}, {rect.Y}, {rect.Width}, {rect.Height}] is not " +
                $"inside the viewBox [0, 0, {regions.ViewBox.Width}, {regions.ViewBox.Height}].");
        }
    }

    // -----------------------------------------------------------------------
    //  Crop-only invariance — board geometry identical to panel-bearing presets
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void BoardOnly_BoardGeometryMatchesNatural_CropOnly(PanelPosition panelPosition)
    {
        // Board-only crops blank allocation and changes nothing else: every
        // region equals its Natural-preset counterpart shifted left by the
        // panel allocation that sat to the board's left (Natural's panel width
        // for a left panel, zero for a right panel). Identical widths and
        // heights everywhere — checkers stay round.
        var b = TestFixtures.MinimalBuilder();
        b.AnalysisPanelPosition = panelPosition;
        var mop = TestFixtures.StartingMop();
        mop[19] = -4;   // render both trays (see BoardOnly_AllHitRegionsLandOnTheBoard)
        b.Mop = mop;
        var request = b.Build();

        var natural = DiagramRenderer.GetHitRegions(request, NaturalOptions);
        var boardOnly = DiagramRenderer.GetHitRegions(request, BoardOnlyOptions);

        double panelWidth = natural.ViewBox.Width - boardOnly.ViewBox.Width;
        Assert.True(panelWidth > 0, "Natural canvas should be wider than board-only.");
        double offset = panelPosition == PanelPosition.Left ? panelWidth : 0;

        Assert.Equal(natural.ViewBox.Height, boardOnly.ViewBox.Height, 2);

        for (int pt = 1; pt <= 24; pt++)
            AssertShiftedEqual($"Point {pt}", natural.Points[pt], boardOnly.Points[pt], offset);

        AssertShiftedEqual("Bar", natural.Bar, boardOnly.Bar, offset);
        AssertShiftedEqual("Cube", natural.Cube!, boardOnly.Cube!, offset);
        AssertShiftedEqual("OnRollTray", natural.OnRollTray!, boardOnly.OnRollTray!, offset);
        AssertShiftedEqual("OpponentTray", natural.OpponentTray!, boardOnly.OpponentTray!, offset);
        AssertShiftedEqual("Dice", natural.Dice!, boardOnly.Dice!, offset);
    }

    private static void AssertShiftedEqual(string name, HitRect panelBearing, HitRect boardOnly, double offset)
    {
        Assert.True(
            Math.Abs(panelBearing.X - offset - boardOnly.X) < 0.01 &&
            Math.Abs(panelBearing.Y - boardOnly.Y) < 0.01 &&
            Math.Abs(panelBearing.Width - boardOnly.Width) < 0.01 &&
            Math.Abs(panelBearing.Height - boardOnly.Height) < 0.01,
            $"{name}: board-only rect [{boardOnly.X}, {boardOnly.Y}, {boardOnly.Width}, " +
            $"{boardOnly.Height}] is not the panel-bearing rect [{panelBearing.X}, " +
            $"{panelBearing.Y}, {panelBearing.Width}, {panelBearing.Height}] shifted by {offset}.");
    }

    // -----------------------------------------------------------------------
    //  Solution-mode guard
    // -----------------------------------------------------------------------

    [Fact]
    public void BoardOnly_SolutionMode_RenderSvgThrows()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        var request = b.Build();

        var ex = Assert.Throws<ArgumentException>(
            () => DiagramRenderer.RenderSvg(request, BoardOnlyOptions));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void BoardOnly_SolutionMode_GetHitRegionsThrows()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        var request = b.Build();

        var ex = Assert.Throws<ArgumentException>(
            () => DiagramRenderer.GetHitRegions(request, BoardOnlyOptions));
        Assert.Equal("options", ex.ParamName);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static (double Width, double Height) ParseSvgViewBox(string svg)
    {
        var match = Regex.Match(svg,
            @"viewBox=""\s*0\s+0\s+([\d.]+)\s+([\d.]+)\s*""",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "RenderSvg output is missing or has malformed viewBox.");
        double w = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        double h = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return (w, h);
    }
}
