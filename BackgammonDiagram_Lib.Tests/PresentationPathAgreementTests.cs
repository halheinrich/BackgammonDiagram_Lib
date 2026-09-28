using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// One score draws one way, whichever path presents it (Hal's ruling of
/// 2026-09-28 on halheinrich/backgammon#273): a board's display facts may
/// state a match's Crawford status and a money session's Jacoby rule, and
/// the diagram words them — rails and cube face, double match point worded
/// from the away scores, never stated — by the one rule a decision's
/// session goes through. It validates none of them, and no display fact
/// brings a decision's cube prompt.
/// </summary>
public class PresentationPathAgreementTests
{
    /// <summary>A score both paths can present: the record's session and the board's stated score.</summary>
    public sealed record Case(string Name, Session Session, RailScore Score)
    {
        public override string ToString() => Name;
    }

    public static TheoryData<Case> Scores() => new()
    {
        new Case("ordinary match", TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 5),
            new MatchRailScore(3, 5)),
        new Case("double match point", TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 1),
            new MatchRailScore(1, 1)),
        new Case("Crawford game", TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 4, isCrawford: true),
            new MatchRailScore(1, 4, isCrawford: true)),
        new Case("money, Jacoby", TestRecords.MoneySession(isJacoby: true), new MoneyRailScore(isJacoby: true)),
        new Case("money, no Jacoby", TestRecords.MoneySession(isJacoby: false), new MoneyRailScore(isJacoby: false)),
    };

    /// <summary>The decision on the standard start in <paramref name="session"/>, the cube at 2 on the player on roll's side.</summary>
    private static string RenderDecision(Session session) => TestFixtures.Render(TestFixtures.RequestFor(
        TestFixtures.CheckerPlayOn(BoardPosition.Standard, session: session, cubeSize: 2, cubeOwner: CubeOwner.OnRoll)));

    /// <summary>The same board and cube, and the default record's names, stated as display facts with <paramref name="score"/>.</summary>
    private static string RenderBoard(RailScore? score, DiceFaces? dice = null) =>
        TestFixtures.Render(DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts
        {
            OnRollName = "Alice",
            OpponentName = "Bob",
            CubeValue = 2,
            CubeOwner = CubeOwner.OnRoll,
            Score = score,
            Dice = dice,
        }));

    /// <summary>The rails' player labels, in document order: the bold, centred texts naming a player.</summary>
    private static List<string> PlayerLabels(string svg) =>
        [.. Regex.Matches(svg, """<text [^>]*dominant-baseline="central"[^>]*font-weight="bold"[^>]*>((?:Alice|Bob)[^<]*)</text>""")
            .Select(m => m.Groups[1].Value)];

    [Theory]
    [MemberData(nameof(Scores))]
    public void ABoardAndADecision_WithTheSameScore_DrawTheSameRailsAndCube(Case score)
    {
        var decision = RenderDecision(score.Session);
        var board = RenderBoard(score.Score);

        Assert.Equal(2, PlayerLabels(decision).Count);
        Assert.Equal(PlayerLabels(decision), PlayerLabels(board));
        Assert.Equal(TestFixtures.CubeFace(decision), TestFixtures.CubeFace(board));
    }

    [Fact]
    public void DoubleMatchPoint_IsWordedFromTheAwayScores_OnABoard()
    {
        // 1-away/1-away, stated as a score and nothing more: the cube reads
        // Dmp, as a decision's does — derived, never a flag.
        var svg = RenderBoard(new MatchRailScore(1, 1));

        Assert.Equal(DiagramPresentation.DoubleMatchPointFace, TestFixtures.CubeFace(svg));
        Assert.Equal(["Bob needs 1", "Alice needs 1"], PlayerLabels(svg));
    }

    [Fact]
    public void CrawfordStatus_IsDrawnOnTheRailsAndTheCube_OnABoard()
    {
        var svg = RenderBoard(new MatchRailScore(1, 4, isCrawford: true));

        Assert.Equal(DiagramPresentation.CrawfordFace, TestFixtures.CubeFace(svg));
        Assert.Equal(["Bob needs 4 Crawford", "Alice needs 1 Crawford"], PlayerLabels(svg));
    }

    [Theory]
    [InlineData(true, "(Money Game, Jacoby)")]
    [InlineData(false, "(Money Game, No Jacoby)")]
    [InlineData(null, "(Money Game)")]
    public void JacobyRule_IsDrawnAsStated_OnABoard(bool? isJacoby, string label)
    {
        // Stated either way, or not at all — the bare label, where a board's
        // source states no rule.
        var svg = RenderBoard(new MoneyRailScore(isJacoby));

        Assert.Equal([$"Bob {label}", $"Alice {label}"], PlayerLabels(svg));
        Assert.Equal("2", TestFixtures.CubeFace(svg));
    }

    // -----------------------------------------------------------------------
    //  Consumed, not validated
    // -----------------------------------------------------------------------

    [Fact]
    public void CrawfordStatus_AtAScoreItCouldNotOccur_DrawsAsGiven()
    {
        // No player 1-away, so no Crawford game could be in play: the diagram
        // holds the stated fact to no rule of the game.
        var svg = RenderBoard(new MatchRailScore(5, 3, isCrawford: true));

        Assert.Equal(DiagramPresentation.CrawfordFace, TestFixtures.CubeFace(svg));
        Assert.Equal(["Bob needs 3 Crawford", "Alice needs 5 Crawford"], PlayerLabels(svg));
    }

    [Fact]
    public void DoubleMatchPoint_TakesPrecedenceOverAStatedCrawfordStatus()
    {
        // Both 1-away with Crawford stated — no Crawford game has that score,
        // but it draws as given, and the cube reads Dmp, as a decision's
        // would: the precedence is the one rule's.
        var svg = RenderBoard(new MatchRailScore(1, 1, isCrawford: true));

        Assert.Equal(DiagramPresentation.DoubleMatchPointFace, TestFixtures.CubeFace(svg));
        Assert.Equal(["Bob needs 1 Crawford", "Alice needs 1 Crawford"], PlayerLabels(svg));
    }

    // -----------------------------------------------------------------------
    //  The cube prompt stays a decision's
    // -----------------------------------------------------------------------

    public static TheoryData<string> BoardScores() => new() { "none", "match", "double match point", "Crawford", "money" };

    private static RailScore? BoardScore(string name) => name switch
    {
        "match" => new MatchRailScore(3, 5),
        "double match point" => new MatchRailScore(1, 1),
        "Crawford" => new MatchRailScore(1, 4, isCrawford: true),
        "money" => new MoneyRailScore(isJacoby: true),
        _ => null,
    };

    [Theory]
    [MemberData(nameof(BoardScores))]
    public void NoDisplayFact_BringsTheCubePrompt(string score)
    {
        // With dice or without, whatever the score: the cube prompt words a
        // decision's kind, which no display fact states.
        Assert.DoesNotContain(DiagramPresentation.CubeActionPrompt, RenderBoard(BoardScore(score)));
        Assert.DoesNotContain(DiagramPresentation.CubeActionPrompt, RenderBoard(BoardScore(score), new DiceFaces(3, 1)));
    }
}
