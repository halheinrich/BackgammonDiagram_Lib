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
    public void Title_PositionNumber_RendersAsSeparateCell()
    {
        var b = TestFixtures.MinimalBuilder();
        b.PositionNumber = 7;
        b.Dice = [3, 1];
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

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
        var b = TestFixtures.MinimalBuilder();
        b.PositionNumber = 7;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(
            """text-anchor="end" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
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

    [Fact]
    public void Title_SourceFile_RendersStemInColumn2()
    {
        // Extension is stripped — slide audience doesn't need the file type.
        var b = TestFixtures.MinimalBuilder();
        b.SourceFile = "mochy-falafel.xg";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">mochy-falafel</text>", svg);
        Assert.DoesNotContain(">mochy-falafel.xg</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_IsCentreAnchored()
    {
        // Col 2 carries text-anchor="middle" in the title-strip attribute
        // ordering. Point numbers and cube text also use text-anchor="middle"
        // but point numbers are font-size="11" (not "12") and cube text has
        // no dominant-baseline attribute — so this specific sequence is
        // unique to the centred title cell.
        var b = TestFixtures.MinimalBuilder();
        b.SourceFile = "game.xg";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(
            """text-anchor="middle" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
        Assert.Contains(">game</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_XgpExtensionStripped()
    {
        var b = TestFixtures.MinimalBuilder();
        b.SourceFile = "game.xgp";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">game</text>", svg);
        Assert.DoesNotContain(">game.xgp</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_OnlyLastExtensionStripped()
    {
        // "abc.weird.xg" must become "abc.weird", not "abc".
        var b = TestFixtures.MinimalBuilder();
        b.SourceFile = "abc.weird.xg";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">abc.weird</text>", svg);
        Assert.DoesNotContain(">abc.weird.xg</text>", svg);
    }

    [Fact]
    public void Title_SourceFile_Null_NoCol2TextElement()
    {
        // Baseline: same builder but no SourceFile → no centred text.
        // Compared SVG: same builder + SourceFile → one additional <text>.
        // The difference count isolates the col 2 cell.
        var bNull = TestFixtures.MinimalBuilder();
        bNull.PositionNumber = 7;
        bNull.SourceFile = null;
        var svgNull = DiagramRenderer.RenderSvg(bNull.Build(), TestFixtures.DefaultOptions());

        var bPop = TestFixtures.MinimalBuilder();
        bPop.PositionNumber = 7;
        bPop.SourceFile = "abc.xg";
        var svgPop = DiagramRenderer.RenderSvg(bPop.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Position 7</text>", svgNull);      // Col 3 intact.
        Assert.DoesNotContain(">abc</text>", svgNull);       // Col 2 empty.
        Assert.Contains(">abc</text>", svgPop);              // Col 2 present when populated.
        Assert.Equal(
            TestFixtures.CountOccurrences(svgNull, "<text ") + 1,
            TestFixtures.CountOccurrences(svgPop, "<text "));
    }

    [Fact]
    public void Title_SourceFile_Empty_TreatedAsAbsent()
    {
        // Empty string behaves like null — same text-element count.
        var bNull = TestFixtures.MinimalBuilder();
        bNull.PositionNumber = 7;
        bNull.SourceFile = null;
        var svgNull = DiagramRenderer.RenderSvg(bNull.Build(), TestFixtures.DefaultOptions());

        var bEmpty = TestFixtures.MinimalBuilder();
        bEmpty.PositionNumber = 7;
        bEmpty.SourceFile = string.Empty;
        var svgEmpty = DiagramRenderer.RenderSvg(bEmpty.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Position 7</text>", svgEmpty);
        Assert.Equal(
            TestFixtures.CountOccurrences(svgNull, "<text "),
            TestFixtures.CountOccurrences(svgEmpty, "<text "));
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
