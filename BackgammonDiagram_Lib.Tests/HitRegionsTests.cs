using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.Tests;

public class HitRegionsTests
{
    private readonly DiagramOptions _defaultOptions = new();
    private static DiagramRequest MinimalRequest()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = true;
        return b.Build();
    }

    [Fact]
    public void GetHitRegions_Returns24Points()
    {
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        Assert.Equal(24, regions.Points.Count);
        for (int pt = 1; pt <= 24; pt++)
            Assert.True(regions.Points.ContainsKey(pt), $"Missing point {pt}");
    }

    [Fact]
    public void GetHitRegions_ViewBoxIncludesPanelAndTitleStrip()
    {
        // MinimalRequest has valid dice, so the title strip is always composed
        // ("X-Y to play") and the ViewBox must include that vertical offset.
        // Per the "identical overall dimensions" invariant, the panel region
        // is always allocated horizontally regardless of Mode.
        var layout = BoardLayout.Default;
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        Assert.Equal(0, regions.ViewBox.X);
        Assert.Equal(0, regions.ViewBox.Y);
        Assert.True(regions.ViewBox.Width > layout.BoardWidth,
            $"ViewBox width {regions.ViewBox.Width} should exceed board width " +
            $"{layout.BoardWidth} (panel space is always allocated).");
        Assert.True(regions.ViewBox.Height > layout.BoardHeight,
            $"ViewBox height {regions.ViewBox.Height} should exceed board height {layout.BoardHeight}.");
    }

    [Fact]
    public void GetHitRegions_PointColumnsAreOneCheckerDiameterWide()
    {
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        double expectedWidth = BoardLayout.Default.ColumnWidth;

        foreach (var (_, rect) in regions.Points)
            Assert.Equal(expectedWidth, rect.Width, 2);
    }

    [Fact]
    public void GetHitRegions_TopPointsSpanCheckerStackExtent()
    {
        // Top points anchor their hit rect at the triangle base (top edge) and
        // extend down toward centre by the full max-stack height, not just the
        // triangle height — so the 6th checker of a tall stack is clickable.
        var layout = BoardLayout.Default;
        double expectedHeight = Math.Max(layout.PointHeight, layout.MaxStackHeight);
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        for (int pt = 13; pt <= 24; pt++)
        {
            var rect = regions.Points[pt];
            Assert.Equal(layout.TopCheckerBaseY + titleOffset, rect.Y, 2);
            Assert.Equal(expectedHeight, rect.Height, 2);
        }
    }

    [Fact]
    public void GetHitRegions_BottomPointsSpanCheckerStackExtent()
    {
        // Bottom points anchor their hit rect at the triangle base (bottom
        // edge) and extend up toward centre by the full max-stack height. The
        // base stays put; only the toward-centre edge moves out to cover the
        // stack.
        var layout = BoardLayout.Default;
        double expectedHeight = Math.Max(layout.PointHeight, layout.MaxStackHeight);
        double baseY = layout.BottomCheckerBaseY + layout.PointHeight;
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        for (int pt = 1; pt <= 12; pt++)
        {
            var rect = regions.Points[pt];
            // Bottom edge unchanged (still at the triangle base)...
            Assert.Equal(baseY + titleOffset, rect.Y + rect.Height, 2);
            // ...and the rect now spans the full stack height.
            Assert.Equal(expectedHeight, rect.Height, 2);
        }
    }

    [Fact]
    public void GetHitRegions_TallStackTopCheckerStaysInsideHitRect()
    {
        // Regression for dogfooding finding #4: a point with a full
        // MaxStackCheckers-high stack draws its topmost checker beyond the
        // triangle, so a triangle-only hit rect left it unclickable (couldn't
        // play e.g. 6/5 off a 6-stack on point 6). Pin render/hit agreement by
        // computing the top checker's centre from the SAME layout quantities
        // the renderer uses (see AppendCheckerStack) and asserting the point's
        // HitRect fully contains that checker — both a bottom point (stacks up)
        // and a top point (stacks down).
        var layout = BoardLayout.Default;
        double r = layout.CheckerRadius;
        int n = BoardLayout.MaxStackCheckers;

        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = true;
        var mop = new int[26];
        mop[6] = n;     // bottom point, on-roll: 6-high stack
        mop[19] = -n;   // top point, opponent: 6-high stack
        b.Mop = mop;
        var request = b.Build();

        var regions = DiagramRenderer.GetHitRegions(request, _defaultOptions);
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        // Bottom point 6: checkers grow upward from the base; topmost is i=n-1.
        double bottomTopCy = layout.BottomCheckerBaseY + layout.PointHeight - r
                             - (n - 1) * r * 2 + titleOffset;
        AssertRectContainsChecker(regions.Points[6], bottomTopCy, r);

        // Top point 19: checkers grow downward from the base.
        double topTopCy = layout.TopCheckerBaseY + r + (n - 1) * r * 2 + titleOffset;
        AssertRectContainsChecker(regions.Points[19], topTopCy, r);
    }

    [Fact]
    public void GetHitRegions_PointsDoNotOverlapBar()
    {
        // The extended (stack-height) point rects must not bleed into the bar's
        // hit region — points live in their own X columns flush against the
        // bar, so X separation (touching at most) is the guarantee.
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        var bar = regions.Bar;
        double barLeft = bar.X;
        double barRight = bar.X + bar.Width;

        for (int pt = 1; pt <= 24; pt++)
        {
            var rect = regions.Points[pt];
            double rectRight = rect.X + rect.Width;
            bool xSeparated = rectRight <= barLeft + 0.01 || rect.X >= barRight - 0.01;
            Assert.True(xSeparated,
                $"Point {pt} rect X [{rect.X}, {rectRight}] overlaps bar X [{barLeft}, {barRight}].");
        }
    }

    private static void AssertRectContainsChecker(HitRect rect, double checkerCy, double r)
    {
        double checkerTop = checkerCy - r;
        double checkerBottom = checkerCy + r;
        double rectTop = rect.Y;
        double rectBottom = rect.Y + rect.Height;
        Assert.True(rectTop <= checkerTop + 0.01 && checkerBottom <= rectBottom + 0.01,
            $"Checker spanning Y [{checkerTop}, {checkerBottom}] is not contained in " +
            $"hit rect Y [{rectTop}, {rectBottom}].");
    }

    [Fact]
    public void GetHitRegions_AdjacentPointsDoNotOverlap()
    {
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        // Adjacent points within the same half (1-6) must not overlap in X.
        for (int pt = 1; pt <= 5; pt++)
        {
            var r1 = regions.Points[pt];
            var r2 = regions.Points[pt + 1];
            double r1Right = r1.X + r1.Width;
            double r2Right = r2.X + r2.Width;
            bool noOverlap = r1.X >= r2Right || r2.X >= r1Right;
            Assert.True(noOverlap, $"Points {pt} and {pt + 1} overlap in X");
        }
    }

    [Fact]
    public void GetHitRegions_OnRollTrayIsNull()
    {
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        Assert.Null(regions.OnRollTray);
    }

    /// <summary>
    /// Regression test for the GetHitRegions ↔ RenderSvg coordinate-space
    /// mismatch: clicks landing on the wrong points (or missing entirely)
    /// regardless of HomeBoardOnRight orientation, root-caused to GetHitRegions
    /// having previously hardcoded panelOnLeft=false and withPanel=false while
    /// RenderSvg derived panelOnLeft from AnalysisPanelPosition (default Left)
    /// and used withPanel=true. The BgDiag_Razor overlay stretches its
    /// hit-regions viewBox to fit the rendered SVG's display area, so any
    /// viewBox-width disagreement scales every hit rectangle into the wrong
    /// column.
    ///
    /// Asserts hit regions live in the same coordinate system as the rendered
    /// SVG across the full {AnalysisPanelPosition} × {HomeBoardOnRight} matrix.
    /// </summary>
    [Theory]
    [InlineData(PanelPosition.Left,  true)]
    [InlineData(PanelPosition.Left,  false)]
    [InlineData(PanelPosition.Right, true)]
    [InlineData(PanelPosition.Right, false)]
    public void GetHitRegions_AlignsWithRenderSvgCoordinateSystem(
        PanelPosition panelPosition, bool homeBoardOnRight)
    {
        var b = TestFixtures.MinimalBuilder();
        b.AnalysisPanelPosition = panelPosition;
        b.HomeBoardOnRight = homeBoardOnRight;
        var request = b.Build();

        string svg = DiagramRenderer.RenderSvg(request, _defaultOptions);
        var regions = DiagramRenderer.GetHitRegions(request, _defaultOptions);

        // 1. Hit-regions ViewBox matches the rendered SVG's viewBox attribute.
        //    This is the single invariant the BgDiag_Razor overlay relies on:
        //    its overlay-SVG viewBox is set from regions.ViewBox and stretched
        //    to fit the rendered SVG's display area, so any disagreement here
        //    propagates to every click.
        var (svgWidth, svgHeight) = ParseSvgViewBox(svg);
        Assert.Equal(svgWidth, regions.ViewBox.Width, 2);
        Assert.Equal(svgHeight, regions.ViewBox.Height, 2);

        // 2. Re-derive the renderer's layout from the parsed SVG width — same
        //    PanelWidthOverride the renderer used for this aspect — and assert
        //    every point's X centre matches ColumnCentreX in that coordinate
        //    system, with panelOnLeft and homeBoardOnRight both flowing from
        //    the request.
        double panelWidth = svgWidth - BoardLayout.Default.BoardWidth;
        var layout = BoardLayout.Default with { PanelWidthOverride = panelWidth };
        bool panelOnLeft = request.PanelOnLeft;

        for (int pt = 1; pt <= 24; pt++)
        {
            var rect = regions.Points[pt];
            double actualCx = rect.X + rect.Width / 2;
            double expectedCx = layout.ColumnCentreX(pt, panelOnLeft, homeBoardOnRight);
            Assert.Equal(expectedCx, actualCx, 2);
        }

        // 3. Bar and cube X coordinates respect the same panel side. (Y/W/H
        //    of these are panel-independent and covered by other tests.)
        Assert.Equal(layout.BarX(panelOnLeft), regions.Bar.X, 2);
        Assert.NotNull(regions.Cube);
        Assert.Equal(layout.LeftRailX(panelOnLeft), regions.Cube!.X, 2);
    }

    [Fact]
    public void GetHitRegions_DiceRegionCoversBothDrawnDice()
    {
        // Producer step 1 of finding #2 (clickable dice). Pin draw/hit
        // agreement the way the finding-#4 checker test does: derive the dice
        // geometry from the SAME shared source AppendDice draws from
        // (DiagramRenderer.DicePairBounds) rather than hand-copying the formula,
        // then assert the Dice hit-region is exactly that bounding box (shifted
        // by the title-strip offset) and that the two actually-rendered dice
        // fall inside it.
        var request = MinimalRequest();

        string svg = DiagramRenderer.RenderSvg(request, _defaultOptions);
        var regions = DiagramRenderer.GetHitRegions(request, _defaultOptions);

        // Re-derive the renderer's layout from the rendered SVG width — same
        // aspect-driven PanelWidthOverride GetHitRegions used (default options
        // force 16:9) — so the shared-source bounds are computed in the same
        // coordinate system. (BoardHeight is panel-independent.)
        var (svgWidth, _) = ParseSvgViewBox(svg);
        double panelWidth = svgWidth - BoardLayout.Default.BoardWidth;
        var layout = BoardLayout.Default with { PanelWidthOverride = panelWidth };
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        Assert.NotNull(regions.Dice);
        var dice = regions.Dice!;

        // The hit-region is exactly the shared dice-pair bounding box, shifted
        // by the title offset every region carries.
        var bounds = DiagramRenderer.DicePairBounds(layout, request, request.PanelOnLeft).Bounds;
        Assert.Equal(bounds.X, dice.X, 2);
        Assert.Equal(bounds.Y + titleOffset, dice.Y, 2);
        Assert.Equal(bounds.Width, dice.Width, 2);
        Assert.Equal(bounds.Height, dice.Height, 2);

        // End-to-end: the two real dice <rect>s in the rendered SVG (board
        // space — drawn inside the title-offset <g>) lie inside the hit-region
        // once the same offset is applied. Closes the loop SVG ↔ shared source
        // ↔ hit-region, so AppendDice and GetHitRegions can't silently drift.
        var drawn = ParseDiceRects(svg);
        Assert.Equal(2, drawn.Count);
        foreach (var d in drawn)
            AssertRectContainsRect(dice, d with { Y = d.Y + titleOffset });
    }

    [Fact]
    public void GetHitRegions_DiceRegionNullForCubeDecision()
    {
        // Cube decisions draw no dice (mirrors the AppendDice call-site gate),
        // so the hit-region must be absent rather than sitting over empty board.
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];

        var regions = DiagramRenderer.GetHitRegions(b.Build(), _defaultOptions);

        Assert.Null(regions.Dice);
    }

    private static void AssertRectContainsRect(HitRect outer, HitRect inner)
    {
        Assert.True(
            outer.X <= inner.X + 0.01 &&
            outer.Y <= inner.Y + 0.01 &&
            inner.X + inner.Width <= outer.X + outer.Width + 0.01 &&
            inner.Y + inner.Height <= outer.Y + outer.Height + 0.01,
            $"Rect [{inner.X}, {inner.Y}, {inner.Width}, {inner.Height}] is not contained in " +
            $"[{outer.X}, {outer.Y}, {outer.Width}, {outer.Height}].");
    }

    /// <summary>
    /// Parses the two die face rects from rendered SVG. Dice are the only
    /// <c>&lt;rect&gt;</c>s drawn with <c>stroke-width="0.75"</c> (the cube and
    /// bear-off bars use 0.5; board/rails are unstroked), so that attribute
    /// uniquely identifies them.
    /// </summary>
    private static List<HitRect> ParseDiceRects(string svg)
    {
        var matches = Regex.Matches(svg,
            @"<rect x=""([\d.]+)"" y=""([\d.]+)"" width=""([\d.]+)"" height=""([\d.]+)"" rx=""[\d.]+"" fill=""[^""]*"" stroke=""#888"" stroke-width=""0\.75""");
        var rects = new List<HitRect>();
        foreach (Match m in matches)
        {
            rects.Add(new HitRect(
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)));
        }
        return rects;
    }

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
