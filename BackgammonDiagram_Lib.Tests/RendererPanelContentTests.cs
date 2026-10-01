using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Themes;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the cube-panel contents rendered into SVG: the Best / Actual banner
/// — the Best line every answer whose cost counts as zero, the Actual line
/// the played answer — the four-row Equity/Loss table — each action's equity
/// and error, the producer's, from its one calculation — the two percentages
/// tables (No double, Take), the Analysis Level footer, plus the shared
/// display of every equity and loss, percentage scale, and
/// PanelBackgroundColor wiring.
///
/// Every cube word pinned here is CubeLabels' spelling; CubeLabelsTests owns
/// the labels themselves, and these tests own which label each line carries.
/// </summary>
public class RendererPanelContentTests
{
    /// <summary>A cube decision's solution with the given equities and played actions.</summary>
    private static string RenderCube(double noDoubleEquity, double doubleTakeEquity,
        CubeAction? userDoublerAction = CubeAction.Double, CubeAction? userTakerAction = CubeAction.Take) =>
        RenderSolution(TestFixtures.CubeWith(noDoubleEquity, doubleTakeEquity, userDoublerAction, userTakerAction));

    /// <summary>The solution diagram of <paramref name="record"/>.</summary>
    private static string RenderSolution(BgDecisionData record) =>
        DiagramRenderer.RenderSvg(TestFixtures.RequestFor(record, DiagramMode.Solution), TestFixtures.DefaultOptions());

    /// <summary>The text of the banner line led by <paramref name="prefix"/>, or null where none is drawn.</summary>
    private static string? BannerLine(string svg, string prefix)
    {
        var lines = Regex.Matches(svg, $">{Regex.Escape(prefix)}([^<]*)</text>").Select(m => m.Groups[1].Value).ToList();
        Assert.True(lines.Count <= 1, $"More than one line led by \"{prefix}\".");
        return lines.SingleOrDefault();
    }

    /// <summary>The Best line's text after its lead-in.</summary>
    private static string BestLine(string svg) =>
        BannerLine(svg, "Best:   ") ?? throw new Xunit.Sdk.XunitException("No Best line drawn.");

    /// <summary>The Actual line's text after its lead-in, or null where none is drawn.</summary>
    private static string? ActualLine(string svg) => BannerLine(svg, "Actual: ");

    // -----------------------------------------------------------------------
    //  The Best line — every answer whose cost counts as zero
    // -----------------------------------------------------------------------
    //
    //  SPEC-scoring §3, "The tie": "The review's Best line lists every answer
    //  whose cost counts as zero, so at a tie it lists them all." Each list
    //  is pinned on exact equities, so no other answer's cost falls under the
    //  display-zero threshold by accident; the whole line is compared, so an
    //  extra answer fails as surely as a missing one.

    [Theory]
    // Each truth, off any tie.
    [InlineData(0.40, 0.60, true, "Double / Take")]
    [InlineData(1.20, 0.50, true, "No double")]           // not good enough to double
    [InlineData(0.25, -0.10, true, "No double")]          // doubling actively bad
    [InlineData(0.30, 1.20, true, "Double / Pass")]
    [InlineData(1.50, 1.20, true, "Too good")]
    // The fourth answer's other label: the same equities where gammons are
    // not possible. No double costs nothing there too (SPEC-scoring §3's
    // no-gammon table), so both are listed though nothing ties.
    [InlineData(1.50, 1.20, false, "No double, No double / Pass")]
    [InlineData(1.20, 1.50, false, "No double, No double / Pass")]
    // halheinrich/backgammon#293's tie: N = 1 with a pass. No double,
    // Double / Pass and the fourth answer all cost 0, under either label.
    [InlineData(1.00, 1.20, true, "No double, Double / Pass, Too good")]
    [InlineData(1.00, 1.20, false, "No double, Double / Pass, No double / Pass")]
    // N = T = 1: Double / Take costs T − 1 = 0 too, so all four.
    [InlineData(1.00, 1.00, true, "No double, Double / Take, Double / Pass, Too good")]
    [InlineData(1.00, 1.00, false, "No double, Double / Take, Double / Pass, No double / Pass")]
    // T = 1 with N < 1: take and pass both cost 0.
    [InlineData(0.50, 1.00, true, "Double / Take, Double / Pass")]
    // T = N < 1: not doubling and doubling tie, and they'd take.
    [InlineData(0.50, 0.50, true, "No double, Double / Take")]
    public void CubePanel_BestLine_ListsEveryAnswerWhoseCostCountsAsZero(
        double noDouble, double doubleTake, bool gammonsPossible, string expected)
    {
        var record = TestFixtures.CubeWithGammons(gammonsPossible, noDouble, doubleTake);

        Assert.Equal(expected, BestLine(RenderSolution(record)));
    }

