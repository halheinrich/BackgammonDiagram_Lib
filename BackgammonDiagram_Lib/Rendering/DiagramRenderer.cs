using BackgammonDiagram_Lib.Themes;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace BackgammonDiagram_Lib.Rendering;

public class DiagramRenderer
{
    private readonly ISvgRasterizer _rasterizer;

    public DiagramRenderer(ISvgRasterizer? rasterizer = null)
    {
        _rasterizer = rasterizer ?? new SkiaSharpRasterizer();
    }

    // -----------------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------------

    public string RenderSvg(DiagramRequest request, DiagramOptions options)
    {
        var theme = options.Theme;
        var layout = BoardLayout.Default;
        bool hasPanel = request.Mode == DiagramMode.Solution;
        bool panelOnLeft = request.AnalysisPanelPosition == PanelPosition.Left;

        double totalWidth = layout.TotalWidth(hasPanel);
        double totalHeight = layout.BoardHeight;

        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {F(totalWidth)} {F(totalHeight)}" width="100%">""");

        AppendBoard(sb, layout, theme, request, hasPanel, panelOnLeft);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    public byte[] RenderPng(DiagramRequest request, DiagramOptions options)
    {
        var svg = RenderSvg(request, options);
        int targetWidth = ResolveTargetWidth(options.Size);
        return _rasterizer.Rasterize(svg, targetWidth);
    }

    public byte[] RenderPdf(DiagramRequest request, DiagramOptions options)
    {
        var png = RenderPng(request, options);
        return PdfBuilder.Build([(png, request.Title)]);
    }

