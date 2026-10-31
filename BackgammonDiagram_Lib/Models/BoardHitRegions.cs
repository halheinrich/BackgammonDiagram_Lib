using System.Collections.Frozen;

namespace BackgammonDiagram_Lib;

/// <summary>
/// Axis-aligned hit rectangles for all clickable board regions and the play
/// panel's candidate rows. All coordinates are in SVG viewBox space matching RenderSvg() output.
/// </summary>
public class BoardHitRegions
{
    private readonly FrozenDictionary<int, HitRect> _points = FrozenDictionary<int, HitRect>.Empty;
    private readonly FrozenDictionary<int, HitRect> _candidates = FrozenDictionary<int, HitRect>.Empty;

    /// <summary>The rendered diagram's viewBox — the coordinate space every
    /// rectangle below is expressed in.</summary>
    public required SvgViewBox ViewBox { get; init; }

    /// <summary>Hit rectangle per board point, keyed by point number 1–24 in
    /// the on-roll player's numbering (unaffected by <c>HomeBoardOnRight</c>,
    /// which mirrors geometry only). An immutable copy, taken on init: no
    /// caller can change it, and a change to the dictionary it was built from
    /// does not reach it.</summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    public required IReadOnlyDictionary<int, HitRect> Points
    {
        get => _points;
        init => _points = (value ?? throw new ArgumentNullException(nameof(Points))).ToFrozenDictionary();
    }

    /// <summary>Hit rectangle for the bar.</summary>
    public required HitRect Bar { get; init; }

    /// <summary>Hit rectangle for the doubling cube: the whole left-rail
    /// column, every place the cube can sit. Always set by
    /// <c>DiagramRenderer.GetHitRegions</c>, since a cube is always drawn. The
    /// bear-off trays lie inside it, so a host hit-testing both decides which
    /// wins.</summary>
    public HitRect? Cube { get; init; }

    /// <summary>Hit rectangle for the on-roll player's bear-off tray, or
    /// <c>null</c> when it is not drawn.</summary>
    public HitRect? OnRollTray { get; init; }

    /// <summary>Hit rectangle for the opponent's bear-off tray, or <c>null</c>
    /// when it is not drawn.</summary>
    public HitRect? OpponentTray { get; init; }

    /// <summary>
    /// Bounding rectangle over the two dice, or <c>null</c> when no dice are
    /// drawn — a cube decision's diagram, or a board whose display facts show
    /// none. One rect covers the whole pair: a click anywhere on
    /// the dice is a single action, leaving the submit-vs-swap choice to the
    /// consumer. Single-sourced with the dice renderer so the hit region tracks
    /// the drawn dice exactly.
    /// </summary>
    public HitRect? Dice { get; init; }

    /// <summary>Hit rectangle per play-panel row, keyed by the candidate's
    /// stored index into the decision's <c>Plays</c> — never its row
    /// position — so a click names the candidate under either ranking, the
    /// analysis-level ceiling and a rescue. Present exactly for the rows the
    /// panel draws: empty unless a Solution-mode checker play's panel is
    /// drawn. Each rect spans the panel's width and the row's height. An
    /// immutable copy, taken on init, as <see cref="Points"/> is.</summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    public required IReadOnlyDictionary<int, HitRect> Candidates
    {
        get => _candidates;
        init => _candidates = (value ?? throw new ArgumentNullException(nameof(Candidates))).ToFrozenDictionary();
    }
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