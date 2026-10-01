using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Invariants of the checker-play analysis panel's cells and layout:
///
///   * The Eq Loss cell is the ranking's error, through the shared display
///     (<see cref="EquityLoss.Format"/>), for every play whose error does not
///     count as zero, and blank where it does: the best play, any play tying
///     it, and any whose error shows as 0.0000. (The ranking's order, numbers
///     and not-scored mark are pinned in <see cref="PlayPanelRankingTests"/>.)
///   * The Depth cell is the candidate's derived abbreviation, and a
///     candidate with no depth recorded draws none.
///   * The Equity and Eq Loss <em>values</em> render bold; their column
///     headers, and every other column, keep the normal weight.
///   * The rank-inversion italic, and the column layout against the panel
///     width (halheinrich/backgammon#252).
/// </summary>
public class RendererPlayPanelTests
{
    /// <summary>A candidate from the standard start: <paramref name="play"/>, at <paramref name="equity"/>, analysed as stated.</summary>
    private static PlayCandidate Candidate(Play play, double equity,
        AnalysisMode mode = AnalysisMode.Evaluation, AnalysisLevel level = AnalysisLevel.Ply3, int? trials = null) =>
        TestRecords.Candidate(play: play, equity: equity, analysisMode: mode, analysisLevel: level, rolloutTrials: trials);

    private static DiagramRequest Solution(List<PlayCandidate> plays, BoardPosition? board = null) =>
        TestFixtures.RequestFor(TestFixtures.CheckerPlayWith(plays, userPlayIndex: null, board: board), DiagramMode.Solution);

    [Fact]
    public void Plays_EqLossForEveryPlayBehindTheBest()
    {
        // Five plays in equity order. The best play's cell is blank; each
        // other's is its error against the best, each formatted value unique.
        List<PlayCandidate> plays =
        [
            Candidate([new(8, 5), new(6, 5)], 0.50),
            Candidate([new(13, 10), new(8, 5)], 0.48),
            Candidate([new(24, 21), new(13, 10)], 0.45),
            Candidate([new(24, 21), new(8, 5)], 0.42),
            Candidate([new(13, 10), new(13, 10)], 0.39),
        ];
        var svg = DiagramRenderer.RenderSvg(Solution(plays), TestFixtures.DefaultOptions());

        // Move text is left-anchored at moveX=53.4 (Medium size, the panel on
        // the left at the full font size), each the play's notation.
        var moveRow = new Regex("""<text x="53\.4" [^>]*font-size="14"[^>]*>([^<]+)</text>""");
        Assert.Equal(plays.Select(p => p.Notation), moveRow.Matches(svg).Select(m => m.Groups[1].Value));

        Assert.Contains(">0.0200</text>", svg);
        Assert.Contains(">0.0500</text>", svg);
        Assert.Contains(">0.0800</text>", svg);
        Assert.Contains(">0.1100</text>", svg);

        // Loss values render right-anchored at lossX=326.4: four behind the
        // best, none for the best. (The "Eq Loss" header also anchors there
        // but is not a decimal.)
        var lossCell = new Regex("""<text x="326\.4" [^>]*text-anchor="end"[^>]*>[0-9]+\.[0-9]{4}</text>""");
        Assert.Equal(4, lossCell.Matches(svg).Count);
    }

    [Fact]
    public void Plays_ATieWithTheBest_IsBlankToo()
    {
        // Error exactly 0 is a best play under the ranking: every tie at the
        // top renders a blank cell, uniformly.
        List<PlayCandidate> plays =
        [
            Candidate([new(8, 5), new(6, 5)], 0.50),
            Candidate([new(13, 10), new(8, 5)], 0.50),
            Candidate([new(24, 21), new(13, 10)], 0.45),
        ];

        Assert.Equal([null, null, "0.0500"], PlayPanelReader.Rows(TestFixtures.Render(Solution(plays))).Select(r => r.Loss));
    }

    [Fact]
    public void Plays_AnErrorThatCountsAsZero_IsBlank_AndOneThatDoesNot_IsShown()
    {
        // The cell is blank exactly where the error counts as zero
        // (EquityLoss.CountsAsZero), so a blank cell and a correct play
        // coincide: 0.00004 behind the best shows as 0.0000 and is blank,
        // 0.00006 shows as 0.0001 and is drawn through the shared display.
        List<PlayCandidate> plays =
        [
            Candidate([new(8, 5), new(6, 5)], 0.50),
            Candidate([new(13, 10), new(8, 5)], 0.49996),
            Candidate([new(24, 21), new(13, 10)], 0.49994),
        ];

        Assert.Equal([null, null, "0.0001"], PlayPanelReader.Rows(TestFixtures.Render(Solution(plays))).Select(r => r.Loss));
    }

