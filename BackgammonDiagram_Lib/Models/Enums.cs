namespace BackgammonDiagram_Lib;

/// <summary>What a diagram renders: the board alone, or the board with its
/// analysis panel.</summary>
public enum DiagramMode
{
    /// <summary>Board only — no analysis panel.</summary>
    Problem,
    /// <summary>Board plus the analysis panel. The panel region is allocated in
    /// both modes, so dimensions match <see cref="Problem"/>.</summary>
    Solution
}

/// <summary>Which side of the board the analysis panel occupies.</summary>
public enum PanelPosition
{
    /// <summary>Panel to the left of the board.</summary>
    Left,
    /// <summary>Panel to the right of the board.</summary>
    Right
}

/// <summary>Named diagram sizes; <see cref="Custom"/> takes an explicit width
/// via <see cref="DiagramSize.Custom(int)"/>.</summary>
public enum DiagramSizePreset
{
    /// <summary>Small preset width.</summary>
    Small,
    /// <summary>Medium preset width (the default).</summary>
    Medium,
    /// <summary>Large preset width.</summary>
    Large,
    /// <summary>Caller-specified width; see <see cref="DiagramSize.CustomWidth"/>.</summary>
    Custom
}