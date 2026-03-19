using SkiaSharp;
using System.Text;

namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// Rasterizes SVG to PNG using SkiaSharp + Svg.Skia.
/// All SkiaSharp dependencies are contained here.
/// </summary>
public class SkiaSharpRasterizer : ISvgRasterizer
{
    public byte[] Rasterize(string svgContent, int targetWidth)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
        var svg = new Svg.Skia.SKSvg();
        svg.Load(stream);

        if (svg.Drawable is null)
            throw new InvalidOperationException("Svg.Skia could not parse the SVG.");

        var (vbWidth, vbHeight) = ParseViewBox(svgContent);
        float scale = targetWidth / vbWidth;
        int h = (int)Math.Ceiling(vbHeight * scale);

        // Record with clip to viewBox bounds — prevents Drawable.Bounds overflow
        var cullRect = new SKRect(0, 0, vbWidth, vbHeight);
        using var recorder = new SKPictureRecorder();
        var recCanvas = recorder.BeginRecording(cullRect);
        recCanvas.ClipRect(cullRect);
        svg.Draw(recCanvas);
        using var picture = recorder.EndRecording();

        var imageInfo = new SKImageInfo(targetWidth, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static (float Width, float Height) ParseViewBox(string svgContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            svgContent,
            @"viewBox=""[\d.]+ [\d.]+ ([\d.]+) ([\d.]+)""");
        if (match.Success)
            return (
                float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
        throw new InvalidOperationException("Could not parse viewBox from SVG.");
    }
}