    [Theory]
    [InlineData(-0.00004)]
    [InlineData(-0.0)]
    public void Plays_AnEquityThatRoundsToZero_ShowsNoMinusSign(double equity)
    {
        List<PlayCandidate> plays = [Candidate([new(8, 5), new(6, 5)], equity)];

        Assert.Equal("+0.0000", PlayPanelReader.Rows(TestFixtures.Render(Solution(plays))).Single().Equity);
    }

    // Three evaluations at falling depth ranks (3-ply 30, 2-ply 20, 1-ply 10):
    // no row sits deeper than its predecessor, so none is italic.
    private static List<PlayCandidate> FallingDepths() =>
    [
        Candidate([new(8, 5), new(6, 5)], 0.50, level: AnalysisLevel.Ply3),
        Candidate([new(13, 10), new(8, 5)], 0.48, level: AnalysisLevel.Ply2),
        Candidate([new(24, 21), new(8, 5)], 0.42, level: AnalysisLevel.Ply1),
    ];

    [Fact]
    public void Plays_DepthColumn_RendersPerPlayAbbreviationLeftAnchoredAtDepthX()
    {
        // depthX = lossX + 1.5 * PlayPanelFontSize = 326.4 + 21 = 347.4.
        // Abbreviations render left-anchored there (no text-anchor attribute).
        var svg = DiagramRenderer.RenderSvg(Solution(FallingDepths()), TestFixtures.DefaultOptions());

        var depthCell = new Regex("""<text x="347\.4" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""");
        Assert.Equal(["Depth", "3-ply", "2-ply", "1-ply"], depthCell.Matches(svg).Select(m => m.Groups[1].Value));
        Assert.DoesNotContain("font-style=\"italic\"", svg);
    }

    [Fact]
    public void Plays_DepthColumn_OmitsTheCellWhenNoDepthIsRecorded()
    {
        // No depth recorded — the abbreviation is null — draws no cell; the
        // column header still renders.
        List<PlayCandidate> plays = [Candidate([new(8, 5), new(6, 5)], 0.50, AnalysisMode.Unknown, AnalysisLevel.Unknown)];
        var svg = DiagramRenderer.RenderSvg(Solution(plays), TestFixtures.DefaultOptions());

        var depthCell = new Regex("""<text x="347\.4" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""");
        Assert.Equal(["Depth"], depthCell.Matches(svg).Select(m => m.Groups[1].Value));
    }

    [Fact]
    public void Plays_ItalicAppliedToEquityLossAndDepthOnRankInversionRows()
    {
        // Three plays, stored in equity order, depth ranks 45 / 130 / 45:
        //   row 0 — no predecessor, never italic.
        //   row 1 — a 1296-trial rollout (130) below an XG Roller+ (45): a
        //           deeper analysis below a shallower one, so italic on its
        //           Equity, Eq Loss and Depth cells.
        //   row 2 — 45 <= 130, so not italic.
        List<PlayCandidate> plays =
        [
            Candidate([new(8, 5), new(6, 5)], 0.50, level: AnalysisLevel.XgRollerPlus),
            Candidate([new(13, 10), new(8, 5)], 0.48, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),
            Candidate([new(24, 21), new(8, 5)], 0.42, level: AnalysisLevel.XgRollerPlus),
        ];
        var request = Solution(plays);
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        // The six-character "3p1296" is wide enough that the move reservation
        // yields (halheinrich/backgammon#252): the Depth column ends at the
        // panel's right limit, and the numeric block hangs off it. Anchors are
        // derived from the renderer's constants, not pasted.
        var (px, pw) = Panel(svg, request);
        double depthX = px + pw - DiagramRenderer.PanelMargin
                        - DiagramRenderer.EstimateTextWidth("3p1296", DiagramRenderer.PlayPanelFontSize,
                                                            DiagramRenderer.TextWeight.Regular);
        double lossX = depthX - DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelColumnGapEm;
        double equityX = lossX - DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelLossColumnEm;
        string depthAt = Regex.Escape(SvgFormat.Number(depthX));
        string lossAt = Regex.Escape(SvgFormat.Number(lossX));
        string equityAt = Regex.Escape(SvgFormat.Number(equityX));

        // Depth cells: header + 3 rows at depthX.
        var depth = new Regex($$"""<text x="{{depthAt}}" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""").Matches(svg).ToList();
        Assert.Equal(4, depth.Count);
        Assert.Equal("Depth", depth[0].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[0].Value);   // header never italic
        Assert.Equal("R+", depth[1].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[1].Value);   // row 0: no predecessor
        Assert.Equal("3p1296", depth[2].Groups[1].Value);
        Assert.Contains("font-style=\"italic\"", depth[2].Value);         // row 1: 130 > 45 → italic
        Assert.DoesNotContain("font-weight=\"bold\"", depth[2].Value);    // ...but Depth is not a numeric column
        Assert.Equal("R+", depth[3].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[3].Value);   // row 2: 45 <= 130

        // Equity cells: header ("Equity") + 3 rows at equityX, right-anchored.
        var equity = new Regex($$"""<text x="{{equityAt}}" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""").Matches(svg).ToList();
        Assert.Equal(4, equity.Count);
        Assert.DoesNotContain("font-style=\"italic\"", equity[0].Value);  // header
        Assert.DoesNotContain("font-style=\"italic\"", equity[1].Value);  // row 0
        Assert.Contains("font-style=\"italic\"", equity[2].Value);        // row 1 inverted
        Assert.DoesNotContain("font-style=\"italic\"", equity[3].Value);  // row 2
        // Weight and style are independent attributes: an inverted row reads
        // bold-italic, the cue does not displace the numeric bold.
        Assert.Contains("font-weight=\"bold\"", equity[2].Value);

        // Eq Loss cells: header + rows 1 and 2 (row 0 is the best play and
        // renders no cell).
        var loss = new Regex($$"""<text x="{{lossAt}}" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""").Matches(svg).ToList();
        Assert.Equal(3, loss.Count);
        Assert.DoesNotContain("font-style=\"italic\"", loss[0].Value);    // header
        Assert.Contains("font-style=\"italic\"", loss[1].Value);          // row 1 inverted
        Assert.DoesNotContain("font-style=\"italic\"", loss[2].Value);    // row 2
        Assert.Contains("font-weight=\"bold\"", loss[1].Value);           // bold survives the italic cue
    }

