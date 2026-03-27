using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// Builds a .pptx byte array from one or more PNG images.
/// Each PNG becomes one slide. An optional per-slide title string
/// is rendered as a text box below the image.
/// Internal — called only by DiagramRenderer.
/// </summary>
internal static class PptxBuilder
{
    // Slide canvas: 13.33" × 7.5" at 914400 EMUs/inch
    private const long SlideWidth = 12192000L;
    private const long SlideHeight = 6858000L;

    // Margins (EMUs)
    private const long MarginH = 457200L;   // 0.5"
    private const long MarginV = 457200L;   // 0.5"
    private const long TitleH = 400000L;   // reserved at bottom when title present
    private const long TitleGap = 91440L;  // gap between image and title

    public static byte[] Build(IEnumerable<(byte[] Png, string? Title)> slides)
    {
        using var ms = new MemoryStream();
        using (var doc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation))
        {
            var presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = BuildPresentation();

            var slideIdList = new SlideIdList();
            presentationPart.Presentation.AppendChild(slideIdList);

            var slideLayoutPart = AddSlideLayoutPart(presentationPart);

            uint slideId = 256;
            foreach (var (png, title) in slides)
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                slideLayoutPart.AddPart(slidePart);   // back-reference so layout resolves
                slidePart.AddPart(slideLayoutPart);

                var imagePart = slidePart.AddNewPart<ImagePart>("image/png", NewId());
                using (var imgStream = new MemoryStream(png))
                    imagePart.FeedData(imgStream);

                string rId = slidePart.GetIdOfPart(imagePart);
                slidePart.Slide = BuildSlide(rId, png, title);
                slidePart.Slide.Save();

                var rid = presentationPart.GetIdOfPart(slidePart);
                slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = rid });
            }

            presentationPart.Presentation.Save();
        }

        return ms.ToArray();
    }

    // -----------------------------------------------------------------------
    //  Presentation skeleton
    // -----------------------------------------------------------------------

    private static Presentation BuildPresentation()
    {
        var pres = new Presentation();
        pres.AppendChild(new SlideMasterIdList());
        pres.AppendChild(new SlideSize { Cx = (int)SlideWidth, Cy = (int)SlideHeight });
        pres.AppendChild(new NotesSize { Cx = 6858000L, Cy = 9144000L });
        return pres;
    }

    // -----------------------------------------------------------------------
    //  Minimal slide layout / slide master (blank)
    // -----------------------------------------------------------------------

    private static SlideLayoutPart AddSlideLayoutPart(PresentationPart presentationPart)
    {
        var masterPart = presentationPart.AddNewPart<SlideMasterPart>();
        masterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()))),
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
            new SlideLayoutIdList());
        masterPart.SlideMaster.Save();

        var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
        layoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()))));
        layoutPart.SlideLayout.Save();
        layoutPart.AddPart(masterPart);

        var smIdList = masterPart.SlideMaster.GetFirstChild<SlideLayoutIdList>() ?? masterPart.SlideMaster.AppendChild(new SlideLayoutIdList());
        smIdList.AppendChild(new SlideLayoutId
        {
            Id = 2199,
            RelationshipId = masterPart.GetIdOfPart(layoutPart)
        });

        var pres = presentationPart.Presentation
            ?? throw new InvalidOperationException("PresentationPart.Presentation is null.");
        var presSmIdList = pres.GetFirstChild<SlideMasterIdList>()
            ?? pres.AppendChild(new SlideMasterIdList());
        presSmIdList.AppendChild(new SlideMasterId
        {
            Id = 2147483648,
            RelationshipId = presentationPart.GetIdOfPart(masterPart)
        });

        return layoutPart;
    }

    // -----------------------------------------------------------------------
    //  Slide content
    // -----------------------------------------------------------------------

    private static Slide BuildSlide(string imageRId, byte[] png, string? title)
    {
        bool hasTitle = !string.IsNullOrWhiteSpace(title);

        // Image area — fill available space minus title row at bottom
        long availW = SlideWidth - MarginH * 2;
        long availH = SlideHeight - MarginV * 2 - (hasTitle ? TitleH + TitleGap : 0);

        // Preserve PNG aspect ratio
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

        // Centre image horizontally; top-align vertically within the image zone
        long imgX = MarginH + (availW - imgW) / 2;
        long imgY = MarginV;

        var tree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup()),
            BuildPicture(imageRId, imgX, imgY, imgW, imgH));

        if (hasTitle)
        {
            long titleY = imgY + imgH + TitleGap;
            tree.AppendChild(BuildTitleBox(title!, MarginH, titleY, availW, TitleH));
        }

        return new Slide(new CommonSlideData(tree));
    }

    // -----------------------------------------------------------------------
    //  Picture element
    // -----------------------------------------------------------------------

    private static P.Picture BuildPicture(string rId, long x, long y, long cx, long cy)
    {
        uint picId = 2;
        return new P.Picture(
            new P.NonVisualPictureProperties(
                new P.NonVisualDrawingProperties { Id = picId, Name = "Diagram" },
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
    //  Title text box
    // -----------------------------------------------------------------------

    private static P.Shape BuildTitleBox(string text, long x, long y, long cx, long cy)
    {
        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = 3, Name = "Title" },
                new P.NonVisualShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = x, Y = y },
                    new A.Extents { Cx = cx, Cy = cy }),
                new A.PresetGeometry(new A.AdjustValueList())
                { Preset = A.ShapeTypeValues.Rectangle }),
            new P.TextBody(
                new A.BodyProperties(),
                new A.ListStyle(),
                new A.Paragraph(
                    new A.ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Center },
                    new A.Run(
                        new A.RunProperties
                        {
                            Language = "en-US",
                            FontSize = 1800,     // 18pt in hundredths of a point
                            Bold = false
                        },
                        new A.Text(text)))));
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>Reads PNG image dimensions from the IHDR chunk without any external dependency.</summary>
    private static (int Width, int Height) ReadPngDimensions(byte[] png)
    {
        // PNG signature = 8 bytes; IHDR chunk: 4 len + 4 type + 4 w + 4 h + ...
        if (png.Length < 24) return (960, 540);
        int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return (w, h);
    }

    private static string NewId() => "rId" + Guid.NewGuid().ToString("N")[..8];
}