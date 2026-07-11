namespace Tobiso.Web.Shared.DTOs;

public record UserStatsDto(
    int StreakDays,
    int TotalRead,
    List<SubjectReadDto> PerSubject,
    List<BadgeDto> Badges
);

public record SubjectReadDto(string SubjectName, int Count);

public record BadgeDto(string SubjectName, string Level);