    [Fact]
    public void Plays_ItalicFollowsTheRankingsOrder_NotTheStoredOrder()
    {
        // Stored rollout first, evaluation second — the evaluation rating
        // higher. Under equity the evaluation ranks first and the deeper
        // rollout below it is the inversion, though stored first; under depth
        // first the rollout ranks first and nothing is inverted.
        List<PlayCandidate> plays =
        [
            Candidate([new(13, 10), new(8, 5)], 0.45, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),
            Candidate([new(8, 5), new(6, 5)], 0.50, level: AnalysisLevel.Ply3),
        ];
        var request = Solution(plays);

        var byEquity = PlayPanelReader.Rows(TestFixtures.Render(request));
        Assert.Equal([false, true], byEquity.Select(r => r.Italic));
        Assert.Equal(plays[0].Notation, byEquity[1].Move);

        var depthFirst = PlayPanelReader.Rows(TestFixtures.Render(request with { Ranking = PlayRanking.DepthFirst }));
        Assert.All(depthFirst, row => Assert.False(row.Italic));
    }

    [Fact]
    public void Plays_AnUnrecordedDepthIsNoInversion()
    {
        // A recorded depth below one not recorded is not "deeper below
        // shallower": an unrecorded depth is not shallow, only unrecorded
        // (its rank is null), so no italic.
        List<PlayCandidate> plays =
        [
            Candidate([new(8, 5), new(6, 5)], 0.50, AnalysisMode.Unknown, AnalysisLevel.Unknown),
            Candidate([new(13, 10), new(8, 5)], 0.48, AnalysisMode.Rollout, AnalysisLevel.Ply3, trials: 1296),
        ];

        Assert.All(PlayPanelReader.Rows(TestFixtures.Render(Solution(plays))), row => Assert.False(row.Italic));
    }

    [Fact]
    public void Plays_EquityAndEqLossValues_RenderBold_HeadersAndOtherColumnsDoNot()
    {
        // Bold is the numeric-column treatment: the Equity and Eq Loss
        // *values* only. Depth ranks fall down the list so no row is
        // italicised — this isolates weight from the rank-inversion style.
        var svg = DiagramRenderer.RenderSvg(Solution(FallingDepths()), TestFixtures.DefaultOptions());

        // Equity column at x=263.4, right-anchored: header + one cell per play.
        var equity = new Regex("""<text x="263\.4" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""").Matches(svg).ToList();
        Assert.Equal(4, equity.Count);
        Assert.Equal("Equity", equity[0].Groups[1].Value);
        Assert.DoesNotContain("font-weight=\"bold\"", equity[0].Value);   // header keeps normal weight
        Assert.All(equity.Skip(1), m => Assert.Contains("font-weight=\"bold\"", m.Value));

        // Eq Loss column at x=326.4: header + rows 1 and 2. Row 0 is the best
        // play and still renders no cell at all.
        var loss = new Regex("""<text x="326\.4" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""").Matches(svg).ToList();
        Assert.Equal(3, loss.Count);
        Assert.Equal("Eq Loss", loss[0].Groups[1].Value);
        Assert.DoesNotContain("font-weight=\"bold\"", loss[0].Value);
        Assert.All(loss.Skip(1), m => Assert.Contains("font-weight=\"bold\"", m.Value));

        // Rank (22.6), move notation (53.4), and Depth (347.4) columns are
        // left-anchored and unaffected. The marker column is bold by a
        // separate, pre-existing rule and is deliberately not swept here.
        foreach (string x in new[] { @"22\.6", @"53\.4", @"347\.4" })
        {
            var cells = new Regex($$"""<text x="{{x}}" y="[0-9.]+" font-family="sans-serif"[^>]*>[^<]+</text>""").Matches(svg);
            Assert.NotEmpty(cells);
            Assert.All(cells, m => Assert.DoesNotContain("font-weight=\"bold\"", m.Value));
        }
    }

