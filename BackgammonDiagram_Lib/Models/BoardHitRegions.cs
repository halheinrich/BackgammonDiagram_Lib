namespace BackgammonDiagram_Lib;

/// <summary>
/// Axis-aligned hit rectangles for all clickable board regions.
/// All coordinates are in SVG viewBox space matching RenderSvg() output.
/// </summary>
public class BoardHitRegions
{
    /// <summary>The rendered diagram's viewBox — the coordinate space every
    /// rectangle below is expressed in.</summary>
    public required SvgViewBox ViewBox { get; init; }

    /// <summary>Hit rectangle per board point, keyed by point number 1–24 in
    /// the on-roll player's numbering (unaffected by <c>HomeBoardOnRight</c>,
    /// which mirrors geometry only).</summary>
    public required IReadOnlyDictionary<int, HitRect> Points { get; init; }

    /// <summary>Hit rectangle for the bar.</summary>
    public required HitRect Bar { get; init; }

    /// <summary>Hit rectangle for the doubling cube, or <c>null</c> when no cube
    /// indicator is drawn.</summary>
    public HitRect? Cube { get; init; }

    /// <summary>Hit rectangle for the on-roll player's bear-off tray, or
    /// <c>null</c> when it is not drawn.</summary>
    public HitRect? OnRollTray { get; init; }

    /// <summary>Hit rectangle for the opponent's bear-off tray, or <c>null</c>
    /// when it is not drawn.</summary>
    public HitRect? OpponentTray { get; init; }

    /// <summary>
    /// Bounding rectangle over the two dice, or <c>null</c> for cube decisions
    /// (no dice are drawn). One rect covers the whole pair: a click anywhere on
    /// the dice is a single action, leaving the submit-vs-swap choice to the
    /// consumer. Single-sourced with the dice renderer so the hit region tracks
    /// the drawn dice exactly.
    /// </summary>
    public HitRect? Dice { get; init; }
}

/// <summary>
/// An SVG <c>viewBox</c> — the origin and extent of the coordinate space the
/// diagram is drawn in.
/// </summary>
/// <param name="X">Left edge (minimum x) of the viewBox.</param>
/// <param name="Y">Top edge (minimum y) of the viewBox.</param>
/// <param name="Width">Width of the viewBox.</param>
/// <param name="Height">Height of the viewBox.</param>
public record SvgViewBox(double X, double Y, double Width, double Height)
{
    /// <summary>
    /// Formats this viewBox as a valid SVG <c>viewBox</c> attribute value
    /// (<c>"minX minY width height"</c>), culture-invariant via
    /// <see cref="SvgFormat.Number"/> — identical to the <c>viewBox</c> that
    /// <c>DiagramRenderer.RenderSvg</c> emits for the same dimensions.
    /// Consumers must use this rather than interpolating the components
    /// themselves: interpolation formats with the thread culture, which emits
    /// invalid comma decimals in locales such as <c>nb-NO</c>.
    /// </summary>
    public string ToAttributeString() =>
        $"{SvgFormat.Number(X)} {SvgFormat.Number(Y)} {SvgFormat.Number(Width)} {SvgFormat.Number(Height)}";
}

/// <summary>An axis-aligned hit rectangle in the diagram's viewBox coordinate
/// space.</summary>
/// <param name="X">Left edge (minimum x) of the rectangle.</param>
/// <param name="Y">Top edge (minimum y) of the rectangle.</param>
/// <param name="Width">Width of the rectangle.</param>
/// <param name="Height">Height of the rectangle.</param>
public record HitRect(double X, double Y, double Width, double Height);