namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// All board layout constants derived from the base checker radius.
/// Change CheckerRadius and everything else scales accordingly.
/// </summary>
internal readonly struct BoardLayout
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
    //  Checker stacking
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maximum number of checkers drawn in a single point or bar stack before
    /// the renderer collapses the remainder into a numeric count label.
    /// Single source of truth: the checker renderer caps its drawn circles at
    /// this value (see DiagramRenderer.AppendCheckerStack) and the hit-region
    /// builder sizes each point's clickable rect from the resulting stack
    /// height (see <see cref="MaxStackHeight"/>), so the topmost drawn checker
    /// is always inside its point's hit region. Do not hardcode this bound at
    /// either site — both must read it here or they drift apart.
    /// </summary>
    public const int MaxStackCheckers = 6;

    /// <summary>
    /// Vertical extent of a maximum-height checker stack, measured from the
    /// point base toward the board centre. A stack of
    /// <see cref="MaxStackCheckers"/> contiguous checkers — each one diameter
    /// tall, the first sitting on the point base — spans exactly this far.
    /// Exceeds <see cref="PointHeight"/> (the triangle is only 5 checkers tall),
    /// which is precisely why point hit-rects key off this rather than the
    /// triangle height.
    /// </summary>
    public double MaxStackHeight => MaxStackCheckers * D;

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

    /// <summary>
    /// Intrinsic panel width when no aspect override is applied. Scales with
    /// CheckerRadius like the rest of the board.
    /// </summary>
    public double DefaultPanelWidth => D * 5.5;   // ~30% of total width at default size

    /// <summary>
    /// Optional override. When set, used verbatim as PanelWidth — enables
    /// aspect-targeted rendering without touching board geometry (so checkers
    /// stay round). When null, falls back to DefaultPanelWidth. Ignored when
    /// <see cref="BoardOnly"/> is set (there is no panel to size).
    /// </summary>
    public double? PanelWidthOverride { get; init; }

    /// <summary>
    /// When <c>true</c>, the layout allocates no analysis panel: the canvas is
    /// the board proper (and the renderer adds no title strip above it either,
    /// so the layout's height is the canvas height). Defaults to
    /// <c>false</c> — the panel-bearing layout every
    /// preset except <c>AspectPreset.BoardOnly</c> uses — so a
    /// <c>default</c>-constructed layout keeps today's geometry.
    /// </summary>
    public bool BoardOnly { get; init; }

    /// <summary>
    /// Width allocated to the analysis panel. Zero when <see cref="BoardOnly"/>
    /// is set — the single definition site that collapses every panel-derived
    /// origin (<see cref="BoardOffsetX"/>, <see cref="PanelX"/>,
    /// <see cref="TotalWidth"/>) to the board-only canvas.
    /// </summary>
    public double PanelWidth => BoardOnly ? 0 : PanelWidthOverride ?? DefaultPanelWidth;

    // -----------------------------------------------------------------------
    //  Derived totals
    // -----------------------------------------------------------------------
    public double BoardWidth =>
        LeftRailWidth + HalfWidth + BarWidth + HalfWidth + RightRailWidth;

    public double BoardHeight =>
        TopRailHeight + PointNumberHeight +
        PointHeight + MiddleGap + PointHeight +
        PointNumberHeight + BottomRailHeight;

    /// <summary>
    /// Full canvas width: board plus panel allocation (the panel contributes
    /// zero under <see cref="BoardOnly"/>). Intentionally not parameterized —
    /// a per-call-site with/without-panel choice is exactly the drift that once
    /// desynced RenderSvg from GetHitRegions (see HitRegionsTests'
    /// coordinate-system regression test).
    /// </summary>
    public double TotalWidth => BoardWidth + PanelWidth;

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
    public double BarCentreX(bool panelOnLeft) => BarX(panelOnLeft) + BarWidth / 2;

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
        double halfX;
        int col;

        if (point >= 1 && point <= 6)
        {
            halfX = InnerHalfX(panelOnLeft);
            col = 6 - point;
        }
        else if (point >= 7 && point <= 12)
        {
            halfX = OuterHalfX(panelOnLeft);
            col = 12 - point;
        }
        else if (point >= 13 && point <= 18)
        {
            halfX = OuterHalfX(panelOnLeft);
            col = point - 13;
        }
        else // 19-24
        {
            halfX = InnerHalfX(panelOnLeft);
            col = point - 19;
        }

        double cx = halfX + col * ColumnWidth + ColumnWidth / 2;
        if (!homeBoardOnRight)
        {
            double spanLeft = OuterHalfX(panelOnLeft);
            double spanRight = InnerHalfX(panelOnLeft) + HalfWidth;
            cx = spanLeft + (spanRight - cx);
        }
        return cx;
    }

    // -----------------------------------------------------------------------
    //  Default instance
    // -----------------------------------------------------------------------
    public static BoardLayout Default => new() { CheckerRadius = 14 };
}
