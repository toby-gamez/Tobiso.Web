namespace Tobiso.Web.Shared.DTOs
{
    public class PracticeProblemRequest
    {
        public int PostId { get; set; }
        public int Count { get; set; } = 3;
    }

    public class PracticeProblem
    {
        public string ProblemText { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
    }

    public class PracticeProblemResponse
    {
        public List<PracticeProblem> Problems { get; set; } = new();
    }
}
