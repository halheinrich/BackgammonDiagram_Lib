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
    public void GetHitRegions_TopPointsSpanTopTriangleArea()
    {
        var layout = BoardLayout.Default;
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        for (int pt = 13; pt <= 24; pt++)
        {
            var rect = regions.Points[pt];
            Assert.Equal(layout.TopCheckerBaseY + titleOffset, rect.Y, 2);
            Assert.Equal(layout.PointHeight, rect.Height, 2);
        }
    }

    [Fact]
    public void GetHitRegions_BottomPointsSpanBottomTriangleArea()
    {
        var layout = BoardLayout.Default;
        var regions = DiagramRenderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        for (int pt = 1; pt <= 12; pt++)
        {
            var rect = regions.Points[pt];
            Assert.Equal(layout.BottomCheckerBaseY + titleOffset, rect.Y, 2);
            Assert.Equal(layout.PointHeight, rect.Height, 2);
        }
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
