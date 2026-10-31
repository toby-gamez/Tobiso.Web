using System.Text;

namespace Tobiso.Web.App.Admin.Components.Shared;

// Form state of the flowchart builder (FlowchartBuilder.razor) and its conversion to the
// [vyvojN] ... [/vyvojN] text block that Tobiso.Web.Shared.Graphs.FlowchartRenderer draws.

public sealed class FlowNodeModel
{
    public string Id { get; set; } = "";
    /// <summary>start | end | process | decision | io</summary>
    public string Kind { get; set; } = "process";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "";
}

public sealed class FlowEdgeModel
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "";
}

public sealed class FlowchartBuilderModel
{
    public static readonly (string Value, string Label)[] Kinds =
    [
        ("start", "Začátek"), ("process", "Krok"), ("decision", "Podmínka"), ("io", "Vstup / výstup"), ("end", "Konec"),
    ];

    public string BlockName { get; set; } = "vyvoj1";
    public string Title { get; set; } = "";
    public bool Right { get; set; }
    public List<FlowNodeModel> Nodes { get; set; } = [];
    public List<FlowEdgeModel> Edges { get; set; } = [];
    public List<GraphNoteRow> Notes { get; set; } = [];

    /// <summary>Next unused short id: a, b, c … z, then n27, n28 …</summary>
    public string NewId()
    {
        for (var c = 'a'; c <= 'z'; c++)
            if (Nodes.All(n => n.Id != c.ToString())) return c.ToString();
        var i = 27;
        while (Nodes.Any(n => n.Id == $"n{i}")) i++;
        return $"n{i}";
    }

    public FlowNodeModel AddNode(string kind, string label = "")
    {
        var node = new FlowNodeModel { Id = NewId(), Kind = kind, Label = label };
        Nodes.Add(node);
        return node;
    }

    public void RemoveNode(FlowNodeModel node)
    {
        Nodes.Remove(node);
        Edges.RemoveAll(e => e.From == node.Id || e.To == node.Id);
    }

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(BlockName).Append("]\n");

        if (!string.IsNullOrWhiteSpace(Title)) sb.Append("title: ").Append(GraphBuilderModel.OneLine(Title)).Append('\n');
        if (Right) sb.Append("direction: right\n");

        var emitted = new HashSet<string>();
        foreach (var node in Nodes)
        {
            var label = GraphBuilderModel.OneLine(node.Label);
            if (label.Length == 0) continue;
            emitted.Add(node.Id);
            sb.Append(node.Kind)
              .Append(GraphBuilderModel.Args(("id", node.Id), ("color", node.Color)))
              .Append(": ").Append(label).Append('\n');
        }

        foreach (var edge in Edges)
        {
            if (!emitted.Contains(edge.From) || !emitted.Contains(edge.To)) continue;
            sb.Append(edge.From).Append(" -> ").Append(edge.To);
            var label = GraphBuilderModel.ArgText(edge.Label);
            if (label.Length > 0) sb.Append(" (").Append(label).Append(')');
            if (!string.IsNullOrWhiteSpace(edge.Color)) sb.Append(' ').Append(edge.Color.Trim());
            sb.Append('\n');
        }

        foreach (var note in Notes.Select(n => GraphBuilderModel.OneLine(n.Text)).Where(n => n.Length > 0))
            sb.Append("note: ").Append(note).Append('\n');

        sb.Append("[/").Append(BlockName).Append(']');
        return sb.ToString();
    }

    public static FlowchartBuilderModel Template(string kind)
    {
        var m = new FlowchartBuilderModel();
        switch (kind)
        {
            case "branch":
            {
                m.Title = "Je číslo sudé?";
                var s = m.AddNode("start", "Začátek");
                var i = m.AddNode("io", "Načti číslo n");
                var d = m.AddNode("decision", "Zbytek po dělení 2 je 0?");
                var a = m.AddNode("process", "Číslo je sudé");
                var b = m.AddNode("process", "Číslo je liché");
                var e = m.AddNode("end", "Konec");
                m.Edges =
                [
                    new() { From = s.Id, To = i.Id }, new() { From = i.Id, To = d.Id },
                    new() { From = d.Id, To = a.Id, Label = "ano", Color = "GREEN" },
                    new() { From = d.Id, To = b.Id, Label = "ne", Color = "RED" },
                    new() { From = a.Id, To = e.Id }, new() { From = b.Id, To = e.Id },
                ];
                break;
            }
            case "loop":
            {
                m.Title = "Odpočítávání";
                var s = m.AddNode("start", "Začátek");
                var i = m.AddNode("process", "n = 10");
                var d = m.AddNode("decision", "n > 0?");
                var p = m.AddNode("io", "Vypiš n");
                var dec = m.AddNode("process", "n = n − 1");
                var e = m.AddNode("end", "Konec");
                m.Edges =
                [
                    new() { From = s.Id, To = i.Id }, new() { From = i.Id, To = d.Id },
                    new() { From = d.Id, To = p.Id, Label = "ano" },
                    new() { From = p.Id, To = dec.Id }, new() { From = dec.Id, To = d.Id },
                    new() { From = d.Id, To = e.Id, Label = "ne" },
                ];
                break;
            }
            default:
            {
                var s = m.AddNode("start", "Začátek");
                var p = m.AddNode("process", "");
                var e = m.AddNode("end", "Konec");
                m.Edges = [new() { From = s.Id, To = p.Id }, new() { From = p.Id, To = e.Id }];
                break;
            }
        }
        return m;
    }
}
