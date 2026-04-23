namespace BackgammonDiagram_Lib;

/// <summary>
/// Built-in watermark image bytes for use with
/// <see cref="DiagramOptions.WatermarkImage"/>. Enables watermarking with a
/// one-liner — no filesystem code on the caller side.
/// </summary>
public static class Watermarks
{
    private static readonly byte[] _default = LoadEmbedded("Assets.board-watermark.jpg");

    /// <summary>
    /// Project default board watermark (JPG). Loaded once from the embedded
    /// resource and cached thereafter — every call returns the same array.
    /// Callers must treat the returned byte[] as immutable; mutating it
    /// corrupts the cached copy for every subsequent render.
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
