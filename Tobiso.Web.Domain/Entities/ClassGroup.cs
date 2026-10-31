namespace Tobiso.Web.Domain.Entities;

/// <summary>A subgroup of a class (e.g. a language or PE group); a member can belong to only one group of their class.</summary>
public class ClassGroup
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
    public string Name { get; set; } = "";

    public ICollection<ClassMembershipGroup> Members { get; set; } = [];
}
