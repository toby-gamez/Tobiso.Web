using System.ComponentModel.DataAnnotations.Schema;

namespace Tobiso.Web.Domain.Entities;

public class Question
{
    public int Id { get; set; }
    
    [Column("Question")]
    public string QuestionText { get; set; } = string.Empty;
    
    public int PostId { get; set; }
    public Post? Post { get; set; }
    public List<Answer> Answers { get; set; } = new();
    public List<Explanation> Explanations { get; set; } = new();
}