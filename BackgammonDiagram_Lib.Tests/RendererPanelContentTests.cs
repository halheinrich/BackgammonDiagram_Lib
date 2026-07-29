using System.Globalization;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Themes;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the cube-panel contents rendered into SVG:
/// the Best Decision banner, the four-row Equity/Loss table, the two
/// percentages tables (No Double, Take), the footer lines, plus equity
/// formatting, percentage scale, and PanelBackgroundColor wiring.
/// </summary>
public class RendererPanelContentTests
{
    // -----------------------------------------------------------------------
    //  Best / Actual banner
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_BestLine_CompoundActionPresent()
    {
        // nd=0.40, dt=0.60 → Double is correct for doubler; Take is correct for opp.
        var request = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   Double / Take", svg);
    }

    [Fact]
    public void CubePanel_BestLine_NoDoubleTakeRendersBothHalves()
    {
        // nd=1.20, dt=0.50 → BestDoublerAction = NoDouble (min(dt,1)=0.50 is
        // not greater than nd=1.20), BestTakerAction = Take (dt < 1). That is
        // the (NoDouble, Take) pair, NOT the too-good (NoDouble, Pass) one:
        // No Double is right because doubling gains nothing, not because the
        // position is too good. Both halves are analysis rather than game
        // record — the taker half says what the response to a double would
        // be — so both render, and "No Double / Take" reads as the distinct
        // verdict it is. Guards the boundary of the Too Good rule from the
        // low side.
        var request = MinimalCubeBuilder(noDoubleEquity: 1.20, doubleTakeEquity: 0.50).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   No Double / Take", svg);
        Assert.DoesNotContain("Too Good", svg);
    }

    [Fact]
    public void CubePanel_BestLine_TooGoodRendersTooGood()
    {
        // nd=1.50, dt=1.20 → BestDoublerAction = NoDouble (min(dt,1)=1.00 is
        // not greater than nd=1.50) and BestTakerAction = Pass (dt >= 1).
        // (NoDouble, Pass) is CubeDecisionPair.TooGood: the doubler wins more
        // playing on than cashing. The banner names that pair rather than
        // printing the bare doubler half.
        var request = MinimalCubeBuilder(noDoubleEquity: 1.50, doubleTakeEquity: 1.20).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   Too Good", svg);
        Assert.DoesNotContain("Best:   No Double", svg);
        // The pair label replaces the halves — no " / Pass" tail.
        Assert.DoesNotContain("Too Good /", svg);
    }

    [Fact]
    public void CubePanel_BestLine_NoDoubleTakeHoldsWhenDoublingIsActivelyBad()
    {
        // nd=0.25, dt=-0.10 → doubleEquity=min(dt,1)=-0.10 < nd, so best is
        // "No Double", and dt < 1 makes the best response Take. The same
        // (NoDouble, Take) pair as above but reached from a negative
        // double/take equity, where doubling loses ground rather than merely
        // gaining none. The taker half still renders: how far short the
        // double falls does not change that a take is what a double would
        // meet.
        var request = MinimalCubeBuilder(noDoubleEquity: 0.25, doubleTakeEquity: -0.10).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   No Double / Take", svg);
    }

