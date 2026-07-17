namespace BackgammonDiagram_Lib;

/// <summary>
/// The target size of a rendered diagram: either a named preset or a custom
/// width. Width is the only degree of freedom — height always follows from it
/// by the fixed board aspect ratio.
/// </summary>
public class DiagramSize
{
    /// <summary>Which size class this instance selects.</summary>
    public DiagramSizePreset Preset { get; init; }

    /// <summary>
    /// Target width in pixels; only consulted when <see cref="Preset"/> is
    /// <see cref="DiagramSizePreset.Custom"/>. Width is the sole degree of
    /// freedom — the rendered height derives from it by the board's fixed
    /// aspect ratio, so there is no companion height property.
    /// </summary>
    public int? CustomWidth { get; init; }

    /// <summary>The small preset.</summary>
    public static readonly DiagramSize Small = new() { Preset = DiagramSizePreset.Small };
    /// <summary>The medium preset (the default size).</summary>
    public static readonly DiagramSize Medium = new() { Preset = DiagramSizePreset.Medium };
    /// <summary>The large preset.</summary>
    public static readonly DiagramSize Large = new() { Preset = DiagramSizePreset.Large };

    /// <summary>
    /// A custom-sized diagram of the given pixel <paramref name="width"/>.
    /// Height is not a parameter: it follows from the width by the fixed
    /// board aspect ratio.
    /// </summary>
    public static DiagramSize Custom(int width) =>
        new() { Preset = DiagramSizePreset.Custom, CustomWidth = width };
}
