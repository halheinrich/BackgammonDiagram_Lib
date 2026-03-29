using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.Tests;
using ExCSS;
using System.Text;

namespace BackgammonDiagram_Lib.Tests;

public class DiagramRendererTests
{
    // -----------------------------------------------------------------------
    //  Shared fixtures
    // -----------------------------------------------------------------------

    private static DiagramRequest MinimalRequest() => new()
    {
        OnRollName = "Hal",
        OpponentName = "Opponent",
        OnRollPipCount = 133,
        OpponentPipCount = 131,
        CubeSize = 2,
        CubeOwner = CubeOwner.Centered,
        OnRollAtBottom = true,
        Mode = DiagramMode.Problem,
        Dice = [3, 1],
        Mop = new int[26]
    };

    private static DiagramOptions DefaultOptions() => new()
    {
        Size = DiagramSize.Medium
    };

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
        var req = MinimalRequest() with { Mode = DiagramMode.Solution };
        var svg = Render(req);
        var path = TestPaths.SvgOutputPath("bg_solution.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderSvg_WritesPanelRightToDisk()
    {
        var req = MinimalRequest() with
        {
            Mode = DiagramMode.Solution,
            AnalysisPanelPosition = PanelPosition.Right
        };
        var svg = Render(req);
        var path = TestPaths.SvgOutputPath("bg_panel_right.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }
    [Fact]
    public void RenderPng_WritesPngToDisk()
    {
        var png = new DiagramRenderer().RenderPng(MinimalRequest(), DefaultOptions());
        var path = TestPaths.SvgOutputPath("bg_default.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
        Assert.True(File.Exists(path));
    }
    [Fact]
    public void RenderSvg_WritesGreyscaleToDisk()
    {
        var opts = new DiagramOptions { ThemeName = "Greyscale" };
        var svg = Render(opts: opts);
        var path = TestPaths.SvgOutputPath("bg_greyscale.svg");
        File.WriteAllText(path, svg);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RenderPng_WritesGreyscalePngToDisk()
    {
        var opts = new DiagramOptions { ThemeName = "Greyscale" };
        var png = new DiagramRenderer().RenderPng(MinimalRequest(), new DiagramOptions { ThemeName = "Greyscale" });
        var path = TestPaths.SvgOutputPath("bg_greyscale.png");
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
        var req1 = MinimalRequest() with { Title = "Opening" };
        var req2 = MinimalRequest() with { Mode = DiagramMode.Solution, Title = "Opening — Solution" };
        var pptx = new DiagramRenderer().RenderPptx([req1, req2], DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_multi.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ToProblemSolutionPair_WritesDeckToDisk()
    {
        var req = MinimalRequest() with { Title = "Position 1" };
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
        var req1 = MinimalRequest() with { Title = "Opening" };
        var req2 = MinimalRequest() with { Mode = DiagramMode.Solution, Title = "Opening — Solution" };
        var pdf = new DiagramRenderer().RenderPdf([req1, req2], DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_multi.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void ToProblemSolutionPair_WritesPdfToDisk()
    {
        var req = MinimalRequest() with { Title = "Position 1" };
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