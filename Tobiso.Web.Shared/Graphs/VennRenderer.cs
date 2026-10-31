using System.Text;
using System.Text.RegularExpressions;
using static Tobiso.Web.Shared.Graphs.DiagramSupport;

namespace Tobiso.Web.Shared.Graphs;

/// <summary>
/// Renders author-written Venn diagram blocks (2 or 3 sets) in post content to inline SVG:
/// <code>
/// [venn1]
/// title: Dělitelé čísel 12 a 18
/// set(name: D12, color: BLUE): 1; 2; 3; 4; 6; 12
/// set(name: D18, color: ORANGE): 1; 2; 3; 6; 9; 18
/// outside: 5; 7
/// note: Průnik tvoří společní dělitelé.
/// [/venn1]
/// </code>
/// Elements listed on a <c>set</c> line land in the region given by which sets contain them, so
/// the intersection fills itself. <c>only(A)</c>, <c>both(A,B)</c>, <c>all</c> and <c>outside</c>
/// place elements into one region explicitly. Elements are separated by ";" (or "," or spaces).
/// Same pre-extraction placeholder scheme as <see cref="GraphRenderer"/>.
/// </summary>
public static class VennRenderer
{
    private static readonly Regex BlockRegex = new(
        @"^[ \t]*\[(venn[\w-]*)\][ \t]*\r?\n(.*?)^[ \t]*\[/\1\][ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private const string PlaceholderPrefix = "TOBISOVENNPLACEHOLDER";
    private static readonly Regex PlaceholderRegex = new($@"(?:<p>\s*)?{PlaceholderPrefix}(\d+)X(?:\s*</p>)?");

    private static readonly string[] DefaultColors = ["#2f6fdf", "#e8833a", "#3a9d5d"];

    public static string ExtractBlocks(string content, List<string> rendered)
    {
        if (string.IsNullOrEmpty(content) || content.IndexOf("[venn", StringComparison.OrdinalIgnoreCase) < 0)
            return content;

        return BlockRegex.Replace(content, m =>
        {
            var index = rendered.Count;
            rendered.Add(Render(m.Groups[2].Value, $"vn{index}-{StableHash(m.Value)}"));
            return $"\n\n{PlaceholderPrefix}{index}X\n\n";
        });
    }

    /// <summary>Renders the first Venn block in <paramref name="text"/> (admin live preview).</summary>
    public static string RenderPreview(string text)
    {
        var rendered = new List<string>();
        ExtractBlocks(text, rendered);
        return rendered.Count > 0 ? rendered[0] : "";
    }

    public static string RestoreBlocks(string html, List<string> rendered)
    {
        if (rendered.Count == 0) return html;
        return PlaceholderRegex.Replace(html, m =>
        {
            var index = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            return index < rendered.Count ? rendered[index] : m.Value;
        });
    }

    // ──────────────────────────────────────────────────────────────────────
    // Parsing
    // ──────────────────────────────────────────────────────────────────────

    private sealed class VennSet
    {
        public string Name = "";
        public string Color = "";
        public List<string> Elements = [];
    }

    private sealed class Venn
    {
        public string? Title;
        public bool Universe;
        /// <summary>Playground: term categories the reader may try (empty = no playground).</summary>
        public readonly HashSet<string> Ops = new();
        /// <summary>Terms the author displays (first one starts highlighted), as raw tokens then op keys.</summary>
        public readonly List<List<string>> ShowLines = [];
        public readonly List<string> ShowKeys = [];
        public readonly List<VennSet> Sets = [];
        public readonly Dictionary<int, List<string>> Explicit = new(); // region mask -> elements
        public readonly List<string> Outside = [];
        public readonly List<string> Notes = [];
        public readonly List<string> Warnings = [];
        public readonly List<(string Kind, string Args, List<string> Items)> RegionLines = [];
    }

    /// <summary>Splits an element list on ";" when present, else on ",", else on whitespace.</summary>
    public static List<string> SplitElements(string value)
    {
        var v = value.Trim();
        if (v.Length == 0) return [];
        var parts = v.Contains(';') ? v.Split(';')
            : v.Contains(',') ? v.Split(',')
            : v.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    }

    private static Venn Parse(string body)
    {
        var v = new Venn();
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            var lm = LineRegex.Match(line);
            if (!lm.Success) continue;

            var key = Normalize(lm.Groups["key"].Value);
            var value = lm.Groups["value"].Value.Trim();
            var args = ParseArgs(lm.Groups["args"].Success ? lm.Groups["args"].Value : null);

            switch (key)
            {
                case "title" or "nazev" or "nadpis":
                    if (value.Length > 0) v.Title = value;
                    break;
                case "note" or "poznamka":
                    if (value.Length > 0) v.Notes.Add(value);
                    break;
                case "show" or "zobraz" or "pojem":
                    v.ShowLines.Add(value.Contains(';')
                        ? value.Split(';').Select(t => t.Trim()).Where(t => t.Length > 0).ToList()
                        : value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList());
                    break;
                case "playground" or "hriste" or "hra" or "operations" or "operace" or "pojmy":
                    foreach (var token in value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries).Select(Normalize))
                    {
                        if (token is "ano" or "yes" or "true" or "vse" or "all" or "1")
                            foreach (var c in OpCategories) v.Ops.Add(c);
                        else if (token is "ne" or "no" or "false" or "0")
                            v.Ops.Clear();
                        else if (token is "prunik" or "sjednoceni" or "rozdil" or "doplnek")
                            v.Ops.Add(token);
                        else
                            v.Warnings.Add($"Neznámý pojem „{token}“ (použij prunik, sjednoceni, rozdil, doplnek nebo ano).");
                    }
                    break;
                case "universe" or "univerzum":
                    v.Universe = Normalize(value) is "yes" or "ano" or "true" or "1";
                    break;
                case "set" or "mnozina":
                {
                    if (v.Sets.Count >= 3)
                    {
                        v.Warnings.Add("Vennův diagram podporuje nejvýše 3 množiny, další jsou ignorovány.");
                        break;
                    }
                    var name = args.GetValueOrDefault("name") ?? args.GetValueOrDefault("nazev") ?? args.GetValueOrDefault("") ?? "";
                    if (name.Length == 0) name = ((char)('A' + v.Sets.Count)).ToString();

                    var color = args.GetValueOrDefault("color") ?? args.GetValueOrDefault("barva");
                    var items = SplitElements(value);
                    // Whitespace-separated lists may end with a bare colour name (e.g. "1 2 3 BLUE").
                    if (color == null && items.Count > 0 && !value.Contains(';') && !value.Contains(',')
                        && GraphRenderer.ResolveColor(items[^1]) != null && items[^1] == items[^1].ToUpperInvariant())
                    {
                        color = items[^1];
                        items.RemoveAt(items.Count - 1);
                    }
                    var resolved = color != null ? GraphRenderer.ResolveColor(color) : null;
                    if (color != null && resolved == null) v.Warnings.Add($"Neznámá barva „{color}“.");

                    v.Sets.Add(new VennSet
                    {
                        Name = name,
                        Color = resolved ?? DefaultColors[v.Sets.Count],
                        Elements = items,
                    });
                    break;
                }
                case "only" or "jen" or "pouze" or "both" or "prunik" or "all" or "vse" or "outside" or "mimo":
                    v.RegionLines.Add((key, lm.Groups["args"].Success ? lm.Groups["args"].Value : "", SplitElements(value)));
                    break;
            }
        }

