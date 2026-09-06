using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Pins <see cref="CubeLabels"/> — the library's one public home for the
/// wording of a cube answer (halheinrich/backgammon#185). Every member is
/// pinned exhaustively over its type, because the point of a single label
/// home is that nothing downstream re-spells these words: a silent change
/// here would ripple through the cube panel, BgDiag_Razor and BgQuiz at once.
/// </summary>
public class CubeLabelsTests
{
    // -----------------------------------------------------------------------
    //  Claims and actions — sentence case, exhaustive
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(CubeClaim.NoDouble, "No double")]
    [InlineData(CubeClaim.Double, "Double")]
    [InlineData(CubeClaim.TooGood, "Too good")]
    public void Label_Claim_IsSentenceCase(CubeClaim claim, string expected)
        => Assert.Equal(expected, CubeLabels.Label(claim));

    [Theory]
    [InlineData(CubeAction.NoDouble, "No double")]
    [InlineData(CubeAction.Double, "Double")]
    [InlineData(CubeAction.Take, "Take")]
    [InlineData(CubeAction.Pass, "Pass")]
    public void Label_Action_IsSentenceCase(CubeAction action, string expected)
        => Assert.Equal(expected, CubeLabels.Label(action));

    [Fact]
    public void Label_Claim_CoversEveryDefinedMember()
    {
        // The suite above is exhaustive by inspection; this makes it
        // exhaustive by construction, so a member added to CubeClaim fails
        // here rather than reaching a reader as an exception.
        foreach (CubeClaim claim in Enum.GetValues<CubeClaim>())
            Assert.False(string.IsNullOrWhiteSpace(CubeLabels.Label(claim)));
    }

    [Fact]
    public void Label_Action_CoversEveryDefinedMember()
    {
        foreach (CubeAction action in Enum.GetValues<CubeAction>())
            Assert.False(string.IsNullOrWhiteSpace(CubeLabels.Label(action)));
    }

    // -----------------------------------------------------------------------
    //  The pair rule — the closed 3×2, all six cells
    // -----------------------------------------------------------------------
    //
    //  Two clauses: a pair reads as its claim alone when that claim has
    //  exactly one reachable pair, else claim and response joined by " / ";
    //  and the incoherent cell reads as the posture it degenerates from. The
    //  four reachable verdicts and the two cells outside them are pinned
    //  together, because the rule is only meaningful as a total function over
    //  the type.

    [Theory]
    // The four reachable verdicts (SPEC-scoring §3, amended 2026-09-02).
    [InlineData(CubeClaim.NoDouble, CubeAction.Take, "No double")]
    [InlineData(CubeClaim.Double, CubeAction.Take, "Double / Take")]
    [InlineData(CubeClaim.Double, CubeAction.Pass, "Double / Pass")]
    [InlineData(CubeClaim.TooGood, CubeAction.Pass, "Too good")]
    // TooGoodTake: representable, but no analysis derives it since Too good
    // came to require the pass. It joins, because its response is exactly
    // what its claim does *not* imply — that contradiction is the whole of
    // what the pair says.
    [InlineData(CubeClaim.TooGood, CubeAction.Take, "Too good / Take")]
    // NoDoublePass, the incoherent cell: reads as Too good, not as its own
    // halves. SPEC-scoring §3's sixth-cell ruling buckets it with
    // Too good / Pass rather than giving it a bucket of its own — it is that
    // posture's degenerate point, derivable only at the exact tie boundary —
    // and a banner must not print a verdict the model calls incoherent.
    [InlineData(CubeClaim.NoDouble, CubeAction.Pass, "Too good")]
    public void Label_Pair_SpellsEveryCellOfTheClosedGrid(
        CubeClaim claim, CubeAction taker, string expected)
        => Assert.Equal(expected, CubeLabels.Label(new CubeClaimPair(claim, taker)));

    [Fact]
    public void Label_Pair_CoversTheWholeClosedGridAndIsNotInjective()
    {
        // Totality, stated as such: every cell of the closed 3×2 labels.
        //
        // Exactly one collision, and it is the deliberate one: NoDoublePass
        // and TooGoodPass both read "Too good", per the sixth-cell ruling.
        // The count is pinned at five rather than left open because the other
        // way a label could collide — the claim-alone compression swallowing
        // a cell its claim does not uniquely reach — would be a defect, and
        // this is what would catch it.
        var labels = new List<string>();
        foreach (CubeClaim claim in Enum.GetValues<CubeClaim>())
            foreach (CubeAction taker in new[] { CubeAction.Take, CubeAction.Pass })
                labels.Add(CubeLabels.Label(new CubeClaimPair(claim, taker)));

        Assert.Equal(6, labels.Count);
        Assert.Equal(5, labels.Distinct().Count());
        Assert.Equal(
            CubeLabels.Label(CubeClaimPair.TooGoodPass),
            CubeLabels.Label(CubeClaimPair.NoDoublePass));
    }

    [Fact]
    public void Label_Pair_AgreesWithItsHalves()
    {
        // The joined form is the two halves' own labels, not a third
        // spelling: no lowercasing of the second half, and the separator is
        // exactly " / " (ruled 2026-09-02).
        Assert.Equal(
            CubeLabels.Label(CubeClaim.Double) + " / " + CubeLabels.Label(CubeAction.Pass),
            CubeLabels.Label(CubeClaimPair.DoublePass));
    }

    [Fact]
    public void Label_Pair_MatchesTheCanonicalInstances()
    {
        // The canonical statics and the (claim, taker) constructor are the
        // same six values; pinning through the statics guards against the
        // rule being keyed to something other than the pair's own halves.
        Assert.Equal("No double", CubeLabels.Label(CubeClaimPair.NoDoubleTake));
        Assert.Equal("Too good", CubeLabels.Label(CubeClaimPair.NoDoublePass));
        Assert.Equal("Double / Take", CubeLabels.Label(CubeClaimPair.DoubleTake));
        Assert.Equal("Double / Pass", CubeLabels.Label(CubeClaimPair.DoublePass));
        Assert.Equal("Too good / Take", CubeLabels.Label(CubeClaimPair.TooGoodTake));
        Assert.Equal("Too good", CubeLabels.Label(CubeClaimPair.TooGoodPass));
    }

    // -----------------------------------------------------------------------
    //  No display fallback
    // -----------------------------------------------------------------------

    [Fact]
    public void Label_Claim_ThrowsOnUndefinedValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.Label((CubeClaim)99));

    [Fact]
    public void Label_Action_ThrowsOnUndefinedValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.Label((CubeAction)99));

    [Fact]
    public void Label_Pair_ThrowsOnTheNonMeaningfulDefault()
    {
        // default(CubeClaimPair) bypasses the type's own half-guards and
        // arrives with a Taker of CubeAction.NoDouble — a defined action, but
        // not a taker response. Labelling it would print the plausible
        // nonsense "No double / No double"; there is no display fallback.
        Assert.Throws<ArgumentOutOfRangeException>(() => CubeLabels.Label(default(CubeClaimPair)));
    }
}
