using System.Text.RegularExpressions;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The renderer's source, for the tests that pin a rule's single statement
/// by surveying where it is written: a rendered SVG can show that a rule
/// holds, not that it is stated once.
/// </summary>
internal static class RendererSource
{
    /// <summary>
    /// The renderer's source file, reached from the test binary the way
    /// <see cref="TestPaths"/> reaches TestData: bin/{config}/{tfm} is three
    /// levels below this test project, which sits beside the library's.
    /// </summary>
    public static string Read(string file = "DiagramRenderer.cs")
    {
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "BackgammonDiagram_Lib", "Rendering", file));
        Assert.True(File.Exists(path), $"Renderer source not found: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// The source of the <c>static void</c> method <paramref name="name"/>,
    /// from its declaration to its closing brace at member indentation.
    /// </summary>
    public static string MethodBody(string source, string name)
    {
        var match = Regex.Match(source, $@"static void {Regex.Escape(name)}\(.*?\n    \}}\r?\n",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"Method {name} not found in the renderer's source.");
        return match.Value;
    }
}
