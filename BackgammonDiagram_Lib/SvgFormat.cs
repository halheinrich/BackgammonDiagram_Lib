using System.Globalization;

namespace BackgammonDiagram_Lib;

/// <summary>
/// Single source of truth for formatting numbers into SVG attribute values.
/// </summary>
/// <remarks>
/// SVG attribute grammar requires a <c>.</c> decimal separator regardless of
/// locale. Formatting a <see cref="double"/> with the thread's current culture
/// (e.g. via string interpolation) emits <c>","</c> in comma-decimal locales
/// such as <c>nb-NO</c>, producing invalid attribute values that browsers
/// parse as 0. Every number written into SVG markup — by this library or by
/// consumers assembling their own SVG fragments (hit-region overlays, custom
/// annotations) — must go through this formatter.
/// </remarks>
public static class SvgFormat
{
    /// <summary>
    /// Formats <paramref name="value"/> for use in an SVG attribute:
    /// invariant culture (always a <c>.</c> decimal separator, independent of
    /// thread culture), at most two fractional digits, trailing zeros
    /// trimmed (format <c>"0.##"</c> — e.g. <c>30.8</c> → <c>"30.8"</c>,
    /// <c>14.0</c> → <c>"14"</c>). This is the exact formatting
    /// <c>DiagramRenderer.RenderSvg</c> uses for all board geometry.
    /// </summary>
    /// <param name="value">The number to format. Must be finite.</param>
    /// <returns>The invariant string representation of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> is <see cref="double.NaN"/> or infinite —
    /// non-finite values have no valid SVG attribute representation.
    /// </exception>
    public static string Number(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "SVG attribute numbers must be finite; NaN and infinities have no valid SVG representation.");
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
