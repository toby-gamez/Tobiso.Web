using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

/// <summary>
/// A single row/marker/card input to <see cref="ChronicleLayout"/>. Either a real item or a
/// collapsed-category placeholder (design doc §4: "v kódu se událost, osobnost i kategorie
/// mapují do jednoho TimelineNode, takže layout s nimi zachází stejně"). Collapsed ERAS are not
/// nodes at all — see <see cref="ChronicleEraBlockInput"/> — since the era addendum specifies
/// them as a full-width block outside the normal card/lane system, not a card.
/// </summary>
public record ChronicleTimelineNode(
    string Key,
    ChronicleItemResponse Node,
    long Start,
    long? End,
    string? Color,
    int CollapsedCount = 0);

/// <summary>A collapsed era: "nejde do layoutu jako řádky jednotlivých let, zabírá jen jeden řádek."</summary>
public record ChronicleEraBlockInput(string Name, long Year, string? Color, int Count);

public record ChronicleLayoutResult(
    List<ChronicleCardResponse> Cards,
    List<ChronicleMarkerResponse> Markers,
    List<ChronicleRowLabelResponse> RowLabels,
    List<ChronicleEraBlockResponse> EraBlocks,
    int TotalHeight,
    IReadOnlyDictionary<long, int> RowYByYear);

/// <summary>
/// Port of the design doc's §5 layout algorithm: rows from distinct years (position = order, not
/// real time), cards placed top-down into the first free lane/side, markers slotted per row.
/// </summary>
public static class ChronicleLayout
{
    private const int RowH = 40, CardH = 120, Gap = 12;

