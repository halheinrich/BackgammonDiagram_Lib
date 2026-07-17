namespace BackgammonDiagram_Lib.Themes;

/// <summary>
/// The colour palette the renderer consumes. Every colour is a CSS colour
/// string emitted verbatim into the SVG. <see cref="DiagramOptions.Theme"/>
/// holds a direct <see cref="ITheme"/> reference — there is no string-based
/// lookup — so a caller supplies a built-in via <see cref="ThemeRegistry"/> or
/// its own implementation (see <see cref="CustomTheme"/>).
/// </summary>
public interface ITheme
{
    /// <summary>Display name of the theme.</summary>
    string Name { get; }
    /// <summary>Fill colour of the board background.</summary>
    string BoardColor { get; }
    /// <summary>Fill colour of the dark (alternating) points.</summary>
    string PointColorDark { get; }
    /// <summary>Fill colour of the light (alternating) points.</summary>
    string PointColorLight { get; }
    /// <summary>Fill colour of the on-roll player's checkers.</summary>
    string CheckerColorOnRoll { get; }
    /// <summary>Fill colour of the opponent's checkers.</summary>
    string CheckerColorOpponent { get; }
    /// <summary>Fill colour of the dice.</summary>
    string DiceColor { get; }
    /// <summary>Colour of rendered text (titles, labels, rail, panel).</summary>
    string TextColor { get; }
    /// <summary>Background fill of the analysis panel.</summary>
    string PanelBackgroundColor { get; }
}
