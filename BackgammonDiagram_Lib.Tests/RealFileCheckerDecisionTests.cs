using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.ExportRaster;
using BgDataTypes_Lib;
using ConvertXgToJson_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Integration-style visual test: parses real .xg / .xgp files via
/// ConvertXgToJson_Lib and renders checker-play decisions to a PPTX so the
/// play panel can be eyeballed on realistic fixtures (the synthetic
/// MinimalBuilder has an empty Plays list).
///
/// Cross-submodule dependency — this file is the only reason
/// BackgammonDiagram_Lib.Tests references ConvertXgToJson_Lib.
/// </summary>
[Trait("Category", "Visual")]
public class RealFileCheckerDecisionTests
{
    /// <summary>
    /// Target number of checker-play decisions pulled from the chosen .xg
    /// file and from the .xgp folder, respectively. Each decision is
    /// rendered as two slides (Problem + Solution), so the final PPTX holds
    /// 2 × 2 × this value = 20 slides.
    /// </summary>
    private const int DecisionsPerSource = 5;

    [Fact]
    public void Pptx_CheckerDecisionsFromXgAndXgp()
    {
        // Running counter drives PositionNumber across both sources so slides
        // carry "Position 1" .. "Position 10" end-to-end in the deck.
        int counter = 0;

        // --- 5 decisions from the first .xg file ----------------------------
        var xgFiles = Directory.GetFiles(TestPaths.XgDir, "*.xg");
        Assert.NotEmpty(xgFiles);
        Array.Sort(xgFiles, StringComparer.Ordinal);

        var xgPairs = TakeCheckerPairs(xgFiles[0], limit: DecisionsPerSource, ref counter).ToList();

        // --- 5 more decisions from the xgp folder, iterating alphabetically --
        var xgpFiles = Directory.GetFiles(TestPaths.XgpDir, "*.xgp");
        Array.Sort(xgpFiles, StringComparer.Ordinal);

        var xgpPairs = new List<(DiagramRequest Problem, DiagramRequest Solution)>();
        foreach (var xgpPath in xgpFiles)
        {
            int remaining = DecisionsPerSource - xgpPairs.Count;
            if (remaining <= 0) break;
            xgpPairs.AddRange(TakeCheckerPairs(xgpPath, limit: remaining, ref counter));
        }

        // --- Combined PPTX — Problem then Solution for each decision --------
        var all = xgPairs.Concat(xgpPairs)
            .SelectMany(pair => new[] { pair.Problem, pair.Solution })
            .ToList();
        Assert.NotEmpty(all);

        var pptx = DiagramRasterRenderer.RenderPptx(all, TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_checker_decisions.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 10_000, $"PPTX too small: {pptx.Length} bytes");
    }

    /// <summary>
    /// Parses the file, yields up to <paramref name="limit"/> (Problem,
    /// Solution) pairs — one pair per checker-play (non-cube) decision. The
    /// shared <paramref name="counter"/> is advanced for every pair so the
    /// final deck carries contiguous "Position N" labels across all sources.
    /// </summary>
    private static IEnumerable<(DiagramRequest Problem, DiagramRequest Solution)>
        TakeCheckerPairs(string path, int limit, ref int counter)
    {
        var file = XgFileReader.ReadFile(path);
        var results = new List<(DiagramRequest, DiagramRequest)>();
        int taken = 0;
        foreach (var data in XgDecisionIterator.IterateDiagramRequests(file, Path.GetFileName(path)))
        {
            if (data.Decision.IsCube) continue;
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
