using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The solution play panel's dual play markers: a bold * at the primary play
/// (<see cref="CheckerPlayDecisionData.UserPlayIndex"/>) and a bold † at the
/// consumer's <see cref="DiagramRequest.SecondaryPlayIndex"/> overlay. Covers
/// the renderer-owned "don't double-mark a coincident row" suppression, the
/// unset overlay (null, the one spelling of none) leaving the output
/// untouched, and the rescue that keeps both marked plays visible with their
/// true rank when they rank beyond the panel's visible window.
/// </summary>
public class DualPlayMarkerTests
{
    private const string Dagger = "†";

    /// <summary>
    /// A solution over <paramref name="playCount"/> distinct candidates in
    /// equity order, the first best, with the user's play at
    /// <paramref name="userPlayIndex"/>.
    /// </summary>
    private static DiagramRequest Solution(int playCount, int? userPlayIndex) =>
        TestFixtures.RequestFor(
            TestFixtures.CheckerPlayWith(TestFixtures.HopCandidates(playCount), userPlayIndex),
            DiagramMode.Solution);

    private static List<string?> Markers(string svg) =>
        [.. PlayPanelReader.Rows(svg).Where(r => r.Marker is not null).Select(r => r.Marker)];

    // -----------------------------------------------------------------------
    //  Both marks within the visible set
    // -----------------------------------------------------------------------

    [Fact]
    public void PrimaryAndDifferingSecondary_BothVisible_RenderStarAndDagger()
    {
        var svg = TestFixtures.Render(Solution(5, userPlayIndex: 0) with { SecondaryPlayIndex = 2 });

        // Exactly one * (row 0) then one † (row 2), in emission order.
        Assert.Equal(["*", Dagger], Markers(svg));
        // Both rows are ordinary visible rows carrying their natural ranks.
        Assert.Equal([1, 2, 3, 4, 5], PlayPanelReader.Ranks(svg));
        Assert.Equal(3, PlayPanelReader.RowMarked(svg, Dagger).Rank);
    }

    // -----------------------------------------------------------------------
    //  Coincident secondary is suppressed
    // -----------------------------------------------------------------------

    [Fact]
    public void SecondaryEqualsPrimary_RendersOnlyStar()
    {
        // A consumer passes both blindly; they collapse to one *.
        var svg = TestFixtures.Render(Solution(5, userPlayIndex: 1) with { SecondaryPlayIndex = 1 });

        Assert.Equal(["*"], Markers(svg));
        Assert.DoesNotContain(Dagger, svg);
    }

    // -----------------------------------------------------------------------
    //  Unset (null) and inactive secondaries change nothing
    // -----------------------------------------------------------------------

    [Fact]
    public void SecondaryUnset_RendersOnlyStar_AndInactiveSecondaryChangesNothing()
    {
        // The export shape: no overlay, so SecondaryPlayIndex is null and only
        // the * is drawn.
        var unset = Solution(5, userPlayIndex: 1);
        Assert.Null(unset.SecondaryPlayIndex);
        var unsetSvg = TestFixtures.Render(unset);

        Assert.Equal(["*"], Markers(unsetSvg));
        Assert.DoesNotContain(Dagger, unsetSvg);

        // An inactive secondary (coincident with the primary, so suppressed)
        // renders byte-identically to the unset one: the dual-marker machinery
        // is inert unless a distinct secondary is supplied.
        Assert.Equal(unsetSvg, TestFixtures.Render(unset with { SecondaryPlayIndex = 1 }));
    }

    [Fact]
    public void NoUserPlay_SecondaryAloneRendersDagger()
    {
        // The record states no user play (null, not -1): the secondary is
        // distinct from "none", so it marks alone.
        var svg = TestFixtures.Render(Solution(5, userPlayIndex: null) with { SecondaryPlayIndex = 3 });

        Assert.Equal([Dagger], Markers(svg));
        Assert.Equal(4, PlayPanelReader.RowMarked(svg, Dagger).Rank);
    }

    // -----------------------------------------------------------------------
    //  Rescue — marked plays beyond the visible window stay visible
    // -----------------------------------------------------------------------

    /// <summary>The most candidates the fixtures offer: every hop from the standard start.</summary>
    private static readonly int MaxPlays = TestFixtures.StandardStartHops.Count;

    /// <summary>
    /// Renders a panel overfilled with candidates and no rescue, then reads
    /// back the number of visible rows — the panel's fitCount at this size.
    /// Self-calibrating so the rescue tests survive a layout tweak.
    /// </summary>
    private static int MeasureFitCount()
    {
        int fit = PlayPanelReader.Ranks(TestFixtures.Render(Solution(MaxPlays, userPlayIndex: 0))).Count;
        Assert.True(fit >= 2 && fit < MaxPlays, $"Panel must fit at least 2 rows and fewer than {MaxPlays}; measured {fit}.");
        return fit;
    }

    [Fact]
    public void SecondaryBeyondFitCount_IsRescuedWithTrueRank()
    {
        int fit = MeasureFitCount();
        int secondary = MaxPlays - 1;              // last play, well beyond the cut

        var svg = TestFixtures.Render(Solution(MaxPlays, userPlayIndex: 0) with { SecondaryPlayIndex = secondary });

        // Both marks present; * on row 0, † on the rescued last row.
        Assert.Equal(["*", Dagger], Markers(svg));

        var ranks = PlayPanelReader.Ranks(svg);
        Assert.Equal(fit, ranks.Count);            // panel didn't grow to fit it
        Assert.Equal(1, ranks[0]);                 // primary keeps rank 1
        Assert.Equal(secondary + 1, ranks[^1]);    // secondary shown with its real rank
    }

    [Fact]
    public void BothMarksBeyondFitCount_BothRescued()
    {
        int fit = MeasureFitCount();
        int primary = MaxPlays - 2;
        int secondary = MaxPlays - 1;
        Assert.True(primary >= fit, "Test premise: both marks must be beyond fitCount.");

        var svg = TestFixtures.Render(Solution(MaxPlays, userPlayIndex: primary) with { SecondaryPlayIndex = secondary });

        // Both rescued to the foot of the panel in rank order: * then †.
        Assert.Equal(["*", Dagger], Markers(svg));

        var ranks = PlayPanelReader.Ranks(svg);
        Assert.Equal(fit, ranks.Count);                // panel didn't grow
        Assert.Equal(primary + 1, ranks[^2]);          // primary's true rank
        Assert.Equal(secondary + 1, ranks[^1]);        // secondary's true rank
    }
}
