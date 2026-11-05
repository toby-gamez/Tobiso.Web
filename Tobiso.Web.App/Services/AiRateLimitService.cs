using System.Collections.Concurrent;

namespace Tobiso.Web.App.Services
{
    public class AiRateLimitService : IAiRateLimitService
    {
        private readonly ConcurrentDictionary<string, (int Count, DateTime Date)> _store = new();
        private readonly ConcurrentDictionary<string, int> _onceStore = new();

        public bool TryConsume(string key, int limit)
        {
            var today = DateTime.UtcNow.Date;
            _store.AddOrUpdate(key, (1, today), (k, v) =>
            {
                if (v.Date < today) return (1, today);
                return (v.Count + 1, v.Date);
            });

            var val = _store[key];

            // Per-IP and per-user keys accumulate; drop the ones from earlier days now and then.
            if (_store.Count > 50_000)
                foreach (var kv in _store)
                    if (kv.Value.Date < today) _store.TryRemove(kv.Key, out _);

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

        public bool TryConsumeOnce(string key, int limit)
        {
            var count = _onceStore.AddOrUpdate(key, 1, (k, v) => v + 1);
            return count <= limit;
        }

        public int GetRemainingOnce(string key, int limit) =>
            _onceStore.TryGetValue(key, out var v) ? Math.Max(0, limit - v) : limit;
    }
}
