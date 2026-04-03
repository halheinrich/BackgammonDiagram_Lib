using BackgammonDiagram_Lib.Themes;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

public class ColourSchemeTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    /// Render with checkers on both sides so all fill colours appear.
    private static string RenderWithCheckers(ITheme theme)
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        return TestFixtures.Render(b.Build(), new DiagramOptions { Theme = theme });
    }

    private static void AssertColour(string svg, string colour, string propertyName)
        => Assert.True(svg.Contains(colour, StringComparison.OrdinalIgnoreCase),
            $"Expected colour {colour} ({propertyName}) not found in SVG.");

    // ── DefaultTheme ─────────────────────────────────────────────────────────

    [Fact]
    public void DefaultTheme_BoardColor_PresentInSvg()
    {
        var theme = ThemeRegistry.Default;
        AssertColour(RenderWithCheckers(theme), theme.BoardColor, nameof(theme.BoardColor));
    }

    [Fact]
    public void DefaultTheme_PointColorDark_PresentInSvg()
    {
        var theme = ThemeRegistry.Default;
        AssertColour(RenderWithCheckers(theme), theme.PointColorDark, nameof(theme.PointColorDark));
    }

    [Fact]
    public void DefaultTheme_PointColorLight_PresentInSvg()
    {
        var theme = ThemeRegistry.Default;
        AssertColour(RenderWithCheckers(theme), theme.PointColorLight, nameof(theme.PointColorLight));
    }

    [Fact]
    public void DefaultTheme_CheckerColorOnRoll_PresentInSvg()
    {
        var theme = ThemeRegistry.Default;
        AssertColour(RenderWithCheckers(theme), theme.CheckerColorOnRoll, nameof(theme.CheckerColorOnRoll));
    }

    [Fact]
    public void DefaultTheme_CheckerColorOpponent_PresentInSvg()
    {
        var theme = ThemeRegistry.Default;
        AssertColour(RenderWithCheckers(theme), theme.CheckerColorOpponent, nameof(theme.CheckerColorOpponent));
    }

    // ── GreyscaleTheme ───────────────────────────────────────────────────────

    [Fact]
    public void GreyscaleTheme_BoardColor_PresentInSvg()
    {
        var theme = ThemeRegistry.Greyscale;
        AssertColour(RenderWithCheckers(theme), theme.BoardColor, nameof(theme.BoardColor));
    }

    [Fact]
    public void GreyscaleTheme_PointColorDark_PresentInSvg()
    {
        var theme = ThemeRegistry.Greyscale;
        AssertColour(RenderWithCheckers(theme), theme.PointColorDark, nameof(theme.PointColorDark));
    }

    [Fact]
    public void GreyscaleTheme_PointColorLight_PresentInSvg()
    {
        var theme = ThemeRegistry.Greyscale;
        AssertColour(RenderWithCheckers(theme), theme.PointColorLight, nameof(theme.PointColorLight));
    }

    [Fact]
    public void GreyscaleTheme_CheckerColorOnRoll_PresentInSvg()
    {
        var theme = ThemeRegistry.Greyscale;
        AssertColour(RenderWithCheckers(theme), theme.CheckerColorOnRoll, nameof(theme.CheckerColorOnRoll));
    }

    [Fact]
    public void GreyscaleTheme_CheckerColorOpponent_PresentInSvg()
    {
        var theme = ThemeRegistry.Greyscale;
        AssertColour(RenderWithCheckers(theme), theme.CheckerColorOpponent, nameof(theme.CheckerColorOpponent));
    }

    // ── CustomTheme — construction validation ────────────────────────────────

    [Fact]
    public void CustomTheme_InvalidHex_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CustomTheme(
            boardColor: "notacolour",
            pointColorDark: "#404040",
            pointColorLight: "#F0F0F0",
            checkerColorOnRoll: "#111111",
            checkerColorOpponent: "#EEEEEE",
            diceColor: "#FFFFFF",
            textColor: "#000000"));
    }

    [Fact]
    public void CustomTheme_ShortHex_IsAccepted()
    {
        // #RGB shorthand must be valid
        var theme = new CustomTheme(
            boardColor: "#ABC",
            pointColorDark: "#123",
            pointColorLight: "#DEF",
            checkerColorOnRoll: "#000",
            checkerColorOpponent: "#FFF",
            diceColor: "#FFF",
            textColor: "#000");
        Assert.Equal("Custom", theme.Name);
    }

    // ── CustomTheme — colours appear in SVG output ───────────────────────────

    [Fact]
    public void CustomTheme_AllColours_PresentInSvg()
    {
        var theme = new CustomTheme(
            boardColor: "#2C3E50",
            pointColorDark: "#E74C3C",
            pointColorLight: "#ECF0F1",
            checkerColorOnRoll: "#1ABC9C",
            checkerColorOpponent: "#F39C12",
            diceColor: "#FFFFFF",
            textColor: "#000000",
            name: "TestPalette");

        var svg = RenderWithCheckers(theme);

        AssertColour(svg, theme.BoardColor, nameof(theme.BoardColor));
        AssertColour(svg, theme.PointColorDark, nameof(theme.PointColorDark));
        AssertColour(svg, theme.PointColorLight, nameof(theme.PointColorLight));
        AssertColour(svg, theme.CheckerColorOnRoll, nameof(theme.CheckerColorOnRoll));
        AssertColour(svg, theme.CheckerColorOpponent, nameof(theme.CheckerColorOpponent));
    }
    [Fact]
    public void CustomTheme_RendersToFile()
    {
        var theme = new CustomTheme(
            boardColor: "#2C3E50",
            pointColorDark: "#E74C3C",
            pointColorLight: "#ECF0F1",
            checkerColorOnRoll: "#1ABC9C",
            checkerColorOpponent: "#F39C12",
            diceColor: "#FFFFFF",
            textColor: "#000000",
            name: "TestPalette");

        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        var svg = TestFixtures.Render(b.Build(), new DiagramOptions { Theme = theme });

        File.WriteAllText(TestPaths.SvgOutputPath("custom_theme.svg"), svg);
    }
}