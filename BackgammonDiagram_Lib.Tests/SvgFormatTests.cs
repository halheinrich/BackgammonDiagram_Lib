using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.Tests;

// SvgFormat.Number and SvgViewBox.ToAttributeString are the public SSOT for
// culture-invariant SVG attribute formatting. These tests pin the formatting
// contract (invariant separator, "0.##" rounding/trimming, finite-only) and
// the round-trip guarantee against RenderSvg's emitted viewBox.
public class SvgFormatTests
{
    // -----------------------------------------------------------------------
    //  SvgFormat.Number — formatting contract
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(30.0, "30")]        // integer value — no decimal point
    [InlineData(0.0, "0")]
    [InlineData(30.8, "30.8")]      // the production-bug value (bar width)
    [InlineData(-0.5, "-0.5")]
    [InlineData(14.0, "14")]        // trailing zeros trimmed
    [InlineData(2.25, "2.25")]
    [InlineData(30.833, "30.83")]   // rounded to at most two decimals
    [InlineData(2.999, "3")]        // rounding can collapse to an integer
    [InlineData(-12.346, "-12.35")]
    public void Number_FormatsInvariantWithAtMostTwoDecimals(double value, string expected)
    {
        Assert.Equal(expected, SvgFormat.Number(value));
    }

    [Fact]
    public void Number_NegativeZero_KeepsSign()
    {
        // "-0" is a valid SVG number, so negative zero needs no guard — this
        // test just documents the behavior.
        Assert.Equal("-0", SvgFormat.Number(-0.0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Number_NonFinite_Throws(double value)
    {
        // NaN would format as "NaN" and infinities as "∞" — the exact
        // invalid-SVG-attribute class this API exists to prevent.
        Assert.Throws<ArgumentOutOfRangeException>(() => SvgFormat.Number(value));
    }

    [Fact]
    public void Number_UsesInvariantSeparatorUnderCommaDecimalCulture()
    {
        RunUnderCulture("nb-NO", () =>
        {
            // Sanity: the hostile culture really does use a comma...
            Assert.Equal("30,8", 30.8.ToString("0.##"));
            // ...and SvgFormat is unaffected by it.
            Assert.Equal("30.8", SvgFormat.Number(30.8));
            Assert.Equal("-12.35", SvgFormat.Number(-12.346));
        });
    }

    // -----------------------------------------------------------------------
    //  SvgViewBox.ToAttributeString
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0.0, 700.0, 500.0, "0 0 700 500")]
    [InlineData(0.0, 0.0, 620.5, 411.25, "0 0 620.5 411.25")]
    [InlineData(-10.0, -20.5, 100.0, 50.0, "-10 -20.5 100 50")]
    public void ToAttributeString_FormatsAllFourComponents(
        double x, double y, double w, double h, string expected)
    {
        Assert.Equal(expected, new SvgViewBox(x, y, w, h).ToAttributeString());
    }

    [Fact]
    public void ToAttributeString_UsesInvariantSeparatorUnderCommaDecimalCulture()
    {
        RunUnderCulture("nb-NO", () =>
            Assert.Equal("0 0 620.5 411.25",
                new SvgViewBox(0, 0, 620.5, 411.25).ToAttributeString()));
    }

    [Fact]
    public void ToAttributeString_NonFiniteComponent_Throws()
    {
        var viewBox = new SvgViewBox(0, 0, double.NaN, 500);
        Assert.Throws<ArgumentOutOfRangeException>(() => viewBox.ToAttributeString());
    }

    // -----------------------------------------------------------------------
    //  Round-trip against RenderSvg
    // -----------------------------------------------------------------------

    [Fact]
    public void ToAttributeString_MatchesRenderSvgViewBoxForSameRequest()
    {
        // Pins the documented contract: GetHitRegions().ViewBox formatted via
        // ToAttributeString is exactly the viewBox RenderSvg emits, so a
        // consumer overlay built from hit regions aligns with the drawn board.
        var request = TestFixtures.MinimalBuilder().Build();
        var options = new DiagramOptions();

        var svg = DiagramRenderer.RenderSvg(request, options);
        var regions = DiagramRenderer.GetHitRegions(request, options);

        var match = Regex.Match(svg, "viewBox=\"([^\"]*)\"");
        Assert.True(match.Success, "RenderSvg output has no viewBox attribute.");
        Assert.Equal(match.Groups[1].Value, regions.ViewBox.ToAttributeString());
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    // Runs the assertion under a hostile comma-decimal culture, restoring the
    // prior cultures afterwards. CurrentCulture is what ToString actually
    // reads on the executing thread; DefaultThreadCurrentCulture is set too
    // so any thread spawned inside the action inherits the hostile culture.
    private static void RunUnderCulture(string cultureName, Action assertion)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var priorCurrent = CultureInfo.CurrentCulture;
        var priorDefault = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            assertion();
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCurrent;
            CultureInfo.DefaultThreadCurrentCulture = priorDefault;
        }
    }
}
