namespace BackgammonDiagram_Lib.Themes;

/// <summary>
/// A caller-supplied colour palette. All seven colours are required.
/// Each value must be a valid CSS hex colour: #RGB or #RRGGBB.
/// </summary>
public partial record CustomTheme : ITheme
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")]
    private static partial System.Text.RegularExpressions.Regex HexColourRegex();

    public string Name { get; }
    public string BoardColor { get; }
    public string PointColorDark { get; }
    public string PointColorLight { get; }
    public string CheckerColorOnRoll { get; }
    public string CheckerColorOpponent { get; }
    public string DiceColor { get; }
    public string TextColor { get; }
    public string PanelBackgroundColor { get; }

    public CustomTheme(
        string boardColor,
        string pointColorDark,
        string pointColorLight,
        string checkerColorOnRoll,
        string checkerColorOpponent,
        string diceColor,
        string textColor,
        string panelBackgroundColor = "#FFFFFF",
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