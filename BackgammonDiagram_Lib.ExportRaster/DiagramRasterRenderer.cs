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
    // construction is cheap (native Skia libs load lazily on first rasterize),
    // and the instance holds no state to share. Whether native Skia tolerates
    // concurrent renders is not established (halheinrich/backgammon#426).
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
    /// landscape whatever the canvas preset, the PPTX slide's size at the
    /// default 16:9), embedding the rasterized PNG and overlaying the request's
    /// XGID as real selectable text.
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
        return PdfBuilder.Build([RenderOverlayPage(request, options, rasterizer)], CancellationToken.None);
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
    /// <param name="cancellationToken">Stops the render. Honoured before each
    /// page is rasterized and after the last, between pages as the document is
    /// composed, each time QuestPDF lays out or draws a page during its
    /// generation, and after the generation returns.</param>
    /// <returns>The PDF file bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requests"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/>
    /// was cancelled; no document is returned.</exception>
    /// <remarks>
    /// <para>The caller owns the QuestPDF license: set
    /// <c>QuestPDF.Settings.License</c> before calling.</para>
    /// <para>What runs to its end once entered: one page's rasterization by
    /// <paramref name="rasterizer"/>; inside QuestPDF's <c>GeneratePdf()</c>,
    /// the work between two of a page's checks; and QuestPDF's cleanup as a
    /// cancelled generation unwinds. A cancellation during one takes effect
    /// when it returns.</para>
    /// </remarks>
    public static byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var pages = RenderOverlayPages(requests, options, rasterizer, cancellationToken);
        return PdfBuilder.Build(pages, cancellationToken);
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
        return PptxBuilder.Build([RenderOverlayPage(request, options, rasterizer)], CancellationToken.None);
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
    /// <param name="cancellationToken">Stops the render. Honoured before each
    /// page is rasterized and after the last, before each slide is built,
    /// before each entry of each package fix-up pass, and after each package
    /// write returns.</param>
    /// <returns>The PPTX file bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requests"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/>
    /// was cancelled; no document is returned.</exception>
    /// <remarks>Three kinds of call run to their end once entered: one page's
    /// rasterization by <paramref name="rasterizer"/>, the OpenXml package's
    /// save as the presentation closes, and each fix-up pass's ZIP archive
    /// rewrite as it closes. A cancellation during one takes effect when it
    /// returns.</remarks>
    public static byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var pages = RenderOverlayPages(requests, options, rasterizer, cancellationToken);
        return PptxBuilder.Build(pages, cancellationToken);
    }

    /// <summary>
    /// Renders each of <paramref name="requests"/>, in order, through
    /// <see cref="RenderOverlayPage"/>. The token is checked before each page
    /// and after the last, so a cancellation during one page's rasterization
    /// stops the call as that page returns, before the next page or the build.
    /// </summary>
    private static List<(byte[] Png, string? Xgid)> RenderOverlayPages(
        IEnumerable<DiagramRequest> requests, DiagramOptions options, ISvgRasterizer? rasterizer,
        CancellationToken cancellationToken)
    {
        var pages = new List<(byte[] Png, string? Xgid)>();
        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(RenderOverlayPage(request, options, rasterizer));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return pages;
    }

    /// <summary>
    /// Renders one request's PNG with the baked XGID label forced off, pairing
    /// it with the request's XGID for the builder to overlay as real selectable
    /// text. Forcing <see cref="DiagramOptions.ShowXgid"/> off here (regardless
    /// of what the caller passed) guarantees the baked pixels can't duplicate
    /// the text overlay — the PDF/PPTX label is always the real-text one.
    /// </summary>
    private static (byte[] Png, string? Xgid) RenderOverlayPage(DiagramRequest request,
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
