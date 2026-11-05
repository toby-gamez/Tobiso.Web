using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Tobiso.Web.Api.Services;
using Tobiso.Web.App.Services;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Tests;

// Regression tests for the AI cost guard. The July audit fix (C2) silently regressed once already:
// the anonymous allowance ended up keyed on the client-controlled X-Device-Id again.
public class AiUsageGuardTests
{
    private sealed class InMemoryAnonymousUsage : IAnonymousUsageService
    {
        private readonly Dictionary<string, int> _counts = new();
        public int RowsCreated => _counts.Count;

        public Task<bool> TryConsumeAsync(string deviceId, int limit)
        {
            _counts.TryGetValue(deviceId, out var c);
            if (c >= limit) return Task.FromResult(false);
            _counts[deviceId] = c + 1;
            return Task.FromResult(true);
        }

        public Task<int> GetRemainingAsync(string deviceId, int limit) =>
            Task.FromResult(Math.Max(0, limit - _counts.GetValueOrDefault(deviceId)));
    }

    private sealed class FakeUsers(int credits) : IUserService
    {
        public Task<AppUser?> GetByIdAsync(int id) => Task.FromResult<AppUser?>(new AppUser { Id = id, Credits = credits });
        public Task<AppUser?> RegisterAsync(string email, string displayName, string password, bool acceptedTerms) => throw new NotSupportedException();
        public Task<AppUser?> FindOrCreateGoogleUserAsync(string googleId, string email, string displayName, string? avatarUrl = null, bool acceptedTerms = false) => throw new NotSupportedException();
        public Task<AppUser?> LoginAsync(string email, string password) => throw new NotSupportedException();
        public Task SetPreferredGradeAsync(int userId, int? gradeId) => throw new NotSupportedException();
        public Task<bool> DeductCreditsAsync(int userId, int amount, string reason) => throw new NotSupportedException();
        public Task RefundCreditsAsync(int userId, int amount, string reason) => throw new NotSupportedException();
        public Task<bool> ClaimDailyBonusAsync(int userId, int amount) => throw new NotSupportedException();
        public Task<bool> ClaimReadBonusAsync(int userId, int amount) => throw new NotSupportedException();
        public Task<DeleteAccountResult> DeleteAccountAsync(int userId, string? password, string? confirmEmail) => throw new NotSupportedException();
        public Task<AppUser?> GetByEmailAsync(string email) => throw new NotSupportedException();
        public Task<bool> IsSessionValidAsync(int userId, string securityStamp) => throw new NotSupportedException();
        public Task RotateSecurityStampAsync(int userId) => throw new NotSupportedException();
    }

    private static IConfiguration Config(int anonDaily = 3, int ipDaily = 10, int global = 1000) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenAI:AnonymousDailyRequests"] = anonDaily.ToString(),
            ["OpenAI:AnonymousIpDailyRequests"] = ipDaily.ToString(),
            ["OpenAI:GlobalDailyRequests"] = global.ToString(),
        }).Build();

    private static ClaimsPrincipal Student(int id) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim("role", "student")], "jwt"));

    [Fact]
    public async Task Anonymous_RotatingDeviceId_IsCappedByIp()
    {
        var anon = new InMemoryAnonymousUsage();
        var guard = new AiUsageGuard(new AiRateLimitService(), anon, Config(anonDaily: 3, ipDaily: 10));

        var allowed = 0;
        for (var i = 0; i < 50; i++)
            if ((await guard.TryConsumeAsync(null, $"device:{Guid.NewGuid()}", clientIp: "203.0.113.7")).Allowed)
                allowed++;

        Assert.Equal(10, allowed);
        // Denied-by-IP requests must not create per-device rows (DB growth).
        Assert.Equal(10, anon.RowsCreated);
    }

    [Fact]
    public async Task Anonymous_SameDevice_IsCappedByDeviceAllowance()
    {
        var guard = new AiUsageGuard(new AiRateLimitService(), new InMemoryAnonymousUsage(), Config(anonDaily: 3, ipDaily: 10));
        var results = new List<bool>();
        for (var i = 0; i < 5; i++)
            results.Add((await guard.TryConsumeAsync(null, "device:abc", clientIp: "203.0.113.7")).Allowed);
        Assert.Equal([true, true, true, false, false], results);
    }

    [Fact]
    public async Task Anonymous_DifferentIps_HaveSeparateCaps()
    {
        var guard = new AiUsageGuard(new AiRateLimitService(), new InMemoryAnonymousUsage(), Config(anonDaily: 100, ipDaily: 2));
        Assert.True((await guard.TryConsumeAsync(null, "device:a", clientIp: "198.51.100.1")).Allowed);
        Assert.True((await guard.TryConsumeAsync(null, "device:a", clientIp: "198.51.100.1")).Allowed);
        Assert.False((await guard.TryConsumeAsync(null, "device:a", clientIp: "198.51.100.1")).Allowed);
        Assert.True((await guard.TryConsumeAsync(null, "device:b", clientIp: "198.51.100.2")).Allowed);
    }

    [Fact]
    public async Task GlobalBudget_StopsEveryone()
    {
        var guard = new AiUsageGuard(new AiRateLimitService(), new InMemoryAnonymousUsage(), Config(anonDaily: 100, ipDaily: 100, global: 3),
            new FakeUsers(credits: 50));
        Assert.True((await guard.TryConsumeAsync(null, "device:a", clientIp: "198.51.100.1")).Allowed);
        Assert.True((await guard.TryConsumeAsync(null, "device:b", clientIp: "198.51.100.2")).Allowed);
        Assert.True((await guard.TryConsumeAsync(Student(1), "device:c")).Allowed);
        Assert.False((await guard.TryConsumeAsync(Student(1), "device:c")).Allowed);
        Assert.False((await guard.TryConsumeAsync(null, "device:d", clientIp: "198.51.100.3")).Allowed);
    }

    [Fact]
    public async Task Student_WithoutCredits_IsRefused()
    {
        var guard = new AiUsageGuard(new AiRateLimitService(), new InMemoryAnonymousUsage(), Config(), new FakeUsers(credits: 0));
        var d = await guard.TryConsumeAsync(Student(7), "device:x");
        Assert.False(d.Allowed);
        Assert.Equal("Nemáš dostatek kreditů.", d.DeniedMessage);
    }

    [Fact]
    public async Task Student_WithCredits_IsAllowed_AndDoesNotUseFreeAllowance()
    {
        var anon = new InMemoryAnonymousUsage();
        var guard = new AiUsageGuard(new AiRateLimitService(), anon, Config(), new FakeUsers(credits: 5));
        Assert.True((await guard.TryConsumeAsync(Student(7), "device:x", clientIp: "203.0.113.9")).Allowed);
        Assert.Equal(0, anon.RowsCreated);
    }
}
