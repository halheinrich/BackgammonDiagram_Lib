using BgDataTypes_Lib;
using BackgammonDiagram_Lib.Themes;
using System.Numerics;
using System.Text;

namespace BackgammonDiagram_Lib.Rendering;

public class DiagramRenderer(ISvgRasterizer? rasterizer = null)
{
    private readonly ISvgRasterizer _rasterizer = rasterizer ?? new SkiaSharpRasterizer();
    private const double TitleStripHeight = 22;

    /// <summary>
    /// Equity when the opponent passes a double. Always 1.0 because cube
    /// equities are normalised per cube — a pass forfeits exactly one cube
    /// by definition, independent of match score or cube value.
    /// </summary>
    private const double PassEquity = 1.0;

    // Analysis-panel layout constants (shared between play and cube panels)
    private const double PanelMargin = 6;
    private const double PanelLineHeight = 13;
    private const double PanelFontSize = 9;

    // -----------------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------------

    public static string RenderSvg(DiagramRequest request, DiagramOptions options)
    {
        var theme = options.Theme;
        var layout = BoardLayout.Default;
        bool panelOnLeft = request.AnalysisPanelPosition == PanelPosition.Left;
        bool hasTitle = !string.IsNullOrWhiteSpace(request.Descriptive.Title);
        double titleOffset = hasTitle ? TitleStripHeight : 0;

        double totalWidth = layout.TotalWidth(withPanel: true);
        double totalHeight = layout.BoardHeight + titleOffset;

        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {F(totalWidth)} {F(totalHeight)}" width="100%">""");

        if (hasTitle)
            AppendTitleStrip(sb, totalWidth, titleOffset, theme, request.Descriptive.Title!);

        if (hasTitle)
            sb.AppendLine($"""  <g transform="translate(0,{F(titleOffset)})">""");

        AppendBoard(sb, layout, theme, request, panelOnLeft);

