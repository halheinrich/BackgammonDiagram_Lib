using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

/// <summary>
/// Single source of truth for the user-facing spelling of a cube answer —
/// the doubler's <see cref="CubeClaim"/>, the taker's
/// <see cref="CubeAction"/> response, and the complete
/// <see cref="CubeClaimPair"/>. Every surface that names a cube answer reads
/// its wording here: this library's own cube panel, and the consuming apps
/// (halheinrich/backgammon#185).
/// </summary>
/// <remarks>
/// <para>
/// One case throughout — sentence case: <c>No double</c>, <c>Double</c>,
/// <c>Too good</c>, <c>Take</c>, <c>Pass</c> (ruled 2026-09-02,
/// halheinrich/backgammon#185). Presentation only: the claim/action model
/// and the derivation of a position's verdict belong to
/// <c>BgDataTypes_Lib</c>, and nothing here re-derives them.
/// </para>
/// <para>
/// Every member is exhaustive over its type and throws
/// <see cref="ArgumentOutOfRangeException"/> on a value outside it. There is
/// no display fallback: an unlabelled value is a programming error, and
/// rendering it as a placeholder would ship the error to the reader.
/// </para>
/// </remarks>
public static class CubeLabels
{
    /// <summary>Joins the two halves of a pair that does not read as its
    /// claim alone. Spaced, as ruled — <c>"Double / Take"</c>.</summary>
    private const string PairSeparator = " / ";

    /// <summary>
    /// The user-facing spelling of a doubler's claim: <c>No double</c>,
    /// <c>Double</c>, <c>Too good</c>.
    /// </summary>
    /// <param name="claim">The claim to label.</param>
    /// <returns>The sentence-case label for <paramref name="claim"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="claim"/> is not a defined <see cref="CubeClaim"/>
    /// member.
    /// </exception>
    public static string Label(CubeClaim claim) => claim switch
    {
        CubeClaim.NoDouble => "No double",
        CubeClaim.Double   => "Double",
        CubeClaim.TooGood  => "Too good",
        _ => throw new ArgumentOutOfRangeException(nameof(claim), claim,
            "CubeLabels.Label requires a defined CubeClaim member.")
    };

