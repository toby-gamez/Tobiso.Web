using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Shared.DTOs;

public record ClassGroupDto(int Id, string Name);

public record SchoolClassDto(int Id, string Name, int? GradeId, string? GradeName, List<ClassGroupDto> Groups,
    int ApprovedCount = 0, int PendingCount = 0);

/// <summary>The signed-in student's class membership (null when they haven't asked to join one).</summary>
public record MyClassDto(int ClassId, string ClassName, ClassMembershipStatus Status, List<ClassGroupDto> Groups,
    List<ClassGroupDto> AvailableGroups);

public record JoinClassRequest(int ClassId, List<int>? GroupIds);

public record UpdateMyGroupsRequest(List<int> GroupIds);

public record ClassMemberDto(int MembershipId, int UserId, string DisplayName, string Email, int ClassId,
    string ClassName, ClassMembershipStatus Status, DateTime RequestedAt, DateTime? DecidedAt, List<ClassGroupDto> Groups);

public record SaveClassRequest(string Name, int? GradeId);

public record SaveClassGroupRequest(string Name);

/// <summary>
/// Who is looking at the calendar. Null (anonymous / not in a class) sees only global events,
/// exactly as before classes existed.
/// </summary>
public record EventViewer(int UserId, int? ClassId, IReadOnlyCollection<int> GroupIds);
