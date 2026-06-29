using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Tests the bear-off tray rendering: off-count derivation from Mop, the
/// [1,14] trigger rule, hit-region population, and the turned-cube vertical
/// shift to the rail edge (which makes room for the tray).
/// </summary>
public class BearOffTests
{
    // Signature of a bear-off bar rect — distinctive enough to count.
    private const string BarSignature = "stroke=\"#888\" stroke-width=\"0.5\"/>";

    // -----------------------------------------------------------------------
    //  Render rule — [1,14] trigger band
    // -----------------------------------------------------------------------

    [Fact]
    public void StartingPosition_HasNoBearOffBars()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();  // 15 on-board each → 0 off each

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));

        // Only the cube rect uses stroke-width="0.5" under a zero-off position.
        Assert.Equal(1, strokedRects);
    }

    [Fact]
    public void AllCheckersOff_DoesNotRenderBars()
    {
        // Upper edge of the trigger band — 15 off is outside [1,14].
        var b = TestFixtures.MinimalBuilder();
        b.Mop = new int[26];

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));
        Assert.Equal(1, strokedRects);  // cube only
    }

    [Fact]
    public void OneOnRollOff_RendersOneBar()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 1, opponentOff: 0);

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));
        Assert.Equal(2, strokedRects);  // cube + 1 on-roll bar
    }

    [Fact]
    public void SevenOnRollOff_RendersSevenBars()
    {
        // Corresponds to the user's ||||| || sketch.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 7, opponentOff: 0);

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));
        Assert.Equal(8, strokedRects);  // cube + 7 bars
    }

    [Fact]
    public void BothPlayersBearingOff_RendersBothStacks()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 3, opponentOff: 5);

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));
        Assert.Equal(9, strokedRects);  // cube + 3 + 5
    }

    [Fact]
    public void FourteenOnRollOff_RendersFourteenBars()
    {
        // Top of the trigger band — 14 off is the largest count that still renders.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 14, opponentOff: 0);

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));
        Assert.Equal(15, strokedRects);  // cube + 14 bars
    }

    // -----------------------------------------------------------------------
    //  Hit-region population
    // -----------------------------------------------------------------------

    [Fact]
    public void GetHitRegions_StartingPosition_OnRollTrayPresentOpponentNull()
    {
        // At the start of play both players have 0 off. The on-roll tray now
        // renders from 0 (so the first checker can be borne off by clicking an
        // empty tray); the opponent tray is display-only and stays hidden until
        // it has something to show.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        Assert.NotNull(regions.OnRollTray);
        Assert.Null(regions.OpponentTray);
    }

    // -----------------------------------------------------------------------
    //  On-roll tray extended to 0 off — E1 regression
    // -----------------------------------------------------------------------

    [Fact]
    public void GetHitRegions_ZeroOnRollOff_OnRollTrayPopulated()
    {
        // E1 regression: with all 15 on the board (0 off), the on-roll tray's
        // hit-region was gated out at the old [1,14] floor, so the first
        // checker couldn't be borne off by clicking the tray. It must now be
        // present at 0.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 0, opponentOff: 0);  // 15 on board each
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        Assert.NotNull(regions.OnRollTray);
    }

    [Fact]
    public void GetHitRegions_ZeroOpponentOff_OpponentTrayHidden()
    {
        // The opponent tray keeps its [1,14] floor (display-only, nothing to
        // click), so 0 opponent-off leaves it null even though the on-roll
        // tray now shows at 0.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 0, opponentOff: 0);
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        Assert.Null(regions.OpponentTray);
    }

    [Fact]
    public void GetHitRegions_AllOnRollOff_OnRollTrayNull()
    {
        // Upper edge of the band: 15 off is all checkers borne off (game over),
        // which is outside [0,14] — no tray.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 15, opponentOff: 0);
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        Assert.Null(regions.OnRollTray);
    }

    [Fact]
    public void EmptyOnRollTray_AtZeroOff_RendersNoOnRollBars()
    {
        // The empty on-roll tray is the rail region with no checkers — it must
        // draw zero bars. Pair it with a non-empty opponent stack so the count
        // pins the on-roll contribution exactly: total stroked rects must equal
        // cube (1) + the opponent's bars, with nothing added by the on-roll
        // tray. If the count-0 stack glitched into drawing anything, this trips.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 0, opponentOff: 7);

        int strokedRects = CountStroke05Rects(TestFixtures.Render(b.Build()));
        Assert.Equal(8, strokedRects);  // cube + 7 opponent bars + 0 on-roll
    }

    [Fact]
    public void GetHitRegions_EmptyOnRollTray_DoesNotOverlapCubeSlots()
    {
        // Mirror of the finding-#4 overlap guard, for the tray now drawn at 0
        // off. The on-roll tray occupies its half of the left rail — the band
        // between the centered-cube slot and the on-roll player's turned-cube
        // slot — and must abut, not overlap, either neighbour. (It necessarily
        // sits inside the full-rail Cube hit-region, which is a deliberate
        // catch-all column, so the guard is against the cube *slots*, derived
        // from the same geometry the renderer uses.)
        var b = TestFixtures.MinimalBuilder();
        b.OnRollAtBottom = true;
        b.Mop = MopFor(onRollOff: 0, opponentOff: 0);
        var request = b.Build();
        var regions = DiagramRenderer.GetHitRegions(request, new DiagramOptions());

        var tray = regions.OnRollTray;
        Assert.NotNull(tray);

        var baseLayout = BoardLayout.Default;
        double panelWidth = regions.ViewBox.Width - baseLayout.BoardWidth;
        var layout = baseLayout with { PanelWidthOverride = panelWidth };
        double titleOffset = regions.ViewBox.Height - layout.BoardHeight;

        // Tray stays within the left-rail X span.
        Assert.Equal(layout.LeftRailX(request.PanelOnLeft), tray!.X, 2);
        Assert.Equal(layout.LeftRailWidth, tray.Width, 2);

        // Cube-slot geometry, re-derived as the renderer computes it.
        double cubeSize = layout.LeftRailWidth * 0.7;
        double centeredCubeBottom = layout.BoardHeight / 2 + cubeSize / 2;
        const double turnedCubeEdgeMargin = 8;  // DiagramRenderer.TurnedCubeEdgeMargin
        double onRollTurnedTop = layout.BoardHeight - turnedCubeEdgeMargin - cubeSize;

        double trayTop = tray.Y;
        double trayBottom = tray.Y + tray.Height;

        // Below the centered cube (no intrusion into the centre slot)...
        Assert.True(trayTop >= centeredCubeBottom + titleOffset - 0.01,
            $"Tray top {trayTop} overlaps the centered-cube slot " +
            $"(bottom {centeredCubeBottom + titleOffset}).");
        // ...and above the on-roll turned-cube slot at the bottom edge.
        Assert.True(trayBottom <= onRollTurnedTop + titleOffset + 0.01,
            $"Tray bottom {trayBottom} overlaps the turned-cube slot " +
            $"(top {onRollTurnedTop + titleOffset}).");
        // And it stays on the board.
        Assert.True(trayBottom <= layout.BoardHeight + titleOffset + 0.01,
            $"Tray bottom {trayBottom} runs past the board.");
    }

    [Fact]
    public void GetHitRegions_OnlyOnRollBearingOff_OnRollTrayPopulatedOpponentNull()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 2, opponentOff: 0);
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        Assert.NotNull(regions.OnRollTray);
        Assert.Null(regions.OpponentTray);
    }

    [Fact]
    public void GetHitRegions_OpponentBearingOffOnRollNotYet_BothTraysPresent()
    {
        // Opponent has borne off, on-roll has not (0 off). The opponent tray is
        // populated as before; the on-roll tray is now also present (empty) so
        // the on-roll player can start bearing off by clicking it.
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 0, opponentOff: 4);
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        Assert.NotNull(regions.OnRollTray);
        Assert.NotNull(regions.OpponentTray);
    }

    [Fact]
    public void GetHitRegions_Trays_OccupyLeftRailXSpan()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 4, opponentOff: 6);
        var request = b.Build();
        var regions = DiagramRenderer.GetHitRegions(request, new DiagramOptions());

        Assert.NotNull(regions.OnRollTray);
        Assert.NotNull(regions.OpponentTray);

        // Re-derive the renderer's layout from the regions' ViewBox so this
        // assertion holds regardless of the aspect-driven PanelWidth and the
        // request's panel side. Tray X must sit at the left rail of that
        // (renderer-consistent) coordinate system.
        var baseLayout = BoardLayout.Default;
        double panelWidth = regions.ViewBox.Width - baseLayout.BoardWidth;
        var layout = baseLayout with { PanelWidthOverride = panelWidth };

        foreach (var tray in new[] { regions.OnRollTray!, regions.OpponentTray! })
        {
            Assert.Equal(layout.LeftRailX(request.PanelOnLeft), tray.X, 2);
            Assert.Equal(layout.LeftRailWidth, tray.Width, 2);
        }
    }

    [Fact]
    public void GetHitRegions_Trays_DontOverlapVertically()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = MopFor(onRollOff: 5, opponentOff: 5);
        var regions = DiagramRenderer.GetHitRegions(b.Build(), new DiagramOptions());

        var onRoll = regions.OnRollTray!;
        var opp = regions.OpponentTray!;
        // One must be entirely above the other — no overlap.
        bool noOverlap = onRoll.Y + onRoll.Height <= opp.Y || opp.Y + opp.Height <= onRoll.Y;
        Assert.True(noOverlap,
            $"Trays overlap: onRoll={onRoll.Y}-{onRoll.Y + onRoll.Height}, opp={opp.Y}-{opp.Y + opp.Height}");
    }

    // -----------------------------------------------------------------------
    //  Turned-cube position — pinned to rail edge
    // -----------------------------------------------------------------------

    [Fact]
    public void OnRollOwnedCube_AtBottomEdgeOfLeftRail()
    {
        var layout = BoardLayout.Default;
        double cubeSize = layout.LeftRailWidth * 0.7;
        var b = TestFixtures.MinimalBuilder();
        b.CubeOwner = CubeOwner.OnRoll;
        b.OnRollAtBottom = true;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Turned cube y = BoardHeight − margin(8) − cubeSize.
        double expectedY = layout.BoardHeight - 8 - cubeSize;
        Assert.Contains($"y=\"{expectedY:0.##}\"", svg);
    }

    [Fact]
    public void OpponentOwnedCube_AtTopEdgeOfLeftRail()
    {
        var b = TestFixtures.MinimalBuilder();
        b.CubeOwner = CubeOwner.Opponent;
        b.OnRollAtBottom = true;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        // Opponent-owned with on-roll at bottom → opp is at top → y = 8.
        Assert.Contains("y=\"8\"", svg);
    }

    [Fact]
    public void CenteredCube_UnchangedByBearOffRefactor()
    {
        var layout = BoardLayout.Default;
        double cubeSize = layout.LeftRailWidth * 0.7;
        var b = TestFixtures.MinimalBuilder();
        b.CubeOwner = CubeOwner.Centered;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        double expectedY = layout.BoardHeight / 2 - cubeSize / 2;
        Assert.Contains($"y=\"{expectedY:0.##}\"", svg);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Counts rects stamped with stroke="#888" stroke-width="0.5" — the
    /// distinctive signature of a bear-off bar (and the cube, which also
    /// uses a 0.5 stroke). Tests subtract the cube contribution explicitly.
    /// </summary>
    private static int CountStroke05Rects(string svg) =>
        TestFixtures.CountOccurrences(svg, BarSignature);

    /// <summary>
    /// Builds a valid Mop with the given target off-counts by placing all
    /// on-board checkers on a single home-board point (or leaving the board
    /// empty for that player when all are off). Off-count is computed as
    /// 15 − (checkers on board), so stacking N on one point is enough.
    /// </summary>
    private static int[] MopFor(int onRollOff, int opponentOff)
    {
        var mop = new int[26];
        int onRollOn = 15 - onRollOff;
        int opponentOn = 15 - opponentOff;

        if (onRollOn > 0)   mop[1]  =  onRollOn;     // on-roll home point
        if (opponentOn > 0) mop[24] = -opponentOn;   // opp home point
        return mop;
    }
}
