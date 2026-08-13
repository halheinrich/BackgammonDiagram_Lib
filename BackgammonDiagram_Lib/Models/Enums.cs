namespace BackgammonDiagram_Lib;

/// <summary>What a diagram's panel shows: blank (the problem to solve) or the
/// filled analysis (the solution under review).</summary>
public enum DiagramMode
{
    /// <summary>Problem view — the analysis panel carries no content. Under
    /// the panel-bearing canvas presets the panel region is still allocated
    /// (blank), so dimensions match <see cref="Solution"/>; under
    /// <see cref="AspectPreset.BoardOnly"/> the allocation is dropped and the
    /// canvas is the board proper plus its title strip.</summary>
    Problem,
    /// <summary>Solution view — board plus the filled analysis panel. Never
    /// board-only: this mode exists to show the panel, so
    /// <see cref="AspectPreset.BoardOnly"/> is rejected for Solution
    /// requests.</summary>
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