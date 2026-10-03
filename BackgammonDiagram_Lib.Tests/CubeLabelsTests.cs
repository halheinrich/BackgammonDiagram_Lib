using System.Reflection;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Pins <see cref="CubeLabels"/> — the library's one public home for the
/// wording of a cube answer and a cube action (halheinrich/backgammon#185):
/// the four answers' full and short labels (SPEC-scoring §3, SPEC-quiz-view
/// §4), the fourth labelled by its decision's reading, every spelling each
/// answer can take, the answer-type breakdown's four bucket names, and the
/// four action labels. Every member is pinned exhaustively over its type, because the
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
    //  Every spelling an answer can take: no decision, for sizing a row
    // -----------------------------------------------------------------------

    /// <summary><paramref name="answer"/>'s spellings as (full, short) pairs, in order.</summary>
    private static List<(string Full, string Short)> SpellingsOf(CubeAnswer answer) =>
        CubeLabels.Spellings(answer).Select(spelling => (spelling.Full, spelling.Short)).ToList();

    [Fact]
    public void Spellings_AreEachAnswersFullAndShortLabels_TheFourthsTooGoodFirst()
    {
        // Exact, full and short together, one per reading: one for each of
        // the first three answers, two for the fourth in a stated order, Too
        // good first, as its bucket name joins them.
        Assert.Equal([("No double", "ND")], SpellingsOf(CubeAnswer.NoDouble));
        Assert.Equal([("Double / Take", "D/T")], SpellingsOf(CubeAnswer.DoubleTake));
        Assert.Equal([("Double / Pass", "D/P")], SpellingsOf(CubeAnswer.DoublePass));
        Assert.Equal([("Too good", "TG"), ("No double / Pass", "NP")], SpellingsOf(CubeAnswer.NoDoublePass));
    }

    [Fact]
    public void Spellings_AreExactlyThePairsTheLabelsGive_AtDecisionsReadingTheFourthEachWay()
    {
        // At each decision an answer's full and short labels, taken together,
        // are one of its spellings; and every spelling is one a decision
        // gives. So the list is neither short of a spelling a row can show
        // nor padded with one it never shows. The two decisions read the
        // fourth answer each way, checked rather than assumed. Exhaustive by
        // construction: an answer added to CubeAnswer is checked here too.
        var decisions = new[] { DecisionWhereGammons(possible: true), DecisionWhereGammons(possible: false) };
        Assert.Equal(
            [CubeClaim.TooGood, CubeClaim.NoDouble],
            decisions.Select(decision => decision.ClaimOf(CubeAnswer.NoDoublePass)));

        foreach (CubeAnswer answer in Enum.GetValues<CubeAnswer>())
        {
            var given = decisions
                .Select(decision => (CubeLabels.Label(answer, decision), CubeLabels.ShortLabel(answer, decision)))
                .Distinct()
                .Order();

            Assert.Equal(given, SpellingsOf(answer).Order());
        }
    }

    [Fact]
    public void Spellings_CannotBeChangedByACaller()
    {
        // An immutable array of immutable spellings. The array refuses every
        // change through the collection interfaces it implements. A spelling
        // has no setter, init included, and no public constructor, so every
        // spelling a caller holds carries the label home's labels. The result
        // held and the next read are both unchanged.
        var spellings = CubeLabels.Spellings(CubeAnswer.NoDoublePass);
        var asList = (IList<CubeAnswerSpelling>)spellings;

        Assert.True(asList.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => asList[0] = asList[1]);
        Assert.Throws<NotSupportedException>(() => asList.Add(asList[0]));
        Assert.Throws<NotSupportedException>(() => asList.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => ((System.Collections.IList)spellings).Clear());

        Assert.All(typeof(CubeAnswerSpelling).GetProperties(), property => Assert.Null(property.SetMethod));
        Assert.Empty(typeof(CubeAnswerSpelling).GetConstructors());

        Assert.Equal([("Too good", "TG"), ("No double / Pass", "NP")], spellings.Select(spelling => (spelling.Full, spelling.Short)));
        Assert.Equal([("Too good", "TG"), ("No double / Pass", "NP")], SpellingsOf(CubeAnswer.NoDoublePass));
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
    public void BreakdownBucketLabel_JoinsTheFullLabelsOfItsAnswersSpellings()
    {
        // The fourth answer's two readings, and their order, are stated once
        // for the bucket names and the spellings both: each bucket name is
        // its answer's spellings' full labels, in order, joined by " or ".
        foreach (CubeAnswer answer in Enum.GetValues<CubeAnswer>())
            Assert.Equal(
                string.Join(" or ", CubeLabels.Spellings(answer).Select(spelling => spelling.Full)),
                CubeLabels.BreakdownBucketLabel(answer));
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
        // Two deliberate exceptions take an answer without a decision, and
        // neither labels an answer at a decision; for the fourth answer each
        // gives both readings rather than choosing one:
        // - BreakdownBucketLabel names the answer-type breakdown's bucket,
        //   which gathers problems across decisions (SPEC-scoring §3, "The
        //   tie").
        // - Spellings lists every spelling an answer can take, for a host
        //   sizing a row of answers before any cube decision is on screen
        //   (SPEC-quiz-view §4, "One budget from the outset").
        // The exceptions are named here, once, so any other member taking an
        // answer alone still fails. A spelling carries no reading either, so
        // no caller can look a label up by a claim.
        string[] withoutADecision = [nameof(CubeLabels.BreakdownBucketLabel), nameof(CubeLabels.Spellings)];
        var members = typeof(CubeLabels).GetMethods(BindingFlags.Public | BindingFlags.Static);
        Assert.NotEmpty(members);
        foreach (var name in withoutADecision)
            Assert.Single(members, member => member.Name == name);

        foreach (var member in members)
        {
            var parameters = member.GetParameters().Select(p => p.ParameterType).ToList();
            Assert.DoesNotContain(typeof(CubeClaim), parameters);
            if (parameters.Contains(typeof(CubeAnswer)) && !withoutADecision.Contains(member.Name))
                Assert.Contains(typeof(CubeDecision), parameters);
        }

        Assert.DoesNotContain(typeof(CubeClaim), typeof(CubeAnswerSpelling).GetProperties().Select(p => p.PropertyType));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.Spellings((CubeAnswer)99));
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
