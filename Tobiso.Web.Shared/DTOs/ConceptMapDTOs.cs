namespace Tobiso.Web.Shared.DTOs;

public class ConceptNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class ConceptEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class ConceptMapResponse
{
    public List<ConceptNode> Nodes { get; set; } = new();
    public List<ConceptEdge> Edges { get; set; } = new();
}
