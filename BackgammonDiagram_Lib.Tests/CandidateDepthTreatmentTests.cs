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
    /// book hit, and an unstamped row. DepthRank values follow the producer's
    /// scale (ply → N, Roller family → 20–22, book → 99, rollout → 100+inner).
    /// </summary>
    private static List<PlayCandidate> MixedPlays()
    {
        var specs = new (string Move, double Eq, string Abbr, int Rank, AnalysisMode Mode, AnalysisLevel Level)[]
        {
            ("m0", 0.500, "4-ply",    4,   AnalysisMode.Evaluation,  AnalysisLevel.Ply4),
            ("m1", 0.480, "3p1296",   103, AnalysisMode.Rollout,     AnalysisLevel.Ply3),
            ("m2", 0.470, "R++",      22,  AnalysisMode.Evaluation,  AnalysisLevel.XgRollerPlusPlus),
            ("m3", 0.460, "2-ply",    2,   AnalysisMode.Evaluation,  AnalysisLevel.Ply2),
            ("m4", 0.450, "B4p12960", 99,  AnalysisMode.BookRollout, AnalysisLevel.Ply4),
            ("m5", 0.440, "1-ply",    1,   AnalysisMode.Evaluation,  AnalysisLevel.Ply1),
            ("m6", 0.430, "4p1296",   104, AnalysisMode.Rollout,     AnalysisLevel.Ply4),
            ("m7", 0.420, "",         0,   AnalysisMode.Unknown,     AnalysisLevel.Unknown),
            ("m8", 0.410, "3-ply",    3,   AnalysisMode.Evaluation,  AnalysisLevel.Ply3),
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

        // Ranks descending: 104, 103, 99, 22, 4, 3, 2, 1, 0.
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
                DepthRank = i == 0 ? 1 : 103,
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
        int[] ranks = [104, 103, 99, 22, 3];
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
