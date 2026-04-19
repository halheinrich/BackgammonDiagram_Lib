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
    //  Title strip — composed from (dice / cube) + optional PositionNumber
    // -----------------------------------------------------------------------

    [Fact]
    public void Title_PlayDecision_ShowsDiceToPlay()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Dice = [3, 1];
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">3-1 to play</text>", svg);
        // Translate-group wrapping the board is added under the strip.
        Assert.Contains("<g transform=\"translate(0,", svg);
    }

    [Fact]
    public void Title_CubeDecision_ShowsCubeAction()
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Cube Action?</text>", svg);
    }

    [Fact]
    public void Title_PositionNumber_RendersAsSeparateSecondColumnCell()
    {
        var b = TestFixtures.MinimalBuilder();
        b.PositionNumber = 7;
        b.Dice = [3, 1];
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Action cell in column 1, position cell in column 2 — distinct <text>
        // elements, no em-dash concatenation.
        Assert.Contains(">3-1 to play</text>", svg);
        Assert.Contains(">Position 7</text>", svg);
    }

    [Fact]
    public void Title_NoPositionNumber_ActionStandsAlone()
    {
        var b = TestFixtures.MinimalBuilder();
        b.PositionNumber = null;
        b.Dice = [3, 1];
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.DoesNotContain(">Position ", svg);
        Assert.Contains(">3-1 to play</text>", svg);
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
    public void GetHitRegions_ViewBoxIncludesTitleStripHeight()
    {
        // The title strip is always present under the new layout (composed
        // from dice-to-play / Cube-Action? plus optional PositionNumber),
        // so hit regions must account for the ~22 px offset.
        var layout = BoardLayout.Default;
        var regions = DiagramRenderer.GetHitRegions(
            TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());

        // ViewBox total height = board height + title strip offset.
        Assert.True(regions.ViewBox.Height > layout.BoardHeight,
            "ViewBox should include title-strip height above the board.");
        // Every point's Y must sit below the title strip.
        double offset = regions.ViewBox.Height - layout.BoardHeight;
        foreach (var (_, rect) in regions.Points)
            Assert.True(rect.Y >= offset - 1,
                $"Point Y {rect.Y} should be at or below title offset {offset}.");
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
