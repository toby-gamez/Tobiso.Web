namespace Tobiso.Web.Shared.DTOs;

public class GenerateQuestionRequest
{
    public int PostId { get; set; }
    public string? Content { get; set; }
    public int Count { get; set; } = 1;
    public List<string> ExistingQuestions { get; set; } = new();
}
