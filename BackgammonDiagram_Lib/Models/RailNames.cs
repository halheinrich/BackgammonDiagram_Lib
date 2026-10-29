namespace BackgammonDiagram_Lib;

/// <summary>
/// The two names a decision's diagram shows on its rails in place of the
/// record's (<see cref="DiagramRequest.RailNames"/>): the on-roll side's and
/// the opponent's. Each rail still reads its name with the side's score.
/// </summary>
/// <remarks>
/// A caller's wording, not a record fact: the record is never altered, and
/// this library holds no wording of its own for them. Both names are stated
/// and neither is blank. Value equality: equal names, compared ordinally. A
/// class with get-only members, so that no instance escapes the
/// constructor's check.
/// </remarks>
public sealed record RailNames
{
    /// <summary>Creates the pair of names shown, the on-roll side's first.</summary>
    /// <param name="onRollName">The name on the on-roll side's rail.</param>
    /// <param name="opponentName">The name on the opponent's rail.</param>
    /// <exception cref="ArgumentNullException">Either name is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Either name is empty or white space.</exception>
    public RailNames(string onRollName, string opponentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(onRollName);
        ArgumentException.ThrowIfNullOrWhiteSpace(opponentName);
        OnRollName = onRollName;
        OpponentName = opponentName;
    }

    /// <summary>
    /// The name on the rail of the side whose checkers are positive in the
    /// board's frame — the side <see cref="BgDataTypes_Lib.BoardPosition"/>
    /// calls the player on roll.
    /// </summary>
    public string OnRollName { get; }

    /// <summary>The name on the other side's rail.</summary>
    public string OpponentName { get; }
}
