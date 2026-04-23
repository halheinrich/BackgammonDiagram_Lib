using SkiaSharp;

namespace BackgammonDiagram_Lib;

/// <summary>
/// Built-in watermark image bytes for use with
/// <see cref="DiagramOptions.WatermarkImage"/>. Enables watermarking with a
/// one-liner — no filesystem code on the caller side.
/// </summary>
public static class Watermarks
{
    private static readonly byte[] _default = BuildTransparentPng(LoadEmbedded("Assets.board-watermark.jpg"));

    /// <summary>
    /// Project default board watermark. Loaded once from the embedded JPG at
    /// class init, processed to swap the light JPEG background for real alpha
    /// transparency (dark pixels opaque, light pixels transparent), and
    /// re-encoded as PNG. Cached thereafter — every call returns the same
    /// array. Callers must treat the returned byte[] as immutable; mutating
    /// it corrupts the cached copy for every subsequent render.
    /// </summary>
    public static byte[] Default => _default;

    private static byte[] LoadEmbedded(string relativeName)
    {
        var asm = typeof(Watermarks).Assembly;
        var fullName = $"{typeof(Watermarks).Namespace}.{relativeName}";
        using var stream = asm.GetManifestResourceStream(fullName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {fullName}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Convert the JPG's light background to real alpha. The asset is a
    /// black-on-near-white logo; JPEG can't carry transparency so the
    /// background bleeds through at whatever SVG opacity the caller uses.
    /// Any pixel above <c>BackgroundLumaThreshold</c> becomes fully
    /// transparent (kills JPEG compression noise in the background);
    /// darker pixels get a linear alpha ramp from 0 at the threshold to
    /// 255 at pure black, which preserves anti-aliased edges. RGB is
    /// forced to pure black so partially-opaque edge pixels composite as
    /// dark grey rather than inheriting JPEG-compressed colour noise.
    /// </summary>
    private static byte[] BuildTransparentPng(byte[] jpgBytes)
    {
        // Threshold tuned for the shipped asset: the true background is
        // at luma ~245+, JPEG artefacts creep down to ~230 in places, and
        // the silhouette interior stays well below 100. Setting the
        // threshold to 200 kills the rectangular halo without clipping
        // any of the logo's grey edge pixels.
        const int BackgroundLumaThreshold = 200;

        using var bitmap = SKBitmap.Decode(jpgBytes)
            ?? throw new InvalidOperationException("Failed to decode watermark JPG.");
        using var output = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var src = bitmap.GetPixel(x, y);
                // ITU-R BT.601 luminance — close enough for a B/W logo.
                int luma = (src.Red * 299 + src.Green * 587 + src.Blue * 114) / 1000;
                int alpha = luma >= BackgroundLumaThreshold
                    ? 0
                    : (BackgroundLumaThreshold - luma) * 255 / BackgroundLumaThreshold;
                output.SetPixel(x, y, new SKColor(0, 0, 0, (byte)alpha));
            }
        }

        using var image = SKImage.FromBitmap(output);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