    /// <summary>
    /// The user-facing spelling of a cube action: <c>No double</c>,
    /// <c>Double</c>, <c>Take</c>, <c>Pass</c>. Covers all four actions, not
    /// just the taker half, because the cube panel's equity/loss table lists
    /// the doubler's two options and the taker's two side by side.
    /// </summary>
    /// <param name="action">The action to label.</param>
    /// <returns>The sentence-case label for <paramref name="action"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="action"/> is not a defined <see cref="CubeAction"/>
    /// member.
    /// </exception>
    public static string Label(CubeAction action) => action switch
    {
        CubeAction.NoDouble => "No double",
        CubeAction.Double   => "Double",
        CubeAction.Take     => "Take",
        CubeAction.Pass     => "Pass",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action,
            "CubeLabels.Label requires a defined CubeAction member.")
    };

    /// <summary>
    /// The user-facing spelling of a complete cube answer, in two clauses.
    /// A pair reads as its claim alone when that claim has exactly one
    /// reachable pair, and otherwise as claim and response joined by
    /// <c>" / "</c> — so the four reachable verdicts read <c>No double</c>,
    /// <c>Double / Take</c>, <c>Double / Pass</c>, <c>Too good</c> (ruled
    /// 2026-09-02, halheinrich/backgammon#185). And the incoherent cell
    /// <see cref="CubeClaimPair.NoDoublePass"/> reads <c>Too good</c>, the
    /// posture whose degenerate point SPEC-scoring §3's sixth-cell ruling
    /// says it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first clause is a compression, not an omission: where the claim
    /// admits only one response, naming that response adds nothing a reader
    /// could not supply.
    /// </para>
    /// <para>
    /// The second clause is why this method is <strong>not injective</strong>
    /// over <see cref="CubeClaimPair"/>, by design:
    /// <see cref="CubeClaimPair.NoDoublePass"/> and
    /// <see cref="CubeClaimPair.TooGoodPass"/> share the label
    /// <c>Too good</c>. §3 buckets the sixth cell with Too good / Pass rather
    /// than giving it a bucket of its own — it is the too-good posture's
    /// degenerate point, derivable only at the exact tie boundary
    /// (<c>NoDoubleEquity == 1</c> with <c>DoubleTakeEquity &gt;= 1</c>,
    /// where the two halves' ruled tie-breaks compose it) — and a banner must
    /// not print a verdict the model itself calls incoherent. Callers that
    /// need to tell the two apart have the pair, which is not lossy; only its
    /// spelling is.
    /// </para>
    /// <para>
    /// <see cref="CubeClaimPair"/> is a closed 3×2 and this function stays
    /// total over it. That leaves
    /// <see cref="CubeClaimPair.TooGoodTake"/> as the one cell taking the
    /// joined form on its own account (<c>Too good / Take</c>): unreachable
    /// as a verdict since Too good came to require the pass (SPEC-scoring §3,
    /// amended 2026-09-02), it arrives here only from a stored or submitted
    /// answer, and its response is exactly what its claim does <em>not</em>
    /// imply — that contradiction is the whole of what the pair says, so
    /// spelling it in full is the honest reading.
    /// </para>
    /// </remarks>
    /// <param name="pair">The cube answer to label.</param>
    /// <returns>The sentence-case label for <paramref name="pair"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="pair"/> carries a half outside its own domain — which
    /// a constructed <see cref="CubeClaimPair"/> cannot, but
    /// <c>default(CubeClaimPair)</c> can: the <see langword="struct"/>
    /// default escapes the type's half-guards and its
    /// <see cref="CubeClaimPair.Taker"/> is <see cref="CubeAction.NoDouble"/>,
    /// not a taker response. Rejected rather than labelled, per this type's
    /// no-fallback rule.
    /// </exception>
    public static string Label(CubeClaimPair pair)
    {
        if (pair.Taker is not (CubeAction.Take or CubeAction.Pass))
            throw new ArgumentOutOfRangeException(nameof(pair), pair,
                "CubeLabels.Label requires a CubeClaimPair whose Taker is a "
                + "taker-half action (Take or Pass).");

        // The sixth cell reads as the posture it degenerates from, ahead of
        // the reachability rule below: SPEC-scoring §3 buckets (No double,
        // Pass) with Too good / Pass rather than giving it a bucket of its
        // own. Named through CubeClaimPair's own IsIncoherent so the cell is
        // identified where it is defined, not re-spelled here.
        if (pair.IsIncoherent)
            return Label(CubeClaim.TooGood);

        string claim = Label(pair.Claim);
        return ReadsAsClaimAlone(pair) ? claim : claim + PairSeparator + Label(pair.Taker);
    }

    /// <summary>
    /// Whether <paramref name="pair"/> is the one reachable pair of its
    /// claim, and so reads as that claim alone. The reachable verdicts are
    /// the four coherent pairs of SPEC-scoring §3 (amended 2026-09-02):
    /// <see cref="CubeClaim.NoDouble"/> reaches only
    /// <see cref="CubeAction.Take"/> and <see cref="CubeClaim.TooGood"/> only
    /// <see cref="CubeAction.Pass"/>, while <see cref="CubeClaim.Double"/>
    /// reaches both and so is always joined. Pure reachability: the
    /// incoherent cell is answered by the caller's earlier clause and never
    /// arrives here, so this stays the one statement of which pairs an
    /// analysis can derive.
    /// </summary>
    private static bool ReadsAsClaimAlone(CubeClaimPair pair) => pair.Claim switch
    {
        CubeClaim.NoDouble => pair.Taker is CubeAction.Take,
        CubeClaim.TooGood  => pair.Taker is CubeAction.Pass,
        CubeClaim.Double   => false,
        _ => throw new ArgumentOutOfRangeException(nameof(pair), pair,
            "CubeLabels.Label requires a defined CubeClaim member.")
    };
}
