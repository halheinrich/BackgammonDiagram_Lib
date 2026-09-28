using System.Globalization;
using System.Numerics;

namespace BackgammonDiagram_Lib;

/// <summary>
/// The score a board's rails show beside the names — a display fact
/// (<see cref="DisplayFacts.Score"/>). One of two kinds: each side's away
/// score in a match, with the match's Crawford status
/// (<see cref="MatchRailScore"/>), or the money-game label with its Jacoby
/// rule (<see cref="MoneyRailScore"/>). The diagram words it exactly as it
/// words a decision's — <c>"{name} needs 3 Crawford"</c>, <c>"{name} (Money
/// Game, Jacoby)"</c> — and draws the cube's face from it the same way, so a
/// caller states the facts and never the text.
/// </summary>
/// <remarks>
/// <para>
/// <b>The facts a board's score presentation needs, and no more</b> (Hal's
/// ruling of 2026-09-28 on halheinrich/backgammon#273): the away scores and
/// the Crawford status for a match, the Jacoby rule for money. The diagram
/// consumes them and neither derives nor validates their legality — a stated
/// Crawford status draws whatever the away scores beside it, and a match won
/// 0-away shows <c>needs 0</c>. Double match point is not stated: the
/// diagram words it from the away scores, by the rule a decision's session
/// goes through. Nothing else of a session — a match's length, a money
/// session's scores, its beaver rule or cube limit — is here, and nothing of
/// a decision: <c>"Cube Action?"</c> is a decision's diagram's alone.
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

    /// <summary>Whether two scores are equal: the same kind, with equal facts.</summary>
    public static bool operator ==(RailScore? left, RailScore? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two scores differ.</summary>
    public static bool operator !=(RailScore? left, RailScore? right) => !(left == right);
}

/// <summary>
/// A match score on the rails: what each side still needs, drawn
/// <c>"{name} needs {n}"</c> on that side's rail, with <c>" Crawford"</c> on
/// both in the Crawford game — and the cube's face that follows from it
/// (<c>Dmp</c> at double match point, <c>Cr</c> in the Crawford game).
/// </summary>
public sealed class MatchRailScore : RailScore
{
    /// <summary>
    /// Creates the score. Every fact is required and shown as given: a caller
    /// adapting a source states the Crawford status, so one left out is a
    /// build error rather than a status silently not drawn.
    /// </summary>
    /// <param name="onRollNeeds">What the side whose checkers are positive in the board's frame needs.</param>
    /// <param name="opponentNeeds">What the other side needs.</param>
    /// <param name="isCrawford">Whether the game shown is the Crawford game.</param>
    public MatchRailScore(int onRollNeeds, int opponentNeeds, bool isCrawford)
    {
        OnRollNeeds = onRollNeeds;
        OpponentNeeds = opponentNeeds;
        IsCrawford = isCrawford;
    }

    /// <summary>
    /// What the side whose checkers are positive in the board's frame needs —
    /// the side <see cref="BgDataTypes_Lib.BoardPosition"/> calls the player
    /// on roll.
    /// </summary>
    public int OnRollNeeds { get; }

    /// <summary>What the other side needs.</summary>
    public int OpponentNeeds { get; }

    /// <summary>
    /// Whether the game shown is the Crawford game: the rails say so and the
    /// cube reads <c>Cr</c>, as a decision's do. Drawn as stated, whatever the
    /// away scores; whether it could be the Crawford game is the domain's
    /// question.
    /// </summary>
    public bool IsCrawford { get; }

    /// <inheritdoc/>
    public override bool Equals(RailScore? other) =>
        other is MatchRailScore match && match.OnRollNeeds == OnRollNeeds
        && match.OpponentNeeds == OpponentNeeds && match.IsCrawford == IsCrawford;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(OnRollNeeds, OpponentNeeds, IsCrawford);

    /// <summary>The score, for a test's failure message: <c>"needs 3 / 1 Crawford"</c>.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"needs {OnRollNeeds} / {OpponentNeeds}{(IsCrawford ? " Crawford" : string.Empty)}");
}

/// <summary>
/// The money-game label on the rails, drawn on both: <c>"(Money Game,
/// Jacoby)"</c> or <c>"(Money Game, No Jacoby)"</c> with the Jacoby rule
/// stated, and the bare <c>"(Money Game)"</c> with none.
/// </summary>
public sealed class MoneyRailScore : RailScore
{
    /// <summary>
    /// Creates the label. The Jacoby rule is required: a caller states it, or
    /// states <see langword="null"/> for a source that states none, so one
    /// left out is a build error rather than a rule silently not drawn.
    /// </summary>
    /// <param name="isJacoby">The Jacoby rule, or <see langword="null"/> where the board's source states none.</param>
    public MoneyRailScore(bool? isJacoby)
    {
        IsJacoby = isJacoby;
    }

    /// <summary>
    /// Whether the Jacoby rule is in play, or <see langword="null"/> where the
    /// board's source states none — the label is then the bare
    /// <c>"(Money Game)"</c>.
    /// </summary>
    public bool? IsJacoby { get; }

    /// <inheritdoc/>
    public override bool Equals(RailScore? other) => other is MoneyRailScore money && money.IsJacoby == IsJacoby;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(typeof(MoneyRailScore), IsJacoby);

    /// <summary>The label, for a test's failure message: <c>"money, Jacoby"</c>.</summary>
    public override string ToString() => IsJacoby switch
    {
        true => "money, Jacoby",
        false => "money, no Jacoby",
        null => "money",
    };
}
