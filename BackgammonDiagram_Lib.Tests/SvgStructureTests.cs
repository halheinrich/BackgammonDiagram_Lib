using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
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
        // The default record's players.
        var svg = TestFixtures.Render();
        Assert.Contains("Alice", svg);
        Assert.Contains("Bob", svg);
    }

    [Fact]
    public void ContainsPipCounts()
    {
        // The standard start's, the board's own: 167 each.
        var svg = TestFixtures.Render();
        Assert.Equal(2, TestFixtures.CountOccurrences(svg, ">Pip: 167</text>"));
    }

    [Fact]
    public void HomeBoardLeftAndRight_ProduceDifferentSvg()
    {
        var svgRight = TestFixtures.Render(TestFixtures.MinimalRequest());
        var svgLeft = TestFixtures.Render(TestFixtures.MinimalRequest() with { HomeBoardOnRight = false });
        Assert.NotEqual(svgRight, svgLeft);
    }

    [Fact]
    public void IsCube_NoDiceRendered()
    {
        var svg = TestFixtures.Render(TestFixtures.RequestFor(TestRecords.Cube()));
        Assert.DoesNotContain("fill=\"#FFFFFF\" stroke=\"#888\" stroke-width=\"0.75\"", svg);
    }

    [Fact]
    public void CubeOwnerDefault_IsCentered()
    {
        var svg = DiagramRenderer.RenderSvg(
            DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()), TestFixtures.DefaultOptions());
        var layout = BoardLayout.Default;
        double cubeSize = layout.LeftRailWidth * 0.7;
        double expectedY = layout.BoardHeight / 2 - cubeSize / 2;
        Assert.Contains($"y=\"{expectedY:0.##}\"", svg);
    }

    [Fact]
    public void BarAndOverflow_LabelsPresent()
    {
        var board = TestFixtures.Board((25, 3), (0, -2), (6, 8), (19, -7));
        var svg = TestFixtures.Render(TestFixtures.RequestFor(TestFixtures.CheckerPlayOn(board)));
        Assert.Contains(">8<", svg);
        Assert.Contains(">7<", svg);
    }
}