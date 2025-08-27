namespace Tobiso.Web.App.Services
{
    public interface IPreferenceService
    {
        Task<string?> GetPreferenceAsync(string key);
        Task SetPreferenceAsync(string key, string value);
        Task RemovePreferenceAsync(string key);
    }
}