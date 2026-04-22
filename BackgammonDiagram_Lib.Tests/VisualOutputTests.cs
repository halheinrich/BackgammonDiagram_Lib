using BackgammonDiagram_Lib.Rendering;
using BgDataTypes_Lib;
using QuestPDF.Infrastructure;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

[Trait("Category", "Visual")]
public class VisualOutputTests
{
    static VisualOutputTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // -----------------------------------------------------------------------
    //  SVG
    // -----------------------------------------------------------------------

    [Fact]
    public void Svg_Widescreen16x9()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        b.Mode = DiagramMode.Solution;
        var options = new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 };
        var path = TestPaths.SvgOutputPath("bg_widescreen_16x9.svg");
        File.WriteAllText(path, DiagramRenderer.RenderSvg(b.Build(), options));
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
    public void Svg_SolutionMode()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        var path = TestPaths.SvgOutputPath("bg_solution.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_PanelRight()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.AnalysisPanelPosition = PanelPosition.Right;
        var path = TestPaths.SvgOutputPath("bg_panel_right.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
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
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = false;
        var path = TestPaths.SvgOutputPath("bg_homeboardleft.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
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
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        var svg = TestFixtures.Render(b.Build());
        Assert.Contains("<circle", svg);
        var path = TestPaths.SvgOutputPath("checkers_starting.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_BarAndOverflow()
    {
        var mop = new int[26];
        mop[25] = 3; mop[0] = -2; mop[6] = 8; mop[19] = -7;
        var b = TestFixtures.MinimalBuilder();
        b.Mop = mop;
        var svg = TestFixtures.Render(b.Build());
        Assert.Contains(">8<", svg);
        Assert.Contains(">7<", svg);
        var path = TestPaths.SvgOutputPath("checkers_bar_overflow.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Dice31()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        b.Dice = [3, 1];
        var path = TestPaths.SvgOutputPath("dice_31.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Dice66()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        b.Dice = [6, 6];
        var path = TestPaths.SvgOutputPath("dice_66.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_Dice11_HomeBoardRight()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = true;
        b.Dice = [1, 1];
        var path = TestPaths.SvgOutputPath("dice_11_homeboardright.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_IsCube()
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        var path = TestPaths.SvgOutputPath("dice_iscube.svg");
        File.WriteAllText(path, TestFixtures.Render(b.Build()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_CubeOwnerDefault_Centered()
    {
        var b = new DiagramRequest.Builder { HomeBoardOnRight = true, Dice = [1, 1] };
        var path = TestPaths.SvgOutputPath("cube_default_centered.svg");
        File.WriteAllText(path, DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions()));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Svg_CubeFace_Crawford_RendersCr()
    {
        // Crawford game (on-roll is 1-away). Cube face replaces the numeric
        // cube size with "Cr" — rail text still carries the long "Crawford"
        // word, so the cube-face check must be anchored to the short form.
        var b = TestFixtures.MinimalBuilder();
        b.MatchLength = 7;
        b.OnRollNeeds = 1;
        b.OpponentNeeds = 5;
        b.IsCrawford = true;
        var svg = TestFixtures.Render(b.Build());
        var path = TestPaths.SvgOutputPath("cube_face_crawford.svg");
        File.WriteAllText(path, svg);
        Assert.Contains(">Cr</text>", svg);
    }

    [Fact]
    public void Svg_CubeFace_OneAwayOneAway_RendersDmp()
    {
        // 1a-1a: cube is dead (double match point), face reads "Dmp". The
        // cube-size value is suppressed regardless of what's set — CubeSize
        // is left at MinimalBuilder's default (2) to confirm the size value
        // doesn't leak through.
        var b = TestFixtures.MinimalBuilder();
        b.MatchLength = 7;
        b.OnRollNeeds = 1;
        b.OpponentNeeds = 1;
        var svg = TestFixtures.Render(b.Build());
        var path = TestPaths.SvgOutputPath("cube_face_1a_1a.svg");
        File.WriteAllText(path, svg);
        Assert.Contains(">Dmp</text>", svg);
        Assert.DoesNotContain(">Cr</text>", svg);
    }

    // -----------------------------------------------------------------------
    //  PNG
    // -----------------------------------------------------------------------

    [Fact]
    public void Png_Default()
    {
        var png = DiagramRenderer.RenderPng(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        var path = TestPaths.PngOutputPath("bg_default.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_Greyscale()
    {
        var png = DiagramRenderer.RenderPng(TestFixtures.MinimalRequest(), TestFixtures.GreyscaleOptions());
        var path = TestPaths.PngOutputPath("bg_greyscale.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_HomeBoardLeft()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = false;
        var png = DiagramRenderer.RenderPng(b.Build(), TestFixtures.DefaultOptions());
        var path = TestPaths.PngOutputPath("bg_homeboardleft.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_StartingPosition()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        var png = DiagramRenderer.RenderPng(b.Build(), TestFixtures.DefaultOptions());
        var path = TestPaths.PngOutputPath("checkers_starting.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_Dice31()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        b.Dice = [3, 1];
        var png = DiagramRenderer.RenderPng(b.Build(), TestFixtures.DefaultOptions());
        var path = TestPaths.PngOutputPath("dice_31.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  PowerPoint
    // -----------------------------------------------------------------------

    [Fact]
    public void Pptx_SingleSlide()
    {
        var pptx = DiagramRenderer.RenderPptx(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_single.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_MultiSlide()
    {
        var b1 = TestFixtures.MinimalBuilder(); b1.Title = "Opening";
        var b2 = TestFixtures.MinimalBuilder(); b2.Mode = DiagramMode.Solution; b2.Title = "Opening \u2014 Solution";

        var pptx = DiagramRenderer.RenderPptx([b1.Build(), b2.Build()], TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_multi.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_ProblemSolutionPair()
    {
        var b = TestFixtures.MinimalBuilder(); b.Title = "Position 1";
        var (problem, solution) = b.Build().ToProblemSolutionPair();
        var pptx = DiagramRenderer.RenderPptx([problem, solution], TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_pair.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_CubeProblemSolutionPair()
    {
        // Exercises the cube-panel content — Problem title ("… — Cube Action?"),
        // Solution Best/Actual banner, equity/loss table, pct tables, and
        // the numeric-block width under the default 16:9 aspect.
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        b.Title = "Position 1";
        b.Mop = TestFixtures.StartingMop();
        b.NoDoubleEquity = 0.40;
        b.DoubleTakeEquity = 0.60;
        b.WinPctAfterNoDouble = 0.702;
        b.GammonPctAfterNoDouble = 0.123;
        b.BgPctAfterNoDouble = 0.011;
        b.LosePctAfterNoDouble = 0.298;
        b.LoseGammonPctAfterNoDouble = 0.091;
        b.LoseBgPctAfterNoDouble = 0.004;
        b.WinPctAfterDoubleTake = 0.715;
        b.GammonPctAfterDoubleTake = 0.131;
        b.BgPctAfterDoubleTake = 0.014;
        b.LosePctAfterDoubleTake = 0.285;
        b.LoseGammonPctAfterDoubleTake = 0.082;
        b.LoseBgPctAfterDoubleTake = 0.003;
        b.UserDoubleError = 0.1;  // user didn't double → Actual shows only "No Double"
        b.UserTakeError = 0;
        b.ProbOfOpponentErrorJustifyingDouble = 0.42;

        var (problem, solution) = b.Build().ToProblemSolutionPair();
        var pptx = DiagramRenderer.RenderPptx([problem, solution], TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_cube_pair.pptx");
        File.WriteAllBytes(path, pptx);
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
        const int onRollOff = 14;
        const int opponentOff = 14;

        var requests = new List<DiagramRequest>(capacity: cells.Length * 2);
        foreach (var cell in cells)
        {
            var b = TestFixtures.MinimalBuilder();
            b.CubeOwner = cell.cube;
            b.AnalysisPanelPosition = cell.panel;
            b.IsCube = true;
            b.Dice = [0, 0];
            b.Mop = BearOffMop(onRollOff, opponentOff);
            b.NoDoubleEquity = 0.40;
            b.DoubleTakeEquity = 0.60;
            b.Title = $"Bear-off {cell.cube}/{cell.panel} {onRollOff}-{opponentOff}";

            var (problem, solution) = b.Build().ToProblemSolutionPair();
            requests.Add(problem);
            requests.Add(solution);
        }

        var pptx = DiagramRenderer.RenderPptx(requests, TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_bearoff_matrix.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 10_000, $"PPTX too small: {pptx.Length} bytes");
    }

    /// <summary>
    /// Builds a Mop with the requested bear-off counts by stacking on-board
    /// checkers across 2-3 home-board points (at most 5 per point so stacks
    /// render without the "capped" overflow label).
    /// </summary>
    private static int[] BearOffMop(int onRollOff, int opponentOff)
    {
        var mop = new int[26];
        DistributeAcrossHome(mop, points: new[] { 1, 2, 3, 4 }, onBoard: 15 - onRollOff, sign: 1);
        DistributeAcrossHome(mop, points: new[] { 24, 23, 22, 21 }, onBoard: 15 - opponentOff, sign: -1);
        return mop;
    }

    private static void DistributeAcrossHome(int[] mop, int[] points, int onBoard, int sign)
    {
        int remaining = onBoard;
        foreach (int p in points)
        {
            if (remaining <= 0) break;
            int take = Math.Min(5, remaining);
            mop[p] = sign * take;
            remaining -= take;
        }
    }

    // -----------------------------------------------------------------------
    //  PDF
    // -----------------------------------------------------------------------

    [Fact]
    public void Pdf_SinglePage()
    {
        var pdf = DiagramRenderer.RenderPdf(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_single.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pdf_MultiPage()
    {
        var b1 = TestFixtures.MinimalBuilder(); b1.Title = "Opening";
        var b2 = TestFixtures.MinimalBuilder(); b2.Mode = DiagramMode.Solution; b2.Title = "Opening \u2014 Solution";
        var pdf = DiagramRenderer.RenderPdf([b1.Build(), b2.Build()], TestFixtures.DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_multi.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pdf_ProblemSolutionPair()
    {
        var b = TestFixtures.MinimalBuilder(); b.Title = "Position 1";
        var (problem, solution) = b.Build().ToProblemSolutionPair();
        var pdf = DiagramRenderer.RenderPdf([problem, solution], TestFixtures.DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_pair.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }
}