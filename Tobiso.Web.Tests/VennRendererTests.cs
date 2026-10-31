using Tobiso.Web.Shared.Graphs;

namespace Tobiso.Web.Tests;

public class VennRendererTests
{
    [Fact]
    public void TwoSets_PlacesSharedElementsInIntersection()
    {
        var html = VennRenderer.RenderPreview("""
            [venn1]
            title: Dělitelé
            set(name: D12, color: BLUE): 1; 2; 3; 4
            set(name: D18, color: ORANGE): 1; 2; 9
            [/venn1]
            """);
        Assert.Contains("post-venn", html);
        Assert.Equal(2, CountOf(html, "class=\"venn-circle\""));
        // "1, 2," is the intersection, "3," and "4" belong to only D12, "9" to only D18.
        Assert.Contains(">1, 2<", html);
        Assert.Contains(">3, 4<", html);
        Assert.Contains(">9<", html);
        Assert.Contains("D12 (4)", html);
        Assert.Contains("D18 (3)", html);
    }

    [Fact]
    public void ThreeSets_AllRegionsFilled()
    {
        var html = VennRenderer.RenderPreview("""
            [venn2]
            set(name: A): a; ab; ac; abc
            set(name: B): b; ab; bc; abc
            set(name: C): c; ac; bc; abc
            [/venn2]
            """);
        Assert.Equal(3, CountOf(html, "class=\"venn-circle\""));
        foreach (var el in new[] { ">a<", ">b<", ">c<", ">ab<", ">ac<", ">bc<", ">abc<" })
            Assert.Contains(el, html);
    }

    [Fact]
    public void ExplicitRegions_AndOutside_DrawUniverse()
    {
        var html = VennRenderer.RenderPreview("""
            [venn3]
            set(name: A): x
            set(name: B): y
            both(A, B): společné
            outside: mimo
            [/venn3]
            """);
        Assert.Contains("venn-universe", html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode("společné"), html);
        Assert.Contains("mimo", html);
    }

    [Fact]
    public void FourthSet_IsIgnoredWithWarning()
    {
        var html = VennRenderer.RenderPreview("""
            [venn4]
            set(name: A): 1
            set(name: B): 2
            set(name: C): 3
            set(name: D): 4
            [/venn4]
            """);
        Assert.Equal(3, CountOf(html, "class=\"venn-circle\""));
        Assert.Contains("post-graph-warning", html);
    }

    [Fact]
    public void FewerThanTwoSets_ShowsErrorBox()
    {
        var html = VennRenderer.RenderPreview("[venn5]\nset(name: A): 1\n[/venn5]");
        Assert.Contains("post-graph-error", html);
    }

    [Fact]
    public void ExtractAndRestore_RoundTrips()
    {
        var rendered = new List<string>();
        var text = VennRenderer.ExtractBlocks("x\n\n[venn1]\nset(name: A): 1\nset(name: B): 2\n[/venn1]\n\ny", rendered);
        Assert.Single(rendered);
        Assert.Contains("TOBISOVENNPLACEHOLDER0X", text);
        Assert.Contains("post-venn", VennRenderer.RestoreBlocks("<p>TOBISOVENNPLACEHOLDER0X</p>", rendered));
    }

    [Fact]
    public void ManyElements_ShrinkOrWarn_AndStayWellFormed()
    {
        var items = string.Join("; ", Enumerable.Range(1, 80));
        var html = VennRenderer.RenderPreview($"[venn6]\nset(name: A): {items}\nset(name: B): 1\n[/venn6]");
        Assert.Contains(System.Net.WebUtility.HtmlEncode("příliš mnoho prvků"), html);
        var svg = html[html.IndexOf("<svg", StringComparison.Ordinal)..(html.IndexOf("</svg>", StringComparison.Ordinal) + 6)];
        System.Xml.Linq.XDocument.Parse(svg);
    }

    [Fact]
    public void Operations_AreOffByDefault()
    {
        var html = VennRenderer.RenderPreview("[venn7]\nset(name: A): 1\nset(name: B): 2\n[/venn7]");
        Assert.DoesNotContain("venn-op", html);
        Assert.DoesNotContain("venn-region", html);
    }

