namespace Tobiso.Web.Shared.DTOs
{
    public class EvaluateComprehensionRequest
    {
        public int PostId { get; set; }
        public string StudentExplanation { get; set; } = string.Empty;
    }

    public class EvaluateComprehensionResponse
    {
        public string Feedback { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<string> StrongPoints { get; set; } = new();
        public List<string> MissingPoints { get; set; } = new();
    }
}
