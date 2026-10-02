using System.Reflection;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Pins <see cref="CubeLabels"/> — the library's one public home for the
/// wording of a cube answer and a cube action (halheinrich/backgammon#185):
/// the four answers' full and short labels (SPEC-scoring §3, SPEC-quiz-view
/// §4), the fourth labelled by its decision's reading, the answer-type
/// breakdown's four bucket names, and the four action labels. Every member is pinned exhaustively over its type, because the
/// point of a single label home is that nothing downstream re-spells these
/// words: a silent change here would ripple through the cube panel,
/// BgDiag_Razor and BgQuiz at once.
/// </summary>
public class CubeLabelsTests
{
    /// <summary>
    /// A decision for each gammon fact. The two differ in that fact alone, so
    /// a label that changes between them changes by the decision's reading.
    /// The equities are a double/take's; no label depends on them.
    /// </summary>
    private static CubeDecision DecisionWhereGammons(bool possible) =>
        TestFixtures.CubeWithGammons(possible, noDoubleEquity: 0.40, doubleTakeEquity: 0.60);

    // -----------------------------------------------------------------------
    //  Actions — sentence case, exhaustive
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAction.NoDouble, "No double")]
    [InlineData(CubeAction.Double, "Double")]
    [InlineData(CubeAction.Take, "Take")]
    [InlineData(CubeAction.Pass, "Pass")]
    public void Label_Action_IsSentenceCase(CubeAction action, string expected)
        => Assert.Equal(expected, CubeLabels.Label(action));

    [Fact]
    public void Label_Action_CoversEveryDefinedMember()
    {
        // The suite above is exhaustive by inspection; this makes it
        // exhaustive by construction, so a member added to CubeAction fails
        // here rather than reaching a reader as an exception.
        foreach (CubeAction action in Enum.GetValues<CubeAction>())
            Assert.False(string.IsNullOrWhiteSpace(CubeLabels.Label(action)));
    }

    // -----------------------------------------------------------------------
    //  Answers — full and short, each under both gammon facts
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAnswer.NoDouble, true, "No double")]
    [InlineData(CubeAnswer.NoDouble, false, "No double")]
    [InlineData(CubeAnswer.DoubleTake, true, "Double / Take")]
    [InlineData(CubeAnswer.DoubleTake, false, "Double / Take")]
    [InlineData(CubeAnswer.DoublePass, true, "Double / Pass")]
    [InlineData(CubeAnswer.DoublePass, false, "Double / Pass")]
    [InlineData(CubeAnswer.NoDoublePass, true, "Too good")]
    [InlineData(CubeAnswer.NoDoublePass, false, "No double / Pass")]
    public void Label_Answer_IsTheFullLabel(CubeAnswer answer, bool gammonsPossible, string expected)
        => Assert.Equal(expected, CubeLabels.Label(answer, DecisionWhereGammons(gammonsPossible)));

    [Theory]
    [InlineData(CubeAnswer.NoDouble, true, "ND")]
    [InlineData(CubeAnswer.NoDouble, false, "ND")]
    [InlineData(CubeAnswer.DoubleTake, true, "D/T")]
    [InlineData(CubeAnswer.DoubleTake, false, "D/T")]
    [InlineData(CubeAnswer.DoublePass, true, "D/P")]
    [InlineData(CubeAnswer.DoublePass, false, "D/P")]
    [InlineData(CubeAnswer.NoDoublePass, true, "TG")]
    [InlineData(CubeAnswer.NoDoublePass, false, "NP")]
    public void ShortLabel_Answer_IsTheShortLabel(CubeAnswer answer, bool gammonsPossible, string expected)
        => Assert.Equal(expected, CubeLabels.ShortLabel(answer, DecisionWhereGammons(gammonsPossible)));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Label_FourthAnswer_RendersTheDecisionsReading(bool gammonsPossible)
    {
        // The label home renders the producer's choice and holds no gammon
        // rule: Too good exactly where the decision reads the fourth answer
        // as Too good, No double / Pass exactly where it reads No double.
        var decision = DecisionWhereGammons(gammonsPossible);
        bool readsTooGood = decision.ClaimOf(CubeAnswer.NoDoublePass) == CubeClaim.TooGood;

        Assert.Equal(readsTooGood ? "Too good" : "No double / Pass", CubeLabels.Label(CubeAnswer.NoDoublePass, decision));
        Assert.Equal(readsTooGood ? "TG" : "NP", CubeLabels.ShortLabel(CubeAnswer.NoDoublePass, decision));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Labels_CoverEveryAnswer_AndTellThemApart(bool gammonsPossible)
    {
        // Exhaustive by construction, and injective at a decision in both
        // forms: a reader can always tell the four answers apart, whichever
        // form a surface shows.
        var decision = DecisionWhereGammons(gammonsPossible);
        var answers = Enum.GetValues<CubeAnswer>();

        var full = answers.Select(answer => CubeLabels.Label(answer, decision)).ToList();
        var brief = answers.Select(answer => CubeLabels.ShortLabel(answer, decision)).ToList();

        Assert.All(full.Concat(brief), label => Assert.False(string.IsNullOrWhiteSpace(label)));
        Assert.Equal(4, full.Distinct().Count());
        Assert.Equal(4, brief.Distinct().Count());
    }

    [Fact]
    public void Label_JoinedAnswers_AreTheirActionsLabels()
    {
        // A full label naming a doubling action and a response is the two
        // actions' own labels, not a third spelling: no lowercasing of the
        // second half, and the separator exactly " / " (ruled 2026-09-02).
        var decision = DecisionWhereGammons(possible: false);
        string Joined(CubeAction doubler, CubeAction response) =>
            CubeLabels.Label(doubler) + " / " + CubeLabels.Label(response);

        Assert.Equal(CubeLabels.Label(CubeAction.NoDouble), CubeLabels.Label(CubeAnswer.NoDouble, decision));
        Assert.Equal(Joined(CubeAction.Double, CubeAction.Take), CubeLabels.Label(CubeAnswer.DoubleTake, decision));
        Assert.Equal(Joined(CubeAction.Double, CubeAction.Pass), CubeLabels.Label(CubeAnswer.DoublePass, decision));
        Assert.Equal(Joined(CubeAction.NoDouble, CubeAction.Pass), CubeLabels.Label(CubeAnswer.NoDoublePass, decision));
    }

    // -----------------------------------------------------------------------
    //  The breakdown's bucket names: no decision, one per answer
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAnswer.NoDouble, "No double")]
    [InlineData(CubeAnswer.DoubleTake, "Double / Take")]
    [InlineData(CubeAnswer.DoublePass, "Double / Pass")]
    [InlineData(CubeAnswer.NoDoublePass, "Too good or No double / Pass")]
    public void BreakdownBucketLabel_IsTheBucketsName(CubeAnswer answer, string expected)
        => Assert.Equal(expected, CubeLabels.BreakdownBucketLabel(answer));

    [Theory]
    [InlineData(CubeAnswer.NoDouble, true)]
    [InlineData(CubeAnswer.NoDouble, false)]
    [InlineData(CubeAnswer.DoubleTake, true)]
    [InlineData(CubeAnswer.DoubleTake, false)]
    [InlineData(CubeAnswer.DoublePass, true)]
    [InlineData(CubeAnswer.DoublePass, false)]
    public void BreakdownBucketLabel_OfAnAnswerReadOneWay_IsItsFullLabel(CubeAnswer answer, bool gammonsPossible)
    {
        // The first three answers read one way at every decision, so their
        // bucket names are their full labels at any decision: one spelling,
        // under both gammon facts.
        Assert.Equal(
            CubeLabels.Label(answer, DecisionWhereGammons(gammonsPossible)),
            CubeLabels.BreakdownBucketLabel(answer));
    }

    [Fact]
    public void BreakdownBucketLabel_OfTheFourthAnswer_IsComposedOfItsTwoFullLabels()
    {
        // Composed from the label home's own spellings of the fourth answer,
        // read at a decision of each gammon fact, so neither phrase is spelled
        // twice. It names the bucket, and is neither reading's label.
        string tooGood = CubeLabels.Label(CubeAnswer.NoDoublePass, DecisionWhereGammons(possible: true));
        string noDoublePass = CubeLabels.Label(CubeAnswer.NoDoublePass, DecisionWhereGammons(possible: false));
        string bucket = CubeLabels.BreakdownBucketLabel(CubeAnswer.NoDoublePass);

        Assert.Equal(tooGood + " or " + noDoublePass, bucket);
        Assert.NotEqual(tooGood, bucket);
        Assert.NotEqual(noDoublePass, bucket);
    }

    [Fact]
    public void BreakdownBucketLabel_CoversEveryAnswer_AndTellsThemApart()
    {
        // Exhaustive by construction: an answer added to CubeAnswer fails
        // here, and the breakdown's buckets stay distinguishable.
        var names = Enum.GetValues<CubeAnswer>().Select(CubeLabels.BreakdownBucketLabel).ToList();

        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    // -----------------------------------------------------------------------
    //  The public surface: an answer is labelled only at its decision
    // -----------------------------------------------------------------------

    [Fact]
    public void PublicSurface_LabelsAnAnswerOnlyWithItsDecision_AndNeverAClaim()
    {
        // No public path labels the fourth answer from the answer alone, or
        // from a reading a caller supplies apart from its decision: every
        // public member taking an answer takes the decision too, and none
        // takes a claim (labelling CubeClaim.TooGood would label the fourth
        // answer from a supplied reading).
        //
        // One deliberate exception: BreakdownBucketLabel takes an answer
        // without a decision, because it names the answer-type breakdown's
        // bucket, which gathers problems across decisions (SPEC-scoring §3,
        // "The tie"). It labels no answer at a decision, and for the fourth
        // answer it names both readings rather than choosing one. The
        // exception is named here, once, so any other member taking an
        // answer alone still fails.
        const string bucketName = nameof(CubeLabels.BreakdownBucketLabel);
        var members = typeof(CubeLabels).GetMethods(BindingFlags.Public | BindingFlags.Static);
        Assert.NotEmpty(members);
        Assert.Single(members, member => member.Name == bucketName);

        foreach (var member in members)
        {
            var parameters = member.GetParameters().Select(p => p.ParameterType).ToList();
            Assert.DoesNotContain(typeof(CubeClaim), parameters);
            if (parameters.Contains(typeof(CubeAnswer)) && member.Name != bucketName)
                Assert.Contains(typeof(CubeDecision), parameters);
        }
    }

    // -----------------------------------------------------------------------
    //  No display fallback
    // -----------------------------------------------------------------------

    [Fact]
    public void Label_Action_ThrowsOnUndefinedValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.Label((CubeAction)99));

    [Fact]
    public void Labels_ThrowOnAnUndefinedAnswer()
    {
        var decision = DecisionWhereGammons(possible: true);

        Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.Label((CubeAnswer)99, decision));
        Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.ShortLabel((CubeAnswer)99, decision));
        Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.BreakdownBucketLabel((CubeAnswer)99));
    }

    [Fact]
    public void Labels_ThrowWithoutADecision()
    {
        // The decision is required for every answer, not only the fourth:
        // an answer is labelled at its decision.
        foreach (CubeAnswer answer in Enum.GetValues<CubeAnswer>())
        {
            Assert.Throws<ArgumentNullException>(() => CubeLabels.Label(answer, null!));
            Assert.Throws<ArgumentNullException>(() => CubeLabels.ShortLabel(answer, null!));
        }
    }
}
