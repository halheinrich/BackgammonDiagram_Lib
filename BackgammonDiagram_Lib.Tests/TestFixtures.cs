// TestFixtures.cs  (new file)
using BackgammonDiagram_Lib.Themes;

namespace BackgammonDiagram_Lib.Tests;

internal static class TestFixtures
{
    /// <summary>
    /// A minimal valid checker-play request. All non-defaulted fields set explicitly.
    /// IsCube=false so Dice must be 1–6.
    /// </summary>
    public static DiagramRequest.Builder MinimalBuilder() => new()
    {
        OnRollName = "Hal",
        OpponentName = "Opponent",
        OnRollPipCount = 133,
        OpponentPipCount = 131,
        CubeSize = 2,
        CubeOwner = CubeOwner.Centered,
        OnRollAtBottom = true,
        Mode = DiagramMode.Problem,
        IsCube = false,
        Dice = [3, 1],
        Mop = new int[26],
    };

    public static DiagramRequest MinimalRequest() => MinimalBuilder().Build();

    public static DiagramOptions DefaultOptions() => new()
    {
        Size = DiagramSize.Medium
    };

    public static DiagramOptions GreyscaleOptions() => new()
    {
        Size = DiagramSize.Medium,
        Theme = ThemeRegistry.Greyscale
    };
}