namespace BackgammonDiagram_Lib.Themes;

public static class ThemeRegistry
{
    /// <summary>
    /// Resolves a theme name to an ITheme instance.
    /// Unknown or null names fall back to DefaultTheme.
    /// </summary>
    public static ITheme Resolve(string? name) => name switch
    {
        "Greyscale" => new GreyscaleTheme(),
        _ => new DefaultTheme()
    };
}