using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Themes;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the cube-panel contents rendered into SVG: the Best / Actual banner,
/// the four-row Equity/Loss table — each action's equity and error, the
/// producer's, from its one calculation — the two percentages tables (No
/// double, Take), the Analysis Level footer, plus equity formatting,
/// percentage scale, and PanelBackgroundColor wiring.
///
/// Every cube word pinned here is CubeLabels' spelling; CubeLabelsTests owns
/// the labels themselves, and these tests own which label each line carries.
/// </summary>
public class RendererPanelContentTests
{
    /// <summary>A cube decision's solution with the given equities and played actions.</summary>
    private static string RenderCube(double noDoubleEquity, double doubleTakeEquity,
        CubeAction? userDoublerAction = CubeAction.Double, CubeAction? userTakerAction = CubeAction.Take) =>
        DiagramRenderer.RenderSvg(
            TestFixtures.RequestFor(
                TestFixtures.CubeWith(noDoubleEquity, doubleTakeEquity, userDoublerAction, userTakerAction),
                DiagramMode.Solution),
            TestFixtures.DefaultOptions());

    // -----------------------------------------------------------------------
    //  Best / Actual banner
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_BestLine_CompoundActionPresent()
    {
        // nd=0.40, dt=0.60 → Double is correct for doubler; Take is correct for opp.
        Assert.Contains("Best:   Double / Take", RenderCube(0.40, 0.60));
    }

    [Fact]
    public void CubePanel_BestLine_NoDoubleClaimReadsAlone()
    {
        // nd=1.20, dt=0.50 → BestDoublerClaim = NoDouble (doubling gains
        // nothing) and BestTakerAction = Take (dt < 1), so BestClaimPair is
        // NoDoubleTake — NOT the too-good pair: the position is not good
        // enough to double, not too good to. No double reaches only the take,
        // so the banner reads the claim alone (halheinrich/backgammon#185).
        var svg = RenderCube(1.20, 0.50);

        Assert.Contains("Best:   No double", svg);
        Assert.DoesNotContain("Best:   No double /", svg);
        Assert.DoesNotContain("Too good", svg);
    }

    [Fact]
    public void CubePanel_BestLine_TooGoodRendersTooGood()
    {
        // nd=1.50, dt=1.20 → BestClaimPair is TooGoodPass. Composed from the
        // two board actions the banner read "No double / Take" (defect 1 of
        // halheinrich/backgammon#185); read whole, the claim pair says Too
        // good, which reaches only the pass, so the response is not printed.
        var svg = RenderCube(1.50, 1.20, CubeAction.NoDouble, userTakerAction: null);

        Assert.Contains("Best:   Too good", svg);
        Assert.DoesNotContain("Best:   No double", svg);
        Assert.DoesNotContain("Too good /", svg);
    }

    [Fact]
    public void CubePanel_BestLine_TieBoundaryIncoherentPairReadsTooGood()
    {
        // nd=1.00, dt=1.20 — the measure-zero boundary where the producer's
        // tie-breaks compose BestClaimPair = NoDoublePass. The banner reads
        // "Too good" (SPEC-scoring §3's sixth-cell ruling), never "No double",
        // which is the NoDoubleTake verdict and a different answer.
        var svg = RenderCube(1.00, 1.20);

        Assert.Contains("Best:   Too good", svg);
        Assert.DoesNotContain("Best:   No double", svg);
    }

    [Fact]
    public void CubePanel_BestLine_NoDoubleClaimHoldsWhenDoublingIsActivelyBad()
    {
        // nd=0.25, dt=-0.10 → the same NoDoubleTake pair, reached from a
        // negative double/take equity: how far short the double falls does
        // not change the claim.
        var svg = RenderCube(0.25, -0.10);

        Assert.Contains("Best:   No double", svg);
        Assert.DoesNotContain("Best:   No double /", svg);
    }

