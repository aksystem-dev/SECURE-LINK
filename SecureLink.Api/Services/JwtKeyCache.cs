using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace SecureLink.Api.Services
{
    public class JwtKeyCache
    {
        private readonly IJwtKeyDataAccess _dataAccess;
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly string CacheKey = "JwtKeys";

        public JwtKeyCache(IJwtKeyDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        private async Task VerifyCacheAsync()
        {
            if (!(_cache.Get(CacheKey) is List<JwtKey> cachedKeys) || !cachedKeys.Any())
            {
                await LoadKeysFromDatabaseAsync();
            }
        }

        private async Task LoadKeysFromDatabaseAsync()
        {
            var activeKeys = await _dataAccess.GetActiveKeysAsync();
            _cache.Set(CacheKey, activeKeys, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) });
        }

        public async Task<List<JwtKey>> GetAllKeysAsync()
        {
            if (!(_cache.Get(CacheKey) is List<JwtKey> keys) || !keys.Any())
            {
                await LoadKeysFromDatabaseAsync();
                keys = _cache.Get(CacheKey) as List<JwtKey>;
            }
            return keys ?? new List<JwtKey>();
        }

        public async Task<JwtKey> GetPrimaryKeyAsync()
        {
            await VerifyCacheAsync();
            var keys = await GetAllKeysAsync();
            return keys.FirstOrDefault(k => k.KeyType == KeyTypeEnum.Primary && k.IsActive);
        }

        public async Task<JwtKey> GetSecondaryKeyAsync()
        {
            await VerifyCacheAsync();
            var keys = await GetAllKeysAsync();
            return keys.FirstOrDefault(k => k.KeyType == KeyTypeEnum.Secondary && k.IsActive);
        }

        public async Task<List<JwtKey>> GetValidSigningKeysAsync()
        {
            await VerifyCacheAsync();
            var keys = await GetAllKeysAsync();
            return keys.Where(k => k.IsActive && k.ExpiresAt > DateTime.Now).ToList();
        }

        public void UpdateCache(List<JwtKey> keys)
        {
            _cache.Set(CacheKey, keys, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) });
        }
    }
}
