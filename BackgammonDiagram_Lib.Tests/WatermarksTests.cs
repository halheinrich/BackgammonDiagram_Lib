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
    public void Default_ReturnsNonEmptyJpgBytes()
    {
        var bytes = Watermarks.Default;
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, $"Watermark too small: {bytes.Length} bytes");
        // JPEG magic bytes: FF D8.
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xD8, bytes[1]);
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
