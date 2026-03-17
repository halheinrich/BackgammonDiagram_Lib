namespace BackgammonDiagram_Lib.Themes;

public class DefaultTheme : ITheme
{
    public string Name               => "Default";
    public string BoardColor         => "#C8A96E";
    public string PointColorDark     => "#8B2500";
    public string PointColorLight    => "#F5DEB3";
    public string CheckerColorOnRoll => "#1A1A1A";
    public string CheckerColorOpponent => "#F0F0F0";
    public string DiceColor          => "#FFFFFF";
    public string TextColor          => "#000000";
}
