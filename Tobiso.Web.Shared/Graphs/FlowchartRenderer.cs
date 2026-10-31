using System.Text;
using System.Text.RegularExpressions;
using static Tobiso.Web.Shared.Graphs.DiagramSupport;

namespace Tobiso.Web.Shared.Graphs;

/// <summary>
/// Renders author-written flowchart blocks (vývojové diagramy) in post content to inline SVG:
/// <code>
/// [vyvoj1]
/// title: Je číslo sudé?
/// start(id: s): Začátek
/// decision(id: d): Zbytek po dělení 2 je 0?
/// process(id: a): Číslo je sudé
/// process(id: b): Číslo je liché
/// end(id: e): Konec
/// s -> d
/// d -> a (ano)
/// d -> b (ne)
/// a -> e
/// b -> e
/// [/vyvoj1]
/// </code>
/// Node kinds: start / end (pill), process (box), decision (diamond), io (parallelogram).
/// Layout is computed here (layers by longest path, barycenter ordering), loops and edges that
/// skip layers are routed around the diagram. Like <see cref="GraphRenderer"/> the block is
/// pulled out as a placeholder before any other text processing, so "->" is never turned into
/// an arrow icon, and everything is rendered server-side with theme-aware CSS classes.
/// </summary>
public static class FlowchartRenderer
{
    private static readonly Regex BlockRegex = new(
        @"^[ \t]*\[(vyvoj[\w-]*)\][ \t]*\r?\n(.*?)^[ \t]*\[/\1\][ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex EdgeRegex = new(
        @"^(?<from>[\p{L}\p{N}_]+)\s*(?:->|→)\s*(?<to>[\p{L}\p{N}_]+)(?:\s*\((?<label>[^)]*)\))?(?:\s+(?<color>\S+))?\s*$",
        RegexOptions.CultureInvariant);

    private const string PlaceholderPrefix = "TOBISOVYVOJPLACEHOLDER";
    private static readonly Regex PlaceholderRegex = new($@"(?:<p>\s*)?{PlaceholderPrefix}(\d+)X(?:\s*</p>)?");

    private enum Kind { Start, End, Process, Decision, Io }

    private static readonly Dictionary<string, Kind> KindAliases = new()
    {
        ["start"] = Kind.Start, ["zacatek"] = Kind.Start,
        ["end"] = Kind.End, ["konec"] = Kind.End, ["stop"] = Kind.End,
        ["process"] = Kind.Process, ["proces"] = Kind.Process, ["krok"] = Kind.Process, ["akce"] = Kind.Process,
        ["decision"] = Kind.Decision, ["podminka"] = Kind.Decision, ["rozhodnuti"] = Kind.Decision, ["vetveni"] = Kind.Decision,
        ["io"] = Kind.Io, ["vstup"] = Kind.Io, ["vystup"] = Kind.Io, ["data"] = Kind.Io,
    };

    /// <summary>
    /// Replaces every flowchart block in <paramref name="content"/> with a placeholder paragraph
    /// and returns the rendered HTML for each one, in order. Pair with <see cref="RestoreBlocks"/>.
    /// </summary>
    public static string ExtractBlocks(string content, List<string> rendered)
    {
        if (string.IsNullOrEmpty(content) || content.IndexOf("[vyvoj", StringComparison.OrdinalIgnoreCase) < 0)
            return content;

        return BlockRegex.Replace(content, m =>
        {
            var index = rendered.Count;
            rendered.Add(Render(m.Groups[2].Value, $"fc{index}-{StableHash(m.Value)}"));
            return $"\n\n{PlaceholderPrefix}{index}X\n\n";
        });
    }

    /// <summary>Renders the first flowchart block in <paramref name="text"/> (admin live preview).</summary>
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

    private sealed class Node
    {
        public string Id = "";
        public Kind Kind;
        public string Label = "";
        public string? Color;
        public List<string> Lines = [];
        public double W, H;          // screen size
        public double M, C;          // size along the main (flow) and cross axis
        public double Cm, Cc;        // centre along main / cross axis
        public int Layer;
    }

    private sealed class Edge
    {
        public int From, To;
        public string? Label;
        public string? Color;
        public bool Back;
    }

