using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Showing a play (SPEC-quiz-view §1, amended 2026-10-25;
/// halheinrich/backgammon#440): in a solution, <see cref="DiagramRequest.ShownPlayIndex"/>
/// draws the position the candidate leaves — its checkers, pip counts and
/// trays — and highlights its row, while the roll, the names, the cube and
/// the score stay the record's. In a problem it changes nothing.
/// </summary>
public class ShownPlayTests
{
    /// <summary>A solution over <paramref name="playCount"/> distinct candidates in equity order, the first the user's.</summary>
    private static DiagramRequest Solution(int playCount, int? userPlayIndex = 0) =>
        TestFixtures.RequestFor(
            TestFixtures.CheckerPlayWith(TestFixtures.HopCandidates(playCount), userPlayIndex),
            DiagramMode.Solution);

    /// <summary>Everything drawn before the board's translate group: the title strip.</summary>
    private static string TitleStrip(string svg) => svg[..svg.IndexOf("<g transform=", StringComparison.Ordinal)];

    /// <summary>The rails' and the title strip's texts (all set at 12 px), pip labels apart.</summary>
    private static List<string> LabelsOtherThanPips(string svg) =>
        [.. Regex.Matches(svg, "<text [^>]*font-size=\"12\"[^>]*>([^<]*)</text>")
            .Select(m => m.Groups[1].Value)
            .Where(text => !text.StartsWith("Pip: ", StringComparison.Ordinal))];

    private static string PipLabel(int pips) => $"Pip: {pips}";

    // -----------------------------------------------------------------------
    //  The board: the shown position; everything else the record's
    // -----------------------------------------------------------------------

    [Fact]
    public void ShownPlay_KeepsTheRecordsTitle_NamesAndCube()
    {
        var played = TestFixtures.RequestFor(TestRecords.CheckerPlay(), DiagramMode.Solution);
        string playedSvg = TestFixtures.Render(played);
        string shownSvg = TestFixtures.Render(played with { ShownPlayIndex = 1 });

        // The title strip still reads the roll, the source and the position.
        Assert.Equal(TitleStrip(playedSvg), TitleStrip(shownSvg));
        Assert.Contains("to play", TitleStrip(shownSvg));
        // The names and the cube are the record's.
        Assert.Equal(LabelsOtherThanPips(playedSvg), LabelsOtherThanPips(shownSvg));
        Assert.Equal(TestFixtures.CubeFace(playedSvg), TestFixtures.CubeFace(shownSvg));
    }

    [Fact]
    public void ShownPlay_TheRailsPipCounts_AreTheShownPositions()
    {
        var record = TestRecords.CheckerPlay();
        var request = TestFixtures.RequestFor(record, DiagramMode.Solution) with { ShownPlayIndex = 1 };
        var before = new BoardState(record.Board);
        var after = new BoardState(record.AfterBoardOf(1).Flipped());
        Assert.NotEqual(before.PipCount, after.PipCount);   // premise: the play moves the mover's pips

        string svg = TestFixtures.Render(request);

        Assert.Contains(PipLabel(after.PipCount), svg);
        Assert.Contains(PipLabel(after.OpponentPipCount), svg);
        // The opening position's 167 is now the opponent's alone.
        Assert.Equal(1, TestFixtures.CountOccurrences(svg, PipLabel(before.PipCount)));
    }

    [Fact]
    public void ShownBearOff_ChangesWhichTraysAreDrawnAndHitTested_Together()
    {
        // The mover has one checker left, on the 1-point (14 off); candidate 0
        // bears it off, leaving every checker off, which draws no tray.
        const string BarSignature = "stroke=\"#888\" stroke-width=\"0.5\"/>";
        var board = TestFixtures.Board((1, 1), (24, -BoardPosition.CheckersPerSide));
        var record = TestFixtures.CheckerPlayWith(
            [TestRecords.Candidate(play: [new Move(1, 0)]), TestRecords.Candidate(play: [])],
            board: board);
        var played = TestFixtures.RequestFor(record, DiagramMode.Solution);
        var shown = played with { ShownPlayIndex = 0 };
        Assert.Equal(BoardPosition.CheckersPerSide, shown.Board.OnRollBorneOffCount);

        // Played: the tray holds 14 bars, beside the cube, and is hit-tested.
        Assert.Equal(1 + 14, TestFixtures.CountOccurrences(TestFixtures.Render(played), BarSignature));
        Assert.NotNull(DiagramRenderer.GetHitRegions(played, new DiagramOptions()).OnRollTray);

        // Shown: neither drawn nor hit-tested.
        Assert.Equal(1, TestFixtures.CountOccurrences(TestFixtures.Render(shown), BarSignature));
        Assert.Null(DiagramRenderer.GetHitRegions(shown, new DiagramOptions()).OnRollTray);
    }

    [Fact]
    public void ShownPlay_InProblem_ChangesNothingDrawn()
    {
        var problem = TestFixtures.RequestFor(TestRecords.CheckerPlay());

        Assert.Equal(TestFixtures.Render(problem), TestFixtures.Render(problem with { ShownPlayIndex = 1 }));
    }

    // -----------------------------------------------------------------------
    //  The highlight
    // -----------------------------------------------------------------------

    [Fact]
    public void Highlight_IsOnExactlyTheShownCandidatesRow()
    {
        var rows = PlayPanelReader.Rows(TestFixtures.Render(Solution(5) with { ShownPlayIndex = 2 }));

        var highlighted = Assert.Single(rows, r => r.Highlighted);
        Assert.Equal(3, highlighted.Rank);
        Assert.Equal(TestFixtures.StandardStartHops[2].ToNotation(), highlighted.Move);
    }

    [Fact]
    public void Highlight_IsAbsentWithoutAShownPlay()
    {
        var rows = PlayPanelReader.Rows(TestFixtures.Render(Solution(5)));

        Assert.DoesNotContain(rows, r => r.Highlighted);
    }

    [Fact]
    public void Highlight_ComposesWithTheMarks()
    {
        // The user's row shown: highlighted, and still marked *.
        var row = PlayPanelReader.RowMarked(
            TestFixtures.Render(Solution(5, userPlayIndex: 1) with { ShownPlayIndex = 1 }), "*");

        Assert.True(row.Highlighted);
        Assert.Equal(2, row.Rank);
    }

    [Fact]
    public void Highlight_IsDrawnBeforeTheRowsText()
    {
        // The rect comes first, so the row's text is drawn over it.
        string svg = TestFixtures.Render(Solution(5) with { ShownPlayIndex = 2 });
        var row = PlayPanelReader.Rows(svg).Single(r => r.Highlighted);
        string top = Regex.Escape(SvgFormat.Number(row.Baseline - DiagramRenderer.PlayPanelLineHeight * 0.8));
        string height = Regex.Escape(SvgFormat.Number(DiagramRenderer.PlayPanelLineHeight));
        string baseline = Regex.Escape(SvgFormat.Number(row.Baseline));

        // The highlight rect, then at once the row's first text.
        Assert.Matches(new Regex($"<rect [^>]*y=\"{top}\" [^>]*height=\"{height}\"[^>]*/>\\s*<text [^>]*y=\"{baseline}\""), svg);
    }

    [Fact]
    public void ShownPlay_AtOrBelowTheCeiling_IsStillDrawn()
    {
        // Every hop is a 3-ply evaluation, so a 3-ply ceiling hides all but
        // the best play (also the user's) — and the shown play.
        var hidden = Solution(5) with { MaximumHiddenCandidateAnalysisLevel = AnalysisLevel.Ply3 };
        Assert.Equal([1], PlayPanelReader.Ranks(TestFixtures.Render(hidden)));

        var rows = PlayPanelReader.Rows(TestFixtures.Render(hidden with { ShownPlayIndex = 3 }));

        Assert.Equal([1, 4], rows.Select(r => r.Rank));
        Assert.True(rows[1].Highlighted);
    }
}
