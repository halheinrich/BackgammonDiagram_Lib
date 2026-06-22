using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BackgammonDiagram_Lib.ExportRaster;

/// <summary>
/// Builds a PDF byte array from one or more PNG images.
/// Each PNG becomes one page, rendered full-bleed.
/// Title is baked into the PNG by the SVG renderer; the XGID (when non-empty)
/// is overlaid per page as real, selectable text in the upper-right corner.
/// Internal — called only by DiagramRasterRenderer.
/// </summary>
internal static class PdfBuilder
{
    // Page dimensions: widescreen landscape matching PPTX (13.33" × 7.5")
    // QuestPDF uses points (1 inch = 72 points)
    private const float PageWidthPt = 13.33f * 72;   // ~960pt
    private const float PageHeightPt = 7.5f * 72;    // 540pt

    private const float MarginPt = 36;               // 0.5"

    // XGID overlay: normal visible text, inset from the top-right page edges.
    private const float XgidFontSizePt = 9;
    private const float XgidInsetPt = 8;

    public static byte[] Build(IEnumerable<(byte[] Png, string Xgid)> pages)
    {
        var pageList = pages.ToList();

        var document = Document.Create(container =>
        {
            foreach (var (png, xgid) in pageList)
            {
                container.Page(page =>
                {
                    page.Size(PageWidthPt, PageHeightPt, Unit.Point);
                    page.Margin(MarginPt, Unit.Point);
                    page.PageColor(Colors.White);

                    page.Content()
                        .AlignCenter()
                        .Image(png)
                        .FitArea();

                    // Real, selectable XGID text in the upper-right. Drawn on
                    // the foreground layer so it sits over the image; skipped
                    // when the request carries no XGID.
                    if (!string.IsNullOrEmpty(xgid))
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
        });

        return document.GeneratePdf();
    }
}