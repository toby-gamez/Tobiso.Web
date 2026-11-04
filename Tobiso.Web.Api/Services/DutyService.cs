using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IDutyService
{
    // Student side and calendar
    Task<MyDutyPersonDto?> GetMyPersonAsync(int userId);
    Task<bool> SetMyPersonAsync(int userId, int? personId);
    Task<DutyOverviewDto> GetOverviewAsync(int classId, DateOnly today);
    Task<List<DutyWeekDto>> GetWeeksAsync(int classId, DateOnly from, DateOnly to);
    /// <summary>The weeks a roster name is on duty, for the calendar feed.</summary>
    Task<List<(DateOnly WeekStart, string? Partner)>> GetWeeksForPersonAsync(int personId, DateOnly from);

    // Admin: roster
    Task<List<DutyPersonDto>> GetPeopleAsync(int classId);
    Task<DutyPersonDto?> AddPersonAsync(int classId, string name);
    Task<bool> RenamePersonAsync(int personId, string name);
    /// <summary>False when the name is still used by a pair.</summary>
    Task<bool> DeletePersonAsync(int personId);
    Task<List<DutyMemberDto>> GetMembersAsync(int classId);
    Task<bool> AssignPersonAsync(int membershipId, int? personId);

    // Admin: pairs
    Task<List<DutyPairDto>> GetPairsAsync(int classId);
    Task<DutyPairDto?> AddPairAsync(int classId, SaveDutyPairRequest request);
    Task<bool> MovePairAsync(int pairId, int direction);
    /// <summary>False when the pair is in the schedule.</summary>
    Task<bool> DeletePairAsync(int pairId);

    // Admin: schedule
    Task<List<DutyWeekDto>> GetScheduleAsync(int classId);
    Task<bool> GenerateAsync(int classId, GenerateDutyScheduleRequest request);
    Task<bool> SetWeekPairAsync(int weekId, int pairId);
    /// <summary>The week's pair serves the next week as well; everything after moves one week later.</summary>
    Task<bool> AddPenaltyAsync(int weekId);
    /// <summary>No duty this week; the pair that was due, and everything after, moves one week later.</summary>
    Task<bool> MarkOffAsync(int weekId);
    /// <summary>Deletes the week and every later one, so the schedule ends before it (e.g. at the end of the school year).</summary>
    Task<bool> DeleteFromAsync(int weekId);
}

public class DutyService : IDutyService
{
    public const int MaxNameLength = 100;
    public const int MaxGeneratedWeeks = 104;

    private readonly IDbContextFactory<TobisoDbContext> _dbFactory;

    public DutyService(IDbContextFactory<TobisoDbContext> dbFactory) => _dbFactory = dbFactory;

    // ---------- Student side ----------

