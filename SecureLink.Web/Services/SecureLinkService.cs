using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecureLink.Shared.Models;

namespace SecureLink.Web.Services
{
    public class SecureLinkService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SecureLinkService> _logger;
        private readonly AuthenticationService _authService;

        public SecureLinkService(HttpClient httpClient, ILogger<SecureLinkService> logger, AuthenticationService authService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authService = authService;
        }

        public async Task<SecureLinkData> ValidateLinkAsync(string encryptedKey, string clientIPAddress)
        {
            try
            {
                _logger.LogInformation("Validating secure link key...");

                await _authService.EnsureAuthenticatedAsync(clientIPAddress);

                var request = new ValidateActionRequest
                {
                    EncryptedKey = encryptedKey,
                    ClientIPAddress = clientIPAddress
                };

                var response = await _httpClient.PostAsJsonAsync("api/securelink/validate", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<SecureLinkData>();
                    return errorResult ?? new SecureLinkData { IsValid = false, Message = "Odkaz není platný nebo vypršel." };
                }

                var result = await response.Content.ReadFromJsonAsync<SecureLinkData>();

                return result ?? new SecureLinkData { IsValid = false, Message = "Neplatná odpověď od serveru." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating secure link.");
                return new SecureLinkData { IsValid = false, Message = "Došlo k chybě při ověřování odkazu." };
            }
        }


        public async Task<ConfirmActionResult> ConfirmActionAsync(string encryptedKey, ActionType action, string clientIPAddress, string comment = "")
        {
            try
            {
                _logger.LogInformation("Confirming action {ActionType} for key {Key}", action, encryptedKey);

                await _authService.EnsureAuthenticatedAsync(clientIPAddress);

                var request = new ConfirmActionRequest
                {
                    Key = encryptedKey,
                    Action = action,
                    ClientIP = clientIPAddress,
                    Comment = comment
                };

                var response = await _httpClient.PostAsJsonAsync("api/securelink/Confirm", request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Action confirmation failed. Status: {StatusCode}", response.StatusCode);
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    return new ConfirmActionResult { Success = false, Message = errorMsg };
                }

                var result = await response.Content.ReadFromJsonAsync<ConfirmActionResult>();
                _logger.LogInformation("Action {ActionType} confirmed successfully for key {Key}", action, encryptedKey);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming action for key {Key}", encryptedKey);
                return new ConfirmActionResult { Success = false, Message = "Došlo k chybě při potvrzování akce." };
            }
        }

    }
}
