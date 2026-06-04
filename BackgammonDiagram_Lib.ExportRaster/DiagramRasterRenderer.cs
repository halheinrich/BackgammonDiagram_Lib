using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.ExportRaster;

/// <summary>
/// Rasterization-backed export formats for a <see cref="DiagramRequest"/>:
/// PNG, PDF, and PPTX. The sibling of the native-free core
/// <see cref="DiagramRenderer"/> — it renders the SVG via
/// <see cref="DiagramRenderer.RenderSvg"/>, then rasterizes and packages it.
/// All native dependencies (SkiaSharp, Svg.Skia, QuestPDF,
/// DocumentFormat.OpenXml) are confined to this assembly so SVG-only / WASM
/// consumers never drag them in.
/// </summary>
public static class DiagramRasterRenderer
{
    // Built-in rasterizer used when callers don't supply one. SkiaSharpRasterizer
    // construction is cheap (native Skia libs load lazily on first rasterize);
    // a single shared readonly instance is safe — rasterization is stateless.
    private static readonly SkiaSharpRasterizer s_defaultRasterizer = new();

    public static byte[] RenderPng(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var svg = DiagramRenderer.RenderSvg(request, options);
        int targetWidth = ResolveTargetWidth(options.Size);
        return (rasterizer ?? s_defaultRasterizer).Rasterize(svg, targetWidth);
    }

    public static byte[] RenderPdf(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var png = RenderPng(request, options, rasterizer);
        return PdfBuilder.Build([png]);
    }

    public static byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pngs = requests.Select(r => RenderPng(r, options, rasterizer)).ToList();
        return PdfBuilder.Build(pngs);
    }

    public static byte[] RenderPptx(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var png = RenderPng(request, options, rasterizer);
        return PptxBuilder.Build([png]);
    }

    public static byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pngs = requests.Select(r => RenderPng(r, options, rasterizer)).ToList();
        return PptxBuilder.Build(pngs);
    }

    /// <summary>
    /// Returns true if QuestPDF is correctly licensed and operational.
    /// Call at application startup before serving PDF requests.
    /// If false, set QuestPDF.Settings.License in your app startup.
    /// Also returns false if QuestPDF's native dependencies cannot load
    /// (e.g. unsupported runtime).
    /// </summary>
    public static bool IsPdfSupported()
    {
        try
        {
            return QuestPDF.Settings.License.HasValue;
        }
        catch
        {
            return false;
        }
    }

    // -----------------------------------------------------------------------
    //  Size resolution
    // -----------------------------------------------------------------------

    private static int ResolveTargetWidth(DiagramSize size) => size.Preset switch
    {
        DiagramSizePreset.Small => 600,
        DiagramSizePreset.Large => 1600,
        DiagramSizePreset.Custom => size.CustomWidth ?? 1000,
        _ => 1000   // Medium
    };
}
