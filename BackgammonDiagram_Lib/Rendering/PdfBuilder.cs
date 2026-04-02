using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// Builds a PDF byte array from one or more PNG images.
/// Each PNG becomes one page. An optional per-page title string
/// is rendered as centered text above the image.
/// Internal — called only by DiagramRenderer.
/// </summary>
internal static class PdfBuilder
{
    // Page dimensions: widescreen landscape matching PPTX (13.33" × 7.5")
    // QuestPDF uses points (1 inch = 72 points)
    private const float PageWidthPt = 13.33f * 72;   // ~960pt
    private const float PageHeightPt = 7.5f * 72;    // 540pt

    private const float MarginPt = 36;               // 0.5"
    private const float TitleFontSize = 18;
    private const float TitleGapPt = 8;

    public static byte[] Build(IEnumerable<(byte[] Png, string? Title)> pages)
    {
        var pageList = pages.ToList();

        var document = Document.Create(container =>
        {
            foreach (var (png, title) in pageList)
            {
                container.Page(page =>
                {
                    page.Size(PageWidthPt, PageHeightPt, Unit.Point);
                    page.Margin(MarginPt, Unit.Point);
                    page.PageColor(Colors.White);

                    bool hasTitle = !string.IsNullOrWhiteSpace(title);

                    if (hasTitle)
                    {
                        page.Header()
                            .PaddingBottom(TitleGapPt, Unit.Point)
                            .AlignCenter()
                            .Text(title!)
                            .FontSize(TitleFontSize)
                            .FontColor(Colors.Black);
                    }

                    page.Content()
                        .AlignCenter()
                        .Image(png)
                        .FitArea();
                });
            }
        });

        return document.GeneratePdf();
    }
}