namespace Tobiso.Web.Shared.DTOs;

public class RelatedSuggestionItem
{
    public int PostId { get; set; }
    public string Text { get; set; } = "";
}

public class RelatedSuggestionsResponse
{
    public List<RelatedSuggestionItem> Items { get; set; } = new();
}
