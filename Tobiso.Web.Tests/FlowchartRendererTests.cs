using Tobiso.Web.Shared.Graphs;

namespace Tobiso.Web.Tests;

public class FlowchartRendererTests
{
    private const string Simple = """
        [vyvoj1]
        title: Sudé číslo
        start(id: s): Začátek
        decision(id: d): Je číslo sudé?
        process(id: a): Vypiš sudé
        process(id: b): Vypiš liché
        end(id: e): Konec
        s -> d
        d -> a (ano)
        d -> b (ne) RED
        a -> e
        b -> e
        [/vyvoj1]
        """;

    [Fact]
    public void Renders_AllNodeShapesAndEdges()
    {
        var html = FlowchartRenderer.RenderPreview(Simple);
        Assert.Contains("post-flowchart", html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("Sudé číslo"), html);
        Assert.Contains("<polygon class=\"fc-node fc-node--decision\"", html);
        Assert.Equal(2, CountOf(html, "fc-node--terminal"));
        Assert.Equal(5, CountOf(html, "class=\"fc-edge\""));
        Assert.Contains(">ano<", html);
        Assert.DoesNotContain("post-graph-warning", html);
    }

    [Fact]
    public void ExtractAndRestore_RoundTrips_AndKeepsArrowsOutOfText()
    {
        var rendered = new List<string>();
        var text = FlowchartRenderer.ExtractBlocks($"before\n\n{Simple}\n\nafter", rendered);
        Assert.Single(rendered);
        Assert.DoesNotContain("->", text);
        Assert.Contains("TOBISOVYVOJPLACEHOLDER0X", text);

        var restored = FlowchartRenderer.RestoreBlocks("<p>TOBISOVYVOJPLACEHOLDER0X</p>", rendered);
        Assert.StartsWith("<figure class=\"post-flowchart\">", restored);
    }

    [Fact]
    public void Loop_DoesNotHang_AndIsDrawn()
    {
        var html = FlowchartRenderer.RenderPreview("""
            [vyvoj2]
            start(id: s): Start
            process(id: a): Krok
            decision(id: d): Znovu?
            s -> a
            a -> d
            d -> a (ano)
            d -> d
            [/vyvoj2]
            """);
        Assert.Equal(4, CountOf(html, "class=\"fc-edge\""));
    }

    [Fact]
    public void SkippedLayer_AndRightDirection_Render()
    {
        var html = FlowchartRenderer.RenderPreview("""
            [vyvoj3]
            direction: right
            start(id: a): A
            process(id: b): B
            end(id: c): C
            a -> b
            b -> c
            a -> c
            [/vyvoj3]
            """);
        Assert.Equal(3, CountOf(html, "class=\"fc-edge\""));
    }

    [Fact]
    public void UnknownNodeAndMissingId_AreWarnings()
    {
        var html = FlowchartRenderer.RenderPreview("""
            [vyvoj4]
            start(id: s): Start
            process: Bez id
            s -> neexistuje
            [/vyvoj4]
            """);
        Assert.Contains("post-graph-warning", html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("neznámý uzel"), html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("nemá id"), html);
    }

    [Fact]
    public void NoNodes_ShowsErrorBox()
    {
        var html = FlowchartRenderer.RenderPreview("[vyvoj5]\ntitle: Nic\n[/vyvoj5]");
        Assert.Contains("post-graph-error", html);
    }

    [Fact]
    public void HtmlInLabels_IsEncoded()
    {
        var html = FlowchartRenderer.RenderPreview("[vyvoj6]\nstart(id: s): <script>x</script>\n[/vyvoj6]");
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void Output_IsWellFormedXml()
    {
        var html = FlowchartRenderer.RenderPreview(Simple);
        var svg = html[html.IndexOf("<svg", StringComparison.Ordinal)..(html.IndexOf("</svg>", StringComparison.Ordinal) + 6)];
        System.Xml.Linq.XDocument.Parse(svg);
    }

    private static int CountOf(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
