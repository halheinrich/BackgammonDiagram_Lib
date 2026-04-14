using BgDataTypes_Lib;
// TestFixtures.cs  (new file)
using BackgammonDiagram_Lib.Rendering;
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
    public static string Render(DiagramRequest? req = null, DiagramOptions? opts = null)
        => DiagramRenderer.RenderSvg(req ?? MinimalRequest(), opts ?? DefaultOptions());

    public static int[] StartingMop()
    {
        var mop = new int[26];
        mop[24] = 2; mop[13] = 5; mop[8] = 3; mop[6] = 5;
        mop[1] = -2; mop[12] = -5; mop[17] = -3; mop[19] = -5;
        return mop;
    }

    public static int CountOccurrences(string source, string pattern)
    {
        int count = 0, index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        { count++; index += pattern.Length; }
        return count;
    }
}