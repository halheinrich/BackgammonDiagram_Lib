// ThemeRegistry.cs  (full replacement — static instances, Resolve removed)
namespace BackgammonDiagram_Lib.Themes;

/// <summary>
/// The built-in themes, exposed as shared <see cref="ITheme"/> instances. The
/// concrete palette types are internal; callers reference the palettes through
/// these properties (or supply their own via <see cref="CustomTheme"/>).
/// </summary>
public static class ThemeRegistry
{
    /// <summary>The default (colour) theme; the default of <see cref="DiagramOptions.Theme"/>.</summary>
    public static ITheme Default { get; } = new DefaultTheme();
    /// <summary>The greyscale theme, for monochrome output.</summary>
    public static ITheme Greyscale { get; } = new GreyscaleTheme();
}