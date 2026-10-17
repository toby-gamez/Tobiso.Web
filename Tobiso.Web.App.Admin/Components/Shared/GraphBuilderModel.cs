using System.Text;
using System.Text.RegularExpressions;

namespace Tobiso.Web.App.Admin.Components.Shared;

// Form state of the graph builder (GraphBuilder.razor) and its conversion to the
// [grafN] ... [/grafN] text block that Tobiso.Web.Shared.Graphs.GraphRenderer draws.

public enum GraphItemKind { Curve, Point, Function }

public sealed class GraphPointRow
{
    public string X { get; set; } = "";
    public string Y { get; set; } = "";
}

public sealed class GraphAxisModel
{
    public string Label { get; set; } = "";
    public string Min { get; set; } = "";
    public string Max { get; set; } = "";
    public string Step { get; set; } = "";
}

public sealed class GraphItemModel
{
    public GraphItemKind Kind { get; set; } = GraphItemKind.Curve;
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public List<GraphPointRow> Points { get; set; } = [new(), new()];
    public bool Smooth { get; set; }
    public bool Dashed { get; set; }
    public bool NoDots { get; set; }
    public string Formula { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
}

public sealed class GraphNoteRow
{
    public string Text { get; set; } = "";
}

public sealed class GraphBuilderModel
{
    private static readonly Regex NumberRegex = new(@"^-?\d+(?:[.,]\d+)?$");

    public string BlockName { get; set; } = "graf1";
    public string Title { get; set; } = "";
    public GraphAxisModel X { get; set; } = new();
    public GraphAxisModel Y { get; set; } = new();
    public List<GraphItemModel> Items { get; set; } = [];
    public List<GraphNoteRow> Notes { get; set; } = [];

    public static bool IsNumber(string? s) => NumberRegex.IsMatch((s ?? "").Trim());
    public static bool IsNumberOrEmpty(string? s) => string.IsNullOrWhiteSpace(s) || IsNumber(s);

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(BlockName).Append("]\n");

        if (!string.IsNullOrWhiteSpace(Title)) sb.Append("title: ").Append(OneLine(Title)).Append('\n');
        AppendAxis(sb, "x", X);
        AppendAxis(sb, "y", Y);

        foreach (var item in Items)
        {
            switch (item.Kind)
            {
                case GraphItemKind.Curve:
                {
                    var points = item.Points.Where(p => IsNumber(p.X) && IsNumber(p.Y)).Select(FormatPoint).ToList();
                    if (points.Count == 0) break;
                    var options = new List<string>();
                    if (item.Smooth) options.Add("smooth");
                    if (item.Dashed) options.Add("dashed");
                    if (item.NoDots) options.Add("nodots");
                    if (!string.IsNullOrWhiteSpace(item.Color)) options.Add(item.Color.Trim());
                    sb.Append("curve").Append(Args(("name", item.Name))).Append(": ")
                      .Append(string.Join(" ", points.Concat(options))).Append('\n');
                    break;
                }
                case GraphItemKind.Point:
                {
                    var point = item.Points.FirstOrDefault(p => IsNumber(p.X) && IsNumber(p.Y));
                    if (point == null) break;
                    sb.Append("point").Append(Args(("name", item.Name))).Append(": ").Append(FormatPoint(point));
                    if (!string.IsNullOrWhiteSpace(item.Color)) sb.Append(' ').Append(item.Color.Trim());
                    sb.Append('\n');
                    break;
                }
                case GraphItemKind.Function:
                {
                    var formula = OneLine(item.Formula);
                    if (formula.Length == 0) break;
                    if (!formula.Contains('=')) formula = "y = " + formula;
                    sb.Append("function")
                      .Append(Args(("name", item.Name),
                                   ("od", IsNumber(item.From) ? Num(item.From) : ""),
                                   ("do", IsNumber(item.To) ? Num(item.To) : "")))
                      .Append(": ").Append(formula);
                    if (!string.IsNullOrWhiteSpace(item.Color)) sb.Append(' ').Append(item.Color.Trim());
                    if (item.Dashed) sb.Append(" dashed");
                    sb.Append('\n');
                    break;
                }
            }
        }

