using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.ExportRaster;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Verifies PPTX slide sizing: the slide size is derived from the first
/// PNG's aspect so the image fills the slide, and all slides in a deck
/// must share the first PNG's dimensions — an OOXML constraint we enforce
/// explicitly rather than silently accepting mismatched images.
/// </summary>
public class PptxSizingTests
{
    private const int EmuPerInch = 914400;
    private const long MarginHalfInch = 457200L;   // matches PptxBuilder.MarginH / MarginV

    // -----------------------------------------------------------------------
    //  Slide size is derived from first PNG's aspect
    // -----------------------------------------------------------------------

    [Fact]
    public void Pptx_SlideSize_MatchesFirstPngAspect_Widescreen()
    {
        var request = TestFixtures.MinimalRequest();
        var options = new DiagramOptions
        {
            Size = DiagramSize.Medium,
            Aspect = AspectPreset.Widescreen16x9,
        };
        var png = DiagramRasterRenderer.RenderPng(request, options);
        var pptx = DiagramRasterRenderer.RenderPptx(request, options);

        var (pngW, pngH) = ReadPngDimensions(png);
        double pngAspect = pngW / (double)pngH;

        var (slideCx, slideCy) = ReadSlideSize(pptx);
        double availableAspect = (slideCx - 2.0 * MarginHalfInch) / (slideCy - 2.0 * MarginHalfInch);

        // Slide width is chosen so (Cx - 2·margin) / (Cy - 2·margin) == PNG aspect.
        Assert.InRange(availableAspect, pngAspect - 0.005, pngAspect + 0.005);
    }

    [Fact]
    public void Pptx_SlideSize_NaturalAspect_ProducesNearSquareSlide()
    {
        var request = TestFixtures.MinimalRequest();
        var options = new DiagramOptions { Aspect = AspectPreset.Natural };
        var png = DiagramRasterRenderer.RenderPng(request, options);
        var pptx = DiagramRasterRenderer.RenderPptx(request, options);

        var (pngW, pngH) = ReadPngDimensions(png);
        double pngAspect = pngW / (double)pngH;

        var (slideCx, slideCy) = ReadSlideSize(pptx);
        double availableAspect = (slideCx - 2.0 * MarginHalfInch) / (slideCy - 2.0 * MarginHalfInch);

        Assert.InRange(availableAspect, pngAspect - 0.005, pngAspect + 0.005);
    }

    // -----------------------------------------------------------------------
    //  Mismatch throws — OOXML allows one slide size per deck, so silent
    //  acceptance would let later images overflow or float in whitespace.
    // -----------------------------------------------------------------------

    [Fact]
    public void Pptx_MismatchedPngDimensions_Throws()
    {
        // DiagramRasterRenderer.RenderPptx(IEnumerable<DiagramRequest>) shares one
        // DiagramOptions across all requests, so its callers can't trigger a
        // mismatch via the public renderer API alone. Call PptxBuilder
        // directly (InternalsVisibleTo makes this available to the test
        // project) to exercise the guard.
        var req = TestFixtures.MinimalRequest();
        var pngNatural = DiagramRasterRenderer.RenderPng(req,
            new DiagramOptions { Aspect = AspectPreset.Natural });
        var pngWide = DiagramRasterRenderer.RenderPng(req,
            new DiagramOptions { Aspect = AspectPreset.Widescreen16x9 });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PptxBuilder.Build(new[] { (pngNatural, string.Empty), (pngWide, string.Empty) }));

        Assert.Contains("one size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static (int Width, int Height) ReadPngDimensions(byte[] png)
    {
        int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return (w, h);
    }

    private static (long Cx, long Cy) ReadSlideSize(byte[] pptx)
    {
        using var zip = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);
        var entry = zip.GetEntry("ppt/presentation.xml")
            ?? throw new InvalidOperationException("ppt/presentation.xml missing");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        var doc = XDocument.Parse(reader.ReadToEnd());
        var ns = XNamespace.Get("http://schemas.openxmlformats.org/presentationml/2006/main");
        var sldSz = doc.Descendants(ns + "sldSz").FirstOrDefault()
            ?? throw new InvalidOperationException("sldSz element missing");
        long cx = long.Parse(sldSz.Attribute("cx")!.Value);
        long cy = long.Parse(sldSz.Attribute("cy")!.Value);
        return (cx, cy);
    }
}
