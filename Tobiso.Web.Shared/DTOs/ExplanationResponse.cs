namespace Tobiso.Web.Shared.DTOs;

public class ExplanationResponse
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int QuestionId { get; set; }
}

public class CreateExplanationRequest
{
    public string Text { get; set; } = string.Empty;
    public int QuestionId { get; set; }
}

public class UpdateExplanationRequest
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int QuestionId { get; set; }
}