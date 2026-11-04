namespace Tobiso.Web.Shared.DTOs;

public record DutyPersonDto(int Id, string Name);

public record DutyPairDto(int Id, int Position, int PersonAId, string PersonAName, int PersonBId, string PersonBName)
{
    public string Label => $"{PersonAName} a {PersonBName}";
}

/// <summary>One week of the schedule. <see cref="Kind"/> is Regular, Penalty or Off; Off has no pair.</summary>
public record DutyWeekDto(int Id, DateOnly WeekStart, string Kind, DutyPairDto? Pair);

/// <summary>The current and the next duty week of a class, for the calendar banner.</summary>
public record DutyOverviewDto(DutyWeekDto? ThisWeek, DutyWeekDto? NextWeek);

public record SaveDutyPairRequest(int PersonAId, int PersonBId);

public record GenerateDutyScheduleRequest(DateOnly FirstMonday, int WeekCount, int? StartPairId);

/// <summary>A class member as the duty admin sees them: which roster name (if any) they picked.</summary>
public record DutyMemberDto(int MembershipId, int UserId, string DisplayName, int? DutyPersonId);

/// <summary>The roster name a student picked, with the whole roster to choose from.</summary>
public record MyDutyPersonDto(int? DutyPersonId, IReadOnlyList<DutyPersonDto> People);
