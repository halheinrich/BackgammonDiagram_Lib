namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// All board layout constants derived from the base checker radius.
/// Change CheckerRadius and everything else scales accordingly.
/// </summary>
public readonly struct BoardLayout
{
    // -----------------------------------------------------------------------
    //  Core unit — everything derives from this
    // -----------------------------------------------------------------------
    public double CheckerRadius { get; init; }
    private double D => CheckerRadius * 2;  // checker diameter = column width

    // -----------------------------------------------------------------------
    //  Point geometry
    // -----------------------------------------------------------------------
    public double ColumnWidth   => D;
    public double PointHeight   => D * 5;       // 5 checkers tall
    public double MiddleGap     => D * 2.5;     // 2.5 checker gap between point tips

    // -----------------------------------------------------------------------
    //  Rails
    // -----------------------------------------------------------------------
    public double LeftRailWidth  => D * 1.5;    // cube lives here
    public double RightRailWidth => D * 0.75;
    public double BarWidth       => D * 1.1;    // bar checkers only

    // -----------------------------------------------------------------------
    //  Top / bottom strips
    // -----------------------------------------------------------------------
    public double TopRailHeight     => 28;      // name · pip · score
    public double PointNumberHeight => 20;      // point number labels
    public double BottomRailHeight  => 28;

    // -----------------------------------------------------------------------
    //  Board halves
    // -----------------------------------------------------------------------
    public double HalfWidth => ColumnWidth * 6; // 6 points per half

    // -----------------------------------------------------------------------
    //  Analysis panel
    // -----------------------------------------------------------------------
    public double PanelWidth => D * 5.5;        // ~30% of total width at default size

    // -----------------------------------------------------------------------
    //  Derived totals
    // -----------------------------------------------------------------------
    public double BoardWidth =>
        LeftRailWidth + HalfWidth + BarWidth + HalfWidth + RightRailWidth;

    public double BoardHeight =>
        TopRailHeight + PointNumberHeight +
        PointHeight + MiddleGap + PointHeight +
        PointNumberHeight + BottomRailHeight;

    public double TotalWidth(bool withPanel) =>
        withPanel ? BoardWidth + PanelWidth : BoardWidth;

    // -----------------------------------------------------------------------
    //  X origins (board starts at x=0; panel added to left if needed)
    // -----------------------------------------------------------------------

    /// <summary>X offset applied to the entire board when the panel is on the left.</summary>
    public double BoardOffsetX(bool panelOnLeft) =>
        panelOnLeft ? PanelWidth : 0;

    public double LeftRailX(bool panelOnLeft)    => BoardOffsetX(panelOnLeft);
    public double OuterHalfX(bool panelOnLeft)   => LeftRailX(panelOnLeft) + LeftRailWidth;
    public double BarX(bool panelOnLeft)         => OuterHalfX(panelOnLeft) + HalfWidth;
    public double InnerHalfX(bool panelOnLeft)   => BarX(panelOnLeft) + BarWidth;
    public double RightRailX(bool panelOnLeft)   => InnerHalfX(panelOnLeft) + HalfWidth;

    public double PanelX(bool panelOnLeft) =>
        panelOnLeft ? 0 : BoardWidth;

    // -----------------------------------------------------------------------
    //  Y origins
    // -----------------------------------------------------------------------
    public double TopRailY         => 0;
    public double TopNumberY       => TopRailHeight;
    public double TopCheckerBaseY  => TopRailHeight + PointNumberHeight;   // checkers grow downward
    public double MiddleY          => TopCheckerBaseY + PointHeight;
    public double BottomCheckerBaseY => MiddleY + MiddleGap;               // checkers grow upward
    public double BottomNumberY    => BottomCheckerBaseY + PointHeight;
    public double BottomRailY      => BottomNumberY + PointNumberHeight;

    // -----------------------------------------------------------------------
    //  Column X centre for a given point index (1-based, 1=bottom-right by default)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the X centre of the column for a given point number (1–24),
    /// assuming OnRollAtBottom=true and OnRollRight orientation.
    /// The renderer applies orientation transforms on top of this.
    /// </summary>
    public double ColumnCentreX(int point, bool panelOnLeft, bool homeBoardOnRight = true)
    {
        // Points 1-6: inner half, right side, columns right-to-left
        // Points 7-12: outer half, right side, columns right-to-left
        // Points 13-18: outer half, left side, columns left-to-right
        // Points 19-24: inner half, left side, columns left-to-right
        if (!homeBoardOnRight) point = 25 - point;
        double halfX;
        int col; // 0-based column within half (left to right)

        if (point >= 1 && point <= 6)
        {
            halfX = InnerHalfX(panelOnLeft);
            col = 6 - point; // pt1=rightmost col5, pt6=leftmost col0
        }
        else if (point >= 7 && point <= 12)
        {
            halfX = OuterHalfX(panelOnLeft);
            col = 12 - point; // pt7=rightmost col5, pt12=leftmost col0
        }
        else if (point >= 13 && point <= 18)
        {
            halfX = OuterHalfX(panelOnLeft);
            col = point - 13; // pt13=leftmost col0, pt18=rightmost col5
        }
        else // 19-24
        {
            halfX = InnerHalfX(panelOnLeft);
            col = point - 19; // pt19=leftmost col0, pt24=rightmost col5
        }

        return halfX + col * ColumnWidth + ColumnWidth / 2;
    }

    // -----------------------------------------------------------------------
    //  Default instance
    // -----------------------------------------------------------------------
    public static BoardLayout Default => new() { CheckerRadius = 14 };
}
