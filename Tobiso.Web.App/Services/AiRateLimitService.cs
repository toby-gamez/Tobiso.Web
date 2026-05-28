using System.Collections.Concurrent;

namespace Tobiso.Web.App.Services
{
    public class AiRateLimitService : IAiRateLimitService
    {
        private readonly ConcurrentDictionary<string, (int Count, DateTime Date)> _store = new();
        private readonly ConcurrentDictionary<string, List<(int Count, DateTime ValidUntil)>> _bonusStore = new();
        private readonly object _bonusLock = new();

        public bool TryConsume(string key, int limit)
        {
            var today = DateTime.UtcNow.Date;
            _store.AddOrUpdate(key, (1, today), (k, v) =>
            {
                if (v.Date < today) return (1, today);
                return (v.Count + 1, v.Date);
            });

            var val = _store[key];
            return val.Count <= limit;
        }

        public int GetRemaining(string key, int limit)
        {
            var today = DateTime.UtcNow.Date;
            if (_store.TryGetValue(key, out var v))
            {
                if (v.Date < today) return limit;
                return Math.Max(0, limit - v.Count);
            }
            return limit;
        }

        public void AddBonusQuestions(string rateKey, int count, DateTime validUntil)
        {
            lock (_bonusLock)
            {
                var list = _bonusStore.GetOrAdd(rateKey, _ => new List<(int, DateTime)>());
                list.Add((count, validUntil));
            }
        }

        public int GetBonusTotal(string rateKey)
        {
            if (!_bonusStore.TryGetValue(rateKey, out var list)) return 0;
            var now = DateTime.UtcNow;
            lock (_bonusLock)
            {
                return list.Where(e => e.ValidUntil > now).Sum(e => e.Count);
            }
        }
    }
}
