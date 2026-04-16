using System.Text.Json;
using BgDataTypes_Lib;
using BackgammonDiagram_Lib.Rendering;
using QuestPDF.Infrastructure;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Drives the full SVG/PNG/PPTX/PDF pipeline from every JSON decision fixture
/// found in <see cref="TestPaths.DiagramRequestDir"/>. No fixture file name is
/// hard-coded — any *.json dropped into that directory becomes a test case on
/// the next run. If the directory is missing or empty, the tests pass without
/// running, which tolerates the convention that TestData contents are not
/// checked in.
/// </summary>
[Trait("Category", "Visual")]
public class DecisionDataDiagramTests
{
    // Sentinel theory row used when the fixture directory is absent or empty,
    // so MemberData never yields zero rows (xUnit 2 treats that as a failure).
    private const string NoFixturesSentinel = "(no fixtures)";

    static DecisionDataDiagramTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // -----------------------------------------------------------------------
    //  Theory data — enumerate *.json files under TestData\DiagramRequest
    // -----------------------------------------------------------------------

    public static IEnumerable<object[]> JsonFixtures()
    {
        var dir = TestPaths.DiagramRequestDir;
        if (!Directory.Exists(dir))
        {
            yield return new object[] { NoFixturesSentinel };
            yield break;
        }

        var files = Directory.GetFiles(dir, "*.json");
        if (files.Length == 0)
        {
            yield return new object[] { NoFixturesSentinel };
            yield break;
        }

        foreach (var f in files.OrderBy(static x => x, StringComparer.Ordinal))
            yield return new object[] { Path.GetFileName(f) };
    }

    // -----------------------------------------------------------------------
    //  Per-format tests — split so one format's failure does not mask others
    // -----------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(JsonFixtures))]
    public void RenderFromJson_Svg(string filename)
    {
        if (filename == NoFixturesSentinel) return;

        var (requests, names) = LoadAndBuild(filename);
        var options = TestFixtures.DefaultOptions();

        for (int i = 0; i < requests.Length; i++)
        {
            var svg = DiagramRenderer.RenderSvg(requests[i], options);
            Assert.Contains("<svg", svg);
            Assert.Contains("<circle", svg);
            var path = TestPaths.SvgOutputPath($"{names[i]}.svg");
            File.WriteAllText(path, svg);
            Assert.True(File.Exists(path));
        }
    }

    [Theory]
    [MemberData(nameof(JsonFixtures))]
    public void RenderFromJson_Png(string filename)
    {
        if (filename == NoFixturesSentinel) return;

        var (requests, names) = LoadAndBuild(filename);
        var options = TestFixtures.DefaultOptions();

        for (int i = 0; i < requests.Length; i++)
        {
            var png = DiagramRenderer.RenderPng(requests[i], options);
            Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
            var path = TestPaths.PngOutputPath($"{names[i]}.png");
            File.WriteAllBytes(path, png);
            Assert.True(File.Exists(path));
        }
    }

