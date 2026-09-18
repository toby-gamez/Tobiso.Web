namespace Tobiso.Web.Shared.DTOs;

public record UserStatsDto(
    int StreakDays,
    int TotalRead,
    List<SubjectReadDto> PerSubject,
    List<BadgeDto> Badges
);

public record SubjectReadDto(string SubjectName, int Count);

public record BadgeDto(string SubjectName, string Level);

public record ContinueReadingDto(int PostId, string Title, int ScrollPercent, string? CategoryPath, string? FilePath);

public record AiUsageDto(
    int QuestionsThisMonth,
    int CreditsSpentThisMonth,
    int CreditsSpentTotal,
    List<AiUsageEntryDto> RecentActivity
);

public record AiUsageEntryDto(int Delta, string Reason, DateTime CreatedAt);
