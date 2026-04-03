namespace BackgammonDiagram_Lib.Themes;

/// <summary>
/// A caller-supplied colour palette. All seven colours are required.
/// Each value must be a valid CSS hex colour: #RGB or #RRGGBB.
/// </summary>
public record CustomTheme : ITheme
{
    private static readonly System.Text.RegularExpressions.Regex _hexColour =
        new(@"^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public string Name { get; }
    public string BoardColor { get; }
    public string PointColorDark { get; }
    public string PointColorLight { get; }
    public string CheckerColorOnRoll { get; }
    public string CheckerColorOpponent { get; }
    public string DiceColor { get; }
    public string TextColor { get; }

    public CustomTheme(
        string boardColor,
        string pointColorDark,
        string pointColorLight,
        string checkerColorOnRoll,
        string checkerColorOpponent,
        string diceColor,
        string textColor,
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
    }

    private string Validate(string value, string paramName)
    {
        if (!_hexColour.IsMatch(value))
            throw new ArgumentException(
                $"Invalid hex colour '{value}'. Expected #RGB or #RRGGBB.", paramName);
        return value;
    }
}