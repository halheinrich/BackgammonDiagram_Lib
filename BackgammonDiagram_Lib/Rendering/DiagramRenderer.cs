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
        var theme = ThemeRegistry.Resolve(options.ThemeName);
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
        double bx = layout.BoardOffsetX(effectivePanelOnLeft);

        // Full canvas background — prevents transparent edges showing in PNG
        sb.AppendLine($"""  <rect x="0" y="0" width="{F(layout.TotalWidth(hasPanel))}" height="{F(layout.BoardHeight)}" fill="{Darken(theme.BoardColor, 0.15)}"/>""");

        sb.AppendLine($"""  <rect x="{F(bx)}" y="0" width="{F(layout.BoardWidth)}" height="{F(layout.BoardHeight)}" fill="{theme.BoardColor}"/>""");

        AppendLeftRail(sb, layout, theme, bx);
        AppendBar(sb, layout, theme, bx);
        AppendPoints(sb, layout, theme, bx, effectivePanelOnLeft);
        AppendPointNumbers(sb, layout, theme, bx, effectivePanelOnLeft);
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
        double bx, bool panelOnLeft)
    {
        for (int pt = 1; pt <= 24; pt++)
        {
            string color = (pt % 2 == 0) ? theme.PointColorDark : theme.PointColorLight;
            double cx = layout.ColumnCentreX(pt, panelOnLeft);
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
        double bx, bool panelOnLeft)
    {
        for (int pt = 1; pt <= 24; pt++)
        {
            double cx = layout.ColumnCentreX(pt, panelOnLeft);

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