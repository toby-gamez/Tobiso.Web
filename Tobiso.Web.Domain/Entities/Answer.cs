namespace Tobiso.Web.Domain.Entities;

public class Answer
{
    public int Id { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int Correct { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
}