using SecureLink.Shared.Models;
using System;
using System.Threading.Tasks;

namespace SecureLink.Api.Data.Interfaces
{
    public interface IUserDataAccess
    {
        Task<User> GetUserByUsernameAsync(string username);
        Task UpdateLastLoginAsync(int userId);
        Task LogLoginAttemptAsync(string username, string ipAddress, string clientIPAddress, bool success);
        Task BlockUserAsync(string username);
        Task BlockIPAsync(string ipAddress, DateTime? blockedUntil = null);
        Task<(int UserAttempts, int IpAttempts, bool IpBlocked)> GetRecentLoginAttemptsAsync(string username, string ipAddress, TimeSpan timeSpan);
        Task<(int userAttemptsClient, int ipAttemptsClient, bool ipBlockedClient)> GetRecentClientLoginAttemptsAsync(string username, string ipAddress, TimeSpan timeSpan);
        Task<bool> IsUserBlockedAsync(string username);
        Task<DateTime?> GetBlockedUntilAsync(string ipAddress);
        Task<int> GetTemporaryBlockCountAsync(string ipAddress);
        Task<bool> CreateUserAsync(User user);
        Task<bool> TestUserCustomDatabaseConnectionAsync(string connectionString);
    }
}
