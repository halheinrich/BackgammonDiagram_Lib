using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Invariants of the checker-play analysis panel — the contract the renderer
/// owes its caller for <see cref="DiagramRequest.Decision"/>.<see cref="DecisionData.Plays"/>:
///
///   * Under the default <see cref="DiagramRequest.CandidateOrdering"/>,
///     caller order is preserved verbatim (no implicit re-sort inside the
///     renderer). The opt-in depth treatment is pinned separately in
///     <see cref="CandidateDepthTreatmentTests"/>.
///   * <see cref="PlayCandidate.EquityLoss"/> is rendered as text for every
///     non-best entry; omitted when the field is &lt;= 0 (which in practice
///     means the candidate is itself a best play — <c>EquityLoss == 0.0</c>
///     marks membership in the best-equity equivalence class).
///   * The Equity and Eq Loss <em>values</em> render bold; their column
///     headers, and every other column, keep the normal weight.
///
/// This pins the contract so a future behavior regression shows up here and
/// upstream data-layer investigations can proceed without having to re-verify
/// the renderer is innocent.
/// </summary>
public class RendererPlayPanelTests
{
    [Fact]
    public void Plays_RenderedInCallerOrder_WithEqLossForEveryNonBestPlay()
    {
        // Five plays pre-sorted descending by Equity. The first (best) play
        // takes the default EquityLoss = 0.0 (membership in the best-equity
        // equivalence class); each subsequent loss is bestEquity - thisEquity.
        // Values chosen so every formatted loss is a unique substring — makes
        // the negative "absent for best" assertion clean.
        var plays = new List<PlayCandidate>
        {
            new() { MoveNotation = "8/5 6/5",     Equity = 0.50 },
            new() { MoveNotation = "13/10 8/5",   Equity = 0.48, EquityLoss = 0.02 },
            new() { MoveNotation = "24/21 13/10", Equity = 0.45, EquityLoss = 0.05 },
            new() { MoveNotation = "24/21 8/5",   Equity = 0.42, EquityLoss = 0.08 },
            new() { MoveNotation = "13/10 13/10", Equity = 0.39, EquityLoss = 0.11 },
        };

        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = plays;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // ---- Row order matches caller input exactly ----
        // Move-notation text is left-anchored at moveX=53.4 (MinimalBuilder's
        // default size). Extract in SVG emission order and compare.
        var moveRow = new Regex("""<text x="53\.4" [^>]*font-size="14"[^>]*>([^<]+)</text>""");
        var renderedMoves = moveRow.Matches(svg).Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(plays.Select(p => p.MoveNotation).ToList(), renderedMoves);

        // ---- Eq-Loss text present for every non-best play ----
        Assert.Contains(">0.0200</text>", svg);
        Assert.Contains(">0.0500</text>", svg);
        Assert.Contains(">0.0800</text>", svg);
        Assert.Contains(">0.1100</text>", svg);

        // ---- Eq-Loss absent only for the best play ----
        // Loss values render right-anchored at lossX=326.4. Four non-best
        // plays should produce exactly four numeric loss texts at that X.
        // (The "Eq Loss" column header also anchors at 326.4 but carries
        // text rather than a decimal, so the numeric pattern excludes it.)
        var lossCell = new Regex("""<text x="326\.4" [^>]*text-anchor="end"[^>]*>[0-9]+\.[0-9]{4}</text>""");
        Assert.Equal(4, lossCell.Matches(svg).Count);
    }