    private sealed class Diagram
    {
        public string? Title;
        public bool Right;
        public readonly List<Node> Nodes = [];
        public readonly List<Edge> Edges = [];
        public readonly List<string> Notes = [];
        public readonly List<string> Warnings = [];
    }

    private static Diagram Parse(string body)
    {
        var d = new Diagram();
        var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pendingEdges = new List<(string From, string To, string? Label, string? Color, string Raw)>();

        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;

            var em = EdgeRegex.Match(line);
            if (em.Success)
            {
                string? color = null;
                if (em.Groups["color"].Success)
                {
                    color = GraphRenderer.ResolveColor(em.Groups["color"].Value);
                    if (color == null) d.Warnings.Add($"Neznámá barva „{em.Groups["color"].Value}“.");
                }
                var label = em.Groups["label"].Success ? em.Groups["label"].Value.Trim() : null;
                pendingEdges.Add((em.Groups["from"].Value, em.Groups["to"].Value, string.IsNullOrEmpty(label) ? null : label, color, line));
                continue;
            }

            var lm = LineRegex.Match(line);
            if (!lm.Success) continue;
            var key = Normalize(lm.Groups["key"].Value);
            var value = lm.Groups["value"].Value.Trim();
            var args = ParseArgs(lm.Groups["args"].Success ? lm.Groups["args"].Value : null);

            if (key is "title" or "nazev" or "nadpis")
            {
                if (value.Length > 0) d.Title = value;
            }
            else if (key is "note" or "poznamka")
            {
                if (value.Length > 0) d.Notes.Add(value);
            }
            else if (key is "direction" or "smer")
            {
                d.Right = Normalize(value) is "right" or "doprava" or "vodorovne" or "horizontal" or "lr";
            }
            else if (KindAliases.TryGetValue(key, out var kind))
            {
                var id = args.GetValueOrDefault("id") ?? args.GetValueOrDefault("") ?? "";
                if (id.Length == 0)
                {
                    id = $"n{d.Nodes.Count + 1}";
                    d.Warnings.Add($"Uzel „{value}“ nemá id (např. {lm.Groups["key"].Value}(id: a)), nelze na něj navázat šipku.");
                }
                if (ids.ContainsKey(id))
                {
                    d.Warnings.Add($"Id „{id}“ je použité vícekrát, druhý uzel je ignorován.");
                    continue;
                }

                string? color = null;
                var colorToken = args.GetValueOrDefault("color") ?? args.GetValueOrDefault("barva");
                if (colorToken != null)
                {
                    color = GraphRenderer.ResolveColor(colorToken);
                    if (color == null) d.Warnings.Add($"Neznámá barva „{colorToken}“.");
                }

                ids[id] = d.Nodes.Count;
                d.Nodes.Add(new Node { Id = id, Kind = kind, Label = value, Color = color });
            }
        }

        foreach (var (from, to, label, color, raw) in pendingEdges)
        {
            if (!ids.TryGetValue(from, out var fi) || !ids.TryGetValue(to, out var ti))
            {
                d.Warnings.Add($"Šipka „{raw}“ odkazuje na neznámý uzel.");
                continue;
            }
            d.Edges.Add(new Edge { From = fi, To = ti, Label = label, Color = color });
        }

        if (d.Nodes.Count > 1)
        {
            var connected = new HashSet<int>(d.Edges.SelectMany(e => new[] { e.From, e.To }));
            foreach (var (n, i) in d.Nodes.Select((n, i) => (n, i)))
                if (!connected.Contains(i))
                    d.Warnings.Add($"Uzel „{n.Id}“ nemá žádnou šipku.");
        }

        return d;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Layout
    // ──────────────────────────────────────────────────────────────────────

    private const double FontSize = 14, CharW = 7.4, LineH = 18;
    private const double InkPad = 16;

    private static void Measure(Node n, bool right)
    {
        var wrap = n.Kind == Kind.Decision ? 16 : 24;
        n.Lines = Wrap(n.Label, wrap);
        var textW = n.Lines.Max(l => l.Length) * CharW;
        var textH = n.Lines.Count * LineH;

        switch (n.Kind)
        {
            case Kind.Decision:
                n.W = Math.Max(textW * 1.9 + 36, 130);
                n.H = Math.Max(textH * 1.9 + 24, 72);
                break;
            case Kind.Io:
                n.W = Math.Max(textW + 60, 96);
                n.H = Math.Max(textH + 20, 42);
                break;
            case Kind.Start or Kind.End:
                n.W = Math.Max(textW + 44, 92);
                n.H = Math.Max(textH + 18, 38);
                break;
            default:
                n.W = Math.Max(textW + 32, 92);
                n.H = Math.Max(textH + 20, 42);
                break;
        }
        n.M = right ? n.W : n.H;
        n.C = right ? n.H : n.W;
    }

