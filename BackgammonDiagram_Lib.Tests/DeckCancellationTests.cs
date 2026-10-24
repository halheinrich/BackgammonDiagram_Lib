using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.ExportRaster;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The multi-request PDF and PPTX renders honour their cancellation token
/// (halheinrich/backgammon#424): before any page is rasterized, between
/// pages, before the build, and inside the builders' own loops — and the
/// caller sees the <see cref="OperationCanceledException"/> as itself.
/// </summary>
public class DeckCancellationTests
{
    public DeckCancellationTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public enum DeckFormat { Pdf, Pptx }

    // One real page, rendered once: the stand-in rasterizer hands it back for
    // every call so the builds see a valid PNG without rasterizing again.
    private static readonly byte[] s_png =
        DiagramRasterRenderer.RenderPng(TestFixtures.MinimalRequest(), new DiagramOptions { ShowXgid = false });

    private static readonly byte[] s_notAPng = [1, 2, 3];

    /// <summary>
    /// An <see cref="ISvgRasterizer"/> that counts its calls, returns
    /// <see cref="s_png"/>, and on its <c>cancelOnCall</c>th call cancels
    /// <see cref="Source"/> (and, when told, returns bytes no build accepts,
    /// so a build reached after it fails differently).
    /// </summary>
    private sealed class CancellingRasterizer(int cancelOnCall = 0, bool refuseCancelledPage = false)
        : ISvgRasterizer
    {
        public CancellationTokenSource Source { get; } = new();

        public int Calls { get; private set; }

        public byte[] Rasterize(string svgContent, int targetWidth)
        {
            Calls++;
            if (Calls != cancelOnCall)
                return s_png;
            Source.Cancel();
            return refuseCancelledPage ? s_notAPng : s_png;
        }
    }

    private static List<DiagramRequest> Requests(int count) =>
        Enumerable.Repeat(TestFixtures.MinimalRequest(), count).ToList();

    private static byte[] Render(DeckFormat format, IEnumerable<DiagramRequest> requests,
        ISvgRasterizer rasterizer, CancellationToken cancellationToken) => format switch
    {
        DeckFormat.Pdf => DiagramRasterRenderer.RenderPdf(requests, new DiagramOptions(), rasterizer, cancellationToken),
        _ => DiagramRasterRenderer.RenderPptx(requests, new DiagramOptions(), rasterizer, cancellationToken),
    };