    public static ChronicleLayoutResult Compute(
        IEnumerable<ChronicleTimelineNode> input,
        IEnumerable<ChronicleEraBlockInput>? eraBlocks = null)
    {
        var nodes = input.ToList();
        var blocks = (eraBlocks ?? Enumerable.Empty<ChronicleEraBlockInput>()).ToList();
        if (nodes.Count == 0 && blocks.Count == 0)
            return new ChronicleLayoutResult(new(), new(), new(), new(), 0, new Dictionary<long, int>());

        // 1) Rows: every distinct year (start or end) gets one row; a collapsed era block also
        // reserves exactly one row of its own, even though it isn't a card.
        var nodeYears = nodes.SelectMany(n => n.End is long e ? new[] { n.Start, e } : new[] { n.Start }).Distinct().ToList();
        var blockYears = blocks.Select(b => b.Year).Distinct().ToList();
        var baseYears = nodeYears.Concat(blockYears).Distinct().OrderBy(y => y).ToList();

        // Rows are spaced by ORDER, not real time — two real rows can land just one row apart even
        // when they're millennia apart in actual years. A normal card's fixed CardH (3 rows) would
        // then physically reach down into a block sitting right below it, so reserve clearance
        // (padding rows with no real meaning, never shown as a date label) before every block.
        const int minRowsBeforeBlock = CardH / RowH;
        var padding = new List<long>();
        foreach (var blockYear in blockYears)
        {
            var idx = baseYears.IndexOf(blockYear);
            if (idx <= 0) continue;
            var prevYear = baseYears[idx - 1];
            for (var p = 1; p < minRowsBeforeBlock; p++)
            {
                var candidate = blockYear - p;
                if (candidate > prevYear) padding.Add(candidate);
            }
        }

        var years = baseYears.Concat(padding).Distinct().OrderBy(y => y).ToList();
        var rowY = years.Select((y, i) => (y, i)).ToDictionary(t => t.y, t => t.i * RowH);

        var eraBlockResponses = blocks
            .Select(b => new ChronicleEraBlockResponse { Name = b.Name, Y = rowY[b.Year], Count = b.Count, Color = b.Color })
            .OrderBy(b => b.Y)
            .ToList();

        if (nodes.Count == 0)
            return new ChronicleLayoutResult(new(), new(), new(), eraBlockResponses, (years.Count - 1) * RowH + Gap, rowY);

        // 2) Cards: top-down, first free lane per side.
        var bottoms = new Dictionary<(ChronicleSide Side, int Lane), int>();
        var cards = new List<ChronicleCardResponse>();
        var flip = 0;

        foreach (var n in nodes.OrderBy(n => rowY[n.Start]).ThenByDescending(n => n.End is long e ? rowY[e] : 0))
        {
            var top = rowY[n.Start];
            var height = n.End is long end ? Math.Max(CardH, rowY[end] - top) : CardH;

            var pref = flip++ % 2 == 0 ? ChronicleSide.Left : ChronicleSide.Right;
            var other = pref == ChronicleSide.Left ? ChronicleSide.Right : ChronicleSide.Left;

            ChronicleCardResponse? placed = null;
            for (var lane = 0; placed is null; lane++)
            {
                foreach (var side in new[] { pref, other })
                {
                    if (bottoms.TryGetValue((side, lane), out var bottom) && top < bottom + Gap) continue;
                    bottoms[(side, lane)] = top + height;
                    placed = new ChronicleCardResponse
                    {
                        Key = n.Key,
                        Node = n.Node,
                        Side = side,
                        Lane = lane,
                        Top = top,
                        Height = height,
                        IsCollapsedCategory = n.Node.ItemType == ChronicleLayoutNodeTypes.Category,
                        CategoryId = n.Node.ItemType == ChronicleLayoutNodeTypes.Category ? n.Node.Id : null,
                        CollapsedCount = n.CollapsedCount
                    };
                    break;
                }
            }
            cards.Add(placed!);
        }

        // 3) Markers: start and end, same-row ones slotted side by side.
        var markers = nodes
            .SelectMany(n => n.End is long e
                ? new[] { (Node: n, IsEnd: false, Y: rowY[n.Start]), (Node: n, IsEnd: true, Y: rowY[e]) }
                : new[] { (Node: n, IsEnd: false, Y: rowY[n.Start]) })
            .GroupBy(t => t.Y)
            .SelectMany(g => g.Select((t, slot) =>
                new ChronicleMarkerResponse { Key = t.Node.Key, IsEnd = t.IsEnd, Y = t.Y, Slot = slot, Color = t.Node.Color }))
            .ToList();

        // A year can be shared by several items with different precision; the row label uses
        // whichever is most precise (doc §4 examples: "ca 800" / "1453" / "2 500 000 př. n. l.").
        var precisionByYear = new Dictionary<long, ChronicleDatePrecision>();
        foreach (var n in nodes)
        {
            foreach (var y in n.End is long e ? new[] { n.Start, e } : new[] { n.Start })
            {
                if (!precisionByYear.TryGetValue(y, out var existing) || n.Node.Precision < existing)
                    precisionByYear[y] = n.Node.Precision;
            }
        }

        var rowLabels = nodeYears
            .Select(y => new ChronicleRowLabelResponse { Year = y, Y = rowY[y], Label = FormatYear(y, precisionByYear.GetValueOrDefault(y)) })
            .OrderBy(r => r.Y).ToList();

        var total = Math.Max(cards.Max(c => c.Top + c.Height), (years.Count - 1) * RowH) + Gap;
        return new ChronicleLayoutResult(cards, markers, rowLabels, eraBlockResponses, total, rowY);
    }

    /// <summary>
    /// Doc §4: "Popisky let u řádků s položkami podle přesnosti: „ca 800", „1453", „2 500 000 př. n. l."."
    /// Day/Year precision shows the exact number; Decade/Century round and prefix "ca "; Millennium
    /// (prehistoric-scale) shows the raw number since the magnitude itself already conveys roughness.
    /// </summary>
    public static string FormatYear(long year, ChronicleDatePrecision precision = ChronicleDatePrecision.Year)
    {
        string Suffix(long y) => y < 0 ? " př. n. l." : "";
        // Doc's own examples group only the large prehistoric numbers ("2 500 000 př. n. l.") and
        // leave ordinary calendar years plain ("1453", not "1 453") — group from 5 digits up.
        string Digits(long y) => Math.Abs(y) >= 10000 ? Math.Abs(y).ToString("N0").Replace(",", " ") : Math.Abs(y).ToString();

        return precision switch
        {
            ChronicleDatePrecision.Decade => $"ca {Digits(RoundTo(year, 10))}{Suffix(year)}",
            ChronicleDatePrecision.Century => $"ca {Digits(RoundTo(year, 100))}{Suffix(year)}",
            _ => $"{Digits(year)}{Suffix(year)}"
        };
    }

    private static long RoundTo(long year, int unit) => (long)Math.Round(year / (double)unit) * unit;
}

/// <summary>ItemType values used only inside layout output, for the collapsed-category pseudo-node (doc §4).</summary>
public static class ChronicleLayoutNodeTypes
{
    public const string Category = "Category";
}