    // -----------------------------------------------------------------------
    //  Column layout against the panel width (halheinrich/backgammon#252)
    // -----------------------------------------------------------------------

    // The longest depth abbreviation the producer emits for a recognised
    // level (nine characters): an XG Roller++ rollout of 20736 trials.
    private const string NineCharDepth = "R++p20736";

    // Half the last digit SvgFormat.Number keeps ("0.##"): a rendered
    // coordinate differs from the value the renderer computed by at most this.
    private const double RenderedRounding = 0.005;

    /// <summary>The depth abbreviations of the layout fixture's three candidates.</summary>
    public enum Depths
    {
        /// <summary>R++, 4-ply, Book: each at most the header's length.</summary>
        Short,
        /// <summary>R++p20736 and B3_20736, then Book.</summary>
        Long,
        /// <summary>
        /// R++p20736, then an abbreviation no recognised level writes —
        /// level-9999p99999, a rollout at a level the library does not
        /// recognise, keeping its raw code — wide enough that even the
        /// minimum font size cannot fit it beside the four-hit move on 16:9,
        /// and not so wide that not even an empty move text fits at the full
        /// size (the narrow presets' overflow, halheinrich/backgammon#253's).
        /// </summary>
        Extreme,
    }

    /// <summary>The best play's (and the longest) move of the layout fixture.</summary>
    public enum FirstMove
    {
        /// <summary>24/21 13/10, from the standard start.</summary>
        Short,
        /// <summary>24/20 13/9 8/4 6/2: too long to fit beside a nine-character depth at the full size; it fits by shrinking.</summary>
        FourPart,
        /// <summary>The user's worst case (2026-09-22): four hits, 24/23* 23/22* 22/21* 21/20*, too long to fit at the full size beside any depth.</summary>
        FourHits,
    }

    /// <summary>
    /// A board with the opponent's blots on the 23- to 20-points, so four
    /// hits in one play are valid, and the on-roll checkers the fixture's
    /// other plays move from.
    /// </summary>
    private static readonly BoardPosition BlotsBoard = TestFixtures.Board(
        (1, -2), (12, -5), (19, -4), (20, -1), (21, -1), (22, -1), (23, -1),
        (24, 1), (13, 5), (8, 3), (6, 6));

    /// <summary>A fixed three-play request: the <paramref name="firstMove"/>
    /// best, then 13/10 8/5 and 8/5 6/5, at the given depths.</summary>
    private static DiagramRequest LayoutRequest(Depths depths, PanelPosition side, FirstMove firstMove = FirstMove.Short)
    {
        Play first = firstMove switch
        {
            FirstMove.Short => [new(24, 21), new(13, 10)],
            FirstMove.FourPart => [new(24, 20), new(13, 9), new(8, 4), new(6, 2)],
            _ => [new(24, -23), new(23, -22), new(22, -21), new(21, -20)],
        };
        PlayCandidate[] depthOf = depths switch
        {
            Depths.Short =>
            [
                Candidate(first, 0.50, level: AnalysisLevel.XgRollerPlusPlus),
                Candidate([new(13, 10), new(8, 5)], 0.48, level: AnalysisLevel.Ply4),
                Candidate([new(8, 5), new(6, 5)], -0.42, AnalysisMode.BookRollout, AnalysisLevel.Unknown),
            ],
            Depths.Long =>
            [
                Candidate(first, 0.50, AnalysisMode.Rollout, AnalysisLevel.XgRollerPlusPlus, trials: 20736),
                Candidate([new(13, 10), new(8, 5)], 0.48, AnalysisMode.BookRollout, AnalysisLevel.Ply3, trials: 20736),
                Candidate([new(8, 5), new(6, 5)], -0.42, AnalysisMode.BookRollout, AnalysisLevel.Unknown),
            ],
            _ =>
            [
                Candidate(first, 0.50, AnalysisMode.Rollout, AnalysisLevel.XgRollerPlusPlus, trials: 20736),
                TestRecords.Candidate(play: [new(13, 10), new(8, 5)], equity: 0.48, analysisMode: AnalysisMode.Rollout,
                    analysisLevel: AnalysisLevel.Unknown, unrecognizedLevelCode: 9999, rolloutTrials: 99999),
                Candidate([new(8, 5), new(6, 5)], -0.42, AnalysisMode.BookRollout, AnalysisLevel.Unknown),
            ],
        };
        return Solution([.. depthOf], firstMove == FirstMove.FourHits ? BlotsBoard : null) with { AnalysisPanelPosition = side };
    }