    // -----------------------------------------------------------------------
    //  The public renders
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(DeckFormat.Pdf)]
    [InlineData(DeckFormat.Pptx)]
    public void AlreadyCancelledToken_ThrowsBeforeAnyRasterization(DeckFormat format)
    {
        var rasterizer = new CancellingRasterizer();
        rasterizer.Source.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => Render(format, Requests(3), rasterizer, rasterizer.Source.Token));
        Assert.Equal(0, rasterizer.Calls);
    }

    [Theory]
    [InlineData(DeckFormat.Pdf)]
    [InlineData(DeckFormat.Pptx)]
    public void CancelledDuringSecondPage_ThrowsBeforeTheThird(DeckFormat format)
    {
        var rasterizer = new CancellingRasterizer(cancelOnCall: 2);

        Assert.Throws<OperationCanceledException>(
            () => Render(format, Requests(4), rasterizer, rasterizer.Source.Token));
        Assert.Equal(2, rasterizer.Calls);
    }

    [Theory]
    [InlineData(DeckFormat.Pdf)]
    [InlineData(DeckFormat.Pptx)]
    public void CancelledDuringLastPage_ThrowsBeforeTheBuild(DeckFormat format)
    {
        // The last page is bytes no build accepts: a build reached with it
        // fails with something other than the cancellation.
        var rasterizer = new CancellingRasterizer(cancelOnCall: 3, refuseCancelledPage: true);

        Assert.Throws<OperationCanceledException>(
            () => Render(format, Requests(3), rasterizer, rasterizer.Source.Token));
        Assert.Equal(3, rasterizer.Calls);
    }

    [Fact]
    public void Pdf_LiveToken_SameDocumentAsWithoutOne()
    {
        using var source = new CancellationTokenSource();
        var without = DiagramRasterRenderer.RenderPdf(Requests(3), new DiagramOptions(), new CancellingRasterizer());
        var with = DiagramRasterRenderer.RenderPdf(Requests(3), new DiagramOptions(), new CancellingRasterizer(), source.Token);

        Assert.Equal(3, PdfPageCount(without));
        Assert.Equal(DateMaskedSha256(without), DateMaskedSha256(with));
    }

    [Fact]
    public void Pptx_LiveToken_SameDeckAsWithoutOne()
    {
        using var source = new CancellationTokenSource();
        var without = DiagramRasterRenderer.RenderPptx(Requests(3), new DiagramOptions(), new CancellingRasterizer());
        var with = DiagramRasterRenderer.RenderPptx(Requests(3), new DiagramOptions(), new CancellingRasterizer(), source.Token);

        Assert.Equal(3, EntryHashes(without).Keys.Count(name => SlidePart().IsMatch(name)));
        Assert.Equal(EntryHashes(without), EntryHashes(with));
    }

    [Theory]
    [InlineData(DeckFormat.Pdf)]
    [InlineData(DeckFormat.Pptx)]
    public void NullRequests_ThrowsArgumentNull(DeckFormat format)
    {
        var rasterizer = new CancellingRasterizer();

        var ex = Assert.Throws<ArgumentNullException>(
            () => Render(format, null!, rasterizer, CancellationToken.None));
        Assert.Equal("requests", ex.ParamName);
        Assert.Equal(0, rasterizer.Calls);
    }

    // -----------------------------------------------------------------------
    //  The builders' own loops
    // -----------------------------------------------------------------------

    [Fact]
    public void PdfComposition_CancelledToken_ArrivesThroughQuestPdfAsItself()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var document = Document.Create(container =>
            PdfBuilder.ComposePages(container, [(s_png, null), (s_png, null)], source.Token));

        Assert.Throws<OperationCanceledException>(() => document.GeneratePdf());
    }

    [Fact]
    public void PptxSlides_CancelledToken_ThrowsBeforeAnySlide()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using var ms = new MemoryStream();
        using var doc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation);
        var presentationPart = doc.AddPresentationPart();
        presentationPart.Presentation = new Presentation(new SlideIdList());
        var layoutPart = presentationPart.AddNewPart<SlideMasterPart>().AddNewPart<SlideLayoutPart>();

        Assert.Throws<OperationCanceledException>(() =>
            PptxBuilder.AddSlides(presentationPart, layoutPart, [(s_png, null), (s_png, null)], 1000, source.Token));
        Assert.Empty(presentationPart.SlideParts);
    }

    public static TheoryData<string> FixUpPasses() =>
    [
        nameof(PptxBuilder.FixContentTypes),
        nameof(PptxBuilder.FixRelationshipTargets),
        nameof(PptxBuilder.FixNamespaceDeclarations),
        nameof(PptxBuilder.FixRelationshipIds),
        nameof(PptxBuilder.ReplaceRelationshipIds),
        nameof(PptxBuilder.FixXmlDeclarations),
    ];

    [Theory]
    [MemberData(nameof(FixUpPasses))]
    public void PptxFixUpPass_CancelledToken_Throws(string pass)
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using var ms = new MemoryStream();
        ms.Write(PptxBuilder.Build([(s_png, null), (s_png, null)]));

        Action run = pass switch
        {
            nameof(PptxBuilder.FixContentTypes) => () => PptxBuilder.FixContentTypes(ms, source.Token),
            nameof(PptxBuilder.FixRelationshipTargets) => () => PptxBuilder.FixRelationshipTargets(ms, source.Token),
            nameof(PptxBuilder.FixNamespaceDeclarations) => () => PptxBuilder.FixNamespaceDeclarations(ms, source.Token),
            nameof(PptxBuilder.FixRelationshipIds) => () => PptxBuilder.FixRelationshipIds(ms, source.Token),
            nameof(PptxBuilder.FixXmlDeclarations) => () => PptxBuilder.FixXmlDeclarations(ms, source.Token),
            _ => () =>
            {
                using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);
                PptxBuilder.ReplaceRelationshipIds(zip, new Dictionary<string, string> { ["rId1"] = "rId2" }, source.Token);
            },
        };

        Assert.Throws<OperationCanceledException>(run);
    }

    // -----------------------------------------------------------------------
    //  Helpers — the comparison ExtractFromXgToCsv's golden harness uses
    // -----------------------------------------------------------------------

    private static Dictionary<string, string> EntryHashes(byte[] pptx)
    {
        using var zip = new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var stream = e.Open();
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        });
    }

    private static int PdfPageCount(byte[] pdf) => PdfPage().Count(Encoding.Latin1.GetString(pdf));

    private static string DateMaskedSha256(byte[] pdf) =>
        Convert.ToHexStringLower(SHA256.HashData(
            Encoding.Latin1.GetBytes(PdfDate().Replace(Encoding.Latin1.GetString(pdf), "${key}(D:MASKED)"))));

    private static Regex SlidePart() => new(@"^ppt/slides/slide\d+\.xml$");

    private static Regex PdfPage() => new(@"/Type\s*/Page(?![A-Za-z])");

    private static Regex PdfDate() => new(@"(?<key>/(?:CreationDate|ModDate)\s*)\(D:[^)]*\)");
}
