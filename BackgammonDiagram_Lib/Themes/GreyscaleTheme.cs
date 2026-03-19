namespace BackgammonDiagram_Lib.Themes;

public class GreyscaleTheme : ITheme
{
    public string Name => "Greyscale";
    public string BoardColor => "#C0C0C0";
    public string PointColorDark => "#404040";
    public string PointColorLight => "#F0F0F0";
    public string CheckerColorOnRoll => "#111111";
    public string CheckerColorOpponent => "#EEEEEE";
    public string DiceColor => "#FFFFFF";
    public string TextColor => "#000000";
}