    public byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options)
    {
        var slides = requests.Select(r => (RenderPng(r, options), r.Title));
        return PdfBuilder.Build(slides);
    }
    public byte[] RenderPptx(DiagramRequest request, DiagramOptions options)
    {
        var png = RenderPng(request, options);
        return PptxBuilder.Build([(png, request.Title)]);
    }

    public byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options)
    {
        var slides = requests.Select(r => (RenderPng(r, options), r.Title));
        return PptxBuilder.Build(slides);
    }

    /// <summary>
    /// Returns hit-test rectangles for all clickable board regions.
    /// Coordinates are in SVG viewBox space matching RenderSvg() output
    /// for a Problem-mode (no panel) diagram.
    /// </summary>
    public BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options)
    {
        var layout = BoardLayout.Default;
        bool homeBoardOnRight = request.HomeBoardOnRight;

        // No panel — hit regions are for interactive (Problem-mode) use
        const bool panelOnLeft = false;
        double totalWidth = layout.TotalWidth(withPanel: false);
        double totalHeight = layout.BoardHeight;

        // --- Points 1–24 ---
        var points = new Dictionary<int, HitRect>(24);

        for (int pt = 1; pt <= 24; pt++)
        {
            double cx = layout.ColumnCentreX(pt, panelOnLeft, homeBoardOnRight);
            double x = cx - layout.ColumnWidth / 2;
            double w = layout.ColumnWidth;

            double y, h;
            if (pt >= 13)
            {
                // Top points: triangle area only
                y = layout.TopCheckerBaseY;
                h = layout.PointHeight;
            }
            else
            {
                // Bottom points: triangle area only
                y = layout.BottomCheckerBaseY;
                h = layout.PointHeight;
            }

            points[pt] = new HitRect(x, y, w, h);
        }

        // --- Bar ---
        var bar = new HitRect(
            layout.BarX(panelOnLeft),
            0,
            layout.BarWidth,
            totalHeight);

        // --- Cube: full left-rail column (covers all possible cube positions) ---
        var cube = new HitRect(
            layout.LeftRailX(panelOnLeft),
            0,
            layout.LeftRailWidth,
            totalHeight);

        return new BoardHitRegions
        {
            ViewBox = new SvgViewBox(0, 0, totalWidth, totalHeight),
            Points = points,
            Bar = bar,
            Cube = cube,
            OnRollTray = null  // bearing-off tray not yet rendered
        };
    }

    // -----------------------------------------------------------------------
    //  Size resolution
    // -----------------------------------------------------------------------

    private static int ResolveTargetWidth(DiagramSize size) => size.Preset switch
    {
        DiagramSizePreset.Small => 600,
        DiagramSizePreset.Large => 1600,
        DiagramSizePreset.Custom => size.CustomWidth ?? 1000,
        _ => 1000   // Medium
    };

    // -----------------------------------------------------------------------
    //  Board
    // -----------------------------------------------------------------------

    private void AppendBoard(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool hasPanel, bool panelOnLeft)
    {
        bool effectivePanelOnLeft = hasPanel && panelOnLeft;
        bool homeBoardOnRight = request.HomeBoardOnRight;
        double bx = layout.BoardOffsetX(effectivePanelOnLeft);

        // Full canvas background — prevents transparent edges showing in PNG
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(layout.TotalWidth(hasPanel))}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");

        sb.AppendLine($"""  <rect x="{F(bx)}" y="0" width="{F(layout.BoardWidth)}" height="{F(layout.BoardHeight)}" fill="{theme.BoardColor}"/>""");

        AppendLeftRail(sb, layout, theme, bx);
        AppendBar(sb, layout, theme, bx);
        AppendPoints(sb, layout, theme, bx, effectivePanelOnLeft, homeBoardOnRight);
        AppendCheckers(sb, layout, theme, request, effectivePanelOnLeft);
        if (!request.IsCube)
            AppendDice(sb, layout, theme, request, effectivePanelOnLeft);
        AppendPointNumbers(sb, layout, theme, bx, effectivePanelOnLeft, homeBoardOnRight);
        AppendTopRail(sb, layout, theme, bx, request);
        AppendBottomRail(sb, layout, theme, bx, request);
        AppendCube(sb, layout, theme, bx, request);
        AppendRightRail(sb, layout, theme, bx);  // last — draws over any overflowing content
    }

    // -----------------------------------------------------------------------
    //  Rails
    // -----------------------------------------------------------------------

    private void AppendLeftRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx)
    {
        sb.AppendLine($"""  <rect x="{F(bx)}" y="0" width="{F(layout.LeftRailWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");
    }

    private void AppendRightRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx)
    {
        double rx = bx + layout.LeftRailWidth + layout.HalfWidth * 2 + layout.BarWidth;
        sb.AppendLine($"""  <rect x="{F(rx)}" y="0" width="{F(layout.RightRailWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");
    }

    private void AppendBar(StringBuilder sb, BoardLayout layout, ITheme theme, double bx)
    {
        double barX = bx + layout.LeftRailWidth + layout.HalfWidth;
        sb.AppendLine($"""  <rect x="{F(barX)}" y="0" width="{F(layout.BarWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.10)}"/>""");
    }

    private void AppendTopRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx,
        DiagramRequest request)
    {
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double railX = bx + layout.LeftRailWidth;
        double cy = layout.TopRailHeight / 2;

        sb.AppendLine($"""  <rect x="{F(railX)}" y="0" width="{F(railWidth)}" height="{F(layout.TopRailHeight)}" fill="{Darken(theme.BoardColor, 0.2)}"/>""");

        string topName = request.OnRollAtBottom ? request.OpponentName : request.OnRollName;
        string topPip = request.OnRollAtBottom
            ? $"Pip= {request.OpponentPipCount}"
            : $"Pip= {request.OnRollPipCount}";

        sb.AppendLine($"""  <text x="{F(railX + 8)}" y="{F(cy)}" dominant-baseline="central" font-family="sans-serif" font-size="12" fill="{theme.TextColor}">{Escape(topName)}</text>""");
        sb.AppendLine($"""  <text x="{F(railX + railWidth - 8)}" y="{F(cy)}" dominant-baseline="central" text-anchor="end" font-family="sans-serif" font-size="12" fill="{theme.TextColor}">{Escape(topPip)}</text>""");
    }

    private void AppendBottomRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx,
        DiagramRequest request)
    {
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double railX = bx + layout.LeftRailWidth;
        double cy = layout.BottomRailY + layout.BottomRailHeight / 2;

        sb.AppendLine($"""  <rect x="{F(railX)}" y="{F(layout.BottomRailY)}" width="{F(railWidth)}" height="{F(layout.BottomRailHeight)}" fill="{Darken(theme.BoardColor, 0.2)}"/>""");

        string bottomName = request.OnRollAtBottom ? request.OnRollName : request.OpponentName;
        string bottomPip = request.OnRollAtBottom
            ? $"Pip= {request.OnRollPipCount}"
            : $"Pip= {request.OpponentPipCount}";

        sb.AppendLine($"""  <text x="{F(railX + 8)}" y="{F(cy)}" dominant-baseline="central" font-family="sans-serif" font-size="12" fill="{theme.TextColor}">{Escape(bottomName)}</text>""");
        sb.AppendLine($"""  <text x="{F(railX + railWidth - 8)}" y="{F(cy)}" dominant-baseline="central" text-anchor="end" font-family="sans-serif" font-size="12" fill="{theme.TextColor}">{Escape(bottomPip)}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Points (triangles)
    // -----------------------------------------------------------------------

    private void AppendPoints(StringBuilder sb, BoardLayout layout, ITheme theme,
            double bx, bool panelOnLeft, bool homeBoardOnRight)
    {
        for (int pt = 1; pt <= 24; pt++)
        {
            string color = (pt % 2 == 0) ? theme.PointColorDark : theme.PointColorLight;
            double cx = layout.ColumnCentreX(pt, panelOnLeft, homeBoardOnRight);
            double halfW = layout.ColumnWidth / 2;

            if (pt >= 13)
            {
                double baseY = layout.TopCheckerBaseY;
                double tipY = layout.TopCheckerBaseY + layout.PointHeight;
                sb.AppendLine($"""  <polygon points="{F(cx - halfW)},{F(baseY)} {F(cx + halfW)},{F(baseY)} {F(cx)},{F(tipY)}" fill="{color}"/>""");
            }
            else
            {
                double baseY = layout.BottomCheckerBaseY + layout.PointHeight;
                double tipY = layout.BottomCheckerBaseY;
                sb.AppendLine($"""  <polygon points="{F(cx - halfW)},{F(baseY)} {F(cx + halfW)},{F(baseY)} {F(cx)},{F(tipY)}" fill="{color}"/>""");
            }
        }
    }

    // -----------------------------------------------------------------------
    //  Point numbers
    // -----------------------------------------------------------------------

    private void AppendPointNumbers(StringBuilder sb, BoardLayout layout, ITheme theme,
            double bx, bool panelOnLeft, bool homeBoardOnRight)
    {
        for (int pt = 1; pt <= 24; pt++)
        {
            double cx = layout.ColumnCentreX(pt, panelOnLeft, homeBoardOnRight);

            if (pt >= 13)
            {
                double y = layout.TopNumberY + layout.PointNumberHeight / 2;
                sb.AppendLine($"""  <text x="{F(cx)}" y="{F(y)}" dominant-baseline="central" text-anchor="middle" font-family="sans-serif" font-size="11" fill="{theme.TextColor}">{pt}</text>""");
            }
            else
            {
                double y = layout.BottomNumberY + layout.PointNumberHeight / 2;
                sb.AppendLine($"""  <text x="{F(cx)}" y="{F(y)}" dominant-baseline="central" text-anchor="middle" font-family="sans-serif" font-size="11" fill="{theme.TextColor}">{pt}</text>""");
            }
        }
    }

    // -----------------------------------------------------------------------
    //  Checkers
    // -----------------------------------------------------------------------

    private void AppendCheckers(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool panelOnLeft)
    {
        // Points 1–24
        for (int pt = 1; pt <= 24; pt++)
        {
            int count = request.Mop[pt];
            if (count == 0) continue;

            bool onRoll = count > 0;
            int abs = Math.Abs(count);
            double cx = layout.ColumnCentreX(pt, panelOnLeft);
            bool bottom = pt <= 12;  // points 1-12 stack upward from bottom

            AppendCheckerStack(sb, layout, theme, cx, abs, onRoll, bottom);
        }

        // On-roll bar (Mop[25], always >= 0) — stacks in the bottom half of the bar
        int onRollBar = request.Mop[25];
        if (onRollBar > 0)
        {
            double cx = layout.BarCentreX(panelOnLeft);
            double anchorCy = layout.TopCheckerBaseY + layout.CheckerRadius
                              + 5 * layout.CheckerRadius * 2;
            AppendCheckerStack(sb, layout, theme, cx, onRollBar, onRoll: true,
                bottomHalf: false, anchorCy: anchorCy, labelAtBase: true);
        }

        // Opponent bar (Mop[0], always <= 0) — stacks in the top half of the bar
        int opponentBar = request.Mop[0];
        if (opponentBar < 0)
        {
            double cx = layout.BarCentreX(panelOnLeft);
            double anchorCy = layout.BottomCheckerBaseY + layout.PointHeight
                              - layout.CheckerRadius
                              - 5 * layout.CheckerRadius * 2;
            AppendCheckerStack(sb, layout, theme, cx, Math.Abs(opponentBar), onRoll: false,
                bottomHalf: true, anchorCy: anchorCy, labelAtBase: false);
        }
    }

    private void AppendCheckerStack(StringBuilder sb, BoardLayout layout, ITheme theme,
            double cx, int abs, bool onRoll, bool bottomHalf,
            double? anchorCy = null, bool labelAtBase = false)
    {
        string fill = onRoll ? theme.CheckerColorOnRoll : theme.CheckerColorOpponent;
        string stroke = "#888888";
        double r = layout.CheckerRadius;
        int draw = Math.Min(abs, 6);
        bool capped = abs > 6;

        for (int i = 0; i < draw; i++)
        {
            double cy = anchorCy.HasValue
                ? bottomHalf
                    ? anchorCy.Value + i * r * 2    // fixed anchor, grow downward
                    : anchorCy.Value - i * r * 2    // fixed anchor, grow upward
                : bottomHalf
                    ? layout.BottomCheckerBaseY + layout.PointHeight - r - i * r * 2
                    : layout.TopCheckerBaseY + r + i * r * 2;

            sb.AppendLine($"""  <circle cx="{F(cx)}" cy="{F(cy)}" r="{F(r)}" fill="{fill}" stroke="{stroke}" stroke-width="0.75"/>""");

            bool isLabelCircle = labelAtBase ? i == 0 : i == draw - 1;
            if (capped && isLabelCircle)
            {
                string labelFill = onRoll ? theme.CheckerColorOpponent : theme.CheckerColorOnRoll;
                double textY = cy + r * 0.35;
                sb.AppendLine($"""  <text x="{F(cx)}" y="{F(textY)}" text-anchor="middle" font-family="sans-serif" font-size="{F(r * 1.1)}" font-weight="bold" fill="{labelFill}">{abs}</text>""");
            }
        }
    }

    // -----------------------------------------------------------------------
    //  Dice
    // -----------------------------------------------------------------------

    private void AppendDice(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool panelOnLeft)
    {
        double r = layout.CheckerRadius;
        double size = r * 1.6;          // die face size
        double gap = size * 0.3;       // gap between the two dice
        double rx = size * 0.15;      // corner radius

        // Dice sit in the middle gap, vertically centred
        double cy = layout.MiddleY + layout.MiddleGap / 2;
        double pairW = size * 2 + gap;

        // Horizontal centre: right half when on-roll is at bottom, left half otherwise
        double halfCx = request.OnRollAtBottom
            ? layout.InnerHalfX(panelOnLeft) + layout.HalfWidth / 2
            : layout.OuterHalfX(panelOnLeft) + layout.HalfWidth / 2;

        double d1X = halfCx - pairW / 2;          // left die top-left x
        double d2X = halfCx - pairW / 2 + size + gap; // right die top-left x
        double dY = cy - size / 2;               // top-left y (same for both)

        AppendDie(sb, theme, d1X, dY, size, rx, request.Dice[0]);
        AppendDie(sb, theme, d2X, dY, size, rx, request.Dice[1]);
    }

    private void AppendDie(StringBuilder sb, ITheme theme,
        double x, double y, double size, double rx, int value)
    {
        // Face
        sb.AppendLine($"""  <rect x="{F(x)}" y="{F(y)}" width="{F(size)}" height="{F(size)}" rx="{F(rx)}" fill="{theme.DiceColor}" stroke="#888" stroke-width="0.75"/>""");

        // Pip grid: 3×3 positions, each pip at fraction of die size
        // col: 0=left(0.25), 1=centre(0.5), 2=right(0.75)
        // row: 0=top(0.25),  1=middle(0.5), 2=bottom(0.75)
        double pipR = size * 0.09;

        var pips = PipPositions(value);
        foreach (var (col, row) in pips)
        {
            double px = x + size * (0.25 + col * 0.25);
            double py = y + size * (0.25 + row * 0.25);
            sb.AppendLine($"""  <circle cx="{F(px)}" cy="{F(py)}" r="{F(pipR)}" fill="#222"/>""");
        }
    }

    /// <summary>Returns (col, row) 0-based pip positions for a die face value 1–6.</summary>
    private static IEnumerable<(int col, int row)> PipPositions(int value) => value switch
    {
        1 => [(1, 1)],
        2 => [(0, 0), (2, 2)],
        3 => [(0, 0), (1, 1), (2, 2)],
        4 => [(0, 0), (2, 0), (0, 2), (2, 2)],
        5 => [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2)],
        6 => [(0, 0), (2, 0), (0, 1), (2, 1), (0, 2), (2, 2)],
        _ => []
    };

    // -----------------------------------------------------------------------
    //  Cube
    // -----------------------------------------------------------------------

    private void AppendCube(StringBuilder sb, BoardLayout layout, ITheme theme,
        double bx, DiagramRequest request)
    {
        double cubeSize = layout.LeftRailWidth * 0.7;
        double cubeX = bx + (layout.LeftRailWidth - cubeSize) / 2;
        double cubeY = request.CubeOwner switch
        {
            CubeOwner.Centered => layout.BoardHeight / 2 - cubeSize / 2,
            CubeOwner.OnRoll => request.OnRollAtBottom
                ? layout.BottomCheckerBaseY + layout.PointHeight / 2 - cubeSize / 2
                : layout.TopCheckerBaseY + layout.PointHeight / 2 - cubeSize / 2,
            CubeOwner.Opponent => request.OnRollAtBottom
                ? layout.TopCheckerBaseY + layout.PointHeight / 2 - cubeSize / 2
                : layout.BottomCheckerBaseY + layout.PointHeight / 2 - cubeSize / 2,
            _ => layout.BoardHeight / 2 - cubeSize / 2
        };

        sb.AppendLine($"""  <rect x="{F(cubeX)}" y="{F(cubeY)}" width="{F(cubeSize)}" height="{F(cubeSize)}" rx="3" fill="white" stroke="#888" stroke-width="0.5"/>""");
        double fontSize = cubeSize * 0.55;
        double textY = cubeY + cubeSize / 2 + fontSize * 0.35;
        sb.AppendLine($"""  <text x="{F(cubeX + cubeSize / 2)}" y="{F(textY)}" text-anchor="middle" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold" fill="#222">{request.CubeSize}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static string F(double v) => v.ToString("0.##");

    private static string Escape(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    private static string Darken(string hex, double factor)
    {
        hex = hex.TrimStart('#');
        int r = (int)(Convert.ToInt32(hex[..2], 16) * (1 - factor));
        int g = (int)(Convert.ToInt32(hex[2..4], 16) * (1 - factor));
        int b = (int)(Convert.ToInt32(hex[4..6], 16) * (1 - factor));
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}