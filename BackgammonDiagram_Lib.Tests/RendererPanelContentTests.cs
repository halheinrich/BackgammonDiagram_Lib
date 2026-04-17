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
        // nd=1.20, dt=0.50 → Too Good to Double; opp would take (dt < 1).
        var request = MinimalCubeBuilder(noDoubleEquity: 1.20, doubleTakeEquity: 0.50).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains("Best:   Too Good to Double / Take", svg);
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
        //   UserDoubleError > 0 → user did the opposite (No Double).
        //   UserTakeError == 0  → user took correctly (Take).
        // Expected Actual: No Double / Take
        var b = MinimalCubeBuilder(noDoubleEquity: 0.40, doubleTakeEquity: 0.60);
        b.UserDoubleError = 0.1;
        b.UserTakeError = 0;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: No Double / Take", svg);
    }

    [Fact]
    public void CubePanel_ActualLine_TooGoodFlavorDroppedInActual()
    {
        // Best = "Too Good to Double" when nd >= 1. User's actual choice is
        // either "No Double" or "Double" — never the stylized "Too Good" form.
        //   UserDoubleError == 0 → user did the correct thing, which is the
        //   flat "No Double" in the Actual line.
        var b = MinimalCubeBuilder(noDoubleEquity: 1.20, doubleTakeEquity: 0.50);
        b.UserDoubleError = 0;
        b.UserTakeError = null;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains("Actual: No Double / ?", svg);
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
    //  Helpers
    // -----------------------------------------------------------------------

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
