using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The solution play panel's dual play markers: a bold * at the primary play
/// (<see cref="DecisionData.UserPlayIndex"/>) and a bold † at the render-only
/// <see cref="DiagramRequest.SecondaryPlayIndex"/> overlay. Covers the
/// producer-owned "don't double-mark a coincident row" suppression, the
/// exports-untouched invariant (secondary defaults to −1), and the rescue
/// logic that keeps both marked plays visible with their true rank when they
/// rank beyond the panel's visible window.
/// </summary>
public class DualPlayMarkerTests
{
    private const string Dagger = "†";

    // Marker cells are the only bold text at the play panel's marker column
    // (markerX = 10) at the play-panel font size (14) — see AppendPlayPanel.
    private static readonly Regex MarkerCell = new(
        """<text x="10" y="[0-9.]+" font-family="sans-serif" font-size="14" font-weight="bold" fill="[^"]*">([^<]+)</text>""");

    // Rank cells: left-anchored, no font-weight, at rankX = 22.6 (Medium size).
    private static readonly Regex RankCell = new(
        """<text x="22\.6" y="[0-9.]+" font-family="sans-serif" font-size="14" fill="[^"]*">([0-9]+)</text>""");

    private static List<string> Markers(string svg) =>
        MarkerCell.Matches(svg).Select(m => m.Groups[1].Value).ToList();

    private static List<int> Ranks(string svg) =>
        RankCell.Matches(svg).Select(m => int.Parse(m.Groups[1].Value)).ToList();

    /// <summary>N equity-sorted plays with distinct notations; row 0 is best.</summary>
    private static List<PlayCandidate> MakePlays(int n)
    {
        const double best = 0.5;
        var plays = new List<PlayCandidate>(n);
        for (int i = 0; i < n; i++)
        {
            double eq = best - i * 0.01;
            plays.Add(new PlayCandidate
            {
                MoveNotation = $"m{i}",
                Equity = eq,
                EquityLoss = i == 0 ? 0.0 : best - eq,
            });
        }
        return plays;
    }

    private static DiagramRequest.Builder SolutionBuilder(int playCount)
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.Plays = MakePlays(playCount);
        return b;
    }

    // -----------------------------------------------------------------------
    //  Both marks within the visible set
    // -----------------------------------------------------------------------

    [Fact]
    public void PrimaryAndDifferingSecondary_BothVisible_RenderStarAndDagger()
    {
        var b = SolutionBuilder(5);
        b.UserPlayIndex = 0;
        b.SecondaryPlayIndex = 2;
        var svg = TestFixtures.Render(b.Build());

        // Exactly one * (row 0) then one † (row 2), in emission order.
        Assert.Equal(new[] { "*", Dagger }, Markers(svg));
        // Both rows are ordinary visible rows carrying their natural ranks.
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, Ranks(svg));
    }

    // -----------------------------------------------------------------------
    //  Coincident secondary is suppressed
    // -----------------------------------------------------------------------

    [Fact]
    public void SecondaryEqualsPrimary_RendersOnlyStar()
    {
        var b = SolutionBuilder(5);
        b.UserPlayIndex = 1;
        b.SecondaryPlayIndex = 1;   // producer passes both blindly; collapses to *
        var svg = TestFixtures.Render(b.Build());

        Assert.Equal(new[] { "*" }, Markers(svg));
        Assert.DoesNotContain(Dagger, svg);
    }

    // -----------------------------------------------------------------------
    //  Export shape — secondary unset (−1) leaves output unchanged
    // -----------------------------------------------------------------------

    [Fact]
    public void SecondaryUnset_RendersOnlyStar_AndInactiveSecondaryChangesNothing()
    {
        // The export shape: FromDecisionData/exports never set the overlay, so
        // SecondaryPlayIndex stays −1 and only the * is drawn.
        var unset = SolutionBuilder(5);
        unset.UserPlayIndex = 1;                       // SecondaryPlayIndex defaults to −1
        var unsetSvg = TestFixtures.Render(unset.Build());

        Assert.Equal(new[] { "*" }, Markers(unsetSvg));
        Assert.DoesNotContain(Dagger, unsetSvg);

        // An inactive secondary (here coincident with the primary, so
        // suppressed) must produce byte-identical output to the unset case —
        // proving the dual-marker machinery is inert unless a distinct, valid
        // secondary is supplied. This is the exports-untouched guard.
        var suppressed = SolutionBuilder(5);
        suppressed.UserPlayIndex = 1;
        suppressed.SecondaryPlayIndex = 1;
        var suppressedSvg = TestFixtures.Render(suppressed.Build());

        Assert.Equal(unsetSvg, suppressedSvg);
    }

    // -----------------------------------------------------------------------
    //  Rescue — marked plays beyond the visible window stay visible
    // -----------------------------------------------------------------------

    /// <summary>
    /// Renders a baseline with more plays than fit and no rescue, then reads
    /// back the number of visible rows — that is the panel's fitCount for this
    /// size. Self-calibrating so the rescue tests survive a layout tweak.
    /// </summary>
    private static int MeasureFitCount(int playCount)
    {
        var b = SolutionBuilder(playCount);
        b.UserPlayIndex = 0;   // within view, no rescue
        var ranks = Ranks(TestFixtures.Render(b.Build()));
        return ranks.Count;
    }

    [Fact]
    public void SecondaryBeyondFitCount_IsRescuedWithTrueRank()
    {
        int fit = MeasureFitCount(playCount: 100);
        Assert.True(fit >= 2, $"Panel must fit at least 2 rows; measured {fit}.");

        int total = fit + 10;
        int secondary = total - 1;              // last play, well beyond the cut
        Assert.True(secondary >= fit, "Test premise: secondary must be beyond fitCount.");

        var b = SolutionBuilder(total);
        b.UserPlayIndex = 0;                    // primary stays in view naturally
        b.SecondaryPlayIndex = secondary;
        var svg = TestFixtures.Render(b.Build());

        // Both marks present; * on row 0, † on the rescued last row.
        Assert.Equal(new[] { "*", Dagger }, Markers(svg));

        var ranks = Ranks(svg);
        Assert.Equal(fit, ranks.Count);          // panel didn't grow to fit it
        Assert.Equal(1, ranks[0]);               // primary keeps rank 1
        Assert.Equal(secondary + 1, ranks[^1]);  // secondary shown with its real rank
    }

    [Fact]
    public void BothMarksBeyondFitCount_BothRescued()
    {
        int fit = MeasureFitCount(playCount: 100);
        Assert.True(fit >= 2, $"Panel must fit at least 2 rows; measured {fit}.");

        int total = fit + 10;
        int primary = total - 2;
        int secondary = total - 1;
        Assert.True(primary >= fit, "Test premise: both marks must be beyond fitCount.");

        var b = SolutionBuilder(total);
        b.UserPlayIndex = primary;
        b.SecondaryPlayIndex = secondary;
        var svg = TestFixtures.Render(b.Build());

        // Both rescued to the foot of the panel in rank order: * then †.
        Assert.Equal(new[] { "*", Dagger }, Markers(svg));

        var ranks = Ranks(svg);
        Assert.Equal(fit, ranks.Count);              // panel didn't grow
        Assert.Equal(primary + 1, ranks[^2]);        // primary's true rank
        Assert.Equal(secondary + 1, ranks[^1]);      // secondary's true rank
    }
}
