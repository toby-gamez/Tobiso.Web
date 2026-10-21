namespace Tobiso.Web.Domain.Entities;

/// <summary>A school class (e.g. "8.B") students can ask to join; membership is admin-approved.</summary>
public class SchoolClass
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? GradeId { get; set; }
    public Grade? Grade { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClassGroup> Groups { get; set; } = [];
    public ICollection<ClassMembership> Memberships { get; set; } = [];
}
