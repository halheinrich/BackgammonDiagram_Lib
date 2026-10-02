using System.Diagnostics;
using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

/// <summary>
/// Single source of truth for the user-facing wording of a cube answer and of
/// a cube action: the four answers, each in a full and a short form, the
/// answer-type breakdown's four bucket names, and the four board actions.
/// Every surface that names a cube answer reads its
/// wording here: this library's own cube panel, and the consuming apps
/// (halheinrich/backgammon#185).
/// </summary>
/// <remarks>
/// <para>
/// <b>The answers</b> are BgDataTypes_Lib's <see cref="CubeAnswer"/>, one of
/// four (SPEC-scoring §3, amended 2026-09-30 and 2026-10-01 on
/// halheinrich/backgammon#326). Their full labels are <c>No double</c>,
/// <c>Double / Take</c>, <c>Double / Pass</c>, and for the fourth answer
/// <c>Too good</c> or <c>No double / Pass</c>; their short labels, the
/// spelling for a row that cannot fit the full ones (SPEC-quiz-view §4, "The
/// action row under quiz navigation"), are <c>ND</c>, <c>D/T</c>, <c>D/P</c>,
/// and <c>TG</c> or <c>NP</c>.
/// </para>
/// <para>
/// <b>The fourth answer is labelled at its decision.</b> Which of its two
/// labels applies is the decision's reading of it,
/// <see cref="CubeDecision.ClaimOf"/>: Too good where it reads
/// <see cref="CubeClaim.TooGood"/> (gammons are possible), No double / Pass
/// where it reads <see cref="CubeClaim.NoDouble"/>. This type renders that
/// choice and re-checks no rule: whether gammons are possible, and what the
/// answer means, are the decision's. So an answer is labelled only together
/// with the decision it answers; no member labels an answer alone, or from a
/// claim a caller supplies apart from its decision, and no caller can pair an
/// answer with another decision's reading. (A bucket name, below, names a
/// bucket of problems, not an answer.)
/// </para>
/// <para>
/// <b>The actions</b> keep their own labels, <c>No double</c>,
/// <c>Double</c>, <c>Take</c>, <c>Pass</c>, for what is an action and not an
/// answer: the cube panel's equity table, which lists each action, and a
/// played half recorded without the other.
/// </para>
/// <para>
/// <b>The breakdown's bucket names</b>, <see cref="BreakdownBucketLabel"/>,
/// name the answer-type breakdown's bucket for each answer (SPEC-scoring §3,
/// "The tie", its breakdown sub-bullet). A bucket gathers problems, so its
/// name takes no decision: the first three are their answers' full labels,
/// and the fourth is <c>Too good or No double / Pass</c>, the fourth answer
/// under either label (Hal, 2026-10-01). A bucket name names a set, not an
/// answer at a decision, and it is never a way to label one answer without
/// its decision.
/// </para>
/// <para>
/// One case throughout, sentence case (ruled 2026-09-02,
/// halheinrich/backgammon#185). A full label that names a doubling action and
/// a response joins their action labels with <c>" / "</c>, so each word has
/// one spelling. Presentation only: the answer model and every rule over it
/// belong to BgDataTypes_Lib.
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
    /// <summary>Joins a doubling action's label and a response's label in a
    /// full label. Spaced, as ruled: <c>"Double / Take"</c>.</summary>
    private const string FullSeparator = " / ";

    /// <summary>Joins the fourth answer's two full labels in its
    /// <see cref="BreakdownBucketLabel"/>.</summary>
    private const string EitherSeparator = " or ";

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
    /// The full label of <paramref name="answer"/> at
    /// <paramref name="decision"/>: <c>No double</c>, <c>Double / Take</c>,
    /// <c>Double / Pass</c>, and for the fourth answer
    /// (<see cref="CubeAnswer.NoDoublePass"/>) <c>Too good</c> or
    /// <c>No double / Pass</c>, as the decision reads it
    /// (<see cref="CubeDecision.ClaimOf"/>).
    /// </summary>
    /// <param name="answer">The answer to label.</param>
    /// <param name="decision">The decision <paramref name="answer"/> answers,
    /// whose reading chooses the fourth answer's label.</param>
    /// <returns>The sentence-case full label.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="decision"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="answer"/> is not one of the four
    /// <see cref="CubeAnswer"/> members.
    /// </exception>
    public static string Label(CubeAnswer answer, CubeDecision decision) => Spell(answer, decision).Full;

    /// <summary>
    /// The short label of <paramref name="answer"/> at
    /// <paramref name="decision"/>, for a row that cannot fit the full one
    /// (SPEC-quiz-view §4): <c>ND</c>, <c>D/T</c>, <c>D/P</c>, and for the
    /// fourth answer (<see cref="CubeAnswer.NoDoublePass"/>) <c>TG</c> or
    /// <c>NP</c>, as the decision reads it
    /// (<see cref="CubeDecision.ClaimOf"/>). When a surface uses it is that
    /// surface's to decide; the full label stays the answer's accessible
    /// name.
    /// </summary>
    /// <param name="answer">The answer to label.</param>
    /// <param name="decision">The decision <paramref name="answer"/> answers,
    /// whose reading chooses the fourth answer's label.</param>
    /// <returns>The short label.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="decision"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="answer"/> is not one of the four
    /// <see cref="CubeAnswer"/> members.
    /// </exception>
    public static string ShortLabel(CubeAnswer answer, CubeDecision decision) => Spell(answer, decision).Short;

    /// <summary>
    /// The name of the answer-type breakdown's bucket for
    /// <paramref name="answer"/> (SPEC-scoring §3, "The tie", its breakdown
    /// sub-bullet): <c>No double</c>, <c>Double / Take</c>,
    /// <c>Double / Pass</c>, and for the fourth answer
    /// (<see cref="CubeAnswer.NoDoublePass"/>)
    /// <c>Too good or No double / Pass</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A bucket gathers problems, each at its own decision, so its name takes
    /// none. The first three answers read one way at every decision, and
    /// their bucket names are their full labels, from the same table
    /// <see cref="Label(CubeAnswer, CubeDecision)"/> reads. The fourth bucket
    /// holds the fourth answer under either label, so its name joins the two
    /// full labels the fourth answer takes; neither is spelled twice.
    /// </para>
    /// <para>
    /// It names a bucket, not an answer at a decision: it infers no gammon
    /// context, and it is never a fallback for labelling one answer. One
    /// answer is labelled only at its decision, by
    /// <see cref="Label(CubeAnswer, CubeDecision)"/> or
    /// <see cref="ShortLabel"/>.
    /// </para>
    /// </remarks>
    /// <param name="answer">The answer whose bucket to name.</param>
    /// <returns>The sentence-case bucket name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="answer"/> is not one of the four
    /// <see cref="CubeAnswer"/> members.
    /// </exception>
    public static string BreakdownBucketLabel(CubeAnswer answer) => answer switch
    {
        CubeAnswer.NoDoublePass => FourthReadTooGood.Full + EitherSeparator + FourthReadNoDouble.Full,
        _ => SpellOneReading(answer).Full
    };

    /// <summary>
    /// Both spellings of <paramref name="answer"/> at
    /// <paramref name="decision"/>, side by side, so a full label and its
    /// short form cannot drift apart.
    /// </summary>
    private static (string Full, string Short) Spell(CubeAnswer answer, CubeDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return answer switch
        {
            CubeAnswer.NoDoublePass => SpellFourth(decision.ClaimOf(answer)),
            _ => SpellOneReading(answer)
        };
    }

    /// <summary>
    /// Both spellings of an answer that reads one way at every decision: the
    /// one table of those answers' labels, read by
    /// <see cref="Label(CubeAnswer, CubeDecision)"/>,
    /// <see cref="ShortLabel"/> and <see cref="BreakdownBucketLabel"/> alike.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="answer"/> is not one of the four
    /// <see cref="CubeAnswer"/> members.
    /// </exception>
    /// <exception cref="UnreachableException">
    /// <paramref name="answer"/> is the fourth answer, whose spelling depends
    /// on its reading: every caller spells it before reaching this table.
    /// </exception>
    private static (string Full, string Short) SpellOneReading(CubeAnswer answer) => answer switch
    {
        CubeAnswer.NoDouble     => (Label(CubeAction.NoDouble), "ND"),
        CubeAnswer.DoubleTake   => (Joined(CubeAction.Double, CubeAction.Take), "D/T"),
        CubeAnswer.DoublePass   => (Joined(CubeAction.Double, CubeAction.Pass), "D/P"),
        CubeAnswer.NoDoublePass => throw new UnreachableException(
            "The fourth answer has two readings; its callers spell it before this table."),
        _ => throw new ArgumentOutOfRangeException(nameof(answer), answer,
            "CubeLabels requires one of the four CubeAnswer members.")
    };

    /// <summary>
    /// The fourth answer's two spellings for the decision's
    /// <paramref name="reading"/> of it: Too good where it reads
    /// <see cref="CubeClaim.TooGood"/>, No double / Pass where it reads
    /// <see cref="CubeClaim.NoDouble"/>. The reading is rendered, never
    /// re-checked.
    /// </summary>
    /// <exception cref="UnreachableException">
    /// The decision read the fourth answer as a claim it cannot make
    /// (<see cref="CubeClaim.Double"/>, or a value outside
    /// <see cref="CubeClaim"/>): a broken producer contract, which no label
    /// can stand for.
    /// </exception>
    private static (string Full, string Short) SpellFourth(CubeClaim reading) => reading switch
    {
        CubeClaim.TooGood  => FourthReadTooGood,
        CubeClaim.NoDouble => FourthReadNoDouble,
        _ => throw new UnreachableException(
            $"CubeDecision.ClaimOf read the fourth answer as {reading}; it reads TooGood or NoDouble.")
    };

    /// <summary>The fourth answer's spellings where it reads Too good.</summary>
    private static (string Full, string Short) FourthReadTooGood => ("Too good", "TG");

    /// <summary>The fourth answer's spellings where it reads No double, with
    /// its pass.</summary>
    private static (string Full, string Short) FourthReadNoDouble =>
        (Joined(CubeAction.NoDouble, CubeAction.Pass), "NP");

    /// <summary>A full label naming a doubling action and a response, each in
    /// its own action label.</summary>
    private static string Joined(CubeAction doubler, CubeAction response) =>
        Label(doubler) + FullSeparator + Label(response);
}
