using System.Text.RegularExpressions;
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
    public void Title_SourceFile_IsLeftAnchoredAfterActionColumn()
    {
        // Col 2 (source) is now left-anchored at a fixed x just right of the
        // reserved action column (edgeMargin 8 + ActionColumnWidth 110 = 118),
        // not centred — so it can't collide with the upper-right XGID label.
        // The attribute order matches the action cell (no text-anchor); x=118
        // pins it to the source column specifically.
        var b = TestFixtures.MinimalBuilder();
        b.SourceFile = "game.xg";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(
            """<text x="118" y="11" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
        Assert.Contains(">game</text>", svg);

        // The former centred title cell (text-anchor="middle" at 12px bold) is
        // gone — nothing else in the strip uses that sequence.
        Assert.DoesNotContain(
            """text-anchor="middle" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" """,
            svg);
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

    // The three money-label states track PositionData.IsJacoby's three states.
    // The rule is per-session but the label is per-player, so both rails carry
    // the same wording.

    [Fact]
    public void RailLabel_MoneyGame_JacobyCarriedTrue_NamesTheRule()
    {
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 0; // money-game sentinel
        b.IsJacoby = true;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice (Money Game, Jacoby)</text>", svg);
        Assert.Contains(">Bob (Money Game, Jacoby)</text>", svg);
    }

    [Fact]
    public void RailLabel_MoneyGame_JacobyCarriedFalse_NamesTheRuleOff()
    {
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 0; // money-game sentinel
        b.IsJacoby = false;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice (Money Game, No Jacoby)</text>", svg);
        Assert.Contains(">Bob (Money Game, No Jacoby)</text>", svg);
    }

    [Fact]
    public void RailLabel_MoneyGame_JacobyNotCarried_KeepsBareSuffix()
    {
        // null is "the source did not stamp it", never "off" — the renderer
        // serves surfaces whose producers may legitimately not carry the fact,
        // so it degrades to the label from before halheinrich/backgammon#143 rather than guessing a rule.
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 0; // money-game sentinel
        b.IsJacoby = null;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice (Money Game)</text>", svg);
        Assert.Contains(">Bob (Money Game)</text>", svg);
        Assert.DoesNotContain("Jacoby", svg);
    }

    [Fact]
    public void RailLabel_MatchPlay_IgnoresAJacobyStamp()
    {
        // Jacoby is a money-game fact; a match record carrying one is
        // tolerated, not rejected, and never reaches the label.
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 7;
        b.OnRollNeeds = 3;
        b.OpponentNeeds = 5;
        b.IsJacoby = true;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">Alice needs 3</text>", svg);
        Assert.Contains(">Bob needs 5</text>", svg);
        Assert.DoesNotContain("Jacoby", svg);
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
    //  Rail labels — weight, anchors, positions, fit (halheinrich/backgammon#229)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void RailLabels_AllFourAreBold_AtTheirAnchorsAndPositions(PanelPosition side)
    {
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = "Alice";
        b.OpponentName = "Bob";
        b.MatchLength = 7;
        b.OnRollNeeds = 3;
        b.OpponentNeeds = 5;
        b.AnalysisPanelPosition = side;
        var request = b.Build();
        // Natural: the intrinsic panel width, so the board's x offset derives
        // from BoardLayout alone.
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = AspectPreset.Natural });

        // Board-local coordinates (the board sits in the title strip's
        // translate group). The rails run between the side rails.
        var layout = BoardLayout.Default;
        double railX = layout.BoardOffsetX(side == PanelPosition.Left) + layout.LeftRailWidth;
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double nameX = railX + DiagramRenderer.RailLabelInset;
        double pipX = railX + railWidth - DiagramRenderer.RailLabelInset;
        double topY = layout.TopRailHeight / 2;
        double bottomY = layout.BottomRailY + layout.BottomRailHeight / 2;

        // On roll at bottom: the bottom rail carries the on-roll player.
        Assert.True(request.OnRollAtBottom);
        AssertRailLabel(svg, "Bob needs 5", nameX, topY, anchorEnd: false);
        AssertRailLabel(svg, $"Pip: {request.Position.OpponentPipCount}", pipX, topY, anchorEnd: true);
        AssertRailLabel(svg, "Alice needs 3", nameX, bottomY, anchorEnd: false);
        AssertRailLabel(svg, $"Pip: {request.Position.OnRollPipCount}", pipX, bottomY, anchorEnd: true);
    }

    [Fact]
    public void RailLabels_BoldIsSpeltOnce_InTheOneRailTextEmitter()
    {
        // A survey of the renderer's source: the rails' only <text> markup is
        // AppendRailLabel's, so the weight (and family and size) is written
        // once. A rail method that emitted its own <text> would be a second
        // copy of the presentation rule.
        string source = File.ReadAllText(RendererSourcePath());

        foreach (string caller in new[] { "AppendTopRail", "AppendBottomRail", "AppendRailLabels" })
        {
            string body = MethodBody(source, caller);
            Assert.DoesNotContain("<text", body);
            Assert.DoesNotContain("font-weight", body);
        }

        string emitter = MethodBody(source, "AppendRailLabel");
        Assert.Equal(1, TestFixtures.CountOccurrences(emitter, "<text"));
        Assert.Equal(1, TestFixtures.CountOccurrences(emitter, """font-weight="bold" """));
    }

    /// <summary>
    /// The longest labels the rail carries: a long real name with the
    /// Crawford suffix, and the same name with the longest money-game text;
    /// the pip label at the largest possible count (15 checkers on the bar).
    /// </summary>
    public static TheoryData<AspectPreset, int> RailFitCases()
    {
        var data = new TheoryData<AspectPreset, int>();
        foreach (var preset in Enum.GetValues<AspectPreset>())
        {
            data.Add(preset, 11);  // match: "... needs 11 Crawford"
            data.Add(preset, 0);   // money: "... (Money Game, No Jacoby)"
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(RailFitCases))]
    public void RailLabels_LongestPlayerLabel_DoesNotReachThePipLabel(AspectPreset preset, int matchLength)
    {
        const string longName = "Mochizuki Masayuki";
        const int maxPip = 375;
        var b = TestFixtures.MinimalBuilder();
        b.OnRollName = longName;
        b.OpponentName = longName;
        b.MatchLength = matchLength;
        b.OnRollNeeds = 11;
        b.OpponentNeeds = 11;
        b.IsCrawford = matchLength > 0;
        b.IsJacoby = false;
        b.OnRollPipCount = maxPip;
        b.OpponentPipCount = maxPip;
        var svg = DiagramRenderer.RenderSvg(b.Build(), new DiagramOptions { Aspect = preset });

        string playerLabel = matchLength > 0
            ? $"{longName} needs 11 Crawford"
            : $"{longName} (Money Game, No Jacoby)";
        string pipLabel = $"Pip: {maxPip}";

        var names = RailTexts(svg, playerLabel);
        var pips = RailTexts(svg, pipLabel);
        Assert.Equal(2, names.Count);
        Assert.Equal(2, pips.Count);

        double nameWidth = DiagramRenderer.EstimateTextWidth(
            playerLabel, DiagramRenderer.RailLabelFontSize, DiagramRenderer.TextWeight.Bold);
        double pipWidth = DiagramRenderer.EstimateTextWidth(
            pipLabel, DiagramRenderer.RailLabelFontSize, DiagramRenderer.TextWeight.Bold);
        for (int rail = 0; rail < 2; rail++)
        {
            double nameRight = Num(names[rail]["x"]) + nameWidth;
            double pipLeft = Num(pips[rail]["x"]) - pipWidth;
            Assert.True(nameRight < pipLeft,
                $"{preset}: \"{playerLabel}\" ends at {nameRight:F2}, \"{pipLabel}\" starts at {pipLeft:F2}.");
        }
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

    /// <summary>
    /// Asserts the one rail <c>&lt;text&gt;</c> carrying <paramref name="content"/>:
    /// bold, at today's family and size, vertically centred at
    /// (<paramref name="x"/>, <paramref name="y"/>), right-anchored when
    /// <paramref name="anchorEnd"/> and start-anchored (no attribute) otherwise.
    /// </summary>
    private static void AssertRailLabel(string svg, string content, double x, double y, bool anchorEnd)
    {
        var attrs = Assert.Single(RailTexts(svg, content));
        Assert.Equal("bold", attrs["font-weight"]);
        Assert.Equal("sans-serif", attrs["font-family"]);
        Assert.Equal(SvgFormat.Number(DiagramRenderer.RailLabelFontSize), attrs["font-size"]);
        Assert.Equal("central", attrs["dominant-baseline"]);
        Assert.Equal(SvgFormat.Number(x), attrs["x"]);
        Assert.Equal(SvgFormat.Number(y), attrs["y"]);
        if (anchorEnd)
            Assert.Equal("end", attrs["text-anchor"]);
        else
            Assert.False(attrs.ContainsKey("text-anchor"), $"\"{content}\" should be start-anchored.");
    }

    /// <summary>
    /// The attributes of every <c>&lt;text&gt;</c> element whose content is
    /// exactly <paramref name="content"/>, in document order.
    /// </summary>
    private static List<Dictionary<string, string>> RailTexts(string svg, string content) =>
        Regex.Matches(svg, $"<text ([^>]*)>{Regex.Escape(content)}</text>")
            .Select(m => Regex.Matches(m.Groups[1].Value, "([a-z-]+)=\"([^\"]*)\"")
                .ToDictionary(a => a.Groups[1].Value, a => a.Groups[2].Value))
            .ToList();

    private static double Num(string x) =>
        double.Parse(x, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The renderer's source file, reached from the test binary the way
    /// <see cref="TestPaths"/> reaches TestData: bin/{config}/{tfm} is three
    /// levels below this test project, which sits beside the library's.
    /// </summary>
    private static string RendererSourcePath()
    {
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "BackgammonDiagram_Lib", "Rendering", "DiagramRenderer.cs"));
        Assert.True(File.Exists(path), $"Renderer source not found: {path}");
        return path;
    }

    /// <summary>
    /// The source of the <c>static void</c> method <paramref name="name"/>,
    /// from its declaration to its closing brace at member indentation.
    /// </summary>
    private static string MethodBody(string source, string name)
    {
        var match = Regex.Match(source, $@"static void {Regex.Escape(name)}\(.*?\n    \}}\r?\n",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"Method {name} not found in the renderer's source.");
        return match.Value;
    }

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
