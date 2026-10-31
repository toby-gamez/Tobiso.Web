using Tobiso.Web.Api.Services;

namespace Tobiso.Web.Tests;

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 26);
    private static DateOnly D(int daysAgo) => Today.AddDays(-daysAgo);

    private static StreakResult Compute(DateOnly[] active, DateOnly[]? freeze = null) =>
        StreakCalculator.Compute(active, freeze ?? [], Today);

    [Fact]
    public void Empty_IsZero()
    {
        var r = Compute([]);
        Assert.Equal(0, r.Current);
        Assert.Equal(0, r.Longest);
        Assert.False(r.TodayActive);
        Assert.Empty(r.Days);
    }

    [Fact]
    public void SingleDay_Today()
    {
        var r = Compute([D(0)]);
        Assert.Equal(1, r.Current);
        Assert.Equal(1, r.Longest);
        Assert.True(r.TodayActive);
    }

    [Fact]
    public void TodayMissing_YesterdayPresent_StillCounts()
    {
        var r = Compute([D(1), D(2), D(3)]);
        Assert.Equal(3, r.Current);
        Assert.False(r.TodayActive);
    }

    [Fact]
    public void LatestDayTwoDaysAgo_BreaksCurrent_ButKeepsLongest()
    {
        var r = Compute([D(2), D(3), D(4)]);
        Assert.Equal(0, r.Current);
        Assert.Equal(3, r.Longest);
    }

    [Fact]
    public void Gap_BreaksTheStreak()
    {
        var r = Compute([D(0), D(1), D(3), D(4), D(5), D(6)]);
        Assert.Equal(2, r.Current);
        Assert.Equal(4, r.Longest);
    }

    [Fact]
    public void FreezeDay_BridgesAGap()
    {
        var r = Compute([D(0), D(1), D(3), D(4)], [D(2)]);
        Assert.Equal(5, r.Current);
        Assert.Equal(5, r.Longest);
        Assert.Equal([D(2)], r.FreezeDays);
        Assert.Contains(D(2), r.Days);
    }

    [Fact]
    public void FreezeDay_AloneKeepsStreakAliveFromYesterday()
    {
        var r = Compute([D(3), D(2)], [D(1)]);
        Assert.Equal(3, r.Current);
    }

    [Fact]
    public void FreezeDay_AlsoActive_IsNotReportedAsFreeze()
    {
        var r = Compute([D(0), D(1)], [D(1)]);
        Assert.Equal(2, r.Current);
        Assert.Empty(r.FreezeDays);
    }

    [Fact]
    public void Duplicates_AreCountedOnce()
    {
        var r = Compute([D(0), D(0), D(1), D(1), D(1)], [D(0)]);
        Assert.Equal(2, r.Current);
        Assert.Equal(2, r.Days.Count);
    }

    [Fact]
    public void FutureDays_AreIgnored()
    {
        var r = Compute([D(0), Today.AddDays(1), Today.AddDays(2)]);
        Assert.Equal(1, r.Current);
        Assert.DoesNotContain(Today.AddDays(1), r.Days);
    }

    [Fact]
    public void Longest_CanBeAnOlderRun()
    {
        var r = Compute([D(0), D(20), D(21), D(22), D(23), D(24)]);
        Assert.Equal(1, r.Current);
        Assert.Equal(5, r.Longest);
    }

    [Fact]
    public void MonthBoundaryAndLeapDay_AreConsecutive()
    {
        var today = new DateOnly(2028, 3, 1);
        var r = StreakCalculator.Compute([new(2028, 3, 1), new(2028, 2, 29), new(2028, 2, 28)], [], today);
        Assert.Equal(3, r.Current);
    }

    [Fact]
    public void Union_AcrossSources()
    {
        // e.g. activity table + read-progress + blob days merged by the caller
        var r = StreakCalculator.Compute(new[] { D(0) }.Concat([D(1)]).Concat([D(2)]), [D(3)], Today);
        Assert.Equal(4, r.Current);
    }
}

public class StreakBlobParseTests
{
    [Fact]
    public void FullBlob()
    {
        var b = StreakCalculator.ParseBlob("""
            {"streak":{"days":["2026-10-21","2026-10-22"],"freezes":2,"usedFreezes":["2026-10-20"],"milestones":[7]}}
            """);
        Assert.Equal(2, b.Days.Count);
        Assert.Contains(new DateOnly(2026, 10, 20), b.UsedFreezes);
        Assert.Equal(2, b.Freezes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"streak":null}""")]
    [InlineData("""{"streak":[1,2]}""")]
    [InlineData("""{"streak":{"days":"2026-10-21","usedFreezes":{},"freezes":"3"}}""")]
    public void MissingOrWrongShapes_GiveEmpty(string? json)
    {
        var b = StreakCalculator.ParseBlob(json);
        Assert.Empty(b.Days);
        Assert.Empty(b.UsedFreezes);
        Assert.Equal(0, b.Freezes);
    }

    [Fact]
    public void MalformedEntries_AreSkipped()
    {
        var b = StreakCalculator.ParseBlob("""
            {"streak":{"days":["2026-10-21",5,null,"2026-13-40","21.10.2026","2026-10-22T10:00:00","1999-01-01",{"a":1},"2026-10-23"]}}
            """);
        Assert.Equal([new DateOnly(2026, 10, 21), new DateOnly(2026, 10, 23)], b.Days.Order().ToArray());
    }

    [Theory]
    [InlineData("99", 3)]
    [InlineData("-4", 0)]
    [InlineData("1.5", 0)]
    [InlineData("2", 2)]
    public void Freezes_AreClampedToZeroThree(string raw, int expected)
    {
        var b = StreakCalculator.ParseBlob("{\"streak\":{\"freezes\":" + raw + "}}");
        Assert.Equal(expected, b.Freezes);
    }

    [Fact]
    public void HugeArrays_AreCapped()
    {
        var start = new DateOnly(2020, 1, 1);
        var days = string.Join(",", Enumerable.Range(0, StreakCalculator.MaxDaysScanned + 500)
            .Select(i => $"\"{start.AddDays(i):yyyy-MM-dd}\""));
        var b = StreakCalculator.ParseBlob("{\"streak\":{\"days\":[" + days + "]}}");
        Assert.Equal(StreakCalculator.MaxDaysScanned, b.Days.Count);
    }
}