    private sealed class Layout
    {
        public List<List<int>> Layers = [];
        public double[] LayerStart = [];
        public double[] LayerEnd = [];
    }

    private static Layout DoLayout(Diagram d)
    {
        var n = d.Nodes.Count;
        foreach (var node in d.Nodes) Measure(node, d.Right);

        var outs = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
        var ins = Enumerable.Range(0, n).Select(_ => new List<int>()).ToArray();
        for (var ei = 0; ei < d.Edges.Count; ei++)
        {
            outs[d.Edges[ei].From].Add(ei);
            ins[d.Edges[ei].To].Add(ei);
        }

        // Back edges (loops) are found by DFS from the roots and left out of the layering.
        var state = new int[n];
        void Dfs(int u)
        {
            state[u] = 1;
            foreach (var ei in outs[u])
            {
                var e = d.Edges[ei];
                if (state[e.To] == 1) e.Back = true;
                else if (state[e.To] == 0) Dfs(e.To);
            }
            state[u] = 2;
        }
        for (var i = 0; i < n; i++) if (ins[i].Count == 0 && state[i] == 0) Dfs(i);
        for (var i = 0; i < n; i++) if (state[i] == 0) Dfs(i);

        // Layer = longest path from a root over the remaining (acyclic) edges.
        for (var pass = 0; pass <= n; pass++)
        {
            var changed = false;
            foreach (var e in d.Edges.Where(e => !e.Back))
                if (d.Nodes[e.To].Layer < d.Nodes[e.From].Layer + 1)
                {
                    d.Nodes[e.To].Layer = d.Nodes[e.From].Layer + 1;
                    changed = true;
                }
            if (!changed) break;
        }

        var layerCount = d.Nodes.Max(x => x.Layer) + 1;
        var layout = new Layout();
        for (var l = 0; l < layerCount; l++) layout.Layers.Add([]);
        for (var i = 0; i < n; i++) layout.Layers[d.Nodes[i].Layer].Add(i);

        List<int> Neighbours(int node, bool parents) =>
            (parents ? ins[node] : outs[node])
            .Select(ei => d.Edges[ei])
            .Where(e => !e.Back)
            .Select(e => parents ? e.From : e.To)
            .ToList();

        // Order inside layers by the barycenter of the neighbours (ties keep declaration order).
        var pos = new double[n];
        void Reindex()
        {
            foreach (var layer in layout.Layers)
                for (var k = 0; k < layer.Count; k++) pos[layer[k]] = k;
        }
        Reindex();
        for (var sweep = 0; sweep < 4; sweep++)
        {
            var down = sweep % 2 == 0;
            for (var step = 1; step < layerCount; step++)
            {
                var l = down ? step : layerCount - 1 - step;
                var key = layout.Layers[l].ToDictionary(i => i, i =>
                {
                    var nb = Neighbours(i, down);
                    return nb.Count == 0 ? pos[i] : nb.Average(j => pos[j]);
                });
                layout.Layers[l] = layout.Layers[l].OrderBy(i => key[i]).ToList();
                Reindex();
            }
        }

        // Cross-axis coordinates: centred rows first, then pulled towards the neighbours.
        var crossGap = d.Right ? 28.0 : 40.0;
        foreach (var layer in layout.Layers)
        {
            var total = layer.Sum(i => d.Nodes[i].C) + crossGap * (layer.Count - 1);
            var cursor = -total / 2;
            foreach (var i in layer)
            {
                d.Nodes[i].Cc = cursor + d.Nodes[i].C / 2;
                cursor += d.Nodes[i].C + crossGap;
            }
        }

        void Relax(int l, bool useParents)
        {
            var list = layout.Layers[l];
            var desired = new double[list.Count];
            for (var k = 0; k < list.Count; k++)
            {
                var nb = Neighbours(list[k], useParents);
                desired[k] = nb.Count > 0 ? nb.Average(j => d.Nodes[j].Cc) : d.Nodes[list[k]].Cc;
            }
            var actual = new double[list.Count];
            var prevRight = double.NegativeInfinity;
            for (var k = 0; k < list.Count; k++)
            {
                var node = d.Nodes[list[k]];
                actual[k] = Math.Max(desired[k], prevRight + crossGap + node.C / 2);
                prevRight = actual[k] + node.C / 2;
            }
            var shift = Enumerable.Range(0, list.Count).Average(k => desired[k] - actual[k]);
            for (var k = 0; k < list.Count; k++) d.Nodes[list[k]].Cc = actual[k] + shift;
        }

        for (var iter = 0; iter < 3; iter++)
        {
            for (var l = 1; l < layerCount; l++) Relax(l, true);
            for (var l = layerCount - 2; l >= 0; l--) Relax(l, false);
        }
        for (var l = 1; l < layerCount; l++) Relax(l, true);

        // Main-axis coordinates.
        var mainGap = d.Right ? 76.0 : 58.0;
        layout.LayerStart = new double[layerCount];
        layout.LayerEnd = new double[layerCount];
        var m = 0.0;
        for (var l = 0; l < layerCount; l++)
        {
            var size = layout.Layers[l].Max(i => d.Nodes[i].M);
            layout.LayerStart[l] = m;
            layout.LayerEnd[l] = m + size;
            foreach (var i in layout.Layers[l]) d.Nodes[i].Cm = m + size / 2;
            m += size + mainGap;
        }
        return layout;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Rendering
    // ──────────────────────────────────────────────────────────────────────

    private static string Render(string body, string id)
    {
        var d = Parse(body);
        if (d.Nodes.Count == 0)
        {
            var reason = d.Warnings.Count > 0
                ? Enc(string.Join(" ", d.Warnings))
                : "Chybí uzly (<code>start(id: s): Začátek</code>).";
            return $"<div class=\"post-graph-error\">Vývojový diagram se nepodařilo vykreslit – {reason}</div>";
        }

        var layout = DoLayout(d);

        (double X, double Y) P(double main, double cross) => d.Right ? (main, cross) : (cross, main);

        var crossMax = d.Nodes.Max(x => x.Cc + x.C / 2);
        var crossMin = d.Nodes.Min(x => x.Cc - x.C / 2);
        var channelUse = new Dictionary<int, int> { [1] = 0, [-1] = 0 };

        // Route every edge as an orthogonal polyline in screen space.
        var routes = new List<List<(double X, double Y)>>();
        foreach (var e in d.Edges)
        {
            var u = d.Nodes[e.From];
            var v = d.Nodes[e.To];
            var span = v.Layer - u.Layer;
            var pts = new List<(double X, double Y)>();

            if (!e.Back && span == 1)
            {
                var dc = v.Cc - u.Cc;
                var siblings = d.Edges.Count(x => x.From == e.From && !x.Back);
                if (Math.Abs(dc) < 2)
                {
                    pts.Add(P(u.Cm + u.M / 2, u.Cc));
                    pts.Add(P(v.Cm - v.M / 2, u.Cc));
                }
                else if (u.Kind == Kind.Decision && siblings > 1 && Math.Abs(dc) > u.C / 2 + 8)
                {
                    var sign = Math.Sign(dc);
                    pts.Add(P(u.Cm, u.Cc + sign * u.C / 2));
                    pts.Add(P(u.Cm, v.Cc));
                    pts.Add(P(v.Cm - v.M / 2, v.Cc));
                }
                else
                {
                    var mid = (layout.LayerEnd[u.Layer] + layout.LayerStart[v.Layer]) / 2;
                    pts.Add(P(u.Cm + u.M / 2, u.Cc));
                    pts.Add(P(mid, u.Cc));
                    pts.Add(P(mid, v.Cc));
                    pts.Add(P(v.Cm - v.M / 2, v.Cc));
                }
            }
            else
            {
                // Loops and edges skipping layers run along the outside of the diagram.
                var side = e.Back ? 1 : (v.Cc >= u.Cc ? 1 : -1);
                var ch = side > 0
                    ? crossMax + 26 + 14 * channelUse[1]++
                    : crossMin - 26 - 14 * channelUse[-1]++;
                var startM = u.Cm;
                var endM = v.Cm;
                if (e.From == e.To) { startM -= Math.Min(10, u.M / 4); endM += Math.Min(10, u.M / 4); }
                pts.Add(P(startM, u.Cc + side * u.C / 2));
                pts.Add(P(startM, ch));
                pts.Add(P(endM, ch));
                pts.Add(P(endM, v.Cc + side * v.C / 2));
            }
            routes.Add(Dedupe(pts));
        }

        // Screen positions of nodes.
        foreach (var node in d.Nodes)
        {
            var (cx, cy) = P(node.Cm, node.Cc);
            node.Cm = cx; // reuse as screen centre from here on
            node.Cc = cy;
        }

        var minX = d.Nodes.Min(x => x.Cm - x.W / 2);
        var maxX = d.Nodes.Max(x => x.Cm + x.W / 2);
        var minY = d.Nodes.Min(x => x.Cc - x.H / 2);
        var maxY = d.Nodes.Max(x => x.Cc + x.H / 2);
        foreach (var p in routes.SelectMany(r => r))
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        minX -= InkPad + 8; maxX += InkPad + 8; minY -= InkPad; maxY += InkPad;
        var width = maxX - minX;
        var height = maxY - minY;

        // Arrow heads: one marker for the default colour, one per custom colour.
        var markers = new Dictionary<string, string>();
        string MarkerFor(string? color)
        {
            var key = color ?? "";
            if (!markers.TryGetValue(key, out var mid))
            {
                mid = color == null ? $"{id}-ah" : $"{id}-ah{markers.Count}";
                markers[key] = mid;
            }
            return mid;
        }

        var edgeSvg = new StringBuilder();
        var labelSvg = new StringBuilder();
        for (var i = 0; i < d.Edges.Count; i++)
        {
            var e = d.Edges[i];
            var pts = routes[i];
            if (pts.Count < 2) continue;
            var colorStyle = e.Color != null ? $" style=\"stroke:{Enc(e.Color)}\"" : "";
            edgeSvg.Append($"<path class=\"fc-edge\"{colorStyle} d=\"{PathD(pts)}\" marker-end=\"url(#{MarkerFor(e.Color)})\"/>");

            if (e.Label != null)
            {
                // Sit on the first segment that is long enough to carry the label chip.
                var segIdx = 0;
                if (Dist(pts[0], pts[1]) < 36 && pts.Count > 2) segIdx = 1;
                var a = pts[segIdx];
                var b = pts[segIdx + 1];
                var lx = (a.X + b.X) / 2;
                var ly = (a.Y + b.Y) / 2;
                var labelW = e.Label.Length * 6.8 + 12;
                var textColor = e.Color != null ? $" style=\"fill:{Enc(e.Color)}\"" : "";
                labelSvg.Append($"<rect class=\"fc-label-bg\" x=\"{F(lx - labelW / 2)}\" y=\"{F(ly - 10)}\" width=\"{F(labelW)}\" height=\"20\" rx=\"5\"/>");
                labelSvg.Append($"<text class=\"fc-edge-label\"{textColor} x=\"{F(lx)}\" y=\"{F(ly + 4.5)}\" text-anchor=\"middle\" font-size=\"13\" font-weight=\"600\">{Enc(e.Label)}</text>");
            }
        }

        var nodeSvg = new StringBuilder();
        foreach (var node in d.Nodes)
        {
            var cx = node.Cm;
            var cy = node.Cc;
            var x = cx - node.W / 2;
            var y = cy - node.H / 2;
            var cls = node.Kind switch
            {
                Kind.Start or Kind.End => "fc-node fc-node--terminal",
                Kind.Decision => "fc-node fc-node--decision",
                Kind.Io => "fc-node fc-node--io",
                _ => "fc-node",
            };
            var style = node.Color != null
                ? $" style=\"fill:{Enc(node.Color)};fill-opacity:.2;stroke:{Enc(node.Color)}\""
                : "";

            nodeSvg.Append(node.Kind switch
            {
                Kind.Start or Kind.End => $"<rect class=\"{cls}\"{style} x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(node.W)}\" height=\"{F(node.H)}\" rx=\"{F(node.H / 2)}\"/>",
                Kind.Decision => $"<polygon class=\"{cls}\"{style} points=\"{F(cx)},{F(y)} {F(x + node.W)},{F(cy)} {F(cx)},{F(y + node.H)} {F(x)},{F(cy)}\"/>",
                Kind.Io => $"<polygon class=\"{cls}\"{style} points=\"{F(x + 14)},{F(y)} {F(x + node.W)},{F(y)} {F(x + node.W - 14)},{F(y + node.H)} {F(x)},{F(y + node.H)}\"/>",
                _ => $"<rect class=\"{cls}\"{style} x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(node.W)}\" height=\"{F(node.H)}\" rx=\"6\"/>",
            });

            nodeSvg.Append($"<text class=\"fc-text\" text-anchor=\"middle\" font-size=\"{F(FontSize)}\">");
            for (var li = 0; li < node.Lines.Count; li++)
            {
                var ty = cy + (li - (node.Lines.Count - 1) / 2.0) * LineH + 5;
                nodeSvg.Append($"<tspan x=\"{F(cx)}\" y=\"{F(ty)}\">{Enc(node.Lines[li])}</tspan>");
            }
            nodeSvg.Append("</text>");
        }

        var aria = d.Title ?? "Vývojový diagram";
        var svg = new StringBuilder();
        svg.Append($"<svg class=\"fc-svg\" viewBox=\"{F(minX)} {F(minY)} {F(width)} {F(height)}\" style=\"max-width:{F(width)}px\" role=\"img\" aria-label=\"{Enc(aria)}\" xmlns=\"http://www.w3.org/2000/svg\">");
        svg.Append("<defs>");
        foreach (var (color, markerId) in markers)
        {
            var fill = color.Length > 0 ? $" style=\"fill:{Enc(color)}\"" : "";
            svg.Append($"<marker id=\"{markerId}\" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"11\" markerHeight=\"11\" markerUnits=\"userSpaceOnUse\" orient=\"auto\"><path class=\"fc-arrowhead\"{fill} d=\"M0,0 L10,5 L0,10 z\"/></marker>");
        }
        svg.Append("</defs>");
        svg.Append(edgeSvg).Append(nodeSvg).Append(labelSvg).Append("</svg>");

        var html = new StringBuilder();
        html.Append("<figure class=\"post-flowchart\">");
        if (d.Title != null)
            html.Append($"<div class=\"post-graph-title\">{Enc(d.Title)}</div>");
        html.Append(svg);
        foreach (var warning in d.Warnings)
            html.Append($"<div class=\"post-graph-warning\">{Enc(warning)}</div>");
        if (d.Notes.Count > 0)
        {
            html.Append("<figcaption>");
            foreach (var note in d.Notes)
                html.Append($"<span class=\"post-graph-note\">{Enc(note)}</span>");
            html.Append("</figcaption>");
        }
        html.Append("</figure>");
        return html.ToString();
    }

    private static double Dist((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static List<(double X, double Y)> Dedupe(List<(double X, double Y)> pts)
    {
        var result = new List<(double X, double Y)>();
        foreach (var p in pts)
            if (result.Count == 0 || Dist(result[^1], p) > 0.01) result.Add(p);
        return result;
    }

    // Polyline with small rounded corners.
    private static string PathD(List<(double X, double Y)> p, double radius = 8)
    {
        var sb = new StringBuilder($"M{F(p[0].X)},{F(p[0].Y)}");
        for (var i = 1; i < p.Count - 1; i++)
        {
            var a = p[i - 1];
            var b = p[i];
            var c = p[i + 1];
            var l1 = Dist(a, b);
            var l2 = Dist(b, c);
            var r = Math.Min(radius, Math.Min(l1, l2) / 2);
            if (r < 0.5)
            {
                sb.Append($" L{F(b.X)},{F(b.Y)}");
                continue;
            }
            var p1 = (X: b.X + (a.X - b.X) / l1 * r, Y: b.Y + (a.Y - b.Y) / l1 * r);
            var p2 = (X: b.X + (c.X - b.X) / l2 * r, Y: b.Y + (c.Y - b.Y) / l2 * r);
            sb.Append($" L{F(p1.X)},{F(p1.Y)} Q{F(b.X)},{F(b.Y)} {F(p2.X)},{F(p2.Y)}");
        }
        if (p.Count > 1) sb.Append($" L{F(p[^1].X)},{F(p[^1].Y)}");
        return sb.ToString();
    }
}
