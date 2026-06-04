using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using BackgammonDiagram_Lib;
using BackgammonDiagram_Lib.Rendering;
using BackgammonDiagram_Lib.ExportRaster;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Regression tests for PPTX conformance fixes in PptxBuilder.
///
/// The OpenXml SDK produces structurally valid PPTX files, but several
/// details trigger PowerPoint's repair prompt. PptxBuilder applies
/// post-processing fixes for each. These tests verify the fixes remain
/// in place by inspecting the raw zip contents of generated PPTX files.
///
/// History: the root cause of the original repair prompt was a
/// sldLayoutId value (2199) below the OOXML minimum of 2147483648.
/// The post-processing fixes (Content_Types, relative .rels paths,
/// namespace hoisting, sequential rIds, XML declarations) are
/// additional hardening against other SDK quirks that PowerPoint
/// is known to flag.
/// </summary>
public class PptxConformanceTests
{
    private static byte[] GenerateSingleSlidePptx()
    {
        var request = new DiagramRequest.Builder
        {
            Mop = new int[26],
            OnRollName = "Player",
            OpponentName = "Opponent",
            IsCube = false,
            Dice = [3, 1],
            CubeSize = 1,
            Mode = DiagramMode.Problem,
            Title = "Conformance Test",
        }.Build();

        var options = new DiagramOptions
        {
            Size = DiagramSize.Medium
        };

        return DiagramRasterRenderer.RenderPptx(request, options);
    }

    private static ZipArchive OpenPptx(byte[] pptx)
        => new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);

    private static string ReadEntry(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path)
            ?? throw new Exception($"Entry not found: {path}");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static List<string> GetEntriesByExtension(ZipArchive zip, params string[] extensions)
        => zip.Entries
            .Where(e => extensions.Any(ext =>
                e.FullName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Select(e => e.FullName)
            .ToList();

    // -----------------------------------------------------------------

    [Fact]
    public void SlideLayoutId_IsAtLeastMinimum()
    {
        // OOXML spec: sldLayoutId must be >= 2147483648 (0x80000000).
        // A value of 2199 was the original root cause of the repair prompt.
        using var zip = OpenPptx(GenerateSingleSlidePptx());

        var masterEntries = GetEntriesByExtension(zip, ".xml")
            .Where(e => e.Contains("slideMasters/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var path in masterEntries)
        {
            var content = ReadEntry(zip, path);
            var doc = XDocument.Parse(content);
            var ns = XNamespace.Get("http://schemas.openxmlformats.org/presentationml/2006/main");

            foreach (var elem in doc.Descendants(ns + "sldLayoutId"))
            {
                var idAttr = elem.Attribute("id");
                Assert.NotNull(idAttr);
                var id = uint.Parse(idAttr!.Value);
                Assert.True(id >= 2147483648,
                    $"sldLayoutId {id} in {path} is below OOXML minimum of 2147483648");
            }
        }
    }

    [Fact]
    public void ContentTypes_XmlDefaultIsApplicationXml()
    {
        // SDK writes the presentation content type as the Default for .xml;
        // it must be the generic "application/xml".
        using var zip = OpenPptx(GenerateSingleSlidePptx());
        var content = ReadEntry(zip, "[Content_Types].xml");
        var doc = XDocument.Parse(content);
        var ns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");

        var xmlDefault = doc.Root!.Elements(ns + "Default")
            .FirstOrDefault(e => e.Attribute("Extension")?.Value == "xml");

        Assert.NotNull(xmlDefault);
        Assert.Equal("application/xml", xmlDefault!.Attribute("ContentType")!.Value);
    }

    [Fact]
    public void RelationshipTargets_AreRelative()
    {
        // OPC spec requires relative Target URIs in .rels files.
        // SDK writes absolute paths like "/ppt/presentation.xml".
        using var zip = OpenPptx(GenerateSingleSlidePptx());
        var relsEntries = GetEntriesByExtension(zip, ".rels");

        var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

        foreach (var path in relsEntries)
        {
            var content = ReadEntry(zip, path);
            var doc = XDocument.Parse(content);

            foreach (var rel in doc.Root!.Elements(relsNs + "Relationship"))
            {
                var target = rel.Attribute("Target")?.Value;
                Assert.NotNull(target);
                Assert.False(target!.StartsWith("/"),
                    $"Absolute Target \"{target}\" in {path}; must be relative");
            }
        }
    }

    [Fact]
    public void NamespaceDeclarations_NotScatteredOnChildren()
    {
        using var zip = OpenPptx(GenerateSingleSlidePptx());

        var xmlEntries = GetEntriesByExtension(zip, ".xml")
            .Where(e => e.StartsWith("ppt/", StringComparison.OrdinalIgnoreCase) &&
                        !e.Contains("/_rels/"))
            .ToList();

        var trackedNamespaces = new[]
        {
        ("a", "http://schemas.openxmlformats.org/drawingml/2006/main"),
        ("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"),
        };

        foreach (var path in xmlEntries)
        {
            var content = ReadEntry(zip, path);
            var doc = XDocument.Parse(content);
            var root = doc.Root!;

            foreach (var descendant in root.Descendants())
            {
                foreach (var attr in descendant.Attributes().Where(a => a.IsNamespaceDeclaration))
                {
                    foreach (var (prefix, ns) in trackedNamespaces)
                    {
                        if (attr.Name.LocalName == prefix && attr.Value == ns)
                        {
                            Assert.Fail(
                                $"Redundant xmlns:{prefix} on non-root element in {path}; " +
                                $"expected declaration only on root element");
                        }
                    }
                }
            }
        }
    }
    [Fact]
    public void RelationshipIds_AreSequential()
    {
        // SDK generates GUID-based rIds; PowerPoint expects sequential rId1, rId2, ...
        using var zip = OpenPptx(GenerateSingleSlidePptx());
        var relsEntries = GetEntriesByExtension(zip, ".rels");

        var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

        foreach (var path in relsEntries)
        {
            var content = ReadEntry(zip, path);
            var doc = XDocument.Parse(content);

            var ids = doc.Root!.Elements(relsNs + "Relationship")
                .Select(e => e.Attribute("Id")?.Value)
                .ToList();

            for (int i = 0; i < ids.Count; i++)
            {
                var expected = $"rId{i + 1}";
                Assert.Equal(expected, ids[i]);
            }
        }
    }

    [Fact]
    public void XmlDeclarations_MatchPowerPointFormat()
    {
        // PowerPoint expects: <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        // SDK writes: encoding="utf-8" without standalone.
        using var zip = OpenPptx(GenerateSingleSlidePptx());

        var allXmlEntries = GetEntriesByExtension(zip, ".xml", ".rels");

        foreach (var path in allXmlEntries)
        {
            var content = ReadEntry(zip, path);
            Assert.StartsWith("<?xml", content,
                StringComparison.Ordinal);
            Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
                content, StringComparison.Ordinal);
        }
    }
}