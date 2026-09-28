namespace BackgammonDiagram_Lib;

/// <summary>
/// The two dice a board's diagram shows, in the order it shows them, left to
/// right — a display fact (<see cref="DisplayFacts.Dice"/>). The order is the
/// diagram's to draw, so unlike <see cref="BgDataTypes_Lib.DiceRoll"/>, which
/// is a roll in canonical form, the faces keep the order they are given in.
/// </summary>
/// <remarks>
/// Each face is 1 to 6, because those are the faces a die is drawn with; the
/// diagram checks that and nothing else. Whether a pair is a legal roll for
/// anything is not its question. Value equality: equal faces in the same
/// order. A class rather than a struct, so that no instance escapes the
/// constructor's check: a struct's default would be a pair of blank faces.
/// </remarks>
public sealed record DiceFaces
{
    /// <summary>Creates the pair of faces shown, left first.</summary>
    /// <param name="left">The face drawn on the left die, 1 to 6.</param>
    /// <param name="right">The face drawn on the right die, 1 to 6.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either face is outside 1 to 6.</exception>
    public DiceFaces(int left, int right)
    {
        Left = Face(left, nameof(left));
        Right = Face(right, nameof(right));
    }

    /// <summary>The face drawn on the left die, 1 to 6.</summary>
    public int Left { get; }

    /// <summary>The face drawn on the right die, 1 to 6.</summary>
    public int Right { get; }

    private static int Face(int face, string parameter) => face is >= 1 and <= 6
        ? face
        : throw new ArgumentOutOfRangeException(parameter, face, "A die is drawn with a face from 1 to 6.");
}
