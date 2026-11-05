using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Tests;

public class DataRetentionServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly DbContextOptions<TobisoDbContext> _options;

    private sealed class Factory(DbContextOptions<TobisoDbContext> o) : IDbContextFactory<TobisoDbContext>
    {
        public TobisoDbContext CreateDbContext() => new(o);
    }

    public DataRetentionServiceTests()
    {
        _conn.Open();
        _options = new DbContextOptionsBuilder<TobisoDbContext>().UseSqlite(_conn).Options;
        using var db = new TobisoDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _conn.Dispose();

    private DataRetentionService Service(Dictionary<string, string?>? cfg = null) =>
        new(new Factory(_options), new ConfigurationBuilder().AddInMemoryCollection(cfg ?? []).Build(),
            NullLogger<DataRetentionService>.Instance);

    private static AppUser User(string email, DateTime created, DateTime? confirmed, DateTime? lastLogin = null) =>
        new() { Email = email, DisplayName = email, CreatedAt = created, EmailConfirmedAt = confirmed, LastLoginAt = lastLogin };

    [Fact]
    public async Task DefaultRules_DeleteOnlyExpiredData()
    {
        using (var db = new TobisoDbContext(_options))
        {
            db.Users.AddRange(
                User("old-unverified@x.cz", Now.AddDays(-8), null),
                User("new-unverified@x.cz", Now.AddDays(-1), null),
                User("old-verified-inactive@x.cz", Now.AddYears(-5), Now.AddYears(-5), Now.AddYears(-5)));
            db.AnonymousAiUsages.AddRange(
                new AnonymousAiUsage { DeviceId = "device:old", LastUsedAt = Now.AddDays(-8) },
                new AnonymousAiUsage { DeviceId = "device:new", LastUsedAt = Now.AddHours(-1) });
            db.DeviceTokens.AddRange(
                new DeviceToken { FcmToken = "stale", LastSeenAt = Now.AddDays(-200), RegisteredAt = Now.AddDays(-300) },
                new DeviceToken { FcmToken = "live", LastSeenAt = Now.AddDays(-1), RegisteredAt = Now.AddDays(-300) });
            db.Feedbacks.AddRange(
                new Feedback { Message = "old", CreatedAt = Now.AddDays(-731) },
                new Feedback { Message = "new", CreatedAt = Now.AddDays(-10) });
            await db.SaveChangesAsync();
        }

        var r = await Service().RunAsync(Now);

        Assert.Equal(new DataRetentionResult(1, 1, 1, 1, 0, 0), r);
        using var check = new TobisoDbContext(_options);
        Assert.Equal(["new-unverified@x.cz", "old-verified-inactive@x.cz"],
            check.Users.Select(u => u.Email).OrderBy(e => e).ToList());
        Assert.Equal("device:new", Assert.Single(check.AnonymousAiUsages).DeviceId);
        Assert.Equal("live", Assert.Single(check.DeviceTokens).FcmToken);
        Assert.Equal("new", Assert.Single(check.Feedbacks).Message);
    }

    [Fact]
    public async Task InactiveAccountRule_WhenEnabled_DeletesAccountAndItsData()
    {
        using (var db = new TobisoDbContext(_options))
        {
            var inactive = User("inactive@x.cz", Now.AddYears(-4), Now.AddYears(-4), Now.AddDays(-1100));
            inactive.ChatSessions.Add(new AiChatSession { UpdatedAt = Now.AddDays(-1100) });
            db.Users.AddRange(inactive, User("active@x.cz", Now.AddYears(-4), Now.AddYears(-4), Now.AddDays(-3)));
            await db.SaveChangesAsync();
        }

        var r = await Service(new() { ["Retention:InactiveAccountDays"] = "1095" }).RunAsync(Now);

        Assert.Equal(1, r.InactiveAccounts);
        using var check = new TobisoDbContext(_options);
        Assert.Equal("active@x.cz", Assert.Single(check.Users).Email);
        Assert.Empty(check.AiChatSessions);
    }
}
