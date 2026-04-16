using System.Globalization;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Themes;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the analysis-panel and cube-panel contents rendered into SVG:
/// DetermineCubeAction branches, equity formatting, and PanelBackgroundColor
/// wiring.
/// </summary>
public class RendererPanelContentTests
{
    // -----------------------------------------------------------------------
    //  Cube action — the four branches of DetermineCubeAction
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1.20, 1.10, "Too Good to Double")]   // nd >= 1.0
    [InlineData(0.40, 1.10, "Double, Pass")]          // dt > nd && dt >= 1.0
    [InlineData(0.40, 0.80, "Double, Take")]          // dt > nd, dt < 1.0
    [InlineData(0.50, 0.40, "No Double")]             // dt <= nd
    public void CubePanel_CubeAction_MatchesBranch(
        double noDoubleEquity, double doubleTakeEquity, string expected)
    {
        var request = MinimalCubeBuilder(noDoubleEquity, doubleTakeEquity).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        Assert.Contains($">{expected}<", svg);
    }

    // -----------------------------------------------------------------------
    //  Equity formatting — invariant-culture, signed, 3 decimals
    // -----------------------------------------------------------------------

    [Fact]
    public void CubePanel_EquityFormat_UsesThreeDecimalsWithSign()
    {
        var request = MinimalCubeBuilder(noDoubleEquity: 0.25, doubleTakeEquity: -0.125).Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        // Positive — explicit + sign.
        Assert.Contains(">+0.250<", svg);
        // Negative — intrinsic minus sign.
        Assert.Contains(">-0.125<", svg);
    }

    [Fact]
    public void CubePanel_EquityFormat_AlwaysUsesInvariantDecimalSeparator()
    {
        // Switch the thread culture to one that formats doubles with a comma
        // (e.g. fr-FR). Invariant-culture formatting in the renderer must be
        // unaffected — the SVG should still contain "0.250", not "0,250".
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var request = MinimalCubeBuilder(0.25, 0.10).Build();
            var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

            Assert.Contains(">+0.250<", svg);
            Assert.DoesNotContain(">+0,250<", svg);
        }
        finally
        {
            CultureInfo.CurrentCulture = prior;
        }
    }

    [Fact]
    public void CubePanel_OpponentErrorPercent_UsesOneDecimal()
    {
        var b = MinimalCubeBuilder(0.4, 0.8);
        b.ProbOfOpponentErrorJustifyingDouble = 0.42; // → "42.0%"
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Contains(">42.0%<", svg);
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
