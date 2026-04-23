using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Invariants of the checker-play analysis panel — the contract the renderer
/// owes its caller for <see cref="DiagramRequest.Decision"/>.<see cref="DecisionData.Plays"/>:
///
///   * Caller order is preserved verbatim (no implicit re-sort inside the
///     renderer).
///   * <see cref="PlayCandidate.EquityLoss"/> is rendered as text for every
///     non-best entry; omitted only when the field is null or &lt;= 0.
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
        // carries EquityLoss = null; each subsequent loss is bestEquity -
        // thisEquity. Values chosen so every formatted loss is a unique
        // substring — makes the negative "absent for best" assertion clean.
        var plays = new List<PlayCandidate>
        {
            new() { MoveNotation = "8/5 6/5",     Equity = 0.50, EquityLoss = null },
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
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Depth cells: header + 3 rows at x=347.4.
        var depthCell = new Regex("""<text x="347\.4" y="[0-9.]+" font-family="sans-serif"[^>]*>([^<]+)</text>""");
        var depth = depthCell.Matches(svg).ToList();
        Assert.Equal(4, depth.Count);
        Assert.Equal("Depth", depth[0].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[0].Value);   // header never italic
        Assert.Equal("R+", depth[1].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[1].Value);   // row 0: no predecessor
        Assert.Equal("3p1296", depth[2].Groups[1].Value);
        Assert.Contains("font-style=\"italic\"", depth[2].Value);         // row 1: 10 > 5 → italic
        Assert.Equal("R+", depth[3].Groups[1].Value);
        Assert.DoesNotContain("font-style=\"italic\"", depth[3].Value);   // row 2: 5 <= 10

        // Equity cells: header ("Equity") + 3 rows at x=263.4, right-anchored.
        var equityCell = new Regex("""<text x="263\.4" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""");
        var equity = equityCell.Matches(svg).ToList();
        Assert.Equal(4, equity.Count);
        Assert.DoesNotContain("font-style=\"italic\"", equity[0].Value);  // header
        Assert.DoesNotContain("font-style=\"italic\"", equity[1].Value);  // row 0
        Assert.Contains("font-style=\"italic\"", equity[2].Value);        // row 1 inverted
        Assert.DoesNotContain("font-style=\"italic\"", equity[3].Value);  // row 2

        // Eq Loss cells: header ("Eq Loss") + rows 1 and 2 at x=326.4 (row 0
        // has null loss and renders no cell). Header is the only non-numeric
        // content in the column, so picking by row content is unambiguous.
        var lossCell = new Regex("""<text x="326\.4" y="[0-9.]+" text-anchor="end" [^>]*>([^<]+)</text>""");
        var loss = lossCell.Matches(svg).ToList();
        Assert.Equal(3, loss.Count);
        Assert.DoesNotContain("font-style=\"italic\"", loss[0].Value);    // header
        Assert.Contains("font-style=\"italic\"", loss[1].Value);          // row 1 inverted
        Assert.DoesNotContain("font-style=\"italic\"", loss[2].Value);    // row 2
    }
}