    [Fact]
    public void Operations_TwoSets_OfferTermsWithRegionsAndElements()
    {
        var html = VennRenderer.RenderPreview("""
            [venn8]
            playground: ano
            set(name: A): 1; 2; 3
            set(name: B): 2; 3; 4
            [/venn8]
            """);
        Assert.Equal(4, CountOf(html, "class=\"venn-region\""));
        Assert.Contains("venn-universe", html); // doplněk needs the universe frame
        // A ∩ B = region 3, A ∪ B = 1,2,3, A \ B = region 1, A′ = 0,2.
        Assert.Contains("data-masks=\"3\" data-desc=\"Průnik", html);
        Assert.Contains("data-masks=\"1,2,3\"", html);
        Assert.Contains(">A \\ B<", html);
        Assert.Contains("data-masks=\"0,2\"", html);
        Assert.Contains("Prvky: {2, 3}.", html);
        Assert.Contains("Prvky: {1}.", html);
        System.Xml.Linq.XDocument.Parse(html[html.IndexOf("<svg", StringComparison.Ordinal)..(html.IndexOf("</svg>", StringComparison.Ordinal) + 6)]);
    }

    [Fact]
    public void Operations_CanBeLimited_AndThreeSetsGetTripleTerms()
    {
        var html = VennRenderer.RenderPreview("""
            [venn9]
            playground: prunik
            set(name: A): a; x
            set(name: B): b; x
            set(name: C): c; x
            [/venn9]
            """);
        Assert.Contains(">A ∩ B ∩ C<", html);
        Assert.DoesNotContain("Sjednocení", html);
        Assert.DoesNotContain("venn-universe", html);
        Assert.Equal(8, CountOf(html, "class=\"venn-region\""));
    }

    [Fact]
    public void Operations_UnknownTerm_Warns()
    {
        var html = VennRenderer.RenderPreview("[venn10]\nplayground: blabla\nset(name: A): 1\nset(name: B): 2\n[/venn10]");
        Assert.Contains("post-graph-warning", html);
        Assert.DoesNotContain("venn-op", html);
    }

    [Fact]
    public void Show_DisplaysOnlyThatTerm_HighlightedFromTheStart()
    {
        var html = VennRenderer.RenderPreview("""
            [venn11]
            set(name: A): 1; 2; 3
            set(name: B): 2; 3; 4
            show: prunik A B
            [/venn11]
            """);
        Assert.Equal(1, CountOf(html, "class=\"venn-op\""));
        Assert.Contains("aria-pressed=\"true\"", html);
        Assert.Contains("data-masks=\"3\"", html);
        // Region 3 (A ∩ B) is lit from the start, the others are not.
        Assert.Equal(1, CountOf(html, "venn-region venn-hl"));
        Assert.Contains("venn-region venn-hl\" data-mask=\"3\"", html);
        // The definition is already shown, with the elements.
        Assert.Contains("Prvky: {2, 3}.</div>", html);
    }

    [Fact]
    public void Show_MultipleTerms_FirstIsActive_AndNamesWithSpacesNeedSemicolons()
    {
        var html = VennRenderer.RenderPreview("""
            [venn12]
            set(name: D 12): 1; 2
            set(name: D 18): 2; 3
            show: sjednoceni; D 12; D 18
            show: rozdil; D 18; D 12
            [/venn12]
            """);
        Assert.Equal(2, CountOf(html, "class=\"venn-op\""));
        Assert.Equal(1, CountOf(html, "aria-pressed=\"true\""));
        Assert.Contains("data-masks=\"1,2,3\"", html); // union first, lit
        Assert.DoesNotContain("post-graph-warning", html);
    }

    [Fact]
    public void Playground_WithShow_StartsWithThatTermLit()
    {
        var html = VennRenderer.RenderPreview("""
            [venn13]
            playground: ano
            show: sjednoceni A B
            set(name: A): 1; 2
            set(name: B): 2; 3
            [/venn13]
            """);
        Assert.True(CountOf(html, "class=\"venn-op\"") > 2);
        Assert.Equal(1, CountOf(html, "aria-pressed=\"true\""));
        Assert.Equal(3, CountOf(html, "venn-region venn-hl"));
    }

    [Fact]
    public void Show_InvalidTerm_WarnsAndShowsNothing()
    {
        var html = VennRenderer.RenderPreview("[venn14]\nset(name: A): 1\nset(name: B): 2\nshow: prunik A X\n[/venn14]");
        Assert.Contains("post-graph-warning", html);
        Assert.DoesNotContain("venn-op\"", html);
    }

    private static int CountOf(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
