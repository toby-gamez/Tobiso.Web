namespace Tobiso.Web.Shared.DTOs;

public class AnswerResponse
{
    public int Id { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int Correct { get; set; }
    public int QuestionId { get; set; }
}

public class CreateAnswerRequest
{
    public string AnswerText { get; set; } = string.Empty;
    public int Correct { get; set; }
}

public class UpdateAnswerRequest
{
    public int Id { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int Correct { get; set; }
    public int QuestionId { get; set; }
}