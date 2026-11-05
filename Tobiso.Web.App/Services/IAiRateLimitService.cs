namespace Tobiso.Web.App.Services
{
    public interface IAiRateLimitService
    {
        bool TryConsume(string key, int limit);
        int GetRemaining(string key, int limit);

        // Lifetime (never-renewing) counters - used for the anonymous free allowance, which is
        // "N requests, ever," not "N requests per day" like TryConsume/GetRemaining above.
        bool TryConsumeOnce(string key, int limit);
        int GetRemainingOnce(string key, int limit);

        // Purchased bonus quota and signed-grant replay protection live in the database: IAiBonusService.
    }
}
