using System.Globalization;
using System.Text.Json;

namespace Tobiso.Web.Api.Services;

/// <summary>What the app keeps under <c>state.streak</c> in the game-state blob, after validation.</summary>
public record StreakBlob(IReadOnlySet<DateOnly> Days, IReadOnlySet<DateOnly> UsedFreezes, int Freezes)
{
    public static readonly StreakBlob Empty = new(new HashSet<DateOnly>(), new HashSet<DateOnly>(), 0);
}

public record StreakResult(
    int Current,
    int Longest,
    bool TodayActive,
    /// <summary>Every covered day (real activity + freeze days), ascending.</summary>
    IReadOnlyList<DateOnly> Days,
    /// <summary>Covered days that were bridged by a freeze and had no real activity, ascending.</summary>
    IReadOnlyList<DateOnly> FreezeDays);

/// <summary>
/// The shared "Řada" rules (identical to the Android app) plus a defensive reader for the opaque blob.
/// Pure and clock-free: <c>today</c> is always passed in.
/// </summary>
public static class StreakCalculator
{
    public const int MaxFreezes = 3;
    // Hard caps on how much of the user-controlled blob is looked at.
    public const int MaxDaysScanned = 5000;
    public const int MaxFreezeDaysScanned = 500;
    // Days before this are treated as garbage.
    private static readonly DateOnly MinDate = new(2020, 1, 1);

    public static StreakResult Compute(IEnumerable<DateOnly> activeDays, IEnumerable<DateOnly> freezeDays, DateOnly today)
    {
        // Future days (clock skew, tampered blob) never count.
        var active = activeDays.Where(d => d <= today).ToHashSet();
        var freeze = freezeDays.Where(d => d <= today).ToHashSet();
        var union = new HashSet<DateOnly>(active);
        union.UnionWith(freeze);

        var current = 0;
        var longest = 0;
        if (union.Count > 0)
        {
            var latest = union.Max();
            if (latest >= today.AddDays(-1))
                for (var d = latest; union.Contains(d); d = d.AddDays(-1)) current++;

            var run = 0;
            DateOnly? prev = null;
            foreach (var d in union.Order())
            {
                run = prev is { } p && p.AddDays(1) == d ? run + 1 : 1;
                longest = Math.Max(longest, run);
                prev = d;
            }
        }

        return new StreakResult(
            current, longest, union.Contains(today),
            [.. union.Order()],
            [.. freeze.Except(active).Order()]);
    }

    /// <summary>
    /// Reads <c>streak.{days, freezes, usedFreezes}</c> from the stored game-state JSON. Anything missing,
    /// of the wrong type or unparsable is skipped; never throws.
    /// </summary>
    public static StreakBlob ParseBlob(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return StreakBlob.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("streak", out var streak)
                || streak.ValueKind != JsonValueKind.Object)
                return StreakBlob.Empty;

            var freezes = 0;
            if (streak.TryGetProperty("freezes", out var f) && f.ValueKind == JsonValueKind.Number
                && f.TryGetInt32(out var n))
                freezes = Math.Clamp(n, 0, MaxFreezes);

            return new StreakBlob(
                ReadDates(streak, "days", MaxDaysScanned),
                ReadDates(streak, "usedFreezes", MaxFreezeDaysScanned),
                freezes);
        }
        catch (JsonException)
        {
            return StreakBlob.Empty;
        }
    }

    private static HashSet<DateOnly> ReadDates(JsonElement parent, string name, int maxScanned)
    {
        var result = new HashSet<DateOnly>();
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array) return result;
        var scanned = 0;
        foreach (var item in arr.EnumerateArray())
        {
            if (++scanned > maxScanned) break;
            if (item.ValueKind == JsonValueKind.String
                && DateOnly.TryParseExact(item.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date)
                && date >= MinDate)
                result.Add(date);
        }
        return result;
    }
}
