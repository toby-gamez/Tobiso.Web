using System.Text;
using System.Text.RegularExpressions;

namespace Tobiso.Web.App.Admin.Components.Shared;

// Form state of the Venn builder (VennBuilder.razor) and its conversion to the
// [vennN] ... [/vennN] text block that Tobiso.Web.Shared.Graphs.VennRenderer draws.

public sealed class VennSetModel
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    /// <summary>One element per line (or separated by ";").</summary>
    public string Elements { get; set; } = "";
}

/// <summary>One term the author displays under the diagram (written as a "show:" line).</summary>
public sealed class VennShowModel
{
    /// <summary>prunik | sjednoceni | rozdil | doplnek</summary>
    public string Kind { get; set; } = "prunik";
    // Průnik / sjednocení: which sets take part.
    public bool InA { get; set; } = true;
    public bool InB { get; set; } = true;
    public bool InC { get; set; }
    // Rozdíl: Left \ Right. Doplněk: Left'.
    public int Left { get; set; }
    public int Right { get; set; } = 1;
}

public sealed class VennBuilderModel
{
    public string BlockName { get; set; } = "venn1";
    public string Title { get; set; } = "";
    public List<VennSetModel> Sets { get; set; } = [new() { Name = "A" }, new() { Name = "B" }];
    public string Outside { get; set; } = "";
    public bool Universe { get; set; }
    /// <summary>Terms displayed under the diagram; the first starts highlighted and readers can toggle it.</summary>
    public List<VennShowModel> Show { get; set; } = [];
    /// <summary>Playground: readers may try every term of the ticked categories.</summary>
    public bool Playground { get; set; }
    public bool PlayPrunik { get; set; } = true;
    public bool PlaySjednoceni { get; set; } = true;
    public bool PlayRozdil { get; set; } = true;
    public bool PlayDoplnek { get; set; } = true;

    public string SetToken(int i)
    {
        var name = i < Sets.Count ? GraphBuilderModel.ArgText(Sets[i].Name).Replace(';', ' ') : "";
        return name.Length > 0 ? name : ((char)('A' + i)).ToString();
    }

    private string? ShowLine(VennShowModel t)
    {
        var n = Sets.Count;
        switch (t.Kind)
        {
            case "prunik" or "sjednoceni":
            {
                var members = new[] { t.InA, t.InB, t.InC }.Take(n).Select((on, i) => (on, i)).Where(x => x.on).Select(x => SetToken(x.i)).ToList();
                return members.Count >= 2 ? $"show: {t.Kind}; {string.Join("; ", members)}" : null;
            }
            case "rozdil":
                return t.Left < n && t.Right < n && t.Left != t.Right ? $"show: rozdil; {SetToken(t.Left)}; {SetToken(t.Right)}" : null;
            case "doplnek":
                return t.Left < n ? $"show: doplnek; {SetToken(t.Left)}" : null;
            default:
                return null;
        }
    }
    public List<GraphNoteRow> Notes { get; set; } = [];

    public static List<string> SplitElements(string? text) =>
        Regex.Split(text ?? "", @"[;\r\n]+").Select(GraphBuilderModel.OneLine).Where(s => s.Length > 0).ToList();

    // A lone element containing a comma needs a trailing ";" so the renderer splits on ";" not ",".
    private static string JoinElements(List<string> items) =>
        string.Join("; ", items) + (items.Count == 1 && items[0].Contains(',') ? ";" : "");

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(BlockName).Append("]\n");

        if (!string.IsNullOrWhiteSpace(Title)) sb.Append("title: ").Append(GraphBuilderModel.OneLine(Title)).Append('\n');

        for (var i = 0; i < Sets.Count; i++)
        {
            var set = Sets[i];
            var name = GraphBuilderModel.ArgText(set.Name);
            if (name.Length == 0) name = ((char)('A' + i)).ToString();
            sb.Append("set").Append(GraphBuilderModel.Args(("name", name), ("color", set.Color)))
              .Append(": ").Append(JoinElements(SplitElements(set.Elements))).Append('\n');
        }

        var outside = SplitElements(Outside);
        if (outside.Count > 0) sb.Append("outside: ").Append(JoinElements(outside)).Append('\n');
        if (Universe) sb.Append("universe: ano\n");
        foreach (var line in Show.Select(ShowLine).OfType<string>()) sb.Append(line).Append('\n');
        if (Playground)
        {
            var cats = new List<string>();
            if (PlayPrunik) cats.Add("prunik");
            if (PlaySjednoceni) cats.Add("sjednoceni");
            if (PlayRozdil) cats.Add("rozdil");
            if (PlayDoplnek) cats.Add("doplnek");
            if (cats.Count == 4) sb.Append("playground: ano\n");
            else if (cats.Count > 0) sb.Append("playground: ").Append(string.Join(", ", cats)).Append('\n');
        }

        foreach (var note in Notes.Select(n => GraphBuilderModel.OneLine(n.Text)).Where(n => n.Length > 0))
            sb.Append("note: ").Append(note).Append('\n');

        sb.Append("[/").Append(BlockName).Append(']');
        return sb.ToString();
    }

    public static VennBuilderModel Template(string kind) => kind switch
    {
        "divisors" => new VennBuilderModel
        {
            Title = "Dělitelé čísel 12 a 18",
            Show = [new VennShowModel { Kind = "prunik" }],
            Sets =
            [
                new() { Name = "D12", Color = "BLUE", Elements = "1\n2\n3\n4\n6\n12" },
                new() { Name = "D18", Color = "ORANGE", Elements = "1\n2\n3\n6\n9\n18" },
            ],
            Notes = [new GraphNoteRow { Text = "Průnik tvoří společní dělitelé." }],
        },
        "three" => new VennBuilderModel
        {
            Title = "Kroužky",
            Sets =
            [
                new() { Name = "Hudba", Color = "RED", Elements = "Anna\nBob\nCyril\nDana" },
                new() { Name = "Sport", Color = "GREEN", Elements = "Bob\nDana\nEva\nFilip" },
                new() { Name = "Divadlo", Color = "PURPLE", Elements = "Cyril\nDana\nEva\nGita" },
            ],
        },
        _ => new VennBuilderModel
        {
            Sets = [new() { Name = "A", Color = "BLUE" }, new() { Name = "B", Color = "ORANGE" }],
        },
    };
}
