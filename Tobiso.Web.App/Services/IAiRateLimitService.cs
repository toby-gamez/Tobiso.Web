namespace Tobiso.Web.App.Services
{
    public interface IAiRateLimitService
    {
        bool TryConsume(string key, int limit);
        int GetRemaining(string key, int limit);
    }
}
