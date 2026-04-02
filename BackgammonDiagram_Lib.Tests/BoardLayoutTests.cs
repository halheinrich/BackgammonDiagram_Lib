using BackgammonDiagram_Lib.Rendering;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

public class BoardLayoutTests
{
    [Fact]
    public void Default_CheckerRadiusIs14()
    {
        Assert.Equal(14, BoardLayout.Default.CheckerRadius);
    }

    [Fact]
    public void PointHeightIs5Diameters()
    {
        var layout = BoardLayout.Default;
        Assert.Equal(layout.CheckerRadius * 2 * 5, layout.PointHeight);
    }

    [Fact]
    public void HalfWidthIs6Columns()
    {
        var layout = BoardLayout.Default;
        Assert.Equal(layout.ColumnWidth * 6, layout.HalfWidth);
    }

    [Fact]
    public void BoardHeightIsReasonable()
    {
        Assert.InRange(BoardLayout.Default.BoardHeight, 400, 560);
    }
}