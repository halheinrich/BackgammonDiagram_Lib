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

    /// <summary>
    /// Renders <paramref name="request"/> to a PNG. The core SVG is produced by
    /// <see cref="DiagramRenderer.RenderSvg"/> and rasterized at the pixel width
    /// resolved from <paramref name="options"/>'s <see cref="DiagramSize"/>.
    /// </summary>
    /// <param name="request">The diagram to render.</param>
    /// <param name="options">Size, theme, watermark, aspect, and XGID options.</param>
    /// <param name="rasterizer">SVG→PNG backend; when <c>null</c>, a shared
    /// built-in <see cref="SkiaSharpRasterizer"/> is used.</param>
    /// <returns>The rendered PNG bytes.</returns>
    public static byte[] RenderPng(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var svg = DiagramRenderer.RenderSvg(request, options);
        int targetWidth = ResolveTargetWidth(options.Size);
        return (rasterizer ?? s_defaultRasterizer).Rasterize(svg, targetWidth);
    }

    /// <summary>
    /// Renders <paramref name="request"/> to a single-page PDF (widescreen
    /// landscape, matching the PPTX slide), embedding the rasterized PNG and
    /// overlaying the request's XGID as real selectable text.
    /// </summary>
    /// <param name="request">The diagram to render.</param>
    /// <param name="options">Render options; the baked XGID label is forced off
    /// so it cannot duplicate the text overlay.</param>
    /// <param name="rasterizer">SVG→PNG backend; when <c>null</c>, a shared
    /// built-in <see cref="SkiaSharpRasterizer"/> is used.</param>
    /// <returns>The PDF file bytes.</returns>
    /// <remarks>The caller owns the QuestPDF license: set
    /// <c>QuestPDF.Settings.License</c> before calling.</remarks>
    public static byte[] RenderPdf(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        return PdfBuilder.Build([RenderOverlayPage(request, options, rasterizer)]);
    }

    /// <summary>
    /// Renders <paramref name="requests"/> to a multi-page PDF — one page per
    /// request, in order — otherwise identical to the single-request overload.
    /// </summary>
    /// <param name="requests">The diagrams to render, one page each.</param>
    /// <param name="options">Render options; the baked XGID label is forced off
    /// so it cannot duplicate the text overlay.</param>
    /// <param name="rasterizer">SVG→PNG backend; when <c>null</c>, a shared
    /// built-in <see cref="SkiaSharpRasterizer"/> is used.</param>
    /// <returns>The PDF file bytes.</returns>
    /// <remarks>The caller owns the QuestPDF license: set
    /// <c>QuestPDF.Settings.License</c> before calling.</remarks>
    public static byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pages = requests.Select(r => RenderOverlayPage(r, options, rasterizer)).ToList();
        return PdfBuilder.Build(pages);
    }

    /// <summary>
    /// Renders <paramref name="request"/> to a single-slide PPTX, embedding the
    /// rasterized PNG and overlaying the request's XGID as real selectable text.
    /// </summary>
    /// <param name="request">The diagram to render.</param>
    /// <param name="options">Render options; the baked XGID label is forced off
    /// so it cannot duplicate the text overlay.</param>
    /// <param name="rasterizer">SVG→PNG backend; when <c>null</c>, a shared
    /// built-in <see cref="SkiaSharpRasterizer"/> is used.</param>
    /// <returns>The PPTX file bytes.</returns>
    public static byte[] RenderPptx(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        return PptxBuilder.Build([RenderOverlayPage(request, options, rasterizer)]);
    }

    /// <summary>
    /// Renders <paramref name="requests"/> to a multi-slide PPTX — one slide per
    /// request, in order — otherwise identical to the single-request overload.
    /// </summary>
    /// <param name="requests">The diagrams to render, one slide each.</param>
    /// <param name="options">Render options; the baked XGID label is forced off
    /// so it cannot duplicate the text overlay.</param>
    /// <param name="rasterizer">SVG→PNG backend; when <c>null</c>, a shared
    /// built-in <see cref="SkiaSharpRasterizer"/> is used.</param>
    /// <returns>The PPTX file bytes.</returns>
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
