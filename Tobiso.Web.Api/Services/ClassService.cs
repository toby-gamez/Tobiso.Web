using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IClassService
{
    // Shared / student side
    Task<List<SchoolClassDto>> GetAllAsync();
    Task<MyClassDto?> GetMyClassAsync(int userId);
    Task<EventViewer> GetViewerAsync(int userId);
    Task<bool> RequestJoinAsync(int userId, int classId, IEnumerable<int>? groupIds);
    Task<bool> UpdateMyGroupsAsync(int userId, IEnumerable<int> groupIds);
    Task LeaveAsync(int userId);

    // Admin side
    Task<List<ClassMemberDto>> GetMembersAsync(int? classId = null, ClassMembershipStatus? status = null);
    Task<int> GetPendingCountAsync();
    Task<bool> ApproveAsync(int membershipId);
    Task<bool> RejectAsync(int membershipId);
    Task<bool> RemoveMemberAsync(int membershipId);
    Task<bool> SetMemberGroupsAsync(int membershipId, IEnumerable<int> groupIds);
    Task<SchoolClassDto?> CreateClassAsync(SaveClassRequest request);
    Task<bool> UpdateClassAsync(int classId, SaveClassRequest request);
    Task<bool> DeleteClassAsync(int classId);
    Task<ClassGroupDto?> AddGroupAsync(int classId, string name);
    Task<bool> RenameGroupAsync(int groupId, string name);
    Task<bool> DeleteGroupAsync(int groupId);
}

public class ClassService : IClassService
{
    public const int MaxNameLength = 50;

    // Factory (own short-lived DbContext per call): the calendar and profile call this alongside
    // other services on the same Blazor circuit.
    private readonly IDbContextFactory<TobisoDbContext> _dbFactory;

