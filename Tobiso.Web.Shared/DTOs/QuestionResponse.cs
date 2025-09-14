namespace Tobiso.Web.Shared.DTOs;

public class QuestionResponse
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int PostId { get; set; }
    public List<AnswerResponse> Answers { get; set; } = new();
}

public class CreateQuestionRequest
{
    public string QuestionText { get; set; } = string.Empty;
    public int PostId { get; set; }
    public List<CreateAnswerRequest> Answers { get; set; } = new();
}

public class UpdateQuestionRequest
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<UpdateAnswerRequest> Answers { get; set; } = new();
}