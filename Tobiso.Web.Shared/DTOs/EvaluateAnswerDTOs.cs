namespace Tobiso.Web.Shared.DTOs
{
    public class EvaluateAnswerRequest
    {
        public int PostId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string StudentAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public class EvaluateAnswerResponse
    {
        public bool IsCorrect { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}
