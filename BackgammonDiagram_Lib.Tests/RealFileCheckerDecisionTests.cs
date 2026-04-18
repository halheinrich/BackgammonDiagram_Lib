using BackgammonDiagram_Lib.Rendering;
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
        // --- 5 decisions from the first .xg file ----------------------------
        var xgFiles = Directory.GetFiles(TestPaths.XgDir, "*.xg");
        Assert.NotEmpty(xgFiles);
        Array.Sort(xgFiles, StringComparer.Ordinal);

        var xgPairs = TakeCheckerPairs(xgFiles[0], limit: DecisionsPerSource).ToList();

        // --- 5 more decisions from the xgp folder, iterating alphabetically --
        var xgpFiles = Directory.GetFiles(TestPaths.XgpDir, "*.xgp");
        Array.Sort(xgpFiles, StringComparer.Ordinal);

        var xgpPairs = new List<(DiagramRequest Problem, DiagramRequest Solution)>();
        foreach (var xgpPath in xgpFiles)
        {
            int remaining = DecisionsPerSource - xgpPairs.Count;
            if (remaining <= 0) break;
            xgpPairs.AddRange(TakeCheckerPairs(xgpPath, limit: remaining));
        }

        // --- Combined PPTX — Problem then Solution for each decision --------
        var all = xgPairs.Concat(xgpPairs)
            .SelectMany(pair => new[] { pair.Problem, pair.Solution })
            .ToList();
        Assert.NotEmpty(all);

        var pptx = DiagramRenderer.RenderPptx(all, TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_checker_decisions.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 10_000, $"PPTX too small: {pptx.Length} bytes");
    }

    /// <summary>
    /// Parses the file, yields up to <paramref name="limit"/> (Problem,
    /// Solution) pairs — one pair per checker-play (non-cube) decision.
    /// Each decision's Title is set to "{filename} #{index}" so the pair
    /// expansion appends "— Problem" / "— Solution" automatically for
    /// deck traceability.
    /// </summary>
    private static IEnumerable<(DiagramRequest Problem, DiagramRequest Solution)>
        TakeCheckerPairs(string path, int limit)
    {
        var file = XgFileReader.ReadFile(path);
        string fileName = Path.GetFileName(path);
        int i = 0;
        foreach (var data in XgDecisionIterator.IterateDiagramRequests(file))
        {
            if (data.Decision.IsCube) continue;
            i++;
            var req = DiagramRequest.FromDecisionData(data, mode: DiagramMode.Solution);

            // Builder.From preserves every field; only Title is re-set.
            var b = DiagramRequest.Builder.From(req);
            b.Title = $"{fileName} #{i}";
            yield return b.Build().ToProblemSolutionPair();
            if (i >= limit) yield break;
        }
    }
}
