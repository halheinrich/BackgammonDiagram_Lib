using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.Tests;

public class HitRegionsTests
{
    private readonly DiagramRenderer _renderer = new();
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
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        Assert.Equal(24, regions.Points.Count);
        for (int pt = 1; pt <= 24; pt++)
            Assert.True(regions.Points.ContainsKey(pt), $"Missing point {pt}");
    }

    [Fact]
    public void GetHitRegions_ViewBoxMatchesBoardOnly()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        Assert.Equal(0, regions.ViewBox.X);
        Assert.Equal(0, regions.ViewBox.Y);
        Assert.Equal(layout.BoardWidth, regions.ViewBox.Width);
        Assert.Equal(layout.BoardHeight, regions.ViewBox.Height);
    }

    [Fact]
    public void GetHitRegions_PointColumnsAreOneCheckerDiameterWide()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        double expectedWidth = layout.ColumnWidth;

        foreach (var (pt, rect) in regions.Points)
            Assert.Equal(expectedWidth, rect.Width, 2);
    }

    [Fact]
    public void GetHitRegions_TopPointsSpanTopTriangleArea()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        for (int pt = 13; pt <= 24; pt++)
        {
            var rect = regions.Points[pt];
            Assert.Equal(layout.TopCheckerBaseY, rect.Y, 2);
            Assert.Equal(layout.PointHeight, rect.Height, 2);
        }
    }

    [Fact]
    public void GetHitRegions_BottomPointsSpanBottomTriangleArea()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        for (int pt = 1; pt <= 12; pt++)
        {
            var rect = regions.Points[pt];
            Assert.Equal(layout.BottomCheckerBaseY, rect.Y, 2);
            Assert.Equal(layout.PointHeight, rect.Height, 2);
        }
    }

    [Fact]
    public void GetHitRegions_Point1IsRightmostBottomColumn()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        // Point 1 should be the rightmost column in the inner (right) half
        double expectedCx = layout.ColumnCentreX(1, panelOnLeft: false, homeBoardOnRight: true);
        var rect = regions.Points[1];
        double actualCx = rect.X + rect.Width / 2;

        Assert.Equal(expectedCx, actualCx, 2);
    }

    [Fact]
    public void GetHitRegions_AdjacentPointsDoNotOverlap()
    {
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        // Check adjacent points within same half don't overlap in X
        // Points 1-6 are in the inner half, right to left
        for (int pt = 1; pt <= 5; pt++)
        {
            var r1 = regions.Points[pt];
            var r2 = regions.Points[pt + 1];
            double r1Right = r1.X + r1.Width;
            double r2Right = r2.X + r2.Width;

            // One should end where the other begins (or not overlap)
            bool noOverlap = r1.X >= r2Right || r2.X >= r1Right;
            Assert.True(noOverlap, $"Points {pt} and {pt + 1} overlap in X");
        }
    }

    [Fact]
    public void GetHitRegions_BarCoversBarStrip()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        Assert.Equal(layout.BarX(panelOnLeft: false), regions.Bar.X, 2);
        Assert.Equal(layout.BarWidth, regions.Bar.Width, 2);
        Assert.Equal(0, regions.Bar.Y, 2);
        Assert.Equal(layout.BoardHeight, regions.Bar.Height, 2);
    }

    [Fact]
    public void GetHitRegions_CubeCoversLeftRail()
    {
        var layout = BoardLayout.Default;
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);

        Assert.NotNull(regions.Cube);
        Assert.Equal(layout.LeftRailX(panelOnLeft: false), regions.Cube.X, 2);
        Assert.Equal(layout.LeftRailWidth, regions.Cube.Width, 2);
    }

    [Fact]
    public void GetHitRegions_OnRollTrayIsNull()
    {
        var regions = _renderer.GetHitRegions(MinimalRequest(), _defaultOptions);
        Assert.Null(regions.OnRollTray);
    }
}