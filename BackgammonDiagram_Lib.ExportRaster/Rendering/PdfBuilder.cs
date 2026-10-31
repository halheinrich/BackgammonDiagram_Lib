using System.Runtime.ExceptionServices;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Elements;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BackgammonDiagram_Lib.ExportRaster;

/// <summary>
/// Builds a PDF byte array from one or more PNG images.
/// Each PNG becomes one page, rendered full-bleed.
/// Title is baked into the PNG by the SVG renderer; the XGID (a decision's)
/// is overlaid per page as real, selectable text in the upper-right corner.
/// Internal — called only by DiagramRasterRenderer.
/// </summary>
internal static class PdfBuilder
{
    // Page dimensions: widescreen landscape (13.33" × 7.5"), fixed whatever the
    // canvas preset. QuestPDF uses points (1 inch = 72 points)
    private const float PageWidthPt = 13.33f * 72;   // ~960pt
    private const float PageHeightPt = 7.5f * 72;    // 540pt

    private const float MarginPt = 36;               // 0.5"

    // XGID overlay: normal visible text, inset from the top-right page edges.
    private const float XgidFontSizePt = 9;
    private const float XgidInsetPt = 8;

    public static byte[] Build(IEnumerable<(byte[] Png, string? Xgid)> pages,
        CancellationToken cancellationToken = default)
    {
        var pageList = pages.ToList();

        var document = Document.Create(container => ComposePages(container, pageList, cancellationToken));
        return Generate(document, cancellationToken);
    }

    /// <summary>
    /// Runs QuestPDF's generation of <paramref name="document"/>. Each page's
    /// <see cref="CancellationCheck"/> stops it as that page is laid out or
    /// drawn; QuestPDF wraps the throw in a <see cref="DocumentDrawingException"/>,
    /// which is unwrapped here only when it carries this token's cancellation.
    /// Any other failure stays wrapped, as QuestPDF reports it.
    /// </summary>
    internal static byte[] Generate(IDocument document, CancellationToken cancellationToken)
    {
        try
        {
            var pdf = document.GeneratePdf();

            // QuestPDF writes the file after the last page's last check; a
            // cancellation during that still ends the call cancelled.
            cancellationToken.ThrowIfCancellationRequested();
            return pdf;
        }
        catch (DocumentDrawingException ex) when (ex.InnerException is OperationCanceledException canceled
            && cancellationToken.IsCancellationRequested && canceled.CancellationToken == cancellationToken)
        {
            ExceptionDispatchInfo.Throw(canceled);
            throw;
        }
    }

    /// <summary>
    /// Declares one page per entry of <paramref name="pages"/> on
    /// <paramref name="container"/>, checking the token before each. Runs
    /// inside QuestPDF's generation, which lets the
    /// <see cref="OperationCanceledException"/> through as itself. When the
    /// token can be cancelled, each page also carries a
    /// <see cref="CancellationCheck"/> on its background layer.
    /// </summary>
    internal static void ComposePages(IDocumentContainer container,
        IReadOnlyList<(byte[] Png, string? Xgid)> pages, CancellationToken cancellationToken)
    {
        foreach (var (png, xgid) in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            container.Page(page =>
            {
                page.Size(PageWidthPt, PageHeightPt, Unit.Point);
                page.Margin(MarginPt, Unit.Point);
                page.PageColor(Colors.White);

                if (cancellationToken.CanBeCanceled)
                    page.Background().Dynamic(new CancellationCheck(cancellationToken));

                page.Content()
                    .AlignCenter()
                    .Image(png)
                    .FitArea();

                // Real, selectable XGID text in the upper-right. Drawn on
                // the foreground layer so it sits over the image; skipped
                // when the request carries no XGID.
                if (xgid is not null)
                {
                    page.Foreground()
                        .AlignTop()
                        .AlignRight()
                        .PaddingTop(XgidInsetPt)
                        .PaddingRight(XgidInsetPt)
                        .Text(xgid)
                        .FontSize(XgidFontSizePt)
                        .FontColor(Colors.Black);
                }
            });
        }
    }

    /// <summary>
    /// A per-page element that draws nothing and checks the token each time
    /// QuestPDF lays out or draws its page, which it does several times per
    /// page across the whole generation. It holds only the token, so being
    /// called again for the same page is harmless.
    /// </summary>
    private sealed class CancellationCheck(CancellationToken cancellationToken) : IDynamicComponent
    {
        public DynamicComponentComposeResult Compose(DynamicContext context)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new DynamicComponentComposeResult
            {
                Content = context.CreateElement(_ => { }),
                HasMoreContent = false,
            };
        }
    }
}
