namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// Converts a hand-rolled SVG string to a rasterized PNG byte array.
/// Abstracted so the SkiaSharp implementation can be replaced without
/// touching DiagramRenderer.
/// </summary>
public interface ISvgRasterizer
{
    byte[] Rasterize(string svgContent, int targetWidth);
}