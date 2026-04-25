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
    public void CubePanel_BestLine_TooGoodWhenNoDoubleEquityAtLeastOne()
    {
        // nd=1.20, dt=0.50 → Too Good to Double. Since the best action is not
        // to double, no opp take/pass decision arises — the Best line carries
        // only the doubler half.
        var request = MinimalCubeBuilder(noDoubleEquity: 1.20, doubleTakeEquity: 0.50).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   Too Good to Double", svg);
        Assert.DoesNotContain("Best:   Too Good to Double /", svg);
    }

    [Fact]
    public void CubePanel_BestLine_NoDoubleHasNoOppHalf()
    {
        // nd=0.25, dt=-0.10 → doubleEquity=min(dt,1)=-0.10 < nd, so best is
        // "No Double". Same rule as "Too Good": no double → no opp decision.
        var request = MinimalCubeBuilder(noDoubleEquity: 0.25, doubleTakeEquity: -0.10).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   No Double", svg);
        Assert.DoesNotContain("Best:   No Double /", svg);
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
    public void CubePanel_ActualLine_DerivedFromUserErrors()
    {
        // nd=0.40, dt=0.60 → Best doubler = Double; Best opp = Take.
        //   UserDoubleError == 0 → user doubled correctly → actualDoubler = Double.
        //   UserTakeError  > 0   → opp got take/pass wrong → actualOpp = Pass.
        // The doubler-side is "Double" (not "No Double"), so the opp half is
        // retained — this test exercises both sides of the derivation.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoubleError = 0;
        b.UserTakeError = 0.1;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: Double / Pass", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_NoDoubleSuppressesOppHalf()
    {
        // nd=0.40, dt=0.60 → Best = Double/Take.
        //   UserDoubleError > 0 → user played the opposite (No Double).
        //   UserTakeError set but stale — opp never faced a take/pass, so
        //   the Actual line must show only the doubler half.
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoubleError = 0.1;
        b.UserTakeError = 0;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: No Double", svg);
        Assert.DoesNotContain("Actual: No Double /", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_TooGoodFlavorDroppedInActual()
    {
        // Best = "Too Good to Double" when nd >= 1. User's actual choice is
        // either "No Double" or "Double" — never the stylized "Too Good" form.
        //   UserDoubleError == 0  → user did the correct thing, which is the
        //     flat "No Double" in the Actual line.
        //   UserTakeError == null → opp never faced a decision; the opp half
        //     is suppressed for any non-Double doubler action.
        var b = MinimalCubeBuilder(noDoubleEquity: 1.20, doubleTakeEquity: 0.50);
        b.UserDoubleError = 0;
        b.UserTakeError = null;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: No Double", svg);
        Assert.DoesNotContain("Actual: No Double /", svg);
        Assert.DoesNotContain("Actual: Too Good to Double", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_SuppressedWhenBothErrorsNull()
    {
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoubleError = null;
        b.UserTakeError = null;
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
    //  Footer — Analysis Level label (sourced from DecisionData.CubeDepthAbbreviation)
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_AnalysisLevel_RendersCubeDepthAbbreviationWhenSet()
    {
        var b = MinimalCubeBuilder(0.4, 0.8);
        // The long form is ignored by the renderer; only the abbreviation reaches the panel.
        b.CubeDepth = "XG Roller+";
        b.CubeDepthAbbreviation = "R+";
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Analysis Level: R+", svg);
        Assert.DoesNotContain("XG Roller+", svg);
    }

    [Fact]
    public void CubePanel_AnalysisLevel_OmittedWhenCubeDepthAbbreviationEmpty()
    {
        var b = MinimalCubeBuilder(0.4, 0.8);
        // A non-empty long CubeDepth alone does not force the Analysis Level
        // line on — the abbreviation is what the panel reads.
        b.CubeDepth = "XG Roller+";
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
