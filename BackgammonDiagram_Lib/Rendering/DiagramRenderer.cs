using BgDataTypes_Lib;
using BackgammonDiagram_Lib.Themes;
using System.Numerics;
using System.Text;

namespace BackgammonDiagram_Lib.Rendering;

public static class DiagramRenderer
{
    // Built-in rasterizer used when callers don't supply one. SkiaSharpRasterizer
    // construction is cheap (native Skia libs load lazily on first rasterize);
    // a single shared readonly instance is safe — rasterization is stateless.
    private static readonly SkiaSharpRasterizer s_defaultRasterizer = new();

    private const double TitleStripHeight = 22;

    /// <summary>
    /// Equity when the opponent passes a double. Always 1.0 because cube
    /// equities are normalised per cube — a pass forfeits exactly one cube
    /// by definition, independent of match score or cube value.
    /// </summary>
    private const double PassEquity = 1.0;

    // Play-panel layout constants. The cube panel has its own set below
    // because its content (Best/Actual banner + 4-row equity/loss table +
    // two 3-row pct tables + footer) is denser and visually unrelated.
    private const double PanelMargin = 6;
    private const double PanelLineHeight = 13;
    private const double PanelFontSize = 9;

    // Cube-panel layout constants. Sized so a full cube decision fills most
    // of the vertical panel space rather than hugging the top quarter.
    private const double CubePanelLineHeight = 20;
    private const double CubePanelFontSize = 14;       // row labels, equity/loss values, Best/Actual banner
    private const double CubePanelLabelFontSize = 12;  // column headers, pct rows, footer
    private const double CubePanelSectionGap = 10;     // vertical gap between banner / eq-loss / pct / footer

    // -----------------------------------------------------------------------
    //  Public API
    // -----------------------------------------------------------------------

    public static string RenderSvg(DiagramRequest request, DiagramOptions options)
    {
        var theme = options.Theme;
        bool panelOnLeft = request.PanelOnLeft;
        var (titleAction, titlePosition, titleSource) = ComposeTitleCells(request);
        // Strip visibility keys only off cols 1 and 3 — col 2 (SourceFile)
        // never forces the strip on its own, matching the pre-SourceFile
        // contract.
        bool hasTitle = titleAction.Length > 0 || titlePosition.Length > 0;
        double titleOffset = hasTitle ? TitleStripHeight : 0;
        var layout = BuildLayout(options.Aspect, titleOffset);

        double totalWidth = layout.TotalWidth(withPanel: true);
        double totalHeight = layout.BoardHeight + titleOffset;

        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {F(totalWidth)} {F(totalHeight)}" width="100%">""");

        if (hasTitle)
        {
            AppendTitleStrip(sb, totalWidth, titleOffset, theme, titleAction, titlePosition, titleSource);
            sb.AppendLine($"""  <g transform="translate(0,{F(titleOffset)})">""");
        }

        AppendBoard(sb, layout, theme, request, panelOnLeft, options.WatermarkImage);

        if (hasTitle)
            sb.AppendLine("  </g>");

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    public static byte[] RenderPng(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var svg = RenderSvg(request, options);
        int targetWidth = ResolveTargetWidth(options.Size);
        return (rasterizer ?? s_defaultRasterizer).Rasterize(svg, targetWidth);
    }

    public static byte[] RenderPdf(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var png = RenderPng(request, options, rasterizer);
        return PdfBuilder.Build([png]);
    }

    public static byte[] RenderPdf(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pngs = requests.Select(r => RenderPng(r, options, rasterizer)).ToList();
        return PdfBuilder.Build(pngs);
    }

    public static byte[] RenderPptx(DiagramRequest request, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var png = RenderPng(request, options, rasterizer);
        return PptxBuilder.Build([png]);
    }

    public static byte[] RenderPptx(IEnumerable<DiagramRequest> requests, DiagramOptions options,
        ISvgRasterizer? rasterizer = null)
    {
        var pngs = requests.Select(r => RenderPng(r, options, rasterizer)).ToList();
        return PptxBuilder.Build(pngs);
    }