    private static IReadOnlyList<PlayCandidate> Plays(DiagramRequest request) =>
        ((CheckerPlayDecision)request.Decision!).Decision.Plays;

    /// <summary>One rendered text element: its x attribute (as emitted),
    /// whether it is right-anchored, its font-size attribute, and its
    /// content.</summary>
    private sealed record TextCell(string X, bool AnchorEnd, string FontSize, string Content);

    private static readonly Regex TextElement =
        new("""<text x="([0-9.]+)" y="[0-9.]+"( text-anchor="end")? font-family="sans-serif" font-size="([0-9.]+)"[^>]*>([^<]+)</text>""");

    private static List<TextCell> Cells(string svg, IEnumerable<string> contents)
    {
        var wanted = contents.ToHashSet();
        return TextElement.Matches(svg)
            .Select(m => new TextCell(m.Groups[1].Value, m.Groups[2].Success, m.Groups[3].Value, m.Groups[4].Value))
            .Where(c => wanted.Contains(c.Content))
            .ToList();
    }

    /// <summary>The panel's left edge and width for <paramref name="request"/>
    /// under <paramref name="svg"/>'s canvas: width from the viewBox less the
    /// fixed board, origin from the layout's own panel placement.</summary>
    private static (double Px, double Pw) Panel(string svg, DiagramRequest request)
    {
        var m = Regex.Match(svg, """viewBox="0 0 ([0-9.]+) [0-9.]+" """);
        double totalWidth = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var layout = BoardLayout.Default with { PanelWidthOverride = totalWidth - BoardLayout.Default.BoardWidth };
        return (layout.PanelX(request.PanelOnLeft), layout.PanelWidth);
    }

    /// <summary>The panel's rendered font size and four column anchors, as
    /// emitted.</summary>
    private sealed record RenderedColumns(string FontSize, string MoveX, string EquityX, string LossX, string DepthX);

    /// <summary>The losses the Eq Loss column shows: each error that does not
    /// count as zero, through the shared display.</summary>
    private static IEnumerable<string> LossCells(DiagramRequest request) =>
        ((CheckerPlayDecision)request.Decision!).Decision.RankedBy(request.Ranking!.Value)
            .Select(row => row.Error)
            .OfType<double>()
            .Where(error => !EquityLoss.CountsAsZero(error))
            .Select(EquityLoss.Format);

    /// <summary>The Depth column's texts: every play's abbreviation.</summary>
    private static IEnumerable<string> DepthTexts(DiagramRequest request) =>
        Plays(request).Select(p => p.DepthAbbreviation).OfType<string>();

    /// <summary>The panel's font size and column anchors as rendered, read off
    /// each column's header, after asserting every cell of the column shares
    /// its anchor, the numeric columns stay right-anchored while Depth stays
    /// left-anchored, and every cell — header row and play rows alike — is
    /// set in one font size.</summary>
    private static RenderedColumns Anchors(string svg, DiagramRequest request)
    {
        var plays = Plays(request);
        var all = Cells(svg, plays.Select(p => p.Notation)
            .Concat(EquityCells(request))
            .Append(DiagramRenderer.PlayPanelLossHeader)
            .Concat(DepthTexts(request))
            .Append(DiagramRenderer.PlayPanelDepthHeader));
        string fontSize = Assert.Single(all.Select(c => c.FontSize).Distinct());

        var move = Cells(svg, plays.Select(p => p.Notation));
        Assert.Equal(plays.Count, move.Count);
        Assert.Single(move.Select(c => c.X).Distinct());
        Assert.All(move, c => Assert.False(c.AnchorEnd));

        var equity = Cells(svg, EquityCells(request));
        Assert.Equal(plays.Count + 1, equity.Count);
        Assert.Single(equity.Select(c => c.X).Distinct());
        Assert.All(equity, c => Assert.True(c.AnchorEnd));

        var loss = Cells(svg, LossCells(request).Append(DiagramRenderer.PlayPanelLossHeader));
        Assert.Equal(LossCells(request).Count() + 1, loss.Count);
        Assert.Single(loss.Select(c => c.X).Distinct());
        Assert.All(loss, c => Assert.True(c.AnchorEnd));

        var depth = Cells(svg, DepthTexts(request).Append(DiagramRenderer.PlayPanelDepthHeader));
        Assert.Equal(DepthTexts(request).Count() + 1, depth.Count);
        Assert.Single(depth.Select(c => c.X).Distinct());
        Assert.All(depth, c => Assert.False(c.AnchorEnd));

        return new RenderedColumns(fontSize, move[0].X, equity[0].X, loss[0].X, depth[0].X);
    }

