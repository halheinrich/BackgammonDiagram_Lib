using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The play panel's depth treatment: the ranking's depth-first order and
/// the consumer-set display ceiling,
/// <see cref="DiagramRequest.MaximumHiddenCandidateAnalysisLevel"/>
/// (halheinrich/backgammon#66), which hides direct evaluations at or below
/// it. Rollout-family and unrecorded rows are never hidden, and neither are
/// the ranking's best play and the marked rows, whatever their depth — review
/// must always show what was best and what was played.
///
/// The ceiling is inclusive on the hide side (the user's ruling of
/// 2026-08-29 on halheinrich/backgammon#66): the top level is therefore a
/// usable selection, and "show only rollouts" — a ceiling of
/// <see cref="AnalysisLevel.XgRollerPlusPlus"/> — has its own pin below.
///
/// The ceiling is this repo's only rigor comparison, so it is where
/// <see cref="AnalysisLevel"/>'s declared order is consumed and therefore
/// where it is asserted from the consumer side: the <c>LevelLadder</c>
/// fixture carries one candidate per level so the ceiling pins name the order
/// member by member rather than sampling it. The order itself is the
/// producer's to state.
///
/// Every candidate's depth is stated as typed facts and its label and rank
/// are the producer's derivations; its move text is its play's notation, a
/// distinct hop from the standard start (<see cref="TestFixtures.StandardStartHops"/>).
/// </summary>
public class CandidateDepthTreatmentTests
{
    private const string Dagger = "†";

    private static List<string> Moves(string svg) => PlayPanelReader.Moves(svg);
    private static List<int> Ranks(string svg) => PlayPanelReader.Ranks(svg);

    private static string Hop(int index) => TestFixtures.StandardStartHops[index].ToNotation();

