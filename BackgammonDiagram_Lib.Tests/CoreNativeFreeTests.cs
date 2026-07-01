using System.Reflection;
using BackgammonDiagram_Lib.Rendering;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Compile-time proof that the core <c>BackgammonDiagram_Lib</c> assembly is
/// native-free — the invariant that lets WASM / SVG-only consumers
/// (BgDiag_Razor, BgQuiz, anything Blazor WASM) depend on it without dragging
/// in SkiaSharp's native libs. All rasterization / export work lives in the
/// <c>BackgammonDiagram_Lib.ExportRaster</c> sibling. If a native package ref
/// ever leaks back into core, this test fails before the WASM consumer does.
/// (A full WASM run is a later phase's job; this is the static guard.)
/// </summary>
public class CoreNativeFreeTests
{
    // Substrings that identify a native (non-WASM-safe) dependency by assembly
    // name. Matched case-insensitively against every assembly the core
    // assembly directly references.
    private static readonly string[] ForbiddenAssemblyTokens =
    [
        "SkiaSharp",            // SkiaSharp (transitive via Svg.Skia / QuestPDF)
        "Svg.Skia",             // Svg.Skia (+ its ShimSkiaSharp/Svg.* friends carry "Skia")
        "QuestPDF",
        "DocumentFormat.OpenXml",
    ];

    [Fact]
    public void CoreAssembly_ReferencesNoNativePackage()
    {
        // typeof(DiagramRenderer) anchors us to the CORE assembly specifically,
        // independent of what this test project references.
        Assembly core = typeof(DiagramRenderer).Assembly;
        Assert.Equal("BackgammonDiagram_Lib", core.GetName().Name);

        var offenders = core.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => ForbiddenAssemblyTokens.Any(tok =>
                name.Contains(tok, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();

        Assert.True(offenders.Count == 0,
            "Core assembly must reference no native package — raster/export belongs " +
            "in BackgammonDiagram_Lib.ExportRaster. Leaked references: " +
            string.Join(", ", offenders));
    }
}
