using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Tests;

public class AccountDeletionServiceTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly DbContextOptions<TobisoDbContext> _options;

    private sealed class Factory(DbContextOptions<TobisoDbContext> o) : IDbContextFactory<TobisoDbContext>
    {
        public TobisoDbContext CreateDbContext() => new(o);
    }

    public AccountDeletionServiceTests()
    {
        _conn.Open();
        _options = new DbContextOptionsBuilder<TobisoDbContext>().UseSqlite(_conn).Options;
        using var db = new TobisoDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _conn.Dispose();

    private AccountDeletionService Service() => new(new Factory(_options));

    private static AppUser User(string email) => new() { Email = email, DisplayName = email };

    [Fact]
    public async Task Delete_RemovesTheUserAndTheirData_AndKeepsOthers()
    {
        int doomedId, keptId;
        using (var db = new TobisoDbContext(_options))
        {
            var doomed = User("doomed@x.cz");
            doomed.ChatSessions.Add(new AiChatSession());
            doomed.PointsLedger.Add(new PointsLedgerEntry { Kind = "quiz", Delta = 5 });
            doomed.ActivityDays.Add(new UserActivityDay { Date = new DateOnly(2026, 11, 1) });
            var kept = User("kept@x.cz");
            kept.ChatSessions.Add(new AiChatSession());
            db.Users.AddRange(doomed, kept);
            await db.SaveChangesAsync();
            doomedId = doomed.Id;
            keptId = kept.Id;
        }

        var deleted = await Service().DeleteAsync(doomedId);

        Assert.True(deleted);
        using var check = new TobisoDbContext(_options);
        Assert.Equal(keptId, Assert.Single(check.Users).Id);
        Assert.Equal(keptId, Assert.Single(check.AiChatSessions).UserId);
        Assert.Empty(check.PointsLedger);
        Assert.Empty(check.UserActivityDays);
    }

    [Fact]
    public async Task Delete_ReturnsFalseForAnUnknownUser()
    {
        Assert.False(await Service().DeleteAsync(12345));
    }
}

public class LegalNoticeServiceTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly DbContextOptions<TobisoDbContext> _options;

    private sealed class Factory(DbContextOptions<TobisoDbContext> o) : IDbContextFactory<TobisoDbContext>
    {
        public TobisoDbContext CreateDbContext() => new(o);
    }

    public LegalNoticeServiceTests()
    {
        _conn.Open();
        _options = new DbContextOptionsBuilder<TobisoDbContext>().UseSqlite(_conn).Options;
        using var db = new TobisoDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _conn.Dispose();

    private async Task<int> SeedAsync(DateTime userCreated, DateTime legalEdited)
    {
        using var db = new TobisoDbContext(_options);
        var grade = new Grade { Name = "9. třída", Level = 9 };
        db.Grades.Add(grade);
        await db.SaveChangesAsync();
        var post = new Post { Title = "Podmínky", FilePath = "legal/terms-of-use-cs.md" };
        post.Versions.Add(new PostVersion { GradeId = grade.Id, Content = "x", LastEdit = legalEdited });
        db.Posts.Add(post);
        var user = new AppUser { Email = "a@b.cz", DisplayName = "A", CreatedAt = userCreated };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task ChangeAfterSignup_IsPending_UntilDismissed()
    {
        var changed = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = await SeedAsync(userCreated: changed.AddDays(-30), legalEdited: changed);
        var service = new LegalNoticeService(new Factory(_options));

        Assert.Equal(changed, await service.GetPendingChangeAsync(userId));

        await service.DismissAsync(userId, changed);

        Assert.Null(await service.GetPendingChangeAsync(userId));
    }

    [Fact]
    public async Task UserWhoSignedUpAfterTheChange_HasNothingPending()
    {
        var changed = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = await SeedAsync(userCreated: changed.AddDays(5), legalEdited: changed);

        Assert.Null(await new LegalNoticeService(new Factory(_options)).GetPendingChangeAsync(userId));
    }
}
