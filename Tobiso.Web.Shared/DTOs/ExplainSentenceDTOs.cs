namespace Tobiso.Web.Shared.DTOs
{
    public class ExplainSentenceRequest
    {
        public int PostId { get; set; }
        public string Sentence { get; set; } = string.Empty;
    }

    public class ExplainSentenceResponse
    {
        public string Explanation { get; set; } = string.Empty;
    }
}
