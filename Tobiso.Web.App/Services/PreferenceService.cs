using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.App.Services
{
    public class PreferenceService : IPreferenceService
    {
        private readonly ProtectedLocalStorage _localStorage;

        private readonly AuthenticationStateProvider _authState;
        private readonly IUserService _userService;

        public PreferenceService(ProtectedLocalStorage localStorage, AuthenticationStateProvider authState, IUserService userService)
        {
            _localStorage = localStorage;
            _authState = authState;
            _userService = userService;
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
                    var user = await _userService.GetByIdAsync(userId);
                    if (user?.PreferredGradeId is { } accountGrade) return accountGrade;
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
                    await _userService.SetPreferredGradeAsync(userId, gradeId);
            }
            catch
            {
                // browser value is already saved
            }
        }
    }
}
