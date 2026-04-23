using BackgammonDiagram_Lib;
using Xunit;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// Exercises the built-in watermark asset accessor. The asset itself is
/// shipped as an <c>EmbeddedResource</c>; this test pins that the logical
/// resource name is correct (so a rename of <c>Assets\board-watermark.jpg</c>
/// shows up here) and that the accessor caches the array across calls.
/// </summary>
public class WatermarksTests
{
    [Fact]
    public void Default_ReturnsNonEmptyPngBytes()
    {
        var bytes = Watermarks.Default;
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, $"Watermark too small: {bytes.Length} bytes");
        // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A.
        // (The source asset is a JPG, but Watermarks post-processes it into
        // a PNG with alpha so the renderer can composite the black silhouette
        // onto the board colour without a light background wash.)
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    [Fact]
    public void Default_IsCachedAcrossCalls()
    {
        // Same reference on every call — the static initializer loads once.
        // Prevents a future refactor from re-reading the embedded resource
        // on every render, which would measurably slow down large decks.
        var a = Watermarks.Default;
        var b = Watermarks.Default;
        Assert.Same(a, b);
    }
}
