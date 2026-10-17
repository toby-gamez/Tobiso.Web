using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Tobiso.Web.Shared.Graphs;

/// <summary>
/// Renders author-written graph blocks in post content to inline SVG:
/// <code>
/// [graf1]
/// x(t [s]): 0-10
/// y(v [m/s]): 0-50 (10)            // (10) = tick step
/// curve1(name: robot1): [0;0] [6;7] [8;7] linear RED
/// point1(name: A): [5;8] LIGHT_BLUE
/// note: Červená křivka zobrazuje rychlost
/// [/graf1]
/// </code>
/// The block tag must start with "graf" and be closed by the same name. Unknown
/// lines are ignored so a typo never breaks the rest of the article. Everything
/// is rendered server-side (no JS charting lib), and colours of axes/grid/text
/// come from CSS classes so the graph follows the light/dark theme.
/// </summary>
public static class GraphRenderer
{
    private static readonly Regex BlockRegex = new(
        @"^[ \t]*\[(graf[\w-]*)\][ \t]*\r?\n(.*?)^[ \t]*\[/\1\][ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex LineRegex = new(
        @"^(?<key>[\p{L}_][\p{L}\p{N}_]*)\s*(?:\((?<args>[^)]*)\))?\s*:\s*(?<value>.*)$",
        RegexOptions.CultureInvariant);

    private const string Num = @"-?\d+(?:[.,]\d+)?";

    private static readonly Regex RangeRegex = new(
        $@"^(?<min>{Num})\s*(?:-|–|\.\.)\s*(?<max>{Num})\s*(?:\(\s*(?<step>\d+(?:[.,]\d+)?)\s*\))?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex PointRegex = new(
        $@"\[\s*(?<x>{Num})\s*;\s*(?<y>{Num})\s*\]",
        RegexOptions.CultureInvariant);

