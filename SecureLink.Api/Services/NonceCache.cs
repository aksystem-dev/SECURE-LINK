using Serilog;
using System.Collections.Concurrent;
using System.Runtime.Caching;

namespace SecureLink.Api.Services
{
    public class NonceCache
    {
        private readonly MemoryCache _cache = MemoryCache.Default;
        private const string CacheKeyPrefix = "Nonce_";

        public void StoreNonce(string ipAddress, string nonce, DateTimeOffset expiration)
        {
            var key = $"{CacheKeyPrefix}{ipAddress}";

            var existingNonces = _cache.Get(key) as List<string> ?? new List<string>();

            if (!existingNonces.Contains(nonce))
            {
                existingNonces.Add(nonce);
            }
            else
            {
                Log.Warning("[NonceCache] - Nonce {Nonce} already exists for IP {IPAddress}, skipping add.", nonce, ipAddress);
                return;
            }

            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = expiration,
                RemovedCallback = args => Log.Information($"Nonce {args.CacheItem.Key} expirovala a byla odstraněna.")
            };

            _cache.Set(key, existingNonces, policy);

            Log.Information("[NonceCache] - Nonce {Nonce} stored for IP {IPAddress} with expiration {Expiration}",
                nonce, ipAddress, expiration);
        }

        public bool ValidateNonce(string ipAddress, string nonce)
        {
            RemoveExpiredNonces();

            var key = $"{CacheKeyPrefix}{ipAddress}";

            if (_cache.Get(key) is List<string> storedNonces && storedNonces.Contains(nonce))
            {
                Log.Information("[NonceCache] - Valid nonce {Nonce} found for IP {IPAddress}.", nonce, ipAddress);
                return true;
            }

            Log.Warning("[NonceCache] - Nonce validation failed for IP {IPAddress}, nonce {Nonce} not found.", ipAddress, nonce);
            return false;
        }

        private void RemoveExpiredNonces()
        {
            List<string> expiredKeys = new();

            foreach (var item in _cache)
            {
                if (item.Key.StartsWith(CacheKeyPrefix) && _cache.Get(item.Key) == null)
                {
                    expiredKeys.Add(item.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
                Log.Information("[NonceCache] - Expired nonce cache removed: {CacheKey}", key);
            }
        }
    }
}
