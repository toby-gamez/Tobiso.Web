using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Shared.Helpers;

namespace Tobiso.Web.Api.Services;

public interface ICalendarFeedService
{
    /// <summary>The user's feed token, created on first use.</summary>
    Task<string> GetOrCreateTokenAsync(int userId);
    /// <summary>Replaces the token, so the previously shared URL stops working.</summary>
    Task<string> RegenerateTokenAsync(int userId);
    /// <summary>The event types the user's feed contains (all of them until they choose).</summary>
    Task<HashSet<EventType>> GetTypesAsync(int userId);
    Task SetTypesAsync(int userId, IEnumerable<EventType> types);
    /// <summary>The .ics body for the token's owner, or null when the token is unknown.</summary>
    Task<string?> BuildFeedAsync(string token, string baseUrl);
}

public class CalendarFeedService : ICalendarFeedService
{
    // Keeps the feed small: a finished event older than this is of no use in a calendar.
    private const int HistoryDays = 90;

    private readonly IDbContextFactory<TobisoDbContext> _dbFactory;
    private readonly IClassService _classService;
    private readonly IEventService _eventService;

    public CalendarFeedService(IDbContextFactory<TobisoDbContext> dbFactory, IClassService classService, IEventService eventService)
    {
        _dbFactory = dbFactory;
        _classService = classService;
        _eventService = eventService;
    }

    public async Task<string> GetOrCreateTokenAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var existing = await db.Users.Where(u => u.Id == userId).Select(u => u.CalendarToken).FirstOrDefaultAsync();
        return existing ?? await RegenerateTokenAsync(userId);
    }

    public async Task<string> RegenerateTokenAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var token = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        await db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.CalendarToken, token));
        return token;
    }

    public async Task<HashSet<EventType>> GetTypesAsync(int userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var mask = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => u.CalendarTypeMask).FirstOrDefaultAsync();
        return FromMask(mask);
    }

    public async Task SetTypesAsync(int userId, IEnumerable<EventType> types)
    {
        var mask = types.Where(Enum.IsDefined).Aggregate(0, (m, t) => m | (1 << (int)t));
        await using var db = _dbFactory.CreateDbContext();
        await db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.CalendarTypeMask, mask));
    }

    private static HashSet<EventType> FromMask(int mask) =>
        Enum.GetValues<EventType>().Where(t => (mask & (1 << (int)t)) != 0).ToHashSet();

    public async Task<string?> BuildFeedAsync(string token, string baseUrl)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 64) return null;

        await using var db = _dbFactory.CreateDbContext();
        var userId = await db.Users.AsNoTracking()
            .Where(u => u.CalendarToken == token)
            .Select(u => new { u.Id, u.CalendarTypeMask })
            .FirstOrDefaultAsync();
        if (userId == null) return null;
        var types = FromMask(userId.CalendarTypeMask);

        var viewer = await _classService.GetViewerAsync(userId.Id);
        var className = viewer.ClassId == null ? null
            : await db.SchoolClasses.AsNoTracking().Where(c => c.Id == viewer.ClassId).Select(c => c.Name).FirstOrDefaultAsync();

        var events = await _eventService.GetFeedEvents(viewer, DateTime.Today.AddDays(-HistoryDays));
        events = events.Where(e => types.Contains(e.Type)).ToList();
        return IcsBuilder.Build(events, className == null ? "Tobiso" : $"Tobiso – {className}", baseUrl);
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>Writes events as an iCalendar (RFC 5545) feed that Apple, Google and Outlook calendars can subscribe to.</summary>
public static class IcsBuilder
{
    public static string Build(IEnumerable<EventResponse> events, string calendarName, string baseUrl)
    {
        var sb = new StringBuilder();
        void Line(string line) => AppendFolded(sb, line);

        Line("BEGIN:VCALENDAR");
        Line("VERSION:2.0");
        Line("PRODID:-//Tobiso//Kalendar//CS");
        Line("CALSCALE:GREGORIAN");
        Line("METHOD:PUBLISH");
        Line($"X-WR-CALNAME:{Escape(calendarName)}");
        Line("X-WR-TIMEZONE:Europe/Prague");
        Line("REFRESH-INTERVAL;VALUE=DURATION:PT1H");
        Line("X-PUBLISHED-TTL:PT1H");

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        foreach (var e in events)
        {
            Line("BEGIN:VEVENT");
            Line($"UID:event-{e.Id}@tobiso");
            Line($"DTSTAMP:{stamp}");
            Line($"SUMMARY:{Escape(Summary(e))}");

            // Wall-clock ("floating") times: the school calendar means the same hour everywhere.
            if (e.IsAllDay)
            {
                var last = (e.EndDate ?? e.StartDate).Date;
                Line($"DTSTART;VALUE=DATE:{e.StartDate:yyyyMMdd}");
                Line($"DTEND;VALUE=DATE:{last.AddDays(1):yyyyMMdd}"); // DTEND is exclusive
            }
            else
            {
                Line($"DTSTART:{e.StartDate:yyyyMMdd'T'HHmmss}");
                Line($"DTEND:{(e.EndDate ?? e.StartDate.AddHours(1)):yyyyMMdd'T'HHmmss}");
            }

            if (e.IsRecurring && RecurrenceFreq(e.RecurrencePattern) is { } freq)
            {
                var rule = $"RRULE:FREQ={freq}";
                if (e.RecurrenceEndDate != null)
                    rule += e.IsAllDay
                        ? $";UNTIL={e.RecurrenceEndDate:yyyyMMdd}"
                        : $";UNTIL={e.RecurrenceEndDate.Value.Date:yyyyMMdd}T235959";
                Line(rule);
            }

            if (!string.IsNullOrWhiteSpace(e.Location)) Line($"LOCATION:{Escape(e.Location)}");
            var description = Description(e, baseUrl);
            if (description.Length > 0) Line($"DESCRIPTION:{Escape(description)}");
            Line($"CATEGORIES:{TypeLabel(e.Type)}");

            // Tests: a reminder the evening before (06:00 before midnight for all-day, a day ahead otherwise).
            if (e.Type == EventType.Test)
            {
                Line("BEGIN:VALARM");
                Line("ACTION:DISPLAY");
                Line($"DESCRIPTION:{Escape(e.Title)}");
                Line(e.IsAllDay ? "TRIGGER:-PT6H" : "TRIGGER:-P1D");
                Line("END:VALARM");
            }
            Line("END:VEVENT");
        }

        Line("END:VCALENDAR");
        return sb.ToString();
    }

