using System.Security.Cryptography;
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

    // Exact byte content of the pre-baked watermark PNG, captured from the
    // original SkiaSharp transform output at the time the asset was baked
    // (see Watermarks.Default xmldoc). This pins the *single source of truth*:
    // because RenderSvg embeds these bytes as base64, an identical hash here
    // guarantees byte-identical SVG watermark output. If the embedded asset is
    // ever regenerated, this hash must be updated deliberately — an accidental
    // change (re-encode, re-tune, wrong file) fails the build instead of
    // silently shifting every rendered diagram.
    private const string ExpectedSha256 =
        "E3B7E458CB3C9C0589F525F497EF10CE64028B89B992D3A2C843B2522B30EA6C";
    private const int ExpectedLength = 183289;

    [Fact]
    public void Default_MatchesPreBakedBytes()
    {
        var bytes = Watermarks.Default;
        Assert.Equal(ExpectedLength, bytes.Length);
        Assert.Equal(ExpectedSha256, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}
