using BackgammonDiagram_Lib.Rendering;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the SVG title strip (baked into the renderer in d02a5d3) and the
/// top/bottom rail labels — match / money / Crawford variants.
/// </summary>
public class RendererTitleAndRailTests
{
    // -----------------------------------------------------------------------
    //  Title strip
    // -----------------------------------------------------------------------

    [Fact]
    public void Title_Absent_NoTitleStripOrOffset()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Title = null;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // No translate-group for board offset.
        Assert.DoesNotContain("<g transform=\"translate(0,", svg);
    }

    [Fact]
    public void Title_Whitespace_TreatedAsAbsent()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Title = "   ";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.DoesNotContain("<g transform=\"translate(0,", svg);
    }

    [Fact]
    public void Title_Present_StripRenderedAndViewBoxTaller()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Title = null;
        var svgPlain = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        b.Title = "Example title";
        var svgTitled = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Title text appears.
        Assert.Contains(">Example title<", svgTitled);
        // Translate-group wrapping the board is added.
        Assert.Contains("<g transform=\"translate(0,", svgTitled);
        // ViewBox is taller when titled.
        var plainHeight = ExtractViewBoxHeight(svgPlain);
        var titledHeight = ExtractViewBoxHeight(svgTitled);
        Assert.True(titledHeight > plainHeight,
            $"Titled viewBox height {titledHeight} not greater than plain {plainHeight}.");
    }

    [Fact]
    public void Title_SpecialChars_AreXmlEscaped()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Title = "A & B <x> \"y\"";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // The raw characters must not appear un-escaped in the title text.
        Assert.Contains("A &amp; B &lt;x&gt; &quot;y&quot;", svg);
        Assert.DoesNotContain(">A & B <x> \"y\"<", svg);
    }

    // -----------------------------------------------------------------------
    //  Rail labels — match / money / Crawford
    // -----------------------------------------------------------------------

    [Fact]
    public void RailLabel_MatchPlay_ShowsNameAndNeeds()
    {
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 7;
        b.OnRollNeeds = 3;
        b.OpponentNeeds = 5;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice needs 3</text>", svg);
        Assert.Contains(">Bob needs 5</text>", svg);
    }

    [Fact]
    public void RailLabel_MoneyGame_UsesParenthesisedSuffix()
    {
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 0; // money-game sentinel
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice (money game)</text>", svg);
        Assert.Contains(">Bob (money game)</text>", svg);
    }

    [Fact]
    public void RailLabel_Crawford_AppendedToMatchLabel()
    {
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 7;
        b.OnRollNeeds = 1;
        b.OpponentNeeds = 5;
        b.IsCrawford = true;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice needs 1 Crawford</text>", svg);
        Assert.Contains(">Bob needs 5 Crawford</text>", svg);
    }

    // -----------------------------------------------------------------------
    //  Hit-region offset correctness when a title is present
    // -----------------------------------------------------------------------

    [Fact]
    public void GetHitRegions_WithTitle_PointsShiftedDownByTitleStrip()
    {
        var layout = BoardLayout.Default;
        var options = TestFixtures.DefaultOptions();

        var bNone = TestFixtures.MinimalBuilder();
        bNone.Title = null;
        var regionsPlain = DiagramRenderer.GetHitRegions(bNone.Build(), options);

        var bTitled = TestFixtures.MinimalBuilder();
        bTitled.Title = "Example title";
        var regionsTitled = DiagramRenderer.GetHitRegions(bTitled.Build(), options);

        double delta = regionsTitled.ViewBox.Height - regionsPlain.ViewBox.Height;
        Assert.True(delta > 0, "Titled viewBox must be taller.");

        // Every point's Y must shift by exactly the title-strip height.
        for (int pt = 1; pt <= 24; pt++)
        {
            var dy = regionsTitled.Points[pt].Y - regionsPlain.Points[pt].Y;
            Assert.Equal(delta, dy, 2);
        }
        // Bar and cube Y shift too.
        Assert.Equal(delta, regionsTitled.Bar.Y - regionsPlain.Bar.Y, 2);
        Assert.NotNull(regionsTitled.Cube);
        Assert.NotNull(regionsPlain.Cube);
        Assert.Equal(delta, regionsTitled.Cube!.Y - regionsPlain.Cube!.Y, 2);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>Extracts the height component from <c>viewBox="0 0 W H"</c>.</summary>
    private static double ExtractViewBoxHeight(string svg)
    {
        const string key = "viewBox=\"";
        int i = svg.IndexOf(key, StringComparison.Ordinal);
        Assert.True(i >= 0, "SVG has no viewBox attribute.");
        int start = i + key.Length;
        int end = svg.IndexOf('"', start);
        var parts = svg[start..end].Split(' ');
        return double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
    }
}