    /// <summary>A candidate on hop <paramref name="hop"/> with the given depth facts and equity.</summary>
    private static PlayCandidate Candidate(int hop, double equity,
        AnalysisMode mode = AnalysisMode.Evaluation, AnalysisLevel level = AnalysisLevel.Ply3,
        int? trials = null, int? code = null) =>
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[hop], equity: equity,
            analysisMode: mode, analysisLevel: level, rolloutTrials: trials, unrecognizedLevelCode: code);

    /// <summary>
    /// Nine equity-sorted candidates spanning the depth taxonomy: rollouts
    /// scattered down the equity order (the tester's own shape — he rolls out
    /// the best of each thematic category), evaluations at several levels, a
    /// book hit, and a row with no depth recorded. <c>m</c><i>i</i> is hop
    /// <i>i</i>'s notation.
    /// </summary>
    private static List<PlayCandidate> MixedPlays() =>
    [
        Candidate(0, 0.500, AnalysisMode.Evaluation, AnalysisLevel.Ply4),                   // 4-ply,   rank 40
        Candidate(1, 0.480, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),         // 3p1296,  rank 130
        Candidate(2, 0.470, AnalysisMode.Evaluation, AnalysisLevel.XgRollerPlusPlus),       // R++,     rank 75
        Candidate(3, 0.460, AnalysisMode.Evaluation, AnalysisLevel.Ply2),                   // 2-ply,   rank 20
        Candidate(4, 0.450, AnalysisMode.BookRollout, AnalysisLevel.Ply4, trials: 12960),   // B4_12960, rank 99
        Candidate(5, 0.440, AnalysisMode.Evaluation, AnalysisLevel.Ply1),                   // 1-ply,   rank 10
        Candidate(6, 0.430, AnalysisMode.Rollout, AnalysisLevel.Ply4, trials: 1296),         // 4p1296,  rank 140
        Candidate(7, 0.420, AnalysisMode.Unknown, AnalysisLevel.Unknown),                   // none,    no rank
        Candidate(8, 0.410, AnalysisMode.Evaluation, AnalysisLevel.Ply3),                   // 3-ply,   rank 30
    ];

    private static string[] Hops(params int[] indices) => [.. indices.Select(Hop)];

    /// <summary>A solution over <paramref name="plays"/>, no user play recorded unless stated.</summary>
    private static DiagramRequest Solution(List<PlayCandidate> plays, int? userPlayIndex = null) =>
        TestFixtures.RequestFor(TestFixtures.CheckerPlayWith(plays, userPlayIndex), DiagramMode.Solution);

    // -----------------------------------------------------------------------
    //  Ordering — depth first, the ranking's
    // -----------------------------------------------------------------------

    [Fact]
    public void DepthFirst_OrdersByDepthRankDescending_AndNumbersByTheRanking()
    {
        var svg = TestFixtures.Render(Solution(MixedPlays()) with { Ranking = PlayRanking.DepthFirst });

        // Depth ranks descending — 140, 130, 99, 75, 40, 30, 20, 10, and the
        // unrecorded depth last — and each row numbered by its place in that
        // ranking, the rank number SPEC-scoring §2a makes the ranking's.
        Assert.Equal(Hops(6, 1, 4, 2, 0, 8, 3, 5, 7), Moves(svg));
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], Ranks(svg));
    }

    [Fact]
    public void DepthFirst_WithinADepth_OrdersByEquity()
    {
        // Two depths, each stored out of equity order: within a depth the
        // ranking orders by equity, not by the stored order.
        List<PlayCandidate> plays =
        [
            Candidate(0, 0.46, AnalysisMode.Evaluation, AnalysisLevel.Ply2),
            Candidate(1, 0.48, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),
            Candidate(2, 0.50, AnalysisMode.Evaluation, AnalysisLevel.Ply2),
            Candidate(3, 0.49, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),
            Candidate(4, 0.44, AnalysisMode.Evaluation, AnalysisLevel.Ply2),
        ];
        var svg = TestFixtures.Render(Solution(plays) with { Ranking = PlayRanking.DepthFirst });

        Assert.Equal(Hops(3, 1, 2, 0, 4), Moves(svg));
    }

    [Fact]
    public void DepthFirst_MarksFollowCandidates_UnderTheOrderScramble()
    {
        // The primary (*) and secondary (†) marks are keyed to candidates and
        // land on the same ones after the depth-first order moves every row,
        // each with its rank in that order.
        var request = Solution(MixedPlays(), userPlayIndex: 8) with
        {
            Ranking = PlayRanking.DepthFirst,
            SecondaryPlayIndex = 3,
        };
        var svg = TestFixtures.Render(request);

        Assert.Equal(Hop(8), PlayPanelReader.RowMarked(svg, "*").Move);
        Assert.Equal(6, PlayPanelReader.RowMarked(svg, "*").Rank);
        Assert.Equal(Hop(3), PlayPanelReader.RowMarked(svg, Dagger).Move);
        Assert.Equal(7, PlayPanelReader.RowMarked(svg, Dagger).Rank);
    }

    [Fact]
    public void DepthFirst_BestPlay_HeadsThePanel_WhereverItIsStored()
    {
        // A rollout stored last is the depth-first best: it heads the panel
        // with rank 1 — the ranking's first is never beyond the window.
        int count = TestFixtures.StandardStartHops.Count;
        var plays = TestFixtures.HopCandidates(count);
        plays[^1] = Candidate(count - 1, 0.01, AnalysisMode.Rollout, AnalysisLevel.Ply4, trials: 1296);
        var svg = TestFixtures.Render(Solution(plays) with { Ranking = PlayRanking.DepthFirst });

        var first = PlayPanelReader.Rows(svg)[0];
        Assert.Equal(Hop(count - 1), first.Move);
        Assert.Equal(1, first.Rank);
        Assert.Null(first.Loss);
    }

    // -----------------------------------------------------------------------
    //  Ceiling — MaximumHiddenCandidateAnalysisLevel
    // -----------------------------------------------------------------------

    [Fact]
    public void Ceiling_HidesShallowEvaluations_KeepsRolloutsBookUnrecordedAndBest()
    {
        // The tester's ask — "4-ply and lower should not be displayed" — is
        // an inclusive-hide ceiling of Ply4. Hidden: the direct evaluations
        // at or below Ply4 — m3 (2-ply), m5 (1-ply), m8 (3-ply). Never
        // hidden: the rollouts m1/m6 (inner levels Ply3/Ply4 are the
        // rollout's inner level, not its depth), the book hit m4, the
        // unrecorded m7, and the best play m0 — itself a 4-ply evaluation, so
        // the ceiling names it and only the best-row exemption keeps it.
        //
        // This fixture spans the *mode* axis, not the whole level axis: it
        // holds no XG Roller / XG Roller+ row, so this pin says nothing about
        // where a Ply4 ceiling cuts the level order.
        // Ceiling_HidesExactlyTheLevelsAtOrBelowIt and its Roller-boundary
        // sibling below carry that claim.
        var svg = TestFixtures.Render(Solution(MixedPlays()) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply4 });

        Assert.Equal(Hops(0, 1, 2, 4, 6, 7), Moves(svg));
        // Survivors keep their ranking ranks.
        Assert.Equal([1, 2, 3, 5, 7, 8], Ranks(svg));
    }

    [Fact]
    public void Ceiling_NeverHidesUserOrSecondaryRows_WhateverTheirDepth()
    {
        // Ceiling at the top of the level axis: every direct evaluation is
        // ceiling-eligible, R++ itself included. The user's play (1-ply) and
        // the secondary (2-ply) survive anyway — review must always show what
        // was played — while the equally shallow m8 (3-ply, unmarked) and m2
        // (R++) are hidden.
        var request = Solution(MixedPlays(), userPlayIndex: 5) with
        {
            MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.XgRollerPlusPlus,
            SecondaryPlayIndex = 3,
        };
        var svg = TestFixtures.Render(request);

        Assert.Equal(Hops(0, 1, 3, 4, 5, 6, 7), Moves(svg));
        Assert.Equal(Hop(5), PlayPanelReader.RowMarked(svg, "*").Move);
        Assert.Equal(Hop(3), PlayPanelReader.RowMarked(svg, Dagger).Move);
    }

    [Fact]
    public void Ceiling_XgRollerPlusPlus_LeavesOnlyRolloutsAndTheExemptRows()
    {
        // The ruling of 2026-08-29 (halheinrich/backgammon#66) in one pin:
        // "show only rollouts" is the top level named as the ceiling. Every
        // direct evaluation goes — m2 (R++) at the ceiling itself, m3, m5, m8
        // beneath it — leaving the rollout family (m1, m6), the book hit (m4),
        // the unrecorded row (m7), and the best play m0, kept by its exemption
        // rather than by its depth.
        var svg = TestFixtures.Render(Solution(MixedPlays()) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.XgRollerPlusPlus });

        Assert.Equal(Hops(0, 1, 4, 6, 7), Moves(svg));
        Assert.Equal([1, 2, 5, 7, 8], Ranks(svg));
    }

    [Fact]
    public void CeilingAndDepthFirst_Compose_TheExemptBestIsTheRankings()
    {
        // Both at once: depth first, and the shallow evaluations hidden. The
        // best-play exemption is the ranking's best — under depth first the
        // 4p1296 rollout m6 — so m0, best by equity alone, is a hidden 4-ply
        // evaluation here.
        var svg = TestFixtures.Render(Solution(MixedPlays()) with
        {
            Ranking = PlayRanking.DepthFirst,
            MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply4,
        });

        Assert.Equal(Hops(6, 1, 4, 2, 7), Moves(svg));
    }

    // -----------------------------------------------------------------------
    //  Ceiling — the level axis, member by member
    // -----------------------------------------------------------------------

    /// <summary>
    /// Every <see cref="AnalysisLevel"/> member except
    /// <see cref="AnalysisLevel.Unknown"/>, one direct-evaluation candidate
    /// each, listed in <b>descending</b> rigor — the reverse of the enum's
    /// declared order, in which the ply family and the XG Roller family
    /// interleave (the producer's contract states it).
    /// <para>
    /// The ladder rows are preceded in the fixture by the rollout anchor, so
    /// the best play — which is exempt — is <em>not</em> a ladder row: no
    /// level is unreachable by some ceiling, R++ included, so parking the best
    /// row on the top of the scale would mask exactly the case the ruling is
    /// about. With the anchor in front, all eleven ladder rows are honestly
    /// ceiling-eligible.
    /// </para>
    /// <para>
    /// Level <i>i</i>'s move is hop 1 + <i>i</i> whatever order the rows are
    /// stored in, so a failure names the level that moved.
    /// </para>
    /// </summary>
    private static readonly AnalysisLevel[] LevelLadder =
    [
        AnalysisLevel.XgRollerPlusPlus,
        AnalysisLevel.Ply7,
        AnalysisLevel.Ply6,
        AnalysisLevel.Ply5,
        AnalysisLevel.XgRollerPlus,
        AnalysisLevel.Ply4,
        AnalysisLevel.XgRoller,
        AnalysisLevel.Ply3,
        AnalysisLevel.Ply3Red,
        AnalysisLevel.Ply2,
        AnalysisLevel.Ply1,
    ];

    /// <summary>The anchor's move: a 4-ply rollout, best by equity, hop 0.</summary>
    private static string LadderAnchor => Hop(0);

    /// <summary>The move of the ladder row at <paramref name="level"/>.</summary>
    private static string Rung(AnalysisLevel level) => Hop(1 + Array.IndexOf(LevelLadder, level));

    /// <summary>The ladder as candidates behind the anchor, stored in the
    /// order given by <paramref name="order"/> — indices into
    /// <see cref="LevelLadder"/>, equities falling down the stored order.
    /// Defaults to the ladder's own descending-rigor order.</summary>
    private static List<PlayCandidate> LadderPlays(int[]? order = null)
    {
        order ??= [.. Enumerable.Range(0, LevelLadder.Length)];
        List<PlayCandidate> plays = [Candidate(0, 0.510, AnalysisMode.Rollout, AnalysisLevel.Ply4, trials: 1296)];
        plays.AddRange(order.Select((li, row) =>
            Candidate(1 + li, 0.500 - row * 0.010, AnalysisMode.Evaluation, LevelLadder[li])));
        return plays;
    }

    /// <summary>The anchor followed by the rungs strictly above
    /// <paramref name="ceiling"/> — what a ceiling at that level must leave
    /// visible.</summary>
    private static string[] LadderAbove(AnalysisLevel ceiling) =>
        [LadderAnchor, .. LevelLadder.TakeWhile(level => level != ceiling).Select(Rung)];

    [Theory]
    [InlineData(AnalysisLevel.XgRollerPlusPlus)]
    [InlineData(AnalysisLevel.Ply7)]
    [InlineData(AnalysisLevel.Ply6)]
    [InlineData(AnalysisLevel.Ply5)]
    [InlineData(AnalysisLevel.XgRollerPlus)]
    [InlineData(AnalysisLevel.Ply4)]
    [InlineData(AnalysisLevel.XgRoller)]
    [InlineData(AnalysisLevel.Ply3)]
    [InlineData(AnalysisLevel.Ply3Red)]
    [InlineData(AnalysisLevel.Ply2)]
    [InlineData(AnalysisLevel.Ply1)]
    public void Ceiling_HidesExactlyTheLevelsAtOrBelowIt(AnalysisLevel ceiling)
    {
        // The whole-axis pin: for every level the ceiling may take, the
        // visible set is the exempt anchor plus exactly the members *strictly
        // above* the ceiling in AnalysisLevel's declared order — the ceiling
        // is inclusive on the hide side, so its own level goes. The R++ cell
        // is the ruled top case seen from the ladder: nothing but the anchor
        // is left.
        var svg = TestFixtures.Render(Solution(LadderPlays()) with { MaximumHiddenCandidateAnalysisLevel = ceiling });

        Assert.Equal(LadderAbove(ceiling), Moves(svg));
    }

    [Fact]
    public void Ceiling_Ply5_SweepsOutTheRollerLevelsBeneathIt()
    {
        // A ply ceiling is not a ply-family-only instrument: XG Roller and XG
        // Roller+ sit beneath Ply5 in the declared order, so a Ply5 ceiling
        // sweeps them out with the shallow plies. Ply5 itself goes as well:
        // the ceiling is inclusive on the hide side.
        var visible = Moves(TestFixtures.Render(Solution(LadderPlays()) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply5 }));

        Assert.DoesNotContain(Rung(AnalysisLevel.XgRoller), visible);
        Assert.DoesNotContain(Rung(AnalysisLevel.XgRollerPlus), visible);
        Assert.DoesNotContain(Rung(AnalysisLevel.Ply5), visible);     // inclusive hide
        Assert.Contains(Rung(AnalysisLevel.Ply6), visible);           // strictly above
        Assert.Contains(Rung(AnalysisLevel.XgRollerPlusPlus), visible);
    }

    [Theory]
    [InlineData(AnalysisLevel.Ply1)]
    [InlineData(AnalysisLevel.Ply2)]
    [InlineData(AnalysisLevel.Ply3)]
    [InlineData(AnalysisLevel.Ply4)]
    [InlineData(AnalysisLevel.Ply5)]
    [InlineData(AnalysisLevel.Ply6)]
    [InlineData(AnalysisLevel.Ply7)]
    public void Ceiling_XgRollerPlusPlus_SurvivesEveryPlyCeiling(AnalysisLevel plyCeiling)
    {
        // XG Roller++ is the most rigorous level XG offers, so no ply ceiling
        // — not even 7-ply — reaches it: a caller must name it outright to
        // suppress it (Ceiling_XgRollerPlusPlus_LeavesOnlyRolloutsAndTheExemptRows).
        var svg = TestFixtures.Render(Solution(LadderPlays()) with { MaximumHiddenCandidateAnalysisLevel = plyCeiling });

        Assert.Contains(Rung(AnalysisLevel.XgRollerPlusPlus), Moves(svg));
    }

    // -----------------------------------------------------------------------
    //  Ply3Red — its own level, end to end
    // -----------------------------------------------------------------------

    [Fact]
    public void Ply3Red_Renders_WithItsOwnProducerLabel()
    {
        // A candidate at XG's reduced-variance 3-ply reaches the panel intact:
        // the Depth cell is the producer's derived abbreviation,
        // PlayCandidate.DepthAbbreviation, so the renderer holds no
        // level-to-label table of its own.
        var rows = PlayPanelReader.Rows(TestFixtures.Render(Solution(LadderPlays())));

        Assert.Equal("3-ply Red", rows.Single(r => r.Move == Rung(AnalysisLevel.Ply3Red)).Depth);
    }

    [Fact]
    public void Ply3Red_SortsIntoItsPlace_UnderDepthFirst()
    {
        // Depth first over a scrambled ladder rebuilds the declared order
        // exactly — 3-ply Red above 2-ply and below a full 3-ply: the
        // producer's rank grid and AnalysisLevel's declared order agree, and
        // this is where that agreement is asserted from the consumer side.
        int[] scrambled = [8, 0, 10, 5, 2, 9, 6, 1, 7, 4, 3];
        var svg = TestFixtures.Render(Solution(LadderPlays(scrambled)) with { Ranking = PlayRanking.DepthFirst });

        // The rollout anchor outranks every evaluation and heads the list;
        // the eleven rungs follow in declared order.
        Assert.Equal([LadderAnchor, .. LevelLadder.Select(Rung)], Moves(svg));
    }

    [Fact]
    public void Ply3Red_IsItsOwnLevelBetweenPly2AndPly3_UnderTheCeiling()
    {
        // A Ply3Red ceiling hides the row it names (inclusive) yet leaves the
        // full Ply3 above it; a Ply2 ceiling, strictly below Ply3Red, leaves
        // Ply3Red standing.
        var underPly3Red = Moves(TestFixtures.Render(Solution(LadderPlays()) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply3Red }));
        var underPly2 = Moves(TestFixtures.Render(Solution(LadderPlays()) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply2 }));

        Assert.DoesNotContain(Rung(AnalysisLevel.Ply3Red), underPly3Red);
        Assert.Contains(Rung(AnalysisLevel.Ply3), underPly3Red);
        Assert.Contains(Rung(AnalysisLevel.Ply3Red), underPly2);
    }

    // -----------------------------------------------------------------------
    //  Unknown — outside the order
    // -----------------------------------------------------------------------

    /// <summary>
    /// <see cref="AnalysisLevel.Unknown"/> sits <i>outside</i> the rigor order
    /// (the producer's contract): it means the level was not recorded, so no
    /// ceiling hides it. The pin matters because the enum's numbering works
    /// against it — <c>Unknown = 0</c> compares at or below every real level,
    /// so the ordinal test <c>AnalysisLevel &lt;= ceiling</c> would hide an
    /// Unknown-level row under any ceiling. Only the renderer's explicit guard
    /// keeps it; delete the guard and this pin fires. Both shapes are covered:
    /// an evaluation whose level the producer could not decode (its raw code
    /// kept), and a row with neither axis recorded.
    /// </summary>
    [Fact]
    public void Ceiling_NeverHidesUnknownLevelRows_WhateverTheCeiling()
    {
        var plays = LadderPlays();
        plays.Add(Candidate(12, 0.100, AnalysisMode.Evaluation, AnalysisLevel.Unknown, code: 77));
        plays.Add(Candidate(13, 0.090, AnalysisMode.Unknown, AnalysisLevel.Unknown));

        // The highest ceiling there is — every level in the order goes, R++
        // included. The two Unknown rows are not in the order, so they stay,
        // alongside the exempt anchor.
        var svg = TestFixtures.Render(Solution(plays) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.XgRollerPlusPlus });

        Assert.Equal([LadderAnchor, Hop(12), Hop(13)], Moves(svg));
        Assert.Equal("level-77", PlayPanelReader.Rows(svg)[1].Depth);
        Assert.Null(PlayPanelReader.Rows(svg)[2].Depth);   // no depth recorded: a blank cell
    }

    // -----------------------------------------------------------------------
    //  Inertness — vacuous settings change nothing
    // -----------------------------------------------------------------------

    [Fact]
    public void VacuousCeiling_IsByteIdenticalToUnset()
    {
        // A Ply1 ceiling over MixedPlays names exactly one row — m5, the sole
        // 1-ply evaluation — and here m5 is the user's play, so the
        // never-hidden contract keeps it and nothing at all is removed. The
        // active-but-vacuous machinery must then be byte-invisible.
        var unset = Solution(MixedPlays(), userPlayIndex: 5);

        Assert.Equal(TestFixtures.Render(unset),
            TestFixtures.Render(unset with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply1 }));
    }

    [Fact]
    public void DepthFirst_WhenDepthAndEquityAgree_IsByteIdenticalToEquity()
    {
        // Stored deepest first and highest equity first: both rankings order
        // the candidates alike, choose the same best, and score every
        // candidate against it alike, so the two renderings are identical.
        List<PlayCandidate> plays =
        [
            Candidate(0, 0.50, AnalysisMode.Rollout, AnalysisLevel.Ply4, trials: 1296),
            Candidate(1, 0.49, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),
            Candidate(2, 0.48, AnalysisMode.BookRollout, AnalysisLevel.Unknown),
            Candidate(3, 0.47, AnalysisMode.Evaluation, AnalysisLevel.XgRollerPlusPlus),
            Candidate(4, 0.46, AnalysisMode.Evaluation, AnalysisLevel.Ply3),
        ];
        var equity = Solution(plays, userPlayIndex: 1);

        Assert.Equal(TestFixtures.Render(equity), TestFixtures.Render(equity with { Ranking = PlayRanking.DepthFirst }));
    }
}
