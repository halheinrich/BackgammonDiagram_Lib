using BgDataTypes_Lib;
using System.Diagnostics;
using BackgammonDiagram_Lib.Themes;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace BackgammonDiagram_Lib.Rendering;

/// <summary>
/// Renders a <see cref="DiagramRequest"/> to an SVG string, and exposes the
/// matching hit-test geometry via <see cref="GetHitRegions"/>. This type is
/// intentionally native-free: it lives in the core assembly that WASM /
/// SVG-only consumers depend on. Rasterization and the PNG / PDF / PPTX export
/// formats live in <c>DiagramRasterRenderer</c> in the
/// <c>BackgammonDiagram_Lib.ExportRaster</c> sibling. All members are
/// <see langword="static"/>; the renderer holds no instance state.
/// </summary>
public static class DiagramRenderer
{
    private const double TitleStripHeight = 22;

    /// <summary>
    /// Fixed horizontal reservation for the title strip's action column
    /// ("Cube Action?" / "{dice} to play"). The source (match name) cell is
    /// left-anchored at <c>edgeMargin + ActionColumnWidth</c> so it always
    /// starts at the same x, a clean gap to the right of the action text —
    /// chosen generously to clear the widest label ("Cube Action?" at 12px
    /// bold) without measuring font metrics. Left-anchoring (vs. the former
    /// centre anchor) keeps the source clear of the upper-right XGID label.
    /// </summary>
    private const double ActionColumnWidth = 110;

    // Play-panel layout constants. The cube panel has its own set below
    // because its content (Best/Actual banner + 4-row equity/loss table +
    // two 3-row pct tables + footer) is denser and visually unrelated.
    // PanelMargin is internal so the play-panel layout tests derive the
    // panel's right limit from it rather than pasting the number.
    internal const double PanelMargin = 6;
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