        foreach (var note in Notes.Select(n => OneLine(n.Text)).Where(n => n.Length > 0))
            sb.Append("note: ").Append(note).Append('\n');

        sb.Append("[/").Append(BlockName).Append(']');
        return sb.ToString();
    }

    private static void AppendAxis(StringBuilder sb, string axis, GraphAxisModel a)
    {
        var label = ArgText(a.Label);
        var hasRange = IsNumber(a.Min) && IsNumber(a.Max);
        if (label.Length == 0 && !hasRange) return;

        sb.Append(axis);
        if (label.Length > 0) sb.Append('(').Append(label).Append(')');
        sb.Append(':');
        if (hasRange)
        {
            // ".." keeps negative numbers unambiguous (-20..-30 rather than -20--30).
            sb.Append(' ').Append(Num(a.Min)).Append("..").Append(Num(a.Max));
            if (IsNumber(a.Step) && !a.Step.Trim().StartsWith('-')) sb.Append(" (").Append(Num(a.Step)).Append(')');
        }
        sb.Append('\n');
    }

    private static string Args(params (string Key, string Value)[] args)
    {
        var parts = args.Select(a => (a.Key, Value: ArgText(a.Value))).Where(a => a.Value.Length > 0)
            .Select(a => $"{a.Key}: {a.Value}").ToList();
        return parts.Count == 0 ? "" : $"({string.Join(", ", parts)})";
    }

    // Text inside "( … )": a ")" would end the argument list early and ", " would start a new
    // argument, so both are neutralised; "(" becomes "[" to keep e.g. "v [m/s]" readable.
    private static string ArgText(string? s) =>
        OneLine(s).Replace('(', '[').Replace(')', ']').Replace(", ", " ");

    private static string OneLine(string? s) => Regex.Replace(s ?? "", @"\s+", " ").Trim();

    private static string Num(string s) => s.Trim().Replace('.', ',');

    private static string FormatPoint(GraphPointRow p) => $"[{Num(p.X)};{Num(p.Y)}]";

    public static GraphBuilderModel Template(string kind) => kind switch
    {
        "measure" => new GraphBuilderModel
        {
            Title = "Pohyb robotů",
            X = new GraphAxisModel { Label = "t [s]", Min = "0", Max = "10" },
            Y = new GraphAxisModel { Label = "v [m/s]", Min = "0", Max = "50", Step = "10" },
            Items =
            [
                new GraphItemModel { Name = "robot1", Color = "RED", Points = [P("0", "0"), P("6", "7"), P("8", "7")] },
                new GraphItemModel { Name = "robot2", Color = "BLUE", Smooth = true, Dashed = true, Points = [P("0", "5"), P("4", "30"), P("10", "45")] },
                new GraphItemModel { Kind = GraphItemKind.Point, Name = "A", Color = "LIGHT_BLUE", Points = [P("5", "8")] },
            ],
            Notes = [new GraphNoteRow { Text = "Červená křivka zobrazuje rychlost robota 1" }],
        },
        "function" => new GraphBuilderModel
        {
            Title = "Lineární a kvadratická funkce",
            X = new GraphAxisModel { Min = "-5", Max = "5" },
            Y = new GraphAxisModel { Min = "-5", Max = "10" },
            Items =
            [
                new GraphItemModel { Kind = GraphItemKind.Function, Formula = "2x + 1", Color = "BLUE" },
                new GraphItemModel { Kind = GraphItemKind.Function, Name = "parabola", Formula = "x^2 - 3", Color = "RED" },
            ],
        },
        _ => new GraphBuilderModel
        {
            X = new GraphAxisModel { Min = "0", Max = "10" },
            Y = new GraphAxisModel { Min = "0", Max = "10" },
            Items = [new GraphItemModel { Points = [P("0", "0"), P("5", "5")] }],
        },
    };

    private static GraphPointRow P(string x, string y) => new() { X = x, Y = y };
}
