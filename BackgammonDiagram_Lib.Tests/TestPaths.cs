namespace BackgammonDiagram_Lib.Tests;

internal static class TestPaths
{
    private static readonly string _root =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\TestData"));

    public static string BothWaysDir => Path.Combine(_root, "BothWays");
    public static string ThisWayXg => Path.Combine(BothWaysDir, "ThisWay.xg");
    public static string ThatWayXg => Path.Combine(BothWaysDir, "ThatWay.xg");

    public static string SvgDir => Path.Combine(_root, "svg");

    /// <summary>
    /// Returns a path for SVG output in TestData\svg\{filename}.
    /// Creates the svg directory if it doesn't exist.
    /// </summary>
    public static string SvgOutputPath(string filename)
    {
        Directory.CreateDirectory(SvgDir);
        return Path.Combine(SvgDir, filename);
    }
    public static string PptxDir => Path.Combine(_root, "pptx");
    public static string PptxOutputPath(string filename)
    {
        Directory.CreateDirectory(PptxDir);
        return Path.Combine(PptxDir, filename);
    }
    public static string PdfDir => Path.Combine(_root, "pdf");
    public static string PdfOutputPath(string filename)
    {
        Directory.CreateDirectory(PdfDir);
        return Path.Combine(PdfDir, filename);
    }
    public static string PngDir => Path.Combine(_root, "png");
    public static string PngOutputPath(string filename)
    {
        Directory.CreateDirectory(PngDir);
        return Path.Combine(PngDir, filename);
    }
    public static string DiagramRequestDir => Path.Combine(_root, "DiagramRequest");

    /// <summary>
    /// Returns a path for a JSON input file in TestData\DiagramRequest\{filename}.
    /// </summary>
    public static string DiagramRequestJson(string filename)
        => Path.Combine(DiagramRequestDir, filename);

}
