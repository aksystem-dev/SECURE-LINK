using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace SecureLink.Api.Services
{
    public class JwtKeyManager
    {
        private readonly IJwtKeyDataAccess _dataAccess;
        private readonly JwtKeyCache _cache;

        public JwtKeyManager(IJwtKeyDataAccess dataAccess, JwtKeyCache cache)
        {
            _dataAccess = dataAccess;
            _cache = cache;
        }

        public async Task<JwtKey> GetPrimaryKeyAsync()
        {
            try
            {
                var now = DateTime.Now;
                var allKeys = await _cache.GetAllKeysAsync();
                var activeKeys = await DeactivateExpiredKeysAsync(allKeys);

                var primaryKey = activeKeys.FirstOrDefault(k => k.KeyType == KeyTypeEnum.Primary);
                if (primaryKey == null)
                {
                    Log.Warning("[JwtKeyManager - GetPrimaryKeyAsync] No primary key found. Rotating keys...");
                    await RotateKeysAsync();
                    primaryKey = await _cache.GetPrimaryKeyAsync();
                }

                if ((primaryKey?.ExpiresAt - now)?.TotalMinutes <= 30)
                {
                    var secondaryKey = activeKeys.FirstOrDefault(k => k.KeyType == KeyTypeEnum.Secondary);
                    if (secondaryKey != null)
                    {
                        Log.Information("[JwtKeyManager - GetPrimaryKeyAsync] Returning secondary key as primary key is expiring soon.");
                        return secondaryKey;
                    }
                }
                return primaryKey;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JwtKeyManager - GetPrimaryKeyAsync] Error retrieving primary key.");
                throw;
            }
        }

        public async Task<List<JwtKey>> GetValidSigningKeysAsync()
        {
            try
            {
                var keys = await _cache.GetValidSigningKeysAsync();
                Log.Information("[JwtKeyManager - GetValidSigningKeysAsync] Retrieved {Count} valid signing keys.", keys?.Count ?? 0);
                return keys;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JwtKeyManager - GetValidSigningKeysAsync] Error retrieving valid signing keys.");
                throw;
            }
        }

        private async Task<List<JwtKey>> DeactivateExpiredKeysAsync(List<JwtKey> keys)
        {
            try
            {
                var expiredKeys = keys.Where(key => key.ExpiresAt <= DateTime.Now || !key.IsActive).ToList();
                if (expiredKeys.Any())
                {
                    foreach (var expiredKey in expiredKeys)
                    {
                        expiredKey.IsActive = false;
                        await _dataAccess.UpdateKeyAsync(expiredKey);
                        Log.Information("[JwtKeyManager - DeactivateExpiredKeysAsync] Deactivated expired key with Id: {Id}", expiredKey.Id);
                    }
                    keys = keys.Except(expiredKeys).ToList();
                    _cache.UpdateCache(keys);
                    Log.Information("[JwtKeyManager - DeactivateExpiredKeysAsync] Cache updated after deactivating expired keys.");
                }
                return keys;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JwtKeyManager - DeactivateExpiredKeysAsync] Error deactivating expired keys.");
                throw;
            }
        }

        public async Task RotateKeysAsync()
        {
            try
            {
                var now = DateTime.Now;
                var activeKeys = await _cache.GetAllKeysAsync();
                var secondaryKey = activeKeys.FirstOrDefault(k => k.KeyType == KeyTypeEnum.Secondary);
                if (secondaryKey == null || (secondaryKey.ExpiresAt - now).TotalMinutes <= 30)
                {
                    Log.Information("[JwtKeyManager - RotateKeysAsync] No valid secondary key found or it is expiring soon. Generating new keys...");
                    await GenerateNewKeysAsync();
                    return;
                }

                secondaryKey.KeyType = KeyTypeEnum.Primary;
                await _dataAccess.UpdateKeyAsync(secondaryKey);
                Log.Information("[JwtKeyManager - RotateKeysAsync] Promoted secondary key (Id: {Id}) to primary.", secondaryKey.Id);

                var newKey = new JwtKey
                {
                    KeyValue = GenerateNewKey(),
                    ValidFrom = now,
                    ExpiresAt = now.AddHours(1).AddMinutes(30),
                    IsActive = true,
                    KeyType = KeyTypeEnum.Secondary
                };

                await _dataAccess.InsertKeyAsync(newKey);
                Log.Information("[JwtKeyManager - RotateKeysAsync] Inserted new secondary key.");

                await _dataAccess.CleanUpOldKeysAsync();
                Log.Information("[JwtKeyManager - RotateKeysAsync] Cleaned up old keys.");

                var refreshedKeys = await _dataAccess.GetActiveKeysAsync();
                _cache.UpdateCache(refreshedKeys);
                Log.Information("[JwtKeyManager - RotateKeysAsync] Cache updated after rotating keys.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JwtKeyManager - RotateKeysAsync] Error rotating keys.");
                throw;
            }
        }

        private async Task GenerateNewKeysAsync()
        {
            try
            {
                var now = DateTime.Now;

                var primaryKey = new JwtKey
                {
                    KeyValue = GenerateNewKey(),
                    ValidFrom = now,
                    ExpiresAt = now.AddHours(1),
                    IsActive = true,
                    KeyType = KeyTypeEnum.Primary
                };

                var secondaryKey = new JwtKey
                {
                    KeyValue = GenerateNewKey(),
                    ValidFrom = now,
                    ExpiresAt = now.AddHours(1).AddMinutes(30),
                    IsActive = true,
                    KeyType = KeyTypeEnum.Secondary
                };

                await _dataAccess.InsertKeysAsync(new List<JwtKey> { primaryKey, secondaryKey });
                Log.Information("[JwtKeyManager - GenerateNewKeysAsync] Inserted new primary and secondary keys.");

                await _dataAccess.CleanUpOldKeysAsync();
                Log.Information("[JwtKeyManager - GenerateNewKeysAsync] Cleaned up old keys.");

                var refreshedKeys = await _dataAccess.GetActiveKeysAsync();
                _cache.UpdateCache(refreshedKeys);
                Log.Information("[JwtKeyManager - GenerateNewKeysAsync] Cache updated with newly generated keys.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JwtKeyManager - GenerateNewKeysAsync] Error generating new keys.");
                throw;
            }
        }

        private string GenerateNewKey()
        {
            try
            {
                var keyBytes = new byte[32];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(keyBytes);
                }
                return Convert.ToBase64String(keyBytes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JwtKeyManager - GenerateNewKey] Error generating new key.");
                throw;
            }
        }
    }
}
