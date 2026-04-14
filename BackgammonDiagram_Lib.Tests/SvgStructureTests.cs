using BackgammonDiagram_Lib.Rendering;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

public class SvgStructureTests
{
    [Fact]
    public void StartsWithSvgTag()
    {
        Assert.StartsWith("<svg", TestFixtures.Render().TrimStart());
    }

    [Fact]
    public void ContainsClosingSvgTag()
    {
        Assert.Contains("</svg>", TestFixtures.Render());
    }

    [Fact]
    public void ContainsViewBox()
    {
        Assert.Contains("viewBox=", TestFixtures.Render());
    }

    [Fact]
    public void Contains24Triangles()
    {
        Assert.Equal(24, TestFixtures.CountOccurrences(TestFixtures.Render(), "<polygon"));
    }

    [Fact]
    public void Contains24PointNumbers()
    {
        int count = TestFixtures.CountOccurrences(TestFixtures.Render(), "<text");
        Assert.True(count >= 24, $"Expected at least 24 <text> elements, found {count}");
    }

    [Fact]
    public void ContainsCubeRect()
    {
        int count = TestFixtures.CountOccurrences(TestFixtures.Render(), "<rect");
        Assert.True(count >= 2, $"Expected at least 2 <rect> elements, found {count}");
    }

    [Fact]
    public void ContainsPlayerNames()
    {
        var svg = TestFixtures.Render();
        Assert.Contains("Hal", svg);
        Assert.Contains("Opponent", svg);
    }

    [Fact]
    public void ContainsPipCounts()
    {
        var svg = TestFixtures.Render();
        Assert.Contains("133", svg);
        Assert.Contains("131", svg);
    }

    [Fact]
    public void HomeBoardLeftAndRight_ProduceDifferentSvg()
    {
        var svgRight = TestFixtures.Render(TestFixtures.MinimalRequest());
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = false;
        var svgLeft = TestFixtures.Render(b.Build());
        Assert.NotEqual(svgRight, svgLeft);
    }

    [Fact]
    public void IsCube_NoDiceRendered()
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        var svg = TestFixtures.Render(b.Build());
        Assert.DoesNotContain("fill=\"#FFFFFF\" stroke=\"#888\" stroke-width=\"0.75\"", svg);
    }

    [Fact]
    public void CubeOwnerDefault_IsCentered()
    {
        var b = new DiagramRequest.Builder { HomeBoardOnRight = true, Dice = [1, 1] };
        var svg =  DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());
        var layout = BoardLayout.Default;
        double cubeSize = layout.LeftRailWidth * 0.7;
        double expectedY = layout.BoardHeight / 2 - cubeSize / 2;
        Assert.Contains($"y=\"{expectedY:0.##}\"", svg);
    }

    [Fact]
    public void BarAndOverflow_LabelsPresent()
    {
        var mop = new int[26];
        mop[25] = 3; mop[0] = -2; mop[6] = 8; mop[19] = -7;
        var b = TestFixtures.MinimalBuilder();
        b.Mop = mop;
        var svg = TestFixtures.Render(b.Build());
        Assert.Contains(">8<", svg);
        Assert.Contains(">7<", svg);
    }
}