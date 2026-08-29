using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The play panel's depth treatment — the two consumer-set display options on
/// <see cref="DiagramRequest"/> (halheinrich/backgammon#150 and
/// halheinrich/backgammon#66):
///
///   * <see cref="DiagramRequest.CandidateOrdering"/> —
///     <see cref="CandidateOrdering.DepthFirst"/> orders rows by the
///     producer-stamped <see cref="PlayCandidate.DepthRank"/> descending,
///     stable within a tier (equity order preserved). The renderer ranks
///     nothing itself.
///   * <see cref="DiagramRequest.MinimumCandidateAnalysisLevel"/> — hides
///     shallow direct evaluations. Rollout-family and unstamped rows are
///     never hidden, and neither are the best-play row and the marked rows,
///     whatever their depth — review must always show what was best and what
///     was played.
///
/// The floor is this repo's only rigor comparison, so it is where
/// <see cref="AnalysisLevel"/>'s contractual order is consumed and therefore
/// where it is asserted (halheinrich/backgammon#159): the ply family and the
/// XG Roller family <i>interleave</i> rather than forming two blocks, and
/// <see cref="AnalysisLevel.Unknown"/> sits outside the scale entirely. The
/// <c>LevelLadder</c> fixture below carries one candidate per level so the
/// floor pins name the order member by member rather than sampling it.
///
/// Also pins the machinery's inertness: with the options at their defaults —
/// and even when active but vacuous — output is byte-identical to the
/// pre-existing rendering, so consumers that never set the options are
/// untouched. Marks, rank numbers, and the rank-inversion italics are keyed
/// to source indices, so they follow candidates, not row positions, under
/// reordering.
/// </summary>
public class CandidateDepthTreatmentTests
{
    private const string Dagger = "†";

    // Cell regexes at the Medium-size play-panel anchors (see AppendPlayPanel
    // and the sibling test classes): marker column bold at x=10, rank at
    // x=22.6, move notation at x=53.4. Each captures the row's y so rows can
    // be paired across columns.
    private static readonly Regex MarkerCell = new(
        """<text x="10" y="([0-9.]+)" font-family="sans-serif" font-size="14" font-weight="bold" fill="[^"]*">([^<]+)</text>""");
    private static readonly Regex RankCell = new(
        """<text x="22\.6" y="([0-9.]+)" font-family="sans-serif" font-size="14" fill="[^"]*">([0-9]+)</text>""");
    private static readonly Regex MoveCell = new(
        """<text x="53\.4" y="([0-9.]+)" font-family="sans-serif" font-size="14" fill="[^"]*">([^<]+)</text>""");

    private static List<string> Moves(string svg) =>
        MoveCell.Matches(svg).Select(m => m.Groups[2].Value).ToList();

    private static List<int> Ranks(string svg) =>
        RankCell.Matches(svg).Select(m => int.Parse(m.Groups[2].Value)).ToList();

    /// <summary>Move notation of the row carrying <paramref name="marker"/>,
    /// paired through the shared row y-coordinate.</summary>
    private static string MoveOfMarker(string svg, string marker)
    {
        var mark = MarkerCell.Matches(svg).Single(m => m.Groups[2].Value == marker);
        return MoveCell.Matches(svg).Single(m => m.Groups[1].Value == mark.Groups[1].Value).Groups[2].Value;
    }

    /// <summary>Depth-cell text of the row whose move notation is
    /// <paramref name="move"/>, paired through the shared row y-coordinate.
    /// The Depth column carries the italic attribute, so its cell pattern is
    /// looser than the others.</summary>
    private static string DepthOfMove(string svg, string move)
    {
        var row = MoveCell.Matches(svg).Single(m => m.Groups[2].Value == move);
        var depth = new Regex(
            $$"""<text x="347\.4" y="{{Regex.Escape(row.Groups[1].Value)}}" """
            + """font-family="sans-serif" font-size="14"[^>]*>([^<]+)</text>""");
        return depth.Match(svg).Groups[1].Value;
    }

    /// <summary>Rank number of the row carrying <paramref name="marker"/>.</summary>
    private static int RankOfMarker(string svg, string marker)
    {
        var mark = MarkerCell.Matches(svg).Single(m => m.Groups[2].Value == marker);
        return int.Parse(RankCell.Matches(svg).Single(m => m.Groups[1].Value == mark.Groups[1].Value).Groups[2].Value);
    }

