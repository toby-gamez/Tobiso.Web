namespace Tobiso.Web.Shared.DTOs
{
    public class PracticeProblemRequest
    {
        public int PostId { get; set; }
        public int Count { get; set; } = 3;
        /// <summary>Preferred grade to generate for (e.g. the visitor's nav-menu default). When omitted,
        /// or when the post has no version for it, the service falls back to the nearest lower grade,
        /// then the nearest grade overall, then the post's easiest (lowest) version.</summary>
        public int? GradeId { get; set; }
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
        /// <summary>Name of the grade version actually used (after fallback resolution), for display.</summary>
        public string? GradeName { get; set; }
    }
}