        // Region lines can only be resolved once all sets (and their names) are known.
        foreach (var (kind, argText, items) in v.RegionLines)
        {
            if (kind is "outside" or "mimo")
            {
                v.Outside.AddRange(items);
                continue;
            }
            if (kind is "all" or "vse")
            {
                AddExplicit(v, (1 << v.Sets.Count) - 1, items);
                continue;
            }

            var refs = argText.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToList();
            var mask = 0;
            var ok = refs.Count > 0;
            foreach (var r in refs)
            {
                var idx = FindSet(v, r);
                if (idx < 0) { ok = false; v.Warnings.Add($"Množina „{r}“ neexistuje."); break; }
                mask |= 1 << idx;
            }
            if (!ok) continue;
            AddExplicit(v, mask, items);
        }

        foreach (var tokens in v.ShowLines)
        {
            var key = ResolveShowKey(v, tokens);
            if (key == null) v.Warnings.Add($"Pojem „{string.Join(' ', tokens)}“ se nepodařilo přečíst (např. show: prunik A B, show: rozdil A B, show: doplnek A).");
            else if (!v.ShowKeys.Contains(key)) v.ShowKeys.Add(key);
        }

        if (v.Sets.Count < 2 && v.Warnings.Count == 0)
            v.Warnings.Add("Vennův diagram potřebuje alespoň 2 množiny.");
        return v;
    }

    // "prunik A B", "sjednoceni A B C", "rozdil A B" (A \\ B), "doplnek A" -> op key, null when invalid.
    private static string? ResolveShowKey(Venn v, List<string> tokens)
    {
        if (tokens.Count < 2) return null;
        var category = Normalize(tokens[0]);
        var idx = new List<int>();
        foreach (var t in tokens.Skip(1))
        {
            var i = FindSet(v, t);
            if (i < 0) { v.Warnings.Add($"Množina „{t}“ neexistuje."); return null; }
            idx.Add(i);
        }
        if (idx.Distinct().Count() != idx.Count) return null;
        switch (category)
        {
            case "prunik" or "sjednoceni" when idx.Count >= 2:
                return $"{category}:{string.Join(',', idx.OrderBy(i => i))}";
            case "rozdil" when idx.Count == 2:
                return $"rozdil:{idx[0]},{idx[1]}";
            case "doplnek" when idx.Count == 1:
                return $"doplnek:{idx[0]}";
            default:
                return null;
        }
    }

    private static int FindSet(Venn v, string reference)
    {
        for (var i = 0; i < v.Sets.Count; i++)
            if (string.Equals(Normalize(v.Sets[i].Name), Normalize(reference), StringComparison.Ordinal))
                return i;
        if (int.TryParse(reference, out var n) && n >= 1 && n <= v.Sets.Count) return n - 1;
        return -1;
    }

    private static void AddExplicit(Venn v, int mask, List<string> items)
    {
        if (!v.Explicit.TryGetValue(mask, out var list)) v.Explicit[mask] = list = [];
        list.AddRange(items);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Geometry
    // ──────────────────────────────────────────────────────────────────────

    private readonly record struct Circle(double X, double Y, double R);

    // Region box: centre plus the room available for text.
    private readonly record struct Slot(double X, double Y, double W, double H);

    private sealed class Geometry
    {
        public Circle[] Circles = [];
        public Dictionary<int, Slot> Slots = new();
        public (double X, double Y, string Anchor)[] NameAnchors = [];
        public (double X, double Y, double W, double H) UniverseRect;
        public (double X, double Y, double W, double H) ViewUniverse, ViewTight;
        public (double X, double Y) OutsideAnchor;
    }

    private static Geometry TwoSets()
    {
        const double r = 125, cy = 185, ax = 215, bx = 345;
        return new Geometry
        {
            Circles = [new(ax, cy, r), new(bx, cy, r)],
            Slots = new()
            {
                [0b01] = new(ax - 65, cy, 110, 190),
                [0b10] = new(bx + 65, cy, 110, 190),
                [0b11] = new((ax + bx) / 2, cy, 104, 200),
            },
            NameAnchors = [(272, 46, "end"), (288, 46, "start")],
            UniverseRect = (20, 14, 520, 324),
            ViewUniverse = (0, 0, 560, 352),
            ViewTight = (50, 24, 460, 306),
            OutsideAnchor = (36, 38),
        };
    }

    private static Geometry ThreeSets()
    {
        const double r = 120, d = 130;
        double ax = 245, ay = 190, bx = ax + d, by = ay, cx = ax + d / 2, cy = ay + d * Math.Sqrt(3) / 2;
        var ox = (ax + bx + cx) / 3;
        var oy = (ay + by + cy) / 3;

        Slot Away(double px, double py, double dist, double w, double h)
        {
            var dx = px - ox;
            var dy = py - oy;
            var len = Math.Sqrt(dx * dx + dy * dy);
            return len < 0.001 ? new Slot(px, py, w, h) : new Slot(px + dx / len * dist, py + dy / len * dist, w, h);
        }

        double MidX(double p, double q) => (p + q) / 2;
        var slots = new Dictionary<int, Slot>
        {
            [0b001] = Away(ax, ay, 0.58 * r, 104, 110),
            [0b010] = Away(bx, by, 0.58 * r, 104, 110),
            [0b100] = Away(cx, cy, 0.58 * r, 110, 96),
            [0b011] = Away(MidX(ax, bx), MidX(ay, by), 0.45 * r, 78, 80),
            [0b101] = Away(MidX(ax, cx), MidX(ay, cy), 0.45 * r, 66, 80),
            [0b110] = Away(MidX(bx, cx), MidX(by, cy), 0.45 * r, 66, 80),
            [0b111] = new Slot(ox, oy + 8, 64, 60),
        };

        return new Geometry
        {
            Circles = [new(ax, ay, r), new(bx, by, r), new(cx, cy, r)],
            Slots = slots,
            NameAnchors = [(ax - 0.866 * (r + 12), ay - 0.5 * (r + 12), "end"), (bx + 0.866 * (r + 12), by - 0.5 * (r + 12), "start"), (cx, cy + r + 20, "middle")],
            UniverseRect = (20, 14, 580, 436),
            ViewUniverse = (0, 0, 620, 464),
            ViewTight = (10, 54, 600, 410),
            OutsideAnchor = (36, 38),
        };
    }

    // ──────────────────────────────────────────────────────────────────────
    // Rendering
    // ──────────────────────────────────────────────────────────────────────

    private static string Render(string body, string id)
    {
        var v = Parse(body);
        if (v.Sets.Count < 2)
        {
            var reason = v.Warnings.Count > 0 ? Enc(string.Join(" ", v.Warnings)) : "Chybí množiny.";
            return $"<div class=\"post-graph-error\">Vennův diagram se nepodařilo vykreslit – {reason} (<code>set(name: A): 1; 2; 3</code>)</div>";
        }

        var geo = v.Sets.Count == 2 ? TwoSets() : ThreeSets();
        var regions = new Dictionary<int, List<string>>();
        void Put(int mask, string element)
        {
            if (!regions.TryGetValue(mask, out var list)) regions[mask] = list = [];
            if (!list.Contains(element, StringComparer.OrdinalIgnoreCase)) list.Add(element);
        }

        // Membership: an element sits in the region made of every set that lists it.
        var seen = new List<string>();
        foreach (var set in v.Sets)
            foreach (var el in set.Elements)
                if (!seen.Contains(el, StringComparer.OrdinalIgnoreCase)) seen.Add(el);
        foreach (var el in seen)
        {
            var mask = 0;
            for (var i = 0; i < v.Sets.Count; i++)
                if (v.Sets[i].Elements.Contains(el, StringComparer.OrdinalIgnoreCase)) mask |= 1 << i;
            Put(mask, el);
        }
        foreach (var (mask, items) in v.Explicit)
            foreach (var el in items) Put(mask, el);

        var allOps = BuildOps(v, regions);
        // Playground: every term of the allowed categories. Shown terms: the author's picks, the
        // first of which starts highlighted; the reader toggles them with their chip.
        var ops = v.Ops.Count > 0
            ? allOps.Where(o => v.Ops.Contains(o.Category) || v.ShowKeys.Contains(o.Key)).ToList()
            : v.ShowKeys.Select(k => allOps.FirstOrDefault(o => o.Key == k)).OfType<Op>().ToList();
        var active = v.ShowKeys.Select(k => ops.FirstOrDefault(o => o.Key == k)).OfType<Op>().FirstOrDefault();
        var universe = v.Universe || v.Outside.Count > 0 || ops.Any(o => o.Category == "doplnek");
        var view = universe ? geo.ViewUniverse : geo.ViewTight;

        var svg = new StringBuilder();
        var aria = v.Title ?? "Vennův diagram";
        svg.Append($"<svg class=\"venn-svg{(active != null ? " venn-active" : "")}\" viewBox=\"{F(view.X)} {F(view.Y)} {F(view.W)} {F(view.H)}\" style=\"max-width:{F(view.W)}px\" role=\"img\" aria-label=\"{Enc(aria)}\" xmlns=\"http://www.w3.org/2000/svg\">");

        AppendRegionDefs(svg, geo, v.Sets.Count, id);

        if (universe)
        {
            var u = geo.UniverseRect;
            svg.Append($"<rect class=\"venn-universe\" x=\"{F(u.X)}\" y=\"{F(u.Y)}\" width=\"{F(u.W)}\" height=\"{F(u.H)}\" rx=\"10\"/>");
            svg.Append($"<text class=\"venn-universe-label\" x=\"{F(u.X + 12)}\" y=\"{F(u.Y + 22)}\" font-size=\"15\" font-style=\"italic\">U</text>");
        }

        for (var i = 0; i < v.Sets.Count; i++)
        {
            var c = geo.Circles[i];
            var color = Enc(v.Sets[i].Color);
            svg.Append($"<circle class=\"venn-circle\" cx=\"{F(c.X)}\" cy=\"{F(c.Y)}\" r=\"{F(c.R)}\" style=\"fill:{color};stroke:{color}\"/>");
        }

        // Highlight shapes for the clickable terms (hidden until a term is chosen, see image-lightbox.js).
        if (ops.Count > 0) AppendRegionShapes(svg, geo, v.Sets.Count, id, active?.Masks ?? []);

        var overflow = new List<string>();
        foreach (var (mask, items) in regions.OrderBy(kv => kv.Key))
        {
            if (!geo.Slots.TryGetValue(mask, out var slot)) continue;
            if (!FitText(items, slot, svg, mask, active?.Masks.Contains(mask) == true))
                overflow.Add(RegionName(v, mask));
        }

        for (var i = 0; i < v.Sets.Count; i++)
        {
            var count = regions.Where(kv => (kv.Key & (1 << i)) != 0).Sum(kv => kv.Value.Count);
            var a = geo.NameAnchors[i];
            var name = count > 0 ? $"{v.Sets[i].Name} ({count})" : v.Sets[i].Name;
            svg.Append($"<text class=\"venn-name\" x=\"{F(a.X)}\" y=\"{F(a.Y)}\" text-anchor=\"{a.Anchor}\" font-size=\"16\" font-weight=\"700\" style=\"fill:{Enc(v.Sets[i].Color)}\">{Enc(name)}</text>");
        }

        if (v.Outside.Count > 0)
        {
            // Elements outside every set: small block in the top-left corner of the universe.
            var lines = WrapElements(v.Outside, 18);
            var (ox, oy) = geo.OutsideAnchor;
            svg.Append($"<text class=\"venn-text{(active?.Masks.Contains(0) == true ? " venn-hl" : "")}\" data-mask=\"0\" font-size=\"13\">");
            for (var li = 0; li < Math.Min(lines.Count, 5); li++)
                svg.Append($"<tspan x=\"{F(ox + 22)}\" y=\"{F(oy + 22 + li * 17)}\">{Enc(lines[li])}</tspan>");
            svg.Append("</text>");
            if (lines.Count > 5) overflow.Add("mimo množiny");
        }
        svg.Append("</svg>");

        foreach (var region in overflow)
            v.Warnings.Add($"V oblasti „{region}“ je příliš mnoho prvků, nevejdou se do diagramu.");

        var html = new StringBuilder();
        html.Append("<figure class=\"post-venn\">");
        if (v.Title != null)
            html.Append($"<div class=\"post-graph-title\">{Enc(v.Title)}</div>");
        html.Append(svg);
        AppendOpsHtml(html, ops, active);
        foreach (var warning in v.Warnings)
            html.Append($"<div class=\"post-graph-warning\">{Enc(warning)}</div>");
        if (v.Notes.Count > 0)
        {
            html.Append("<figcaption>");
            foreach (var note in v.Notes)
                html.Append($"<span class=\"post-graph-note\">{Enc(note)}</span>");
            html.Append("</figcaption>");
        }
        html.Append("</figure>");
        return html.ToString();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Selectable terms (průnik, sjednocení, rozdíl, doplněk)
    // ──────────────────────────────────────────────────────────────────────

    private const string IdleText = "Klepni na pojem a ukáže se v diagramu.";

    private static readonly string[] OpCategories = ["prunik", "sjednoceni", "rozdil", "doplnek"];

    private sealed record Op(string Category, string Key, string Label, int[] Masks, string Description);

    private static string CategoryTitle(string category) => category switch
    {
        "prunik" => "Průnik",
        "sjednoceni" => "Sjednocení",
        "rozdil" => "Rozdíl",
        _ => "Doplněk",
    };

    // The terms offered for the chosen categories. Each one is a set of regions (bit masks:
    // bit i = "inside set i", 0 = outside all sets) plus a plain-language definition and the
    // elements that fall into it.
    private static List<Op> BuildOps(Venn v, Dictionary<int, List<string>> regions)
    {
        var ops = new List<Op>();
        var n = v.Sets.Count;
        if (n < 2) return ops;
        var full = (1 << n) - 1;

        string Name(int i) => v.Sets[i].Name;
        int[] Masks(Func<int, bool> pred) => Enumerable.Range(0, full + 1).Where(pred).ToArray();

        string Elements(int[] masks)
        {
            var items = new List<string>();
            foreach (var m in masks)
            {
                if (regions.TryGetValue(m, out var list)) items.AddRange(list);
                if (m == 0) items.AddRange(v.Outside);
            }
            items = items.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return items.Count == 0 ? " Prvky: žádné (∅)." : $" Prvky: {{{string.Join(", ", items)}}}.";
        }

        void Add(string category, string key, string label, int[] masks, string meaning) =>
            ops.Add(new Op(category, $"{category}:{key}", label, masks, meaning + Elements(masks)));

        string Join(IEnumerable<int> idx, string sep) => string.Join(sep, idx.Select(Name));
        var pairs = from i in Enumerable.Range(0, n) from j in Enumerable.Range(i + 1, n - i - 1) select (i, j);

        // prunik
        {
            foreach (var (i, j) in pairs)
                Add("prunik", $"{i},{j}", $"{Name(i)} ∩ {Name(j)}", Masks(m => (m >> i & 1) == 1 && (m >> j & 1) == 1),
                    $"Průnik množin {Name(i)} a {Name(j)} ({Name(i)} ∩ {Name(j)}) tvoří prvky, které patří do {Name(i)} i do {Name(j)}.");
            if (n == 3)
                Add("prunik", "0,1,2", $"{Join([0, 1, 2], " ∩ ")}", Masks(m => m == full),
                    $"Průnik všech tří množin ({Join([0, 1, 2], " ∩ ")}) tvoří prvky, které patří do všech tří množin zároveň.");
        }

        // sjednoceni
        {
            foreach (var (i, j) in pairs)
                Add("sjednoceni", $"{i},{j}", $"{Name(i)} ∪ {Name(j)}", Masks(m => (m >> i & 1) == 1 || (m >> j & 1) == 1),
                    $"Sjednocení množin {Name(i)} a {Name(j)} ({Name(i)} ∪ {Name(j)}) tvoří prvky, které patří aspoň do jedné z nich (prvky z průniku se počítají jen jednou).");
            if (n == 3)
                Add("sjednoceni", "0,1,2", $"{Join([0, 1, 2], " ∪ ")}", Masks(m => m != 0),
                    $"Sjednocení všech tří množin ({Join([0, 1, 2], " ∪ ")}) tvoří prvky, které patří aspoň do jedné z nich.");
        }

        // rozdil
        {
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var (a, b) = (i, j);
                    Add("rozdil", $"{a},{b}", $"{Name(a)} \\ {Name(b)}", Masks(m => (m >> a & 1) == 1 && (m >> b & 1) == 0),
                        $"Rozdíl {Name(a)} \\ {Name(b)} tvoří prvky, které patří do {Name(a)}, ale ne do {Name(b)}.");
                }
        }

        // doplnek
        {
            for (var i = 0; i < n; i++)
            {
                var a = i;
                Add("doplnek", $"{a}", $"{Name(a)}′", Masks(m => (m >> a & 1) == 0),
                    $"Doplněk množiny {Name(a)} ({Name(a)}′) tvoří všechny prvky univerza (rámečku U), které do {Name(a)} nepatří.");
            }
        }
        return ops;
    }

    // clipPaths (one per circle) and masks (one per region that must exclude some circles).
    private static void AppendRegionDefs(StringBuilder svg, Geometry geo, int n, string id)
    {
        var u = geo.UniverseRect;
        svg.Append("<defs>");
        for (var i = 0; i < n; i++)
        {
            var c = geo.Circles[i];
            svg.Append($"<clipPath id=\"{id}-c{i}\"><circle cx=\"{F(c.X)}\" cy=\"{F(c.Y)}\" r=\"{F(c.R)}\"/></clipPath>");
        }
        for (var mask = 0; mask < 1 << n; mask++)
        {
            var outside = Enumerable.Range(0, n).Where(i => (mask >> i & 1) == 0).ToList();
            if (outside.Count == 0) continue;
            svg.Append($"<mask id=\"{id}-m{mask}\" maskUnits=\"userSpaceOnUse\" x=\"{F(u.X)}\" y=\"{F(u.Y)}\" width=\"{F(u.W)}\" height=\"{F(u.H)}\">");
            svg.Append($"<rect x=\"{F(u.X)}\" y=\"{F(u.Y)}\" width=\"{F(u.W)}\" height=\"{F(u.H)}\" fill=\"#fff\"/>");
            foreach (var i in outside)
            {
                var c = geo.Circles[i];
                svg.Append($"<circle cx=\"{F(c.X)}\" cy=\"{F(c.Y)}\" r=\"{F(c.R)}\" fill=\"#000\"/>");
            }
            svg.Append("</mask>");
        }
        svg.Append("</defs>");
    }

    // One overlay per region: the intersection of the circles it is inside (nested clip paths),
    // minus the circles it is outside of (mask).
    private static void AppendRegionShapes(StringBuilder svg, Geometry geo, int n, string id, int[] activeMasks)
    {
        var u = geo.UniverseRect;
        for (var mask = 0; mask < 1 << n; mask++)
        {
            var inside = Enumerable.Range(0, n).Where(i => (mask >> i & 1) == 1).ToList();
            foreach (var i in inside) svg.Append($"<g clip-path=\"url(#{id}-c{i})\">");
            var maskAttr = inside.Count == n ? "" : $" mask=\"url(#{id}-m{mask})\"";
            var lit = activeMasks.Contains(mask) ? " venn-hl" : "";
            svg.Append($"<rect class=\"venn-region{lit}\" data-mask=\"{mask}\" x=\"{F(u.X)}\" y=\"{F(u.Y)}\" width=\"{F(u.W)}\" height=\"{F(u.H)}\"{maskAttr}/>");
            foreach (var _ in inside) svg.Append("</g>");
        }
    }

    private static void AppendOpsHtml(StringBuilder html, List<Op> ops, Op? active)
    {
        if (ops.Count == 0) return;
        html.Append("<div class=\"venn-ops\">");
        foreach (var group in ops.GroupBy(o => o.Category))
        {
            html.Append($"<div class=\"venn-op-row\"><span class=\"venn-op-group\">{CategoryTitle(group.Key)}</span>");
            foreach (var op in group)
                html.Append($"<button type=\"button\" class=\"venn-op\" aria-pressed=\"{(op == active ? "true" : "false")}\" data-masks=\"{string.Join(',', op.Masks)}\" data-desc=\"{Enc(op.Description)}\">{Enc(op.Label)}</button>");
            html.Append("</div>");
        }
        html.Append("<div class=\"venn-op-key\"><span class=\"venn-op-swatch\"></span> vybarvená část = výsledek pojmu</div>");
        html.Append($"<div class=\"venn-op-desc\" aria-live=\"polite\">{Enc(active?.Description ?? IdleText)}</div></div>");
    }

    private static string RegionName(Venn v, int mask)
    {
        var names = Enumerable.Range(0, v.Sets.Count).Where(i => (mask & (1 << i)) != 0).Select(i => v.Sets[i].Name).ToList();
        return names.Count == 1 ? $"jen {names[0]}" : string.Join(" ∩ ", names);
    }

    // Elements joined with ", " and wrapped greedily to a line length.
    private static List<string> WrapElements(List<string> items, int maxChars)
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        for (var i = 0; i < items.Count; i++)
        {
            var piece = i < items.Count - 1 ? items[i] + "," : items[i];
            if (current.Length > 0 && current.Length + 1 + piece.Length > maxChars)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(piece);
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines;
    }

    // Picks the largest font size at which the region's elements fit, then writes the text.
    // Returns false when even the smallest size does not fit (the text is still drawn, clipped by space).
    private static bool FitText(List<string> items, Slot slot, StringBuilder svg, int mask, bool lit)
    {
        double[] sizes = [14, 13, 12, 11, 10];
        List<string> lines = [];
        double size = sizes[^1];
        var fits = false;
        foreach (var s in sizes)
        {
            var maxChars = Math.Max(3, (int)Math.Floor(slot.W / (s * 0.56)));
            lines = WrapElements(items, maxChars);
            size = s;
            if (lines.Count * (s + 4) <= slot.H && lines.All(l => l.Length <= maxChars + 6)) { fits = true; break; }
        }

        var lineH = size + 4;
        svg.Append($"<text class=\"venn-text{(lit ? " venn-hl" : "")}\" data-mask=\"{mask}\" text-anchor=\"middle\" font-size=\"{F(size)}\">");
        for (var i = 0; i < lines.Count; i++)
        {
            var y = slot.Y + (i - (lines.Count - 1) / 2.0) * lineH + size * 0.35;
            svg.Append($"<tspan x=\"{F(slot.X)}\" y=\"{F(y)}\">{Enc(lines[i])}</tspan>");
        }
        svg.Append("</text>");
        return fits;
    }
}
