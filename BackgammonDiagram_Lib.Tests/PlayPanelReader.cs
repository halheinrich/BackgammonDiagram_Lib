using System.Globalization;
using System.Text.RegularExpressions;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>One play row of a rendered play panel, cell by cell.</summary>
/// <param name="Marker">The bold mark in the marker column (<c>*</c> or <c>†</c>), or null.</param>
/// <param name="Rank">The rank number.</param>
/// <param name="Move">The move text.</param>
/// <param name="Equity">The Equity cell's text.</param>
/// <param name="Loss">The Eq Loss cell's text, or null for a blank cell.</param>
/// <param name="Depth">The Depth cell's text, or null for a blank cell.</param>
/// <param name="Italic">Whether the row's numeric cells carry the rank-inversion italic.</param>
/// <param name="Baseline">The row's text y, in the panel's own (board-relative) space.</param>
/// <param name="Highlighted">Whether the shown play's highlight is drawn behind the row.</param>
internal sealed record PanelRow(string? Marker, int Rank, string Move, string Equity, string? Loss, string? Depth, bool Italic,
    double Baseline, bool Highlighted);

/// <summary>
/// Reads a rendered play panel back into rows. Structural, not pinned to
/// anchors or a font size — the panel shrinks its font when its content is
/// too wide (halheinrich/backgammon#252), and every column but the marker's
/// moves with the size. The columns are found from their headers, and a
/// row's cells by the y they share: a play row emits its marker (if any),
/// its rank and its move text first, then its Equity, Eq Loss and Depth
/// cells, all on one y. Layout itself is pinned in RendererPlayPanelTests.
/// A row is highlighted when a rect one row pitch high
/// (<see cref="DiagramRenderer.PlayPanelLineHeight"/>) is drawn with its top
/// one row's ascent above the row's baseline — the shown play's highlight.
/// </summary>
internal static class PlayPanelReader
{
    private sealed record TextElement(Dictionary<string, string> Attributes, string Content)
    {
        public string X => Attributes["x"];
        public string Y => Attributes["y"];
        public bool Has(string attribute) => Attributes.ContainsKey(attribute);
    }

    /// <summary>The play rows of <paramref name="svg"/>'s panel, top to bottom.</summary>
    public static List<PanelRow> Rows(string svg)
    {
        var texts = Regex.Matches(svg, "<text ([^>]*)>([^<]*)</text>")
            .Select(m => new TextElement(
                Regex.Matches(m.Groups[1].Value, "([a-z-]+)=\"([^\"]*)\"")
                    .ToDictionary(a => a.Groups[1].Value, a => a.Groups[2].Value),
                Unescape(m.Groups[2].Value)))
            .ToList();

        // The tops of the rects one row high: the highlight's candidates.
        string rowHeight = SvgFormat.Number(DiagramRenderer.PlayPanelLineHeight);
        var rowRectTops = Regex.Matches(svg, "<rect ([^>]*)/>")
            .Select(m => Regex.Matches(m.Groups[1].Value, "([a-z-]+)=\"([^\"]*)\"")
                .ToDictionary(a => a.Groups[1].Value, a => a.Groups[2].Value))
            .Where(a => a.GetValueOrDefault("height") == rowHeight)
            .Select(a => a["y"])
            .ToHashSet();

        string equityX = Header(texts, DiagramRenderer.PlayPanelEquityHeader).X;
        string lossX = Header(texts, DiagramRenderer.PlayPanelLossHeader).X;
        string depthX = Header(texts, DiagramRenderer.PlayPanelDepthHeader).X;

        var rows = new List<PanelRow>();
        for (int i = 0; i + 1 < texts.Count; i++)
        {
            var rank = texts[i];
            var move = texts[i + 1];
            if (!IsPlainCell(rank) || !IsPlainCell(move) || rank.Y != move.Y || !int.TryParse(rank.Content, out int number))
                continue;

            string y = rank.Y;
            var marker = i > 0 && texts[i - 1].Y == y && texts[i - 1].Attributes.GetValueOrDefault("font-weight") == "bold"
                         && !texts[i - 1].Has("text-anchor")
                ? texts[i - 1].Content
                : null;
            var equity = texts.Single(t => t.Y == y && t.X == equityX && t.Has("text-anchor"));
            var loss = texts.SingleOrDefault(t => t.Y == y && t.X == lossX && t.Has("text-anchor"));
            var depth = texts.SingleOrDefault(t => t.Y == y && t.X == depthX && !t.Has("text-anchor"));
            double baseline = double.Parse(y, CultureInfo.InvariantCulture);
            string top = SvgFormat.Number(baseline - DiagramRenderer.PlayPanelLineHeight * 0.8);
            rows.Add(new PanelRow(marker, number, move.Content, equity.Content, loss?.Content, depth?.Content,
                equity.Attributes.GetValueOrDefault("font-style") == "italic", baseline, rowRectTops.Contains(top)));
            i++;
        }
        return rows;
    }

    /// <summary>The move texts of the panel's rows, top to bottom.</summary>
    public static List<string> Moves(string svg) => [.. Rows(svg).Select(r => r.Move)];

    /// <summary>The rank numbers of the panel's rows, top to bottom.</summary>
    public static List<int> Ranks(string svg) => [.. Rows(svg).Select(r => r.Rank)];

    /// <summary>The row carrying <paramref name="marker"/>.</summary>
    public static PanelRow RowMarked(string svg, string marker) => Rows(svg).Single(r => r.Marker == marker);

    // A rank or move cell: plain regular text, anchored at its start, not a
    // rail label (which is centred on its baseline) or a point number.
    private static bool IsPlainCell(TextElement text) =>
        !text.Has("text-anchor") && !text.Has("dominant-baseline") && !text.Has("font-weight");

    private static TextElement Header(List<TextElement> texts, string header) =>
        texts.Single(t => t.Content == header && !t.Has("font-weight"));

    private static string Unescape(string text) => text
        .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&amp;", "&");
}
