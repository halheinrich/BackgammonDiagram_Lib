using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.ExportRaster;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Elements;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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
    //  The builders' checks are inside their loops: cancelled after the
    //  first page, slide or entry, each loop stops there
    // -----------------------------------------------------------------------

    [Fact]
    public void PdfComposition_CancelledAfterFirstPage_StopsBeforeTheSecond()
    {
        using var source = new CancellationTokenSource();
        var pages = new CancellingList<(byte[] Png, string? Xgid)>(
            [(s_png, null), (s_png, null), (s_png, null)], cancelOnItem: 1, source);
        var document = Document.Create(container => PdfBuilder.ComposePages(container, pages, source.Token));

        Assert.Throws<OperationCanceledException>(() => document.GeneratePdf());
        Assert.Equal(2, pages.Handed);
    }

    [Fact]
    public void PptxSlides_CancelledAfterFirstSlide_StopsBeforeTheSecond()
    {
        using var source = new CancellationTokenSource();
        var slides = new CancellingList<(byte[] Png, string? Xgid)>(
            [(s_png, null), (s_png, null), (s_png, null)], cancelOnItem: 1, source);
        using var ms = new MemoryStream();
        using var doc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation);
        var presentationPart = doc.AddPresentationPart();
        presentationPart.Presentation = new Presentation(new SlideIdList());
        var layoutPart = presentationPart.AddNewPart<SlideMasterPart>().AddNewPart<SlideLayoutPart>();

        Assert.Throws<OperationCanceledException>(() =>
            PptxBuilder.AddSlides(presentationPart, layoutPart, slides, 1000, source.Token));
        Assert.Single(presentationPart.SlideParts);
    }

    // -----------------------------------------------------------------------
    //  Inside QuestPDF's generation: each deck page checks the token as it
    //  is laid out and drawn; only this operation's cancellation is unwrapped
    // -----------------------------------------------------------------------

    [Fact]
    public void PdfGeneration_CancelledAsItRuns_StopsAtTheNextDeckPageAsItself()
    {
        using var source = new CancellationTokenSource();
        int laterPageCalls = 0;
        var document = Document.Create(container =>
        {
            ActionPage(container, source.Cancel);    // cancels as generation lays out page 1
            PdfBuilder.ComposePages(container, [(s_png, null), (s_png, null)], source.Token);
            ActionPage(container, () => laterPageCalls++);
        });

        var ex = Assert.Throws<OperationCanceledException>(() => PdfBuilder.Generate(document, source.Token));
        Assert.Equal(source.Token, ex.CancellationToken);
        Assert.Equal(0, laterPageCalls);    // stopped at the first deck page, not after the whole generation
    }

    [Fact]
    public void PdfGeneration_UnrelatedFailure_StaysAnError_EvenWithTheTokenCancelled()
    {
        using var source = new CancellationTokenSource();
        var document = Document.Create(container =>
        {
            ActionPage(container, () =>
            {
                source.Cancel();
                throw new InvalidOperationException("A rendering failure.");
            });
            PdfBuilder.ComposePages(container, [(s_png, null)], source.Token);
        });

        var ex = Assert.Throws<DocumentDrawingException>(() => PdfBuilder.Generate(document, source.Token));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void PdfGeneration_AnotherTokensCancellation_StaysAnError()
    {
        using var source = new CancellationTokenSource();
        using var other = new CancellationTokenSource();
        other.Cancel();
        var document = Document.Create(container =>
        {
            ActionPage(container, () =>
            {
                source.Cancel();
                other.Token.ThrowIfCancellationRequested();
            });
            PdfBuilder.ComposePages(container, [(s_png, null)], source.Token);
        });

        var ex = Assert.Throws<DocumentDrawingException>(() => PdfBuilder.Generate(document, source.Token));
        var inner = Assert.IsType<OperationCanceledException>(ex.InnerException);
        Assert.Equal(other.Token, inner.CancellationToken);
    }

    /// <summary>
    /// A page that runs <paramref name="act"/> each time QuestPDF draws it. It
    /// sits on the background layer, as the deck pages' check does: that layer
    /// is drawn and never measured, and QuestPDF wraps a throw while drawing
    /// in a <see cref="DocumentDrawingException"/> (a throw while measuring
    /// passes unwrapped).
    /// </summary>
    private static void ActionPage(IDocumentContainer container, Action act) =>
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Background().Dynamic(new ActingComponent(act));
            page.Content().Text(string.Empty);
        });

    private sealed class ActingComponent(Action act) : IDynamicComponent
    {
        public DynamicComponentComposeResult Compose(DynamicContext context)
        {
            act();
            return new DynamicComponentComposeResult { Content = context.CreateElement(_ => { }), HasMoreContent = false };
        }
    }

    private const string RelsXml ="<?xml version=\"1.0\" encoding=\"utf-8\"?>"
        + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
        + "<Relationship Id=\"rIdOld\" Type=\"t\" Target=\"/ppt/presentation.xml\"/></Relationships>";

    private const string PartXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
        + "<r xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><c id=\"rIdOld\">"
        + "<a:x xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"/></c></r>";

    // Every pass that processes more than one entry, with two entries it
    // rewrites: (pass, first entry, second entry, both entries' content).
    public static TheoryData<string, string, string, string> MultiEntryPasses() => new()
    {
        { nameof(PptxBuilder.FixRelationshipTargets), "_rels/.rels", "ppt/_rels/presentation.xml.rels", RelsXml },
        { nameof(PptxBuilder.FixNamespaceDeclarations), "ppt/a.xml", "ppt/b.xml", PartXml },
        { nameof(PptxBuilder.FixRelationshipIds), "_rels/.rels", "ppt/_rels/presentation.xml.rels", RelsXml },
        { nameof(PptxBuilder.ReplaceRelationshipIds), "ppt/a.xml", "ppt/b.xml", PartXml },
        { nameof(PptxBuilder.FixXmlDeclarations), "ppt/a.xml", "ppt/b.xml", PartXml },
    };

    [Theory]
    [MemberData(nameof(MultiEntryPasses))]
    public void PptxFixUpPass_CancelledAfterFirstEntry_StopsBeforeTheSecond(
        string pass, string first, string second, string content)
    {
        using var source = new CancellationTokenSource();
        using var ms = CancellingZipStream.Of(source, (first, content), (second, content));

        Action run = pass switch
        {
            nameof(PptxBuilder.FixRelationshipTargets) => () => PptxBuilder.FixRelationshipTargets(ms, source.Token),
            nameof(PptxBuilder.FixNamespaceDeclarations) => () => PptxBuilder.FixNamespaceDeclarations(ms, source.Token),
            nameof(PptxBuilder.FixRelationshipIds) => () => PptxBuilder.FixRelationshipIds(ms, source.Token),
            nameof(PptxBuilder.FixXmlDeclarations) => () => PptxBuilder.FixXmlDeclarations(ms, source.Token),
            _ => () =>
            {
                using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);
                PptxBuilder.ReplaceRelationshipIds(zip, new Dictionary<string, string> { ["rIdOld"] = "rId1" }, source.Token);
            },
        };

        Assert.Throws<OperationCanceledException>(run);
        var after = EntryTexts(ms.ToArray());
        Assert.NotEqual(content, after[first]);   // the first entry was processed …
        Assert.Equal(content, after[second]);     // … and the loop stopped before the second
    }

    /// <summary>
    /// A list whose enumerator cancels <c>source</c> as it hands over the
    /// item at <c>cancelOnItem</c>, counting the items handed.
    /// </summary>
    private sealed class CancellingList<T>(IReadOnlyList<T> items, int cancelOnItem, CancellationTokenSource source)
        : IReadOnlyList<T>
    {
        public int Handed { get; private set; }

        public int Count => items.Count;

        public T this[int index] => items[index];

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i == cancelOnItem)
                    source.Cancel();
                Handed++;
                yield return items[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A ZIP archive on an expandable stream that cancels <c>source</c> on
    /// the first read of the first entry's data. Opening the archive in
    /// Update mode reads only the entries' headers; an entry's data is read
    /// when it is opened, so the cancel lands as a pass opens its first entry.
    /// </summary>
    private sealed class CancellingZipStream : MemoryStream
    {
        private readonly CancellationTokenSource _source;
        private readonly long _firstDataStart;
        private readonly long _firstDataEnd;

        private CancellingZipStream(CancellationTokenSource source, long firstDataStart, long firstDataEnd)
        {
            _source = source;
            _firstDataStart = firstDataStart;
            _firstDataEnd = firstDataEnd;
        }

        public static CancellingZipStream Of(CancellationTokenSource source, params (string Name, string Content)[] entries)
        {
            using var build = new MemoryStream();
            long firstDataEnd = 0;
            using (var zip = new ZipArchive(build, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (name, content) in entries)
                {
                    using (var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)))
                        writer.Write(content);
                    if (firstDataEnd == 0)
                        firstDataEnd = build.Position;
                }
            }

            // The first local header starts at 0: 30 fixed bytes, then the
            // name and the extra field, whose lengths sit at offsets 26 and 28.
            var bytes = build.ToArray();
            long firstDataStart = 30 + BitConverter.ToUInt16(bytes, 26) + BitConverter.ToUInt16(bytes, 28);

            var stream = new CancellingZipStream(source, firstDataStart, firstDataEnd);
            stream.Write(bytes);
            stream.Position = 0;
            return stream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Observe();
            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            Observe();
            return base.Read(buffer);
        }

        public override int ReadByte()
        {
            Observe();
            return base.ReadByte();
        }

        private void Observe()
        {
            if (Position >= _firstDataStart && Position < _firstDataEnd)
                _source.Cancel();
        }
    }

    private static Dictionary<string, string> EntryTexts(byte[] zipBytes)
    {
        using var zip = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
        return zip.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var reader = new StreamReader(e.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        });
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
