namespace Tobiso.Web.Shared.DTOs;

public class QuestionSearchRequest
{
    public List<int>? CategoryIds { get; set; }
    public string? Search { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}

public class QuestionSearchResult
{
    public List<QuestionResponse> Questions { get; set; } = new();
    public int TotalCount { get; set; }
}

public class CategoryQuestionCount
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PracticeStatsDto
{
    public int TotalQuestions { get; set; }
    public int AttemptedCount { get; set; }
    public int MasteredCount { get; set; }
    public double AccuracyPercent { get; set; }
}

public class CategoryAccuracyDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Attempted { get; set; }
    public double AccuracyPercent { get; set; }
}