    /// <summary>
    /// Renders <paramref name="request"/> to a complete, self-contained SVG
    /// document string, honouring <paramref name="options"/> for size, theme,
    /// watermark, canvas preset, and the optional XGID label. The result
    /// shares its viewBox with <see cref="GetHitRegions"/> for the same inputs,
    /// so overlay coordinates align with the drawing. All numbers are formatted
    /// culture-invariantly (see <see cref="SvgFormat.Number"/>), so the output
    /// is valid regardless of the current thread culture.
    /// </summary>
    /// <param name="request">The board/match state and display flags to render.</param>
    /// <param name="options">Size, theme, watermark, canvas preset, and XGID options.</param>
    /// <returns>The rendered SVG document as a string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options"/> selects
    /// <see cref="AspectPreset.BoardOnly"/> for a
    /// <see cref="DiagramMode.Solution"/> request (see
    /// <see cref="PlanCanvas"/>).
    /// </exception>
    public static string RenderSvg(DiagramRequest request, DiagramOptions options)
    {
        var theme = options.Theme;
        bool panelOnLeft = request.PanelOnLeft;
        var plan = PlanCanvas(request, options);
        var layout = plan.Layout;
        double titleOffset = plan.TitleOffset;
        double totalWidth = plan.ViewBox.Width;

        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="{plan.ViewBox.ToAttributeString()}" width="100%">""");

        if (plan.HasTitle)
        {
            AppendTitleStrip(sb, totalWidth, titleOffset, theme, plan.TitleAction, plan.TitlePosition, plan.TitleSource);
            sb.AppendLine($"""  <g transform="translate(0,{F(titleOffset)})">""");
        }

        AppendBoard(sb, layout, theme, request, plan.Presentation, panelOnLeft, options.WatermarkImage);

        if (plan.HasTitle)
            sb.AppendLine("  </g>");

        // Opt-in baked XGID label. Emitted last so it draws over the board,
        // in the upper-right corner clear of the title strip. Default-off
        // (ShowXgid) keeps interactive consumers unchanged; the export formats
        // that overlay the XGID as real text force it off (see
        // DiagramRasterRenderer) so a baked label can't duplicate the overlay.
        // Only a decision's diagram has an XGID.
        if (options.ShowXgid && request.Xgid is { } xgid)
            AppendXgidLabel(sb, totalWidth, titleOffset, theme, xgid);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Returns hit-test rectangles for all clickable board regions.
    /// Coordinates are in SVG viewBox space matching <see cref="RenderSvg"/>
    /// output for the same request — including the analysis-panel allocation
    /// (present under every panel-bearing preset, where Problem and Solution
    /// share dimensions; absent under <see cref="AspectPreset.BoardOnly"/>,
    /// which drops the title strip's vertical offset with it) and the panel
    /// side. The shared <see cref="PlanCanvas"/> prologue makes that agreement
    /// structural rather than mirrored.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options"/> selects
    /// <see cref="AspectPreset.BoardOnly"/> for a
    /// <see cref="DiagramMode.Solution"/> request (see
    /// <see cref="PlanCanvas"/>).
    /// </exception>
    public static BoardHitRegions GetHitRegions(DiagramRequest request, DiagramOptions options)
    {
        bool homeBoardOnRight = request.HomeBoardOnRight;
        bool panelOnLeft = request.PanelOnLeft;

        var plan = PlanCanvas(request, options);
        var layout = plan.Layout;
        // Title strip, if present, offsets all board-relative Y coords in the
        // rendered SVG — hit regions must match.
        double titleOffset = plan.TitleOffset;

        // --- Points 1–24 ---
        var points = new Dictionary<int, HitRect>(24);

        for (int pt = 1; pt <= 24; pt++)
        {
            double cx = layout.ColumnCentreX(pt, panelOnLeft, homeBoardOnRight);
            double x = cx - layout.ColumnWidth / 2;
            double w = layout.ColumnWidth;

            // Cover the full checker-stack extent, not just the triangle: a
            // max-height stack is MaxStackHeight tall (6 checkers), which is
            // taller than the triangle (PointHeight, 5 checkers). Keying off
            // the same MaxStackCheckers bound the renderer caps the stack at
            // (see AppendCheckerStack) keeps the topmost checker clickable and
            // prevents render/hit drift. max() guards the degenerate case where
            // a future PointHeight exceeds the stack — the rect still covers the
            // triangle. The rect stays anchored at the point base and extends
            // toward the board centre (up for bottom points, down for top).
            double h = Math.Max(layout.PointHeight, layout.MaxStackHeight);

            double y;
            if (pt >= 13)
            {
                // Top points: base at the top edge, stack grows downward.
                y = layout.TopCheckerBaseY + titleOffset;
            }
            else
            {
                // Bottom points: base at the bottom edge, stack grows upward —
                // anchor the rect's bottom to the base and extend it up.
                double baseY = layout.BottomCheckerBaseY + layout.PointHeight;
                y = baseY - h + titleOffset;
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
        int onRollOff = request.Board.OnRollBorneOffCount;
        int opponentOff = request.Board.OpponentBorneOffCount;
        double cubeSize = layout.LeftRailWidth * 0.7;
        HitRect? onRollTray = onRollOff is >= OnRollTrayMinCount and <= BearOffMaxCount
            ? TrayHitRect(layout, panelOnLeft, titleOffset, cubeSize, atBottom: request.OnRollAtBottom)
            : null;
        HitRect? opponentTray = opponentOff is >= OpponentTrayMinCount and <= BearOffMaxCount
            ? TrayHitRect(layout, panelOnLeft, titleOffset, cubeSize, atBottom: !request.OnRollAtBottom)
            : null;

        // --- Dice: bounding box over the pair, from the same geometry source
        //     AppendDice draws from. Only a diagram that draws dice has the
        //     region, mirroring the AppendDice call site; a cube decision, or a
        //     board showing none, gets null. The board-space bounds are shifted
        //     by titleOffset like every other region so they line up with the
        //     rendered dice.
        HitRect? dice = null;
        if (plan.Presentation.Dice is not null)
        {
            var bounds = DicePairBounds(layout, request, panelOnLeft).Bounds;
            dice = bounds with { Y = bounds.Y + titleOffset };
        }

        return new BoardHitRegions
        {
            ViewBox = plan.ViewBox,
            Points = points,
            Bar = bar,
            Cube = cube,
            OnRollTray = onRollTray,
            OpponentTray = opponentTray,
            Dice = dice,
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
    /// The resolved canvas for one (request, options) pair: the request's
    /// presentation, the layout, the title-strip cells and vertical offset,
    /// and the viewBox they produce. Built exclusively by
    /// <see cref="PlanCanvas"/> and consumed by both <see cref="RenderSvg"/>
    /// and <see cref="GetHitRegions"/>, so the two public entry points
    /// describe the same canvas by construction — the single-sourcing that
    /// keeps overlay hit-testing aligned with the drawing.
    /// </summary>
    private readonly record struct CanvasPlan(
        DiagramPresentation Presentation,
        BoardLayout Layout,
        double TitleOffset,
        SvgViewBox ViewBox,
        string TitleAction,
        string TitlePosition,
        string TitleSource)
    {
        /// <summary>Whether the title strip renders (<see cref="StripShows"/>).</summary>
        public bool HasTitle => StripShows(TitleAction, TitlePosition, TitleSource);
    }

    /// <summary>
    /// Whether a title strip with these cells renders: when any of its three
    /// cells has content. The one statement of the rule — the canvas plan's
    /// vertical offset and its <see cref="CanvasPlan.HasTitle"/> both read it.
    /// </summary>
    private static bool StripShows(string action, string position, string source) =>
        action.Length > 0 || source.Length > 0 || position.Length > 0;

    /// <summary>
    /// Shared prologue of <see cref="RenderSvg"/> and
    /// <see cref="GetHitRegions"/>: validates the mode/preset combination,
    /// resolves the request's presentation, composes the title cells, builds
    /// the layout, and derives the viewBox. Under
    /// <see cref="AspectPreset.BoardOnly"/> no title cells are composed, which
    /// is what drops the strip: the canvas is the board proper alone.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options"/> selects
    /// <see cref="AspectPreset.BoardOnly"/> for a
    /// <see cref="DiagramMode.Solution"/> request. Solution mode exists to
    /// show the filled analysis panel, so a board-only Solution canvas is a
    /// contradiction — the ratified quiz-view model renders review at full
    /// canvas, always.
    /// </exception>
    private static CanvasPlan PlanCanvas(DiagramRequest request, DiagramOptions options)
    {
        bool boardOnly = options.Aspect == AspectPreset.BoardOnly;
        if (boardOnly && request.Mode == DiagramMode.Solution)
            throw new ArgumentException(
                $"{nameof(AspectPreset)}.{nameof(AspectPreset.BoardOnly)} requires " +
                $"{nameof(DiagramMode)}.{nameof(DiagramMode.Problem)}: Solution mode " +
                "exists to show the filled analysis panel.",
                nameof(options));

        var presentation = DiagramPresentation.Of(request);

        // Board-only leaves the title cells uncomposed — the single gate for
        // the strip's absence. Everything downstream falls out of it with no
        // second branch: HasTitle is false, so the offset is zero, RenderSvg
        // skips the strip and its translate group, and the viewBox height is
        // the board proper's.
        (string titleAction, string titlePosition, string titleSource) = boardOnly
            ? (string.Empty, string.Empty, string.Empty)
            : ComposeTitleCells(request, presentation);
        double titleOffset = StripShows(titleAction, titlePosition, titleSource) ? TitleStripHeight : 0;
        var layout = BuildLayout(options.Aspect, titleOffset);

        // SvgViewBox.ToAttributeString is the one place the rendered attribute
        // value is assembled; GetHitRegions returns this same instance.
        var viewBox = new SvgViewBox(0, 0, layout.TotalWidth, layout.BoardHeight + titleOffset);
        return new CanvasPlan(presentation, layout, titleOffset, viewBox, titleAction, titlePosition, titleSource);
    }

    /// <summary>
    /// Builds the board layout for the preset. Board geometry stays derived
    /// from CheckerRadius so checkers remain round; the aspect presets adjust
    /// only PanelWidth to hit their target, and
    /// <see cref="AspectPreset.BoardOnly"/> drops the panel allocation instead
    /// (no panel, so no aspect targeting — the canvas takes the board's
    /// intrinsic aspect; its title offset is always zero, the strip being
    /// dropped with the panel). Title strip height is included so the total SVG
    /// (title + board) matches the target aspect, not just the board portion.
    ///
    /// If the target aspect is narrower than the board alone (i.e. requires a
    /// negative panel width), the override is dropped and the intrinsic panel
    /// width is used. This is a safety floor for unusual CheckerRadius values;
    /// in practice the board is near-square so all common presets fit.
    /// </summary>
    private static BoardLayout BuildLayout(AspectPreset preset, double titleOffset)
    {
        var baseLayout = BoardLayout.Default;
        if (preset == AspectPreset.BoardOnly)
            return baseLayout with { BoardOnly = true };

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
    /// Composes the three title-strip cells. Column 1 (left edge of the full
    /// diagram, left-anchored) is the presentation's action text — "{dice} to
    /// play" where dice are drawn, "Cube Action?" for a cube decision.
    /// Column 2 (left-anchored at a fixed offset just right of the action
    /// column — see <see cref="ActionColumnWidth"/>) is the presentation's
    /// source: a decision's source file stem, or a board's title. Column 3
    /// (right edge, right-anchored) is "Position {N}" when the request's
    /// PositionNumber is set. A cell is empty when its source is absent, and
    /// the strip shows when any cell has content.
    /// </summary>
    private static (string Action, string Position, string Source) ComposeTitleCells(
        DiagramRequest request, DiagramPresentation presentation)
    {
        string position = request.PositionNumber is int n
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Position {n}")
            : string.Empty;
        return (presentation.TitleAction, position, presentation.TitleSource);
    }

    private static void AppendTitleStrip(StringBuilder sb,
        double totalWidth, double height, ITheme theme,
        string action, string position, string source)
    {
        // Col 1: left edge of full diagram (left-anchored).
        // Col 2: source (match name), left-anchored at a fixed offset just
        //        right of the action column — see ActionColumnWidth.
        // Col 3: right edge of full diagram (right-anchored via text-anchor="end").
        const double edgeMargin = 8;
        double actionX   = edgeMargin;
        double sourceX   = edgeMargin + ActionColumnWidth;
        double positionX = totalWidth - edgeMargin;
        string bg = theme.PanelBackgroundColor;
        string textColor = ContrastText(bg);
        double textY = height / 2;
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(totalWidth)}" height="{F(height)}" fill="{bg}"/>""");
        if (action.Length > 0)
            sb.AppendLine($"""  <text x="{F(actionX)}" y="{F(textY)}" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(action)}</text>""");
        if (source.Length > 0)
            sb.AppendLine($"""  <text x="{F(sourceX)}" y="{F(textY)}" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(source)}</text>""");
        if (position.Length > 0)
            sb.AppendLine($"""  <text x="{F(positionX)}" y="{F(textY)}" text-anchor="end" dominant-baseline="central" font-family="sans-serif" font-size="12" font-weight="bold" fill="{textColor}">{Escape(position)}</text>""");
    }

    // -----------------------------------------------------------------------
    //  XGID label (opt-in baked overlay)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Draws the XGID as a right-aligned label in the upper-right corner of
    /// the canvas, just below the title strip (or the top edge when no strip
    /// is shown). Plain SVG <c>&lt;text&gt;</c> in a normal visible font; it
    /// sits over the top rail, so its colour contrasts that rail's
    /// background. Only emitted when <see cref="DiagramOptions.ShowXgid"/> is
    /// set and the XGID is non-empty (caller-gated).
    /// </summary>
    private static void AppendXgidLabel(StringBuilder sb, double totalWidth,
        double titleOffset, ITheme theme, string xgid)
    {
        const double edgeMargin = 8;
        const double fontSize = 11;
        double x = totalWidth - edgeMargin;
        double y = titleOffset + fontSize + 2;   // baseline just below the strip
        // The label overlays the top rail (Darken(BoardColor, 0.1)); match the
        // rail's own text-contrast rule so it stays legible across themes.
        string textColor = ContrastText(Darken(theme.BoardColor, 0.1));
        sb.AppendLine($"""  <text x="{F(x)}" y="{F(y)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{textColor}">{Escape(xgid)}</text>""");
    }

    // -----------------------------------------------------------------------
    //  Board
    // -----------------------------------------------------------------------

    private static void AppendBoard(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, DiagramPresentation presentation, bool panelOnLeft, ImmutableArray<byte>? watermarkImage)
    {
        bool effectivePanelOnLeft = panelOnLeft;
        bool homeBoardOnRight = request.HomeBoardOnRight;
        double bx = layout.BoardOffsetX(effectivePanelOnLeft);
        var board = request.Board;

        // The pip counts are the drawn board's, by BgDataTypes_Lib's one pip
        // rule through its public surface — never stated by a caller, never
        // computed by a rule of this library's.
        var pips = new BoardState(board);

        // Full canvas background — prevents transparent edges showing in PNG.
        // TotalWidth already accounts for the panel allocation (zero under a
        // board-only layout); under panel-bearing presets a Problem-mode render
        // allocates the panel region blank, and it needs this fill too.
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(layout.TotalWidth)}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");

        sb.AppendLine($"""  <rect x="{F(bx)}" y="0" width="{F(layout.BoardWidth)}" height="{F(layout.BoardHeight)}" fill="{theme.BoardColor}"/>""");

        AppendLeftRail(sb, layout, theme, bx);
        AppendBar(sb, layout, theme, bx);
        AppendPoints(sb, layout, theme, effectivePanelOnLeft, homeBoardOnRight);
        if (watermarkImage is { } image)
            AppendWatermark(sb, layout, effectivePanelOnLeft, image);
        AppendCheckers(sb, layout, theme, request, effectivePanelOnLeft);
        if (presentation.Dice is { } dice)
            AppendDice(sb, layout, theme, request, dice, effectivePanelOnLeft);
        AppendPointNumbers(sb, layout, theme, effectivePanelOnLeft, homeBoardOnRight);
        AppendTopRail(sb, layout, theme, bx, request, presentation, pips);
        AppendBottomRail(sb, layout, theme, bx, request, presentation, pips);
        AppendBearOff(sb, layout, theme, bx, request);
        AppendCube(sb, layout, theme, bx, request, presentation);
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
        DiagramRequest request, DiagramPresentation presentation, BoardState pips)
    {
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double railX = bx + layout.LeftRailWidth;
        double cy = layout.TopRailHeight / 2;

        sb.AppendLine($"""  <rect x="{F(railX)}" y="0" width="{F(railWidth)}" height="{F(layout.TopRailHeight)}" fill="{Darken(theme.BoardColor, 0.1)}"/>""");

        string topName = request.OnRollAtBottom ? presentation.OpponentLabel : presentation.OnRollLabel;
        string topPip = PipLabel(request.OnRollAtBottom ? pips.OpponentPipCount : pips.PipCount);

        string railBg = Darken(theme.BoardColor, 0.1);
        string railText = ContrastText(railBg);

        AppendRailLabels(sb, railX, railWidth, cy, railText, topName, topPip);
    }

    private static void AppendBottomRail(StringBuilder sb, BoardLayout layout, ITheme theme, double bx,
        DiagramRequest request, DiagramPresentation presentation, BoardState pips)
    {
        double railWidth = layout.BoardWidth - layout.LeftRailWidth - layout.RightRailWidth;
        double railX = bx + layout.LeftRailWidth;
        double cy = layout.BottomRailY + layout.BottomRailHeight / 2;

        sb.AppendLine($"""  <rect x="{F(railX)}" y="{F(layout.BottomRailY)}" width="{F(railWidth)}" height="{F(layout.BottomRailHeight)}" fill="{Darken(theme.BoardColor, 0.1)}"/>""");

        string bottomName = request.OnRollAtBottom ? presentation.OnRollLabel : presentation.OpponentLabel;
        string bottomPip = PipLabel(request.OnRollAtBottom ? pips.PipCount : pips.OpponentPipCount);

        string railBg = Darken(theme.BoardColor, 0.1);
        string railText = ContrastText(railBg);

        AppendRailLabels(sb, railX, railWidth, cy, railText, bottomName, bottomPip);
    }

    /// <summary>A rail's pip label: <c>"Pip: 167"</c>.</summary>
    private static string PipLabel(int pipCount) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Pip: {pipCount}");

    /// <summary>
    /// Inset, in px, of both rail labels from their rail's ends: the player
    /// label's left edge sits this far in from the rail's left end, the pip
    /// label's right edge this far in from its right end.
    /// </summary>
    internal const double RailLabelInset = 8;

    /// <summary>Font size of both rail labels, in px.</summary>
    internal const double RailLabelFontSize = 12;

    /// <summary>
    /// Emits one rail's two labels, vertically centred on
    /// <paramref name="cy"/>: the player label left-anchored at the rail's
    /// left end and the pip label right-anchored at its right end, each
    /// <see cref="RailLabelInset"/> in. The top and bottom rails differ only
    /// in <paramref name="cy"/> and the texts.
    /// </summary>
    private static void AppendRailLabels(StringBuilder sb, double railX, double railWidth,
        double cy, string fill, string playerLabel, string pipLabel)
    {
        AppendRailLabel(sb, railX + RailLabelInset, cy, anchorEnd: false, fill, playerLabel);
        AppendRailLabel(sb, railX + railWidth - RailLabelInset, cy, anchorEnd: true, fill, pipLabel);
    }

    /// <summary>
    /// Emits one rail label — the one place a rail <c>&lt;text&gt;</c> is
    /// written, so its family, size and weight are stated once. Both rail
    /// labels, the player label and the pip count, are bold on every surface
    /// (halheinrich/backgammon#229): unconditional, because a per-surface
    /// switch would be a second copy of a presentation rule.
    /// <paramref name="anchorEnd"/> right-anchors the text at
    /// <paramref name="x"/>; otherwise it starts there.
    /// </summary>
    private static void AppendRailLabel(StringBuilder sb, double x, double cy,
        bool anchorEnd, string fill, string text)
    {
        string anchor = anchorEnd ? """ text-anchor="end" """ : " ";
        sb.AppendLine($"""  <text x="{F(x)}" y="{F(cy)}" dominant-baseline="central"{anchor}font-family="sans-serif" font-size="{F(RailLabelFontSize)}" font-weight="bold" fill="{fill}">{Escape(text)}</text>""");
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
        var board = request.Board;

        // Points 1–24
        for (int pt = 1; pt <= 24; pt++)
        {
            int count = board[pt];
            if (count == 0) continue;

            bool onRoll = count > 0;
            int abs = Math.Abs(count);
            double cx = layout.ColumnCentreX(pt, panelOnLeft, request.HomeBoardOnRight);
            bool bottom = pt <= 12;  // points 1-12 stack upward from bottom

            AppendCheckerStack(sb, layout, theme, cx, abs, onRoll, bottom);
        }

        // On-roll bar (slot 25, always >= 0) — stacks in the bottom half of the bar
        int onRollBar = board[25];
        if (onRollBar > 0)
        {
            double cx = layout.BarCentreX(panelOnLeft);
            double anchorCy = layout.TopCheckerBaseY + layout.CheckerRadius
                              + 5 * layout.CheckerRadius * 2;
            AppendCheckerStack(sb, layout, theme, cx, onRollBar, onRoll: true,
                bottomHalf: false, anchorCy: anchorCy, labelAtBase: true);
        }

        // Opponent bar (slot 0, always <= 0) — stacks in the top half of the bar
        int opponentBar = board[0];
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
        int draw = Math.Min(abs, BoardLayout.MaxStackCheckers);
        bool capped = abs > BoardLayout.MaxStackCheckers;

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

    /// <summary>
    /// Geometry of the two-die pair. Both dice are <see cref="Size"/> square and
    /// share the top edge <see cref="Y"/>; the left die sits at <see cref="D1X"/>
    /// and the right at <see cref="D2X"/>, both in board space (no title offset).
    /// Single source of truth for dice placement: <see cref="AppendDice"/> draws
    /// from it and <see cref="GetHitRegions"/> sizes the dice hit-region from
    /// <see cref="Bounds"/>, so the hit region can never drift from the drawn
    /// dice (cf. the point-stack draw/hit drift behind finding 4).
    /// </summary>
    internal readonly record struct DicePairGeometry(double D1X, double D2X, double Y, double Size, double Rx)
    {
        /// <summary>Axis-aligned bounding box enclosing both dice, in board space.</summary>
        public HitRect Bounds => new(D1X, Y, D2X + Size - D1X, Size);
    }

    /// <summary>
    /// Computes the dice-pair geometry for the current request. Caller-gated to
    /// checker decisions — for cube decisions no dice are drawn and there is no
    /// dice hit-region (mirrors the <see cref="AppendDice"/> call site).
    /// </summary>
    internal static DicePairGeometry DicePairBounds(BoardLayout layout,
        DiagramRequest request, bool panelOnLeft)
    {
        double r = layout.CheckerRadius;
        double size = r * 1.6;          // die face size
        double gap = size * 0.3;        // gap between the two dice
        double rx = size * 0.15;        // corner radius

        // Dice sit in the middle gap, vertically centred.
        double cy = layout.MiddleY + layout.MiddleGap / 2;
        double pairW = size * 2 + gap;

        // Horizontal centre: right half when on-roll is at bottom, left half otherwise.
        double halfCx = request.OnRollAtBottom
            ? layout.InnerHalfX(panelOnLeft) + layout.HalfWidth / 2
            : layout.OuterHalfX(panelOnLeft) + layout.HalfWidth / 2;

        double d1X = halfCx - pairW / 2;          // left die top-left x
        double d2X = d1X + size + gap;            // right die top-left x
        double dY = cy - size / 2;                // top-left y (same for both)
        return new DicePairGeometry(d1X, d2X, dY, size, rx);
    }

    private static void AppendDice(StringBuilder sb, BoardLayout layout, ITheme theme,
        DiagramRequest request, DiceFaces faces, bool panelOnLeft)
    {
        var dice = DicePairBounds(layout, request, panelOnLeft);

        // Dice take the on-roll player's checker colour; pips contrast against it.
        string faceFill = theme.CheckerColorOnRoll;
        string pipFill  = ContrastText(faceFill);

        AppendDie(sb, dice.D1X, dice.Y, dice.Size, dice.Rx, faces.Left, faceFill, pipFill);
        AppendDie(sb, dice.D2X, dice.Y, dice.Size, dice.Rx, faces.Right, faceFill, pipFill);
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
    /// (see <see cref="Watermarks.Default"/>); this multiplier tones the
    /// whole silhouette down to a quiet background mark.
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
        bool panelOnLeft, ImmutableArray<byte> imageBytes)
    {
        string mime = GuessImageMime(imageBytes.AsSpan());
        // Base64-encode once per render; both halves reference the same
        // string literal in the emitted SVG (SVG size-dedup via <defs>/<use>
        // would save bytes but complicate the emission — skipped YAGNI).
        string dataUri = $"data:{mime};base64,{Convert.ToBase64String(imageBytes.AsSpan())}";

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
    private static string GuessImageMime(ReadOnlySpan<byte> bytes)
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
        double bx, DiagramRequest request, DiagramPresentation presentation)
    {
        double cubeSize = layout.LeftRailWidth * 0.7;
        double cubeX = bx + (layout.LeftRailWidth - cubeSize) / 2;
        double cubeY = presentation.CubeOwner switch
        {
            CubeOwner.Centered => layout.BoardHeight / 2 - cubeSize / 2,
            CubeOwner.OnRoll => TurnedCubeY(layout, cubeSize, atBottom: request.OnRollAtBottom),
            CubeOwner.Opponent => TurnedCubeY(layout, cubeSize, atBottom: !request.OnRollAtBottom),
            _ => throw new ArgumentOutOfRangeException(nameof(presentation), presentation.CubeOwner,
                "Every source of a presentation holds its cube owner to a defined value."),
        };
        sb.AppendLine($"""  <rect x="{F(cubeX)}" y="{F(cubeY)}" width="{F(cubeSize)}" height="{F(cubeSize)}" rx="3" fill="{theme.DiceColor}" stroke="#888" stroke-width="0.5"/>""");

        // The face text is the presentation's — a value, or a match state's
        // mark in its place (see DiagramPresentation).
        string cubeText = presentation.CubeFace;

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

    // Trigger bands — how many checkers off makes a player's bear-off tray
    // render (visual stack + hit-region). The two players differ at the lower
    // edge:
    //
    //  * On-roll tray: shown throughout play, from 0 off. At 0 off it draws an
    //    empty tray (count-0 stack — the rail region, no checkers) so the first
    //    checker can be borne off by clicking the tray. Without this the tray
    //    was missing at the start of a bear-off and TryBearOffMax was
    //    unreachable from a position with every checker on the board.
    //  * Opponent tray: display-only (no hit-region drives interaction), so it
    //    keeps the original "at least one off" floor — an empty opponent tray
    //    would be visual noise with nothing to click.
    //
    // Both share the upper bound, one short of BoardPosition.CheckersPerSide:
    // every checker off is the game over, which renders no tray. The counts
    // are the position's (BoardPosition.OnRollBorneOffCount and
    // OpponentBorneOffCount); the renderer derives neither.
    private const int OnRollTrayMinCount = 0;
    private const int OpponentTrayMinCount = 1;
    private const int BearOffMaxCount = BoardPosition.CheckersPerSide - 1;

    private static void AppendBearOff(StringBuilder sb, BoardLayout layout, ITheme theme,
        double bx, DiagramRequest request)
    {
        int onRollOff = request.Board.OnRollBorneOffCount;
        int opponentOff = request.Board.OpponentBorneOffCount;
        double cubeSize = layout.LeftRailWidth * 0.7;

        if (onRollOff >= OnRollTrayMinCount && onRollOff <= BearOffMaxCount)
        {
            AppendBearOffStack(sb, layout, bx, request.OnRollAtBottom,
                count: onRollOff, cubeSize: cubeSize, fill: theme.CheckerColorOnRoll);
        }
        if (opponentOff >= OpponentTrayMinCount && opponentOff <= BearOffMaxCount)
        {
            AppendBearOffStack(sb, layout, bx, !request.OnRollAtBottom,
                count: opponentOff, cubeSize: cubeSize, fill: theme.CheckerColorOpponent);
        }
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
        // Empty tray: the rail region is the tray, so an empty stack is simply
        // no bars. Return before the stack-height math (which would go negative
        // for count 0) so the count-0 case is an explicit, clean no-op.
        if (count <= 0)
            return;

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
        // Board-only canvas: no panel allocation at all — return before the
        // background rect so no zero-width panel element is emitted.
        if (layout.BoardOnly)
            return;

        double px = layout.PanelX(panelOnLeft);
        double pw = layout.PanelWidth;
        double ph = layout.BoardHeight;
        string panelBg = theme.PanelBackgroundColor;
        string panelText = ContrastText(panelBg);
        string dimText = Lighten(panelText, 0.3);

        // Panel background
        sb.AppendLine($"""  <rect x="{F(px)}" y="0" width="{F(pw)}" height="{F(ph)}" fill="{panelBg}"/>""");

        // Problem mode: blank panel. Solution mode is a decision's alone — the
        // request refuses it for a board — so a decision is always in hand
        // here.
        if (request.Mode != DiagramMode.Solution || request.Decision is not { } decision)
            return;

        decision.Switch(
            play => AppendPlayPanel(sb, px, pw, ph, panelText, dimText, play, request),
            cube => AppendCubePanel(sb, px, pw, panelText, dimText, cube));
    }

    // -----------------------------------------------------------------------
    //  Play analysis panel
    // -----------------------------------------------------------------------

    // Play-panel layout constants. Matches the cube-panel type ramp for
    // deck-wide visual consistency. The horizontal ones are internal so the
    // play-panel layout tests derive their expected anchors from them rather
    // than pasting numbers.
    // Row pitch. Independent of the font size: a panel set in a smaller size
    // keeps its rows where they are.
    private const double PlayPanelLineHeight = 20;

    /// <summary>
    /// The play panel's font size whenever its content fits at it — every
    /// render that fits is set in this size. See
    /// <see cref="LayOutPlayPanelColumns"/> for the shrink when it does not.
    /// </summary>
    internal const double PlayPanelFontSize = 14;

    /// <summary>
    /// The smallest font size the play panel shrinks to so its content fits;
    /// below it the move reservation's floor wins instead and the Depth column
    /// overruns (see <see cref="LayOutPlayPanelColumns"/>).
    /// </summary>
    internal const double PlayPanelMinimumFontSize = 11;

    /// <summary>
    /// Fixed inset, in px, from the panel's margin to the marker column. The
    /// one horizontal quantity in the play panel besides the margins that
    /// does not scale with the font size.
    /// </summary>
    internal const double PlayPanelMarkerInset = 4;

    /// <summary>Marker column to rank column, in em.</summary>
    internal const double PlayPanelRankOffsetEm = 0.9;

    /// <summary>Rank column to move-text column, in em.</summary>
    internal const double PlayPanelMoveOffsetEm = 2.2;

    /// <summary>
    /// The move-text reservation when the panel has room for it, in em: the
    /// distance from the move text's left edge to the Equity column's right
    /// edge. The numeric block sits just past this reservation rather than
    /// pinned to the panel's right edge, so it stays close to the move
    /// notation however wide the aspect preset stretches the panel. It yields
    /// only when the Depth column would otherwise run past the panel — see
    /// <see cref="LayOutPlayPanelColumns"/>.
    /// </summary>
    internal const double PlayPanelMoveReserveEm = 15;

    /// <summary>
    /// Width of the Eq Loss column, in em: the distance from the Equity
    /// column's right edge to the Eq Loss column's right edge (both
    /// right-anchored).
    /// </summary>
    internal const double PlayPanelLossColumnEm = 4.5;

    /// <summary>
    /// Inter-column gap, in em: from the Eq Loss column's right edge to the
    /// left-anchored Depth column, and the gap the move-reservation floor
    /// keeps between the longest move text and the widest Equity value.
    /// </summary>
    internal const double PlayPanelColumnGapEm = 1.5;

    // Column-header words. Named once because each is both emitted and
    // measured (the headers take part in their columns' width estimates).
    internal const string PlayPanelEquityHeader = "Equity";
    internal const string PlayPanelLossHeader   = "Eq Loss";
    internal const string PlayPanelDepthHeader  = "Depth";

    /// <summary>
    /// The Eq Loss cell of a candidate the ranking does not score. Under
    /// depth first, a candidate analysed at another depth from the best play
    /// that rates higher than it is not scored and has no error
    /// (<see cref="RankedPlay.IsScored"/>); its row never shows an error and
    /// never reads like the best play, whose cell is blank. The dash states
    /// "no error" in the column that shows errors.
    /// </summary>
    internal const string PlayPanelNotScoredMark = "—";

    /// <summary>
    /// Render the checker-play candidate list. One line per visible play, in
    /// the order of the request's ranking — <see cref="DiagramRequest.Ranking"/>,
    /// through the producer's <see cref="CheckerPlayDecisionData.RankedBy"/>,
    /// which decides the order, the rank numbers, the best play and every
    /// error (SPEC-scoring §2a) — less the candidates the request's
    /// <see cref="DiagramRequest.MaximumHiddenCandidateAnalysisLevel"/> hides
    /// (see <see cref="BuildDisplaySequence"/>). The candidates arrive in the
    /// analyser's stored order; this renderer ranks nothing itself.
    /// Columns from left to right:
    ///   * user-play marker, rank, move notation, equity, equity loss, depth.
    /// The marks are keyed to the candidate's stored index, so they follow
    /// candidates, not row positions, and the rank-inversion italics to its
    /// place in the ranking. When the panel doesn't have room for every
    /// candidate, trim the tail but always include the marked plays on the
    /// last lines (with their real rank numbers) if they would otherwise be
    /// cut; the best play is the ranking's first, so it is never cut.
    /// </summary>
    private static void AppendPlayPanel(StringBuilder sb, double px, double pw, double ph,
        string textColor, string dimColor, CheckerPlayDecision decision, DiagramRequest request)
    {
        var data = decision.Decision;
        var ranked = data.RankedBy(request.Ranking
            ?? throw new UnreachableException("A request presenting a decision states its ranking."));

        int? userIndex = data.UserPlayIndex;
        // The secondary mark is active only when set and distinct from the
        // primary. A coincident secondary collapses to a single *: the
        // renderer owns the "don't double-mark a row" rule so consumers can
        // pass both indices blindly. (The request holds a set one to the
        // candidates' range.)
        int? secondaryIndex = request.SecondaryPlayIndex is int secondary && secondary != userIndex
            ? secondary
            : null;

        // The display sequence — the ranking's order, after the request's
        // optional analysis-level ceiling (halheinrich/backgammon#66).
        List<RankedPlay> sequence = BuildDisplaySequence(
            ranked, request.MaximumHiddenCandidateAnalysisLevel, userIndex, secondaryIndex);

        // One font size and the column anchors for the whole panel — header
        // row and every play row alike (see LayOutPlayPanelColumns).
        var (fontSize, markerX, rankX, moveX, equityX, lossX, depthX) =
            LayOutPlayPanelColumns(px, pw, sequence);

        double y = PanelMargin;

        // Column-header row — a checker play always has candidates to label.
        sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(y + PlayPanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{dimColor}">{PlayPanelEquityHeader}</text>""");
        sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(y + PlayPanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" fill="{dimColor}">{PlayPanelLossHeader}</text>""");
        sb.AppendLine($"""  <text x="{F(depthX)}" y="{F(y + PlayPanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(fontSize)}" fill="{dimColor}">{PlayPanelDepthHeader}</text>""");
        y += PlayPanelLineHeight;

        double rowBudget = ph - PanelMargin;

        int fitCount = (int)Math.Max(0, (rowBudget - y) / PlayPanelLineHeight);

        // Decide the visible rows. Normally the first
        // min(fitCount, sequence.Count) entries of the display sequence. Up to
        // two plays are marked and must both stay visible: the primary (*) at
        // UserPlayIndex and the secondary (†) at SecondaryPlayIndex. Any
        // marked play whose display position falls beyond the cut
        // (position >= fitCount) is "rescued" by displacing a tail entry, so
        // it still shows with its real rank — so up to two tail rows may be
        // displaced (neither, one, or both). The best play needs no rescue:
        // it is the ranking's first, and the ceiling never hides it.
        int visibleCount = Math.Min(fitCount, sequence.Count);

        // In the ranking's order, so rescued rows read in rank order at the
        // foot of the panel. (fitCount == 0 leaves no room to rescue into.)
        var rescued = new List<RankedPlay>(2);
        if (fitCount > 0)
        {
            foreach (var row in sequence.Skip(fitCount))
            {
                if (row.Index == userIndex || row.Index == secondaryIndex)
                    rescued.Add(row);
            }
        }

        // Keep the top rows of the display sequence, then give the remaining
        // slots to the rescued plays. keepCount never goes negative; if more
        // plays need rescue than there are slots (a panel with room for only a
        // single row), show as many as fit rather than overflow the budget.
        int keepCount = Math.Max(0, visibleCount - rescued.Count);
        var visible = new List<RankedPlay>(visibleCount);
        visible.AddRange(sequence.Take(keepCount));
        visible.AddRange(rescued.Take(visibleCount - visible.Count));

        foreach (var row in visible)
        {
            var candidate = row.Candidate;
            int playIdx = row.Index;
            // Primary * wins over secondary †. They can't both be true (an
            // active secondary is a distinct index), but keep * authoritative.
            string marker = playIdx == userIndex ? "*" : playIdx == secondaryIndex ? "†" : string.Empty;
            string rank = row.Rank.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Italic flags a rank inversion in the ranking's order: a deeper
            // analysis (higher DepthRank) ranked below a shallower one — the
            // candidate the ranking places immediately before it. Keyed off
            // the candidate's place in the ranking, not its displayed slot, so
            // a rescued user play carries the italic state from its place in
            // the ranking, and a row after hidden ones compares with its
            // neighbour in the ranking, hidden or not. Depth first orders by
            // depth, so no row is inverted under it; under equity, a deeper
            // analysis rating below a shallower one is. A depth not recorded
            // compares with nothing (its rank is null): it is not shallower
            // than a recorded one, only unrecorded. Applied to the Equity, Eq
            // Loss, and Depth cells -- three cells instead of one makes the
            // cue stand out enough to notice at a glance. Italic composes with
            // the bold weight the two numeric columns carry (see below): they
            // are independent attributes, so an inverted row reads
            // bold-italic.
            int place = row.Rank - 1;
            bool italic = place > 0 && candidate.DepthRank > ranked[place - 1].Candidate.DepthRank;
            string italicAttr = italic ? """ font-style="italic" """ : " ";

            double lineY = y + PlayPanelLineHeight * 0.8;

            if (marker.Length > 0)
                sb.AppendLine($"""  <text x="{F(markerX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold" fill="{textColor}">{marker}</text>""");

            sb.AppendLine($"""  <text x="{F(rankX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(fontSize)}" fill="{textColor}">{Escape(rank)}</text>""");
            sb.AppendLine($"""  <text x="{F(moveX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(fontSize)}" fill="{textColor}">{Escape(candidate.Notation)}</text>""");
            // Bold on the two numeric columns: the equity figures are what a
            // reader scans the panel for, and at the play-panel size they get
            // lost against the move notation. Values only -- the "Equity" and
            // "Eq Loss" column headers stay at normal weight, as do the rank,
            // move-notation, and Depth cells. (The marker cell above is bold
            // under its own, older rule.)
            sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(lineY)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold"{italicAttr}fill="{textColor}">{EquityDisplay.FormatEquity(candidate.Equity)}</text>""");
            // The Eq Loss cell is the ranking's error: blank where it counts
            // as zero (the best play, any play tying it, and any whose error
            // shows as 0.0000), so a blank cell and a correct play coincide;
            // the error, through the shared display, where it does not; and
            // the not-scored mark where the ranking gives none.
            string? loss = row.Error switch
            {
                null => PlayPanelNotScoredMark,
                double error when EquityDisplay.CountsAsZero(error) => null,
                double error => EquityDisplay.FormatLoss(error),
            };
            if (loss is not null)
                sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(lineY)}" text-anchor="end" font-family="sans-serif" font-size="{F(fontSize)}" font-weight="bold"{italicAttr}fill="{dimColor}">{Escape(loss)}</text>""");
            if (candidate.DepthAbbreviation is { } depth)
                sb.AppendLine($"""  <text x="{F(depthX)}" y="{F(lineY)}" font-family="sans-serif" font-size="{F(fontSize)}"{italicAttr}fill="{dimColor}">{Escape(depth)}</text>""");

            y += PlayPanelLineHeight;
        }
    }

    /// <summary>
    /// The play panel's resolved horizontal layout: the one font size every
    /// text in the panel is set in, and the column x-anchors at that size
    /// (marker, rank and move text left edges; Equity and Eq Loss right
    /// edges; Depth left edge).
    /// </summary>
    private readonly record struct PlayPanelColumns(
        double FontSize, double MarkerX, double RankX, double MoveX,
        double EquityX, double LossX, double DepthX);

    /// <summary>
    /// Lays out the play panel's columns for the plays
    /// <paramref name="sequence"/> shows. The numeric block (Equity, Eq Loss,
    /// Depth) hangs off a move-text reservation — the distance from the move
    /// text's left edge to the Equity column's right edge — rather than the
    /// panel's right edge. Two estimated widths govern it
    /// (<see cref="EstimateTextWidth"/>, each cell at the weight it is
    /// emitted in):
    /// <list type="bullet">
    /// <item><description><em>room</em> — the reservation that ends the Depth
    /// column (its widest cell, header included) exactly at the panel's right
    /// edge less <see cref="PanelMargin"/>;</description></item>
    /// <item><description><em>floor</em> — the longest move text, plus
    /// <see cref="PlayPanelColumnGapEm"/>, plus the widest Equity cell
    /// (header or value): Equity is right-anchored at the reservation's end,
    /// so all three live inside it.</description></item>
    /// </list>
    /// Three steps, in order:
    /// <list type="number">
    /// <item><description><b>Fit at <see cref="PlayPanelFontSize"/>.</b> When
    /// the floor fits in the room, the size is 14 and the reservation is
    /// <c>min(<see cref="PlayPanelMoveReserveEm"/>, room)</c> — the full
    /// 15 em whenever there is room for it, so every render that fits is
    /// unchanged. Nothing overlaps and nothing overruns.</description></item>
    /// <item><description><b>Shrink to fit.</b> Otherwise the size is the
    /// largest at which the floor equals the room, solved in closed form, not
    /// below <see cref="PlayPanelMinimumFontSize"/>. At that size nothing
    /// overlaps and nothing overruns.</description></item>
    /// <item><description><b>At the minimum, the floor wins.</b> If even the
    /// minimum size cannot fit, the reservation is the floor: move text and
    /// Equity never collide, and the Depth column overruns by the least amount
    /// possible (floor minus room).</description></item>
    /// </list>
    /// All three steps are the one expression
    /// <c>reserve = max(floor, min(15 em, room))</c> at the chosen size.
    /// <para>
    /// Where not even an empty move text fits at 14 (the room is below the gap
    /// plus the widest Equity cell) nothing here can help: that is the narrow
    /// presets' (<see cref="AspectPreset.Natural"/>,
    /// <see cref="AspectPreset.Standard4x3"/>) pre-existing overflow, which
    /// halheinrich/backgammon#253 owns. There the size stays 14 and the
    /// reservation the full <see cref="PlayPanelMoveReserveEm"/>, so their
    /// output is exactly what it was before any of this could apply.
    /// </para>
    /// </summary>
    private static PlayPanelColumns LayOutPlayPanelColumns(double px, double pw,
        IReadOnlyList<RankedPlay> sequence)
    {
        // Widths per unit of font size (EstimateTextWidth is linear in the
        // size): the widest cell each measured column shows, each at the
        // weight AppendPlayPanel emits it in — the headers, move text and
        // Depth regular, the Equity values bold.
        double depthPerSize  = EstimateTextWidth(PlayPanelDepthHeader, 1, TextWeight.Regular);
        double equityPerSize = EstimateTextWidth(PlayPanelEquityHeader, 1, TextWeight.Regular);
        double movePerSize   = 0;
        foreach (var row in sequence)
        {
            var play = row.Candidate;
            depthPerSize  = Math.Max(depthPerSize, EstimateTextWidth(play.DepthAbbreviation ?? string.Empty, 1, TextWeight.Regular));
            equityPerSize = Math.Max(equityPerSize, EstimateTextWidth(EquityDisplay.FormatEquity(play.Equity), 1, TextWeight.Bold));
            movePerSize   = Math.Max(movePerSize, EstimateTextWidth(play.Notation, 1, TextWeight.Regular));
        }
        double equityCellPerSize = PlayPanelColumnGapEm + equityPerSize;
        double floorPerSize      = movePerSize + equityCellPerSize;

        // Fixed terms: the left inset (margin + marker inset) and the right
        // margin. Everything between them scales with the font size.
        double markerX = px + PanelMargin + PlayPanelMarkerInset;
        double limit   = px + pw - PanelMargin;

        double size = PlayPanelFontSize;
        double room = Room(size);

        // Not even an empty move text fits at 14: the narrow presets'
        // pre-existing overflow (halheinrich/backgammon#253). Left exactly as
        // it was — no shrink, the full reservation.
        if (room < size * equityCellPerSize)
            return At(size, size * PlayPanelMoveReserveEm);

        if (size * floorPerSize > room)
        {
            // The size at which floor == room. From markerX to the limit, the
            // row reads: rank offset + move offset + floor (move text, gap,
            // Equity) + Eq Loss column + gap + Depth — every term size × a
            // per-size width, so the size is the span over their sum. The
            // branch condition puts the solution below 14; only the minimum
            // needs clamping.
            double perSize = PlayPanelRankOffsetEm + PlayPanelMoveOffsetEm + floorPerSize
                             + PlayPanelLossColumnEm + PlayPanelColumnGapEm + depthPerSize;
            size = Math.Max(PlayPanelMinimumFontSize, (limit - markerX) / perSize);
            room = Room(size);
        }

        double reserve = Math.Max(size * floorPerSize, Math.Min(size * PlayPanelMoveReserveEm, room));
        return At(size, reserve);

        // The reservation that ends the Depth column exactly at the limit.
        double Room(double s) =>
            limit - s * depthPerSize - s * PlayPanelColumnGapEm - s * PlayPanelLossColumnEm - At(s, 0).MoveX;

        PlayPanelColumns At(double s, double reserveWidth)
        {
            double rankX   = markerX + s * PlayPanelRankOffsetEm;     // rank number
            double moveX   = rankX   + s * PlayPanelMoveOffsetEm;     // move text (left-anchored)
            double equityX = moveX   + reserveWidth;                  // equity right-edge
            double lossX   = equityX + s * PlayPanelLossColumnEm;     // eq-loss right-edge
            double depthX  = lossX   + s * PlayPanelColumnGapEm;      // depth left-edge
            return new PlayPanelColumns(s, markerX, rankX, moveX, equityX, lossX, depthX);
        }
    }

    /// <summary>
    /// The play panel's display sequence: the candidates in the ranking's
    /// order (<paramref name="ranked"/>, the producer's), less those the
    /// request's analysis-level ceiling (halheinrich/backgammon#66) hides.
    /// With no ceiling this is the ranking's order, complete.
    /// <para>
    /// A candidate is hidden exactly when its numbers came from a direct
    /// evaluation (<see cref="AnalysisMode.Evaluation"/>) whose
    /// <see cref="PlayCandidate.AnalysisLevel"/> is at or below the ceiling in
    /// <see cref="AnalysisLevel"/>'s declared order — the producer's contract,
    /// which states the order and not this renderer. Inclusive on the hide
    /// side, so the top level is a usable ceiling and "show only rollouts" is
    /// expressible. Never hidden: a rollout-family candidate (its level is the
    /// rollout's inner one, not the analysis's own depth); a candidate whose
    /// level is not recorded (<see cref="AnalysisLevel.Unknown"/> sits outside
    /// the order, and its zero value would compare at or below every ceiling,
    /// so the guard is explicit); and the ranking's best play and both marked
    /// rows, whatever their depth — see the contract on
    /// <see cref="DiagramRequest.MaximumHiddenCandidateAnalysisLevel"/>.
    /// </para>
    /// </summary>
    /// <param name="ranked">The candidates under the request's ranking.</param>
    /// <param name="ceiling">The request's ceiling, or null to hide nothing.</param>
    /// <param name="userIndex">The user's play, exempt from the ceiling.</param>
    /// <param name="activeSecondaryIndex">The secondary play when its mark is
    /// active, exempt from the ceiling.</param>
    private static List<RankedPlay> BuildDisplaySequence(
        RankedPlays ranked, AnalysisLevel? ceiling, int? userIndex, int? activeSecondaryIndex)
    {
        var sequence = new List<RankedPlay>(ranked.Count);
        foreach (var row in ranked)
        {
            bool hidden = ceiling is AnalysisLevel level
                && row.Candidate.AnalysisMode == AnalysisMode.Evaluation
                && row.Candidate.AnalysisLevel != AnalysisLevel.Unknown
                && row.Candidate.AnalysisLevel <= level
                && row != ranked.Best
                && row.Index != userIndex
                && row.Index != activeSecondaryIndex;
            if (!hidden)
                sequence.Add(row);
        }
        return sequence;
    }

    // -----------------------------------------------------------------------
    //  Cube analysis panel
    // -----------------------------------------------------------------------

    // Cube panel layout:
    //   Best / Actual banner
    //   Equity/Loss table (header + 4 rows: No double, Double, Take, Pass)
    //   Percentages table for No double (played-out stats)
    //   Percentages table for Take (played-out stats)
    //   Footer line: Analysis Level
    //
    // The banner speaks in cube answers (SPEC-scoring §3), labelled at their
    // decision by CubeLabels. The table speaks in actions: two decisions are
    // surfaced, the doubler's (Double vs. No double) and the opponent's (Take
    // vs. Pass), and each row's loss is its action's error against the best
    // action of its half. Those errors are the analysis's facts, not what an
    // answer costs: answer costs are the producer's, CubeDecision.CostOf.
    private static void AppendCubePanel(StringBuilder sb, double px, double pw,
        string textColor, string dimColor, CubeDecision cube)
    {
        CubeDecisionData d = cube.Decision;

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

        // ── Best / Actual banner ───────────────────────────────────────
        // Both lines name cube answers, each labelled at this decision by
        // CubeLabels; see CubeBestLines and CubeActualLine. The Best line's
        // lead is drawn on its own and its answers from one x past it, so a
        // wrapped line's continuations start exactly under the first answer;
        // every line the wrap adds moves everything below it down.
        double bestAnswersX = textX + CubeBestLineIndent;
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{Escape(CubeBestLineLead)}</text>""");
        foreach (string line in CubeBestLines(cube, px + pw - PanelMargin - bestAnswersX))
        {
            sb.AppendLine($"""  <text x="{F(bestAnswersX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{Escape(line)}</text>""");
            y += CubePanelLineHeight;
        }

        if (CubeActualLine(cube) is { } actual)
        {
            string actualLine = CubeActualLinePrefix + actual;
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{Escape(actualLine)}</text>""");
            y += CubePanelLineHeight;
        }
        y += CubePanelSectionGap;

        // ── Equity/Loss table ──────────────────────────────────────────
        // Column headers
        sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">Equity</text>""");
        sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">Loss</text>""");
        y += CubePanelLineHeight;

        // Each row shows its action's equity and its error, both the
        // producer's, from its one calculation (CubeDecisionData.ActionEquity):
        // the equity in the doubler's perspective — doubling's is the taker's
        // best response's — and the action's error against the best action of
        // its half. The renderer states neither the pass's value nor the rule
        // for doubling's equity, so each row's equity and error agree with
        // each other. They are the analysis's action facts, not what the
        // scoring charges: a cube answer's cost is the producer's
        // CubeDecision.CostOf, which adds the response the answer commits to
        // and, where gammons are possible, charges SPEC-scoring §3's two
        // conventions for misreadings that lose no equity
        // (halheinrich/backgammon#326).
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: CubeLabels.Label(CubeAction.NoDouble), equity: d.ActionEquity(CubeAction.NoDouble), loss: d.DoublerActionError(CubeAction.NoDouble));
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: CubeLabels.Label(CubeAction.Double),   equity: d.ActionEquity(CubeAction.Double),   loss: d.DoublerActionError(CubeAction.Double));
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: CubeLabels.Label(CubeAction.Take),     equity: d.ActionEquity(CubeAction.Take),     loss: d.TakerActionError(CubeAction.Take));
        y = AppendCubeRow(sb, textX, equityX, lossX, y, textColor, dimColor,
            label: CubeLabels.Label(CubeAction.Pass),     equity: d.ActionEquity(CubeAction.Pass),     loss: d.TakerActionError(CubeAction.Pass));

        y += CubePanelSectionGap;

        // ── Percentages tables (No double, Take) ───────────────────────
        y = AppendPctTable(sb, textX, numericRightX, y, CubePanelLabelFontSize, textColor, dimColor,
            decisionLabel: CubeLabels.Label(CubeAction.NoDouble),
            onRollWin: d.WinPctAfterNoDouble,
            onRollGammon: d.GammonPctAfterNoDouble,
            onRollBg: d.BgPctAfterNoDouble,
            oppWin: d.LosePctAfterNoDouble,
            oppGammon: d.LoseGammonPctAfterNoDouble,
            oppBg: d.LoseBgPctAfterNoDouble);

        y += CubePanelSectionGap;

        y = AppendPctTable(sb, textX, numericRightX, y, CubePanelLabelFontSize, textColor, dimColor,
            decisionLabel: CubeLabels.Label(CubeAction.Take),
            onRollWin: d.WinPctAfterDoubleTake,
            onRollGammon: d.GammonPctAfterDoubleTake,
            onRollBg: d.BgPctAfterDoubleTake,
            oppWin: d.LosePctAfterDoubleTake,
            oppGammon: d.LoseGammonPctAfterDoubleTake,
            oppBg: d.LoseBgPctAfterDoubleTake);

        y += CubePanelSectionGap;

        // ── Footer line ────────────────────────────────────────────────
        // Cube decisions show one analysis depth — no per-row column to
        // compress like the play panel — so the full depth label fits and is
        // more informative than the abbreviation. (Play panel keeps
        // PlayCandidate.DepthAbbreviation: per-play column space is tight.)
        // No depth recorded (null) draws no line.
        if (d.Depth is { } depth)
            sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelLabelFontSize)}" fill="{dimColor}">{Escape($"Analysis Level: {depth}")}</text>""");
    }

    private static double AppendCubeRow(StringBuilder sb, double textX, double equityX, double lossX,
        double y, string textColor, string dimColor, string label, double equity, double loss)
    {
        sb.AppendLine($"""  <text x="{F(textX)}" y="{F(y + CubePanelLineHeight * 0.8)}" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" font-weight="bold" fill="{textColor}">{Escape(label)}</text>""");
        // Equity as its own text element so invariant-culture format tests can
        // assert ">+0.XXXX<" directly.
        sb.AppendLine($"""  <text x="{F(equityX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{textColor}">{EquityDisplay.FormatEquity(equity)}</text>""");
        // Loss shown unconditionally, through the shared display: 0.0000 for
        // the correct option, and for any error that counts as zero.
        sb.AppendLine($"""  <text x="{F(lossX)}" y="{F(y + CubePanelLineHeight * 0.8)}" text-anchor="end" font-family="sans-serif" font-size="{F(CubePanelFontSize)}" fill="{dimColor}">{EquityDisplay.FormatLoss(loss)}</text>""");
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
    //  Best and Actual lines
    // -----------------------------------------------------------------------
    //
    //  Wording is not this renderer's to choose: every cube label it prints
    //  comes from CubeLabels, the library's one public label home
    //  (halheinrich/backgammon#185), which labels an answer at its decision.
    //  What lives here is only each line's shape.

    /// <summary>The Best line's lead, drawn on its own ahead of its answers.</summary>
    private const string CubeBestLineLead = "Best:";

    /// <summary>The gap from the Best line's lead to its first answer, in em.</summary>
    private const double CubeBestLineLeadGapEm = 0.5;

    /// <summary>
    /// Where the Best line's answers start, past its lead: the lead's
    /// estimated width (<see cref="EstimateTextWidth"/>) and the gap. Every
    /// line of a wrapped list starts here, so its answers align.
    /// </summary>
    private static double CubeBestLineIndent =>
        EstimateTextWidth(CubeBestLineLead, CubePanelFontSize, TextWeight.Regular)
        + CubePanelFontSize * CubeBestLineLeadGapEm;

    /// <summary>The Actual line's lead-in, ahead of <see cref="CubeActualLine"/>.</summary>
    private const string CubeActualLinePrefix = "Actual: ";

    /// <summary>
    /// Joins the answers the Best line lists. A wrapped line breaks after it,
    /// keeping its comma and dropping its space.
    /// </summary>
    private const string CubeBestLineSeparator = ", ";

    /// <summary>
    /// The Best line's answers, as drawn lines no wider than
    /// <paramref name="width"/>: the producer's
    /// <see cref="CubeDecision.ZeroCostAnswers"/>, in its order, each labelled
    /// at <paramref name="cube"/> through
    /// <see cref="CubeLabels.Label(CubeAnswer, CubeDecision)"/>, joined by
    /// <see cref="CubeBestLineSeparator"/> (SPEC-scoring §3, "The tie": "The
    /// review's Best line lists every answer whose cost counts as zero, so at
    /// a tie it lists them all").
    /// </summary>
    /// <remarks>
    /// <para>
    /// Which answers are listed is the producer's alone, stated on
    /// <see cref="CubeDecision.ZeroCostAnswers"/>: off an equity tie it can
    /// still hold two answers. The line never names the quiz user's answer:
    /// the verdict below the board does (Hal, 2026-10-01).
    /// </para>
    /// <para>
    /// <b>The wrap</b> (Hal, 2026-10-01: "Yes, wrap it"). The answers fill a
    /// line while it fits <paramref name="width"/> by
    /// <see cref="EstimateTextWidth"/>, its trailing comma included; an
    /// answer that would not fit starts the next line. A line breaks only
    /// after a separator, between two answers, never inside a label, and
    /// every answer keeps its full label. A single label wider than the
    /// panel takes a line of its own and overruns it: the narrow presets'
    /// overflow is halheinrich/backgammon#253's.
    /// </para>
    /// </remarks>
    private static List<string> CubeBestLines(CubeDecision cube, double width)
    {
        var labels = cube.ZeroCostAnswers.Select(answer => CubeLabels.Label(answer, cube)).ToList();
        string comma = CubeBestLineSeparator.TrimEnd();

        var lines = new List<string>();
        string line = labels[0];
        for (int i = 1; i < labels.Count; i++)
        {
            string extended = line + CubeBestLineSeparator + labels[i];
            bool more = i < labels.Count - 1;
            if (Fits(more ? extended + comma : extended))
            {
                line = extended;
            }
            else
            {
                lines.Add(line + comma);
                line = labels[i];
            }
        }
        lines.Add(line);
        return lines;

        bool Fits(string drawn) => EstimateTextWidth(drawn, CubePanelFontSize, TextWeight.Regular) <= width;
    }

    /// <summary>
    /// The Actual line: what was played, read off the record's played halves
    /// (<see cref="CubeDecisionData.UserDoublerAction"/>,
    /// <see cref="CubeDecisionData.UserTakerAction"/>), or
    /// <see langword="null"/> where neither half is recorded, which drops the
    /// line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// With both halves present the record states an answer, formed the one
    /// way two actions become one (<see cref="CubeAnswerExtensions.Of"/>) and
    /// labelled at its decision like any answer: a recorded no double with a
    /// pass reads Too good or No double / Pass as the decision reads it, and a
    /// recorded no double with a take is the No double answer, which reads
    /// <c>No double</c>.
    /// </para>
    /// <para>
    /// With one half missing no answer is inferred: the present half renders
    /// alone in its action label, and a missing doubler half shows
    /// <c>?</c> ahead of the taker half. Neither line reads a played action
    /// off an error: a zero error does not identify the action when the two
    /// cube equities tie.
    /// </para>
    /// </remarks>
    private static string? CubeActualLine(CubeDecision cube) =>
        (cube.Decision.UserDoublerAction, cube.Decision.UserTakerAction) switch
        {
            (CubeAction doubler, CubeAction taker) => CubeLabels.Label(CubeAnswerExtensions.Of(doubler, taker), cube),
            (CubeAction doubler, null)             => CubeLabels.Label(doubler),
            (null, CubeAction taker)               => "? / " + CubeLabels.Label(taker),
            (null, null)                           => null,
        };

    // -----------------------------------------------------------------------
    //  Text-width estimation
    // -----------------------------------------------------------------------

    /// <summary>
    /// The two font weights this renderer emits text in, as far as its width
    /// estimate is concerned: no <c>font-weight</c> attribute, or
    /// <c>font-weight="bold"</c>. Selects the advance table
    /// <see cref="EstimateTextWidth"/> sums.
    /// </summary>
    internal enum TextWeight
    {
        /// <summary>Normal weight — no <c>font-weight</c> attribute.</summary>
        Regular,
        /// <summary><c>font-weight="bold"</c>.</summary>
        Bold,
    }

    /// <summary>
    /// Advance widths, in em, of the printable ASCII characters
    /// (<c>U+0020</c>–<c>U+007E</c>) at normal weight. SVG built server-side
    /// cannot measure text, so <see cref="EstimateTextWidth"/> sums these
    /// instead.
    /// <para>
    /// Source: the published Helvetica metrics (Adobe's Core 14 set, widths
    /// per 1000 units of em), entered by hand — no metrics file was read.
    /// Arial is metric-compatible with Helvetica, so the same numbers serve
    /// both. The apostrophe and backtick take the ASCII glyphs' widths
    /// (<c>quotesingle</c>, <c>grave</c>), not the typographic quotes Adobe's
    /// standard encoding puts at those codes.
    /// </para>
    /// <para>
    /// Verified 2026-09-21 by measurement: every entry here and in
    /// <see cref="HelveticaBoldAdvanceEm"/> against the advance SkiaSharp
    /// 2.88.9 reports for the same character at 1000 px in the Windows 11
    /// Arial and Arial Bold faces on one machine (the tables parsed from this
    /// source, not retyped). All 95 entries per weight agree; the largest
    /// deviation is 0.0002 em, which is Arial's 2048-unit grid rounded to
    /// Helvetica's 1000. Arial Italic and Arial Bold Italic measure the same
    /// against these tables, so italic text is measured here too.
    /// </para>
    /// </summary>
    private static readonly FrozenDictionary<char, double> HelveticaAdvanceEm =
        new Dictionary<char, double>
        {
            [' '] = 0.278, ['!'] = 0.278, ['"'] = 0.355, ['#'] = 0.556, ['$'] = 0.556,
            ['%'] = 0.889, ['&'] = 0.667, ['\''] = 0.191, ['('] = 0.333, [')'] = 0.333,
            ['*'] = 0.389, ['+'] = 0.584, [','] = 0.278, ['-'] = 0.333, ['.'] = 0.278,
            ['/'] = 0.278,
            ['0'] = 0.556, ['1'] = 0.556, ['2'] = 0.556, ['3'] = 0.556, ['4'] = 0.556,
            ['5'] = 0.556, ['6'] = 0.556, ['7'] = 0.556, ['8'] = 0.556, ['9'] = 0.556,
            [':'] = 0.278, [';'] = 0.278, ['<'] = 0.584, ['='] = 0.584, ['>'] = 0.584,
            ['?'] = 0.556, ['@'] = 1.015,
            ['A'] = 0.667, ['B'] = 0.667, ['C'] = 0.722, ['D'] = 0.722, ['E'] = 0.667,
            ['F'] = 0.611, ['G'] = 0.778, ['H'] = 0.722, ['I'] = 0.278, ['J'] = 0.500,
            ['K'] = 0.667, ['L'] = 0.556, ['M'] = 0.833, ['N'] = 0.722, ['O'] = 0.778,
            ['P'] = 0.667, ['Q'] = 0.778, ['R'] = 0.722, ['S'] = 0.667, ['T'] = 0.611,
            ['U'] = 0.722, ['V'] = 0.667, ['W'] = 0.944, ['X'] = 0.667, ['Y'] = 0.667,
            ['Z'] = 0.611,
            ['['] = 0.278, ['\\'] = 0.278, [']'] = 0.278, ['^'] = 0.469, ['_'] = 0.556,
            ['`'] = 0.333,
            ['a'] = 0.556, ['b'] = 0.556, ['c'] = 0.500, ['d'] = 0.556, ['e'] = 0.556,
            ['f'] = 0.278, ['g'] = 0.556, ['h'] = 0.556, ['i'] = 0.222, ['j'] = 0.222,
            ['k'] = 0.500, ['l'] = 0.222, ['m'] = 0.833, ['n'] = 0.556, ['o'] = 0.556,
            ['p'] = 0.556, ['q'] = 0.556, ['r'] = 0.333, ['s'] = 0.500, ['t'] = 0.278,
            ['u'] = 0.556, ['v'] = 0.500, ['w'] = 0.722, ['x'] = 0.500, ['y'] = 0.500,
            ['z'] = 0.500,
            ['{'] = 0.334, ['|'] = 0.260, ['}'] = 0.334, ['~'] = 0.584,
        }.ToFrozenDictionary();

    /// <summary>
    /// Advance widths, in em, of the printable ASCII characters
    /// (<c>U+0020</c>–<c>U+007E</c>) at bold weight — the bold counterpart of
    /// <see cref="HelveticaAdvanceEm"/>: the published Helvetica-Bold metrics,
    /// entered and verified against measured Arial Bold on the same terms
    /// (see there; largest deviation 0.0002 em). Bold widens most letters
    /// (<c>i</c> 0.222 → 0.278, <c>m</c> 0.833 → 0.889); the digits and the
    /// <c>+ - .</c> the numeric cells use keep their widths.
    /// </summary>
    private static readonly FrozenDictionary<char, double> HelveticaBoldAdvanceEm =
        new Dictionary<char, double>
        {
            [' '] = 0.278, ['!'] = 0.333, ['"'] = 0.474, ['#'] = 0.556, ['$'] = 0.556,
            ['%'] = 0.889, ['&'] = 0.722, ['\''] = 0.238, ['('] = 0.333, [')'] = 0.333,
            ['*'] = 0.389, ['+'] = 0.584, [','] = 0.278, ['-'] = 0.333, ['.'] = 0.278,
            ['/'] = 0.278,
            ['0'] = 0.556, ['1'] = 0.556, ['2'] = 0.556, ['3'] = 0.556, ['4'] = 0.556,
            ['5'] = 0.556, ['6'] = 0.556, ['7'] = 0.556, ['8'] = 0.556, ['9'] = 0.556,
            [':'] = 0.333, [';'] = 0.333, ['<'] = 0.584, ['='] = 0.584, ['>'] = 0.584,
            ['?'] = 0.611, ['@'] = 0.975,
            ['A'] = 0.722, ['B'] = 0.722, ['C'] = 0.722, ['D'] = 0.722, ['E'] = 0.667,
            ['F'] = 0.611, ['G'] = 0.778, ['H'] = 0.722, ['I'] = 0.278, ['J'] = 0.556,
            ['K'] = 0.722, ['L'] = 0.611, ['M'] = 0.833, ['N'] = 0.722, ['O'] = 0.778,
            ['P'] = 0.667, ['Q'] = 0.778, ['R'] = 0.722, ['S'] = 0.667, ['T'] = 0.611,
            ['U'] = 0.722, ['V'] = 0.667, ['W'] = 0.944, ['X'] = 0.667, ['Y'] = 0.667,
            ['Z'] = 0.611,
            ['['] = 0.333, ['\\'] = 0.278, [']'] = 0.333, ['^'] = 0.584, ['_'] = 0.556,
            ['`'] = 0.333,
            ['a'] = 0.556, ['b'] = 0.611, ['c'] = 0.556, ['d'] = 0.611, ['e'] = 0.556,
            ['f'] = 0.333, ['g'] = 0.611, ['h'] = 0.611, ['i'] = 0.278, ['j'] = 0.278,
            ['k'] = 0.556, ['l'] = 0.278, ['m'] = 0.889, ['n'] = 0.611, ['o'] = 0.611,
            ['p'] = 0.611, ['q'] = 0.611, ['r'] = 0.389, ['s'] = 0.556, ['t'] = 0.333,
            ['u'] = 0.611, ['v'] = 0.556, ['w'] = 0.778, ['x'] = 0.556, ['y'] = 0.556,
            ['z'] = 0.500,
            ['{'] = 0.389, ['|'] = 0.280, ['}'] = 0.389, ['~'] = 0.584,
        }.ToFrozenDictionary();

    // The advance charged to a character absent from a table (anything
    // outside printable ASCII): the widest width in that table, so an
    // unknown glyph errs wide.
    private static readonly double HelveticaWidestAdvanceEm = HelveticaAdvanceEm.Values.Max();
    private static readonly double HelveticaBoldWidestAdvanceEm = HelveticaBoldAdvanceEm.Values.Max();

    /// <summary>
    /// Multiplier applied on top of the summed Helvetica/Arial advances. The
    /// SVG asks for generic <c>sans-serif</c>, which is Arial or Helvetica in
    /// the browsers on Windows and macOS but can resolve to a wider face
    /// elsewhere (DejaVu Sans is the usual example). The 10% is an assumed
    /// margin for that, not a measurement.
    /// <para>
    /// The one measured data point (2026-09-21, one Windows 11 machine): the
    /// raster export path — Svg.Skia 3.6.0 over SkiaSharp 2.88.9 — resolves
    /// <c>sans-serif</c> to Segoe UI (its render pixel-identical to an explicit
    /// Segoe UI one, not to Arial). Segoe UI's widths at 14 px for the play
    /// panel's strings sit within about 2% of Arial's: <c>Depth</c> +1.8%,
    /// <c>24/21 13/10</c> +1.6%, <c>R++p20736</c> +0.4%, <c>+0.5000</c> −1.3%.
    /// No DejaVu-class face was available to measure.
    /// </para>
    /// </summary>
    internal const double TextWidthSafetyFactor = 1.10;

    /// <summary>
    /// Estimated rendered width of <paramref name="text"/> at
    /// <paramref name="fontSize"/> and <paramref name="weight"/>: the summed
    /// advances from <see cref="HelveticaAdvanceEm"/> or
    /// <see cref="HelveticaBoldAdvanceEm"/> (a character outside printable
    /// ASCII charged that table's widest), times
    /// <see cref="TextWidthSafetyFactor"/>. The one owner of text-width
    /// estimation in this renderer — the play panel's column layout and the
    /// rail-label fit both measure through it. Linear in
    /// <paramref name="fontSize"/>, so a size of 1 gives the width per unit
    /// of font size.
    /// </summary>
    internal static double EstimateTextWidth(string text, double fontSize, TextWeight weight)
    {
        var (advances, widest) = weight switch
        {
            TextWeight.Regular => (HelveticaAdvanceEm, HelveticaWidestAdvanceEm),
            TextWeight.Bold    => (HelveticaBoldAdvanceEm, HelveticaBoldWidestAdvanceEm),
            _ => throw new ArgumentOutOfRangeException(nameof(weight), weight, null),
        };
        double em = 0;
        foreach (char c in text)
            em += advances.GetValueOrDefault(c, widest);
        return em * fontSize * TextWidthSafetyFactor;
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    // Delegates to the public SSOT so the "SVG numbers are invariant" rule
    // has exactly one home (see SvgFormat).
    private static string F(double v) => SvgFormat.Number(v);

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
}