    [Theory]
    [MemberData(nameof(JsonFixtures))]
    public void RenderFromJson_Pptx(string filename)
    {
        if (filename == NoFixturesSentinel) return;

        var (requests, _) = LoadAndBuild(filename);
        var options = TestFixtures.DefaultOptions();

        var pptx = DiagramRenderer.RenderPptx(requests, options);
        var outputName = Path.GetFileNameWithoutExtension(filename) + "_all.pptx";
        var path = TestPaths.PptxOutputPath(outputName);
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Theory]
    [MemberData(nameof(JsonFixtures))]
    public void RenderFromJson_Pdf(string filename)
    {
        if (filename == NoFixturesSentinel) return;

        var (requests, _) = LoadAndBuild(filename);
        var options = TestFixtures.DefaultOptions();

        var pdf = DiagramRenderer.RenderPdf(requests, options);
        var outputName = Path.GetFileNameWithoutExtension(filename) + "_all.pdf";
        var path = TestPaths.PdfOutputPath(outputName);
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  Fixture load + request assembly
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loads the named JSON fixture, picks one play and one cube decision,
    /// and expands each into problem/solution × left-panel/right-panel — eight
    /// DiagramRequests per fixture, with matching short names for output files.
    /// </summary>
    private static (DiagramRequest[] Requests, string[] Names) LoadAndBuild(string filename)
    {
        var jsonPath = TestPaths.DiagramRequestJson(filename);
        Assert.True(File.Exists(jsonPath), $"JSON file not found: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var decisions = JsonSerializer.Deserialize<List<BgDecisionData>>(json, s_jsonOptions);
        Assert.NotNull(decisions);
        Assert.NotEmpty(decisions);

        var play = decisions.First(d => !d.Decision.IsCube);
        var cube = decisions.First(d => d.Decision.IsCube);

        // Base name derived from the fixture file, not the hard-coded "ajhh".
        var baseName = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();

        var playL = FromDecisionData(play);
        var cubeL = FromDecisionData(cube);
        var (playProbL, playSolL) = playL.ToProblemSolutionPair();
        var (cubeProbL, cubeSolL) = cubeL.ToProblemSolutionPair();

        var playR = FromDecisionData(play, analysisPanelPosition: PanelPosition.Right);
        var cubeR = FromDecisionData(cube, analysisPanelPosition: PanelPosition.Right);
        var (playProbR, playSolR) = playR.ToProblemSolutionPair();
        var (cubeProbR, cubeSolR) = cubeR.ToProblemSolutionPair();

        var requests = new[]
        {
            playProbL, playSolL, cubeProbL, cubeSolL,
            playProbR, playSolR, cubeProbR, cubeSolR
        };
        var names = new[]
        {
            $"{baseName}_play_problem",        $"{baseName}_play_solution",
            $"{baseName}_cube_problem",        $"{baseName}_cube_solution",
            $"{baseName}_play_problem_right",  $"{baseName}_play_solution_right",
            $"{baseName}_cube_problem_right",  $"{baseName}_cube_solution_right",
        };
        return (requests, names);
    }

    // -----------------------------------------------------------------------
    //  BgDecisionData → DiagramRequest
    // -----------------------------------------------------------------------

    private static DiagramRequest FromDecisionData(
        BgDecisionData data,
        DiagramMode mode = DiagramMode.Solution,
        bool homeBoardOnRight = true,
        bool onRollAtBottom = true,
        PanelPosition analysisPanelPosition = PanelPosition.Left)
    {
        return new DiagramRequest.Builder
        {
            // Position
            Mop = [.. data.Position.Mop],
            OnRollNeeds = data.Position.OnRollNeeds,
            OpponentNeeds = data.Position.OpponentNeeds,
            OnRollPipCount = data.Position.OnRollPipCount,
            OpponentPipCount = data.Position.OpponentPipCount,
            CubeSize = data.Position.CubeSize,
            CubeOwner = data.Position.CubeOwner,
            IsCrawford = data.Position.IsCrawford,

            // Decision
            IsCube = data.Decision.IsCube,
            Dice = [.. data.Decision.Dice],
            Plays = [.. data.Decision.Plays],
            AnalysisDepths = [.. data.Decision.AnalysisDepths],
            BestPlayIndex = data.Decision.BestPlayIndex,
            UserPlayIndex = data.Decision.UserPlayIndex,
            NoDoubleEquity = data.Decision.NoDoubleEquity,
            DoubleTakeEquity = data.Decision.DoubleTakeEquity,
            WinPctAfterNoDouble = data.Decision.WinPctAfterNoDouble,
            GammonPctAfterNoDouble = data.Decision.GammonPctAfterNoDouble,
            BgPctAfterNoDouble = data.Decision.BgPctAfterNoDouble,
            LosePctAfterNoDouble = data.Decision.LosePctAfterNoDouble,
            LoseGammonPctAfterNoDouble = data.Decision.LoseGammonPctAfterNoDouble,
            LoseBgPctAfterNoDouble = data.Decision.LoseBgPctAfterNoDouble,
            WinPctAfterDoubleTake = data.Decision.WinPctAfterDoubleTake,
            GammonPctAfterDoubleTake = data.Decision.GammonPctAfterDoubleTake,
            BgPctAfterDoubleTake = data.Decision.BgPctAfterDoubleTake,
            LosePctAfterDoubleTake = data.Decision.LosePctAfterDoubleTake,
            LoseGammonPctAfterDoubleTake = data.Decision.LoseGammonPctAfterDoubleTake,
            LoseBgPctAfterDoubleTake = data.Decision.LoseBgPctAfterDoubleTake,
            ProbOfOpponentErrorJustifyingDouble = data.Decision.ProbOfOpponentErrorJustifyingDouble,

            // Descriptive
            OnRollName = data.Descriptive.OnRollName,
            OpponentName = data.Descriptive.OpponentName,
            Title = data.Descriptive.Title,
            MatchLength = data.Descriptive.MatchLength,
            Date = data.Descriptive.Date,
            Event = data.Descriptive.Event,

            // Diagram-specific
            Mode = mode,
            HomeBoardOnRight = homeBoardOnRight,
            OnRollAtBottom = onRollAtBottom,
            AnalysisPanelPosition = analysisPanelPosition,
        }.Build();
    }
}
