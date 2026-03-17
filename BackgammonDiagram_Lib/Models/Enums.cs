namespace BackgammonDiagram_Lib;

public enum DiagramMode
{
    Problem,
    Solution
}

public enum DiagramOrientation
{
    /// <summary>On-roll player's home board is on the right.</summary>
    OnRollRight,
    /// <summary>Opponent's home board is on the right.</summary>
    OpponentRight
}

public enum PanelPosition
{
    Left,
    Right
}

public enum CubeOwner
{
    OnRoll,
    Opponent,
    Centered
}

public enum DiagramSizePreset
{
    Small,
    Medium,
    Large,
    Custom
}