    /// <summary>The anchors the full move reservation produces, from the
    /// rendered move anchor and the renderer's own constants — the layout
    /// before the reservation could yield.</summary>
    private static (string EquityX, string LossX, string DepthX) FullReservationAnchors(string moveX)
    {
        double move = double.Parse(moveX, CultureInfo.InvariantCulture);
        double equityX = move + DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelMoveReserveEm;
        double lossX = equityX + DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelLossColumnEm;
        double depthX = lossX + DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelColumnGapEm;
        return (SvgFormat.Number(equityX), SvgFormat.Number(lossX), SvgFormat.Number(depthX));
    }

    private static double Num(string x) => double.Parse(x, CultureInfo.InvariantCulture);

    private const double FullSize = DiagramRenderer.PlayPanelFontSize;
    private const double GapEm = DiagramRenderer.PlayPanelColumnGapEm;
    private const double LossEm = DiagramRenderer.PlayPanelLossColumnEm;

    private static double Width(string text, double size,
        DiagramRenderer.TextWeight weight = DiagramRenderer.TextWeight.Regular) =>
        DiagramRenderer.EstimateTextWidth(text, size, weight);

    /// <summary>The Equity column's texts: its header and every play's
    /// value, formatted as the renderer formats them.</summary>
    private static IEnumerable<string> EquityCells(DiagramRequest request) =>
        Plays(request)
            .Select(p => DiagramRenderer.FormatEquity(p.Equity))
            .Append(DiagramRenderer.PlayPanelEquityHeader);

    // Each cell at the weight it is emitted in: the header regular, the
    // values bold.
    private static double WidestEquityCell(DiagramRequest request, double size) =>
        EquityCells(request).Max(t => Width(t, size,
            t == DiagramRenderer.PlayPanelEquityHeader
                ? DiagramRenderer.TextWeight.Regular
                : DiagramRenderer.TextWeight.Bold));

    private static double LongestMove(DiagramRequest request, double size) =>
        Plays(request).Max(p => Width(p.Notation, size));

    private static double WidestDepthCell(DiagramRequest request, double size) =>
        DepthTexts(request).Append(DiagramRenderer.PlayPanelDepthHeader).Max(t => Width(t, size));

    /// <summary>The reservation's floor at <paramref name="size"/>: longest
    /// move text, the column gap, the widest Equity cell.</summary>
    private static double Floor(DiagramRequest request, double size) =>
        LongestMove(request, size) + size * GapEm + WidestEquityCell(request, size);

    /// <summary>The panel's right limit (its right edge less the margin) and
    /// its marker column (left edge plus margin plus the fixed inset).</summary>
    private static (double Limit, double MarkerX) Bounds(double px, double pw) =>
        (px + pw - DiagramRenderer.PanelMargin,
         px + DiagramRenderer.PanelMargin + DiagramRenderer.PlayPanelMarkerInset);

    private static double MoveXAt(double markerX, double size) =>
        markerX + size * (DiagramRenderer.PlayPanelRankOffsetEm + DiagramRenderer.PlayPanelMoveOffsetEm);

    /// <summary>The reservation at <paramref name="size"/> that ends the Depth
    /// column exactly at the panel's right limit.</summary>
    private static double Room(DiagramRequest request, double px, double pw, double size)
    {
        var (limit, markerX) = Bounds(px, pw);
        return limit - WidestDepthCell(request, size) - size * GapEm - size * LossEm - MoveXAt(markerX, size);
    }

    /// <summary>The spec's size: 14 when the floor fits in the room at 14;
    /// otherwise the closed-form size at which floor equals room — the span
    /// from the marker column to the right limit over the per-size sum of
    /// every scaling term — clamped to the minimum.</summary>
    private static double ExpectedSize(DiagramRequest request, double px, double pw)
    {
        if (Floor(request, FullSize) <= Room(request, px, pw, FullSize))
            return FullSize;
        var (limit, markerX) = Bounds(px, pw);
        double perSize = DiagramRenderer.PlayPanelRankOffsetEm + DiagramRenderer.PlayPanelMoveOffsetEm
                         + Floor(request, 1) + LossEm + GapEm + WidestDepthCell(request, 1);
        return Math.Max(DiagramRenderer.PlayPanelMinimumFontSize, (limit - markerX) / perSize);
    }