    /// <summary>
    /// Nine equity-sorted candidates spanning the depth taxonomy: rollouts
    /// scattered down the equity order (the tester's own shape — he rolls out
    /// the best of each thematic category), evaluations at several levels, a
    /// book hit, and an unstamped row.
    /// <para>
    /// DepthRank values are the producer's live grid
    /// (halheinrich/backgammon#159): evaluations on a decade scale — a full
    /// N-ply ranks 10 × N and an interleaved level takes the midpoint
    /// (3-ply Red 25, XG Roller 35, XG Roller+ 45, XG Roller++ 75) — book 99,
    /// rollout 100 + inner ply. This fixture previously carried the flat
    /// scale that preceded halheinrich/backgammon#159
    /// (ply → N, Roller family → 20–22), which encoded the
    /// superseded plies-below-Rollers block order; its assertions survived the
    /// interleave only because it holds no XG Roller / XG Roller+ row to
    /// disagree about. <see cref="LevelLadder"/> is the fixture that does
    /// exercise the interleave.
    /// </para>
    /// </summary>
    private static List<PlayCandidate> MixedPlays()
    {
        var specs = new (string Move, double Eq, string Abbr, int Rank, AnalysisMode Mode, AnalysisLevel Level)[]
        {
            ("m0", 0.500, "4-ply",    40,  AnalysisMode.Evaluation,  AnalysisLevel.Ply4),
            ("m1", 0.480, "3p1296",   103, AnalysisMode.Rollout,     AnalysisLevel.Ply3),
            ("m2", 0.470, "R++",      75,  AnalysisMode.Evaluation,  AnalysisLevel.XgRollerPlusPlus),
            ("m3", 0.460, "2-ply",    20,  AnalysisMode.Evaluation,  AnalysisLevel.Ply2),
            ("m4", 0.450, "B4p12960", 99,  AnalysisMode.BookRollout, AnalysisLevel.Ply4),
            ("m5", 0.440, "1-ply",    10,  AnalysisMode.Evaluation,  AnalysisLevel.Ply1),
            ("m6", 0.430, "4p1296",   104, AnalysisMode.Rollout,     AnalysisLevel.Ply4),
            ("m7", 0.420, "",         0,   AnalysisMode.Unknown,     AnalysisLevel.Unknown),
            ("m8", 0.410, "3-ply",    30,  AnalysisMode.Evaluation,  AnalysisLevel.Ply3),
        };
        return specs.Select(s => new PlayCandidate
        {
            MoveNotation = s.Move,
            Equity = s.Eq,
            EquityLoss = 0.500 - s.Eq,
            DepthAbbreviation = s.Abbr,
            DepthRank = s.Rank,
            AnalysisMode = s.Mode,
            AnalysisLevel = s.Level,
        }).ToList();
    }