    public async Task<MyDutyPersonDto?> GetMyPersonAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var membership = await db.ClassMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == ClassMembershipStatus.Approved)
            .Select(m => new { m.ClassId, m.DutyPersonId })
            .FirstOrDefaultAsync();
        if (membership == null) return null;
        return new MyDutyPersonDto(membership.DutyPersonId, await GetPeopleAsync(db, membership.ClassId));
    }

    public async Task<bool> SetMyPersonAsync(int userId, int? personId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var membership = await db.ClassMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Status == ClassMembershipStatus.Approved);
        if (membership == null) return false;
        return await ApplyPersonAsync(db, membership, personId);
    }

    public async Task<DutyOverviewDto> GetOverviewAsync(int classId, DateOnly today)
    {
        var monday = MondayOf(today);
        // On a weekend the duty of the coming week is the interesting one.
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) monday = monday.AddDays(7);
        var weeks = await GetWeeksAsync(classId, monday, monday.AddDays(7));
        return new DutyOverviewDto(
            weeks.FirstOrDefault(w => w.WeekStart == monday),
            weeks.FirstOrDefault(w => w.WeekStart == monday.AddDays(7)));
    }

    public async Task<List<DutyWeekDto>> GetWeeksAsync(int classId, DateOnly from, DateOnly to)
    {
        await using var db = _dbFactory.CreateDbContext();
        var rows = await WeekQuery(db, classId)
            .Where(w => w.WeekStart >= from && w.WeekStart <= to)
            .ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task<List<(DateOnly WeekStart, string? Partner)>> GetWeeksForPersonAsync(int personId, DateOnly from)
    {
        await using var db = _dbFactory.CreateDbContext();
        var rows = await db.DutyWeeks.AsNoTracking()
            .Where(w => w.WeekStart >= from && w.Pair != null && (w.Pair.PersonAId == personId || w.Pair.PersonBId == personId))
            .OrderBy(w => w.WeekStart)
            .Select(w => new
            {
                w.WeekStart,
                Partner = w.Pair!.PersonAId == personId ? w.Pair.PersonB.Name : w.Pair.PersonA.Name
            })
            .ToListAsync();
        return rows.Select(r => (r.WeekStart, (string?)r.Partner)).ToList();
    }

    // ---------- Roster ----------

    public async Task<List<DutyPersonDto>> GetPeopleAsync(int classId)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await GetPeopleAsync(db, classId);
    }

    public async Task<DutyPersonDto?> AddPersonAsync(int classId, string name)
    {
        var clean = CleanName(name);
        if (clean == null) return null;

        await using var db = _dbFactory.CreateDbContext();
        if (!await db.SchoolClasses.AnyAsync(c => c.Id == classId)) return null;
        if (await db.DutyPeople.AnyAsync(p => p.ClassId == classId && p.Name == clean)) return null;

        var sortOrder = await db.DutyPeople.Where(p => p.ClassId == classId).MaxAsync(p => (int?)p.SortOrder) ?? 0;
        var person = new DutyPerson { ClassId = classId, Name = clean, SortOrder = sortOrder + 1 };
        db.DutyPeople.Add(person);
        await db.SaveChangesAsync();
        return new DutyPersonDto(person.Id, person.Name);
    }

    public async Task<bool> RenamePersonAsync(int personId, string name)
    {
        var clean = CleanName(name);
        if (clean == null) return false;

        await using var db = _dbFactory.CreateDbContext();
        var person = await db.DutyPeople.FindAsync(personId);
        if (person == null) return false;
        if (await db.DutyPeople.AnyAsync(p => p.ClassId == person.ClassId && p.Name == clean && p.Id != personId)) return false;

        person.Name = clean;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePersonAsync(int personId)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (await db.DutyPairs.AnyAsync(p => p.PersonAId == personId || p.PersonBId == personId)) return false;

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ClassMemberships.Where(m => m.DutyPersonId == personId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.DutyPersonId, (int?)null));
        var deleted = await db.DutyPeople.Where(p => p.Id == personId).ExecuteDeleteAsync();
        await tx.CommitAsync();
        return deleted > 0;
    }

    public async Task<List<DutyMemberDto>> GetMembersAsync(int classId)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.ClassMemberships.AsNoTracking()
            .Where(m => m.ClassId == classId && m.Status == ClassMembershipStatus.Approved)
            .OrderBy(m => m.User.DisplayName)
            .Select(m => new DutyMemberDto(m.Id, m.UserId, m.User.DisplayName, m.DutyPersonId))
            .ToListAsync();
    }

    public async Task<bool> AssignPersonAsync(int membershipId, int? personId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var membership = await db.ClassMemberships.FirstOrDefaultAsync(m => m.Id == membershipId);
        if (membership == null) return false;
        return await ApplyPersonAsync(db, membership, personId);
    }

    // A name can only be taken from the member's own class's roster.
    private static async Task<bool> ApplyPersonAsync(TobisoDbContext db, ClassMembership membership, int? personId)
    {
        if (personId != null && !await db.DutyPeople.AnyAsync(p => p.Id == personId && p.ClassId == membership.ClassId))
            return false;
        membership.DutyPersonId = personId;
        await db.SaveChangesAsync();
        return true;
    }

    // ---------- Pairs ----------

    public async Task<List<DutyPairDto>> GetPairsAsync(int classId)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.DutyPairs.AsNoTracking()
            .Where(p => p.ClassId == classId)
            .OrderBy(p => p.Position)
            .Select(p => new DutyPairDto(p.Id, p.Position, p.PersonAId, p.PersonA.Name, p.PersonBId, p.PersonB.Name))
            .ToListAsync();
    }

    public async Task<DutyPairDto?> AddPairAsync(int classId, SaveDutyPairRequest request)
    {
        if (request.PersonAId == request.PersonBId) return null;

        await using var db = _dbFactory.CreateDbContext();
        var people = await db.DutyPeople.AsNoTracking()
            .Where(p => p.ClassId == classId && (p.Id == request.PersonAId || p.Id == request.PersonBId))
            .ToListAsync();
        if (people.Count != 2) return null;

        var position = await db.DutyPairs.Where(p => p.ClassId == classId).MaxAsync(p => (int?)p.Position) ?? 0;
        var pair = new DutyPair
        {
            ClassId = classId, Position = position + 1,
            PersonAId = request.PersonAId, PersonBId = request.PersonBId
        };
        db.DutyPairs.Add(pair);
        await db.SaveChangesAsync();
        return new DutyPairDto(pair.Id, pair.Position,
            pair.PersonAId, people.First(p => p.Id == pair.PersonAId).Name,
            pair.PersonBId, people.First(p => p.Id == pair.PersonBId).Name);
    }

    public async Task<bool> MovePairAsync(int pairId, int direction)
    {
        await using var db = _dbFactory.CreateDbContext();
        var pair = await db.DutyPairs.FindAsync(pairId);
        if (pair == null) return false;

        var ordered = await db.DutyPairs.Where(p => p.ClassId == pair.ClassId).OrderBy(p => p.Position).ThenBy(p => p.Id).ToListAsync();
        var index = ordered.FindIndex(p => p.Id == pairId);
        var target = index + Math.Sign(direction);
        if (target < 0 || target >= ordered.Count) return false;

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var i = 0; i < ordered.Count; i++) ordered[i].Position = i + 1;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePairAsync(int pairId)
    {
        await using var db = _dbFactory.CreateDbContext();
        if (await db.DutyWeeks.AnyAsync(w => w.PairId == pairId)) return false;
        return await db.DutyPairs.Where(p => p.Id == pairId).ExecuteDeleteAsync() > 0;
    }

    // ---------- Schedule ----------

    public async Task<List<DutyWeekDto>> GetScheduleAsync(int classId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var rows = await WeekQuery(db, classId).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    // Replaces the schedule from the given Monday on and cycles through the pairs in order.
    // Earlier weeks are history and stay as they are.
    public async Task<bool> GenerateAsync(int classId, GenerateDutyScheduleRequest request)
    {
        if (request.WeekCount is < 1 or > MaxGeneratedWeeks) return false;

        await using var db = _dbFactory.CreateDbContext();
        var pairIds = await db.DutyPairs.Where(p => p.ClassId == classId)
            .OrderBy(p => p.Position).Select(p => p.Id).ToListAsync();
        if (pairIds.Count == 0) return false;

        var start = pairIds.IndexOf(request.StartPairId ?? pairIds[0]);
        if (start < 0) return false;
        var first = MondayOf(request.FirstMonday);

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.DutyWeeks.Where(w => w.ClassId == classId && w.WeekStart >= first).ExecuteDeleteAsync();
        for (var i = 0; i < request.WeekCount; i++)
        {
            db.DutyWeeks.Add(new DutyWeek
            {
                ClassId = classId,
                WeekStart = first.AddDays(7 * i),
                PairId = pairIds[(start + i) % pairIds.Count],
                Kind = DutyWeekKind.Regular
            });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> SetWeekPairAsync(int weekId, int pairId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var week = await db.DutyWeeks.FindAsync(weekId);
        if (week == null) return false;
        if (!await db.DutyPairs.AnyAsync(p => p.Id == pairId && p.ClassId == week.ClassId)) return false;

        week.PairId = pairId;
        week.Kind = DutyWeekKind.Regular;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddPenaltyAsync(int weekId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var week = await db.DutyWeeks.FindAsync(weekId);
        if (week?.PairId == null) return false; // nothing to repeat on a week without duty

        await using var tx = await db.Database.BeginTransactionAsync();
        await ShiftFromAsync(db, week.ClassId, week.WeekStart.AddDays(7));
        db.DutyWeeks.Add(new DutyWeek
        {
            ClassId = week.ClassId,
            WeekStart = week.WeekStart.AddDays(7),
            PairId = week.PairId,
            Kind = DutyWeekKind.Penalty
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> MarkOffAsync(int weekId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var week = await db.DutyWeeks.FindAsync(weekId);
        if (week == null || week.Kind == DutyWeekKind.Off) return false;

        await using var tx = await db.Database.BeginTransactionAsync();
        // The week itself turns into the free week, so its pair moves to the week after - like everything behind it.
        await ShiftFromAsync(db, week.ClassId, week.WeekStart.AddDays(7));
        db.DutyWeeks.Add(new DutyWeek
        {
            ClassId = week.ClassId,
            WeekStart = week.WeekStart.AddDays(7),
            PairId = week.PairId,
            Kind = week.Kind
        });
        week.PairId = null;
        week.Kind = DutyWeekKind.Off;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> DeleteFromAsync(int weekId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var week = await db.DutyWeeks.AsNoTracking().FirstOrDefaultAsync(w => w.Id == weekId);
        if (week == null) return false;
        await db.DutyWeeks.Where(w => w.ClassId == week.ClassId && w.WeekStart >= week.WeekStart).ExecuteDeleteAsync();
        return true;
    }

    // Moves every week from the given Monday on one week later, last one first so the unique
    // (class, week) index never sees two rows on the same Monday.
    private static async Task ShiftFromAsync(TobisoDbContext db, int classId, DateOnly from)
    {
        var later = await db.DutyWeeks.Where(w => w.ClassId == classId && w.WeekStart >= from)
            .OrderByDescending(w => w.WeekStart).ToListAsync();
        foreach (var w in later)
        {
            w.WeekStart = w.WeekStart.AddDays(7);
            await db.SaveChangesAsync();
        }
    }

    // ---------- Helpers ----------

    private static IQueryable<DutyWeek> WeekQuery(TobisoDbContext db, int classId) =>
        db.DutyWeeks.AsNoTracking()
            .Include(w => w.Pair!).ThenInclude(p => p.PersonA)
            .Include(w => w.Pair!).ThenInclude(p => p.PersonB)
            .Where(w => w.ClassId == classId)
            .OrderBy(w => w.WeekStart);

    private static DutyWeekDto ToDto(DutyWeek w) => new(
        w.Id, w.WeekStart, w.Kind.ToString(),
        w.Pair == null ? null
            : new DutyPairDto(w.Pair.Id, w.Pair.Position, w.Pair.PersonAId, w.Pair.PersonA.Name, w.Pair.PersonBId, w.Pair.PersonB.Name));

    private static Task<List<DutyPersonDto>> GetPeopleAsync(TobisoDbContext db, int classId) =>
        db.DutyPeople.AsNoTracking()
            .Where(p => p.ClassId == classId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new DutyPersonDto(p.Id, p.Name))
            .ToListAsync();

    public static DateOnly MondayOf(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static string? CleanName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength ? null : trimmed;
    }
}
