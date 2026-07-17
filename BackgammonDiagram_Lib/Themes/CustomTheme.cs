namespace BackgammonDiagram_Lib.Themes;

/// <summary>
/// A caller-supplied colour palette. All eight colours are required.
/// Each value must be a valid CSS hex colour: #RGB or #RRGGBB.
/// </summary>
public partial record CustomTheme : ITheme
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")]
    private static partial System.Text.RegularExpressions.Regex HexColourRegex();

    /// <inheritdoc/>
    public string Name { get; }
    /// <inheritdoc/>
    public string BoardColor { get; }
    /// <inheritdoc/>
    public string PointColorDark { get; }
    /// <inheritdoc/>
    public string PointColorLight { get; }
    /// <inheritdoc/>
    public string CheckerColorOnRoll { get; }
    /// <inheritdoc/>
    public string CheckerColorOpponent { get; }
    /// <inheritdoc/>
    public string DiceColor { get; }
    /// <inheritdoc/>
    public string TextColor { get; }
    /// <inheritdoc/>
    public string PanelBackgroundColor { get; }

    /// <summary>
    /// Creates a theme from eight explicit colours. Each must be a valid CSS
    /// hex colour (<c>#RGB</c> or <c>#RRGGBB</c>); an invalid value throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="boardColor">Board background fill.</param>
    /// <param name="pointColorDark">Dark (alternating) point fill.</param>
    /// <param name="pointColorLight">Light (alternating) point fill.</param>
    /// <param name="checkerColorOnRoll">On-roll player's checker fill.</param>
    /// <param name="checkerColorOpponent">Opponent's checker fill.</param>
    /// <param name="diceColor">Dice fill.</param>
    /// <param name="textColor">Rendered-text colour.</param>
    /// <param name="panelBackgroundColor">Analysis-panel background fill.</param>
    /// <param name="name">Display name; defaults to <c>"Custom"</c>.</param>
    /// <exception cref="ArgumentException">A colour is not <c>#RGB</c> or
    /// <c>#RRGGBB</c>.</exception>
    public CustomTheme(
        string boardColor,
        string pointColorDark,
        string pointColorLight,
        string checkerColorOnRoll,
        string checkerColorOpponent,
        string diceColor,
        string textColor,
        string panelBackgroundColor,
        string name = "Custom")
    {
        Name = name;
        BoardColor = Validate(boardColor, nameof(boardColor));
        PointColorDark = Validate(pointColorDark, nameof(pointColorDark));
        PointColorLight = Validate(pointColorLight, nameof(pointColorLight));
        CheckerColorOnRoll = Validate(checkerColorOnRoll, nameof(checkerColorOnRoll));
        CheckerColorOpponent = Validate(checkerColorOpponent, nameof(checkerColorOpponent));
        DiceColor = Validate(diceColor, nameof(diceColor));
        TextColor = Validate(textColor, nameof(textColor));
        PanelBackgroundColor = Validate(panelBackgroundColor, nameof(panelBackgroundColor));
    }

    private static string Validate(string value, string paramName)
    {
        if (!HexColourRegex().IsMatch(value))
            throw new ArgumentException(
                $"Invalid hex colour '{value}'. Expected #RGB or #RRGGBB.", paramName);
        return value;
    }
}