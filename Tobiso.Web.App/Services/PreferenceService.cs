using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Services
{
    public class PreferenceService : IPreferenceService
    {
        private readonly ProtectedLocalStorage _localStorage;

        private readonly AuthenticationStateProvider _authState;
        // Own short-lived DbContext per call: NavMenu (layout) and the routed page both read the
        // preferred grade at the same time, and the circuit's scoped DbContext can't be shared
        // by concurrent operations.
        private readonly IDbContextFactory<TobisoDbContext> _dbFactory;

        public PreferenceService(ProtectedLocalStorage localStorage, AuthenticationStateProvider authState, IDbContextFactory<TobisoDbContext> dbFactory)
        {
            _localStorage = localStorage;
            _authState = authState;
            _dbFactory = dbFactory;
        }

        private async Task<int?> GetUserIdAsync()
        {
            var auth = await _authState.GetAuthenticationStateAsync();
            return int.TryParse(auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
        }

        public async Task<string?> GetPreferenceAsync(string key)
        {
            try
            {
                var result = await _localStorage.GetAsync<string>(key);
                return result.Success ? result.Value : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task SetPreferenceAsync(string key, string value)
        {
            await _localStorage.SetAsync(key, value);
        }

        public async Task RemovePreferenceAsync(string key)
        {
            await _localStorage.DeleteAsync(key);
        }

        public async Task<int?> GetPreferredGradeIdAsync()
        {
            try
            {
                if (await GetUserIdAsync() is { } userId)
                {
                    await using var db = _dbFactory.CreateDbContext();
                    var accountGradeId = await db.Users.AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => u.PreferredGradeId)
                        .FirstOrDefaultAsync();
                    if (accountGradeId is { } accountGrade) return accountGrade;
                }
            }
            catch
            {
                // fall back to the browser value
            }

            var val = await GetPreferenceAsync("preferredGradeId");
            if (int.TryParse(val, out var g)) return g;
            return null;
        }

        public async Task SetPreferredGradeIdAsync(int gradeId)
        {
            await SetPreferenceAsync("preferredGradeId", gradeId.ToString());
            try
            {
                if (await GetUserIdAsync() is { } userId)
                {
                    await using var db = _dbFactory.CreateDbContext();
                    await db.Users.Where(u => u.Id == userId)
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.PreferredGradeId, (int?)gradeId));
                }
            }
            catch
            {
                // browser value is already saved
            }
        }
    }
}