    private static readonly Regex HexColorRegex = new(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$");

    // Placeholder that survives every text pre-processing step (no slashes, arrows,
    // dots, pipes...) and that Markdig renders as a plain <p>TOKEN</p> paragraph.
    private const string PlaceholderPrefix = "TOBISOGRAFPLACEHOLDER";
    private static readonly Regex PlaceholderRegex = new($@"(?:<p>\s*)?{PlaceholderPrefix}(\d+)X(?:\s*</p>)?");

    private static readonly Dictionary<string, string> NamedColors = new()
    {
        ["RED"] = "#d1495b", ["CERVENA"] = "#d1495b",
        ["DARK_RED"] = "#8f1d2c", ["TMAVE_CERVENA"] = "#8f1d2c",
        ["BLUE"] = "#2f6fdf", ["MODRA"] = "#2f6fdf",
        ["LIGHT_BLUE"] = "#3fa7e0", ["SVETLE_MODRA"] = "#3fa7e0",
        ["DARK_BLUE"] = "#1d3f8f", ["TMAVE_MODRA"] = "#1d3f8f",
        ["GREEN"] = "#3a9d5d", ["ZELENA"] = "#3a9d5d",
        ["LIGHT_GREEN"] = "#7cc66e", ["SVETLE_ZELENA"] = "#7cc66e",
        ["DARK_GREEN"] = "#22663a", ["TMAVE_ZELENA"] = "#22663a",
        ["ORANGE"] = "#e8833a", ["ORANZOVA"] = "#e8833a",
        ["YELLOW"] = "#d9a400", ["ZLUTA"] = "#d9a400",
        ["PURPLE"] = "#8e5cc4", ["FIALOVA"] = "#8e5cc4",
        ["PINK"] = "#e05a9b", ["RUZOVA"] = "#e05a9b",
        ["BROWN"] = "#8a5a3b", ["HNEDA"] = "#8a5a3b",
        ["GRAY"] = "#8a8580", ["GREY"] = "#8a8580", ["SEDA"] = "#8a8580",
        // Black/white follow the theme's text colour so they stay visible in dark mode.
        ["BLACK"] = "currentColor", ["CERNA"] = "currentColor",
        ["WHITE"] = "currentColor", ["BILA"] = "currentColor",
    };

    private static readonly string[] DefaultPalette =
        ["#d1495b", "#2f6fdf", "#3a9d5d", "#e8833a", "#8e5cc4", "#3fa7e0", "#d9a400", "#e05a9b"];

    /// <summary>
    /// Replaces every graph block in <paramref name="content"/> with a placeholder paragraph
    /// and returns the rendered HTML for each one, in order. Run it before any other text
    /// processing so fraction/arrow/strikethrough rules never touch the graph source, then
    /// call <see cref="RestoreBlocks"/> as the very last step on the final HTML.
    /// </summary>
    public static string ExtractBlocks(string content, List<string> rendered)
    {
        if (string.IsNullOrEmpty(content) || content.IndexOf("[graf", StringComparison.OrdinalIgnoreCase) < 0)
            return content;

        return BlockRegex.Replace(content, m =>
        {
            var index = rendered.Count;
            rendered.Add(Render(m.Groups[2].Value, $"pg{index}-{StableHash(m.Value)}"));
            return $"\n\n{PlaceholderPrefix}{index}X\n\n";
        });
    }

    /// <summary>Renders the first graph block in <paramref name="text"/> (e.g. for the admin
    /// graph builder's live preview); empty when there is none.</summary>
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
            var index = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            return index < rendered.Count ? rendered[index] : m.Value;
        });
    }

    // ──────────────────────────────────────────────────────────────────────
    // Parsing
    // ──────────────────────────────────────────────────────────────────────

    private sealed class Axis
    {
        public string Label = "";
        public double? Min, Max, Step;
    }

    private sealed record Series(string? Name, List<(double X, double Y)> Points, bool Smooth, bool Dashed, bool ShowDots, string Color);
    private sealed record Marker(string? Name, double X, double Y, string Color);
    private sealed record FunctionPlot(string? Name, Func<double, double> F, double? From, double? To, bool Dashed, string Color);

    private sealed class Graph
    {
        public readonly Axis X = new();
        public readonly Axis Y = new();
        public readonly List<Series> Series = [];
        public readonly List<Marker> Markers = [];
        public readonly List<FunctionPlot> Functions = [];
        public readonly List<string> Notes = [];
        public readonly List<string> Errors = [];
        public string? Title;
        public bool HasAxis;
    }

    private static Graph Parse(string body)
    {
        var graph = new Graph();
        var colorIndex = 0;

        foreach (var rawLine in Regex.Split(body, @"\r?\n"))
        {
            // "//" starts a comment, but only at line start or after whitespace so URLs survive.
            var line = Regex.Replace(rawLine, @"(^|\s)//.*$", "").Trim();
            if (line.Length == 0) continue;

            var m = LineRegex.Match(line);
            if (!m.Success) continue;

            var key = Regex.Replace(Normalize(m.Groups["key"].Value), @"\d+$", "");
            var args = m.Groups["args"].Success ? m.Groups["args"].Value.Trim() : "";
            var value = m.Groups["value"].Value.Trim();

            switch (key)
            {
                case "x":
                    ParseAxis(graph.X, args, value);
                    graph.HasAxis = true;
                    break;
                case "y":
                    ParseAxis(graph.Y, args, value);
                    graph.HasAxis = true;
                    break;
                case "curve" or "krivka" or "line" or "cara":
                {
                    var (name, argColor, _, _) = ParseArgs(args);
                    var points = PointRegex.Matches(value).Select(p => (ParseNum(p.Groups["x"].Value), ParseNum(p.Groups["y"].Value))).ToList();
                    if (points.Count == 0) break;

                    bool smooth = false, dashed = false, showDots = true;
                    string? color = argColor;
                    foreach (var token in PointRegex.Replace(value, " ").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                    {
                        switch (Normalize(token))
                        {
                            case "linear" or "linearni" or "primka" or "rovna": smooth = false; break;
                            case "smooth" or "curved" or "hladka" or "zakrivena" or "oblouk": smooth = true; break;
                            case "dashed" or "carkovana" or "prerusovana": dashed = true; break;
                            case "nodots" or "bezbodu": showDots = false; break;
                            default: color ??= ResolveColor(token); break;
                        }
                    }
                    graph.Series.Add(new Series(name, points, smooth, dashed, showDots, color ?? DefaultPalette[colorIndex++ % DefaultPalette.Length]));
                    break;
                }
                case "point" or "bod":
                {
                    var (name, argColor, _, _) = ParseArgs(args);
                    var p = PointRegex.Match(value);
                    if (!p.Success) break;

                    var color = argColor;
                    foreach (var token in PointRegex.Replace(value, " ").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                        color ??= ResolveColor(token);
                    graph.Markers.Add(new Marker(name, ParseNum(p.Groups["x"].Value), ParseNum(p.Groups["y"].Value),
                        color ?? DefaultPalette[colorIndex++ % DefaultPalette.Length]));
                    break;
                }
                case "function" or "funkce" or "fce":
                {
                    var (name, argColor, from, to) = ParseArgs(args);

                    // Trailing words that are options or colours ("… RED dashed"); the rest is the formula.
                    var words = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
                    bool dashed = false;
                    string? color = argColor;
                    while (words.Count > 1)
                    {
                        var last = words[^1];
                        var option = Normalize(last);
                        if (option is "dashed" or "carkovana" or "prerusovana") dashed = true;
                        else if (option is "linear" or "linearni" or "smooth" or "hladka" or "nodots" or "bezbodu") { }
                        else if (ResolveColor(last) is { } c) color ??= c;
                        else break;
                        words.RemoveAt(words.Count - 1);
                    }

                    var formula = string.Join(" ", words);
                    if (!GraphExpression.TryParse(formula, out var fn, out var error))
                    {
                        graph.Errors.Add($"Funkci „{formula}“ se nepodařilo přečíst: {error}.");
                        break;
                    }
                    graph.Functions.Add(new FunctionPlot(name ?? formula, fn, from, to, dashed,
                        color ?? DefaultPalette[colorIndex++ % DefaultPalette.Length]));
                    break;
                }
                case "note" or "poznamka" or "popis":
                    if (value.Length > 0) graph.Notes.Add(value);
                    break;
                case "title" or "nazev":
                    if (value.Length > 0) graph.Title = value;
                    break;
            }
        }

        return graph;
    }

    private static void ParseAxis(Axis axis, string args, string value)
    {
        axis.Label = args;
        var m = RangeRegex.Match(value);
        if (!m.Success) return;

        var min = ParseNum(m.Groups["min"].Value);
        var max = ParseNum(m.Groups["max"].Value);
        axis.Min = Math.Min(min, max);
        axis.Max = Math.Max(min, max);
        if (m.Groups["step"].Success)
        {
            var step = ParseNum(m.Groups["step"].Value);
            if (step > 0) axis.Step = step;
        }
    }

    // "(name: robot1, color: RED)", just "(robot1)", or for functions "(od: 0, do: 6,28)".
    // A comma splits arguments only before a "key:" or when followed by a space, so decimal
    // commas ("od: 0,5") survive.
    private static (string? Name, string? Color, double? From, double? To) ParseArgs(string args)
    {
        string? name = null, color = null;
        double? from = null, to = null;
        if (args.Length == 0) return (name, color, from, to);

        foreach (var part in Regex.Split(args, @",(?=\s*[\p{L}_]+\s*:)|,\s+"))
        {
            var kv = part.Split(':', 2);
            if (kv.Length == 1)
            {
                var bare = kv[0].Trim();
                if (bare.Length > 0) name ??= bare;
                continue;
            }

            var k = Normalize(kv[0]);
            var v = kv[1].Trim();
            if (v.Length == 0) continue;
            if (k is "name" or "nazev" or "jmeno" or "popisek" or "label") name = v;
            else if (k is "color" or "colour" or "barva") color = ResolveColor(v);
            else if (k is "from" or "od" && Regex.IsMatch(v, $"^{Num}$")) from = ParseNum(v);
            else if (k is "to" or "do" && Regex.IsMatch(v, $"^{Num}$")) to = ParseNum(v);
        }

        return (name, color, from, to);
    }

    /// <summary>Hex (or "currentColor") for a colour name or hex code, null when unknown.</summary>
    public static string? ResolveColor(string token)
    {
        var t = token.Trim();
        if (HexColorRegex.IsMatch(t)) return t;
        var key = Normalize(t).ToUpperInvariant().Replace('-', '_');
        return NamedColors.TryGetValue(key, out var c) ? c : null;
    }

    private static double ParseNum(string s) =>
        double.Parse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

    // Lower-case + strip diacritics, so "Křivka"/"krivka" and "ČERVENÁ"/"cervena" match.
    private static string Normalize(string s)
    {
        var decomposed = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Rendering
    // ──────────────────────────────────────────────────────────────────────

    private const double Width = 640, Height = 400;
    private const double MarginLeft = 90, MarginRight = 28, MarginTop = 28, MarginBottom = 64;
    private const double PlotW = Width - MarginLeft - MarginRight, PlotH = Height - MarginTop - MarginBottom;

    private static string Render(string body, string id)
    {
        var g = Parse(body);
        if (!g.HasAxis && g.Series.Count == 0 && g.Markers.Count == 0 && g.Functions.Count == 0)
        {
            var reason = g.Errors.Count > 0
                ? Enc(string.Join(" ", g.Errors))
                : "Chybí osy (<code>x: 0-10</code>) i data (<code>curve: [0;0] [1;1]</code>).";
            return $"<div class=\"post-graph-error\">Graf se nepodařilo vykreslit – {reason}</div>";
        }

        var allX = g.Series.SelectMany(s => s.Points.Select(p => p.X)).Concat(g.Markers.Select(p => p.X))
            .Concat(g.Functions.SelectMany(f => new[] { f.From, f.To }).OfType<double>()).ToList();
        // A function on its own (no range, no domain, no points) gets a symmetric -10..10 window.
        if (allX.Count == 0 && g.Functions.Count > 0) allX.AddRange([-10, 10]);
        var (xMin, xMax) = ResolveRange(g.X, allX);
        var xStep = ResolveStep(g.X.Step, xMin, xMax);
        // Every step gets a labelled tick and the axes end on one, so e.g. "0-45 (10)" runs to 50.
        (xMin, xMax) = SnapToStep(xMin, xMax, xStep);

        // Functions are sampled once the x range is known; their values then feed the y range.
        var samples = g.Functions.Select(f => SampleFunction(f, xMin, xMax)).ToList();
        var allY = g.Series.SelectMany(s => s.Points.Select(p => p.Y)).Concat(g.Markers.Select(p => p.Y)).ToList();
        allY.AddRange(FunctionYExtent(samples));
        var (yMin, yMax) = ResolveRange(g.Y, allY);
        var yStep = ResolveStep(g.Y.Step, yMin, yMax);
        (yMin, yMax) = SnapToStep(yMin, yMax, yStep);

        double Sx(double x) => MarginLeft + (x - xMin) / (xMax - xMin) * PlotW;
        double Sy(double y) => MarginTop + PlotH - (y - yMin) / (yMax - yMin) * PlotH;

        var plotBottom = MarginTop + PlotH;
        var plotRight = MarginLeft + PlotW;

        // Axes cross at the origin. With only non-negative data (auto range) the origin is the
        // bottom-left corner, so just the 1st quadrant is drawn; negative values extend the range
        // and the axes move inside, showing only the quadrants that are actually needed.
        var axisY = Sy(Math.Clamp(0, yMin, yMax)); // screen y of the x axis
        var axisX = Sx(Math.Clamp(0, xMin, xMax)); // screen x of the y axis
        var xAxisAtEdge = axisY >= plotBottom - 0.5;
        // Only negative y: the x axis runs along the top, so its numbers go above it (and the
        // axis title to the bottom) instead of colliding with the y labels and the data below.
        var xAxisAtTop = !xAxisAtEdge && axisY <= MarginTop + 0.5;
        var yAxisAtEdge = axisX <= MarginLeft + 0.5;
        // Origin strictly inside: print a single "0" at the crossing instead of one per axis.
        var interiorOrigin = xMin < 0 && xMax > 0 && yMin < 0 && yMax > 0;

        var aria = g.Title ?? (g.Notes.Count > 0 ? g.Notes[0] : "Graf");
        var svg = new StringBuilder();
        svg.Append($"<svg class=\"post-graph-svg\" viewBox=\"0 0 {F(Width)} {F(Height)}\" role=\"img\" aria-label=\"{Enc(aria)}\" xmlns=\"http://www.w3.org/2000/svg\">");
        svg.Append($"<defs><clipPath id=\"{id}-clip\"><rect x=\"{F(MarginLeft - 4)}\" y=\"{F(MarginTop - 4)}\" width=\"{F(PlotW + 8)}\" height=\"{F(PlotH + 8)}\"/></clipPath></defs>");

        // Grid + tick labels. Presentation attributes are a fallback (e.g. PDF); CSS classes win on the
        // site, where text is drawn larger in the small inline figure and normal-sized in the lightbox.
        // Every step gets a tick mark and grid line; labels are thinned (every 2nd, 5th… multiple of
        // the step, so 0 always keeps its label) only when they would otherwise overlap.
        var xEvery = LabelEvery(Ticks(xMin, xMax, xStep).Select(v => FormatTick(v, xStep).Length).DefaultIfEmpty(1).Max() * 12 + 8, PlotW * xStep / (xMax - xMin));
        var yEvery = LabelEvery(24, PlotH * yStep / (yMax - yMin));
        foreach (var x in Ticks(xMin, xMax, xStep))
        {
            var sx = Sx(x);
            if (Math.Abs(sx - axisX) > 0.5)
                svg.Append($"<line class=\"pg-grid\" x1=\"{F(sx)}\" y1=\"{F(MarginTop)}\" x2=\"{F(sx)}\" y2=\"{F(plotBottom)}\" stroke=\"#ddd\"/>");
            svg.Append($"<line class=\"pg-axis\" x1=\"{F(sx)}\" y1=\"{F(axisY)}\" x2=\"{F(sx)}\" y2=\"{F(axisY + 5)}\" stroke=\"#444\"/>");
            if (interiorOrigin && x == 0 || Math.Round(x / xStep) % xEvery != 0) continue;
            if (xAxisAtTop)
            {
                // The label at the y axis would sit on the axis arrow - nudge it to the right.
                var onYAxis = Math.Abs(sx - axisX) <= 0.5;
                svg.Append($"<text class=\"pg-tick\" x=\"{F(onYAxis ? sx + 8 : sx)}\" y=\"{F(axisY - 10)}\" text-anchor=\"{(onYAxis ? "start" : "middle")}\" font-size=\"14\">{FormatTick(x, xStep)}</text>");
                continue;
            }
            svg.Append($"<text class=\"pg-tick\" x=\"{F(sx)}\" y=\"{F(axisY + 26)}\" text-anchor=\"middle\" font-size=\"14\">{FormatTick(x, xStep)}</text>");
        }
        foreach (var y in Ticks(yMin, yMax, yStep))
        {
            var sy = Sy(y);
            if (Math.Abs(sy - axisY) > 0.5)
                svg.Append($"<line class=\"pg-grid\" x1=\"{F(MarginLeft)}\" y1=\"{F(sy)}\" x2=\"{F(plotRight)}\" y2=\"{F(sy)}\" stroke=\"#ddd\"/>");
            svg.Append($"<line class=\"pg-axis\" x1=\"{F(axisX - 5)}\" y1=\"{F(sy)}\" x2=\"{F(axisX)}\" y2=\"{F(sy)}\" stroke=\"#444\"/>");
            if (interiorOrigin && y == 0 || Math.Round(y / yStep) % yEvery != 0) continue;
            // Right under the crossing the "0" of the origin already sits there.
            if (interiorOrigin && sy > axisY && sy - axisY < 44) continue;
            svg.Append($"<text class=\"pg-tick\" x=\"{F(axisX - 10)}\" y=\"{F(sy)}\" dy=\"0.35em\" text-anchor=\"end\" font-size=\"14\">{FormatTick(y, yStep)}</text>");
        }
        if (interiorOrigin)
            svg.Append($"<text class=\"pg-tick\" x=\"{F(axisX - 10)}\" y=\"{F(axisY + 26)}\" text-anchor=\"end\" font-size=\"14\">0</text>");

        // Axes with arrow heads, drawn slightly past the plot area.
        svg.Append($"<line class=\"pg-axis\" x1=\"{F(MarginLeft)}\" y1=\"{F(axisY)}\" x2=\"{F(plotRight + 14)}\" y2=\"{F(axisY)}\" stroke=\"#444\" stroke-width=\"1.5\"/>");
        svg.Append($"<polygon class=\"pg-arrow\" points=\"{F(plotRight + 20)},{F(axisY)} {F(plotRight + 11)},{F(axisY - 4)} {F(plotRight + 11)},{F(axisY + 4)}\" fill=\"#444\"/>");
        svg.Append($"<line class=\"pg-axis\" x1=\"{F(axisX)}\" y1=\"{F(plotBottom)}\" x2=\"{F(axisX)}\" y2=\"{F(MarginTop - 14)}\" stroke=\"#444\" stroke-width=\"1.5\"/>");
        svg.Append($"<polygon class=\"pg-arrow\" points=\"{F(axisX)},{F(MarginTop - 20)} {F(axisX - 4)},{F(MarginTop - 11)} {F(axisX + 4)},{F(MarginTop - 11)}\" fill=\"#444\"/>");

        // Axis labels: along the edge when the axis is on the plot border, otherwise next to its arrow.
        if (g.X.Label.Length > 0)
            svg.Append(xAxisAtEdge || xAxisAtTop
                ? $"<text class=\"pg-label\" x=\"{F(MarginLeft + PlotW / 2)}\" y=\"{F(Height - 8)}\" text-anchor=\"middle\" font-size=\"15\">{Enc(g.X.Label)}</text>"
                : $"<text class=\"pg-label\" x=\"{F(plotRight + 20)}\" y=\"{F(axisY - 12)}\" text-anchor=\"end\" font-size=\"15\">{Enc(g.X.Label)}</text>");
        if (g.Y.Label.Length > 0)
            svg.Append(yAxisAtEdge
                ? $"<text class=\"pg-label\" transform=\"translate(22 {F(MarginTop + PlotH / 2)}) rotate(-90)\" text-anchor=\"middle\" font-size=\"15\">{Enc(g.Y.Label)}</text>"
                : axisX >= plotRight - 0.5
                    // y axis on the right edge (only negative x): keep the label inside the image
                    ? $"<text class=\"pg-label\" x=\"{F(axisX - 12)}\" y=\"{F(MarginTop - 6)}\" text-anchor=\"end\" font-size=\"15\">{Enc(g.Y.Label)}</text>"
                    : $"<text class=\"pg-label\" x=\"{F(axisX + 12)}\" y=\"{F(MarginTop - 6)}\" text-anchor=\"start\" font-size=\"15\">{Enc(g.Y.Label)}</text>");

        svg.Append($"<g clip-path=\"url(#{id}-clip)\">");
        foreach (var s in g.Series)
        {
            var color = Enc(s.Color);
            if (s.Points.Count == 1)
            {
                svg.Append($"<circle class=\"pg-point\" cx=\"{F(Sx(s.Points[0].X))}\" cy=\"{F(Sy(s.Points[0].Y))}\" r=\"5\" fill=\"{color}\" stroke=\"#fff\" stroke-width=\"1.5\"/>");
                continue;
            }
            var dash = s.Dashed ? " stroke-dasharray=\"8 6\"" : "";
            svg.Append($"<path d=\"{BuildPath(s, Sx, Sy)}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{dash}/>");
        }
        for (var i = 0; i < g.Functions.Count; i++)
        {
            var path = BuildFunctionPath(samples[i], Sx, Sy, MarginTop, plotBottom);
            if (path.Length == 0) continue;
            var dash = g.Functions[i].Dashed ? " stroke-dasharray=\"8 6\"" : "";
            svg.Append($"<path d=\"{path}\" fill=\"none\" stroke=\"{Enc(g.Functions[i].Color)}\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{dash}/>");
        }
        // Each authored point of a curve is a visible dot on top of the line ("nodots" turns it off).
        foreach (var s in g.Series.Where(ser => ser.ShowDots && ser.Points.Count > 1))
        {
            foreach (var (x, y) in s.Points)
                svg.Append($"<circle class=\"pg-point\" cx=\"{F(Sx(x))}\" cy=\"{F(Sy(y))}\" r=\"5\" fill=\"{Enc(s.Color)}\" stroke=\"#fff\" stroke-width=\"1.5\"/>");
        }
        foreach (var p in g.Markers)
        {
            svg.Append($"<circle class=\"pg-point\" cx=\"{F(Sx(p.X))}\" cy=\"{F(Sy(p.Y))}\" r=\"6\" fill=\"{Enc(p.Color)}\" stroke=\"#fff\" stroke-width=\"2\"/>");
            if (!string.IsNullOrEmpty(p.Name))
                svg.Append($"<text class=\"pg-point-label\" x=\"{F(Sx(p.X) + 9)}\" y=\"{F(Sy(p.Y) - 9)}\" font-size=\"13\" font-weight=\"600\">{Enc(p.Name)}</text>");
        }
        svg.Append("</g></svg>");

        var html = new StringBuilder();
        html.Append("<figure class=\"post-graph\">");
        if (g.Title != null)
            html.Append($"<div class=\"post-graph-title\">{Enc(g.Title)}</div>");
        html.Append(svg);

        var legend = g.Series.Where(s => !string.IsNullOrEmpty(s.Name)).Select(s => (s.Name!, s.Color, IsPoint: false))
            .Concat(g.Functions.Select(f => (f.Name!, f.Color, IsPoint: false)))
            .Concat(g.Markers.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => (p.Name!, p.Color, IsPoint: true)))
            .ToList();
        if (legend.Count > 0)
        {
            html.Append("<div class=\"post-graph-legend\">");
            foreach (var (name, color, isPoint) in legend)
            {
                var swatchClass = isPoint ? "post-graph-swatch post-graph-swatch--point" : "post-graph-swatch";
                html.Append($"<span class=\"post-graph-legend-item\"><span class=\"{swatchClass}\" style=\"background:{Enc(color)}\"></span>{Enc(name)}</span>");
            }
            html.Append("</div>");
        }

        foreach (var error in g.Errors)
            html.Append($"<div class=\"post-graph-warning\">{Enc(error)}</div>");

        if (g.Notes.Count > 0)
        {
            html.Append("<figcaption>");
            foreach (var note in g.Notes)
                html.Append($"<span class=\"post-graph-note\">{Enc(note)}</span>");
            html.Append("</figcaption>");
        }
        html.Append("</figure>");
        return html.ToString();
    }

    private const int FunctionSamples = 600;

    // Evaluates f across its domain clipped to the visible x range; NaN marks gaps (outside
    // the function's domain, e.g. sqrt of a negative, or 1/0).
    private static List<(double X, double Y)> SampleFunction(FunctionPlot f, double xMin, double xMax)
    {
        var lo = Math.Max(xMin, f.From ?? xMin);
        var hi = Math.Min(xMax, f.To ?? xMax);
        var result = new List<(double X, double Y)>();
        if (hi <= lo) return result;

        for (var i = 0; i <= FunctionSamples; i++)
        {
            var x = lo + (hi - lo) * i / FunctionSamples;
            double y;
            try { y = f.F(x); } catch { y = double.NaN; }
            result.Add((x, double.IsFinite(y) ? y : double.NaN));
        }
        return result;
    }

    // y extent of the sampled functions for the automatic y range, ignoring the outer 2 % on each
    // side so an asymptote (1/x, tan x) doesn't squash the rest of the graph into a flat line.
    private static IEnumerable<double> FunctionYExtent(List<List<(double X, double Y)>> samples)
    {
        var ys = samples.SelectMany(s => s.Select(p => p.Y)).Where(double.IsFinite).OrderBy(y => y).ToList();
        if (ys.Count == 0) return [];
        var lo = ys[(int)Math.Floor((ys.Count - 1) * 0.02)];
        var hi = ys[(int)Math.Ceiling((ys.Count - 1) * 0.98)];
        return [lo, hi];
    }

    private static string BuildFunctionPath(List<(double X, double Y)> samples, Func<double, double> sx, Func<double, double> sy,
        double top, double bottom)
    {
        // Keep off-screen points finite and not astronomically far away; the clip path hides them.
        var limitTop = top - 2 * (bottom - top);
        var limitBottom = bottom + 2 * (bottom - top);

        var sb = new StringBuilder();
        double? prevY = null;
        foreach (var (x, y) in samples)
        {
            if (double.IsNaN(y)) { prevY = null; continue; }
            var py = Math.Clamp(sy(y), limitTop, limitBottom);
            // Jumping from far above to far below the plot (or back) between neighbouring samples
            // is an asymptote, not a steep line - lift the pen instead of drawing a vertical bar.
            var jump = prevY is { } p && (p < top && py > bottom || p > bottom && py < top);
            sb.Append(prevY == null || jump ? " M" : " L").Append(F(sx(x))).Append(',').Append(F(py));
            prevY = py;
        }
        return sb.ToString().Trim();
    }

    private static (double Min, double Max) ResolveRange(Axis axis, List<double> data)
    {
        double min, max;
        if (axis.Min.HasValue && axis.Max.HasValue)
        {
            (min, max) = (axis.Min.Value, axis.Max.Value);
        }
        else if (data.Count > 0)
        {
            // No explicit range: fit the data, anchored at 0 when all values share a sign.
            min = Math.Min(0, data.Min());
            max = Math.Max(0, data.Max());
            var step = NiceStep((max - min) / 8);
            min = Math.Floor(min / step) * step;
            max = Math.Ceiling(max / step) * step;
        }
        else
        {
            (min, max) = (0, 10);
        }

        if (max - min < 1e-9) { min -= 1; max += 1; }
        return (min, max);
    }

    private static double ResolveStep(double? step, double min, double max)
    {
        var span = max - min;
        // Ignore steps that would draw an unreadable forest of ticks.
        if (step is > 0 && span / step.Value <= 40) return step.Value;
        // Short axes count 1, 2, 3…; longer ones get a round step (2, 5, 10, 20…).
        if (span >= 2 && span <= 20) return 1;
        return NiceStep(span / 8);
    }

    // How many ticks apart labels must be so that a label of the given size fits between them.
    private static int LabelEvery(double labelSize, double tickSpacing) =>
        tickSpacing <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(labelSize / tickSpacing));

    private static (double Min, double Max) SnapToStep(double min, double max, double step)
    {
        var snappedMin = Math.Floor(min / step + 1e-9) * step;
        var snappedMax = Math.Ceiling(max / step - 1e-9) * step;
        return snappedMax - snappedMin > 1e-9 ? (snappedMin, snappedMax) : (min, max);
    }

    private static double NiceStep(double raw)
    {
        if (raw <= 0 || double.IsNaN(raw) || double.IsInfinity(raw)) return 1;
        var exp = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var f = raw / exp;
        var nice = f < 1.5 ? 1 : f < 3 ? 2 : f < 7 ? 5 : 10;
        return nice * exp;
    }

    private static IEnumerable<double> Ticks(double min, double max, double step)
    {
        var start = Math.Ceiling(min / step - 1e-9) * step;
        for (var i = 0; i <= 100; i++)
        {
            var v = start + i * step;
            if (v > max + step * 1e-9) yield break;
            yield return Math.Abs(v) < step * 1e-9 ? 0 : v;
        }
    }

    // Czech number formatting (decimal comma), with as many decimals as the step needs.
    private static string FormatTick(double v, double step)
    {
        var decimals = 0;
        while (decimals < 6 && Math.Abs(step * Math.Pow(10, decimals) - Math.Round(step * Math.Pow(10, decimals))) > 1e-6)
            decimals++;
        return Math.Round(v, decimals).ToString("0." + new string('#', Math.Max(decimals, 1)), CultureInfo.InvariantCulture)
            .Replace('.', ',').Replace("-", "−");
    }

    private static string BuildPath(Series s, Func<double, double> sx, Func<double, double> sy)
    {
        var pts = s.Points;
        var sb = new StringBuilder();
        sb.Append($"M{F(sx(pts[0].X))},{F(sy(pts[0].Y))}");

        if (!s.Smooth || pts.Count == 2)
        {
            for (var i = 1; i < pts.Count; i++)
                sb.Append($" L{F(sx(pts[i].X))},{F(sy(pts[i].Y))}");
            return sb.ToString();
        }

        var increasingX = true;
        for (var i = 1; i < pts.Count; i++)
            if (pts[i].X <= pts[i - 1].X) { increasingX = false; break; }

        if (increasingX)
        {
            // Monotone cubic (Fritsch–Carlson): smooth, but never overshoots, so flat
            // segments stay flat and a non-negative speed never dips below zero.
            var n = pts.Count;
            var d = new double[n - 1];
            for (var k = 0; k < n - 1; k++)
                d[k] = (pts[k + 1].Y - pts[k].Y) / (pts[k + 1].X - pts[k].X);

            var m = new double[n];
            m[0] = d[0];
            m[n - 1] = d[n - 2];
            for (var k = 1; k < n - 1; k++)
                m[k] = d[k - 1] * d[k] <= 0 ? 0 : (d[k - 1] + d[k]) / 2;

            for (var k = 0; k < n - 1; k++)
            {
                if (d[k] == 0) { m[k] = 0; m[k + 1] = 0; continue; }
                var a = m[k] / d[k];
                var b = m[k + 1] / d[k];
                var h = a * a + b * b;
                if (h > 9)
                {
                    var t = 3 / Math.Sqrt(h);
                    m[k] = t * a * d[k];
                    m[k + 1] = t * b * d[k];
                }
            }

            for (var k = 0; k < n - 1; k++)
            {
                var dx = (pts[k + 1].X - pts[k].X) / 3;
                sb.Append($" C{F(sx(pts[k].X + dx))},{F(sy(pts[k].Y + m[k] * dx))}"
                    + $" {F(sx(pts[k + 1].X - dx))},{F(sy(pts[k + 1].Y - m[k + 1] * dx))}"
                    + $" {F(sx(pts[k + 1].X))},{F(sy(pts[k + 1].Y))}");
            }
            return sb.ToString();
        }

        // x not strictly increasing (loops, vertical parts): Catmull-Rom through all points.
        var screen = pts.Select(p => (X: sx(p.X), Y: sy(p.Y))).ToList();
        for (var i = 0; i < screen.Count - 1; i++)
        {
            var p0 = screen[Math.Max(i - 1, 0)];
            var p1 = screen[i];
            var p2 = screen[i + 1];
            var p3 = screen[Math.Min(i + 2, screen.Count - 1)];
            sb.Append($" C{F(p1.X + (p2.X - p0.X) / 6)},{F(p1.Y + (p2.Y - p0.Y) / 6)}"
                + $" {F(p2.X - (p3.X - p1.X) / 6)},{F(p2.Y - (p3.Y - p1.Y) / 6)}"
                + $" {F(p2.X)},{F(p2.Y)}");
        }
        return sb.ToString();
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Enc(string s) => WebUtility.HtmlEncode(s);

    // Deterministic across processes (string.GetHashCode is randomised), keeps clipPath ids stable.
    private static string StableHash(string s)
    {
        uint h = 2166136261;
        foreach (var c in s) h = (h ^ c) * 16777619;
        return h.ToString("x8", CultureInfo.InvariantCulture);
    }
}