    [Theory]
    // N = 0.99996 with a pass: No double and Too good each cost 1 − N =
    // 0.00004, which is not 0 but shows as 0.0000, so both are listed beside
    // the truth, Double / Pass.
    [InlineData(0.99996, "No double, Double / Pass, Too good")]
    // N = 0.99994: 1 − N = 0.00006 shows as 0.0001, so it is not.
    [InlineData(0.99994, "Double / Pass")]
    public void CubePanel_BestLine_JudgesEachCostByTheSharedZeroRule(double noDouble, string expected)
    {
        var record = TestFixtures.CubeWithGammons(possible: true, noDouble, doubleTakeEquity: 1.20);

        Assert.Equal(expected, BestLine(RenderSolution(record)));
    }

    public static TheoryData<double, double, bool> BestLineGrid()
    {
        double[] equities = [-0.50, 0.00, 0.30, 0.50, 0.99994, 0.99996, 1.00, 1.00004, 1.00006, 1.20, 1.50, 2.00];
        var grid = new TheoryData<double, double, bool>();
        foreach (double noDouble in equities)
            foreach (double doubleTake in equities)
                foreach (bool gammonsPossible in new[] { true, false })
                    grid.Add(noDouble, doubleTake, gammonsPossible);
        return grid;
    }

    [Theory]
    [MemberData(nameof(BestLineGrid))]
    public void CubePanel_BestLine_AlwaysListsTheTruth(double noDouble, double doubleTake, bool gammonsPossible)
    {
        // The truth always costs nothing, so it is always listed, though it
        // does not decide how many answers are. A decision whose list lacks it
        // contradicts the producer.
        var record = TestFixtures.CubeWithGammons(gammonsPossible, noDouble, doubleTake);
        var listed = BestLine(RenderSolution(record)).Split(", ");

        Assert.Contains(CubeLabels.Label(record.Decision.BestAnswer, record), listed);
    }

    // -----------------------------------------------------------------------
    //  The Actual line — the played halves, read as an answer when both are
    // -----------------------------------------------------------------------

