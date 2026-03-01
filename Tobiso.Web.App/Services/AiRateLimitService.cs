using System.Collections.Concurrent;

namespace Tobiso.Web.App.Services
{
    public class AiRateLimitService : IAiRateLimitService
    {
        private readonly ConcurrentDictionary<string, (int Count, DateTime Date)> _store = new();

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
    }
}