    [Fact]
    public void Plays_DepthColumn_RendersPerPlayAbbreviationLeftAnchoredAtDepthX()
    {
        // depthX = lossX + 1.5 * PlayPanelFontSize = 326.4 + 21 = 347.4
        // (MinimalBuilder's default Medium size). Abbreviation strings render
        // left-anchored at depthX (no text-anchor attribute). Ranks are
        // monotone non-increasing (3, 2, 0) so no row is italicised.
        var plays = new List<PlayCandidate>
        {
            new() { MoveNotation = "8/5 6/5",   Equity = 0.50,                     DepthAbbreviation = "3-ply", DepthRank = 3 },
            new() { MoveNotation = "13/10 8/5", Equity = 0.48, EquityLoss = 0.02,  DepthAbbreviation = "2-ply", DepthRank = 2 },
            new() { MoveNotation = "24/21 8/5", Equity = 0.42, EquityLoss = 0.08,  DepthAbbreviation = "R+",    DepthRank = 0 },
        };

        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = plays;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // All x=347.4 text cells, in emission order: one header + one per play.
        // Matching on left-anchored cells (no text-anchor attribute) is what
        // distinguishes the Depth column from the right-anchored Equity/Eq Loss.
        var depthCell = new Regex("""<text x="347\.4" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""");
        var rendered = depthCell.Matches(svg).Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(new[] { "Depth", "3-ply", "2-ply", "R+" }, rendered);

        // None of the ranks go up along the list, so none of the depth cells
        // carry italic styling.
        Assert.DoesNotContain("font-style=\"italic\"", svg);
    }

    [Fact]
    public void Plays_DepthColumn_OmitsRowWhenPlayAbbreviationEmpty()
    {
        var plays = new List<PlayCandidate>
        {
            new() { MoveNotation = "8/5 6/5", Equity = 0.50 /* DepthAbbreviation defaults to "" */ },
        };
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = plays;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Header still renders; the one play row contributes no depth cell.
        // So exactly one x=347.4 left-anchored cell — the header — should match.
        var depthCell = new Regex("""<text x="347\.4" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""");
        var rendered = depthCell.Matches(svg).Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(new[] { "Depth" }, rendered);
    }

    [Fact]
    public void Plays_ItalicAppliedToEquityLossAndDepthOnRankInversionRows()
    {
        // Three plays, equity-sorted. Ranks [5, 10, 5]:
        //   row 0 — no predecessor, never italic.
        //   row 1 — rank 10 > 5, so italic (deeper analysis below shallower)
        //           on all three of Equity, Eq Loss, and Depth cells.
        //   row 2 — rank 5 <= 10, so not italic.
        var plays = new List<PlayCandidate>
        {
            new() { MoveNotation = "8/5 6/5",   Equity = 0.50,                     DepthAbbreviation = "R+",     DepthRank = 5 },
            new() { MoveNotation = "13/10 8/5", Equity = 0.48, EquityLoss = 0.02,  DepthAbbreviation = "3p1296", DepthRank = 10 },
            new() { MoveNotation = "24/21 8/5", Equity = 0.42, EquityLoss = 0.08,  DepthAbbreviation = "R+",     DepthRank = 5 },
        };

        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = plays;
        var request = b.Build();
        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());

        // The six-character "3p1296" is wide enough that the move reservation
        // yields (halheinrich/backgammon#252): the Depth column ends at the
        // panel's right limit, and the numeric block hangs off it. Anchors are
        // derived from the renderer's constants, not pasted.
        var (px, pw) = Panel(svg, request);
        double depthX = px + pw - DiagramRenderer.PanelMargin
                        - DiagramRenderer.EstimatePlayPanelTextWidth("3p1296");
        double lossX = depthX - DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelColumnGapEm;
        double equityX = lossX - DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelLossColumnEm;
        string depthAt = Regex.Escape(SvgFormat.Number(depthX));
        string lossAt = Regex.Escape(SvgFormat.Number(lossX));
        string equityAt = Regex.Escape(SvgFormat.Number(equityX));

        // Depth cells: header + 3 rows at depthX.
        var depthCell = new Regex($$"""<text x="{{depthAt}}" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""");
        var depth = depthCell.Matches(svg).ToList();
        Assert.Equal(4, depth.Count);
        Assert.Equal("Depth", depth[0].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[0].Value);   // header never italic
        Assert.Equal("R+", depth[1].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[1].Value);   // row 0: no predecessor
        Assert.Equal("3p1296", depth[2].Groups[1].Value);
        Assert.Contains("font-style=\"italic\"", depth[2].Value);         // row 1: 10 > 5 → italic
        Assert.DoesNotContain("font-weight=\"bold\"", depth[2].Value);    // ...but Depth is not a numeric column
        Assert.Equal("R+", depth[3].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[3].Value);   // row 2: 5 <= 10