    [Fact]
    public void CubePanel_BestLine_PassWhenDoubleTakeEquityExceedsOne()
    {
        // nd=0.30, dt=1.20 → Double is correct; opp should pass (dt > 1).
        var request = MinimalCubeBuilder(noDoubleEquity: 0.30, doubleTakeEquity: 1.20).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   Double / Pass", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_ReadsStampedPlayedActions()
    {
        // nd=0.40, dt=0.60 → Best = (Double, Take). The doubled game was
        // passed, so both halves are stamped, in contract, and both render.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoublerAction = CubeAction.Double;
        b.UserTakerAction = CubeAction.Pass;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: Double / Pass", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_EquityTieDoubleStillRendersDouble()
    {
        // Regression — the bug the stamped fields exist to fix. nd == dt ==
        // 0.50: doubling gains nothing, so the tie-break picks NoDouble as
        // BestDoublerAction and UserDoubleError is 0 because the double cost
        // nothing. The old derivation read that zero as "played the best
        // action" and printed "No Double" for a game that was doubled and
        // taken. Both errors are set here to exactly the values that used to
        // mislead the line; only the stamped actions decide it now.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.50, doubleTakeEquity: 0.50);
        b.UserDoubleError = 0;
        b.UserTakeError = 0;
        b.UserDoublerAction = CubeAction.Double;
        b.UserTakerAction = CubeAction.Take;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: Double / Take", svg);
        Assert.DoesNotContain("Actual: No Double", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_UndoubledGameShowsDoublerHalfAlone()
    {
        // An undoubled game: the producer stamps the doubler half and leaves
        // the taker half null, because the opponent never faced the cube.
        // This is the in-contract one-sided record, and the null half alone
        // — no suppression involved — carries the doubler label by itself.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoublerAction = CubeAction.NoDouble;
        b.UserTakerAction = null;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: No Double", svg);
        Assert.DoesNotContain("Actual: No Double /", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_StaleTakerOnNoDoubleIsSuppressed()
    {
        // Defence against an out-of-contract stamp, not a property of the
        // line. DecisionData validates each played half only against its own
        // action domain and leaves cross-half consistency (a recorded taker
        // response implies the doubler doubled) to the producer, so a stamped
        // (NoDouble, Take) can reach the renderer even though the opponent
        // cannot have taken a cube that was never offered. The Actual line
        // drops that stale taker at its stamped-data boundary. The Best line
        // has no such rule — its (NoDouble, Take) is analysis and renders in
        // full.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoublerAction = CubeAction.NoDouble;
        b.UserTakerAction = CubeAction.Take;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: No Double", svg);
        Assert.DoesNotContain("Actual: No Double /", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_StampedTooGoodPairRendersTooGood()
    {
        // The too-good classification is pair-level and applies to Actual
        // exactly as it does to Best: a stamped (NoDouble, Pass) is the
        // too-good pair and names itself. Guards the reach of the
        // stale-taker filter, which drops only (NoDouble, Take) and must
        // leave this NoDouble-doubler pair intact to be classified.
        var b = MinimalCubeBuilder(noDoubleEquity: 1.50, doubleTakeEquity: 1.20);
        b.UserDoublerAction = CubeAction.NoDouble;
        b.UserTakerAction = CubeAction.Pass;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: Too Good", svg);
        Assert.DoesNotContain("Actual: No Double", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_StampedActionsIgnoreTheBestPair()
    {
        // nd=1.50, dt=1.20 → Best = (NoDouble, Pass) = Too Good. The player
        // doubled anyway and was passed, so Actual is (Double, Pass). Guards
        // that the line reads its own halves rather than leaking the Best
        // pair's classification onto them.
        var b = MinimalCubeBuilder(noDoubleEquity: 1.50, doubleTakeEquity: 1.20);
        b.UserDoublerAction = CubeAction.Double;
        b.UserTakerAction = CubeAction.Pass;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Best:   Too Good", svg);
        Assert.Contains("Actual: Double / Pass", svg);
        Assert.DoesNotContain("Actual: Too Good", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_TakerHalfAloneRendersUnknownDoubler()
    {
        // A taker half with no doubler half violates the producer contract (a
        // recorded response implies a double), but the line still has to
        // render something: the unknown doubler half prints "?" rather than
        // being silently dropped.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoublerAction = null;
        b.UserTakerAction = CubeAction.Take;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: ? / Take", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_SuppressedWhenNoActionStamped()
    {
        // Null means "not recorded" — a resignation-terminal cube record, or
        // JSON written before the played-action fields existed. The error
        // fields are set and deliberately ignored: there is no inference
        // fallback, so an unrecorded decision drops the line entirely rather
        // than guessing an action from a zero error.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoubleError = 0;
        b.UserTakeError = 0.1;
        b.UserDoublerAction = null;
        b.UserTakerAction = null;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.DoesNotContain("Actual:", svg);
    }

    // -----------------------------------------------------------------------
    //  Equity/Loss table — all four rows, fixed order, losses always shown
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_Rows_AllFourOptionsPresent()
    {
        var request = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains(">No Double<", svg);
        Assert.Contains(">Double<", svg);
        Assert.Contains(">Take<", svg);
        Assert.Contains(">Pass<", svg);
    }

    [Fact]
    public void CubePanel_EquityLoss_ShownForAllRows_IncludingZero()
    {
        // nd=0.40, dt=0.60.
        //   No Double loss = 0.60 - 0.40 = 0.2000
        //   Double    loss = 0          = 0.0000
        //   Take      loss = 0          = 0.0000
        //   Pass      loss = 1.00 - 0.60 = 0.4000
        var request = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains(">0.0000<", svg);   // correct-option rows
        Assert.Contains(">0.2000<", svg);   // No Double mistake
        Assert.Contains(">0.4000<", svg);   // Pass mistake
        Assert.DoesNotContain(">-0.2000<", svg);
        Assert.DoesNotContain(">-0.4000<", svg);
    }

    // -----------------------------------------------------------------------
    //  Percentages — source data is 0..1, renderer scales to percent
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_Percentages_ScaledFromFractionToPercent()
    {
        var b = MinimalCubeBuilder(0.40, 0.60);
        b.WinPctAfterNoDouble = 0.702;
        b.LosePctAfterNoDouble = 0.298;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // 0.702 must render as "70.2%", not "0.7%".
        Assert.Contains(">70.2%<", svg);
        Assert.Contains(">29.8%<", svg);
    }

    [Fact]
    public void CubePanel_Percentages_TablesHaveColumnHeaders()
    {
        var request = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

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
        // BG at 0, Gammon at 47, Win at 120. Win offset chosen so the
        // Win→Gammon header gap visually matches Gammon→BG (~33px).
        var request = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

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
        var request = MinimalCubeBuilder(noDoubleEquity: 0.25, doubleTakeEquity: -0.125).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        // Positive — explicit + sign.
        Assert.Contains(">+0.2500<", svg);
        // Negative — intrinsic minus sign.
        Assert.Contains(">-0.1250<", svg);
    }

    [Fact]
    public void CubePanel_EquityFormat_AlwaysUsesInvariantDecimalSeparator()
    {
        // Switch the thread culture to one that formats doubles with a comma
        // (e.g. fr-FR). Invariant-culture formatting in the renderer must be
        // unaffected — the SVG should still contain "0.2500", not "0,2500".
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var request = MinimalCubeBuilder(0.25, 0.10).Build();
            var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

            Assert.Contains(">+0.2500<", svg);
            Assert.DoesNotContain(">+0,2500<", svg);
        }
        finally
        {
            CultureInfo.CurrentCulture = prior;
        }
    }

    // -----------------------------------------------------------------------
    //  Footer — Pass Justifying Dbl label
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_PassJustifyingDbl_UsesOneDecimal()
    {
        var b = MinimalCubeBuilder(0.4, 0.8);
        b.ProbOfOpponentErrorJustifyingDouble = 0.42;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Pass Justifying Dbl: 42.0%", svg);
    }

    // -----------------------------------------------------------------------
    //  Footer — Analysis Level label (sourced from DecisionData.CubeDepth)
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_AnalysisLevel_RendersFullCubeDepthWhenSet()
    {
        var b = MinimalCubeBuilder(0.4, 0.8);
        // The cube panel has only one analysis depth and column space to
        // spare, so it renders the full CubeDepth string. The abbreviation
        // does not appear in the panel.
        b.CubeDepth = "Rollout: 1296 trials. 3-ply";
        b.CubeDepthAbbreviation = "3p1296";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Analysis Level: Rollout: 1296 trials. 3-ply", svg);
        Assert.DoesNotContain("3p1296", svg);
    }

    [Fact]
    public void CubePanel_AnalysisLevel_OmittedWhenCubeDepthEmpty()
    {
        var b = MinimalCubeBuilder(0.4, 0.8);
        // CubeDepth is what the panel reads; a non-empty abbreviation alone
        // does not force the Analysis Level line on.
        b.CubeDepthAbbreviation = "3p1296";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.DoesNotContain("Analysis Level:", svg);
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
        var options = new DiagramOptions { Theme = theme };

        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        var svg = DiagramRenderer.RenderSvg(b.Build(), options);

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

        // The inner <rect> painted immediately after the dark outer rect is
        // the board-proper rect with width="<BoardWidth>". It must be
        // identical across aspect presets.
        var boardNatural = ExtractInnerBoardRectWidth(natural);
        var boardWide = ExtractInnerBoardRectWidth(wide);
        Assert.Equal(boardNatural, boardWide, precision: 4);

        // But the viewBox total width must differ.
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
        // viewBox="0 0 W H"
        var m = System.Text.RegularExpressions.Regex.Match(svg,
            "viewBox=\"0 0 ([0-9.]+) ([0-9.]+)\"");
        Assert.True(m.Success, "viewBox not found in SVG");
        double w = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        double h = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        return (w, h);
    }

    private static double ExtractInnerBoardRectWidth(string svg)
    {
        // Rect order in the SVG: title-strip bg, full-canvas dark, board-proper.
        // The board-proper rect carries the intrinsic BoardWidth and must not
        // depend on Aspect preset.
        var matches = System.Text.RegularExpressions.Regex.Matches(svg,
            "<rect x=\"[0-9.]+\" y=\"0\" width=\"([0-9.]+)\" height=\"[0-9.]+\"");
        Assert.True(matches.Count >= 3, "expected at least three root-level rects (title + canvas + board)");
        return double.Parse(matches[2].Groups[1].Value, CultureInfo.InvariantCulture);
    }


    /// <summary>Builds a Solution-mode cube decision with the given equities.</summary>
    private static DiagramRequest.Builder MinimalCubeBuilder(
        double noDoubleEquity, double doubleTakeEquity)
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        b.Mode = DiagramMode.Solution;
        b.NoDoubleEquity = noDoubleEquity;
        b.DoubleTakeEquity = doubleTakeEquity;
        return b;
    }
}
