namespace BackgammonDiagram_Lib;

public class DiagramSize
{
    public DiagramSizePreset Preset { get; init; }

    /// <summary>Width in pixels. Only used when Preset == Custom.</summary>
    public int? CustomWidth { get; init; }

    /// <summary>Height in pixels. Only used when Preset == Custom.</summary>
    public int? CustomHeight { get; init; }

    public static readonly DiagramSize Small = new() { Preset = DiagramSizePreset.Small };
    public static readonly DiagramSize Medium = new() { Preset = DiagramSizePreset.Medium };
    public static readonly DiagramSize Large = new() { Preset = DiagramSizePreset.Large };

    public static DiagramSize Custom(int width, int height) =>
        new() { Preset = DiagramSizePreset.Custom, CustomWidth = width, CustomHeight = height };
}
