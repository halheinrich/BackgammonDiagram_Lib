using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using ConvertXgToJson_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Parallel to <see cref="RealFileCheckerDecisionTests"/> but selecting
/// cube decisions (IsCube == true) for visual smoke-testing the cube
/// analysis panel on realistic fixtures.
/// </summary>
[Trait("Category", "Visual")]
public class RealFileCubeDecisionTests
{
    /// <summary>
    /// Decisions per source: 5 from the first .xg file, 5 more accumulating
    /// from the alphabetical .xgp sequence. Each becomes a Problem + Solution
    /// pair, so the PPTX carries 20 slides total.
    /// </summary>
    private const int DecisionsPerSource = 5;

    [Fact]
    public void Pptx_CubeDecisionsFromXgAndXgp()
    {
        int counter = 0;

        // --- 5 cube decisions from the first .xg file -----------------------
        var xgFiles = Directory.GetFiles(TestPaths.XgDir, "*.xg");
        Assert.NotEmpty(xgFiles);
        Array.Sort(xgFiles, StringComparer.Ordinal);

        var xgPairs = TakeCubePairs(xgFiles[0], limit: DecisionsPerSource, ref counter).ToList();

        // --- 5 more decisions from the xgp folder, iterating alphabetically --
        var xgpFiles = Directory.GetFiles(TestPaths.XgpDir, "*.xgp");
        Array.Sort(xgpFiles, StringComparer.Ordinal);

        var xgpPairs = new List<(DiagramRequest Problem, DiagramRequest Solution)>();
        foreach (var xgpPath in xgpFiles)
        {
            int remaining = DecisionsPerSource - xgpPairs.Count;
            if (remaining <= 0) break;
            xgpPairs.AddRange(TakeCubePairs(xgpPath, limit: remaining, ref counter));
        }

        var all = xgPairs.Concat(xgpPairs)
            .SelectMany(pair => new[] { pair.Problem, pair.Solution })
            .ToList();
        Assert.NotEmpty(all);

        var pptx = DiagramRenderer.RenderPptx(all, TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_cube_decisions.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 10_000, $"PPTX too small: {pptx.Length} bytes");
    }

    /// <summary>
    /// Parses the file, yields up to <paramref name="limit"/> (Problem,
    /// Solution) pairs — one pair per cube decision. The shared
    /// <paramref name="counter"/> advances per pair so the deck carries
    /// contiguous "Position N" labels across sources.
    /// </summary>
    private static IEnumerable<(DiagramRequest Problem, DiagramRequest Solution)>
        TakeCubePairs(string path, int limit, ref int counter)
    {
        var file = XgFileReader.ReadFile(path);
        var results = new List<(DiagramRequest, DiagramRequest)>();
        int taken = 0;
        foreach (var data in XgDecisionIterator.IterateDiagramRequests(file))
        {
            if (!data.Decision.IsCube) continue;
            taken++;
            counter++;
            var req = DiagramRequest.FromDecisionData(data, mode: DiagramMode.Solution);
            var b = DiagramRequest.Builder.From(req);
            b.PositionNumber = counter;
            results.Add(b.Build().ToProblemSolutionPair());
            if (taken >= limit) break;
        }
        return results;
    }
}