    [Fact]
    public void CubePanel_BestLine_PassWhenDoubleTakeEquityExceedsOne()
    {
        // nd=0.30, dt=1.20 → Double is correct; opp should pass (dt > 1).
        Assert.Contains("Best:   Double / Pass", RenderCube(0.30, 1.20, CubeAction.Double, CubeAction.Pass));
    }

    [Fact]
    public void CubePanel_ActualLine_ReadsStampedPlayedActions()
    {
        // The doubled game was passed, so both halves are stamped and render.
        Assert.Contains("Actual: Double / Pass", RenderCube(0.40, 0.60, CubeAction.Double, CubeAction.Pass));
    }

    [Fact]
    public void CubePanel_ActualLine_EquityTieDoubleStillRendersDouble()
    {
        // Regression — the bug the stamped actions exist to fix. nd == dt ==
        // 0.50: doubling gains nothing, so the tie-break picks NoDouble as the
        // best doubler action and the double's error is 0. Read from that
        // zero, the line printed "No double" for a game that was doubled and
        // taken; only the stamped actions decide it.
        var svg = RenderCube(0.50, 0.50, CubeAction.Double, CubeAction.Take);

        Assert.Contains("Actual: Double / Take", svg);
        Assert.DoesNotContain("Actual: No double", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_UndoubledGameShowsDoublerHalfAlone()
    {
        // An undoubled game: the doubler half stamped, the taker half null,
        // because the opponent never faced the cube.
        var svg = RenderCube(0.40, 0.60, CubeAction.NoDouble, userTakerAction: null);

        Assert.Contains("Actual: No double", svg);
        Assert.DoesNotContain("Actual: No double /", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_StaleTakerOnNoDoubleIsSuppressed()
    {
        // Defence against an out-of-contract stamp: the record holds each
        // played half to its own domain and leaves cross-half consistency to
        // the producer, so a stamped (NoDouble, Take) can reach the renderer.
        // The Actual line drops that stale taker at its stamped-data boundary.
        var svg = RenderCube(0.40, 0.60, CubeAction.NoDouble, CubeAction.Take);

        Assert.Contains("Actual: No double", svg);
        Assert.DoesNotContain("Actual: No double /", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_StampedTooGoodPairRendersTooGood()
    {
        // A stamped (NoDouble, Pass) is the too-good pair and names itself
        // through CubeLabels.Label(CubeClaimPair.TooGoodPass), the Best
        // banner's spelling. Guards the reach of the stale-taker filter,
        // which drops only (NoDouble, Take).
        var svg = RenderCube(1.50, 1.20, CubeAction.NoDouble, CubeAction.Pass);

        Assert.Contains("Actual: Too good", svg);
        Assert.DoesNotContain("Actual: No double", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_StampedActionsIgnoreTheBestPair()
    {
        // BestClaimPair = TooGoodPass; the player doubled anyway and was
        // passed: the line reads its own halves.
        var svg = RenderCube(1.50, 1.20, CubeAction.Double, CubeAction.Pass);

        Assert.Contains("Best:   Too good", svg);
        Assert.Contains("Actual: Double / Pass", svg);
        Assert.DoesNotContain("Actual: Too good", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_TakerHalfAloneRendersUnknownDoubler()
    {
        // A taker half with no doubler half violates the producer's contract,
        // but the line still renders it: the unknown doubler half prints "?".
        Assert.Contains("Actual: ? / Take", RenderCube(0.40, 0.60, userDoublerAction: null, CubeAction.Take));
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
        var svg = DiagramRenderer.RenderSvg(TestFixtures.RequestFor(record, DiagramMode.Solution), TestFixtures.DefaultOptions());

        Assert.DoesNotContain("Actual:", svg);
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

        string Equity(CubeAction action) => (decision.ActionEquity(action) >= 0 ? "+" : "")
            + decision.ActionEquity(action).ToString("F4", CultureInfo.InvariantCulture);
        static string Loss(double error) => error.ToString("F4", CultureInfo.InvariantCulture);

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
    //  Equity formatting — invariant-culture, signed, 4 decimals
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