        // Equity cells: header ("Equity") + 3 rows at equityX, right-anchored.
        var equityCell = new Regex($$"""<text x="{{equityAt}}" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""");
        var equity = equityCell.Matches(svg).ToList();
        Assert.Equal(4, equity.Count);
        Assert.DoesNotContain("font-style=\"italic\"", equity[0].Value);  // header
        Assert.DoesNotContain("font-style=\"italic\"", equity[1].Value);  // row 0
        Assert.Contains("font-style=\"italic\"", equity[2].Value);        // row 1 inverted
        Assert.DoesNotContain("font-style=\"italic\"", equity[3].Value);  // row 2
        // Weight and style are independent attributes: an inverted row reads
        // bold-italic, the cue does not displace the numeric bold.
        Assert.Contains("font-weight=\"bold\"", equity[2].Value);

        // Eq Loss cells: header ("Eq Loss") + rows 1 and 2 at lossX (row 0
        // is a best play with EquityLoss = 0.0 and renders no cell). Header
        // is the only non-numeric content in the column, so picking by row
        // content is unambiguous.
        var lossCell = new Regex($$"""<text x="{{lossAt}}" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""");
        var loss = lossCell.Matches(svg).ToList();
        Assert.Equal(3, loss.Count);
        Assert.DoesNotContain("font-style=\"italic\"", loss[0].Value);    // header
        Assert.Contains("font-style=\"italic\"", loss[1].Value);          // row 1 inverted
        Assert.DoesNotContain("font-style=\"italic\"", loss[2].Value);    // row 2
        Assert.Contains("font-weight=\"bold\"", loss[1].Value);           // bold survives the italic cue
    }

    [Fact]
    public void Plays_EquityAndEqLossValues_RenderBold_HeadersAndOtherColumnsDoNot()
    {
        // Bold is the numeric-column treatment: the Equity and Eq Loss
        // *values* only. Ranks are monotone non-increasing so no row is
        // italicised — this isolates weight from the rank-inversion style.
        var plays = new List<PlayCandidate>
        {
            new() { MoveNotation = "8/5 6/5",   Equity = 0.50,                     DepthAbbreviation = "3-ply", DepthRank = 3 },
            new() { MoveNotation = "13/10 8/5", Equity = 0.48, EquityLoss = 0.02,  DepthAbbreviation = "2-ply", DepthRank = 2 },
            new() { MoveNotation = "24/21 8/5", Equity = 0.42, EquityLoss = 0.08,  DepthAbbreviation = "R+",    DepthRank = 0 },
        };

        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = plays;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Equity column at x=263.4, right-anchored: header + one cell per play.
        var equityCell = new Regex("""<text x="263\.4" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""");
        var equity = equityCell.Matches(svg).ToList();
        Assert.Equal(4, equity.Count);
        Assert.Equal("Equity", equity[0].Groups[1].Value);
        Assert.DoesNotContain("font-weight=\"bold\"", equity[0].Value);   // header keeps normal weight
        Assert.All(equity.Skip(1), m => Assert.Contains("font-weight=\"bold\"", m.Value));

        // Eq Loss column at x=326.4: header + rows 1 and 2. Row 0 is a best
        // play (EquityLoss == 0.0) and still renders no cell at all — this
        // change moves weight, not the blank-cell contract.
        var lossCell = new Regex("""<text x="326\.4" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""");
        var loss = lossCell.Matches(svg).ToList();
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

    // The longest depth abbreviation the producer emits (nine characters).
    private const string NineCharDepth = "R++p20736";

    // Half the last digit SvgFormat.Number keeps ("0.##"): a rendered
    // coordinate differs from the value the renderer computed by at most this.
    private const double RenderedRounding = 0.005;

    // A four-part move too long to fit beside a nine-character depth on 16:9
    // even at the floor.
    private const string FourPartMove = "24/20 13/9 8/4 6/2";

    /// <summary>A fixed three-play request. <paramref name="longDepth"/> puts
    /// the nine-character abbreviation on one play and an eight-character one
    /// on another; otherwise every depth is at most the header's length.
    /// <paramref name="firstMove"/> is the best play's (and the longest)
    /// move text.</summary>
    private static DiagramRequest LayoutRequest(bool longDepth, PanelPosition side,
        string firstMove = "24/21 13/10")
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.AnalysisPanelPosition = side;
        b.Plays =
        [
            new() { MoveNotation = firstMove,     Equity = 0.50,                    DepthAbbreviation = longDepth ? NineCharDepth : "R++", DepthRank = 3 },
            new() { MoveNotation = "13/10 8/5",   Equity = 0.48, EquityLoss = 0.02, DepthAbbreviation = longDepth ? "B3_20736" : "4-ply", DepthRank = 2 },
            new() { MoveNotation = "8/5 6/5",     Equity = -0.42, EquityLoss = 0.92, DepthAbbreviation = "Book", DepthRank = 0 },
        ];
        return b.Build();
    }

    /// <summary>One rendered text element: its x attribute (as emitted and
    /// parsed), whether it is right-anchored, and its content.</summary>
    private sealed record TextCell(string X, bool AnchorEnd, string Content)
    {
        public double XValue => double.Parse(X, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static readonly Regex TextElement =
        new("""<text x="([0-9.]+)" y="[0-9.]+"( text-anchor="end")?[^>]*>([^<]+)</text>""");

    private static List<TextCell> Cells(string svg, IEnumerable<string> contents)
    {
        var wanted = contents.ToHashSet();
        return TextElement.Matches(svg)
            .Select(m => new TextCell(m.Groups[1].Value, m.Groups[2].Success, m.Groups[3].Value))
            .Where(c => wanted.Contains(c.Content))
            .ToList();
    }

    /// <summary>The panel's left edge and width for <paramref name="request"/>
    /// under <paramref name="svg"/>'s canvas: width from the viewBox less the
    /// fixed board, origin from the layout's own panel placement.</summary>
    private static (double Px, double Pw) Panel(string svg, DiagramRequest request)
    {
        var m = Regex.Match(svg, """viewBox="0 0 ([0-9.]+) [0-9.]+" """);
        double totalWidth = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var layout = BoardLayout.Default with { PanelWidthOverride = totalWidth - BoardLayout.Default.BoardWidth };
        return (layout.PanelX(request.PanelOnLeft), layout.PanelWidth);
    }

    /// <summary>The four column anchors as rendered, read off each column's
    /// header, after asserting every cell of the column shares it and the
    /// numeric columns stay right-anchored while Depth stays left-anchored.</summary>
    private static (string MoveX, string EquityX, string LossX, string DepthX) Anchors(
        string svg, DiagramRequest request)
    {
        var plays = request.Decision.Plays;

        var move = Cells(svg, plays.Select(p => p.MoveNotation));
        Assert.Equal(plays.Count, move.Count);
        Assert.Single(move.Select(c => c.X).Distinct());
        Assert.All(move, c => Assert.False(c.AnchorEnd));

        var equity = Cells(svg, EquityCells(request));
        Assert.Equal(plays.Count + 1, equity.Count);
        Assert.Single(equity.Select(c => c.X).Distinct());
        Assert.All(equity, c => Assert.True(c.AnchorEnd));

        var loss = Cells(svg, plays.Where(p => p.EquityLoss > 0).Select(p => p.EquityLoss.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))
            .Append(DiagramRenderer.PlayPanelLossHeader));
        Assert.Equal(plays.Count(p => p.EquityLoss > 0) + 1, loss.Count);
        Assert.Single(loss.Select(c => c.X).Distinct());
        Assert.All(loss, c => Assert.True(c.AnchorEnd));

        var depth = Cells(svg, plays.Select(p => p.DepthAbbreviation)
            .Append(DiagramRenderer.PlayPanelDepthHeader));
        Assert.Equal(plays.Count + 1, depth.Count);
        Assert.Single(depth.Select(c => c.X).Distinct());
        Assert.All(depth, c => Assert.False(c.AnchorEnd));

        return (move[0].X, equity[0].X, loss[0].X, depth[0].X);
    }

    /// <summary>The anchors the full move reservation produces, from the
    /// rendered move anchor and the renderer's own constants — the layout
    /// before the reservation could yield.</summary>
    private static (string EquityX, string LossX, string DepthX) FullReservationAnchors(string moveX)
    {
        double move = double.Parse(moveX, System.Globalization.CultureInfo.InvariantCulture);
        double equityX = move + DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelMoveReserveEm;
        double lossX = equityX + DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelLossColumnEm;
        double depthX = lossX + DiagramRenderer.PlayPanelFontSize * DiagramRenderer.PlayPanelColumnGapEm;
        return (SvgFormat.Number(equityX), SvgFormat.Number(lossX), SvgFormat.Number(depthX));
    }

    private static double Num(string x) =>
        double.Parse(x, System.Globalization.CultureInfo.InvariantCulture);

    private static double Em(double em) => DiagramRenderer.PlayPanelFontSize * em;

    /// <summary>The Equity column's texts: its header and every play's
    /// value, formatted as the renderer formats them.</summary>
    private static IEnumerable<string> EquityCells(DiagramRequest request) =>
        request.Decision.Plays
            .Select(p => (p.Equity >= 0 ? "+" : "")
                         + p.Equity.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))
            .Append(DiagramRenderer.PlayPanelEquityHeader);

    private static double WidestEquityCell(DiagramRequest request) =>
        EquityCells(request).Max(DiagramRenderer.EstimatePlayPanelTextWidth);

    private static double LongestMove(DiagramRequest request) =>
        request.Decision.Plays.Max(p => DiagramRenderer.EstimatePlayPanelTextWidth(p.MoveNotation));

    /// <summary>The reservation's floor: longest move text, the column gap,
    /// the widest Equity cell.</summary>
    private static double Floor(DiagramRequest request) =>
        LongestMove(request) + Em(DiagramRenderer.PlayPanelColumnGapEm) + WidestEquityCell(request);

    [Fact]
    public void TextWidthEstimate_UnknownGlyph_ChargedTheWidestKnownGlyph()
    {
        // Every character class the table lists; '§' is in none of them.
        const string known = "0123456789 +-_/*().BDELRabdefhikloprqstuvy";
        double widestKnown = known.Max(c => DiagramRenderer.EstimatePlayPanelTextWidth(c.ToString()));
        Assert.Equal(widestKnown, DiagramRenderer.EstimatePlayPanelTextWidth("§"));
    }

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void NineCharDepth_Widescreen_DepthColumnEndsInsidePanel(PanelPosition side)
    {
        var request = LayoutRequest(longDepth: true, side);
        var options = new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 };
        var svg = DiagramRenderer.RenderSvg(request, options);

        var (px, pw) = Panel(svg, request);
        var (moveX, equityX, _, depthX) = Anchors(svg, request);
        double limit = px + pw - DiagramRenderer.PanelMargin;

        // The Depth column's estimated right edge is at or inside the limit —
        // and, the reservation having yielded, exactly at it: the numeric
        // block shifted left by the shortfall and no further.
        double depthRight = Num(depthX) + DiagramRenderer.EstimatePlayPanelTextWidth(NineCharDepth);
        Assert.InRange(depthRight, limit - 2 * RenderedRounding, limit + RenderedRounding);

        // The reservation did yield, and not below its floor: the longest
        // move text, the column gap, and the widest Equity cell.
        double reserve = Num(equityX) - Num(moveX);
        double full = Em(DiagramRenderer.PlayPanelMoveReserveEm);
        double floor = Floor(request);
        Assert.True(reserve < full, $"reservation {reserve} did not yield from {full}");
        Assert.True(reserve >= floor - 2 * RenderedRounding, $"reservation {reserve} below floor {floor}");
    }

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void FourPartMove_NineCharDepth_Widescreen_ReservesExactlyTheFloor(PanelPosition side)
    {
        // Where even the floor does not leave the Depth column inside the
        // panel, the reservation is the floor: move text and Equity never
        // collide, and the Depth column overruns by exactly floor minus room.
        var request = LayoutRequest(longDepth: true, side, FourPartMove);
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });

        var (px, pw) = Panel(svg, request);
        var (moveX, equityX, _, depthX) = Anchors(svg, request);
        double limit = px + pw - DiagramRenderer.PanelMargin;

        double room = limit - DiagramRenderer.EstimatePlayPanelTextWidth(NineCharDepth)
                      - Em(DiagramRenderer.PlayPanelColumnGapEm)
                      - Em(DiagramRenderer.PlayPanelLossColumnEm)
                      - Num(moveX);
        double floor = Floor(request);
        Assert.True(room < floor, $"fixture should not fit: room {room}, floor {floor}");

        Assert.InRange(Num(equityX) - Num(moveX), floor - 2 * RenderedRounding, floor + 2 * RenderedRounding);

        double overrun = Num(depthX) + DiagramRenderer.EstimatePlayPanelTextWidth(NineCharDepth) - limit;
        Assert.InRange(overrun, floor - room - 3 * RenderedRounding, floor - room + 3 * RenderedRounding);
    }

    [Theory]
    [InlineData(false, "24/21 13/10", PanelPosition.Left)]
    [InlineData(false, "24/21 13/10", PanelPosition.Right)]
    [InlineData(true,  "24/21 13/10", PanelPosition.Left)]
    [InlineData(true,  "24/21 13/10", PanelPosition.Right)]
    [InlineData(false, FourPartMove,  PanelPosition.Left)]
    [InlineData(false, FourPartMove,  PanelPosition.Right)]
    [InlineData(true,  FourPartMove,  PanelPosition.Left)]
    [InlineData(true,  FourPartMove,  PanelPosition.Right)]
    public void Widescreen_EquityNeverPrecedesMoveTextPlusGap(bool longDepth, string firstMove, PanelPosition side)
    {
        var request = LayoutRequest(longDepth, side, firstMove);
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });
        var (moveX, equityX, _, _) = Anchors(svg, request);

        double equityLeft = Num(equityX) - WidestEquityCell(request);
        double moveRightPlusGap = Num(moveX) + LongestMove(request) + Em(DiagramRenderer.PlayPanelColumnGapEm);
        Assert.True(equityLeft >= moveRightPlusGap - 2 * RenderedRounding,
            $"equity left edge {equityLeft} precedes move right edge plus gap {moveRightPlusGap}");
    }

    [Theory]
    [InlineData(PanelPosition.Left)]
    [InlineData(PanelPosition.Right)]
    public void ShortDepths_Widescreen_AnchorsAreTheFullReservation(PanelPosition side)
    {
        // Behaviour-neutral pin: with room to spare the reservation does not
        // yield, so every anchor is what the fixed reservation always gave.
        var request = LayoutRequest(longDepth: false, side);
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });

        var (moveX, equityX, lossX, depthX) = Anchors(svg, request);
        Assert.Equal(FullReservationAnchors(moveX), (equityX, lossX, depthX));
    }

    [Theory]
    [InlineData(AspectPreset.Natural, false)]
    [InlineData(AspectPreset.Natural, true)]
    [InlineData(AspectPreset.Standard4x3, false)]
    [InlineData(AspectPreset.Standard4x3, true)]
    public void NarrowPresets_KeepTheFullReservation(AspectPreset aspect, bool longDepth)
    {
        // The narrow presets cannot hold the play panel's columns at all — a
        // pre-existing overflow owned by halheinrich/backgammon#253. The
        // yielding reservation must leave their output exactly as it was;
        // this pins that it does, not that the geometry is right. Retire it
        // with halheinrich/backgammon#253's fix.
        var request = LayoutRequest(longDepth, PanelPosition.Left);
        var svg = DiagramRenderer.RenderSvg(request, new DiagramOptions { Aspect = aspect });

        var (moveX, equityX, lossX, depthX) = Anchors(svg, request);
        Assert.Equal(FullReservationAnchors(moveX), (equityX, lossX, depthX));
    }
}