    private static DiagramRequest.Builder SolutionBuilder(List<PlayCandidate> plays)
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = plays;
        return b;
    }

    // -----------------------------------------------------------------------
    //  Ordering — DepthFirst
    // -----------------------------------------------------------------------

    [Fact]
    public void DepthFirst_OrdersByDepthRankDescending_RanksStaySourceRanks()
    {
        var b = SolutionBuilder(MixedPlays());
        b.CandidateOrdering = CandidateOrdering.DepthFirst;
        var svg = TestFixtures.Render(b.Build());

        // Ranks descending: 104, 103, 99, 75, 40, 30, 20, 10, 0.
        Assert.Equal(new[] { "m6", "m1", "m4", "m2", "m0", "m8", "m3", "m5", "m7" }, Moves(svg));
        // Each row keeps its source (equity) rank — rank follows the
        // candidate, not the display slot.
        Assert.Equal(new[] { 7, 2, 5, 3, 1, 9, 4, 6, 8 }, Ranks(svg));
    }

    [Fact]
    public void DepthFirst_WithinADepthTier_EquityOrderIsPreserved()
    {
        // Interleaved ranks with ties: the sort must be stable, so within a
        // tier (equal DepthRank) the caller's equity order survives.
        var plays = new List<PlayCandidate>();
        int[] ranks = [2, 103, 2, 103, 2];
        for (int i = 0; i < ranks.Length; i++)
            plays.Add(new PlayCandidate
            {
                MoveNotation = $"m{i}",
                Equity = 0.50 - i * 0.01,
                EquityLoss = i * 0.01,
                DepthRank = ranks[i],
            });

        var b = SolutionBuilder(plays);
        b.CandidateOrdering = CandidateOrdering.DepthFirst;
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(new[] { "m1", "m3", "m0", "m2", "m4" }, Moves(svg));
    }

    [Fact]
    public void DepthFirst_MarksFollowCandidates_UnderTheOrderScramble()
    {
        // The order-scrambling pin: the primary (*) and secondary (†) marks
        // are index-driven and must land on the same candidates after the
        // depth-first sort moves every row.
        var b = SolutionBuilder(MixedPlays());
        b.CandidateOrdering = CandidateOrdering.DepthFirst;
        b.UserPlayIndex = 8;        // m8 — sorts from position 8 to position 5
        b.SecondaryPlayIndex = 3;   // m3 — sorts from position 3 to position 6
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal("m8", MoveOfMarker(svg, "*"));
        Assert.Equal(9, RankOfMarker(svg, "*"));
        Assert.Equal("m3", MoveOfMarker(svg, Dagger));
        Assert.Equal(4, RankOfMarker(svg, Dagger));
    }

    // -----------------------------------------------------------------------
    //  Floor — MinimumCandidateAnalysisLevel
    // -----------------------------------------------------------------------

    [Fact]
    public void Floor_HidesShallowEvaluations_KeepsRolloutsBookUnstampedAndBest()
    {
        // The tester's ask — "4-ply and lower should not be displayed" — is an
        // inclusive floor of Ply5. Hidden: the sub-Ply5 direct evaluations m3
        // (2-ply), m5 (1-ply), m8 (3-ply). Never hidden: the rollouts m1/m6
        // (inner levels Ply3/Ply4 are the rollout's inner level, not its
        // depth), the book hit m4, the unstamped m7, and the best play m0 —
        // itself a 4-ply evaluation, kept by the best-row exemption.
        //
        // This fixture spans the *mode* axis, not the whole level axis: it
        // holds no XG Roller / XG Roller+ row, so this pin says nothing about
        // where a Ply5 floor cuts the interleaved order. Floor_ShowsExactly-
        // TheLevelsAtOrAboveIt and its Ply5 sibling below carry that claim.
        var b = SolutionBuilder(MixedPlays());
        b.MinimumCandidateAnalysisLevel = AnalysisLevel.Ply5;
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(new[] { "m0", "m1", "m2", "m4", "m6", "m7" }, Moves(svg));
        // Survivors keep their true source ranks.
        Assert.Equal(new[] { 1, 2, 3, 5, 7, 8 }, Ranks(svg));
    }

    [Fact]
    public void Floor_NeverHidesUserOrSecondaryRows_WhateverTheirDepth()
    {
        // Floor at the top of the level axis: every direct evaluation below
        // R++ is floor-eligible. The user's play (1-ply) and the secondary
        // (2-ply) must survive anyway — review must always show what was
        // played — while the equally shallow m8 (3-ply, unmarked) is hidden.
        var b = SolutionBuilder(MixedPlays());
        b.MinimumCandidateAnalysisLevel = AnalysisLevel.XgRollerPlusPlus;
        b.UserPlayIndex = 5;        // m5, 1-ply evaluation
        b.SecondaryPlayIndex = 3;   // m3, 2-ply evaluation
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(new[] { "m0", "m1", "m2", "m3", "m4", "m5", "m6", "m7" }, Moves(svg));
        Assert.Equal("m5", MoveOfMarker(svg, "*"));
        Assert.Equal("m3", MoveOfMarker(svg, Dagger));
    }

    [Fact]
    public void FloorAndDepthFirst_Compose()
    {
        // Both options at once: sort by depth, hide the shallow evaluations.
        var b = SolutionBuilder(MixedPlays());
        b.CandidateOrdering = CandidateOrdering.DepthFirst;
        b.MinimumCandidateAnalysisLevel = AnalysisLevel.Ply5;
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(new[] { "m6", "m1", "m4", "m2", "m0", "m7" }, Moves(svg));
    }

    // -----------------------------------------------------------------------
    //  Floor — the interleaved level axis (halheinrich/backgammon#159)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Every <see cref="AnalysisLevel"/> member except
    /// <see cref="AnalysisLevel.Unknown"/>, one direct-evaluation candidate
    /// each, listed in <b>descending</b> rigor — the reverse of the enum's
    /// contractual declaration order, in which the ply family and the XG
    /// Roller family interleave: 1-ply, 2-ply, 3-ply Red, 3-ply, XG Roller,
    /// 4-ply, XG Roller+, 5-ply, 6-ply, 7-ply, XG Roller++.
    /// <para>
    /// Descending is deliberate. <see cref="DiagramRequest.Builder.BestPlayIndex"/>
    /// defaults to 0 and the best row is floor-exempt, so index 0 must be a
    /// row no floor could hide anyway — the top of the scale. Every other row
    /// is then honestly floor-eligible and the exemption masks nothing.
    /// </para>
    /// <para>
    /// Ranks are the producer's decade grid; abbreviations and move notation
    /// are the level's own name, so a failure names the level that moved.
    /// </para>
    /// </summary>
    private static readonly (AnalysisLevel Level, int Rank, string Abbr)[] LevelLadder =
    [
        (AnalysisLevel.XgRollerPlusPlus, 75, "R++"),
        (AnalysisLevel.Ply7,             70, "7-ply"),
        (AnalysisLevel.Ply6,             60, "6-ply"),
        (AnalysisLevel.Ply5,             50, "5-ply"),
        (AnalysisLevel.XgRollerPlus,     45, "R+"),
        (AnalysisLevel.Ply4,             40, "4-ply"),
        (AnalysisLevel.XgRoller,         35, "R"),
        (AnalysisLevel.Ply3,             30, "3-ply"),
        (AnalysisLevel.Ply3Red,          25, "3-ply Red"),
        (AnalysisLevel.Ply2,             20, "2-ply"),
        (AnalysisLevel.Ply1,             10, "1-ply"),
    ];

    /// <summary>The ladder as candidates, in the caller (equity) order given
    /// by <paramref name="order"/> — indices into <see cref="LevelLadder"/>.
    /// Defaults to the ladder's own descending-rigor order.</summary>
    private static List<PlayCandidate> LadderPlays(int[]? order = null)
    {
        order ??= [.. Enumerable.Range(0, LevelLadder.Length)];
        return [.. order.Select((li, row) => new PlayCandidate
        {
            MoveNotation = LevelLadder[li].Level.ToString(),
            Equity = 0.500 - row * 0.010,
            EquityLoss = row * 0.010,
            DepthAbbreviation = LevelLadder[li].Abbr,
            DepthRank = LevelLadder[li].Rank,
            AnalysisMode = AnalysisMode.Evaluation,
            AnalysisLevel = LevelLadder[li].Level,
        })];
    }

    /// <summary>The ladder's level names from the top down to and including
    /// <paramref name="floor"/> — what a floor at that level must leave
    /// visible.</summary>
    private static string[] LadderDownTo(AnalysisLevel floor)
    {
        int cut = Array.FindIndex(LevelLadder, e => e.Level == floor);
        return [.. LevelLadder.Take(cut + 1).Select(e => e.Level.ToString())];
    }

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
    public void Floor_ShowsExactlyTheLevelsAtOrAboveIt(AnalysisLevel floor)
    {
        // The whole-axis pin: for every level the floor may take, the visible
        // set is exactly the members at or above it in AnalysisLevel's
        // contractual ascending-rigor order — the floor is inclusive, so the
        // floor's own level renders. Sweeping all eleven members asserts the
        // interleave itself (halheinrich/backgammon#159): under the
        // superseded plies-below-Rollers order the Roller rows sat above
        // every ply, so most of these cells named a different set.
        var b = SolutionBuilder(LadderPlays());
        b.MinimumCandidateAnalysisLevel = floor;
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(LadderDownTo(floor), Moves(svg));
    }

    [Fact]
    public void Floor_Ply5_HidesTheRollerLevelsBelowIt()
    {
        // The named consequence of the interleave for this repo's own ask.
        // "4-ply and lower should not be displayed" is still an inclusive Ply5
        // floor, but Ply5 no longer sits above only plies: XG Roller (between
        // 3-ply and 4-ply) and XG Roller+ (between 4-ply and 5-ply) are below
        // it too, so the floor now sweeps them out with the shallow plies.
        // Under the superseded order both survived a Ply5 floor.
        var b = SolutionBuilder(LadderPlays());
        b.MinimumCandidateAnalysisLevel = AnalysisLevel.Ply5;
        var svg = TestFixtures.Render(b.Build());

        var visible = Moves(svg);
        Assert.DoesNotContain(nameof(AnalysisLevel.XgRoller), visible);
        Assert.DoesNotContain(nameof(AnalysisLevel.XgRollerPlus), visible);
        Assert.Contains(nameof(AnalysisLevel.Ply5), visible);          // inclusive
        Assert.Contains(nameof(AnalysisLevel.XgRollerPlusPlus), visible);
    }

    [Theory]
    [InlineData(AnalysisLevel.Ply1)]
    [InlineData(AnalysisLevel.Ply2)]
    [InlineData(AnalysisLevel.Ply3)]
    [InlineData(AnalysisLevel.Ply4)]
    [InlineData(AnalysisLevel.Ply5)]
    [InlineData(AnalysisLevel.Ply6)]
    [InlineData(AnalysisLevel.Ply7)]
    public void Floor_XgRollerPlusPlus_SurvivesEveryPlyFloor(AnalysisLevel plyFloor)
    {
        // The other direction of the same boundary: XG Roller++ is the most
        // rigorous level XG offers, so no ply floor — not even 7-ply, the
        // deepest ply — can reach it. The one Roller-family member the
        // interleave left above the whole ply family.
        var b = SolutionBuilder(LadderPlays());
        b.MinimumCandidateAnalysisLevel = plyFloor;
        var svg = TestFixtures.Render(b.Build());

        Assert.Contains(nameof(AnalysisLevel.XgRollerPlusPlus), Moves(svg));
    }

    // -----------------------------------------------------------------------
    //  Ply3Red — the level added with the interleave, end to end
    // -----------------------------------------------------------------------

    [Fact]
    public void Ply3Red_Renders_WithItsOwnProducerLabel()
    {
        // A candidate stamped as XG's reduced-variance 3-ply reaches the panel
        // intact: the producer's abbreviation "3-ply Red" is drawn verbatim.
        // The renderer holds no level→label table of its own — the Depth cell
        // is PlayCandidate.DepthAbbreviation, so the label is the producer's
        // fact travelling through, not a rendering decision.
        var b = SolutionBuilder(LadderPlays());
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal("3-ply Red", DepthOfMove(svg, nameof(AnalysisLevel.Ply3Red)));
    }

    [Fact]
    public void Ply3Red_SortsIntoItsInterleavedPosition_UnderDepthFirst()
    {
        // Depth-first over a scrambled ladder must rebuild the contractual
        // order exactly — which places 3-ply Red (rank 25) above 2-ply (20)
        // and below a full 3-ply (30), XG's own ranking of its
        // reduced-variance search. The producer's decade grid and
        // AnalysisLevel's declaration order agree; this pin is where that
        // agreement is asserted from the consumer side.
        int[] scrambled = [8, 0, 10, 5, 2, 9, 6, 1, 7, 4, 3];
        var b = SolutionBuilder(LadderPlays(scrambled));
        b.CandidateOrdering = CandidateOrdering.DepthFirst;
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal([.. LevelLadder.Select(e => e.Level.ToString())], Moves(svg));
    }

    [Fact]
    public void Ply3Red_IsHiddenByAPly3Floor_AndShownByAPly3RedFloor()
    {
        // The two sides of "3-ply Red is its own level, ranked below a full
        // 3-ply" — not a label variant of Ply3, which would make these two
        // floors indistinguishable. The Ply3Red floor is inclusive, so it
        // shows the row it names; the Ply3 floor is strictly above it.
        var underPly3 = SolutionBuilder(LadderPlays());
        underPly3.MinimumCandidateAnalysisLevel = AnalysisLevel.Ply3;
        var underPly3Red = SolutionBuilder(LadderPlays());
        underPly3Red.MinimumCandidateAnalysisLevel = AnalysisLevel.Ply3Red;

        Assert.DoesNotContain(nameof(AnalysisLevel.Ply3Red), Moves(TestFixtures.Render(underPly3.Build())));
        Assert.Contains(nameof(AnalysisLevel.Ply3Red), Moves(TestFixtures.Render(underPly3Red.Build())));
    }

    // -----------------------------------------------------------------------
    //  Unknown — outside the rigor scale (AnalysisLevel contract, clause (a))
    // -----------------------------------------------------------------------

    /// <summary>
    /// Clause (a) of the <see cref="AnalysisLevel"/> ruling of 2026-08-28:
    /// <see cref="AnalysisLevel.Unknown"/> sits <i>outside</i> the rigor
    /// scale. It is not "the least rigorous level" — it means the level was
    /// not recorded — so it is <b>never excluded by a rigor floor</b>, and
    /// its position at the head of the declaration is the zero-value
    /// requirement, not a rank.
    /// <para>
    /// This repo is that clause's enforcement home, and the floor is the only
    /// rigor comparison in it. The pin matters because the enum's numbering
    /// actively works against the clause: <c>Unknown = 0</c> compares below
    /// every real level, so the ordinal test <c>AnalysisLevel &lt; floor</c>
    /// would hide an Unknown-level row under <em>any</em> floor. Only the
    /// renderer's explicit Unknown guard keeps clause (a); delete it and this
    /// pin is what fires.
    /// </para>
    /// <para>
    /// Both Unknown axes are covered here, because either alone is enough to
    /// mean "not recorded": an <see cref="AnalysisMode.Evaluation"/> whose
    /// level the producer could not decode, and a wholly unstamped row. The
    /// clause's other half — Unknown is "never offered as a rigor threshold"
    /// — is enforced at <see cref="DiagramRequest.Builder.Build"/> and pinned
    /// by <see cref="Build_RejectsUnknownFloor"/>.
    /// </para>
    /// </summary>
    [Fact]
    public void Floor_NeverHidesUnknownLevelRows_WhateverTheFloor()
    {
        var plays = LadderPlays();                       // index 0 = R++, the exempt best row
        plays.Add(new PlayCandidate
        {
            MoveNotation = "decoded-nothing",            // Evaluation, level not recorded
            Equity = 0.100,
            EquityLoss = 0.400,
            DepthAbbreviation = "level-77",
            DepthRank = 0,
            AnalysisMode = AnalysisMode.Evaluation,
            AnalysisLevel = AnalysisLevel.Unknown,
        });
        plays.Add(new PlayCandidate
        {
            MoveNotation = "unstamped",                  // neither axis recorded
            Equity = 0.090,
            EquityLoss = 0.410,
            AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown,
        });

        // The most rigorous floor there is — everything on the scale below
        // R++ goes. The two Unknown rows are not on the scale, so they stay.
        var b = SolutionBuilder(plays);
        b.MinimumCandidateAnalysisLevel = AnalysisLevel.XgRollerPlusPlus;
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(
            [nameof(AnalysisLevel.XgRollerPlusPlus), "decoded-nothing", "unstamped"],
            Moves(svg));
    }

    // -----------------------------------------------------------------------
    //  Window rescue — the treatment never pushes best or marked out of view
    // -----------------------------------------------------------------------

    /// <summary>Self-calibrating fitCount, as in DualPlayMarkerTests.</summary>
    private static int MeasureFitCount(int playCount)
    {
        var plays = new List<PlayCandidate>();
        for (int i = 0; i < playCount; i++)
            plays.Add(new PlayCandidate
            {
                MoveNotation = $"m{i}",
                Equity = 0.50 - i * 0.001,
                EquityLoss = i * 0.001,
                DepthRank = 103,
            });
        return Ranks(TestFixtures.Render(SolutionBuilder(plays).Build())).Count;
    }

    [Fact]
    public void DepthFirst_BestPlaySortedBeyondTheCut_IsRescuedWithTrueRank()
    {
        int fit = MeasureFitCount(playCount: 100);
        Assert.True(fit >= 2, $"Panel must fit at least 2 rows; measured {fit}.");

        // Best play is a shallow evaluation; everything below it is a rollout.
        // Depth-first sorts the best play to the very last display position,
        // beyond the window — the depth treatment must rescue it into view
        // with its true rank 1.
        int total = fit + 10;
        var plays = new List<PlayCandidate>();
        for (int i = 0; i < total; i++)
            plays.Add(new PlayCandidate
            {
                MoveNotation = $"m{i}",
                Equity = 0.50 - i * 0.001,
                EquityLoss = i * 0.001,
                DepthRank = i == 0 ? 10 : 103,
                AnalysisMode = i == 0 ? AnalysisMode.Evaluation : AnalysisMode.Rollout,
                AnalysisLevel = i == 0 ? AnalysisLevel.Ply1 : AnalysisLevel.Ply3,
            });

        var b = SolutionBuilder(plays);
        b.CandidateOrdering = CandidateOrdering.DepthFirst;
        var svg = TestFixtures.Render(b.Build());

        var ranks = Ranks(svg);
        Assert.Equal(fit, ranks.Count);   // panel didn't grow to fit it
        Assert.Equal(1, ranks[^1]);       // best play rescued to the foot, true rank
        Assert.Equal(2, ranks[0]);        // deepest-first body starts at the first rollout
    }

    [Fact]
    public void DefaultOptions_BestPlayBeyondTheCut_KeepsTheLegacyWindow()
    {
        // The byte-compat contract's one deliberate fork: best-play rescue is
        // part of the depth treatment, so with both options at their defaults
        // the window trims exactly as it always has — marked rows only. A
        // BestPlayIndex beyond the cut (which contract-conforming,
        // equity-sorted data never produces) is trimmed under defaults, not
        // rescued; options-unset output stays byte-identical to the
        // pre-treatment renderer for every input.
        int fit = MeasureFitCount(playCount: 100);
        Assert.True(fit >= 2, $"Panel must fit at least 2 rows; measured {fit}.");

        int total = fit + 10;
        var plays = new List<PlayCandidate>();
        for (int i = 0; i < total; i++)
            plays.Add(new PlayCandidate
            {
                MoveNotation = $"m{i}",
                Equity = 0.50 - i * 0.001,
                EquityLoss = i * 0.001,
                DepthRank = 103,
            });

        var b = SolutionBuilder(plays);
        b.BestPlayIndex = total - 1;   // beyond the window; options at defaults
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(Enumerable.Range(1, fit).ToArray(), Ranks(svg));
    }

    // -----------------------------------------------------------------------
    //  Inertness — defaults and vacuous settings change nothing
    // -----------------------------------------------------------------------

    [Fact]
    public void VacuousFloor_IsByteIdenticalToUnset()
    {
        // A floor of Ply1 hides nothing (no direct evaluation sits strictly
        // below Ply1, and Unknown-level rows are excluded by rule), so the
        // active-but-vacuous machinery must be byte-invisible.
        var unset = SolutionBuilder(MixedPlays());
        unset.UserPlayIndex = 1;
        var floored = SolutionBuilder(MixedPlays());
        floored.UserPlayIndex = 1;
        floored.MinimumCandidateAnalysisLevel = AnalysisLevel.Ply1;

        Assert.Equal(TestFixtures.Render(unset.Build()), TestFixtures.Render(floored.Build()));
    }

    [Fact]
    public void DepthFirst_OnAnAlreadyDepthSortedList_IsByteIdenticalToEquity()
    {
        // A list whose caller order is already depth-descending reorders to
        // itself, so the DepthFirst rendering must be byte-identical to the
        // default — the sort machinery adds nothing of its own.
        var plays = new List<PlayCandidate>();
        int[] ranks = [104, 103, 99, 75, 30];
        for (int i = 0; i < ranks.Length; i++)
            plays.Add(new PlayCandidate
            {
                MoveNotation = $"m{i}",
                Equity = 0.50 - i * 0.01,
                EquityLoss = i * 0.01,
                DepthRank = ranks[i],
            });

        var equity = SolutionBuilder(plays);
        equity.UserPlayIndex = 1;
        var depthFirst = SolutionBuilder(plays);
        depthFirst.UserPlayIndex = 1;
        depthFirst.CandidateOrdering = CandidateOrdering.DepthFirst;

        Assert.Equal(TestFixtures.Render(equity.Build()), TestFixtures.Render(depthFirst.Build()));
    }

    // -----------------------------------------------------------------------
    //  Validation — the options are caller configuration
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RejectsUnknownFloor()
    {
        var b = SolutionBuilder(MixedPlays());
        b.MinimumCandidateAnalysisLevel = AnalysisLevel.Unknown;
        var ex = Assert.Throws<InvalidOperationException>(() => b.Build());
        Assert.Contains("Unknown", ex.Message);
    }

    [Fact]
    public void Build_RejectsUndefinedFloorValue()
    {
        var b = SolutionBuilder(MixedPlays());
        b.MinimumCandidateAnalysisLevel = (AnalysisLevel)99;
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void Build_RejectsUndefinedCandidateOrdering()
    {
        var b = SolutionBuilder(MixedPlays());
        b.CandidateOrdering = (CandidateOrdering)99;
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }
}
