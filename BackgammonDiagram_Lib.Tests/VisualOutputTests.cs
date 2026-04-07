using BackgammonDiagram_Lib.Rendering;
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
        File.WriteAllText(path, new DiagramRenderer().RenderSvg(b.Build(), TestFixtures.DefaultOptions()));
        Assert.True(File.Exists(path));
    }

    // -----------------------------------------------------------------------
    //  PNG
    // -----------------------------------------------------------------------

    [Fact]
    public void Png_Default()
    {
        var png = new DiagramRenderer().RenderPng(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        var path = TestPaths.PngOutputPath("bg_default.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_Greyscale()
    {
        var png = new DiagramRenderer().RenderPng(TestFixtures.MinimalRequest(), TestFixtures.GreyscaleOptions());
        var path = TestPaths.PngOutputPath("bg_greyscale.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_HomeBoardLeft()
    {
        var b = TestFixtures.MinimalBuilder();
        b.HomeBoardOnRight = false;
        var png = new DiagramRenderer().RenderPng(b.Build(), TestFixtures.DefaultOptions());
        var path = TestPaths.PngOutputPath("bg_homeboardleft.png");
        File.WriteAllBytes(path, png);
        Assert.True(png.Length > 1000, $"PNG too small: {png.Length} bytes");
    }

    [Fact]
    public void Png_StartingPosition()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Mop = TestFixtures.StartingMop();
        var png = new DiagramRenderer().RenderPng(b.Build(), TestFixtures.DefaultOptions());
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
        var png = new DiagramRenderer().RenderPng(b.Build(), TestFixtures.DefaultOptions());
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
        var pptx = new DiagramRenderer().RenderPptx(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_single.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_MultiSlide()
    {
        var b1 = TestFixtures.MinimalBuilder(); b1.Title = "Opening";
        var b2 = TestFixtures.MinimalBuilder(); b2.Mode = DiagramMode.Solution; b2.Title = "Opening \u2014 Solution";

        var pptx = new DiagramRenderer().RenderPptx([b1.Build(), b2.Build()], TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_multi.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    [Fact]
    public void Pptx_ProblemSolutionPair()
    {
        var b = TestFixtures.MinimalBuilder(); b.Title = "Position 1";
        var (problem, solution) = b.Build().ToProblemSolutionPair();
        var pptx = new DiagramRenderer().RenderPptx([problem, solution], TestFixtures.DefaultOptions());
        var path = TestPaths.PptxOutputPath("bg_pair.pptx");
        File.WriteAllBytes(path, pptx);
        Assert.True(pptx.Length > 5_000, $"PPTX too small: {pptx.Length} bytes");
    }

    // -----------------------------------------------------------------------
    //  PDF
    // -----------------------------------------------------------------------

    [Fact]
    public void Pdf_SinglePage()
    {
        var pdf = new DiagramRenderer().RenderPdf(TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_single.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pdf_MultiPage()
    {
        var b1 = TestFixtures.MinimalBuilder(); b1.Title = "Opening";
        var b2 = TestFixtures.MinimalBuilder(); b2.Mode = DiagramMode.Solution; b2.Title = "Opening \u2014 Solution";
        var pdf = new DiagramRenderer().RenderPdf([b1.Build(), b2.Build()], TestFixtures.DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_multi.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }

    [Fact]
    public void Pdf_ProblemSolutionPair()
    {
        var b = TestFixtures.MinimalBuilder(); b.Title = "Position 1";
        var (problem, solution) = b.Build().ToProblemSolutionPair();
        var pdf = new DiagramRenderer().RenderPdf([problem, solution], TestFixtures.DefaultOptions());
        var path = TestPaths.PdfOutputPath("bg_pair.pdf");
        File.WriteAllBytes(path, pdf);
        Assert.True(pdf.Length > 1_000, $"PDF too small: {pdf.Length} bytes");
    }
}