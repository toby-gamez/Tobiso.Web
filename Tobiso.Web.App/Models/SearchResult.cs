namespace Tobiso.Web.App.Models
{
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string HighlightedTerm { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool FoundInTitle { get; set; }
        public bool FoundInContent { get; set; }
    }
}