using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Themes;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The shared test inputs. Records are built through the producer's
/// <see cref="TestRecords"/>, the one way tests build them, so every record
/// here is one a producer could build — a decision position, candidates valid
/// from it. A board that is not a decision position is drawn through the
/// board path (<see cref="DiagramRequest.ForBoard"/>), never as a record.
/// </summary>
internal static class TestFixtures
{
    /// <summary>
    /// The ranking a test states when the ranking is not its subject: the
    /// default an app without the setting uses.
    /// </summary>
    public const PlayRanking Ranking = PlayRanking.Equity;

    /// <summary>
    /// <see cref="TestRecords.CheckerPlay"/>'s default decision's request, as
    /// a problem: the opening 3-1 from the standard start, Alice (on roll)
    /// against Bob at 0-0 in a 7-point match, in <c>match.xg</c>.
    /// </summary>
    public static DiagramRequest MinimalRequest() => RequestFor(TestRecords.CheckerPlay());

    /// <summary>The request for <paramref name="record"/> in <paramref name="mode"/>, under <see cref="Ranking"/>.</summary>
    public static DiagramRequest RequestFor(BgDecisionData record, DiagramMode mode = DiagramMode.Problem) =>
        DiagramRequest.ForDecision(record, Ranking) with { Mode = mode };

    /// <summary>
    /// A checker play on <paramref name="board"/> — a decision position — with
    /// the one candidate valid from every position, the pass
    /// (<see cref="TestRecords.CheckerPlay"/>'s default for a board other than
    /// the standard start).
    /// </summary>
    public static CheckerPlayDecision CheckerPlayOn(
        BoardPosition board, Session? session = null, int cubeSize = 1, CubeOwner cubeOwner = CubeOwner.Centered) =>
        TestRecords.CheckerPlay(position: TestRecords.Position(mop: board, cubeSize: cubeSize, cubeOwner: cubeOwner, session: session));

    /// <summary>
    /// A checker play from the standard start (or <paramref name="board"/>)
    /// holding <paramref name="plays"/>, each valid from it, with the user's
    /// play at <paramref name="userPlayIndex"/>.
    /// </summary>
    public static CheckerPlayDecision CheckerPlayWith(
        IReadOnlyList<PlayCandidate> plays, int? userPlayIndex = 0, IReadOnlyList<int>? dice = null,
        BoardPosition? board = null) =>
        TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: board),
            decision: TestRecords.CheckerPlayData(dice: dice, plays: plays, userPlayIndex: userPlayIndex));

    /// <summary>
    /// A cube decision — <see cref="TestRecords.Cube"/>'s short race — with
    /// the two equities the analysis turns on and the played actions.
    /// </summary>
    public static CubeDecision CubeWith(
        double noDoubleEquity, double doubleTakeEquity,
        CubeAction? userDoublerAction = CubeAction.Double, CubeAction? userTakerAction = CubeAction.Take,
        AnalysisMode analysisMode = AnalysisMode.Evaluation, AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        int? rolloutTrials = null) =>
        TestRecords.Cube(decision: TestRecords.CubeData(
            analysisMode: analysisMode, analysisLevel: analysisLevel, rolloutTrials: rolloutTrials,
            noDoubleEquity: noDoubleEquity, doubleTakeEquity: doubleTakeEquity,
            userDoublerAction: userDoublerAction, userTakerAction: userTakerAction));

    /// <summary>
    /// A board from slot counts in <see cref="BoardPosition"/>'s layout: slot
    /// 0 the opponent's bar, 1–24 the points, 25 the on-roll player's bar;
    /// positive counts on roll, negative the opponent's. Unnamed slots are 0.
    /// </summary>
    public static BoardPosition Board(params (int Slot, int Count)[] counts)
    {
        var slots = new int[26];
        foreach (var (slot, count) in counts)
            slots[slot] = count;
        return new BoardPosition(slots);
    }

    /// <summary>
    /// Every single-hop play valid from the standard start, in a fixed order:
    /// each of the on-roll player's four stacks (24, 13, 8, 6, highest first)
    /// to each lower point the opponent does not hold, then off. Valid, not
    /// legal — the dice are the move generator's business, not a record's —
    /// and pairwise distinct, so each reaches its own position and writes its
    /// own notation: enough distinct candidates to overfill the play panel.
    /// </summary>
    public static IReadOnlyList<Play> StandardStartHops { get; } = BuildStandardStartHops();

    private static List<Play> BuildStandardStartHops()
    {
        var start = BoardPosition.Standard;
        var hops = new List<Play>();
        foreach (int from in (int[])[24, 13, 8, 6])
        {
            for (int to = from - 1; to >= 1; to--)
            {
                if (start[to] > -2)
                    hops.Add([new Move(from, to)]);
            }
            hops.Add([new Move(from, 0)]);
        }
        return hops;
    }

    /// <summary>
    /// <paramref name="count"/> candidates from the standard start, each a
    /// distinct hop (<see cref="StandardStartHops"/>), at equities falling by
    /// <paramref name="step"/> from <paramref name="best"/> in stored order.
    /// </summary>
    public static List<PlayCandidate> HopCandidates(int count, double best = 0.5, double step = 0.01) =>
        [.. StandardStartHops.Take(count).Select((play, i) => TestRecords.Candidate(play: play, equity: best - i * step))];

    public static DiagramOptions DefaultOptions() => new()
    {
        Size = DiagramSize.Medium
    };

    public static DiagramOptions GreyscaleOptions() => new()
    {
        Size = DiagramSize.Medium,
        Theme = ThemeRegistry.Greyscale
    };

    public static string Render(DiagramRequest? req = null, DiagramOptions? opts = null)
        => DiagramRenderer.RenderSvg(req ?? MinimalRequest(), opts ?? DefaultOptions());

    /// <summary>
    /// The text on the cube's face in <paramref name="svg"/>: the text drawn
    /// right after the cube's square, the one rect with a corner radius of
    /// exactly 3.
    /// </summary>
    public static string CubeFace(string svg)
    {
        var match = System.Text.RegularExpressions.Regex.Match(svg,
            """<rect [^>]*rx="3" [^>]*/>\s*<text [^>]*>([^<]*)</text>""");
        Assert.True(match.Success, "No cube face found.");
        return match.Groups[1].Value;
    }

    public static int CountOccurrences(string source, string pattern)
    {
        int count = 0, index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        { count++; index += pattern.Length; }
        return count;
    }
}
