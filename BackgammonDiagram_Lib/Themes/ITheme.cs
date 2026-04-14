namespace BackgammonDiagram_Lib.Themes;

public interface ITheme
{
    string Name { get; }
    string BoardColor { get; }
    string PointColorDark { get; }
    string PointColorLight { get; }
    string CheckerColorOnRoll { get; }
    string CheckerColorOpponent { get; }
    string DiceColor { get; }
    string TextColor { get; }
    string PanelBackgroundColor { get; }
}
