using System.Text.Json;
using BgDataTypes_Lib;
using BackgammonDiagram_Lib.Rendering;
using QuestPDF.Infrastructure;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

[Trait("Category", "Visual")]
public class DecisionDataDiagramTests
{
    static DecisionDataDiagramTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Ajhh_PlayAndCube_ProblemSolutionPairs()
    {
        // ── Load JSON ──────────────────────────────────────────────────
        var jsonPath = TestPaths.DiagramRequestJson("AJHH_20260413.json");
        Assert.True(File.Exists(jsonPath), $"JSON file not found: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var decisions = JsonSerializer.Deserialize<List<BgDecisionData>>(json, s_jsonOptions);
        Assert.NotNull(decisions);
        Assert.NotEmpty(decisions);

        // ── Pick one play decision and one cube decision ───────────────
        var play = decisions.First(d => !d.Decision.IsCube);
        var cube = decisions.First(d => d.Decision.IsCube);

        // ── Build DiagramRequests (panel left — default) ──────────────
        var playRequest = FromDecisionData(play);
        var cubeRequest = FromDecisionData(cube);

        var (playProblem, playSolution) = playRequest.ToProblemSolutionPair();
        var (cubeProblem, cubeSolution) = cubeRequest.ToProblemSolutionPair();

        var allRequests = new[] { playProblem, playSolution, cubeProblem, cubeSolution };
        var names = new[] { "ajhh_play_problem", "ajhh_play_solution",
                            "ajhh_cube_problem", "ajhh_cube_solution" };

        // ── Build DiagramRequests (panel right) ───────────────────────
        var playRequestR = FromDecisionData(play, analysisPanelPosition: PanelPosition.Right);
        var cubeRequestR = FromDecisionData(cube, analysisPanelPosition: PanelPosition.Right);

        var (playProblemR, playSolutionR) = playRequestR.ToProblemSolutionPair();
        var (cubeProblemR, cubeSolutionR) = cubeRequestR.ToProblemSolutionPair();

        var rightRequests = new[] { playProblemR, playSolutionR, cubeProblemR, cubeSolutionR };
        var rightNames = new[] { "ajhh_play_problem_right", "ajhh_play_solution_right",
                                 "ajhh_cube_problem_right", "ajhh_cube_solution_right" };

        var options = TestFixtures.DefaultOptions();

        // ── SVG output ─────────────────────────────────────────────────
        for (int i = 0; i < allRequests.Length; i++)
        {
            var svg = DiagramRenderer.RenderSvg(allRequests[i], options);
            Assert.Contains("<svg", svg);
            Assert.Contains("<circle", svg);
            var path = TestPaths.SvgOutputPath($"{names[i]}.svg");
            File.WriteAllText(path, svg);
            Assert.True(File.Exists(path));
        }
        for (int i = 0; i < rightRequests.Length; i++)
        {
            var svg = DiagramRenderer.RenderSvg(rightRequests[i], options);
            Assert.Contains("<svg", svg);
            var path = TestPaths.SvgOutputPath($"{rightNames[i]}.svg");
            File.WriteAllText(path, svg);
            Assert.True(File.Exists(path));
        }

        // ── PNG output ─────────────────────────────────────────────────
        var renderer = new DiagramRenderer();
        for (int i = 0; i < allRequests.Length; i++)
        {
            var png = renderer.RenderPng(allRequests[i], options);
            Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
            var path = TestPaths.PngOutputPath($"{names[i]}.png");
            File.WriteAllBytes(path, png);
            Assert.True(File.Exists(path));
        }
        for (int i = 0; i < rightRequests.Length; i++)
        {
            var png = renderer.RenderPng(rightRequests[i], options);
            Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
            var path = TestPaths.PngOutputPath($"{rightNames[i]}.png");
            File.WriteAllBytes(path, png);
            Assert.True(File.Exists(path));
        }

        // ── PPTX output (all 8 slides) ────────────────────────────────
        var allCombined = allRequests.Concat(rightRequests).ToArray();
        var pptx = renderer.RenderPptx(allCombined, options);
        var pptxPath = TestPaths.PptxOutputPath("ajhh_all.pptx");
        File.WriteAllBytes(pptxPath, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");

        // ── PDF output (all 8 pages) ───────────────────────────────────
        var pdf = renderer.RenderPdf(allCombined, options);
        var pdfPath = TestPaths.PdfOutputPath("ajhh_all.pdf");
        File.WriteAllBytes(pdfPath, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
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