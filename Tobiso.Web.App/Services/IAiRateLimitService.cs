namespace Tobiso.Web.App.Services
{
    public interface IAiRateLimitService
    {
        bool TryConsume(string key, int limit);
        int GetRemaining(string key, int limit);
        void AddBonusQuestions(string rateKey, int count, DateTime validUntil);
        int GetBonusTotal(string rateKey);

        // Lifetime (never-renewing) counters - used for the anonymous free allowance, which is
        // "N requests, ever," not "N requests per day" like TryConsume/GetRemaining above.
        bool TryConsumeOnce(string key, int limit);
        int GetRemainingOnce(string key, int limit);

        // Records a signed credit-grant as spent. Returns false if this exact signature was
        // already registered (a replay of the same signed payload), so a captured grant can't
        // be resubmitted to accumulate unlimited bonus quota before it expires.
        bool TryRegisterCreditGrant(string signature, DateTime validUntil);
    }
}
