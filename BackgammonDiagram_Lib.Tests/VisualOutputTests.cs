using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.ExportRaster;
using BgDataTypes_Lib;
using BgDataTypes_Lib.TestSupport;
using QuestPDF.Infrastructure;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Renders representative diagrams to <c>TestData</c>'s output folders for a
/// person to look at, asserting only that each rendered. A few also pin a
/// raster fact an SVG assertion cannot reach (italics and the watermark
/// survive rasterizing). Every input is synthesized; nothing is read.
/// </summary>
[Trait("Category", "Visual")]
public class VisualOutputTests
{
    static VisualOutputTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static DiagramRequest Solution() => TestFixtures.MinimalRequest() with { Mode = DiagramMode.Solution };

    private static DiagramRequest WithDice(params int[] dice) =>
        TestFixtures.RequestFor(TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(dice: dice)));

    // -----------------------------------------------------------------------
    //  SVG
    // -----------------------------------------------------------------------

    [Fact]
    public void Svg_Widescreen16x9()
    {
        var options = new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 };
        var path = TestPaths.SvgOutputPath("bg_widescreen_16x9.svg");
        File.WriteAllText(path, DiagramRenderer.RenderSvg(Solution(), options));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_ProblemMode()
    {
        var path = TestPaths.SvgOutputPath("bg_problem.svg");
        File.WriteAllText(path, TestFixtures.Render());
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_ProblemBoardOnly()
    {
        // The halheinrich/backgammon#41 board-only canvas, as amended by
        // halheinrich/backgammon#98: the board proper alone — no blank panel
        // field and no title strip. Same request as Svg_ProblemMode plus a
        // position number, which a panel-bearing preset would put in the
        // strip, for a side-by-side eyeball.
        var options = new DiagramOptions { Aspect = AspectPreset.BoardOnly };
        var path = TestPaths.SvgOutputPath("bg_problem_board_only.svg");
        File.WriteAllText(path, DiagramRenderer.RenderSvg(TestFixtures.MinimalRequest() with { PositionNumber = 1 }, options));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_SolutionMode()
    {
        var path = TestPaths.SvgOutputPath("bg_solution.svg");
        File.WriteAllText(path, TestFixtures.Render(Solution()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_PanelRight()
    {
        var path = TestPaths.SvgOutputPath("bg_panel_right.svg");
        File.WriteAllText(path, TestFixtures.Render(Solution() with { AnalysisPanelPosition = PanelPosition.Right }));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Greyscale()
    {
        var path = TestPaths.SvgOutputPath("bg_greyscale.svg");
        File.WriteAllText(path, TestFixtures.Render(opts: TestFixtures.GreyscaleOptions()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_HomeBoardLeft()
    {
        var path = TestPaths.SvgOutputPath("bg_homeboardleft.svg");
        File.WriteAllText(path, TestFixtures.Render(TestFixtures.MinimalRequest() with { HomeBoardOnRight = false }));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_HomeBoardRight()
    {
        var path = TestPaths.SvgOutputPath("bg_homeboardright.svg");
        File.WriteAllText(path, TestFixtures.Render(TestFixtures.MinimalRequest()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_StartingPosition()
    {
        var svg = TestFixtures.Render(TestFixtures.MinimalRequest());
        Assert.Contains("<circle", svg);
        var path = TestPaths.SvgOutputPath("checkers_starting.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_BarAndOverflow()
    {
        var board = TestFixtures.Board((25, 3), (0, -2), (6, 8), (19, -7));
        var svg = TestFixtures.Render(TestFixtures.RequestFor(TestFixtures.CheckerPlayOn(board)));
        Assert.Contains(">8<", svg);
        Assert.Contains(">7<", svg);
        var path = TestPaths.SvgOutputPath("checkers_bar_overflow.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Dice31()
    {
        var path = TestPaths.SvgOutputPath("dice_31.svg");
        File.WriteAllText(path, TestFixtures.Render(WithDice(3, 1)));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Dice66()
    {
        var path = TestPaths.SvgOutputPath("dice_66.svg");
        File.WriteAllText(path, TestFixtures.Render(WithDice(6, 6)));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Dice11_HomeBoardRight()
    {
        var path = TestPaths.SvgOutputPath("dice_11_homeboardright.svg");
        File.WriteAllText(path, TestFixtures.Render(WithDice(1, 1) with { HomeBoardOnRight = true }));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_And_Png_PlayPanelWithRankInversion()
    {
        // Exercises the italic-on-rank-inversion path end-to-end: SVG emission
        // carries font-style="italic" on row 1's cells, and rasterizing
        // confirms Svg.Skia honours the attribute — the same SVG with the
        // attribute stripped must rasterize differently, or italic was
        // silently dropped.
        List<PlayCandidate> plays =
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], equity: 0.50, analysisLevel: AnalysisLevel.XgRollerPlus),
            TestRecords.Candidate(play: [new(13, 10), new(8, 5)], equity: 0.48, analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296),
            TestRecords.Candidate(play: [new(24, 21), new(8, 5)], equity: 0.42, analysisLevel: AnalysisLevel.XgRollerPlus),
        ];
        var request = TestFixtures.RequestFor(TestFixtures.CheckerPlayWith(plays), DiagramMode.Solution);

        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());
        File.WriteAllText(TestPaths.SvgOutputPath("play_panel_rank_inversion.svg"), svg);
        Assert.Contains("font-style=\"italic\"", svg);

        var png = DiagramRasterRenderer.RenderPng(request, TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("play_panel_rank_inversion.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");

        var rasterizer = new SkiaSharpRasterizer();
        var italic = rasterizer.Rasterize(svg, 1000);
        var upright = rasterizer.Rasterize(svg.Replace(" font-style=\"italic\"", string.Empty), 1000);
        Assert.False(italic.AsSpan().SequenceEqual(upright),
            "PNG identical to the upright variant — Svg.Skia silently dropped font-style=\"italic\".");
    }

    [Fact]
    public void Svg_And_Png_PlayPanelWithANotScoredRow()
    {
        // The depth-first ranking's not-scored mark, for a person to look
        // at: a 3-ply evaluation rating above the two rollouts ranked ahead of
        // it is not scored, so its Eq Loss cell shows the mark.
        List<PlayCandidate> plays =
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], equity: 0.512),
            TestRecords.Candidate(play: [new(13, 10), new(8, 5)], equity: 0.487, analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296),
            TestRecords.Candidate(play: [new(24, 21), new(13, 10)], equity: 0.455, analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296),
            TestRecords.Candidate(play: [new(24, 23), new(24, 21)], equity: 0.430, analysisLevel: AnalysisLevel.Ply2),
        ];
        var request = DiagramRequest.ForDecision(TestFixtures.CheckerPlayWith(plays, userPlayIndex: 0), PlayRanking.DepthFirst)
            with { Mode = DiagramMode.Solution };

        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());
        File.WriteAllText(TestPaths.SvgOutputPath("play_panel_not_scored.svg"), svg);
        Assert.Contains($">{DiagramRenderer.PlayPanelNotScoredMark}</text>", svg);

        var png = DiagramRasterRenderer.RenderPng(request, TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("play_panel_not_scored.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Svg_And_Png_FinalPosition_ThroughTheBoardPath()
    {
        // A game's final position — the player on roll borne off — with the
        // display facts a replay states.
        var board = TestFixtures.Board((19, -3), (20, -3), (21, -3), (22, -2), (23, -1), (24, -1));
        var request = DiagramRequest.ForBoard(board, new DisplayFacts
        {
            OnRollName = "Engine One",
            OpponentName = "Engine Two",
            Title = "Game 5, final position",
            CubeValue = 2,
            CubeOwner = CubeOwner.Opponent,
            Score = new MatchRailScore(onRollNeeds: 0, opponentNeeds: 3, isCrawford: false),
        });

        File.WriteAllText(TestPaths.SvgOutputPath("board_final_position.svg"), TestFixtures.Render(request));
        var png = DiagramRasterRenderer.RenderPng(request, TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("board_final_position.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Svg_WorkingBoard_MidEntry()
    {
        // The opening 3-1 after 8/5, its dice swapped on screen.
        var board = TestFixtures.Board((1, -2), (12, -5), (17, -3), (19, -5), (24, 2), (13, 5), (8, 2), (6, 5), (5, 1));
        var request = TestFixtures.MinimalRequest().WithWorkingBoard(board, DiceOrder.Reversed);

        var path = TestPaths.SvgOutputPath("board_working_mid_entry.svg");
        File.WriteAllText(path, TestFixtures.Render(request));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Pdf_Watermarked()
    {
        // Default DiagramOptions carries the built-in watermark, so this test
        // just emits the artefact for eyeballing -- the PDF path rasterizes to
        // PNG first.
        var pdf = DiagramRasterRenderer.RenderPdf(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PdfOutputPath("watermarked.pdf"), pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pptx_Watermarked()
    {
        // Same rationale as Pdf_Watermarked -- PPTX embeds the rendered PNG.
        var pptx = DiagramRasterRenderer.RenderPptx(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PptxOutputPath("watermarked.pptx"), pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Svg_And_Png_WatermarkRendersAndDiffersFromExplicitOptOut()
    {
        // Exercises the watermark end-to-end: SVG carries two <image>
        // elements with data-URI base64 PNG; PNG rasterization confirms
        // Svg.Skia honours <image> rather than silently dropping the
        // element. An identical PNG to the opt-out render would mean the
        // image was dropped.
        var request = TestFixtures.MinimalRequest();

        var svg = DiagramRenderer.RenderSvg(request, TestFixtures.DefaultOptions());
        File.WriteAllText(TestPaths.SvgOutputPath("watermarked.svg"), svg);

        Assert.Equal(2, TestFixtures.CountOccurrences(svg, "<image "));
        Assert.Contains("data:image/png;base64,", svg);
        Assert.Contains("rotate(90 ", svg);
        Assert.Contains("rotate(-90 ", svg);

        var pngWm = DiagramRasterRenderer.RenderPng(request, TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("watermarked.png"), pngWm);
        Assert.True(pngWm.Length > 1000, $"PNG too small: {pngWm.Length} bytes");

        var optsNone = TestFixtures.DefaultOptions() with { WatermarkImage = null };
        Assert.DoesNotContain("<image ", DiagramRenderer.RenderSvg(request, optsNone));
        var pngNone = DiagramRasterRenderer.RenderPng(request, optsNone);
        Assert.NotEqual(pngWm.Length, pngNone.Length);
        Assert.False(pngWm.AsSpan().SequenceEqual(pngNone),
            "PNG identical to opt-out variant — Svg.Skia silently dropped <image>.");
    }

    [Fact]
    public void Svg_IsCube()
    {
        var path = TestPaths.SvgOutputPath("dice_iscube.svg");
        File.WriteAllText(path, TestFixtures.Render(TestFixtures.RequestFor(TestRecords.Cube())));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_CubeOwnerDefault_Centered()
    {
        var path = TestPaths.SvgOutputPath("cube_default_centered.svg");
        File.WriteAllText(path, DiagramRenderer.RenderSvg(
            DiagramRequest.ForBoard(BoardPosition.Standard, new DisplayFacts()), TestFixtures.DefaultOptions()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_CubeFace_Crawford_RendersCr()
    {
        // Crawford game (the player on roll is 1-away): the face reads "Cr".
        var svg = TestFixtures.Render(TestFixtures.RequestFor(TestFixtures.CheckerPlayOn(BoardPosition.Standard,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 5, isCrawford: true))));
        File.WriteAllText(TestPaths.SvgOutputPath("cube_face_crawford.svg"), svg);
        Assert.Equal(DiagramPresentation.CrawfordFace, TestFixtures.CubeFace(svg));
    }

    [Fact]
    public void Svg_CubeFace_OneAwayOneAway_RendersDmp()
    {
        // 1a-1a: the cube is dead (double match point), face reads "Dmp",
        // whatever its value.
        var svg = TestFixtures.Render(TestFixtures.RequestFor(TestFixtures.CheckerPlayOn(BoardPosition.Standard, cubeSize: 2,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 1, opponentNeeds: 1))));
        File.WriteAllText(TestPaths.SvgOutputPath("cube_face_1a_1a.svg"), svg);
        Assert.Equal(DiagramPresentation.DoubleMatchPointFace, TestFixtures.CubeFace(svg));
    }

    // -----------------------------------------------------------------------
    //  PNG
    // -----------------------------------------------------------------------

    [Fact]
    public void Png_Default()
    {
        var png = DiagramRasterRenderer.RenderPng(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("bg_default.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_Greyscale()
    {
        var png = DiagramRasterRenderer.RenderPng(TestFixtures.MinimalRequest(), TestFixtures.GreyscaleOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("bg_greyscale.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_HomeBoardLeft()
    {
        var png = DiagramRasterRenderer.RenderPng(TestFixtures.MinimalRequest() with { HomeBoardOnRight = false }, TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("bg_homeboardleft.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_StartingPosition()
    {
        var png = DiagramRasterRenderer.RenderPng(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("checkers_starting.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_Dice31()
    {
        var png = DiagramRasterRenderer.RenderPng(WithDice(3, 1), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PngOutputPath("dice_31.png"), png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  PowerPoint
    // -----------------------------------------------------------------------

    [Fact]
    public void Pptx_SingleSlide()
    {
        var pptx = DiagramRasterRenderer.RenderPptx(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PptxOutputPath("bg_single.pptx"), pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_MultiSlide()
    {
        var pptx = DiagramRasterRenderer.RenderPptx([TestFixtures.MinimalRequest(), Solution()], TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PptxOutputPath("bg_multi.pptx"), pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_ProblemSolutionPair()
    {
        var (problem, solution) = (TestFixtures.MinimalRequest() with { PositionNumber = 1 }).ToProblemSolutionPair();
        var pptx = DiagramRasterRenderer.RenderPptx([problem, solution], TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PptxOutputPath("bg_pair.pptx"), pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_CubeProblemSolutionPair()
    {
        // Exercises the cube-panel content — the Problem's "Cube Action?"
        // title, the Solution's Best/Actual banner, equity/loss table, pct
        // tables, and the numeric-block width under the default 16:9 aspect.
        // An undoubled game, so Actual shows only "No double".
        var record = TestRecords.Cube(decision: TestRecords.CubeData(
            noDoubleEquity: 0.40, doubleTakeEquity: 0.60,
            winPctAfterNoDouble: 0.702, gammonPctAfterNoDouble: 0.123, bgPctAfterNoDouble: 0.011,
            loseGammonPctAfterNoDouble: 0.091, loseBgPctAfterNoDouble: 0.004,
            winPctAfterDoubleTake: 0.715, gammonPctAfterDoubleTake: 0.131, bgPctAfterDoubleTake: 0.014,
            loseGammonPctAfterDoubleTake: 0.082, loseBgPctAfterDoubleTake: 0.003,
            userDoublerAction: CubeAction.NoDouble, userTakerAction: null));

        var (problem, solution) = (TestFixtures.RequestFor(record) with { PositionNumber = 1 }).ToProblemSolutionPair();
        var pptx = DiagramRasterRenderer.RenderPptx([problem, solution], TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PptxOutputPath("bg_cube_pair.pptx"), pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_BearOffMatrix()
    {
        // 6 (Problem, Solution) pairs × 2 slides = 12 slides. Axes:
        //   3 cube positions × 2 analysis-panel locations.
        // Every cell carries the maximum renderable count (14 off each side)
        // so the full-tray visual is exercised under all three cube placements.
        var cells = new[]
        {
            (cube: CubeOwner.Centered, panel: PanelPosition.Left),
            (cube: CubeOwner.Centered, panel: PanelPosition.Right),
            (cube: CubeOwner.OnRoll,   panel: PanelPosition.Left),
            (cube: CubeOwner.OnRoll,   panel: PanelPosition.Right),
            (cube: CubeOwner.Opponent, panel: PanelPosition.Left),
            (cube: CubeOwner.Opponent, panel: PanelPosition.Right),
        };

        // One checker left on each side's 1-point (their own home): 14 off each.
        var board = TestFixtures.Board((1, 1), (24, -1));
        var requests = new List<DiagramRequest>(capacity: cells.Length * 2);
        foreach (var cell in cells)
        {
            var record = TestRecords.Cube(position: TestRecords.Position(
                mop: board, cubeSize: cell.cube == CubeOwner.Centered ? 1 : 2, cubeOwner: cell.cube));
            var (problem, solution) = (TestFixtures.RequestFor(record) with { AnalysisPanelPosition = cell.panel })
                .ToProblemSolutionPair();
            requests.Add(problem);
            requests.Add(solution);
        }

        var pptx = DiagramRasterRenderer.RenderPptx(requests, TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PptxOutputPath("bg_bearoff_matrix.pptx"), pptx);
        Assert.True(pptx.Length > 10_000, $"PPTX too small: {pptx.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  PDF
    // -----------------------------------------------------------------------

    [Fact]
    public void Pdf_SinglePage()
    {
        var pdf = DiagramRasterRenderer.RenderPdf(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PdfOutputPath("bg_single.pdf"), pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pdf_MultiPage()
    {
        var pdf = DiagramRasterRenderer.RenderPdf([TestFixtures.MinimalRequest(), Solution()], TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PdfOutputPath("bg_multi.pdf"), pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pdf_ProblemSolutionPair()
    {
        var (problem, solution) = (TestFixtures.MinimalRequest() with { PositionNumber = 1 }).ToProblemSolutionPair();
        var pdf = DiagramRasterRenderer.RenderPdf([problem, solution], TestFixtures.DefaultOptions());
        File.WriteAllBytes(TestPaths.PdfOutputPath("bg_pair.pdf"), pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }
}
