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
}
