namespace BackgammonDiagram_Lib;

public class DiagramOptions
{
    public bool ShowPipCount { get; init; }
    public DiagramSize Size { get; init; } = DiagramSize.Medium;

    /// <summary>
    /// Optional watermark text. Rendered twice — once centered in each board half —
    /// at low opacity.
    /// </summary>
    public string? WatermarkText { get; init; }

    /// <summary>
    /// Optional theme name. Resolved via ThemeRegistry.Resolve().
    /// Null or unrecognised values fall back to DefaultTheme.
    /// </summary>
    public string? ThemeName { get; init; }
}
