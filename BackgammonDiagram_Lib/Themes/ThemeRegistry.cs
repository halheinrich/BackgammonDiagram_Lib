// ThemeRegistry.cs  (full replacement — static instances, Resolve removed)
namespace BackgammonDiagram_Lib.Themes;

public static class ThemeRegistry
{
    public static ITheme Default { get; } = new DefaultTheme();
    public static ITheme Greyscale { get; } = new GreyscaleTheme();
}