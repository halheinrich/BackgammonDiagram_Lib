namespace BackgammonDiagram_Lib.ExportRaster;

/// <summary>
/// Converts a hand-rolled SVG string to a rasterized PNG byte array.
/// Abstracted so the SkiaSharp implementation can be replaced without
/// touching DiagramRasterRenderer.
/// </summary>
public interface ISvgRasterizer
{
    /// <summary>
    /// Rasterizes <paramref name="svgContent"/> to a PNG at the given pixel
    /// width; the height follows from the SVG's aspect ratio.
    /// </summary>
    /// <param name="svgContent">The SVG document to rasterize.</param>
    /// <param name="targetWidth">Output width in pixels.</param>
    /// <returns>The rendered PNG as a byte array.</returns>
    byte[] Rasterize(string svgContent, int targetWidth);
}