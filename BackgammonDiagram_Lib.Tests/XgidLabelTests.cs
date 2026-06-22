using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.ExportRaster;
using QuestPDF.Infrastructure;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Covers the XGID label across the three export layers:
///   * SVG  — baked <text>, gated on <see cref="DiagramOptions.ShowXgid"/>.
///   * PPTX — real text run in a slide shape (plain XML, asserted directly).
///   * PDF  — real selectable text. QuestPDF subsets fonts, so the content
///            stream carries glyph IDs, not ASCII; the proof that the glyphs
///            are real text is the embedded ToUnicode CMap, which must map
///            every character of the XGID to its Unicode value.
/// </summary>
public class XgidLabelTests
{
    static XgidLabelTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private const string SampleXgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:0:0:10";

    // -----------------------------------------------------------------------
    //  SVG — opt-in baked label
    // -----------------------------------------------------------------------

    [Fact]
    public void Svg_ShowXgid_EmitsXgidText()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Xgid = SampleXgid;
        var svg = DiagramRenderer.RenderSvg(b.Build(),
            TestFixtures.DefaultOptions() with { ShowXgid = true });

        Assert.Contains(SampleXgid, svg);
    }

    [Fact]
    public void Svg_Default_DoesNotEmitXgidText()
    {
        // ShowXgid defaults off, so interactive consumers (BgDiag_Razor,
        // BgQuiz) render exactly as before even when an XGID is present.
        var b = TestFixtures.MinimalBuilder();
        b.Xgid = SampleXgid;
        var svg = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.DoesNotContain(SampleXgid, svg);
    }

    [Fact]
    public void Svg_ShowXgid_EmptyXgid_EmitsNoLabel()
    {
        // Empty XGID short-circuits the label, so ShowXgid=true with no XGID
        // is byte-identical to the default render — nothing extra is drawn.
        var b = TestFixtures.MinimalBuilder();
        b.Xgid = string.Empty;
        var withFlag = DiagramRenderer.RenderSvg(b.Build(),
            TestFixtures.DefaultOptions() with { ShowXgid = true });
        var without = DiagramRenderer.RenderSvg(b.Build(), TestFixtures.DefaultOptions());

        Assert.Equal(without, withFlag);
    }

    // -----------------------------------------------------------------------
    //  PPTX — real text run
    // -----------------------------------------------------------------------

    [Fact]
    public void Pptx_CarriesXgidAsRealTextRun()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Xgid = SampleXgid;
        var pptx = DiagramRasterRenderer.RenderPptx(b.Build(), TestFixtures.DefaultOptions());

        using var zip = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);
        var a = XNamespace.Get("http://schemas.openxmlformats.org/drawingml/2006/main");

        var slideTexts = zip.Entries
            .Where(e => Regex.IsMatch(e.FullName, @"^ppt/slides/slide\d+\.xml$"))
            .SelectMany(e =>
            {
                using var reader = new StreamReader(e.Open(), Encoding.UTF8);
                return XDocument.Parse(reader.ReadToEnd())
                    .Descendants(a + "t")
                    .Select(t => t.Value)
                    .ToList();
            })
            .ToList();

        // The XGID is the real text, distinct from the baked-PNG image.
        Assert.Contains(SampleXgid, slideTexts);
    }

    [Fact]
    public void Pptx_NoXgid_StaysGreenAndAddsNoTextRun()
    {
        // No XGID → no text shape, so a plain image-only slide is unchanged
        // and the conformance post-processors have nothing new to handle.
        var pptx = DiagramRasterRenderer.RenderPptx(
            TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());

        using var zip = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);
        var a = XNamespace.Get("http://schemas.openxmlformats.org/drawingml/2006/main");

        var slideTexts = zip.Entries
            .Where(e => Regex.IsMatch(e.FullName, @"^ppt/slides/slide\d+\.xml$"))
            .SelectMany(e =>
            {
                using var reader = new StreamReader(e.Open(), Encoding.UTF8);
                return XDocument.Parse(reader.ReadToEnd())
                    .Descendants(a + "t")
                    .Select(t => t.Value);
            })
            .ToList();

        Assert.Empty(slideTexts);
    }

    // -----------------------------------------------------------------------
    //  PDF — real selectable text (verified via the ToUnicode CMap)
    // -----------------------------------------------------------------------

    [Fact]
    public void Pdf_CarriesXgidAsRealText()
    {
        var b = TestFixtures.MinimalBuilder();
        b.Xgid = SampleXgid;
        var pdf = DiagramRasterRenderer.RenderPdf(b.Build(), TestFixtures.DefaultOptions());

        var mappedUnicode = CollectToUnicodeChars(pdf);

        // Every distinct character of the XGID must be reachable as real text
        // via the embedded font's ToUnicode CMap — i.e. selecting the label in
        // a reader yields the XGID, not glyph soup.
        Assert.NotEmpty(mappedUnicode);
        foreach (char c in SampleXgid.Distinct())
            Assert.Contains(c, mappedUnicode);
    }

    [Fact]
    public void Pdf_NoXgid_EmitsNoToUnicodeText()
    {
        // Without an XGID the page carries no real text at all, so the PDF has
        // no ToUnicode mappings — proving the overlay is the only text source.
        var pdf = DiagramRasterRenderer.RenderPdf(
            TestFixtures.MinimalRequest(), TestFixtures.DefaultOptions());

        Assert.Empty(CollectToUnicodeChars(pdf));
    }

    /// <summary>
    /// Inflates every FlateDecode stream in the PDF, parses any ToUnicode
    /// CMap (bfchar / bfrange) blocks, and returns the set of Unicode
    /// characters the embedded fonts can map glyphs back to.
    /// </summary>
    private static HashSet<char> CollectToUnicodeChars(byte[] pdf)
    {
        var ascii = Encoding.ASCII.GetString(pdf);
        var chars = new HashSet<char>();

        int idx = 0;
        while ((idx = ascii.IndexOf("stream", idx, StringComparison.Ordinal)) >= 0)
        {
            int start = idx + "stream".Length;
            if (start < pdf.Length && pdf[start] == '\r') start++;
            if (start < pdf.Length && pdf[start] == '\n') start++;
            int end = ascii.IndexOf("endstream", start, StringComparison.Ordinal);
            if (end < 0) break;

            string inflated = TryInflate(pdf, start, end - start);
            idx = end + "endstream".Length;
            if (inflated.Length == 0 || !inflated.Contains("beginbf")) continue;

            ParseBfChar(inflated, chars);
            ParseBfRange(inflated, chars);
        }
        return chars;
    }

    private static string TryInflate(byte[] data, int offset, int length)
    {
        if (length <= 2) return string.Empty;
        try
        {
            // PDF FlateDecode == zlib: skip the 2-byte zlib header.
            using var msIn = new MemoryStream(data, offset + 2, length - 2);
            using var ds = new DeflateStream(msIn, CompressionMode.Decompress);
            using var msOut = new MemoryStream();
            ds.CopyTo(msOut);
            return Encoding.ASCII.GetString(msOut.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    private static readonly Regex BfCharBlock =
        new(@"beginbfchar(.*?)endbfchar", RegexOptions.Singleline);
    private static readonly Regex BfRangeBlock =
        new(@"beginbfrange(.*?)endbfrange", RegexOptions.Singleline);
    private static readonly Regex HexToken = new(@"<([0-9A-Fa-f]+)>");

    private static void ParseBfChar(string cmap, HashSet<char> chars)
    {
        foreach (Match block in BfCharBlock.Matches(cmap))
        {
            // Entries are <src> <dst> pairs; the dst is the Unicode value.
            var tokens = HexToken.Matches(block.Groups[1].Value);
            for (int i = 1; i < tokens.Count; i += 2)
                AddUnicode(tokens[i].Groups[1].Value, chars);
        }
    }

    private static void ParseBfRange(string cmap, HashSet<char> chars)
    {
        foreach (Match block in BfRangeBlock.Matches(cmap))
        {
            // Entries are <srcLo> <srcHi> <dstLo> triples; expand dst over
            // the source span.
            var tokens = HexToken.Matches(block.Groups[1].Value);
            for (int i = 0; i + 2 < tokens.Count; i += 3)
            {
                int srcLo = Convert.ToInt32(tokens[i].Groups[1].Value, 16);
                int srcHi = Convert.ToInt32(tokens[i + 1].Groups[1].Value, 16);
                int dstLo = Convert.ToInt32(tokens[i + 2].Groups[1].Value, 16);
                for (int k = 0; k <= srcHi - srcLo && k < 0x10000; k++)
                {
                    int cp = dstLo + k;
                    if (cp is > 0 and <= 0xFFFF) chars.Add((char)cp);
                }
            }
        }
    }

    private static void AddUnicode(string hex, HashSet<char> chars)
    {
        // Unicode values are UTF-16BE, 4 hex digits per code unit.
        for (int i = 0; i + 4 <= hex.Length; i += 4)
        {
            int cp = Convert.ToInt32(hex.Substring(i, 4), 16);
            if (cp > 0) chars.Add((char)cp);
        }
    }
}
