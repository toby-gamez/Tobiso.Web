using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Tobiso.Web.App.Services
{
    public class PreferenceService : IPreferenceService
    {
        private readonly ProtectedLocalStorage _localStorage;

        public PreferenceService(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
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
    }
}