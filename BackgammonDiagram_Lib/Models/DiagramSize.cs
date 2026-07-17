namespace BackgammonDiagram_Lib;

public class DiagramSize
{
    public DiagramSizePreset Preset { get; init; }

    /// <summary>
    /// Target width in pixels; only consulted when <see cref="Preset"/> is
    /// <see cref="DiagramSizePreset.Custom"/>. Width is the sole degree of
    /// freedom — the rendered height derives from it by the board's fixed
    /// aspect ratio, so there is no companion height property.
    /// </summary>
    public int? CustomWidth { get; init; }

    public static readonly DiagramSize Small = new() { Preset = DiagramSizePreset.Small };
    public static readonly DiagramSize Medium = new() { Preset = DiagramSizePreset.Medium };
    public static readonly DiagramSize Large = new() { Preset = DiagramSizePreset.Large };

    /// <summary>
    /// A custom-sized diagram of the given pixel <paramref name="width"/>.
    /// Height is not a parameter: it follows from the width by the fixed
    /// board aspect ratio.
    /// </summary>
    public static DiagramSize Custom(int width) =>
        new() { Preset = DiagramSizePreset.Custom, CustomWidth = width };
}
