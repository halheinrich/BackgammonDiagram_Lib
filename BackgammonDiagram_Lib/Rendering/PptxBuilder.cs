using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// Builds a .pptx byte array from one or more PNG images.
/// Each PNG becomes one slide. Title is baked into the PNG by the SVG renderer.
/// Internal — called only by DiagramRenderer.
/// </summary>
internal static class PptxBuilder
{
    // Slide canvas: 13.33" × 7.5" at 914400 EMUs/inch
    private const int SlideWidth = 12192000;
    private const int SlideHeight = 6858000;

    // Margins (EMUs)
    private const long MarginH = 457200L;  // 0.5"
    private const long MarginV = 457200L;  // 0.5"

    public static byte[] Build(IEnumerable<byte[]> slides)
    {
        using var ms = new MemoryStream();
        using (var doc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = BuildPresentation();

            // --- Required parts that PowerPoint expects ---

            var presProps = presentationPart.AddNewPart<PresentationPropertiesPart>();
            presProps.PresentationProperties = new PresentationProperties();
            presProps.PresentationProperties.Save();

            var viewProps = presentationPart.AddNewPart<ViewPropertiesPart>();
            viewProps.ViewProperties = new ViewProperties();
            viewProps.ViewProperties.Save();

            var tableStyles = presentationPart.AddNewPart<TableStylesPart>();
            using (var tw = new StreamWriter(tableStyles.GetStream(FileMode.Create)))
                tw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><a:tblStyleLst xmlns:a=""http://schemas.openxmlformats.org/drawingml/2006/main"" def=""{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}""/>");

            doc.AddExtendedFilePropertiesPart();
            doc.ExtendedFilePropertiesPart!.Properties =
                new DocumentFormat.OpenXml.ExtendedProperties.Properties();
            doc.ExtendedFilePropertiesPart.Properties.Save();

            doc.AddCoreFilePropertiesPart();
            using (var cw = new StreamWriter(doc.CoreFilePropertiesPart!.GetStream(FileMode.Create), new UTF8Encoding(false)))
                cw.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><cp:coreProperties xmlns:cp=""http://schemas.openxmlformats.org/package/2006/metadata/core-properties"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:dcterms=""http://purl.org/dc/terms/"" xmlns:dcmitype=""http://purl.org/dc/dcmitype/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>");

            // --- Slide master, layout, theme ---

            var (slideLayoutPart, _) = AddMasterAndLayout(presentationPart);
            var slideIdList = presentationPart.Presentation.GetFirstChild<SlideIdList>()!;

            uint slideId = 256;
            foreach (var png in slides)
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                slidePart.AddPart(slideLayoutPart);

                var imagePart = slidePart.AddNewPart<ImagePart>("image/png", NewId());
                using (var imgStream = new MemoryStream(png))
                    imagePart.FeedData(imgStream);

                string rId = slidePart.GetIdOfPart(imagePart);
                var slide = BuildSlide(rId, png);
                slidePart.Slide = slide;
                slide.Save();

                var slideRId = presentationPart.GetIdOfPart(slidePart);
                slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = slideRId });
            }

            presentationPart.Presentation.Save();
        }

        // --- Post-process: fix SDK quirks in the zip ---
        //
        // The OpenXml SDK produces valid XML but several aspects trigger
        // PowerPoint's repair prompt. Each fix below addresses a specific
        // SDK behavior that doesn't match PowerPoint's expectations:
        //
        //  FixContentTypes     — SDK writes Default Extension="xml" with the
        //                        presentation content type instead of the
        //                        generic "application/xml".
        //  FixRelationshipTargets — SDK writes absolute Target paths in .rels
        //                        (e.g. "/ppt/presentation.xml"); OPC spec
        //                        requires relative URIs.
        //  FixNamespaceDeclarations — SDK scatters xmlns:a and xmlns:r on
        //                        individual child elements instead of
        //                        declaring them on root elements.
        //  FixRelationshipIds  — SDK generates GUID-based relationship IDs;
        //                        PowerPoint expects sequential rId1, rId2, ...
        //  FixXmlDeclarations  — SDK/XDocument writes encoding="utf-8"
        //                        without standalone; PowerPoint expects
        //                        encoding="UTF-8" standalone="yes".
        //
        FixContentTypes(ms);
        FixRelationshipTargets(ms);
        FixNamespaceDeclarations(ms);
        FixRelationshipIds(ms);
        FixXmlDeclarations(ms);
        return ms.ToArray();
    }



    // -----------------------------------------------------------------------
    //  Content_Types post-processing
    // -----------------------------------------------------------------------

    /// <summary>
    /// The OpenXml SDK writes Default Extension="xml" with the presentation
    /// main content type instead of "application/xml". PowerPoint expects the
    /// generic default plus explicit Override entries for each .xml part.
    /// This method rewrites [Content_Types].xml in the zip stream to fix that.
    /// </summary>
    private static void FixContentTypes(MemoryStream ms)
    {
        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

        var ctEntry = zip.GetEntry("[Content_Types].xml");
        if (ctEntry is null) return;

        // Read current content
        string original;
        using (var reader = new StreamReader(ctEntry.Open(), Encoding.UTF8))
            original = reader.ReadToEnd();

        var doc = XDocument.Parse(original);
        var ns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");

        // Find the bad Default Extension="xml" element
        var xmlDefault = doc.Root!.Elements(ns + "Default")
            .FirstOrDefault(e => e.Attribute("Extension")?.Value == "xml");

        if (xmlDefault is null) return;

        string badContentType = xmlDefault.Attribute("ContentType")!.Value;

        // If it's already "application/xml", nothing to fix
        if (badContentType == "application/xml") return;

        // Fix: set Default to generic application/xml
        xmlDefault.SetAttributeValue("ContentType", "application/xml");

        // Collect all .xml parts that currently rely on the Default
        // (i.e., .xml parts that do NOT already have an Override)
        var existingOverrides = doc.Root.Elements(ns + "Override")
            .Select(e => e.Attribute("PartName")?.Value)
            .Where(v => v != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The presentation.xml part was using the bad Default — add an Override for it
        // We know the SDK puts presentation at /ppt/presentation.xml
        string presentationPartName = "/ppt/presentation.xml";
        if (!existingOverrides.Contains(presentationPartName))
        {
            doc.Root.Add(new XElement(ns + "Override",
                new XAttribute("PartName", presentationPartName),
                new XAttribute("ContentType", badContentType)));
        }

        // Delete old entry and write corrected content
        ctEntry.Delete();
        var newEntry = zip.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
        using (var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(false)))
        {
            // Write without BOM, matching PowerPoint's output
            writer.Write(doc.Declaration?.ToString() ?? "");
            writer.Write(doc.Root.ToString());
        }
    }

    // -----------------------------------------------------------------------
    //  Relationship Target post-processing
    // -----------------------------------------------------------------------

    /// <summary>
    /// The OpenXml SDK writes absolute Target paths in .rels files
    /// (e.g. Target="/ppt/presentation.xml"). The OPC spec requires relative
    /// URIs and PowerPoint flags absolute paths for repair.
    /// This method rewrites all .rels files to use relative Target paths.
    /// </summary>
    private static void FixRelationshipTargets(MemoryStream ms)
    {
        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

        var relsEntries = zip.Entries
            .Where(e => e.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName)
            .ToList();

        var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

        foreach (var relsPath in relsEntries)
        {
            var entry = zip.GetEntry(relsPath);
            if (entry is null) continue;

            string original;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                original = reader.ReadToEnd();

            var doc = XDocument.Parse(original);
            bool changed = false;

            // The owning folder is the parent of the _rels folder.
            // e.g. "_rels/.rels" owns "/", "ppt/_rels/presentation.xml.rels" owns "ppt/"
            string ownerDir = GetOwnerDirectory(relsPath);

            foreach (var rel in doc.Root!.Elements(relsNs + "Relationship"))
            {
                var targetAttr = rel.Attribute("Target");
                if (targetAttr is null) continue;

                string target = targetAttr.Value;
                if (!target.StartsWith('/')) continue; 

                // Convert absolute → relative to the owning directory
                string absolute = target.TrimStart('/');
                string relative = MakeRelative(ownerDir, absolute);

                targetAttr.Value = relative;
                changed = true;
            }

            if (!changed) continue;

            entry.Delete();
            var newEntry = zip.CreateEntry(relsPath, CompressionLevel.Optimal);
            using var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(false));
            writer.Write(doc.Declaration?.ToString() ?? "");
            writer.Write(doc.Root.ToString());
        }
    }

    /// <summary>
    /// Gets the directory that "owns" a .rels file.
    /// "_rels/.rels" → "" (root), "ppt/_rels/presentation.xml.rels" → "ppt/"
    /// </summary>
    private static string GetOwnerDirectory(string relsPath)
    {
        // relsPath is e.g. "_rels/.rels" or "ppt/_rels/presentation.xml.rels"
        // The owner directory is everything before "_rels/"
        int idx = relsPath.IndexOf("_rels/", StringComparison.OrdinalIgnoreCase);
        return idx <= 0 ? "" : relsPath[..idx];
    }

    /// <summary>
    /// Converts an absolute zip path to a relative path from a base directory.
    /// e.g. base="ppt/",        absolute="ppt/presProps.xml"     → "presProps.xml"
    ///      base="",             absolute="ppt/presentation.xml"  → "ppt/presentation.xml"
    ///      base="ppt/slides/",  absolute="ppt/media/image.png"   → "../media/image.png"
    /// </summary>
    private static string MakeRelative(string baseDir, string absolutePath)
    {
        // Split into segments
        var baseParts = baseDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var targetParts = absolutePath.Split('/');

        // Find common prefix length
        int common = 0;
        while (common < baseParts.Length && common < targetParts.Length &&
               string.Equals(baseParts[common], targetParts[common], StringComparison.OrdinalIgnoreCase))
        {
            common++;
        }

        // Build: one "../" per remaining base segment, then the remaining target segments
        int ups = baseParts.Length - common;
        var parts = Enumerable.Repeat("..", ups).Concat(targetParts.Skip(common));
        return string.Join("/", parts);
    }

    // -----------------------------------------------------------------------
    //  Namespace declaration post-processing
    // -----------------------------------------------------------------------

    /// <summary>
    /// The OpenXml SDK scatters xmlns:a and xmlns:r declarations on individual
    /// child elements instead of declaring them on the root element. PowerPoint
    /// treats this as requiring repair. This method hoists common namespace
    /// declarations to the root element of each XML part under ppt/, then
    /// removes redundant declarations from descendant elements.
    /// </summary>
    private static void FixNamespaceDeclarations(MemoryStream ms)
    {
        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

        // Target: all .xml files under ppt/ (not .rels, not [Content_Types].xml)
        var xmlEntries = zip.Entries
            .Where(e => e.FullName.StartsWith("ppt/", StringComparison.OrdinalIgnoreCase) &&
                        e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                        !e.FullName.Contains("/_rels/"))
            .Select(e => e.FullName)
            .ToList();

        var nsA = XNamespace.Get("http://schemas.openxmlformats.org/drawingml/2006/main");
        var nsR = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");

        foreach (var entryPath in xmlEntries)
        {
            var entry = zip.GetEntry(entryPath);
            if (entry is null) continue;

            string original;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                original = reader.ReadToEnd();

            var doc = XDocument.Parse(original);
            var root = doc.Root;
            if (root is null) continue;

            // Check if any descendant uses a: or r: namespace
            bool usesA = root.DescendantsAndSelf().Any(e =>
                e.Name.Namespace == nsA ||
                e.Attributes().Any(a => a.Name.Namespace == nsA));
            bool usesR = root.DescendantsAndSelf().Any(e =>
                e.Name.Namespace == nsR ||
                e.Attributes().Any(a =>
                    a.Name.Namespace == nsR ||
                    (a.Name.Namespace == XNamespace.None && a.Name.LocalName.StartsWith("r:")) ||
                    a.Name.Namespace.NamespaceName == nsR.NamespaceName));

            // Also check for r: prefixed attributes (like r:embed, r:id)
            if (!usesR)
                usesR = original.Contains("r:embed") || original.Contains("r:id");

            bool changed = false;

            // Hoist to root if not already there
            if (usesA && root.GetNamespaceOfPrefix("a") is null)
            {
                root.SetAttributeValue(XNamespace.Xmlns + "a", nsA.NamespaceName);
                changed = true;
            }
            if (usesR && root.GetNamespaceOfPrefix("r") is null)
            {
                root.SetAttributeValue(XNamespace.Xmlns + "r", nsR.NamespaceName);
                changed = true;
            }

            // Remove redundant namespace declarations from all descendants.
            // A declaration on a child is redundant if the same prefix is
            // already declared with the same URI on an ancestor.
            foreach (var element in root.Descendants().ToList())
            {
                var redundant = element.Attributes()
                    .Where(a => a.IsNamespaceDeclaration && IsRedundantNsDeclaration(element, a))
                    .ToList();

                foreach (var attr in redundant)
                {
                    attr.Remove();
                    changed = true;
                }
            }

            if (!changed) continue;

            entry.Delete();
            var newEntry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(false));
            writer.Write(doc.Declaration?.ToString() ?? "");
            writer.Write(doc.Root!.ToString());
        }
    }

    /// <summary>
    /// Returns true if the namespace declaration attribute on this element
    /// is redundant — i.e., the same prefix is already declared with the
    /// same namespace URI on a proper ancestor.
    /// </summary>
    private static bool IsRedundantNsDeclaration(XElement element, XAttribute nsAttr)
    {
        // nsAttr.Name is like {xmlns}a or {xmlns}r or just "xmlns" for default ns
        string prefix = nsAttr.Name.LocalName;
        string uri = nsAttr.Value;

        // Walk up ancestors looking for the same declaration
        var ancestor = element.Parent;
        while (ancestor is not null)
        {
            var match = ancestor.Attributes()
                .FirstOrDefault(a => a.IsNamespaceDeclaration && a.Name.LocalName == prefix);
            if (match is not null)
            {
                // Same prefix declared on ancestor — redundant if same URI
                return match.Value == uri;
            }
            ancestor = ancestor.Parent;
        }

        return false;
    }

    // -----------------------------------------------------------------------
    //  XML declaration post-processing
    // -----------------------------------------------------------------------

    /// <summary>
    /// PowerPoint expects XML declarations with encoding="UTF-8" (uppercase)
    /// and standalone="yes". The OpenXml SDK and XDocument write
    /// encoding="utf-8" without standalone. This method rewrites every XML
    /// part in the zip to use PowerPoint's expected declaration format.
    /// Runs last so it catches declarations written by all earlier fix passes.
    /// </summary>
    private static void FixXmlDeclarations(MemoryStream ms)
    {
        const string target = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

        var xmlEntries = zip.Entries
            .Where(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName)
            .ToList();

        foreach (var entryPath in xmlEntries)
        {
            var entry = zip.GetEntry(entryPath);
            if (entry is null) continue;

            string content;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                content = reader.ReadToEnd();

            string updated;
            if (content.StartsWith("<?xml", StringComparison.Ordinal))
            {
                // Replace existing declaration (everything up to and including "?>")
                int end = content.IndexOf("?>", StringComparison.Ordinal);
                if (end < 0) continue;
                updated = target + content[(end + 2)..];
            }
            else
            {
                // No declaration present — prepend one
                updated = target + content;
            }

            if (updated == content) continue;

            entry.Delete();
            var newEntry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(false));
            writer.Write(updated);
        }
    }

    // -----------------------------------------------------------------------
    //  Relationship ID post-processing
    // -----------------------------------------------------------------------

    /// <summary>
    /// The OpenXml SDK generates GUID-based relationship IDs (e.g. "rId3a7f...").
    /// PowerPoint expects sequential IDs like "rId1", "rId2", etc.
    /// This method rewrites all .rels files to use sequential IDs and updates
    /// all corresponding references in XML parts.
    /// Runs before FixXmlDeclarations so declaration normalization covers the rewrites.
    /// </summary>
    private static void FixRelationshipIds(MemoryStream ms)
    {
        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true);

        var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

        // Collect all .rels entries
        var relsEntries = zip.Entries
            .Where(e => e.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName)
            .ToList();

        // Phase 1: Build old→new ID mappings per .rels file, and identify
        //          which XML parts are targeted by each .rels file.
        //          Also collect a global mapping for all rId replacements.
        var globalIdMap = new Dictionary<string, string>(StringComparer.Ordinal);

        // For each .rels, determine the owner directory and process
        foreach (var relsPath in relsEntries)
        {
            var entry = zip.GetEntry(relsPath);
            if (entry is null) continue;

            string content;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                content = reader.ReadToEnd();

            var doc = XDocument.Parse(content);
            var relationships = doc.Root!.Elements(relsNs + "Relationship").ToList();

            // Build sequential mapping for this .rels file
            var localMap = new Dictionary<string, string>(StringComparer.Ordinal);
            int seq = 1;
            foreach (var rel in relationships)
            {
                var idAttr = rel.Attribute("Id");
                if (idAttr is null) continue;

                string oldId = idAttr.Value;
                string newId = $"rId{seq}";
                seq++;

                if (oldId != newId)
                {
                    localMap[oldId] = newId;
                    globalIdMap[oldId] = newId;
                }
            }

            if (localMap.Count == 0) continue;

            // Rewrite the .rels with new IDs
            foreach (var rel in relationships)
            {
                var idAttr = rel.Attribute("Id")!;
                if (localMap.TryGetValue(idAttr.Value, out var newId))
                    idAttr.Value = newId;
            }

            entry.Delete();
            var newEntry = zip.CreateEntry(relsPath, CompressionLevel.Optimal);
            using var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(false));
            writer.Write(doc.Declaration?.ToString() ?? "");
            writer.Write(doc.Root!.ToString());
        }

        if (globalIdMap.Count == 0) return;

        // Phase 2: Update rId references in all XML parts.
        // We do straight string replacement, longest-first to avoid partial matches.
        var sortedReplacements = globalIdMap
            .OrderByDescending(kv => kv.Key.Length)
            .ThenByDescending(kv => kv.Key)
            .ToList();

        var xmlEntries = zip.Entries
            .Where(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                        !e.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName)
            .ToList();

        foreach (var entryPath in xmlEntries)
        {
            var entry = zip.GetEntry(entryPath);
            if (entry is null) continue;

            string content;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                content = reader.ReadToEnd();

            // Check if any old IDs appear in this content
            bool hasAny = false;
            foreach (var kv in sortedReplacements)
            {
                if (content.Contains(kv.Key, StringComparison.Ordinal))
                {
                    hasAny = true;
                    break;
                }
            }
            if (!hasAny) continue;

            // Replace old IDs with new — use attribute-value context to avoid
            // false positives. The IDs appear in patterns like:
            //   r:id="xxx"  r:embed="xxx"  RelationshipId="xxx"  r:link="xxx"
            // We replace within quote-delimited attribute values.
            string updated = content;
            foreach (var kv in sortedReplacements)
            {
                updated = updated.Replace($"\"{kv.Key}\"", $"\"{kv.Value}\"");
            }

            if (updated == content) continue;

            entry.Delete();
            var newEntry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(false));
            writer.Write(updated);
        }
    }

    // -----------------------------------------------------------------------
    //  Presentation skeleton
    // -----------------------------------------------------------------------

    private static Presentation BuildPresentation()
    {
        var pres = new Presentation();
        pres.AppendChild(new SlideMasterIdList());
        pres.AppendChild(new SlideIdList());          // must come before SldSz
        pres.AppendChild(new SlideSize { Cx = SlideWidth, Cy = SlideHeight });
        pres.AppendChild(new NotesSize { Cx = 6858000, Cy = 9144000 });
        pres.AppendChild(BuildDefaultTextStyle());
        return pres;
    }

    // -----------------------------------------------------------------------
    //  Default text style (PowerPoint expects this)
    // -----------------------------------------------------------------------

    private static P.DefaultTextStyle BuildDefaultTextStyle()
    {
        var dts = new P.DefaultTextStyle();
        dts.AppendChild(new A.DefaultParagraphProperties(
            new A.DefaultRunProperties { Language = "en-US" }));

        for (int level = 1; level <= 9; level++)
        {
            var defRPr = new A.DefaultRunProperties(
                new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.Text1 }),
                new A.LatinFont { Typeface = "+mn-lt" },
                new A.EastAsianFont { Typeface = "+mn-ea" },
                new A.ComplexScriptFont { Typeface = "+mn-cs" })
            {
                FontSize = 1800,
                Kerning = 1200
            };

            var pPr = CreateLevelParagraphProperties(level, defRPr);
            dts.AppendChild(pPr);
        }

        return dts;
    }

    private static OpenXmlElement CreateLevelParagraphProperties(int level, A.DefaultRunProperties defRPr)
    {
        int marL = (level - 1) * 457200;
        return level switch
        {
            1 => new A.Level1ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            2 => new A.Level2ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            3 => new A.Level3ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            4 => new A.Level4ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            5 => new A.Level5ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            6 => new A.Level6ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            7 => new A.Level7ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            8 => new A.Level8ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            9 => new A.Level9ParagraphProperties(defRPr)
            { LeftMargin = marL, Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };
    }

    // -----------------------------------------------------------------------
    //  Slide master + layout
    // -----------------------------------------------------------------------

    private static (SlideLayoutPart, SlideMasterPart) AddMasterAndLayout(
        PresentationPart presentationPart)
    {
        var masterPart = presentationPart.AddNewPart<SlideMasterPart>();

        // Theme part — added to PresentationPart, referenced by master
        var themePart = presentationPart.AddNewPart<ThemePart>();
        themePart.Theme = BuildBlankTheme();
        themePart.Theme.Save();
        masterPart.AddPart(themePart);

        masterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(
                new P.Background(
                    new P.BackgroundStyleReference(
                        new A.SchemeColor { Val = A.SchemeColorValues.Background1 })
                    { Index = 1001 }),
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(BuildZeroTransformGroup()))),
            new P.ColorMap
            {
                Background1 = A.ColorSchemeIndexValues.Light1,
                Text1 = A.ColorSchemeIndexValues.Dark1,
                Background2 = A.ColorSchemeIndexValues.Light2,
                Text2 = A.ColorSchemeIndexValues.Dark2,
                Accent1 = A.ColorSchemeIndexValues.Accent1,
                Accent2 = A.ColorSchemeIndexValues.Accent2,
                Accent3 = A.ColorSchemeIndexValues.Accent3,
                Accent4 = A.ColorSchemeIndexValues.Accent4,
                Accent5 = A.ColorSchemeIndexValues.Accent5,
                Accent6 = A.ColorSchemeIndexValues.Accent6,
                Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink
            },
            new SlideLayoutIdList(),
            BuildSlideMasterTextStyles());
        masterPart.SlideMaster.Save();

        var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
        layoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(BuildZeroTransformGroup()))),
            new P.ColorMapOverride(new A.MasterColorMapping()))
        {
            Type = SlideLayoutValues.Blank,
            Preserve = true
        };
        layoutPart.SlideLayout.Save();
        layoutPart.AddPart(masterPart);  // layout → master

        // Register layout in master's SlideLayoutIdList
        var smIdList = masterPart.SlideMaster.GetFirstChild<SlideLayoutIdList>()
            ?? masterPart.SlideMaster.AppendChild(new SlideLayoutIdList());
        smIdList.AppendChild(new SlideLayoutId
        {
            // OOXML spec requires sldLayoutId >= 2147483648 (0x80000000).
            // Values below this minimum trigger PowerPoint's repair prompt.
            Id = 2147483649,
            RelationshipId = masterPart.GetIdOfPart(layoutPart)
        });

        // Register master in presentation's SlideMasterIdList
        var pres = presentationPart.Presentation
            ?? throw new InvalidOperationException("PresentationPart.Presentation is null.");
        var presSmIdList = pres.GetFirstChild<SlideMasterIdList>()
            ?? pres.AppendChild(new SlideMasterIdList());
        presSmIdList.AppendChild(new SlideMasterId
        {
            Id = 2147483648,
            RelationshipId = presentationPart.GetIdOfPart(masterPart)
        });

        return (layoutPart, masterPart);
    }

    // -----------------------------------------------------------------------
    //  Blank theme (required by PowerPoint, content unused)
    // -----------------------------------------------------------------------

    private static A.Theme BuildBlankTheme()
    {
        return new A.Theme(
            new A.ThemeElements(
                new A.ColorScheme(
                    new A.Dark1Color(new A.SystemColor { LastColor = "000000", Val = A.SystemColorValues.WindowText }),
                    new A.Light1Color(new A.SystemColor { LastColor = "FFFFFF", Val = A.SystemColorValues.Window }),
                    new A.Dark2Color(new A.RgbColorModelHex { Val = "1F497D" }),
                    new A.Light2Color(new A.RgbColorModelHex { Val = "EEECE1" }),
                    new A.Accent1Color(new A.RgbColorModelHex { Val = "4F81BD" }),
                    new A.Accent2Color(new A.RgbColorModelHex { Val = "C0504D" }),
                    new A.Accent3Color(new A.RgbColorModelHex { Val = "9BBB59" }),
                    new A.Accent4Color(new A.RgbColorModelHex { Val = "8064A2" }),
                    new A.Accent5Color(new A.RgbColorModelHex { Val = "4BACC6" }),
                    new A.Accent6Color(new A.RgbColorModelHex { Val = "F79646" }),
                    new A.Hyperlink(new A.RgbColorModelHex { Val = "0000FF" }),
                    new A.FollowedHyperlinkColor(new A.RgbColorModelHex { Val = "800080" }))
                { Name = "Backgammon" },
                new A.FontScheme(
                    new A.MajorFont(
                        new A.LatinFont { Typeface = "Calibri" },
                        new A.EastAsianFont { Typeface = "" },
                        new A.ComplexScriptFont { Typeface = "" }),
                    new A.MinorFont(
                        new A.LatinFont { Typeface = "Calibri" },
                        new A.EastAsianFont { Typeface = "" },
                        new A.ComplexScriptFont { Typeface = "" }))
                { Name = "Office" },
                new A.FormatScheme(
                    new A.FillStyleList(
                        new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor }),
                        new A.GradientFill(
                            new A.GradientStopList(
                                new A.GradientStop(new A.SchemeColor(
                                    new A.LuminanceModulation { Val = 110000 },
                                    new A.SaturationModulation { Val = 105000 },
                                    new A.Tint { Val = 67000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 0 },
                                new A.GradientStop(new A.SchemeColor(
                                    new A.LuminanceModulation { Val = 105000 },
                                    new A.SaturationModulation { Val = 103000 },
                                    new A.Tint { Val = 73000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 50000 },
                                new A.GradientStop(new A.SchemeColor(
                                    new A.LuminanceModulation { Val = 105000 },
                                    new A.SaturationModulation { Val = 109000 },
                                    new A.Tint { Val = 81000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 100000 }),
                            new A.LinearGradientFill { Angle = 5400000, Scaled = false })
                        { RotateWithShape = true },
                        new A.GradientFill(
                            new A.GradientStopList(
                                new A.GradientStop(new A.SchemeColor(
                                    new A.SaturationModulation { Val = 103000 },
                                    new A.LuminanceModulation { Val = 102000 },
                                    new A.Tint { Val = 94000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 0 },
                                new A.GradientStop(new A.SchemeColor(
                                    new A.SaturationModulation { Val = 110000 },
                                    new A.LuminanceModulation { Val = 100000 },
                                    new A.Shade { Val = 100000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 50000 },
                                new A.GradientStop(new A.SchemeColor(
                                    new A.LuminanceModulation { Val = 99000 },
                                    new A.SaturationModulation { Val = 120000 },
                                    new A.Shade { Val = 78000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 100000 }),
                            new A.LinearGradientFill { Angle = 5400000, Scaled = false })
                        { RotateWithShape = true }),
                    new A.LineStyleList(
                        new A.Outline(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor }),
                            new A.PresetDash { Val = A.PresetLineDashValues.Solid },
                            new A.Miter { Limit = 800000 })
                        { Width = 12700, CapType = A.LineCapValues.Flat, CompoundLineType = A.CompoundLineValues.Single, Alignment = A.PenAlignmentValues.Center },
                        new A.Outline(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor }),
                            new A.PresetDash { Val = A.PresetLineDashValues.Solid },
                            new A.Miter { Limit = 800000 })
                        { Width = 19050, CapType = A.LineCapValues.Flat, CompoundLineType = A.CompoundLineValues.Single, Alignment = A.PenAlignmentValues.Center },
                        new A.Outline(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor }),
                            new A.PresetDash { Val = A.PresetLineDashValues.Solid },
                            new A.Miter { Limit = 800000 })
                        { Width = 25400, CapType = A.LineCapValues.Flat, CompoundLineType = A.CompoundLineValues.Single, Alignment = A.PenAlignmentValues.Center }),
                    new A.EffectStyleList(
                        new A.EffectStyle(new A.EffectList()),
                        new A.EffectStyle(new A.EffectList()),
                        new A.EffectStyle(new A.EffectList())),
                    new A.BackgroundFillStyleList(
                        new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor }),
                        new A.SolidFill(new A.SchemeColor(
                            new A.Tint { Val = 95000 },
                            new A.SaturationModulation { Val = 170000 })
                        { Val = A.SchemeColorValues.PhColor }),
                        new A.GradientFill(
                            new A.GradientStopList(
                                new A.GradientStop(new A.SchemeColor(
                                    new A.Tint { Val = 93000 },
                                    new A.SaturationModulation { Val = 150000 },
                                    new A.Shade { Val = 98000 },
                                    new A.LuminanceModulation { Val = 102000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 0 },
                                new A.GradientStop(new A.SchemeColor(
                                    new A.Tint { Val = 98000 },
                                    new A.SaturationModulation { Val = 130000 },
                                    new A.Shade { Val = 90000 },
                                    new A.LuminanceModulation { Val = 103000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 50000 },
                                new A.GradientStop(new A.SchemeColor(
                                    new A.Shade { Val = 63000 },
                                    new A.SaturationModulation { Val = 120000 })
                                { Val = A.SchemeColorValues.PhColor })
                                { Position = 100000 }),
                            new A.LinearGradientFill { Angle = 5400000, Scaled = false })
                        { RotateWithShape = true }))
                { Name = "Office" }),
            new A.ObjectDefaults(),
            new A.ExtraColorSchemeList())
        { Name = "Backgammon" };
    }

    // -----------------------------------------------------------------------
    //  Slide content
    // -----------------------------------------------------------------------

    private static Slide BuildSlide(string imageRId, byte[] png)
    {
        long availW = SlideWidth - MarginH * 2;
        long availH = SlideHeight - MarginV * 2;

        var (pngW, pngH) = ReadPngDimensions(png);
        double aspect = pngW / (double)pngH;

        long imgW, imgH;
        if (availW / (double)availH >= aspect)
        {
            imgH = availH;
            imgW = (long)(availH * aspect);
        }
        else
        {
            imgW = availW;
            imgH = (long)(availW / aspect);
        }

        long imgX = MarginH + (availW - imgW) / 2;
        long imgY = MarginV;

        var tree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(BuildZeroTransformGroup()),
            BuildPicture(imageRId, imgX, imgY, imgW, imgH));

        return new Slide(
            new CommonSlideData(tree),
            new P.ColorMapOverride(new A.MasterColorMapping()));
    }

    // -----------------------------------------------------------------------
    //  Picture element
    // -----------------------------------------------------------------------

    private static P.Picture BuildPicture(string rId, long x, long y, long cx, long cy)
    {
        return new P.Picture(
            new P.NonVisualPictureProperties(
                new P.NonVisualDrawingProperties { Id = 2, Name = "Diagram" },
                new P.NonVisualPictureDrawingProperties(
                    new A.PictureLocks { NoChangeAspect = true }),
                new ApplicationNonVisualDrawingProperties()),
            new P.BlipFill(
                new A.Blip { Embed = rId },
                new A.Stretch(new A.FillRectangle())),
            new P.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = x, Y = y },
                    new A.Extents { Cx = cx, Cy = cy }),
                new A.PresetGeometry(new A.AdjustValueList())
                { Preset = A.ShapeTypeValues.Rectangle }));
    }
   
    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a TransformGroup with explicit zero offsets and extents.
    /// PowerPoint requires these child elements rather than an empty xfrm.
    /// </summary>
    private static A.TransformGroup BuildZeroTransformGroup()
    {
        return new A.TransformGroup(
            new A.Offset { X = 0, Y = 0 },
            new A.Extents { Cx = 0, Cy = 0 },
            new A.ChildOffset { X = 0, Y = 0 },
            new A.ChildExtents { Cx = 0, Cy = 0 });
    }

    /// <summary>
    /// Builds the TextStyles element required by the slide master.
    /// Contains titleStyle, bodyStyle, and otherStyle.
    /// We provide a minimal otherStyle; title and body are empty placeholders.
    /// </summary>
    private static P.TextStyles BuildSlideMasterTextStyles()
    {
        return new P.TextStyles(
            new P.TitleStyle(),
            new P.BodyStyle(),
            new P.OtherStyle(
                new A.DefaultParagraphProperties(
                    new A.DefaultRunProperties { Language = "en-US" }),
                new A.Level1ParagraphProperties(
                    new A.DefaultRunProperties(
                        new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.Text1 }),
                        new A.LatinFont { Typeface = "+mn-lt" },
                        new A.EastAsianFont { Typeface = "+mn-ea" },
                        new A.ComplexScriptFont { Typeface = "+mn-cs" })
                    { FontSize = 1800, Kerning = 1200 })
                {
                    LeftMargin = 0,
                    Alignment = A.TextAlignmentTypeValues.Left,
                    DefaultTabSize = 914400,
                    RightToLeft = false,
                    EastAsianLineBreak = true,
                    LatinLineBreak = false,
                    Height = true
                }));
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] png)
    {
        if (png.Length < 24) return (960, 540);
        int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return (w, h);
    }

    private static string NewId() => "rId" + Guid.NewGuid().ToString("N")[..8];
}