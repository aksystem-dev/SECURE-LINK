using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecureLink.Api.Services;
using SecureLink.Shared.Models;
using SecureLink.Web.Services;
using System;
using System.Threading.Tasks;

namespace SecureLink.Api.Controllers
{
    [ApiController]
    [Route("api/securelink")]
    [Authorize]
    [NonceAuthorize]
    public class SecureLinkController : ControllerBase
    {
        private readonly LinkService _linkService;
        private readonly ILogger<SecureLinkController> _logger;
        private readonly IConfiguration _configuration;

        public SecureLinkController(LinkService linkService, ILogger<SecureLinkController> logger, IConfiguration configuration)
        {
            _linkService = linkService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("validate")]
        [UserTypeAuthorize("Admin", "Writer", "Reader")]
        public async Task<IActionResult> Validate([FromBody] ValidateActionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ClientIPAddress))
                {
                    _logger.LogDebug("Chybí IP adresa klienta v požadavku.");
                    return BadRequest("Client IP address is required.");
                }

                _logger.LogInformation("Začínám validaci secure linku s klíčem {Key} od klienta {ClientIP}.", request.EncryptedKey, request.ClientIPAddress);
                var result = await _linkService.ValidateLinkAsync(request.EncryptedKey, request.ClientIPAddress);

                if (!result.IsValid)
                {
                    _logger.LogInformation("Validace selhala pro klíč {Key} od klienta {ClientIP}. Důvod: {Reason}", request.EncryptedKey, request.ClientIPAddress, result.Message);
                    return BadRequest(result);
                }

                _logger.LogDebug("Validace úspěšná pro klíč {Key} od klienta {ClientIP}.", request.EncryptedKey, request.ClientIPAddress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SecureLinkController - Validate] Chyba při validaci akce pro klíč {Key}.", request.EncryptedKey);
                return BadRequest("Došlo k neočekávané chybě při získávání akce.");
            }
        }

        [HttpPost("confirm")]
        [UserTypeAuthorize("Admin", "Writer", "Reader")]
        public async Task<ConfirmActionResult> Confirm([FromBody] ConfirmActionRequest request)
        {
            try
            {
                _logger.LogInformation("Potvrzuji akci {Action} pro klíč {Key} od klienta {ClientIP} s komentářem: {Comment}",
                    request.Action, request.Key, request.ClientIP, request.Comment);

                var result = await _linkService.ConfirmActionAsync(request.Key, request.Action, request.ClientIP, request.Comment);

                if (result.Success)
                {
                    _logger.LogDebug("Akce {Action} úspěšně potvrzena pro klíč {Key} od klienta {ClientIP}.",
                        request.Action, request.Key, request.ClientIP);
                }
                else
                {
                    _logger.LogInformation("Potvrzení akce pro klíč {Key} od klienta {ClientIP} selhalo. Důvod: {Message}",
                        request.Key, request.ClientIP, result.Message);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SecureLinkController - Confirm] Chyba při potvrzování akce pro klíč {Key}.", request.Key);
                return new ConfirmActionResult { Success = false, Message = "Došlo k neočekávané chybě při potvrzování akce." };
            }
        }

        [HttpPost("create")]
        [UserTypeAuthorize("Admin", "Writer")]
        public async Task<IActionResult> CreateSecureLink([FromBody] SecureLinkInsertModel model)
        {
            try
            {
                var username = User?.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("[CreateSecureLink] Uživatelské jméno nebylo nalezeno v kontextu.");
                    return Unauthorized("Uživatelské jméno nebylo nalezeno.");
                }

                _logger.LogInformation("[CreateSecureLink] Vytvářím secure link s daty: {@Model}, {Username}", model, username);

                string secretKey = await _linkService.CreateSecureLinkAsync(model, username);

                _logger.LogInformation("Secure link vytvořen, klíč: {SecretKey}", secretKey);

                string baseUrl = _configuration["BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogWarning("BaseUrl není nakonfigurován.");
                    return StatusCode(500, "BaseUrl není nakonfigurován.");
                }

                string fullLink = $"{baseUrl}?key={secretKey}";
                _logger.LogInformation("Generován kompletní odkaz: {FullLink}", fullLink);
                return Ok(new { Link = fullLink });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při vytváření secure linku.");
                return StatusCode(500, "Chyba při vytváření secure linku.");
            }
        }
    }
}
