using Microsoft.Extensions.Caching.Memory;
using SecureLink.Shared.Models;
using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Serilog;

namespace SecureLink.Web.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly AuthConfigService _authConfigService;

        private static readonly string TokenCacheKey = "auth_token";
        private static readonly string ExpirationCacheKey = "auth_token_expiration";
        private static readonly TimeSpan TokenRefreshThreshold = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan TokenCacheDuration = TimeSpan.FromMinutes(30);

        public AuthenticationService(HttpClient httpClient, ILogger<AuthenticationService> logger, IMemoryCache memoryCache, AuthConfigService authConfigService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _memoryCache = memoryCache;
            _authConfigService = authConfigService;
        }

        public async Task EnsureAuthenticatedAsync(string clientIPAddress)
        {
            _logger.LogInformation("Checking authentication...");

            if (_memoryCache.TryGetValue(TokenCacheKey, out string token) &&
                _memoryCache.TryGetValue(ExpirationCacheKey, out DateTime tokenExpiration) &&
                DateTime.Now < tokenExpiration.Subtract(TokenRefreshThreshold))
            {
                _logger.LogInformation("Existing token is still valid.");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return;
            }

            _logger.LogInformation("Token missing or expired. Refreshing...");
            await LoginAsync(clientIPAddress);
        }

        private async Task<bool> LoginAsync(string clientIPAddress)
        {
            try
            {
                string nonce = Guid.NewGuid().ToString();

                var response = await _httpClient.PostAsJsonAsync("auth/login", new
                {
                    Username = _authConfigService.Username,
                    Password = _authConfigService.Password,
                    Nonce = nonce,
                    ClientIPAddress = clientIPAddress
                });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[AuthenticationService - LoginAsync] Authentication failed. Status: {StatusCode}", response.StatusCode);
                    return false;
                }

                var authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
                {
                    _logger.LogWarning("[AuthenticationService - LoginAsync] Received invalid authentication response.");
                    return false;
                }

                _logger.LogInformation("[AuthenticationService - LoginAsync] Authentication successful.");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

                // Uložíme token a expiraci do cache
                _memoryCache.Set(TokenCacheKey, authResponse.Token, TokenCacheDuration);
                _memoryCache.Set(ExpirationCacheKey, authResponse.ExpiresAt.ToLocalTime(), TokenCacheDuration);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthenticationService - LoginAsync] Error during authentication.");
                return false;
            }
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