    private static DiagramRequest WidescreenRequest(Depths depths, PanelPosition side, FirstMove firstMove,
        out string svg, out double px, out double pw)
    {
        var request = LayoutRequest(depths, side, firstMove);
        svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });
        (px, pw) = Panel(svg, request);
        return request;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextWidthEstimate_UnknownGlyph_ChargedTheWidestKnownGlyph(bool bold)
    {
        var weight = bold ? DiagramRenderer.TextWeight.Bold : DiagramRenderer.TextWeight.Regular;
        // The tables list printable ASCII, U+0020 to U+007E; '§' is outside it.
        var known = Enumerable.Range(0x20, 0x7F - 0x20).Select(c => (char)c).ToList();
        double widestKnown = known.Max(c => Width(c.ToString(), FullSize, weight));
        Assert.Equal(widestKnown, Width("§", FullSize, weight));
        // Exactly one ASCII character ('@', in both weights) measures the
        // widest — so none is missing from the table, since a missing one
        // would be charged the widest too.
        Assert.Single(known, c => Width(c.ToString(), FullSize, weight) == widestKnown);
    }

    [Fact]
    public void Fixture_DepthsAreTheProducersAbbreviations()
    {
        // The layout fixture's premise, from the producer's derivation.
        Assert.Equal(["R++p20736", "B3_20736", "Book"],
            Plays(LayoutRequest(Depths.Long, PanelPosition.Left)).Select(p => p.DepthAbbreviation));
        Assert.Equal("24/23* 23/22* 22/21* 21/20*",
            Plays(LayoutRequest(Depths.Short, PanelPosition.Left, FirstMove.FourHits))[0].Notation);
        Assert.Equal([NineCharDepth, "level-9999p99999", "Book"],
            Plays(LayoutRequest(Depths.Extreme, PanelPosition.Left)).Select(p => p.DepthAbbreviation));
    }

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void NineCharDepth_Widescreen_FitsAt14_DepthColumnEndsAtTheLimit(PanelPosition side)
    {
        var request = WidescreenRequest(Depths.Long, side, FirstMove.Short, out var svg, out var px, out var pw);
        var c = Anchors(svg, request);
        var (limit, _) = Bounds(px, pw);

        // It fits at the full size.
        Assert.Equal(SvgFormat.Number(FullSize), c.FontSize);

        // The Depth column's estimated right edge is at or inside the limit —
        // and, the reservation having yielded, exactly at it: the numeric
        // block shifted left by the shortfall and no further.
        double depthRight = Num(c.DepthX) + Width(NineCharDepth, FullSize);
        Assert.InRange(depthRight, limit - 2 * RenderedRounding, limit + RenderedRounding);

        // The reservation did yield, and not below its floor: the longest
        // move text, the column gap, and the widest Equity cell.
        double reserve = Num(c.EquityX) - Num(c.MoveX);
        double full = FullSize * DiagramRenderer.PlayPanelMoveReserveEm;
        double floor = Floor(request, FullSize);
        Assert.True(reserve < full, $"reservation {reserve} did not yield from {full}");
        Assert.True(reserve >= floor - 2 * RenderedRounding, $"reservation {reserve} below floor {floor}");
    }

    [Theory]
    [InlineData(Depths.Short, FirstMove.FourHits, PanelPosition.Left)]
    [InlineData(Depths.Short, FirstMove.FourHits, PanelPosition.Right)]
    [InlineData(Depths.Long,  FirstMove.FourHits, PanelPosition.Left)]
    [InlineData(Depths.Long,  FirstMove.FourHits, PanelPosition.Right)]
    [InlineData(Depths.Long,  FirstMove.FourPart, PanelPosition.Left)]
    [InlineData(Depths.Long,  FirstMove.FourPart, PanelPosition.Right)]
    public void TooWideAt14_Widescreen_ShrinksToTheClosedFormSizeAndFits(Depths depths, FirstMove firstMove, PanelPosition side)
    {
        var request = WidescreenRequest(depths, side, firstMove, out var svg, out var px, out var pw);
        var c = Anchors(svg, request);
        var (limit, _) = Bounds(px, pw);

        // Precondition: the content does not fit at the full size.
        Assert.True(Floor(request, FullSize) > Room(request, px, pw, FullSize), "fixture should not fit at 14");

        // The size is the closed-form one, strictly inside the clamp.
        double size = ExpectedSize(request, px, pw);
        Assert.InRange(size, DiagramRenderer.PlayPanelMinimumFontSize, FullSize);
        Assert.NotEqual(DiagramRenderer.PlayPanelMinimumFontSize, size);
        Assert.InRange(Num(c.FontSize), size - RenderedRounding, size + RenderedRounding);

        // At that size it fits: floor equals room, so the reservation is the
        // floor and the Depth column ends at the right limit, not past it.
        Assert.InRange(Num(c.EquityX) - Num(c.MoveX),
            Floor(request, size) - 2 * RenderedRounding, Floor(request, size) + 2 * RenderedRounding);
        double depthRight = Num(c.DepthX) + WidestDepthCell(request, size);
        Assert.True(depthRight <= limit + 2 * RenderedRounding, $"depth ends at {depthRight}, past {limit}");
    }

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void TooWideEvenAtMinimum_Widescreen_FloorWins_DepthOverrunsByFloorMinusRoom(PanelPosition side)
    {
        var request = WidescreenRequest(Depths.Extreme, side, FirstMove.FourHits, out var svg, out var px, out var pw);
        var c = Anchors(svg, request);
        var (limit, _) = Bounds(px, pw);
        const double min = DiagramRenderer.PlayPanelMinimumFontSize;

        // Clamped at the minimum, where it still cannot fit.
        Assert.Equal(SvgFormat.Number(min), c.FontSize);
        double room = Room(request, px, pw, min);
        double floor = Floor(request, min);
        Assert.True(floor > room, $"fixture should not fit at the minimum: room {room}, floor {floor}");

        // The floor wins over the cap: move text and Equity do not collide,
        // and the Depth column overruns by exactly floor minus room.
        Assert.InRange(Num(c.EquityX) - Num(c.MoveX), floor - 2 * RenderedRounding, floor + 2 * RenderedRounding);
        double overrun = Num(c.DepthX) + WidestDepthCell(request, min) - limit;
        Assert.InRange(overrun, floor - room - 3 * RenderedRounding, floor - room + 3 * RenderedRounding);
    }

    [Theory]
    [InlineData(Depths.Short,   FirstMove.Short,    PanelPosition.Left)]
    [InlineData(Depths.Short,   FirstMove.Short,    PanelPosition.Right)]
    [InlineData(Depths.Long,    FirstMove.Short,    PanelPosition.Left)]
    [InlineData(Depths.Long,    FirstMove.Short,    PanelPosition.Right)]
    [InlineData(Depths.Short,   FirstMove.FourPart, PanelPosition.Left)]
    [InlineData(Depths.Short,   FirstMove.FourPart, PanelPosition.Right)]
    [InlineData(Depths.Long,    FirstMove.FourPart, PanelPosition.Left)]
    [InlineData(Depths.Long,    FirstMove.FourPart, PanelPosition.Right)]
    [InlineData(Depths.Short,   FirstMove.FourHits, PanelPosition.Left)]
    [InlineData(Depths.Short,   FirstMove.FourHits, PanelPosition.Right)]
    [InlineData(Depths.Long,    FirstMove.FourHits, PanelPosition.Left)]
    [InlineData(Depths.Long,    FirstMove.FourHits, PanelPosition.Right)]
    [InlineData(Depths.Extreme, FirstMove.FourHits, PanelPosition.Left)]
    [InlineData(Depths.Extreme, FirstMove.FourHits, PanelPosition.Right)]
    public void Widescreen_EquityNeverPrecedesMoveTextPlusGap(Depths depths, FirstMove firstMove, PanelPosition side)
    {
        // At every size — full, shrunk, or clamped at the minimum.
        var request = WidescreenRequest(depths, side, firstMove, out var svg, out var px, out var pw);
        var c = Anchors(svg, request);
        double size = ExpectedSize(request, px, pw);

        double equityLeft = Num(c.EquityX) - WidestEquityCell(request, size);
        double moveRightPlusGap = Num(c.MoveX) + LongestMove(request, size) + size * GapEm;
        Assert.True(equityLeft >= moveRightPlusGap - 2 * RenderedRounding,
            $"equity left edge {equityLeft} precedes move right edge plus gap {moveRightPlusGap}");
    }

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void ShortDepths_Widescreen_AnchorsAreTheFullReservation(PanelPosition side)
    {
        // Behaviour-neutral pin: with room to spare nothing yields or shrinks,
        // so the size is 14 and every anchor is what the fixed reservation
        // always gave.
        var request = WidescreenRequest(Depths.Short, side, FirstMove.Short, out var svg, out _, out _);

        var c = Anchors(svg, request);
        Assert.Equal(SvgFormat.Number(FullSize), c.FontSize);
        Assert.Equal(FullReservationAnchors(c.MoveX), (c.EquityX, c.LossX, c.DepthX));
    }

    [Theory]
    [InlineData(AspectPreset.Natural, Depths.Short)]
    [InlineData(AspectPreset.Natural, Depths.Long)]
    [InlineData(AspectPreset.Standard4x3, Depths.Short)]
    [InlineData(AspectPreset.Standard4x3, Depths.Long)]
    public void NarrowPresets_KeepTheFullReservation(AspectPreset aspect, Depths depths)
    {
        // The narrow presets cannot hold the play panel's columns at all — a
        // pre-existing overflow owned by halheinrich/backgammon#253. The
        // yielding reservation must leave their output exactly as it was;
        // this pins that it does, not that the geometry is right. Retire it
        // with halheinrich/backgammon#253's fix.
        var request = LayoutRequest(depths, PanelPosition.Left);
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = aspect });

        var c = Anchors(svg, request);
        Assert.Equal(SvgFormat.Number(FullSize), c.FontSize);
        Assert.Equal(FullReservationAnchors(c.MoveX), (c.EquityX, c.LossX, c.DepthX));
    }
}
