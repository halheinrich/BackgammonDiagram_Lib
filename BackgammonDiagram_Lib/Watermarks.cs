namespace BackgammonDiagram_Lib;

/// <summary>
/// Built-in watermark image bytes for use with
/// <see cref="DiagramOptions.WatermarkImage"/>. Enables watermarking with a
/// one-liner — no filesystem code on the caller side.
/// </summary>
public static class Watermarks
{
    private static readonly byte[] _default = LoadEmbedded("Assets.board-watermark.png");

    /// <summary>
    /// Project default board watermark — a transparent PNG whose dark logo
    /// silhouette carries per-pixel alpha, so the renderer composites it onto
    /// the board colour without a light background wash. Loaded once from the
    /// embedded resource at class init and cached thereafter; every call
    /// returns the same array.
    /// <para>
    /// The asset is <b>pre-baked</b> and is the single source of truth: it was
    /// produced once from the original <c>board-watermark.jpg</c> by a SkiaSharp
    /// luma-threshold transform (background luma ≥ 200 → fully transparent,
    /// darker pixels a linear alpha ramp to pure black). That transform pulled a
    /// native dependency into this otherwise pure-managed assembly, so it was
    /// removed; the JPG and the transform remain recoverable from git history if
    /// the silhouette ever needs regenerating.
    /// </para>
    /// Callers must treat the returned <see cref="byte"/> array as immutable;
    /// mutating it corrupts the cached copy for every subsequent render.
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
}