    private static string Summary(EventResponse e) => e.Type switch
    {
        EventType.Test => $"Test: {e.Title}",
        EventType.Homework => $"Úkol: {e.Title}",
        _ => e.Title
    };

    private static string TypeLabel(EventType type) => type switch
    {
        EventType.Test => "Test",
        EventType.Homework => "Úkol",
        EventType.DayOff => "Volno",
        _ => "Jiné"
    };

    private static string Description(EventResponse e, string baseUrl)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(e.Description)) parts.Add(e.Description.Trim());
        if (e.Themes.Count > 0)
        {
            var themes = e.Themes.Select(t => t.PostId != null
                ? $"{t.Name}: {baseUrl}{PostSlug.Url(t.PostId.Value, t.PostFilePath)}"
                : $"{t.Name}: {baseUrl}/categories/{t.CategoryId}");
            parts.Add("Témata:\n" + string.Join("\n", themes));
        }
        return string.Join("\n\n", parts);
    }

    private static string? RecurrenceFreq(string? pattern) => pattern?.ToLowerInvariant() switch
    {
        "daily" => "DAILY",
        "weekly" => "WEEKLY",
        "monthly" => "MONTHLY",
        "yearly" => "YEARLY",
        null or "" => null,
        _ => "WEEKLY" // the calendar itself treats an unknown pattern as weekly
    };

    private static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
        .Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");

    // RFC 5545 §3.1: lines are at most 75 octets, continued on the next line after a single space.
    // Breaks never fall inside a UTF-8 sequence.
    private static void AppendFolded(StringBuilder sb, string line)
    {
        var bytes = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (bytes + size > 75)
            {
                sb.Append("\r\n ");
                bytes = 1;
            }
            sb.Append(rune.ToString());
            bytes += size;
        }
        sb.Append("\r\n");
    }
}
