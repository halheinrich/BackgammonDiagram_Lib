namespace BackgammonDiagram_Lib.Rendering;

public class DiagramRenderer
{
    /// <summary>Renders the diagram as an SVG string.</summary>
    public string RenderSvg(DiagramRequest request, DiagramOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>Renders the diagram as a PNG byte array.</summary>
    public byte[] RenderPng(DiagramRequest request, DiagramOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>Renders a single diagram as a PDF byte array.</summary>
    public byte[] RenderPdf(DiagramRequest request, DiagramOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>Renders multiple diagrams into a single PDF byte array.</summary>
    public byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>Renders a single diagram as a PowerPoint (.pptx) byte array.</summary>
    public byte[] RenderPptx(DiagramRequest request, DiagramOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>Renders multiple diagrams into a single PowerPoint (.pptx) byte array.</summary>
    public byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options)
    {
        throw new NotImplementedException();
    }
}
