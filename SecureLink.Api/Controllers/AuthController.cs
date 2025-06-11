using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using SecureLink.Api.Services;
using SecureLink.Shared.Models;
using LoginRequest = SecureLink.Shared.Models.LoginRequest;

[ApiController]
[Route("auth")]
public class AuthenthController : ControllerBase
{
    private readonly JwtKeyManager _keyManager;
    private readonly UserManager _userManager;
    private readonly LogManager _logManager;
    private readonly NonceCache _nonceCache;

    private const int MaxLoginAttempts = 5;
    private const int MaxIPFailedConnections = 10;
    private const int MaxTempIPBlocations = 3;
    private static readonly TimeSpan LoginAttemptWindow = TimeSpan.FromHours(24);

    public AuthenthController(JwtKeyManager keyManager,
                              UserManager userManager,
                              LogManager logManager,
                              NonceCache nonceCache)
    {
        _keyManager = keyManager;
        _userManager = userManager;
        _logManager = logManager;
        _nonceCache = nonceCache;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        Log.Information("Login attempt started. Username: {Username}, IP: {IPAddress}, Client IP: {ClientIPAddress}", request.Username, ipAddress, request.ClientIPAddress);

        try
        {
            var (userAttempts, ipAttempts, ipBlocked) = await _userManager.GetRecentLoginAttemptsAsync(request.Username, ipAddress, LoginAttemptWindow);

            if (ipBlocked || await HandleExcessiveIpAttemptsAsync(ipAddress, ipAttempts))
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, request.ClientIPAddress, success: false);
                return Forbid("IP address is blocked.");
            }

            var (userAttemptsClient, ipAttemptsClient, ipBlockedClient) = await _userManager.GetRecentClientLoginAttemptsAsync(request.Username, request.ClientIPAddress, LoginAttemptWindow);

            if (ipBlockedClient || await HandleExcessiveIpAttemptsAsync(request.ClientIPAddress, ipAttemptsClient))
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, request.ClientIPAddress, success: false);
                return Forbid("IP address is blocked.");
            }


            if (await HandleExcessiveUserAttemptsAsync(userAttempts, request.Username, request.ClientIPAddress))
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, request.ClientIPAddress, success: false);
                return Forbid("This account is blocked due to too many failed login attempts.");
            }

            var user = await _userManager.GetUserByUsernameAsync(request.Username);
            if (!ValidateUserCredentials(user, request.Password, ipAddress, request.ClientIPAddress, request.Username))
            {
                return Unauthorized("Invalid username or password.");
            }

            if (string.IsNullOrEmpty(request.Nonce))
            {
                return BadRequest(new { Message = "Nonce is required." });
            }

            await LogLoginAttemptAsync(request.Username, ipAddress, request.ClientIPAddress, success: true);
            var token = await GenerateJwtTokenAsync(user, request.Nonce);

            var activeKey = await _keyManager.GetPrimaryKeyAsync();
            var expiration = activeKey?.ExpiresAt ?? DateTime.Now.AddHours(1);

            _nonceCache.StoreNonce(ipAddress, request.Nonce, expiration);

            await LogSuccessfulLoginAsync(user, ipAddress, request.Nonce);

            return Ok(new { token, expiresAt = expiration });
        }
        catch (Exception ex)
        {
            Log.Error("Error during login process. Username: {Username}, IP: {IPAddress}, Client IP: {ClientIP}, Exception: {Exception}", request.Username, ipAddress, request.ClientIPAddress, ex);
            return StatusCode(500, "An unexpected error occurred during login. Please try again later.");
        }
    }

    private async Task<string> GenerateJwtTokenAsync(User user, string nonce)
    {
        var activeKey = await _keyManager.GetPrimaryKeyAsync();
        if (activeKey == null)
        {
            throw new InvalidOperationException("No active JWT key available.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(activeKey.KeyValue);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.UserType.ToString()),
                new Claim("nonce", nonce)
            }),
            Expires = activeKey.ExpiresAt,
            Issuer = "AK System",
            Audience = "AK System",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    private async Task<bool> HandleExcessiveIpAttemptsAsync(string ipAddress, int failedAttempts)
    {
        var tempBlockCount = await _userManager.GetTemporaryBlockCountAsync(ipAddress);
        if (tempBlockCount >= MaxTempIPBlocations)
        {
            await _userManager.BlockIPAsync(ipAddress); // Trvalá blokace
            Log.Warning("IP address {IPAddress} permanently blocked after exceeding temporary block limit.", ipAddress);
            return true;
        }

        if (failedAttempts >= MaxIPFailedConnections)
        {
            await _userManager.BlockIPAsync(ipAddress, DateTime.Now.AddHours(1));
            Log.Warning("IP address {IPAddress} temporarily blocked due to excessive failed attempts.", ipAddress);
            return true;
        }

        return false;
    }

    private async Task<bool> HandleExcessiveUserAttemptsAsync(int userAttempts, string username, string ipAddress)
    {
        if (userAttempts >= MaxLoginAttempts)
        {
            await _userManager.BlockUserAsync(username);
            Log.Warning("User blocked due to too many failed attempts. Username: {Username}, IP: {IPAddress}", username, ipAddress);
            return true;
        }
        return false;
    }

    private async Task LogLoginAttemptAsync(string username, string ipAddress, string clientIPAddress, bool success)
    {
        await _userManager.LogLoginAttemptAsync(username, ipAddress, clientIPAddress, success);
        if (!success)
        {
            Log.Warning("Invalid login attempt. Username: {Username}, IP: {IPAddress}, ClientIPAddress: {ClientIPAddress}", username, ipAddress, clientIPAddress);
        }
    }

    private async Task LogSuccessfulLoginAsync(User user, string ipAddress, string nonce)
    {
        await _logManager.LogKeyAssigmentAsync(user.Username, ipAddress, (await _keyManager.GetPrimaryKeyAsync()).KeyValue, nonce);
        Log.Information("Key assignment logged. Username: {Username}, IP: {IPAddress}", user.Username, ipAddress);
        await _userManager.UpdateLastLoginAsync(user.Id);
        Log.Information("Last login updated. Username: {Username}, IP: {IPAddress}", user.Username, ipAddress);
    }

    private bool ValidateUserCredentials(User user, string password, string ipAddress, string clientLoginAddress, string username)
    {
        if (user == null || !_userManager.VerifyPassword(password, user.PasswordHash))
        {
            Log.Warning("Invalid login credentials. Username: {Username}, IP: {IPAddress}, Client IP: {ClientLoginAddress}", username, ipAddress, clientLoginAddress);
            _ = LogLoginAttemptAsync(username, ipAddress, clientLoginAddress, success: false);
            return false;
        }
        return true;
    }
}
