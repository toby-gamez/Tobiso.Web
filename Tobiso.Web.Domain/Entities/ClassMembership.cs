using System.Text.Json.Serialization;

namespace Tobiso.Web.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter<ClassMembershipStatus>))]
public enum ClassMembershipStatus { Pending, Approved, Rejected }

/// <summary>A user's request to join a class. One per user - joining another class replaces it.</summary>
public class ClassMembership
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
    public ClassMembershipStatus Status { get; set; } = ClassMembershipStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }

    public ICollection<ClassMembershipGroup> Groups { get; set; } = [];
}

public class ClassMembershipGroup
{
    public int MembershipId { get; set; }
    public ClassMembership Membership { get; set; } = null!;
    public int GroupId { get; set; }
    public ClassGroup Group { get; set; } = null!;
}