    /// <summary>
    /// Returns hit-test rectangles for all clickable board regions.
    /// Coordinates are in SVG viewBox space matching <see cref="RenderSvg"/>
    /// output for the same request — including the analysis-panel allocation
    /// (Problem and Solution share dimensions per the "identical overall
    /// dimensions" invariant) and the panel side.
    /// </summary>
    public static BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options)
    {
        bool homeBoardOnRight = request.HomeBoardOnRight;
        bool panelOnLeft = request.PanelOnLeft;

        // Title strip, if present, offsets all board-relative Y coords in the
        // rendered SVG — hit regions must match.
        var (titleAction, titlePosition, _) = ComposeTitleCells(request);
        bool hasTitle = titleAction.Length > 0 || titlePosition.Length > 0;
        double titleOffset = hasTitle ? TitleStripHeight : 0;

        // Mirror RenderSvg's layout build exactly — same aspect-driven
        // PanelWidthOverride, same titleOffset — so hit regions and the
        // rendered SVG live in one coordinate system.
        var layout = BuildLayout(options.Aspect, titleOffset);

        double totalWidth = layout.TotalWidth(withPanel: true);
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

        // --- Bear-off trays: populated only when the render rule is satisfied. ---
        // The tray occupies the half of the left rail between the centered-cube
        // position and the turned-cube position — same region the renderer uses
        // for the stack, minus the bar-specific padding.
        var (onRollOff, opponentOff) = CountBorneOff(request.Position.Mop);
        double cubeSize = layout.LeftRailWidth * 0.7;
        HitRect? onRollTray = onRollOff is >= BearOffMinCount and <= BearOffMaxCount
            ? TrayHitRect(layout, panelOnLeft, titleOffset, cubeSize, atBottom: request.OnRollAtBottom)
            : null;
        HitRect? opponentTray = opponentOff is >= BearOffMinCount and <= BearOffMaxCount
            ? TrayHitRect(layout, panelOnLeft, titleOffset, cubeSize, atBottom: !request.OnRollAtBottom)
            : null;

        return new BoardHitRegions
        {
            ViewBox = new SvgViewBox(0, 0, totalWidth, totalHeight),
            Points = points,
            Bar = bar,
            Cube = cube,
            OnRollTray = onRollTray,
            OpponentTray = opponentTray,
        };
    }

    /// <summary>
    /// Hit-rectangle for one player's bear-off tray — spans left-rail width,
    /// top-to-bottom from the centered-cube edge to the turned-cube edge.
    /// </summary>
    private static HitRect TrayHitRect(BoardLayout layout, bool panelOnLeft,
        double titleOffset, double cubeSize, bool atBottom)
    {
        double centerCubeTop = layout.BoardHeight / 2 - cubeSize / 2;
        double centerCubeBottom = centerCubeTop + cubeSize;
        double turnedTop = TurnedCubeY(layout, cubeSize, atBottom);
        double turnedBottom = turnedTop + cubeSize;

        double y = atBottom ? centerCubeBottom : turnedBottom;
        double h = atBottom ? turnedTop - centerCubeBottom : centerCubeTop - turnedBottom;
        return new HitRect(
            layout.LeftRailX(panelOnLeft),
            y + titleOffset,
            layout.LeftRailWidth,
            h);
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

    /// <summary>
    /// Builds the board layout, widening the analysis panel (if necessary) to
    /// hit the aspect preset. Board geometry stays derived from CheckerRadius
    /// so checkers remain round; only PanelWidth is adjusted. Title strip
    /// height is included so the total SVG (title + board) matches the target
    /// aspect, not just the board portion.
    ///
    /// If the target aspect is narrower than the board alone (i.e. requires a
    /// negative panel width), the override is dropped and the intrinsic panel
    /// width is used. This is a safety floor for unusual CheckerRadius values;
    /// in practice the board is near-square so all common presets fit.
    /// </summary>
    private static BoardLayout BuildLayout(AspectPreset preset, double titleOffset)
    {
        var baseLayout = BoardLayout.Default;
        double? targetAspect = preset switch
        {
            AspectPreset.Widescreen16x9 => 16.0 / 9.0,
            AspectPreset.Standard4x3    => 4.0 / 3.0,
            _                           => null,
        };
        if (targetAspect is not double aspect)
            return baseLayout;

        double totalHeight = baseLayout.BoardHeight + titleOffset;
        double desiredTotalWidth = totalHeight * aspect;
        double desiredPanelWidth = desiredTotalWidth - baseLayout.BoardWidth;
        if (desiredPanelWidth <= 0)
            return baseLayout;

        return baseLayout with { PanelWidthOverride = desiredPanelWidth };
    }

    // -----------------------------------------------------------------------
    //  Title strip
    // -----------------------------------------------------------------------

    /// <summary>
    /// Composes the three title-strip cells from the request. Column 1
    /// (left edge of the full diagram, left-anchored) is the action text —
    /// "{dice} to play" for checker decisions, "Cube Action?" for cube
    /// decisions. Column 2 (strip centre, centre-anchored) is the
    /// SourceFile stem (filename minus its last extension) when
    /// Descriptive.SourceFile is populated. Column 3 (right edge,
    /// right-anchored) is "Position {N}" when PositionNumber is set. A
    /// cell is empty when its source is absent; the strip itself shows
    /// only when col 1 or col 3 has content — col 2 alone never forces
    /// the strip on, matching the pre-SourceFile contract.
    /// </summary>
    private static (string Action, string Position, string Source) ComposeTitleCells(DiagramRequest request)
    {
        string action;
        if (request.Decision.IsCube)
        {
            action = "Cube Action?";
        }
        else
        {
            var dice = request.Decision.Dice;
            // Defensive — a malformed checker-decision with non-standard dice
            // values contributes no action text rather than "0-0 to play".
            action = (dice.Count == 2 && dice[0] >= 1 && dice[1] >= 1)
                ? $"{dice[0]}-{dice[1]} to play"
                : string.Empty;
        }
        string position = request.PositionNumber is int n ? $"Position {n}" : string.Empty;
        string source = request.Descriptive.SourceFile is string s && s.Length > 0
            ? StripLastExtension(s)
            : string.Empty;
        return (action, position, source);
    }

    /// <summary>
    /// Drops the last dot-extension from a filename, preserving any earlier
    /// dots. "abc.xg" → "abc"; "abc.weird.xg" → "abc.weird"; ".xg" → ".xg"
    /// (leading-dot-only inputs pass through rather than degenerate to "").
    /// </summary>
    private static string StripLastExtension(string filename)
    {
        int dot = filename.LastIndexOf('.');
        return dot > 0 ? filename[..dot] : filename;
    }

    private static void AppendTitleStrip(StringBuilder sb,
        double totalWidth, double height, ITheme theme,
        string action, string position, string source)
    {
        // Col 1: left edge of full diagram (left-anchored).
        // Col 2: centre of the strip (centre-anchored via text-anchor="middle").
        // Col 3: right edge of full diagram (right-anchored via text-anchor="end").
        const double edgeMargin = 8;
        double actionX   = edgeMargin;
        double sourceX   = totalWidth / 2;
        double positionX = totalWidth - edgeMargin;
        string bg = theme.PanelBackgroundColor;
        string textColor = ContrastText(bg);
        double textY = height / 2;
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(totalWidth)}" height="{F(height)}" fill="{bg}"/>""");
        if (action.Length > 0)
            sb.AppendLine($"""  <text x="{F(actionX)}" y="{F(textY)}" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(action)}</text>""");
        if (source.Length > 0)
            sb.AppendLine($"""  <text x="{F(sourceX)}" y="{F(textY)}" text-anchor="middle" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(source)}</text>""");
        if (position.Length > 0)
            sb.AppendLine($"""  <text x="{F(positionX)}" y="{F(textY)}" text-anchor="end" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(position)}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Board
    // -----------------------------------------------------------------------

    private static void AppendBoard(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, bool panelOnLeft, byte[]? watermarkImage)
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
        if (watermarkImage is not null)
            AppendWatermark(sb, layout, effectivePanelOnLeft, watermarkImage);
        AppendCheckers(sb, layout, theme, request, effectivePanelOnLeft);
        if (!request.Decision.IsCube)
            AppendDice(sb, layout, theme, request, effectivePanelOnLeft);
        AppendPointNumbers(sb, layout, theme, effectivePanelOnLeft, homeBoardOnRight);
        AppendTopRail(sb, layout, theme, bx, request);
        AppendBottomRail(sb, layout, theme, bx, request);
        AppendBearOff(sb, layout, theme, bx, request);
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

        // Dice take the on-roll player's checker colour; pips contrast against it.
        string faceFill = theme.CheckerColorOnRoll;
        string pipFill  = ContrastText(faceFill);

        AppendDie(sb, d1X, dY, size, rx, request.Decision.Dice[0], faceFill, pipFill);
        AppendDie(sb, d2X, dY, size, rx, request.Decision.Dice[1], faceFill, pipFill);
    }

    private static void AppendDie(StringBuilder sb,
        double x, double y, double size, double rx, int value,
        string faceFill, string pipFill)
    {
        // Face
        sb.AppendLine($"""  <rect x="{F(x)}" y="{F(y)}" width="{F(size)}" height="{F(size)}" rx="{F(rx)}" fill="{faceFill}" stroke="#888" stroke-width="0.75"/>""");

        // Pip grid: 3×3 positions, each pip at fraction of die size
        // col: 0=left(0.25), 1=centre(0.5), 2=right(0.75)
        // row: 0=top(0.25),  1=middle(0.5), 2=bottom(0.75)
        double pipR = size * 0.09;

        var pips = PipPositions(value);
        foreach (var (col, row) in pips)
        {
            double px = x + size * (0.25 + col * 0.25);
            double py = y + size * (0.25 + row * 0.25);
            sb.AppendLine($"""  <circle cx="{F(px)}" cy="{F(py)}" r="{F(pipR)}" fill="{pipFill}"/>""");
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
    //  Watermark
    // -----------------------------------------------------------------------

    /// <summary>
    /// Alpha for the watermark — low enough to read as a background wash
    /// beneath points and checkers, high enough to be visible on a
    /// light-coloured board. The watermark asset carries per-pixel alpha
    /// from <see cref="Watermarks.BuildTransparentPng"/>; this multiplier
    /// tones the whole silhouette down to a quiet background mark.
    /// </summary>
    private const double WatermarkOpacity = 0.22;

    /// <summary>
    /// Maximum displayed watermark size as a fraction of
    /// <see cref="BoardLayout.MiddleGap"/>. Keeps the watermark fully
    /// contained within the middle-gap band between the two triangle rows,
    /// so triangle tips stay untouched.
    /// </summary>
    private const double WatermarkMiddleGapFraction = 0.9;

    /// <summary>
    /// Horizontal gap between the watermark and its neighbours (bar and
    /// the dice pair). Applied on both the bar side and the dice side so
    /// the watermark visibly floats in the strip rather than touching
    /// either.
    /// </summary>
    private const double WatermarkBarPadding = 3;

    /// <summary>
    /// Emits two watermark image elements — one per board-half — rotated 90°
    /// so the natural tops face each other across the bar. Both are sized
    /// to fit within the middle gap and pushed bar-adjacent, leaving the
    /// rest of the gap clear for dice. Called after points but before
    /// checkers/dice so the image sits behind the interactive content. The
    /// asset shipped via <see cref="Watermarks.Default"/> is square, so
    /// rotation doesn't change the bounding box; non-square inputs render
    /// with a width=height box and would shift aspect after rotation.
    /// </summary>
    private static void AppendWatermark(StringBuilder sb, BoardLayout layout,
        bool panelOnLeft, byte[] imageBytes)
    {
        string mime = GuessImageMime(imageBytes);
        // Base64-encode once per render; both halves reference the same
        // string literal in the emitted SVG (SVG size-dedup via <defs>/<use>
        // would save bytes but complicate the emission — skipped YAGNI).
        string dataUri = $"data:{mime};base64,{Convert.ToBase64String(imageBytes)}";

        // Horizontal strip available between the bar and the inner edge of
        // the dice pair in the on-roll half. Mirrors the dice sizing in
        // AppendDice (die size = r*1.6, intra-gap = 0.3 * size → half of
        // the dice pair = r * 1.84). Applied uniformly to both halves —
        // dice appear on only one side, but using the same size on both
        // keeps the watermark visually symmetric across the bar.
        double dicePairHalfWidth = layout.CheckerRadius * 1.84;
        double diceToBarClearance = layout.HalfWidth / 2 - dicePairHalfWidth;
        double horizontalMax = diceToBarClearance - 2 * WatermarkBarPadding;
        double verticalMax = layout.MiddleGap * WatermarkMiddleGapFraction;
        double size = Math.Min(horizontalMax, verticalMax);

        double cy = layout.MiddleY + layout.MiddleGap / 2;

        // Outer (left-of-bar) half: watermark hugs the bar's left edge
        // with WatermarkBarPadding between it and the bar.
        double cxLeft  = layout.BarX(panelOnLeft) - WatermarkBarPadding - size / 2;
        // Inner (right-of-bar) half: watermark hugs the bar's right edge.
        double cxRight = layout.BarX(panelOnLeft) + layout.BarWidth + WatermarkBarPadding + size / 2;

        AppendWatermarkImage(sb, dataUri, cxLeft,  cy, size, rotationDeg: 90);
        AppendWatermarkImage(sb, dataUri, cxRight, cy, size, rotationDeg: -90);
    }

    private static void AppendWatermarkImage(StringBuilder sb, string dataUri,
        double centreX, double centreY, double size, double rotationDeg)
    {
        double x = centreX - size / 2;
        double y = centreY - size / 2;
        sb.AppendLine($"""  <image href="{dataUri}" x="{F(x)}" y="{F(y)}" width="{F(size)}" height="{F(size)}" opacity="{F(WatermarkOpacity)}" transform="rotate({F(rotationDeg)} {F(centreX)} {F(centreY)})"/>""");
    }

    /// <summary>
    /// Sniffs the image MIME type from the first bytes of the array. Only
    /// PNG is explicitly detected; anything else falls back to JPEG, which
    /// is the format of the built-in asset.
    /// </summary>
    private static string GuessImageMime(byte[] bytes)
    {
        if (bytes.Length >= 4
            && bytes[0] == 0x89 && bytes[1] == 0x50
            && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";
        return "image/jpeg";
    }

    // -----------------------------------------------------------------------
    //  Cube
    // -----------------------------------------------------------------------

    // Vertical margin between a turned cube and the top/bottom of the left rail.
    // The turned cube is pushed against the rail edge so the interior of the rail
    // (between centered-cube position and turned-cube position) is free for the
    // bear-off tray.
    private const double TurnedCubeEdgeMargin = 8;

    private static void AppendCube(StringBuilder sb, BoardLayout layout, ITheme theme,
        double bx, DiagramRequest request)
    {
        double cubeSize = layout.LeftRailWidth * 0.7;
        double cubeX = bx + (layout.LeftRailWidth - cubeSize) / 2;
        double cubeY = request.Position.CubeOwner switch
        {
            CubeOwner.Centered => layout.BoardHeight / 2 - cubeSize / 2,
            CubeOwner.OnRoll => TurnedCubeY(layout, cubeSize, atBottom: request.OnRollAtBottom),
            CubeOwner.Opponent => TurnedCubeY(layout, cubeSize, atBottom: !request.OnRollAtBottom),
            _ => layout.BoardHeight / 2 - cubeSize / 2
        };
        sb.AppendLine($"""  <rect x="{F(cubeX)}" y="{F(cubeY)}" width="{F(cubeSize)}" height="{F(cubeSize)}" rx="3" fill="{theme.DiceColor}" stroke="#888" stroke-width="0.5"/>""");

        // Cube face text — special match states override the numeric cube size:
        //   * 1a-1a (match play AND both players one point away): face reads
        //     "Dmp" (double match point). The cube is dead at 1a-1a — no cube
        //     decisions arise — so a cube value would be misleading. Takes
        //     precedence over Crawford. Gated on match play because
        //     OnRollNeeds / OpponentNeeds are meaningless in money game
        //     (MatchLength == 0 is the sentinel).
        //   * Crawford: face reads "Cr". A Crawford game is played without the
        //     cube; the face marks that fact in place of a cube value.
        bool isDoubleMatchPoint = request.Descriptive.MatchLength > 0
                                  && request.Position.OnRollNeeds == 1
                                  && request.Position.OpponentNeeds == 1;
        string cubeText =
            isDoubleMatchPoint ? "Dmp" :
            request.Position.IsCrawford ? "Cr" :
            request.Position.CubeSize == 1 ? "64" :
            request.Position.CubeSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Font sizing — 0.55 is tuned for the common 1- and 2-character faces
        // ("2", "64", "Cr"). Longer strings ("Dmp", "128", "1024") get a
        // smaller ratio so the text doesn't crowd the cube edge.
        double fontSize = cubeSize * (cubeText.Length <= 2 ? 0.55 : 0.4);
        double textY = cubeY + cubeSize / 2 + fontSize * 0.35;
        string cubeTextColor = ContrastText(theme.DiceColor);
        sb.AppendLine($"""  <text x="{F(cubeX + cubeSize / 2)}" y="{F(textY)}" text-anchor="middle" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold" fill="{cubeTextColor}">{cubeText}</text>""");
    }

    /// <summary>
    /// Y-coordinate for a turned (owned) cube pinned against the top or bottom
    /// edge of the left rail, leaving the interior of the rail free for the
    /// bear-off tray.
    /// </summary>
    private static double TurnedCubeY(BoardLayout layout, double cubeSize, bool atBottom) =>
        atBottom
            ? layout.BoardHeight - TurnedCubeEdgeMargin - cubeSize
            : TurnedCubeEdgeMargin;

    // -----------------------------------------------------------------------
    //  Bear-off tray
    // -----------------------------------------------------------------------

    // Trigger band — render a player's bear-off stack only when at least one
    // checker is off and at least one is still on the board. Excludes both the
    // "game hasn't started" case (0 off) and the degenerate "all zeros" Mop
    // fixture (15 off / 0 on, which doesn't represent a meaningful position).
    private const int BearOffMinCount = 1;
    private const int BearOffMaxCount = 14;

    private static void AppendBearOff(StringBuilder sb, BoardLayout layout, ITheme theme,
        double bx, DiagramRequest request)
    {
        var (onRollOff, opponentOff) = CountBorneOff(request.Position.Mop);
        double cubeSize = layout.LeftRailWidth * 0.7;

        if (onRollOff >= BearOffMinCount && onRollOff <= BearOffMaxCount)
        {
            AppendBearOffStack(sb, layout, bx, request.OnRollAtBottom,
                count: onRollOff, cubeSize: cubeSize, fill: theme.CheckerColorOnRoll);
        }
        if (opponentOff >= BearOffMinCount && opponentOff <= BearOffMaxCount)
        {
            AppendBearOffStack(sb, layout, bx, !request.OnRollAtBottom,
                count: opponentOff, cubeSize: cubeSize, fill: theme.CheckerColorOpponent);
        }
    }

    /// <summary>
    /// Returns (onRollOff, opponentOff): count of checkers each player has
    /// borne off, computed as 15 minus the checkers currently on the board
    /// (points 1-24 plus the bar for that player).
    /// </summary>
    private static (int onRollOff, int opponentOff) CountBorneOff(IReadOnlyList<int> mop)
    {
        int onRollOn = 0;
        int opponentOn = 0;
        for (int i = 0; i < mop.Count; i++)
        {
            int v = mop[i];
            if (v > 0) onRollOn += v;
            else if (v < 0) opponentOn += -v;
        }
        return (15 - onRollOn, 15 - opponentOn);
    }

    /// <summary>
    /// Renders one player's bear-off stack: horizontal bars stacked vertically,
    /// centered in the left-rail region between the centered-cube slot and
    /// the player's turned-cube slot. Groups of five are visually separated
    /// by a small extra gap.
    /// </summary>
    private static void AppendBearOffStack(StringBuilder sb, BoardLayout layout, double bx,
        bool atBottom, int count, double cubeSize, string fill)
    {
        double d = layout.ColumnWidth;
        double barWidth = layout.LeftRailWidth * 0.55;
        double barHeight = d * 0.22;
        double intraGap = d * 0.071;       // 2 px at D=28
        double groupGapExtra = d * 0.143;  // 4 px at D=28

        double barX = bx + (layout.LeftRailWidth - barWidth) / 2;

        // Region bounded by the inner edge of the centered-cube slot and the
        // inner edge of this player's turned-cube slot.
        double centeredTop = layout.BoardHeight / 2 - cubeSize / 2;
        double centeredBottom = centeredTop + cubeSize;
        double turnedTop = TurnedCubeY(layout, cubeSize, atBottom);
        double turnedBottom = turnedTop + cubeSize;
        double regionTop = atBottom ? centeredBottom : turnedBottom;
        double regionBottom = atBottom ? turnedTop : centeredTop;
        double regionCenter = (regionTop + regionBottom) / 2;

        // Stack height: N bars + (N-1) intra-gaps + (groupsPassed) extra gaps.
        int groupsPassedAtEnd = (count - 1) / 5;
        double stackHeight = count * barHeight
                             + (count - 1) * intraGap
                             + groupsPassedAtEnd * groupGapExtra;
        double stackTop = regionCenter - stackHeight / 2;

        double slotPitch = barHeight + intraGap;
        for (int i = 0; i < count; i++)
        {
            int groupsPassed = i / 5;
            double y = stackTop + i * slotPitch + groupsPassed * groupGapExtra;
            sb.AppendLine($"""  <rect x="{F(barX)}" y="{F(y)}" width="{F(barWidth)}" height="{F(barHeight)}" fill="{fill}" stroke="#888" stroke-width="0.5"/>""");
        }
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

    // Play-panel layout constants. Matches the cube-panel type ramp for
    // deck-wide visual consistency.
    private const double PlayPanelLineHeight = 20;
    private const double PlayPanelFontSize   = 14;

    /// <summary>
    /// Render the checker-play candidate list. One line per play, in the
    /// order supplied (assumed equity-loss ascending — XG's native order).
    /// Columns from left to right:
    ///   * user-play marker, rank, move notation, equity, equity loss, depth.
    /// When the panel doesn't have room for every candidate, trim the tail
    /// but always include the user's play on the last line (with its real
    /// rank number) if it would otherwise be cut.
    /// </summary>
    private static void AppendPlayPanel(StringBuilder sb, double px, double pw, double ph,
        string textColor, string dimColor, DiagramRequest request)
    {
        _ = pw;  // panel width not used — numeric block anchors to moveX instead.
        var plays = request.Decision.Plays;
        if (plays.Count == 0) return;

        // Column x-anchors. Equity and Eq Loss sit just past a fixed move-text
        // reservation rather than pinned to the panel's right edge, so the
        // numeric block stays close to the move notation regardless of how
        // wide the panel is stretched by the aspect preset.
        double markerX = px + PanelMargin + 4;                     // "*" or blank
        double rankX   = markerX + PlayPanelFontSize * 0.9;        // rank number
        double moveX   = rankX   + PlayPanelFontSize * 2.2;        // move text (left-anchored)
        double equityX = moveX   + PlayPanelFontSize * 15;         // equity right-edge
        double lossX   = equityX + PlayPanelFontSize * 4.5;        // eq-loss right-edge
        double depthX  = lossX   + PlayPanelFontSize * 1.5;        // depth left-edge

        double y = PanelMargin;

        // Column-header row — applies only when there are plays to label.
        sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(y + PlayPanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}" fill="{dimColor}">Equity</text>""");
        sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(y + PlayPanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}" fill="{dimColor}">Eq Loss</text>""");
        sb.AppendLine($"""  <text x="{F(depthX)}" y="{F(y + PlayPanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}" fill="{dimColor}">Depth</text>""");
        y += PlayPanelLineHeight;

        double rowBudget = ph - PanelMargin;

        int fitCount = (int)Math.Max(0, (rowBudget - y) / PlayPanelLineHeight);
        int total = plays.Count;

        // Decide the visible index set. Normally [0 .. min(fitCount, total)).
        // If the user's play would be cut (userIndex >= fitCount), displace
        // the last otherwise-visible entry and append the user's play with
        // its real rank.
        int userIndex = request.Decision.UserPlayIndex;
        bool userNeedsRescue = userIndex >= 0
                               && userIndex < total
                               && userIndex >= fitCount
                               && fitCount > 0;

        int visibleCount = Math.Min(fitCount, total);
        for (int slot = 0; slot < visibleCount; slot++)
        {
            // Last slot swaps to the rescued user play when needed.
            int playIdx = (userNeedsRescue && slot == visibleCount - 1)
                ? userIndex
                : slot;

            var play = plays[playIdx];
            bool isUser = playIdx == userIndex;
            string marker = isUser ? "*" : string.Empty;
            string rank = $"{playIdx + 1}";
            string moveText = play.MoveNotation;

            // Italic flags a rank inversion in the equity-sorted source list:
            // a deeper analysis (higher DepthRank) ranked below a shallower one
            // for the immediately preceding candidate. Keyed off playIdx, not
            // slot, so a rescued user play carries the italic state from its
            // original position in the list rather than its displayed slot.
            // Applied to the Equity, Eq Loss, and Depth cells -- three cells
            // instead of one makes the rank-inversion cue stand out enough
            // to notice at a glance.
            bool italic = playIdx > 0 && plays[playIdx].DepthRank > plays[playIdx - 1].DepthRank;
            string italicAttr = italic ? """ font-style="italic" """ : " ";

            double lineY = y + PlayPanelLineHeight * 0.8;

            if (marker.Length > 0)
                sb.AppendLine($"""  <text x="{F(markerX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}" font-weight="bold" fill="{textColor}">{marker}</text>""");

            sb.AppendLine($"""  <text x="{F(rankX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}" fill="{textColor}">{Escape(rank)}</text>""");
            sb.AppendLine($"""  <text x="{F(moveX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}" fill="{textColor}">{Escape(moveText)}</text>""");
            sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(lineY)}" text-anchor="end" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}"{italicAttr}fill="{textColor}">{FormatEquity(play.Equity)}</text>""");
            // Blank Eq Loss cell for best plays: EquityLoss == 0.0 marks
            // membership in the best-equity equivalence class (per
            // PlayCandidate xmldoc); ties at zero all render blank uniformly.
            if (play.EquityLoss > 0)
                sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(lineY)}" text-anchor="end" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}"{italicAttr}fill="{dimColor}">{FormatEquityLoss(play.EquityLoss)}</text>""");
            if (!string.IsNullOrEmpty(play.DepthAbbreviation))
                sb.AppendLine($"""  <text x="{F(depthX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(PlayPanelFontSize)}"{italicAttr}fill="{dimColor}">{Escape(play.DepthAbbreviation)}</text>""");

            y += PlayPanelLineHeight;
        }
    }

    // -----------------------------------------------------------------------
    //  Cube analysis panel
    // -----------------------------------------------------------------------

    // Cube panel layout:
    //   Best Decision (two centered lines: "Best Decision" / "<doubler> / <opp>")
    //   Equity/Loss table (header + 4 rows: No Double, Double, Take, Pass)
    //   Percentages table for No Double (played-out stats)
    //   Percentages table for Take (played-out stats)
    //   Footer lines: Analysis Level, Pass Prob Justifying Dbl
    //
    // Two decisions are surfaced: the doubler's (Double vs. No Double) and
    // the opponent's (Take vs. Pass). Losses are mistake costs measured
    // against the decider's correct play.
    private static void AppendCubePanel(StringBuilder sb, double px, double pw, double ph,
        string textColor, string dimColor, DiagramRequest request)
    {
        // Width allocated for the right-hand numeric columns (equity/loss and
        // the Win/Gammon/BG pct columns), measured from the left label edge.
        // Fixed rather than panel-relative so the numeric block stays tight
        // against the row labels regardless of panel width — widening the
        // panel (e.g. under the 16:9 aspect preset) just leaves empty space
        // on the right instead of spreading the columns apart.
        const double NumericBlockWidth = 215;

        double y = PanelMargin;
        double textX = px + PanelMargin + 4;
        double numericRightX = textX + NumericBlockWidth;
        // Equity/Loss columns: Loss rightmost, Equity left of it.
        double lossX = numericRightX;
        double equityX = numericRightX - 70;

        var d = request.Decision;
        // Per-row equity values shown by the Equity/Loss table. These are
        // rendering values, not scoring values: DecisionData's cube helpers
        // expose only losses (DoublerActionError / ResponderActionError), so
        // the equity numbers themselves are still computed here. The "Double"
        // row shows what the doubler actually earns by doubling against a
        // rational opponent — the opponent picks the response that's least
        // bad for them, which minimises the doubler's equity.
        double nd = d.NoDoubleEquity;
        double dt = d.DoubleTakeEquity;
        double pass = PassEquity;
        double doubleEquity = Math.Min(dt, pass);

        // ── Best / Actual banner ───────────────────────────────────────
        // "Best" labels the correct verdict at the verdict-level — the only
        // place "Too Good to Double" is meaningful, since it's the
        // NoDouble↔TooGood distinction that lifts NoDouble from "fine" to
        // "leaving equity on the table". "Actual" is what the user played,
        // derived from UserDoubleError / UserTakeError (0 = correct, >0 =
        // wrong). Actuals work at the atomic level — they never carry the
        // "Too Good to Double" flavor because the user chose plain "Double"
        // or "No Double".
        string bestLine = VerdictBestLine(d.BestVerdict);
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{Escape(bestLine)}</text>""");
        y += CubePanelLineHeight;

        CubeAction bestDoublerAction = d.BestDoublerAction;
        CubeAction bestResponderAction = d.BestResponderAction;
        CubeAction? actualDoublerAction = d.UserDoubleError is double ude
            ? (ude > 0 ? OppositeDoublerAction(bestDoublerAction) : bestDoublerAction)
            : null;
        CubeAction? actualResponderAction = d.UserTakeError is double ute
            ? (ute > 0 ? OppositeResponderAction(bestResponderAction) : bestResponderAction)
            : null;
        if (actualDoublerAction != null || actualResponderAction != null)
        {
            // Same rule as the Best line: suppress the opp half whenever the
            // (actual) doubler action isn't "Double" — a stale UserTakeError
            // on a "No Double" play isn't a real take/pass decision. A null
            // doubler action (UserDoubleError absent) is *not* treated as
            // No Double, preserving the prior "?" / "Take" shape.
            string actualLine = "Actual: " + (actualDoublerAction is CubeAction ad ? ActionLabel(ad) : "?");
            if (actualResponderAction is CubeAction ar && actualDoublerAction != CubeAction.NoDouble)
                actualLine += " / " + ActionLabel(ar);
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{Escape(actualLine)}</text>""");
            y += CubePanelLineHeight;
        }
        y += CubePanelSectionGap;

        // ── Equity/Loss table ──────────────────────────────────────────
        // Column headers
        sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">Equity</text>""");
        sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">Loss</text>""");
        y += CubePanelLineHeight;

        // Per-row loss values come from the atomic-level helpers — each row
        // is the mistake cost for the decider of that row, independent of
        // any verdict-level NoDouble↔TooGood strategic-confusion override.
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: "No Double", equity: nd,           loss: d.DoublerActionError(CubeAction.NoDouble));
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: "Double",    equity: doubleEquity, loss: d.DoublerActionError(CubeAction.Double));
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: "Take",      equity: dt,           loss: d.ResponderActionError(CubeAction.Take));
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: "Pass",      equity: pass,         loss: d.ResponderActionError(CubeAction.Pass));

        y += CubePanelSectionGap;

        // ── Percentages tables (No Double, Take) ───────────────────────
        y = AppendPctTable(sb, textX, numericRightX, y, CubePanelLabelFontSize, textColor, dimColor,
            decisionLabel: "No Double",
            onRollWin: d.WinPctAfterNoDouble,
            onRollGammon: d.GammonPctAfterNoDouble,
            onRollBg: d.BgPctAfterNoDouble,
            oppWin: d.LosePctAfterNoDouble,
            oppGammon: d.LoseGammonPctAfterNoDouble,
            oppBg: d.LoseBgPctAfterNoDouble);

        y += CubePanelSectionGap;

        y = AppendPctTable(sb, textX, numericRightX, y, CubePanelLabelFontSize, textColor, dimColor,
            decisionLabel: "Take",
            onRollWin: d.WinPctAfterDoubleTake,
            onRollGammon: d.GammonPctAfterDoubleTake,
            onRollBg: d.BgPctAfterDoubleTake,
            oppWin: d.LosePctAfterDoubleTake,
            oppGammon: d.LoseGammonPctAfterDoubleTake,
            oppBg: d.LoseBgPctAfterDoubleTake);

        y += CubePanelSectionGap;

        // ── Footer lines ───────────────────────────────────────────────
        // Cube decisions show one analysis depth — no per-row column to
        // compress like the play panel — so the full CubeDepth string fits
        // and is more informative than the abbreviation. (Play panel keeps
        // PlayCandidate.DepthAbbreviation: per-play column space is tight.)
        if (!string.IsNullOrEmpty(d.CubeDepth))
        {
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">{Escape($"Analysis Level: {d.CubeDepth}")}</text>""");
            y += CubePanelLineHeight;
        }

        double probErr = d.ProbOfOpponentErrorJustifyingDouble;
        if (probErr > 0)
        {
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">{Escape($"Pass Justifying Dbl: {F1(probErr * 100)}%")}</text>""");
            y += CubePanelLineHeight;
        }
    }

    private static double AppendCubeRow(StringBuilder sb, double textX, double equityX, double lossX,
        double y, string textColor, string dimColor, string label, double equity, double loss)
    {
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" font-weight="bold" fill="{textColor}">{Escape(label)}</text>""");
        // Equity as its own text element so invariant-culture format tests can
        // assert ">+0.XXXX<" directly.
        sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{FormatEquity(equity)}</text>""");
        // Loss shown unconditionally — "0.0000" for the correct option, a
        // positive magnitude for the wrong option. Always four decimal places.
        sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{dimColor}">{FormatEquityLoss(loss)}</text>""");
        return y + CubePanelLineHeight + 3;
    }

    private static double AppendPctTable(StringBuilder sb, double textX, double rightX, double y,
        double fontSize, string textColor, string dimColor, string decisionLabel,
        double onRollWin, double onRollGammon, double onRollBg,
        double oppWin, double oppGammon, double oppBg)
    {
        // Right-anchored numeric cells. Columns count back from rightX so the
        // pct table's right edge stays flush with the equity/loss table above
        // regardless of panel width. The Win offset is sized so its header
        // gap to "Gammon" visually matches the Gammon→BG header gap (~33px):
        // "Gammon" is ~6 chars / ~42px at the label font, so winX needs to
        // sit ≈ 42 + 33 = 75 left of gammonX.
        double bgX     = rightX;
        double gammonX = rightX - 47;
        double winX    = rightX - 120;

        // Header row: decision label on the left, column headers right-anchored.
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold" fill="{textColor}">{Escape(decisionLabel)}</text>""");
        sb.AppendLine($"""  <text x="{F(winX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{dimColor}">Win</text>""");
        sb.AppendLine($"""  <text x="{F(gammonX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{dimColor}">Gammon</text>""");
        sb.AppendLine($"""  <text x="{F(bgX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{dimColor}">BG</text>""");
        y += CubePanelLineHeight;

        y = AppendPctRow(sb, textX, winX, gammonX, bgX, y, fontSize, textColor,
            label: "On-roll",  win: onRollWin, gammon: onRollGammon, bg: onRollBg);
        y = AppendPctRow(sb, textX, winX, gammonX, bgX, y, fontSize, textColor,
            label: "Opponent", win: oppWin,    gammon: oppGammon,    bg: oppBg);

        return y;
    }

    private static double AppendPctRow(StringBuilder sb, double textX,
        double winX, double gammonX, double bgX, double y, double fontSize, string color,
        string label, double win, double gammon, double bg)
    {
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">{Escape(label)}</text>""");
        sb.AppendLine($"""  <text x="{F(winX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">{F1(win * 100)}%</text>""");
        sb.AppendLine($"""  <text x="{F(gammonX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">{F1(gammon * 100)}%</text>""");
        sb.AppendLine($"""  <text x="{F(bgX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{color}">{F1(bg * 100)}%</text>""");
        return y + CubePanelLineHeight;
    }

    // -----------------------------------------------------------------------
    //  Cube label formatting
    // -----------------------------------------------------------------------
    //
    //  Label helpers map BgDataTypes_Lib's CubeAction / CubeVerdict values
    //  to their user-facing strings. Centralised here rather than scattered
    //  through AppendCubePanel so the four CubeVerdict cases of the Best
    //  line, the two-word "No Double" formatting, and the Doubler/Responder
    //  flips of the Actual line each have one definition site.

    private static string VerdictBestLine(CubeVerdict verdict) => verdict switch
    {
        CubeVerdict.NoDouble   => "Best:   No Double",
        CubeVerdict.DoubleTake => "Best:   Double / Take",
        CubeVerdict.DoublePass => "Best:   Double / Pass",
        CubeVerdict.TooGood    => "Best:   Too Good to Double",
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
    };

    private static string ActionLabel(CubeAction action) => action switch
    {
        CubeAction.NoDouble => "No Double",
        CubeAction.Double   => "Double",
        CubeAction.Take     => "Take",
        CubeAction.Pass     => "Pass",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };

    // Doubler-half flip (Double↔NoDouble) for deriving the Actual doubler
    // action from the Best doubler action when UserDoubleError > 0.
    private static CubeAction OppositeDoublerAction(CubeAction action) => action switch
    {
        CubeAction.Double   => CubeAction.NoDouble,
        CubeAction.NoDouble => CubeAction.Double,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action,
            "OppositeDoublerAction requires a doubler-half action (Double or NoDouble).")
    };

    // Responder-half flip (Take↔Pass) for deriving the Actual responder
    // action from the Best responder action when UserTakeError > 0.
    private static CubeAction OppositeResponderAction(CubeAction action) => action switch
    {
        CubeAction.Take => CubeAction.Pass,
        CubeAction.Pass => CubeAction.Take,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action,
            "OppositeResponderAction requires a responder-half action (Take or Pass).")
    };

    // -----------------------------------------------------------------------
    //  Equity formatting
    // -----------------------------------------------------------------------

    private static string FormatEquity(double equity)
    {
        string sign = equity >= 0 ? "+" : "";
        return sign + equity.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Equity loss is always a non-negative magnitude — a negative "loss" would
    // be a gain, and is not produced here. Bare 4-decimal invariant formatting.
    private static string FormatEquityLoss(double loss)
    {
        return loss.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
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