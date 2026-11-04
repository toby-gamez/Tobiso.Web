using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Tobiso.Web.Api.Services;

/// <summary>One lesson from the school's public Bakaláři timetable.</summary>
public record TimetableLesson(
    DateTime Start,
    DateTime End,
    string SubjectAbbrev,
    string SubjectName,
    string? Teacher,
    string? Room,
    string? Group,
    string? Theme);

public enum TimetableWeek { Actual, Next }

public interface ITimetableService
{
    /// <summary>Lessons of the week whose subject abbreviation or name contains <paramref name="subject"/> (all when empty).</summary>
    /// <exception cref="InvalidOperationException">The timetable could not be loaded or understood.</exception>
    Task<List<TimetableLesson>> FindLessonsAsync(string classCode, TimetableWeek week, string? subject, CancellationToken ct = default);
}

public class TimetableService : ITimetableService
{
    private static readonly Regex DataRegex = new(@"const\s+timetableData\s*=\s*", RegexOptions.Compiled);
    private static readonly Regex DayMonthRegex = new(@"(\d{1,2})\.\s*(\d{1,2})\.", RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;

    public TimetableService(HttpClient http, IMemoryCache cache, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _baseUrl = (config["Timetable:BaseUrl"] ?? "https://bakalari.spssol.cz/bakaweb/Timetable/Public").TrimEnd('/');
    }

    public async Task<List<TimetableLesson>> FindLessonsAsync(string classCode, TimetableWeek week, string? subject, CancellationToken ct = default)
    {
        classCode = classCode.Trim();
        if (classCode.Length == 0) throw new InvalidOperationException("Chybí kód třídy.");

        var key = $"timetable:{classCode}:{week}";
        if (!_cache.TryGetValue(key, out List<TimetableLesson>? lessons) || lessons is null)
        {
            lessons = await LoadAsync(classCode, week, ct);
            _cache.Set(key, lessons, TimeSpan.FromMinutes(15));
        }

        var term = subject?.Trim();
        return string.IsNullOrEmpty(term)
            ? lessons
            : lessons.Where(l =>
                l.SubjectAbbrev.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.SubjectName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task<List<TimetableLesson>> LoadAsync(string classCode, TimetableWeek week, CancellationToken ct)
    {
        var url = $"{_baseUrl}/{week}/Class/{Uri.EscapeDataString(classCode)}";
        string html;
        try
        {
            html = await _http.GetStringAsync(url, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Rozvrh se nepodařilo načíst z Bakalářů.", ex);
        }

        var m = DataRegex.Match(html);
        if (!m.Success) throw new InvalidOperationException("Rozvrh má neočekávaný formát (Bakaláři se možná změnily).");

        try
        {
            // The object is followed by more script, so read exactly one JSON value from where it starts.
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(html[(m.Index + m.Length)..]));
            using var doc = JsonDocument.ParseValue(ref reader);
            return Parse(doc.RootElement, ReferenceMonday(week));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new InvalidOperationException("Rozvrh má neočekávaný formát (Bakaláři se možná změnily).", ex);
        }
    }

    private static DateTime ReferenceMonday(TimetableWeek week)
    {
        var today = DateTime.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        // Bakaláři switch the "actual" week on the weekend, so Saturday/Sunday already show Monday ahead.
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) monday = monday.AddDays(7);
        return week == TimetableWeek.Next ? monday.AddDays(7) : monday;
    }

    private static List<TimetableLesson> Parse(JsonElement root, DateTime monday)
    {
        var result = new List<TimetableLesson>();
        foreach (var day in root.GetProperty("Days").EnumerateArray())
        {
            var dm = DayMonthRegex.Match(day.GetProperty("Date").GetString() ?? "");
            if (!dm.Success || day.GetProperty("DayOff").GetBoolean()) continue;
            var date = NearestDate(int.Parse(dm.Groups[1].Value), int.Parse(dm.Groups[2].Value), monday);

            foreach (var hour in day.GetProperty("Hours").EnumerateArray())
            {
                if (hour.GetProperty("Type").GetString() != "atoms" || hour.GetProperty("Atoms").ValueKind != JsonValueKind.Array) continue;
                if (!TimeSpan.TryParse(hour.GetProperty("Begin").GetString(), CultureInfo.InvariantCulture, out var begin) ||
                    !TimeSpan.TryParse(hour.GetProperty("End").GetString(), CultureInfo.InvariantCulture, out var end)) continue;

                foreach (var atom in hour.GetProperty("Atoms").EnumerateArray())
                {
                    var abbrev = Str(atom, "SubjectAbbrev");
                    if (string.IsNullOrEmpty(abbrev)) continue;
                    result.Add(new TimetableLesson(
                        date + begin, date + end, abbrev, Str(atom, "SubjectText") ?? abbrev,
                        Str(atom, "TeacherFullname") ?? Str(atom, "Teacher"), Str(atom, "Room"),
                        Str(atom, "GroupsNames"), Str(atom, "Theme")));
                }
            }
        }
        return result.OrderBy(l => l.Start).ToList();
    }

    // The page has no year: take the one that puts the date closest to the week's Monday.
    private static DateTime NearestDate(int day, int month, DateTime monday) =>
        new[] { monday.Year - 1, monday.Year, monday.Year + 1 }
            .Select(y => new DateTime(y, month, day))
            .MinBy(d => Math.Abs((d - monday).TotalDays));

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s ? s : null;
}
