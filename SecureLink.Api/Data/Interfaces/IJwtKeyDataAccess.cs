using SecureLink.Shared.Models;

namespace SecureLink.Api.Data.Interfaces
{
    public interface IJwtKeyDataAccess
    {
        Task<List<JwtKey>> GetActiveKeysAsync();
        Task InsertKeyAsync(JwtKey key);
        Task InsertKeysAsync(IEnumerable<JwtKey> keys);
        Task UpdateKeyAsync(JwtKey key);
        Task DeactivateExpiredKeysAsync();
        Task CleanUpOldKeysAsync();
    }
}
