namespace Tobiso.Web.Shared.DTOs
{
    public class GrammarCheckRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class GrammarIssue
    {
        // The exact original incorrect snippet from the text
        public string OriginalText { get; set; } = string.Empty;
        // The suggested correction (replacement text)
        public string Correction { get; set; } = string.Empty;
        // Short explanation in the same language as the text
        public string Explanation { get; set; } = string.Empty;
    }

    public class GrammarCheckResponse
    {
        public List<GrammarIssue> Issues { get; set; } = new List<GrammarIssue>();
    }
}
