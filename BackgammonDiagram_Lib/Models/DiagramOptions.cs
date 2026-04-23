using BackgammonDiagram_Lib.Themes;

namespace BackgammonDiagram_Lib;

public record DiagramOptions
{
    public bool ShowPipCount { get; init; }
    public DiagramSize Size { get; init; } = DiagramSize.Medium;

    /// <summary>
    /// Board watermark image bytes (typically PNG or JPG). Rendered twice —
    /// once bar-adjacent in each half of the board, rotated 90° so both tops
    /// face the bar — at low opacity. Defaults to
    /// <see cref="Watermarks.Default"/> so every rendered diagram carries
    /// the built-in mark; set explicitly to <c>null</c> to opt out, or
    /// assign a caller-supplied byte array to substitute a different image.
    /// </summary>
    public byte[]? WatermarkImage { get; init; } = Watermarks.Default;

    /// <summary>
    /// Theme used for rendering. Defaults to ThemeRegistry.Default.
    /// </summary>
    public ITheme Theme { get; init; } = ThemeRegistry.Default;

    /// <summary>
    /// Target overall aspect ratio (total width / total height) of the
    /// rendered diagram. Board geometry is fixed by CheckerRadius to keep
    /// checkers perfectly round; the analysis panel's width adjusts to hit
    /// the target. Defaults to <see cref="AspectPreset.Widescreen16x9"/> —
    /// the primary use case is slide-show display, where 16:9 fills a
    /// modern projector / PPTX slide edge-to-edge.
    /// </summary>
    public AspectPreset Aspect { get; init; } = AspectPreset.Widescreen16x9;
}

/// <summary>
/// Target aspect ratio for the rendered diagram. Board geometry is fixed;
/// the analysis panel widens to hit the target.
/// </summary>
public enum AspectPreset
{
    /// <summary>BoardLayout's intrinsic panel width (no aspect forcing).</summary>
    Natural,
    /// <summary>16:9 — matches modern widescreen PowerPoint slides.</summary>
    Widescreen16x9,
    /// <summary>4:3 — matches standard (legacy) PowerPoint slides.</summary>
    Standard4x3,
}
