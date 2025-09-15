namespace Tobiso.Web.Domain.Entities;

public class Explanation
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
}