        if (hasTitle)
            sb.AppendLine("  </g>");

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
        return PdfBuilder.Build([png]);
    }

    public byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options)
    {
        var pngs = requests.Select(r => RenderPng(r, options)).ToList();
        return PdfBuilder.Build(pngs);
    }

    public byte[] RenderPptx(DiagramRequest request, DiagramOptions options)
    {
        var png = RenderPng(request, options);
        return PptxBuilder.Build([png]);
    }

    public byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options)
    {
        var pngs = requests.Select(r => RenderPng(r, options)).ToList();
        return PptxBuilder.Build(pngs);
    }

    /// <summary>
    /// Returns hit-test rectangles for all clickable board regions.
    /// Coordinates are in SVG viewBox space matching RenderSvg() output
    /// for a Problem-mode (no panel) diagram.
    /// </summary>
    public static BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options)
    {
        _ = options; // reserved for future use (e.g. theme-aware hit region sizing)
        var layout = BoardLayout.Default;
        bool homeBoardOnRight = request.HomeBoardOnRight;

        // No panel — hit regions are for interactive (Problem-mode) use
        const bool panelOnLeft = false;

        // Title strip, if present, offsets all board-relative Y coords in the
        // rendered SVG — hit regions must match.
        bool hasTitle = !string.IsNullOrWhiteSpace(request.Descriptive.Title);
        double titleOffset = hasTitle ? TitleStripHeight : 0;

        double totalWidth = layout.TotalWidth(withPanel: false);
        double totalHeight = layout.BoardHeight + titleOffset;

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
                y = layout.TopCheckerBaseY + titleOffset;
                h = layout.PointHeight;
            }
            else
            {
                // Bottom points: triangle area only
                y = layout.BottomCheckerBaseY + titleOffset;
                h = layout.PointHeight;
            }

            points[pt] = new HitRect(x, y, w, h);
        }

        // --- Bar ---
        var bar = new HitRect(
            layout.BarX(panelOnLeft),
            titleOffset,
            layout.BarWidth,
            layout.BoardHeight);

        // --- Cube: full left-rail column (covers all possible cube positions) ---
        var cube = new HitRect(
            layout.LeftRailX(panelOnLeft),
            titleOffset,
            layout.LeftRailWidth,
            layout.BoardHeight);

        return new BoardHitRegions
        {
            ViewBox = new SvgViewBox(0, 0, totalWidth, totalHeight),
            Points = points,
            Bar = bar,
            Cube = cube,
            OnRollTray = null  // bearing-off tray not yet rendered
        };
    }

    /// <summary>
    /// Returns true if QuestPDF is correctly licensed and operational.
    /// Call at application startup before serving PDF requests.
    /// If false, set QuestPDF.Settings.License in your app startup.
    /// Also returns false if QuestPDF's native dependencies cannot load
    /// (e.g. unsupported runtime).
    /// </summary>
    public static bool IsPdfSupported()
    {
        try
        {
            return QuestPDF.Settings.License.HasValue;
        }
        catch
        {
            return false;
        }
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
    //  Title strip
    // -----------------------------------------------------------------------

    private static void AppendTitleStrip(StringBuilder sb, double totalWidth, double height,
        ITheme theme, string title)
    {
        string bg = theme.PanelBackgroundColor;
        string textColor = ContrastText(bg);
        double textY = height / 2;
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(totalWidth)}" height="{F(height)}" fill="{bg}"/>""");
        sb.AppendLine($"""  <text x="8" y="{F(textY)}" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(title)}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Board
    // -----------------------------------------------------------------------

    private static void AppendBoard(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool panelOnLeft)
    {
        bool effectivePanelOnLeft = panelOnLeft;
        bool homeBoardOnRight = request.HomeBoardOnRight;
        double bx = layout.BoardOffsetX(effectivePanelOnLeft);

        // Full canvas background — prevents transparent edges showing in PNG.
        // Must match viewBox width (always withPanel: true); in Problem mode the
        // panel region is allocated but blank, and needs a background fill too.
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(layout.TotalWidth(withPanel: true))}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");

        sb.AppendLine($"""  <rect x="{F(bx)}" y="0" width="{F(layout.BoardWidth)}" height="{F(layout.BoardHeight)}" fill="{theme.BoardColor}"/>""");

        AppendLeftRail(sb, layout, theme, bx);
        AppendBar(sb, layout, theme, bx);
        AppendPoints(sb, layout, theme, effectivePanelOnLeft, homeBoardOnRight);
        AppendCheckers(sb, layout, theme, request, effectivePanelOnLeft);
        if (!request.Decision.IsCube)
            AppendDice(sb, layout, theme, request, effectivePanelOnLeft);
        AppendPointNumbers(sb, layout, theme, effectivePanelOnLeft, homeBoardOnRight);
        AppendTopRail(sb, layout, theme, bx, request);
        AppendBottomRail(sb, layout, theme, bx, request);
        AppendCube(sb, layout, theme, bx, request);
        AppendRightRail(sb, layout, theme, bx);  // last — draws over any overflowing content
        AppendAnalysisPanel(sb, layout, theme, request, panelOnLeft);
    }

    // -----------------------------------------------------------------------
    //  Rails
    // -----------------------------------------------------------------------

    private static void AppendLeftRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx)
    {
        sb.AppendLine($"""  <rect x="{F(bx)}" y="0" width="{F(layout.LeftRailWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");
    }

    private static void AppendRightRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx)
    {
        double rx = bx + layout.LeftRailWidth + layout.HalfWidth * 2 + layout.BarWidth;
        sb.AppendLine($"""  <rect x="{F(rx)}" y="0" width="{F(layout.RightRailWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");
    }

    private static void AppendBar(StringBuilder sb, BoardLayout layout, ITheme theme, double bx)
    {
        double barX = bx + layout.LeftRailWidth + layout.HalfWidth;
        sb.AppendLine($"""  <rect x="{F(barX)}" y="0" width="{F(layout.BarWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.10)}"/>""");
    }

    private static void AppendTopRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx,
        DiagramRequest request)
    {
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double railX = bx + layout.LeftRailWidth;
        double cy = layout.TopRailHeight / 2;

        sb.AppendLine($"""  <rect x="{F(railX)}" y="0" width="{F(railWidth)}" height="{F(layout.TopRailHeight)}" fill="{Darken(theme.BoardColor, 0.1)}"/>""");

        string topName = FormatPlayerLabel(request, isOnRoll: !request.OnRollAtBottom);
        string topPip = request.OnRollAtBottom
            ? $"Pip: {request.Position.OpponentPipCount}"
            : $"Pip: {request.Position.OnRollPipCount}";

        string railBg = Darken(theme.BoardColor, 0.1);
        string railText = ContrastText(railBg);

        sb.AppendLine($"""  <text x="{F(railX + 8)}" y="{F(cy)}" dominant-baseline="central" font-family="sans-serif" font-size="12" fill="{railText}">{Escape(topName)}</text>""");
        sb.AppendLine($"""  <text x="{F(railX + railWidth - 8)}" y="{F(cy)}" dominant-baseline="central" text-anchor="end" font-family="sans-serif" font-size="12" fill="{railText}">{Escape(topPip)}</text>""");
    }

    private static void AppendBottomRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx,
        DiagramRequest request)
    {
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double railX = bx + layout.LeftRailWidth;
        double cy = layout.BottomRailY + layout.BottomRailHeight / 2;

        sb.AppendLine($"""  <rect x="{F(railX)}" y="{F(layout.BottomRailY)}" width="{F(railWidth)}" height="{F(layout.BottomRailHeight)}" fill="{Darken(theme.BoardColor, 0.1)}"/>""");

        string bottomName = FormatPlayerLabel(request, isOnRoll: request.OnRollAtBottom);
        string bottomPip = request.OnRollAtBottom
            ? $"Pip: {request.Position.OnRollPipCount}"
            : $"Pip: {request.Position.OpponentPipCount}";

        string railBg = Darken(theme.BoardColor, 0.1);
        string railText = ContrastText(railBg);

        sb.AppendLine($"""  <text x="{F(railX + 8)}" y="{F(cy)}" dominant-baseline="central" font-family="sans-serif" font-size="12" fill="{railText}">{Escape(bottomName)}</text>""");
        sb.AppendLine($"""  <text x="{F(railX + railWidth - 8)}" y="{F(cy)}" dominant-baseline="central" text-anchor="end" font-family="sans-serif" font-size="12" fill="{railText}">{Escape(bottomPip)}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Points (triangles)
    // -----------------------------------------------------------------------

    private static void AppendPoints(StringBuilder sb, BoardLayout layout, ITheme theme, bool panelOnLeft, bool homeBoardOnRight)
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

    private static void AppendPointNumbers(StringBuilder sb, BoardLayout layout, ITheme theme, bool panelOnLeft, bool homeBoardOnRight)
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

    private static void AppendCheckers(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool panelOnLeft)
    {
        // Points 1–24
        for (int pt = 1; pt <= 24; pt++)
        {
            int count = request.Position.Mop[pt];
            if (count == 0) continue;

            bool onRoll = count > 0;
            int abs = Math.Abs(count);
            double cx = layout.ColumnCentreX(pt, panelOnLeft, request.HomeBoardOnRight);
            bool bottom = pt <= 12;  // points 1-12 stack upward from bottom

            AppendCheckerStack(sb, layout, theme, cx, abs, onRoll, bottom);
        }

        // On-roll bar (Mop[25], always >= 0) — stacks in the bottom half of the bar
        int onRollBar = request.Position.Mop[25];
        if (onRollBar > 0)
        {
            double cx = layout.BarCentreX(panelOnLeft);
            double anchorCy = layout.TopCheckerBaseY + layout.CheckerRadius
                              + 5 * layout.CheckerRadius * 2;
            AppendCheckerStack(sb, layout, theme, cx, onRollBar, onRoll: true,
                bottomHalf: false, anchorCy: anchorCy, labelAtBase: true);
        }

        // Opponent bar (Mop[0], always <= 0) — stacks in the top half of the bar
        int opponentBar = request.Position.Mop[0];
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

    private static void AppendCheckerStack(StringBuilder sb, BoardLayout layout, ITheme theme,
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

    private static void AppendDice(StringBuilder sb, BoardLayout layout, ITheme theme,
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

        AppendDie(sb, theme, d1X, dY, size, rx, request.Decision.Dice[0]);
        AppendDie(sb, theme, d2X, dY, size, rx, request.Decision.Dice[1]);
    }

    private static void AppendDie(StringBuilder sb, ITheme theme,
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

    private static void AppendCube(StringBuilder sb, BoardLayout layout, ITheme theme,
        double bx, DiagramRequest request)
    {
        double cubeSize = layout.LeftRailWidth * 0.7;
        double cubeX = bx + (layout.LeftRailWidth - cubeSize) / 2;
        double cubeY = request.Position.CubeOwner switch
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
        sb.AppendLine($"""  <rect x="{F(cubeX)}" y="{F(cubeY)}" width="{F(cubeSize)}" height="{F(cubeSize)}" rx="3" fill="{theme.DiceColor}" stroke="#888" stroke-width="0.5"/>""");
        double fontSize = cubeSize * 0.55;
        double textY = cubeY + cubeSize / 2 + fontSize * 0.35;
        string cubeText = request.Position.CubeSize == 1 ? "64" : request.Position.CubeSize.ToString();
        string cubeTextColor = ContrastText(theme.DiceColor);
        sb.AppendLine($"""  <text x="{F(cubeX + cubeSize / 2)}" y="{F(textY)}" text-anchor="middle" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold" fill="{cubeTextColor}">{cubeText}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Analysis panel
    // -----------------------------------------------------------------------

    private static void AppendAnalysisPanel(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool panelOnLeft)
    {
        double px = layout.PanelX(panelOnLeft);
        double pw = layout.PanelWidth;
        double ph = layout.BoardHeight;
        string panelBg = theme.PanelBackgroundColor;
        string panelText = ContrastText(panelBg);
        string dimText = Lighten(panelText, 0.3);

        // Panel background
        sb.AppendLine($"""  <rect x="{F(px)}" y="0" width="{F(pw)}" height="{F(ph)}" fill="{panelBg}"/>""");

        if (request.Mode != DiagramMode.Solution)
            return; // Problem mode: blank panel

        if (request.Decision.IsCube)
            AppendCubePanel(sb, px, pw, ph, panelText, dimText, request);
        else
            AppendPlayPanel(sb, px, pw, ph, panelText, dimText, request);
    }

    // -----------------------------------------------------------------------
    //  Play analysis panel
    // -----------------------------------------------------------------------

    private static void AppendPlayPanel(StringBuilder sb, double px, double pw, double ph,
        string textColor, string dimColor, DiagramRequest request)
    {
        double innerW = pw - PanelMargin * 2;
        double y = PanelMargin;
        double boxH = PanelLineHeight * 2 + 6; // two lines + padding
        double boxGap = 3;

        // Header
        sb.AppendLine($"""  <text x="{F(px + pw / 2)}" y="{F(y + PanelLineHeight * 0.8)}" text-anchor="middle" font-family="sans-serif" font-size="10" font-weight="bold" fill="{textColor}">Checker Play</text>""");
        y += PanelLineHeight + 4;

        var plays = request.Decision.Plays;
        if (plays.Count == 0) return;

        for (int i = 0; i < plays.Count; i++)
        {
            if (y + boxH > ph - PanelMargin) break; // stop if no room

            var play = plays[i];
            bool isBest = i == request.Decision.BestPlayIndex;
            bool isUser = i == request.Decision.UserPlayIndex;

            // Box background
            string boxBg = isBest ? "rgba(255,255,255,0.08)" : "rgba(0,0,0,0.06)";
            sb.AppendLine($"""  <rect x="{F(px + PanelMargin)}" y="{F(y)}" width="{F(innerW)}" height="{F(boxH)}" rx="2" fill="{boxBg}"/>""");

            double textX = px + PanelMargin + 4;
            double rightX = px + pw - PanelMargin - 4;

            // Line 1: rank + move notation + equity
            double line1Y = y + PanelLineHeight;
            string rankPrefix = isBest ? "\u265B " : isUser ? "\u2713 " : $"{i + 1}. ";
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(line1Y)}" font-family="sans-serif" font-size="{F(PanelFontSize)}" fill="{textColor}">{Escape(rankPrefix + play.MoveNotation)}</text>""");
            sb.AppendLine($"""  <text x="{F(rightX)}" y="{F(line1Y)}" text-anchor="end" font-family="sans-serif" font-size="{F(PanelFontSize)}" fill="{textColor}">{FormatEquity(play.Equity)}</text>""");

            // Line 2: equity loss (right-aligned)
            double line2Y = y + PanelLineHeight * 2;
            if (play.EquityLoss.HasValue && play.EquityLoss.Value > 0)
            {
                sb.AppendLine($"""  <text x="{F(rightX)}" y="{F(line2Y)}" text-anchor="end" font-family="sans-serif" font-size="{F(PanelFontSize)}" fill="{dimColor}">{FormatEquityLoss(play.EquityLoss.Value)}</text>""");
            }

            y += boxH + boxGap;
        }

        // Analysis depth at bottom if room
        AppendAnalysisDepths(sb, px, pw, ph, dimColor, request, PanelFontSize);
    }

    // -----------------------------------------------------------------------
    //  Cube analysis panel
    // -----------------------------------------------------------------------

    private static void AppendCubePanel(StringBuilder sb, double px, double pw, double ph,
        string textColor, string dimColor, DiagramRequest request)
    {
        const double LabelFontSize = 8;

        double y = PanelMargin;
        double textX = px + PanelMargin + 4;
        double rightX = px + pw - PanelMargin - 4;
        double centreX = px + pw / 2;

        // Header
        sb.AppendLine($"""  <text x="{F(centreX)}" y="{F(y + PanelLineHeight * 0.8)}" text-anchor="middle" font-family="sans-serif" font-size="10" font-weight="bold" fill="{textColor}">Cube Decision</text>""");
        y += PanelLineHeight + 6;

        // Proper cube action
        string action = DetermineCubeAction(request.Decision);
        sb.AppendLine($"""  <text x="{F(centreX)}" y="{F(y + PanelLineHeight * 0.8)}" text-anchor="middle" font-family="sans-serif" font-size="{F(PanelFontSize)}" font-weight="bold" fill="{textColor}">{Escape(action)}</text>""");
        y += PanelLineHeight + 8;

        // ── No Double section ──────────────────────────────────────────
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + PanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(PanelFontSize)}" font-weight="bold" fill="{textColor}">No Double</text>""");
        sb.AppendLine($"""  <text x="{F(rightX)}" y="{F(y + PanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(PanelFontSize)}" fill="{textColor}">{FormatEquity(request.Decision.NoDoubleEquity)}</text>""");
        y += PanelLineHeight + 2;

        y = AppendCubePercentages(sb, textX, y, LabelFontSize, dimColor,
            request.Decision.WinPctAfterNoDouble,
            request.Decision.GammonPctAfterNoDouble,
            request.Decision.BgPctAfterNoDouble,
            request.Decision.LosePctAfterNoDouble,
            request.Decision.LoseGammonPctAfterNoDouble,
            request.Decision.LoseBgPctAfterNoDouble);

        y += 6;

        // ── Double/Take section ────────────────────────────────────────
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + PanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(PanelFontSize)}" font-weight="bold" fill="{textColor}">Double/Take</text>""");
        sb.AppendLine($"""  <text x="{F(rightX)}" y="{F(y + PanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(PanelFontSize)}" fill="{textColor}">{FormatEquity(request.Decision.DoubleTakeEquity)}</text>""");
        y += PanelLineHeight + 2;

        y = AppendCubePercentages(sb, textX, y, LabelFontSize, dimColor,
            request.Decision.WinPctAfterDoubleTake,
            request.Decision.GammonPctAfterDoubleTake,
            request.Decision.BgPctAfterDoubleTake,
            request.Decision.LosePctAfterDoubleTake,
            request.Decision.LoseGammonPctAfterDoubleTake,
            request.Decision.LoseBgPctAfterDoubleTake);

        y += 8;

        // ── Probability of opponent error ──────────────────────────────
        double probErr = request.Decision.ProbOfOpponentErrorJustifyingDouble;
        if (probErr > 0)
        {
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + PanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(LabelFontSize)}" fill="{dimColor}">Opp. Error to Justify Double</text>""");
            y += PanelLineHeight;
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + PanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(PanelFontSize)}" fill="{textColor}">{F1(probErr * 100)}%</text>""");
        }

        // Analysis depth
        AppendAnalysisDepths(sb, px, pw, ph, dimColor, request, LabelFontSize);
    }

    private static double AppendCubePercentages(StringBuilder sb, double textX,
        double y, double fontSize, string color,
        double win, double gammon, double bg,
        double lose, double loseGammon, double loseBg)
    {
        // Win line
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + PanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">Win  {F1(win)}%   G {F1(gammon)}%   BG {F1(bg)}%</text>""");
        y += PanelLineHeight;

        // Lose line
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + PanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">Lose {F1(lose)}%   G {F1(loseGammon)}%   BG {F1(loseBg)}%</text>""");
        y += PanelLineHeight;

        return y;
    }

    // -----------------------------------------------------------------------
    //  Cube action determination
    // -----------------------------------------------------------------------

    private static string DetermineCubeAction(DecisionData d)
    {
        double nd = d.NoDoubleEquity;
        double dt = d.DoubleTakeEquity;

        if (nd >= PassEquity)
            return "Too Good to Double";
        if (dt > nd && dt >= PassEquity)
            return "Double, Pass";
        if (dt > nd)
            return "Double, Take";
        return "No Double";
    }

    // -----------------------------------------------------------------------
    //  Analysis depths (shared by play and cube panels)
    // -----------------------------------------------------------------------

    private static void AppendAnalysisDepths(StringBuilder sb, double px, double pw, double ph,
        string color, DiagramRequest request, double fontSize)
    {
        var depths = request.Decision.AnalysisDepths;
        if (depths.Count == 0) return;

        // Position at bottom of panel, stacked upward.
        double y = ph - 6 - depths.Count * 12;
        if (y < 0) return; // panel too short — skip rather than overlap

        double centreX = px + pw / 2;
        foreach (var depth in depths)
        {
            sb.AppendLine($"""  <text x="{F(centreX)}" y="{F(y + 10)}" text-anchor="middle" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">{Escape(depth.Label)}</text>""");
            y += 12;
        }
    }

    // -----------------------------------------------------------------------
    //  Equity formatting
    // -----------------------------------------------------------------------

    private static string FormatEquity(double equity)
    {
        string sign = equity >= 0 ? "+" : "";
        return sign + equity.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatEquityLoss(double loss)
    {
        return "-" + loss.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static string F(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    // Invariant 1-decimal format, used for percentages where "42.0%" looks
    // better than the trimmed "42%" that F() would produce.
    private static string F1(double v) => v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    private static string Escape(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    /// <summary>Darken a 6-char hex colour by the given factor (0..1).</summary>
    private static string Darken(string hex, double factor)
    {
        if (factor < 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Use Lighten() for negative factors.");
        return ScaleRgb(hex, 1.0 - factor);
    }

    /// <summary>Lighten a 6-char hex colour by the given factor (0..).</summary>
    private static string Lighten(string hex, double factor)
    {
        if (factor < 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Use Darken() for negative factors.");
        return ScaleRgb(hex, 1.0 + factor);
    }

    private static string ScaleRgb(string hex, double scale)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            throw new ArgumentException($"Theme color must be a 6-character hex value, got '{hex}'.");
        int r = Math.Clamp((int)(int.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber) * scale), 0, 255);
        int g = Math.Clamp((int)(int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber) * scale), 0, 255);
        int b = Math.Clamp((int)(int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber) * scale), 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
    private static string ContrastText(string bgHex)
    {
        bgHex = bgHex.TrimStart('#');
        int r = int.Parse(bgHex[..2], System.Globalization.NumberStyles.HexNumber);
        int g = int.Parse(bgHex[2..4], System.Globalization.NumberStyles.HexNumber);
        int b = int.Parse(bgHex[4..6], System.Globalization.NumberStyles.HexNumber);
        // Relative luminance (ITU-R BT.709)
        double luminance = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0;
        return luminance > 0.5 ? "#1A1A1A" : "#F0F0F0";
    }
    private static string FormatPlayerLabel(DiagramRequest request, bool isOnRoll)
    {
        string name = isOnRoll ? request.Descriptive.OnRollName : request.Descriptive.OpponentName;
        int matchLength = request.Descriptive.MatchLength;

        // MatchLength == 0 is the money-game sentinel from DescriptiveData.
        if (matchLength == 0)
            return $"{name} (money game)";

        int needs = isOnRoll ? request.Position.OnRollNeeds : request.Position.OpponentNeeds;
        string crawford = request.Position.IsCrawford ? " Crawford" : "";
        return $"{name} needs {needs}{crawford}";
    }
}