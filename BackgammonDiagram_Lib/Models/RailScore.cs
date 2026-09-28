using System.Globalization;
using System.Numerics;

namespace BackgammonDiagram_Lib;

/// <summary>
/// The score a board's rails show beside the names — a display fact
/// (<see cref="DisplayFacts.Score"/>). One of two kinds: each side's away
/// score in a match (<see cref="MatchRailScore"/>), or the money-game label
/// (<see cref="MoneyRailScore"/>). The diagram words it — <c>"{name} needs
/// 3"</c>, <c>"{name} (Money Game)"</c> — so a caller states the numbers and
/// never the text.
/// </summary>
/// <remarks>
/// <para>
/// What the rails print, not a session: nothing here is a match's length, a
/// standing, a Crawford game or a money session's rules, and the diagram
/// holds the numbers to no rule of the game — a match won 0-away shows
/// <c>needs 0</c>. A decision's rails are worded from its record's own
/// session instead (<see cref="DiagramRequest.ForDecision"/>).
/// </para>
/// <para>
/// A closed pair: the constructor is not reachable outside this library, so
/// these two kinds are the only ones. Value equality within a kind, as the
/// session kinds it stands beside have.
/// </para>
/// </remarks>
public abstract class RailScore : IEquatable<RailScore>, IEqualityOperators<RailScore, RailScore, bool>
{
    /// <summary>Reachable from the two kinds in this library only.</summary>
    private protected RailScore()
    {
    }

    /// <inheritdoc/>
    public abstract bool Equals(RailScore? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is RailScore other && Equals(other);

    /// <inheritdoc/>
    public abstract override int GetHashCode();

    /// <summary>Whether two scores are equal: the same kind, with equal numbers.</summary>
    public static bool operator ==(RailScore? left, RailScore? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two scores differ.</summary>
    public static bool operator !=(RailScore? left, RailScore? right) => !(left == right);
}

/// <summary>
/// A match score on the rails: what each side still needs, drawn
/// <c>"{name} needs {n}"</c> on that side's rail.
/// </summary>
public sealed class MatchRailScore : RailScore
{
    /// <summary>Creates the score. The numbers are shown as given.</summary>
    /// <param name="onRollNeeds">What the side whose checkers are positive in the board's frame needs.</param>
    /// <param name="opponentNeeds">What the other side needs.</param>
    public MatchRailScore(int onRollNeeds, int opponentNeeds)
    {
        OnRollNeeds = onRollNeeds;
        OpponentNeeds = opponentNeeds;
    }

    /// <summary>
    /// What the side whose checkers are positive in the board's frame needs —
    /// the side <see cref="BgDataTypes_Lib.BoardPosition"/> calls the player
    /// on roll.
    /// </summary>
    public int OnRollNeeds { get; }

    /// <summary>What the other side needs.</summary>
    public int OpponentNeeds { get; }

    /// <inheritdoc/>
    public override bool Equals(RailScore? other) =>
        other is MatchRailScore match && match.OnRollNeeds == OnRollNeeds && match.OpponentNeeds == OpponentNeeds;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(OnRollNeeds, OpponentNeeds);

    /// <summary>The score, for a test's failure message: <c>"needs 3 / 5"</c>.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"needs {OnRollNeeds} / {OpponentNeeds}");
}

/// <summary>
/// The money-game label on the rails, drawn <c>"{name} (Money Game)"</c> on
/// both rails. It carries nothing else: a money session's rules are a
/// session's facts, and a board's display facts restate none.
/// </summary>
public sealed class MoneyRailScore : RailScore
{
    /// <summary>Creates the label. Every instance is equal to every other.</summary>
    public MoneyRailScore()
    {
    }

    /// <inheritdoc/>
    public override bool Equals(RailScore? other) => other is MoneyRailScore;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(MoneyRailScore).GetHashCode();

    /// <summary>The label, for a test's failure message: <c>"money"</c>.</summary>
    public override string ToString() => "money";
}
