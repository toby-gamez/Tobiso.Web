namespace Tobiso.Web.App.Services
{
    public interface IAiRateLimitService
    {
        bool TryConsume(string key, int limit);
        int GetRemaining(string key, int limit);
        void AddBonusQuestions(string rateKey, int count, DateTime validUntil);
        int GetBonusTotal(string rateKey);
    }
}