    public ClassService(IDbContextFactory<TobisoDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<SchoolClassDto>> GetAllAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.SchoolClasses.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SchoolClassDto(
                c.Id, c.Name, c.GradeId, c.Grade != null ? c.Grade.Name : null,
                c.Groups.OrderBy(g => g.Name).Select(g => new ClassGroupDto(g.Id, g.Name)).ToList(),
                c.Memberships.Count(m => m.Status == ClassMembershipStatus.Approved),
                c.Memberships.Count(m => m.Status == ClassMembershipStatus.Pending)))
            .ToListAsync();
    }

    public async Task<MyClassDto?> GetMyClassAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.ClassMemberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new MyClassDto(
                m.ClassId, m.Class.Name, m.Status,
                m.Groups.OrderBy(g => g.Group.Name).Select(g => new ClassGroupDto(g.GroupId, g.Group.Name)).ToList(),
                m.Class.Groups.OrderBy(g => g.Name).Select(g => new ClassGroupDto(g.Id, g.Name)).ToList()))
            .FirstOrDefaultAsync();
    }

    // Only an approved membership grants class/group visibility; pending or rejected users see
    // what everyone else sees, plus their own personal events.
    public async Task<EventViewer> GetViewerAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var membership = await db.ClassMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == ClassMembershipStatus.Approved)
            .Select(m => new { m.ClassId, GroupIds = m.Groups.Select(g => g.GroupId).ToList() })
            .FirstOrDefaultAsync();

        return membership == null
            ? new EventViewer(userId, null, [])
            : new EventViewer(userId, membership.ClassId, membership.GroupIds);
    }

    public async Task<bool> RequestJoinAsync(int userId, int classId, IEnumerable<int>? groupIds)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (!await db.SchoolClasses.AnyAsync(c => c.Id == classId)) return false;

        var membership = await db.ClassMemberships.Include(m => m.Groups)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (membership == null)
        {
            membership = new ClassMembership { UserId = userId, ClassId = classId };
            db.ClassMemberships.Add(membership);
        }
        else if (membership.ClassId != classId || membership.Status == ClassMembershipStatus.Rejected)
        {
            // A different class (or another try after a rejection) needs approval again.
            membership.ClassId = classId;
            membership.Status = ClassMembershipStatus.Pending;
            membership.RequestedAt = DateTime.UtcNow;
            membership.DecidedAt = null;
        }

        await ReplaceGroupsAsync(db, membership, groupIds);
        await db.SaveChangesAsync();
        return true;
    }

    // Picking groups inside an already chosen class doesn't need approval again.
    public async Task<bool> UpdateMyGroupsAsync(int userId, IEnumerable<int> groupIds)
    {
        await using var db = _dbFactory.CreateDbContext();
        var membership = await db.ClassMemberships.Include(m => m.Groups)
            .FirstOrDefaultAsync(m => m.UserId == userId);
        if (membership == null) return false;

        await ReplaceGroupsAsync(db, membership, groupIds);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task LeaveAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        await db.ClassMemberships.Where(m => m.UserId == userId).ExecuteDeleteAsync();
    }

    public async Task<List<ClassMemberDto>> GetMembersAsync(int? classId = null, ClassMembershipStatus? status = null)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.ClassMemberships.AsNoTracking();
        if (classId != null) query = query.Where(m => m.ClassId == classId);
        if (status != null) query = query.Where(m => m.Status == status);

        return await query
            .OrderBy(m => m.Status).ThenBy(m => m.Class.Name).ThenBy(m => m.User.DisplayName)
            .Select(m => new ClassMemberDto(
                m.Id, m.UserId, m.User.DisplayName, m.User.Email, m.ClassId, m.Class.Name,
                m.Status, m.RequestedAt, m.DecidedAt,
                m.Groups.OrderBy(g => g.Group.Name).Select(g => new ClassGroupDto(g.GroupId, g.Group.Name)).ToList()))
            .ToListAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.ClassMemberships.CountAsync(m => m.Status == ClassMembershipStatus.Pending);
    }

    public Task<bool> ApproveAsync(int membershipId) => DecideAsync(membershipId, ClassMembershipStatus.Approved);

    public Task<bool> RejectAsync(int membershipId) => DecideAsync(membershipId, ClassMembershipStatus.Rejected);

    private async Task<bool> DecideAsync(int membershipId, ClassMembershipStatus status)
    {
        await using var db = _dbFactory.CreateDbContext();
        var updated = await db.ClassMemberships.Where(m => m.Id == membershipId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, status)
                .SetProperty(m => m.DecidedAt, DateTime.UtcNow));
        return updated > 0;
    }

    public async Task<bool> RemoveMemberAsync(int membershipId)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.ClassMemberships.Where(m => m.Id == membershipId).ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> SetMemberGroupsAsync(int membershipId, IEnumerable<int> groupIds)
    {
        await using var db = _dbFactory.CreateDbContext();
        var membership = await db.ClassMemberships.Include(m => m.Groups)
            .FirstOrDefaultAsync(m => m.Id == membershipId);
        if (membership == null) return false;

        await ReplaceGroupsAsync(db, membership, groupIds);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<SchoolClassDto?> CreateClassAsync(SaveClassRequest request)
    {
        var name = CleanName(request.Name);
        if (name == null) return null;

        await using var db = _dbFactory.CreateDbContext();
        if (await db.SchoolClasses.AnyAsync(c => c.Name == name)) return null;

        var schoolClass = new SchoolClass { Name = name, GradeId = await ValidGradeIdAsync(db, request.GradeId) };
        db.SchoolClasses.Add(schoolClass);
        await db.SaveChangesAsync();
        return new SchoolClassDto(schoolClass.Id, schoolClass.Name, schoolClass.GradeId, null, []);
    }

    public async Task<bool> UpdateClassAsync(int classId, SaveClassRequest request)
    {
        var name = CleanName(request.Name);
        if (name == null) return false;

        await using var db = _dbFactory.CreateDbContext();
        var schoolClass = await db.SchoolClasses.FindAsync(classId);
        if (schoolClass == null) return false;
        if (await db.SchoolClasses.AnyAsync(c => c.Name == name && c.Id != classId)) return false;

        schoolClass.Name = name;
        schoolClass.GradeId = await ValidGradeIdAsync(db, request.GradeId);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClassAsync(int classId)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (!await db.SchoolClasses.AnyAsync(c => c.Id == classId)) return false;

        // The group foreign keys are NoAction (see TobisoDbContext), so clear what points at this
        // class's groups first; memberships and class events then go with the class by cascade.
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ClassMembershipGroups.Where(mg => mg.Group.ClassId == classId).ExecuteDeleteAsync();
        await db.Events.Where(e => e.ClassId == classId || (e.Group != null && e.Group.ClassId == classId)).ExecuteDeleteAsync();
        await db.SchoolClasses.Where(c => c.Id == classId).ExecuteDeleteAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<ClassGroupDto?> AddGroupAsync(int classId, string name)
    {
        var clean = CleanName(name);
        if (clean == null) return null;

        await using var db = _dbFactory.CreateDbContext();
        if (!await db.SchoolClasses.AnyAsync(c => c.Id == classId)) return null;
        if (await db.ClassGroups.AnyAsync(g => g.ClassId == classId && g.Name == clean)) return null;

        var group = new ClassGroup { ClassId = classId, Name = clean };
        db.ClassGroups.Add(group);
        await db.SaveChangesAsync();
        return new ClassGroupDto(group.Id, group.Name);
    }

    public async Task<bool> RenameGroupAsync(int groupId, string name)
    {
        var clean = CleanName(name);
        if (clean == null) return false;

        await using var db = _dbFactory.CreateDbContext();
        var group = await db.ClassGroups.FindAsync(groupId);
        if (group == null) return false;
        if (await db.ClassGroups.AnyAsync(g => g.ClassId == group.ClassId && g.Name == clean && g.Id != groupId)) return false;

        group.Name = clean;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (!await db.ClassGroups.AnyAsync(g => g.Id == groupId)) return false;

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ClassMembershipGroups.Where(mg => mg.GroupId == groupId).ExecuteDeleteAsync();
        await db.Events.Where(e => e.GroupId == groupId).ExecuteDeleteAsync();
        await db.ClassGroups.Where(g => g.Id == groupId).ExecuteDeleteAsync();
        await tx.CommitAsync();
        return true;
    }

    private static async Task ReplaceGroupsAsync(TobisoDbContext db, ClassMembership membership, IEnumerable<int>? groupIds)
    {
        var validGroupIds = await ValidGroupIdsAsync(db, membership.ClassId, groupIds);
        // Diff rather than Clear()+re-add: re-adding a row with the key of one just removed would
        // make EF track two instances with the same (MembershipId, GroupId).
        foreach (var link in membership.Groups.Where(g => !validGroupIds.Contains(g.GroupId)).ToList())
            membership.Groups.Remove(link);
        foreach (var groupId in validGroupIds.Where(id => membership.Groups.All(g => g.GroupId != id)))
            membership.Groups.Add(new ClassMembershipGroup { GroupId = groupId });
    }

    // Drops ids that don't belong to the class, so a crafted request can't join another class's group.
    private static async Task<List<int>> ValidGroupIdsAsync(TobisoDbContext db, int classId, IEnumerable<int>? groupIds)
    {
        var requested = groupIds?.Distinct().ToList() ?? [];
        if (requested.Count == 0) return [];
        return await db.ClassGroups
            .Where(g => g.ClassId == classId && requested.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();
    }

    private static async Task<int?> ValidGradeIdAsync(TobisoDbContext db, int? gradeId) =>
        gradeId != null && await db.Grades.AnyAsync(g => g.Id == gradeId) ? gradeId : null;

    private static string? CleanName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength ? null : trimmed;
    }
}
