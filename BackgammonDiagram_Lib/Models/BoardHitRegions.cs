namespace BackgammonDiagram_Lib;

/// <summary>
/// Axis-aligned hit rectangles for all clickable board regions.
/// All coordinates are in SVG viewBox space matching RenderSvg() output.
/// </summary>
public class BoardHitRegions
{
    public required SvgViewBox ViewBox { get; init; }
    public required IReadOnlyDictionary<int, HitRect> Points { get; init; }  // 1–24
    public required HitRect Bar { get; init; }
    public HitRect? Cube { get; init; }
    public HitRect? OnRollTray { get; init; }
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

public record SvgViewBox(double X, double Y, double Width, double Height);
public record HitRect(double X, double Y, double Width, double Height);