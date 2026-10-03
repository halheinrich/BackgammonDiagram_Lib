using BgDataTypes_Lib;

namespace BackgammonDiagram_Lib;

/// <summary>
/// One spelling of a cube answer: its full label and its short label under one
/// reading, together. The full label is the wording of
/// <see cref="CubeLabels.Label(CubeAnswer, CubeDecision)"/>, and the short
/// label the wording of <see cref="CubeLabels.ShortLabel"/>, for a row that
/// cannot fit the full ones (SPEC-quiz-view §4). <see cref="CubeLabels"/>
/// makes every one, and <see cref="CubeLabels.Spellings"/> hands them out.
/// </summary>
/// <remarks>
/// <para>
/// The two labels are never parted, so a full label and its short form cannot
/// drift apart. A spelling labels no answer at a decision: one answer is
/// labelled only at its decision, by
/// <see cref="CubeLabels.Label(CubeAnswer, CubeDecision)"/> or
/// <see cref="CubeLabels.ShortLabel"/>. It carries no reading either, so no
/// caller can look up a label from a claim.
/// </para>
/// <para>
/// Value equality: equal full and short labels. Its constructor is internal,
/// so every spelling a caller holds carries the label home's labels. A class
/// rather than a struct, so that no instance escapes the label home: a
/// struct's default would be a spelling with no labels. Its
/// <see cref="object.ToString"/> is a record's diagnostic form, not a label.
/// </para>
/// </remarks>
public sealed record CubeAnswerSpelling
{
    /// <summary>Creates a spelling. The label home's alone.</summary>
    /// <param name="fullLabel">The full label.</param>
    /// <param name="shortLabel">The short label.</param>
    internal CubeAnswerSpelling(string fullLabel, string shortLabel)
    {
        Full = fullLabel;
        Short = shortLabel;
    }

    /// <summary>The full label, in sentence case: <c>Double / Take</c>,
    /// <c>Too good</c>.</summary>
    public string Full { get; }

    /// <summary>The short label: <c>D/T</c>, <c>TG</c>.</summary>
    public string Short { get; }
}
