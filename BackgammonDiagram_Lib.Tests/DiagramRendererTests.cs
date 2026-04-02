using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Tests;
using QuestPDF.Infrastructure;
using System.Text;

namespace BackgammonDiagram_Lib.Tests;

public class DiagramRendererTests
{
    static DiagramRendererTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    // -----------------------------------------------------------------------
    //  Shared fixtures
    // -----------------------------------------------------------------------

    private static DiagramRequest MinimalRequest() => TestFixtures.MinimalRequest();
    private static DiagramOptions DefaultOptions() => TestFixtures.DefaultOptions();

    private static string Render(DiagramRequest? req = null, DiagramOptions? opts = null)
        => new DiagramRenderer().RenderSvg(req ?? MinimalRequest(), opts ?? DefaultOptions());

    // -----------------------------------------------------------------------
    //  Basic SVG structure
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSvg_StartsWithSvgTag()
    {
        var svg = Render();
        Assert.StartsWith("<svg", svg.TrimStart());
    }

    [Fact]
    public void RenderSvg_ContainsClosingSvgTag()
    {
        var svg = Render();
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void RenderSvg_ContainsViewBox()
    {
        var svg = Render();
        Assert.Contains("viewBox=", svg);
    }

    // -----------------------------------------------------------------------
    //  Board elements present
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSvg_Contains24Triangles()
    {
        var svg = Render();
        int count = CountOccurrences(svg, "<polygon");
        Assert.Equal(24, count);
    }

    [Fact]
    public void RenderSvg_Contains24PointNumbers()
    {
        var svg = Render();
        // Each point number is a <text> element; also rail texts exist,
        // so we check we have at least 24 text elements
        int count = CountOccurrences(svg, "<text");
        Assert.True(count >= 24, $"Expected at least 24 <text> elements, found {count}");
    }

    [Fact]
    public void RenderSvg_ContainsCubeRect()
    {
        var svg = Render();
        // Cube is a rect; board background is also a rect — just verify >1
        int count = CountOccurrences(svg, "<rect");
        Assert.True(count >= 2, $"Expected at least 2 <rect> elements, found {count}");
    }

    [Fact]
    public void RenderSvg_ContainsPlayerNames()
    {
        var svg = Render();
        Assert.Contains("Hal", svg);
        Assert.Contains("Opponent", svg);
    }

    [Fact]
    public void RenderSvg_ContainsPipCounts()
    {
        var svg = Render();
        Assert.Contains("133", svg);
        Assert.Contains("131", svg);
    }

    // -----------------------------------------------------------------------
    //  Board dimensions (via BoardLayout)
    // -----------------------------------------------------------------------

    [Fact]
    public void BoardLayout_Default_CheckerRadiusIs14()
    {
        var layout = BoardLayout.Default;
        Assert.Equal(14, layout.CheckerRadius);
    }

    [Fact]
    public void BoardLayout_PointHeightIs5Diameters()
    {
        var layout = BoardLayout.Default;
        Assert.Equal(layout.CheckerRadius * 2 * 5, layout.PointHeight);
    }

    [Fact]
    public void BoardLayout_HalfWidthIs6Columns()
    {
        var layout = BoardLayout.Default;
        Assert.Equal(layout.ColumnWidth * 6, layout.HalfWidth);
    }

    [Fact]
    public void BoardLayout_BoardHeightIsReasonable()
    {
        var layout = BoardLayout.Default;
        // Should be roughly 400–560px at default checker size
        Assert.InRange(layout.BoardHeight, 400, 560);
    }

    // -----------------------------------------------------------------------
    //  Visual output — writes SVG to temp for manual browser inspection
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSvg_WritesProblemModeToDisk()
    {
        var svg = Render();
        var path = TestPaths.SvgOutputPath("bg_problem.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
        // Open TestData\Output\bg_problem.svg in a browser to inspect visually
    }

    [Fact]
    public void RenderSvg_WritesSolutionModeToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        var req = b.Build();
        var svg = Render(req);
        var path = TestPaths.SvgOutputPath("bg_solution.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_WritesPanelRightToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mode = DiagramMode.Solution;
        b.AnalysisPanelPosition = PanelPosition.Right;
        var req = b.Build();
        var svg = Render(req);
        var path = TestPaths.SvgOutputPath("bg_panel_right.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }
    [Fact]
    public void RenderPng_WritesPngToDisk()
    {
        var png = new DiagramRenderer().RenderPng(MinimalRequest(), DefaultOptions());
        var path = TestPaths.PngOutputPath("bg_default.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
        Assert.True(File.Exists(path));
    }
    [Fact]
    public void RenderSvg_WritesGreyscaleToDisk()
    {
        var svg = Render(opts: TestFixtures.GreyscaleOptions());
        var path = TestPaths.SvgOutputPath("bg_greyscale.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPng_WritesGreyscalePngToDisk()
    {
        var png = new DiagramRenderer().RenderPng(MinimalRequest(), TestFixtures.GreyscaleOptions());
        var path = TestPaths.PngOutputPath("bg_greyscale.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
        Assert.True(File.Exists(path));
    }
    // -----------------------------------------------------------------------
    //  PowerPoint rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderPptx_WritesSingleSlideToDisk()
    {
        var pptx = new DiagramRenderer().RenderPptx(MinimalRequest(), DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_single.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPptx_WritesMultiSlideToDisk()
    {
        var b1 = TestFixtures.MinimalBuilder();
        b1.Title = "Opening";
        var req1 = b1.Build();

        var b2 = TestFixtures.MinimalBuilder();
        b2.Mode = DiagramMode.Solution;
        b2.Title = "Opening \u2014 Solution";
        var req2 = b2.Build();
        var pptx = new DiagramRenderer().RenderPptx([req1, req2], DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_multi.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ToProblemSolutionPair_WritesDeckToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Title = "Position 1";
        var req = b.Build();
        var (problem, solution) = req.ToProblemSolutionPair();
        Assert.Equal(DiagramMode.Problem, problem.Mode);
        Assert.Equal(DiagramMode.Solution, solution.Mode);
        Assert.Equal("Position 1 \u2014 Problem", problem.Title);
        Assert.Equal("Position 1 \u2014 Solution", solution.Title);

        var pptx = new DiagramRenderer().RenderPptx([problem, solution], DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_pair.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  PDF rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderPdf_WritesSinglePageToDisk()
    {
        var pdf = new DiagramRenderer().RenderPdf(MinimalRequest(), DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_single.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPdf_WritesMultiPageToDisk()
    {
        var b1 = TestFixtures.MinimalBuilder();
        b1.Title = "Opening";
        var req1 = b1.Build();

        var b2 = TestFixtures.MinimalBuilder();
        b2.Mode = DiagramMode.Solution;
        b2.Title = "Opening \u2014 Solution";
        var req2 = b2.Build();
        var pdf = new DiagramRenderer().RenderPdf([req1, req2], DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_multi.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ToProblemSolutionPair_WritesPdfToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Title = "Position 1";
        var req = b.Build();
        var (problem, solution) = req.ToProblemSolutionPair();
        Assert.Equal(DiagramMode.Problem, problem.Mode);
        Assert.Equal(DiagramMode.Solution, solution.Mode);
        Assert.Equal("Position 1 \u2014 Problem", problem.Title);
        Assert.Equal("Position 1 \u2014 Solution", solution.Title);

        var pdf = new DiagramRenderer().RenderPdf([problem, solution], DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_pair.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  HomeBoardOnRight
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSvg_WritesHomeBoardLeftToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = false;
        var req = b.Build();
        var svg = Render(req);
        var path = TestPaths.SvgOutputPath("bg_homeboardleft.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPng_WritesHomeBoardLeftToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = false;
        var req = b.Build();
        var png = new DiagramRenderer().RenderPng(req, DefaultOptions());
        var path = TestPaths.PngOutputPath("bg_homeboardleft.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_WritesHomeBoardRightToDisk()
    {
        var svg = Render(MinimalRequest());
        var path = TestPaths.SvgOutputPath("bg_homeboardright.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_HomeBoardLeftAndRight_ProduceDifferentSvg()
    {
        var svgRight = Render(MinimalRequest());
        var bl = TestFixtures.MinimalBuilder();
        bl.HomeBoardOnRight = false;
        var svgLeft = Render(bl.Build());
        Assert.NotEqual(svgRight, svgLeft);
    }

    // -----------------------------------------------------------------------
    //  Checker rendering
    // -----------------------------------------------------------------------

    /// <summary>
    /// Backgammon opening position (standard setup).
    /// 2 on pt24, 5 on pt13, 3 on pt8, 5 on pt6  (on-roll)
    /// 2 on pt1,  5 on pt12, 3 on pt17, 5 on pt19 (opponent, negated)
    /// </summary>
    private static int[] StartingMop()
    {
        var mop = new int[26];
        // On-roll checkers (positive)
        mop[24] = 2;
        mop[13] = 5;
        mop[8] = 3;
        mop[6] = 5;
        // Opponent checkers (negative, mirror image)
        mop[1] = -2;
        mop[12] = -5;
        mop[17] = -3;
        mop[19] = -5;
        return mop;
    }

    [Fact]
    public void RenderSvg_StartingPosition_WritesToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = StartingMop();
        var req = b.Build();
        var svg = new DiagramRenderer().RenderSvg(req, DefaultOptions());

        // Should contain circles (checkers)
        Assert.Contains("<circle", svg);

        var path = TestPaths.SvgOutputPath("checkers_starting.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_BarAndOverflow_WritesToDisk()
    {
        var mop = new int[26];
        mop[25] = 3;    // 3 on-roll checkers on bar
        mop[0] = -2;    // 2 opponent checkers on bar
        mop[6] = 8;    // overflow: 8 checkers on a single point (draws cap label)
        mop[19] = -7;    // overflow: 7 opponent checkers

        var b = TestFixtures.MinimalBuilder();
        b.Mop = mop;
        var req = b.Build();
        var svg = new DiagramRenderer().RenderSvg(req, DefaultOptions());

        // Overflow label: count text should appear for 8 and 7
        Assert.Contains(">8<", svg);
        Assert.Contains(">7<", svg);

        var path = TestPaths.SvgOutputPath("checkers_bar_overflow.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPng_StartingPosition_WritesToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = StartingMop();
        var req = b.Build();
        var png = new DiagramRenderer().RenderPng(req, DefaultOptions());
        var path = TestPaths.PngOutputPath("checkers_starting.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
        Assert.True(File.Exists(path));
    }

    // -----------------------------------------------------------------------
    //  Dice rendering
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderSvg_Dice31_WritesToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = StartingMop();
        b.Dice = [3, 1];
        var req = b.Build();
        var svg = new DiagramRenderer().RenderSvg(req, DefaultOptions());
        Assert.Contains("<circle", svg);
        var path = TestPaths.SvgOutputPath("dice_31.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_Dice66_WritesToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = StartingMop();
        b.Dice = [6, 6];
        var req = b.Build();
        var svg = new DiagramRenderer().RenderSvg(req, DefaultOptions());
        var path = TestPaths.SvgOutputPath("dice_66.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_IsCube_NoDiceRendered()
    {
        var b = TestFixtures.MinimalBuilder();
        b.IsCube = true;
        b.Dice = [0, 0];
        var req = b.Build();
        var svg = new DiagramRenderer().RenderSvg(req, DefaultOptions());
        // No die face rects beyond the board background rects — check pip count is 0
        // (pips are circles; checkers are also circles so we can't use that)
        // Die faces: white rect with grey stroke — absent when IsCube=true
        Assert.DoesNotContain("fill=\"#FFFFFF\" stroke=\"#888\" stroke-width=\"0.75\"", svg);
        var path = TestPaths.SvgOutputPath("dice_iscube.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPng_Dice31_WritesToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = StartingMop();
        b.Dice = [3, 1];
        var req = b.Build();
        var png = new DiagramRenderer().RenderPng(req, DefaultOptions());
        var path = TestPaths.PngOutputPath("dice_31.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_HomeBoardOnRight_Dice11_WritesToDisk()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = true;
        b.Dice = [1, 1];
        var req = b.Build();
        var svg = new DiagramRenderer().RenderSvg(req, DefaultOptions());
        Assert.Contains("<circle", svg);
        var path = TestPaths.SvgOutputPath("dice_11_homeboardright.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_CubeOwnerDefault_IsCentered()
    {
        // Do NOT set CubeOwner — rely on Builder default, matching BgQuiz_Blazor construction
        var b = new DiagramRequest.Builder { HomeBoardOnRight = true, Dice = [1, 1] };
        var svg = new DiagramRenderer().RenderSvg(b.Build(), DefaultOptions());

        var layout = BoardLayout.Default;
        double cubeSize = layout.LeftRailWidth * 0.7;
        double expectedY = layout.BoardHeight / 2 - cubeSize / 2;

        var path = TestPaths.SvgOutputPath("cube_default_centered.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));

        Assert.Contains($"y=\"{expectedY:0.##}\"", svg);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static int CountOccurrences(string source, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}