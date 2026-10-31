namespace Tobiso.Web.Shared.DTOs;

public record UserStatsDto(
    int StreakDays,
    int TotalRead,
    List<SubjectReadDto> PerSubject,
    List<BadgeDto> Badges,
    // Streak ("Řada") - shared with the app. Days are yyyy-MM-dd (Europe/Prague), last ~400 days only;
    // FreezeDays is the subset covered by a streak freeze rather than real activity.
    int MaxStreak = 0,
    int Freezes = 0,
    bool TodayActive = false,
    List<string>? Days = null,
    List<string>? FreezeDays = null
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
