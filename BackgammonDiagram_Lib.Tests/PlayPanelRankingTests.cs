using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The play panel draws a checker play's candidates by the request's
/// ranking — BgDataTypes_Lib's one definition of the best play (SPEC-scoring
/// §2a): the candidates arrive in the analyser's stored order, and the
/// ranking decides the order they are drawn in, their rank numbers, which is
/// best, and every error. A candidate the ranking does not score shows no
/// error and never reads like the best play. Each candidate's move text is
/// its play's notation.
/// </summary>
public class PlayPanelRankingTests
{
    private static string Notation(int hop) => TestFixtures.StandardStartHops[hop].ToNotation();

    private static string RenderSolution(CheckerPlayDecision record, PlayRanking ranking) =>
        TestFixtures.Render(DiagramRequest.ForDecision(record, ranking) with { Mode = DiagramMode.Solution });

    // -----------------------------------------------------------------------
    //  Equity: the stored order is not the drawn order
    // -----------------------------------------------------------------------

    /// <summary>Three candidates stored out of equity order: 0.40, 0.50, 0.45.</summary>
    private static CheckerPlayDecision StoredOutOfEquityOrder(int? userPlayIndex = null) => TestFixtures.CheckerPlayWith(
    [
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[0], equity: 0.40),
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[1], equity: 0.50),
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[2], equity: 0.45),
    ], userPlayIndex);

    [Fact]
    public void Equity_DrawsTheCandidatesInTheRankingsOrder_NotTheStoredOrder()
    {
        var rows = PlayPanelReader.Rows(RenderSolution(StoredOutOfEquityOrder(), PlayRanking.Equity));

        Assert.Equal([Notation(1), Notation(2), Notation(0)], rows.Select(r => r.Move));
        Assert.Equal([1, 2, 3], rows.Select(r => r.Rank));
    }

    [Fact]
    public void Equity_TheBestPlayIsTheRankingsFirst_AndErrorsAreMeasuredAgainstIt()
    {
        var rows = PlayPanelReader.Rows(RenderSolution(StoredOutOfEquityOrder(), PlayRanking.Equity));

        // The best play's Eq Loss cell is blank; each other is its error
        // against the best, the producer's figure.
        Assert.Equal([null, "0.0500", "0.1000"], rows.Select(r => r.Loss));
    }

    [Fact]
    public void TheUsersMark_FollowsItsCandidate_WithItsRankingRank()
    {
        // Stored third, ranked second: the mark is keyed to the candidate.
        var row = PlayPanelReader.RowMarked(RenderSolution(StoredOutOfEquityOrder(userPlayIndex: 2), PlayRanking.Equity), "*");

        Assert.Equal(Notation(2), row.Move);
        Assert.Equal(2, row.Rank);
    }

    // -----------------------------------------------------------------------
    //  Depth first: another best, other numbers, other errors
    // -----------------------------------------------------------------------

    /// <summary>
    /// A 3-ply evaluation rating highest (0.50), then two 1296-trial
    /// rollouts (0.45, 0.40) — deeper, and rating lower.
    /// </summary>
    private static CheckerPlayDecision EvaluationAboveRollouts() => TestFixtures.CheckerPlayWith(
    [
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[0], equity: 0.50),
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[1], equity: 0.45,
            analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296),
        TestRecords.Candidate(play: TestFixtures.StandardStartHops[2], equity: 0.40,
            analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296),
    ], userPlayIndex: null);

    [Fact]
    public void DepthFirst_OrdersAndNumbersByTheDepthFirstRanking()
    {
        var rows = PlayPanelReader.Rows(RenderSolution(EvaluationAboveRollouts(), PlayRanking.DepthFirst));

        // Deepest first, then by equity within a depth: the rollouts, then
        // the evaluation — numbered 1 to 3 in that order.
        Assert.Equal([Notation(1), Notation(2), Notation(0)], rows.Select(r => r.Move));
        Assert.Equal([1, 2, 3], rows.Select(r => r.Rank));
    }

    [Fact]
    public void DepthFirst_MeasuresErrorsAgainstItsOwnBest()
    {
        var rows = PlayPanelReader.Rows(RenderSolution(EvaluationAboveRollouts(), PlayRanking.DepthFirst));

        // The deeper rollout is best (blank); the other is 0.05 behind it.
        Assert.Null(rows[0].Loss);
        Assert.Equal("0.0500", rows[1].Loss);
    }

    [Fact]
    public void DepthFirst_ACandidateTheRankingDoesNotScore_ShowsTheMark()
    {
        // The evaluation is at another depth from the best and rates higher
        // than it: the ranking does not score it, so it has no error. Its row
        // shows the not-scored mark — never an error, and never the best's
        // blank cell.
        var record = EvaluationAboveRollouts();
        var ranked = record.Decision.RankedBy(PlayRanking.DepthFirst);
        Assert.False(ranked.ForCandidate(0).IsScored);

        var row = PlayPanelReader.Rows(RenderSolution(record, PlayRanking.DepthFirst))
            .Single(r => r.Move == Notation(0));

        Assert.Equal(DiagramRenderer.PlayPanelNotScoredMark, row.Loss);
        Assert.Equal(3, row.Rank);
    }

    [Fact]
    public void Equity_ScoresEveryCandidate_SoNoMarkIsShown()
    {
        // The same candidates under equity: the evaluation is best, every
        // candidate is scored, and no row carries the mark.
        var rows = PlayPanelReader.Rows(RenderSolution(EvaluationAboveRollouts(), PlayRanking.Equity));

        Assert.Equal([Notation(0), Notation(1), Notation(2)], rows.Select(r => r.Move));
        Assert.Equal([null, "0.0500", "0.1000"], rows.Select(r => r.Loss));
        Assert.DoesNotContain(DiagramRenderer.PlayPanelNotScoredMark, RenderSolution(EvaluationAboveRollouts(), PlayRanking.Equity));
    }

    [Fact]
    public void TheRequestsRanking_IsTheOneDrawn()
    {
        // Varying only the request's ranking changes the drawn best play: the
        // diagram draws the ranking the caller states, and assumes none.
        var request = TestFixtures.RequestFor(EvaluationAboveRollouts(), DiagramMode.Solution);

        Assert.Equal(Notation(0), PlayPanelReader.Rows(TestFixtures.Render(request with { Ranking = PlayRanking.Equity }))[0].Move);
        Assert.Equal(Notation(1), PlayPanelReader.Rows(TestFixtures.Render(request with { Ranking = PlayRanking.DepthFirst }))[0].Move);
    }

    // -----------------------------------------------------------------------
    //  Notation from the play
    // -----------------------------------------------------------------------

    [Fact]
    public void MoveText_IsThePlaysNotation()
    {
        // Encoded out of canonical order — the 6-point hop first — and with
        // the hit mark on the second checker to reach the 3-point: the text is
        // the play's own notation, which orders the chains and places the
        // mark on its point's carrier, not a rendering of the encoding.
        var board = TestFixtures.Board(
            (1, -2), (3, -1), (12, -4), (17, -3), (19, -5), (24, 2), (13, 5), (8, 3), (7, 1), (6, 4));
        Play encoded = [new Move(6, 5), new Move(8, 5), new Move(8, 3), new Move(7, -3)];
        var record = TestFixtures.CheckerPlayWith([TestRecords.Candidate(play: encoded)], board: board);

        var move = Assert.Single(PlayPanelReader.Rows(RenderSolution(record, PlayRanking.Equity))).Move;

        Assert.Equal(encoded.ToNotation(), move);
        Assert.Equal("8/5 8/3* 7/3 6/5", move);
    }
}
