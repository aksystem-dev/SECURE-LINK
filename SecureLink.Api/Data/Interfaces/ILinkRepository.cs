using SecureLink.Shared.Models;

namespace SecureLink.Api.Data.Interfaces
{
    public interface ILinkRepository
    {
        Task<SecureLinkData?> GetLinkDataAsync(string encryptedKey, string clientIPAddress);
        Task<ActionOption?> GetActionOptionAsync(string encryptedKey, ActionType action);
        Task LogSecureLinkRequestAsync(string encryptedKey, string requestType, string clientIPAddress, bool isSuccess, string message);
        Task<bool> IsIpBlockedAsync(string clientIPAddress);
        Task RegisterFailedAttemptAsync(string clientIPAddress, string message);
        Task ResetFailedAttemptsAsync(string clientIPAddress);
        Task<int> InsertSecureLinkSettingsAsync(SecureLinkSettings settings);
        Task InsertActionOptionsAsync(IEnumerable<ActionOption> options);
        Task<bool> MarkAsProcessedAsync(string encryptedKey);
        Task ConfirmActionInPohodaAsync(string sql, string connectionName, object parameters = null);
        Task<SecureLinkSettings?> GetLinkSettingsAsync(string encryptedKey);
    }


}
