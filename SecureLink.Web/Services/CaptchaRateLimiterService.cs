using Microsoft.Extensions.Caching.Memory;

namespace SecureLink.Web.Services
{
    public class CaptchaRateLimiterService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
        private readonly int _maxAttempts = 30;
        private readonly ILogger<CaptchaRateLimiterService> _logger;

        public CaptchaRateLimiterService(IMemoryCache cache, ILogger<CaptchaRateLimiterService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool ShouldRequireCaptcha(string ip)
        {
            if (_cache.TryGetValue(GetCacheKey(ip), out int attempts))
            {
                _logger.LogInformation("ShouldRequireCaptcha - number of attempts: {attempts}, IP adress: {ip}.", attempts, ip);
                return attempts >= _maxAttempts;
            }
            return false;
        }

        public void RecordAttempt(string ip)
        {
            var key = GetCacheKey(ip);
            if (_cache.TryGetValue(key, out int attempts))
            {
                attempts++;
                _logger.LogInformation("RecordAttempt - adding new attempt (total:{attempts}) for IP adress: {ip}.", attempts, ip);
                _cache.Set(key, attempts, _timeWindow);
            }
            else
            {
                _logger.LogInformation("RecordAttempt - adding first attempt for IP adress: {ip}.", ip);
                _cache.Set(key, 1, _timeWindow);
            }
        }

        public void ResetAttempts(string ip)
        {
            _logger.LogInformation("ResetAttempts - resetting attempts for {ip}.", ip);
            var key = GetCacheKey(ip);
            _cache.Set(key, 0, _timeWindow);
        }

        private string GetCacheKey(string ip) => $"attempt_{ip}";
    }
}
