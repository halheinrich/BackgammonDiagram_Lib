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
        return PdfBuilder.Build([RenderOverlayPage(request, options, rasterizer)]);
    }

    public static byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pages = requests.Select(r => RenderOverlayPage(r, options, rasterizer)).ToList();
        return PdfBuilder.Build(pages);
    }

    public static byte[] RenderPptx(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        return PptxBuilder.Build([RenderOverlayPage(request, options, rasterizer)]);
    }

    public static byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pages = requests.Select(r => RenderOverlayPage(r, options, rasterizer)).ToList();
        return PptxBuilder.Build(pages);
    }

    /// <summary>
    /// Renders one request's PNG with the baked XGID label forced off, pairing
    /// it with the request's XGID for the builder to overlay as real selectable
    /// text. Forcing <see cref="DiagramOptions.ShowXgid"/> off here (regardless
    /// of what the caller passed) guarantees the baked pixels can't duplicate
    /// the text overlay — the PDF/PPTX label is always the real-text one.
    /// </summary>
    private static (byte[] Png, string Xgid) RenderOverlayPage(DiagramRequest request,
        DiagramOptions options, ISvgRasterizer? rasterizer)
    {
        var png = RenderPng(request, options with { ShowXgid = false }, rasterizer);
        return (png, request.Xgid);
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
