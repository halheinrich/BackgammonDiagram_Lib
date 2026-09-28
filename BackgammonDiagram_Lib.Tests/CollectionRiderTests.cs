using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using BackgammonDiagram_Lib.ExportRaster;
using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The collection rider (Hal, 2026-09-26, on halheinrich/backgammon#273): no
/// public member hands out a live mutable collection or array, behind a
/// read-only interface or as a raw array. The sweep of this repository found
/// three: <see cref="Watermarks.Default"/>'s cached array, the same array (or
/// a caller's) handed back by <see cref="DiagramOptions.WatermarkImage"/>,
/// and <see cref="BoardHitRegions.Points"/>, a live dictionary behind a
/// read-only interface. The renderers' byte results are fresh arrays each
/// call, owned by the caller, so they hand out nothing shared.
/// </summary>
public class CollectionRiderTests
{
    /// <summary>The shipped assemblies' public types.</summary>
    private static IEnumerable<Type> PublicTypes() =>
        new[] { typeof(DiagramRequest).Assembly, typeof(DiagramRasterRenderer).Assembly }
            .SelectMany(a => a.GetExportedTypes());

    [Fact]
    public void NoPublicProperty_IsAnArray()
    {
        // A property hands out stored state; an array is mutable whoever
        // holds it. (A method's fresh result is the caller's own.)
        var offenders = PublicTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(p => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType).IsArray)
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void DiagramOptions_DefaultsToTheBuiltInWatermark()
    {
        Assert.True(new DiagramOptions().WatermarkImage == Watermarks.Default);
    }

    [Fact]
    public void DiagramOptions_RefusesADefaultArray()
    {
        // An uninitialized ImmutableArray holds no bytes, not even none: the
        // opt-out is null.
        var ex = Assert.Throws<ArgumentException>(() => new DiagramOptions { WatermarkImage = default(ImmutableArray<byte>) });
        Assert.Equal(nameof(DiagramOptions.WatermarkImage), ex.ParamName);
        Assert.Null(new DiagramOptions { WatermarkImage = null }.WatermarkImage);
    }

    [Fact]
    public void HitRegions_Points_CannotBeChangedThroughTheInterface()
    {
        // Handed out behind IReadOnlyDictionary, the points were a live
        // Dictionary a caller could cast back and change.
        var points = DiagramRenderer.GetHitRegions(TestFixtures.MinimalRequest(), new DiagramOptions()).Points;

        Assert.True(((ICollection<KeyValuePair<int, HitRect>>)points).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IDictionary)points).Clear());
        Assert.Equal(24, points.Count);
    }

    [Fact]
    public void BoardHitRegions_CopiesThePointsItIsGiven()
    {
        // A change to the dictionary the regions were built from does not
        // reach them.
        var source = new Dictionary<int, HitRect> { [1] = new HitRect(0, 0, 1, 1) };
        var regions = new BoardHitRegions
        {
            ViewBox = new SvgViewBox(0, 0, 10, 10),
            Points = source,
            Bar = new HitRect(0, 0, 1, 1),
        };

        source[2] = new HitRect(1, 1, 1, 1);
        source.Remove(1);

        Assert.Equal([1], regions.Points.Keys);
    }
}