    [Theory]
    // Both halves present: the answer they form, labelled at its decision.
    [InlineData(CubeAction.NoDouble, CubeAction.Take, true, "No double")]
    [InlineData(CubeAction.Double, CubeAction.Take, true, "Double / Take")]
    [InlineData(CubeAction.Double, CubeAction.Pass, true, "Double / Pass")]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, true, "Too good")]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, false, "No double / Pass")]
    // One half missing: never inferred. The present half alone, in its
    // action label; a missing doubler half shows "?".
    [InlineData(CubeAction.NoDouble, null, true, "No double")]
    [InlineData(CubeAction.Double, null, true, "Double")]
    [InlineData(null, CubeAction.Take, true, "? / Take")]
    [InlineData(null, CubeAction.Pass, true, "? / Pass")]
    [InlineData(null, CubeAction.Pass, false, "? / Pass")]
    public void CubePanel_ActualLine_ReadsEveryRecordedCombination(
        CubeAction? doubler, CubeAction? taker, bool gammonsPossible, string expected)
    {
        // A too-good position, so no line can borrow the truth's label.
        var record = TestFixtures.CubeWithGammons(gammonsPossible, 1.50, 1.20, doubler, taker);

        Assert.Equal(expected, ActualLine(RenderSolution(record)));
    }

    [Fact]
    public void CubePanel_ActualLine_SuppressedWhenNoActionStamped()
    {
        // No action stated, only the analyser's errors: there is no inference
        // from an error, so an unrecorded decision drops the line entirely.
        var record = TestRecords.Cube(decision: TestRecords.CubeData(
            noDoubleEquity: 0.40, doubleTakeEquity: 0.60,
            userDoublerAction: null, userTakerAction: null,
            unstatedDoublerActionError: 0, unstatedTakerActionError: 0.1));

        Assert.Null(ActualLine(RenderSolution(record)));
    }

    [Fact]
    public void CubePanel_ActualLine_EquityTieDoubleStillRendersDouble()
    {
        // Regression — the bug the stamped actions exist to fix. nd == dt ==
        // 0.50: doubling gains nothing, so the tie-break picks NoDouble as the
        // best doubler action and the double's error is 0. Read from that
        // zero, the line printed "No double" for a game that was doubled and
        // taken; only the stamped actions decide it.
        Assert.Equal("Double / Take", ActualLine(RenderCube(0.50, 0.50, CubeAction.Double, CubeAction.Take)));
    }

    [Fact]
    public void CubePanel_ActualLine_IsThePlayedAnswer_NotTheBest()
    {
        // Too good is best; the player doubled anyway and was passed. Each
        // line reads its own answer.
        var svg = RenderCube(1.50, 1.20, CubeAction.Double, CubeAction.Pass);

        Assert.Equal("Too good", BestLine(svg));
        Assert.Equal("Double / Pass", ActualLine(svg));
    }

    // -----------------------------------------------------------------------
    //  Equity/Loss table — each action's equity and error, the producer's
    // -----------------------------------------------------------------------

    /// <summary>The Equity/Loss table's four rows: each label with its equity and loss cells.</summary>
    private static List<(string Label, string Equity, string Loss)> CubeRows(string svg) =>
        Regex.Matches(svg,
                """font-weight="bold" fill="[^"]*">([^<]+)</text>\s*<text [^>]*text-anchor="end"[^>]*>([+-][0-9]+\.[0-9]{4})</text>\s*<text [^>]*text-anchor="end"[^>]*>([0-9]+\.[0-9]{4})</text>""")
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value))
            .ToList();

    [Theory]
    [InlineData(0.40, 0.60)]    // double / take
    [InlineData(0.30, 1.20)]    // double / pass: doubling is worth the cash
    [InlineData(1.50, 1.20)]    // too good
    [InlineData(0.25, -0.10)]   // no double
    [InlineData(0.50, 0.50)]    // the doubler's tie
    [InlineData(0.80, 1.00)]    // the taker's tie
    public void CubePanel_EachRowIsItsActionsEquityAndError_FromTheRecord(double noDouble, double doubleTake)
    {
        var decision = TestFixtures.CubeWith(noDouble, doubleTake).Decision;
        var rows = CubeRows(RenderCube(noDouble, doubleTake));

        string Equity(CubeAction action) => DiagramRenderer.FormatEquity(decision.ActionEquity(action));
        static string Loss(double error) => EquityLoss.Format(error);

        Assert.Equal(
        [
            (CubeLabels.Label(CubeAction.NoDouble), Equity(CubeAction.NoDouble), Loss(decision.DoublerActionError(CubeAction.NoDouble))),
            (CubeLabels.Label(CubeAction.Double), Equity(CubeAction.Double), Loss(decision.DoublerActionError(CubeAction.Double))),
            (CubeLabels.Label(CubeAction.Take), Equity(CubeAction.Take), Loss(decision.TakerActionError(CubeAction.Take))),
            (CubeLabels.Label(CubeAction.Pass), Equity(CubeAction.Pass), Loss(decision.TakerActionError(CubeAction.Pass))),
        ], rows);
    }

    [Fact]
    public void CubePanel_StatesNoPassValueAndNoDoublingRuleOfItsOwn()
    {
        // The pass's value and the rule for doubling's equity are the
        // producer's, stated once in CubeDecisionData.ActionEquity. The
        // renderer reads each row's equity there and restates neither: no
        // pass constant, no min of the take and the cash. A rendered panel
        // cannot show this — a copy computes the same numbers — so the
        // source is surveyed.
        string source = RendererSource.Read();
        string panel = RendererSource.MethodBody(source, "AppendCubePanel");

        Assert.DoesNotContain("PassEquity", source);
        Assert.DoesNotContain("Math.Min", panel);
        Assert.DoesNotContain("1.0", panel);
        foreach (var action in Enum.GetValues<CubeAction>())
            Assert.Contains($"ActionEquity(CubeAction.{action})", panel);
    }

    [Fact]
    public void CubePanel_Rows_AllFourOptionsPresent()
    {
        var svg = RenderCube(0.40, 0.60);

        Assert.Contains(">No double<", svg);
        Assert.Contains(">Double<", svg);
        Assert.Contains(">Take<", svg);
        Assert.Contains(">Pass<", svg);
    }

    [Fact]
    public void CubePanel_EquityLoss_ShownForAllRows_IncludingZero()
    {
        // nd=0.40, dt=0.60: No double 0.2000 behind, Double and Take 0.0000,
        // Pass 0.4000 behind.
        var svg = RenderCube(0.40, 0.60);

        Assert.Contains(">0.0000<", svg);
        Assert.Contains(">0.2000<", svg);
        Assert.Contains(">0.4000<", svg);
        Assert.DoesNotContain(">-0.2000<", svg);
        Assert.DoesNotContain(">-0.4000<", svg);
    }

    // -----------------------------------------------------------------------
    //  Percentages — source data is 0..1, renderer scales to percent
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_Percentages_ScaledFromFractionToPercent()
    {
        var record = TestRecords.Cube(decision: TestRecords.CubeData(winPctAfterNoDouble: 0.702));
        var svg = DiagramRenderer.RenderSvg(TestFixtures.RequestFor(record, DiagramMode.Solution), TestFixtures.DefaultOptions());

        // 0.702 must render as "70.2%", not "0.7%"; the loss, 1 − the win,
        // is the record's derivation.
        Assert.Contains(">70.2%<", svg);
        Assert.Contains(">29.8%<", svg);
    }

    [Fact]
    public void CubePanel_Percentages_TablesHaveColumnHeaders()
    {
        var svg = RenderCube(0.40, 0.60);

        Assert.Contains(">Win<", svg);
        Assert.Contains(">Gammon<", svg);
        Assert.Contains(">BG<", svg);
        Assert.Contains(">On-roll<", svg);
        Assert.Contains(">Opponent<", svg);
    }

    [Fact]
    public void CubePanel_Percentages_ColumnHeadersAtExpectedXPositions()
    {
        // textX = PanelMargin(6) + 4 = 10; NumericBlockWidth = 215;
        // numericRightX = 225. Right-anchored offsets in AppendPctTable:
        // BG at 0, Gammon at 47, Win at 120.
        var svg = RenderCube(0.40, 0.60);

        Assert.Matches(@"<text x=""105""[^>]*>Win<",    svg);
        Assert.Matches(@"<text x=""178""[^>]*>Gammon<", svg);
        Assert.Matches(@"<text x=""225""[^>]*>BG<",    svg);
    }

    // -----------------------------------------------------------------------
    //  Equity and loss display — the shared precision, signed, invariant
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_EquityFormat_UsesFourDecimalsWithSign()
    {
        var svg = RenderCube(0.25, -0.125);

        Assert.Contains(">+0.2500<", svg);    // positive — explicit + sign
        Assert.Contains(">-0.1250<", svg);    // negative — intrinsic minus sign
    }

    [Fact]
    public void CubePanel_EquityFormat_AlwaysUsesInvariantDecimalSeparator()
    {
        // Under a culture that formats doubles with a comma (fr-FR), the SVG
        // still carries "0.2500", not "0,2500".
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var svg = RenderCube(0.25, 0.10);

            Assert.Contains(">+0.2500<", svg);
            Assert.DoesNotContain(">+0,2500<", svg);
        }
        finally
        {
            CultureInfo.CurrentCulture = prior;
        }
    }

    [Theory]
    [InlineData(-0.00004)]
    [InlineData(0.00004)]
    [InlineData(-0.0)]
    public void CubePanel_EquityBelowTheDisplayZero_ShowsThroughTheSharedPrecision_WithNoMinusSign(double noDouble)
    {
        // The No double row's equity is the no-double equity. Below 0.00005 in
        // magnitude it shows as the shared display's 0.0000, with the panel's
        // explicit plus and never a minus: a negative zero once read "+-0.0000".
        var rows = CubeRows(RenderCube(noDouble, 0.50));

        Assert.Equal("+" + EquityLoss.Format(0), rows[0].Equity);
        Assert.Equal("+0.0000", rows[0].Equity);
    }

    [Fact]
    public void CubePanel_LossBelowTheDisplayZero_ShowsThroughTheSharedPrecision()
    {
        // nd = 0.5, dt = 0.50004: doubling is best by 0.00004, so No double's
        // error is 0.00004, which is not 0 but shows as 0.0000. (Every error
        // the table shows is derived, so never negative.)
        var rows = CubeRows(RenderCube(0.50, 0.50004));

        Assert.Equal("No double", rows[0].Label);
        Assert.Equal("0.0000", rows[0].Loss);
    }

    [Fact]
    public void Renderer_StatesNoPrecisionForAnEquityOrALoss()
    {
        // The display precision has one owner, BgDataTypes_Lib's EquityLoss
        // (SPEC-scoring §3, halheinrich/backgammon#202). The renderer's one
        // numeric format string is the percentages' one decimal, which is not
        // a cost; no four-decimal format, or any other, returns.
        string source = RendererSource.Read();
        var formats = Regex.Matches(source, "\"[FfNn][0-9]+\"").Select(m => m.Value).Distinct();

        Assert.Equal(["\"F1\""], formats);
    }

    // -----------------------------------------------------------------------
    //  Footer — Analysis Level; no Pass Justifying Dbl line
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_AnalysisLevel_RendersTheFullDepthLabel()
    {
        // One analysis depth and column space to spare: the full label, never
        // the abbreviation. Both are the record's, derived from its facts.
        var record = TestFixtures.CubeWith(0.4, 0.8, analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296);
        Assert.Equal("3p1296", record.Decision.DepthAbbreviation);

        var svg = DiagramRenderer.RenderSvg(TestFixtures.RequestFor(record, DiagramMode.Solution), TestFixtures.DefaultOptions());

        Assert.Contains("Analysis Level: Rollout: 1296 trials. 3-ply", svg);
        Assert.DoesNotContain("3p1296", svg);
    }

    [Fact]
    public void CubePanel_AnalysisLevel_OmittedWhenNoDepthIsRecorded()
    {
        var record = TestFixtures.CubeWith(0.4, 0.8, analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown);
        Assert.Null(record.Decision.Depth);

        var svg = DiagramRenderer.RenderSvg(TestFixtures.RequestFor(record, DiagramMode.Solution), TestFixtures.DefaultOptions());

        Assert.DoesNotContain("Analysis Level:", svg);
    }

    [Fact]
    public void CubePanel_HasNoPassJustifyingDoubleLine()
    {
        // The line went with the stored field it read, which XG never stored
        // (halheinrich/backgammon#273). Deriving the figure is
        // halheinrich/backgammon#288's.
        Assert.DoesNotContain("Pass Justifying", RenderCube(0.4, 0.8));
    }

    // -----------------------------------------------------------------------
    //  PanelBackgroundColor — honoured in SVG output
    // -----------------------------------------------------------------------

    [Fact]
    public void Panel_UsesThemePanelBackgroundColor()
    {
        const string distinctive = "#ABCDEF";
        var theme = new CustomTheme(
            boardColor: "#C8A96E",
            pointColorDark: "#8B2500",
            pointColorLight: "#F5DEB3",
            checkerColorOnRoll: "#1A1A1A",
            checkerColorOpponent: "#F0F0F0",
            diceColor: "#FFFFFF",
            textColor: "#000000",
            panelBackgroundColor: distinctive,
            name: "PanelBgTest");

        var svg = DiagramRenderer.RenderSvg(
            TestFixtures.MinimalRequest() with { Mode = DiagramMode.Solution },
            new DiagramOptions { Theme = theme });

        Assert.Contains($"fill=\"{distinctive}\"", svg);
    }

    // -----------------------------------------------------------------------
    //  Aspect preset — board geometry fixed, panel widens to hit target
    // -----------------------------------------------------------------------

    [Fact]
    public void AspectPreset_Natural_UsesIntrinsicPanelWidth()
    {
        var svg = DiagramRenderer.RenderSvg(
            TestFixtures.MinimalRequest(),
            new DiagramOptions { Aspect = AspectPreset.Natural });

        double aspect = ExtractViewBoxAspect(svg);
        // BoardLayout.Default: BoardWidth ≈ 429.8, PanelWidth = 154,
        // BoardHeight = 446, title strip = 22 → aspect ≈ 583.8 / 468 ≈ 1.248.
        Assert.InRange(aspect, 1.24, 1.26);
    }

    [Fact]
    public void AspectPreset_Widescreen16x9_ForcesViewBoxTo16x9()
    {
        var svg = DiagramRenderer.RenderSvg(
            TestFixtures.MinimalRequest(),
            new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });

        double aspect = ExtractViewBoxAspect(svg);
        Assert.InRange(aspect, 16.0 / 9.0 - 0.01, 16.0 / 9.0 + 0.01);
    }

    [Fact]
    public void AspectPreset_Standard4x3_ForcesViewBoxTo4x3()
    {
        var svg = DiagramRenderer.RenderSvg(
            TestFixtures.MinimalRequest(),
            new DiagramOptions { Aspect = AspectPreset.Standard4x3 });

        double aspect = ExtractViewBoxAspect(svg);
        Assert.InRange(aspect, 4.0 / 3.0 - 0.01, 4.0 / 3.0 + 0.01);
    }

    [Fact]
    public void AspectPreset_Widescreen_BoardWidthUnchanged_OnlyPanelGrows()
    {
        // Checker radius drives board width; the aspect change must only
        // grow the panel. Board rects keep their natural size → checkers
        // stay perfectly round.
        string natural = DiagramRenderer.RenderSvg(
            TestFixtures.MinimalRequest(),
            new DiagramOptions { Aspect = AspectPreset.Natural });
        string wide = DiagramRenderer.RenderSvg(
            TestFixtures.MinimalRequest(),
            new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });

        Assert.Equal(ExtractInnerBoardRectWidth(natural), ExtractInnerBoardRectWidth(wide), precision: 4);
        Assert.NotEqual(ExtractViewBoxWidth(natural), ExtractViewBoxWidth(wide));
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static double ExtractViewBoxAspect(string svg)
    {
        (double w, double h) = ExtractViewBox(svg);
        return w / h;
    }

    private static double ExtractViewBoxWidth(string svg) => ExtractViewBox(svg).w;

    private static (double w, double h) ExtractViewBox(string svg)
    {
        var m = Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+) ([0-9.]+)\"");
        Assert.True(m.Success, "viewBox not found in SVG");
        return (double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static double ExtractInnerBoardRectWidth(string svg)
    {
        // Rect order in the SVG: title-strip bg, full-canvas dark, board-proper.
        // The board-proper rect carries the intrinsic BoardWidth and must not
        // depend on Aspect preset.
        var matches = Regex.Matches(svg, "<rect x=\"[0-9.]+\" y=\"0\" width=\"([0-9.]+)\" height=\"[0-9.]+\"");
        Assert.True(matches.Count >= 3, "expected at least three root-level rects (title + canvas + board)");
        return double.Parse(matches[2].Groups[1].Value, CultureInfo.InvariantCulture);
    }
}
