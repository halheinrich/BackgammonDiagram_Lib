using BackgammonDiagram_Lib.Themes;

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
    /// Theme used for rendering. Defaults to ThemeRegistry.Default.
    /// </summary>
    public ITheme Theme { get; init; } = ThemeRegistry.Default;
}
