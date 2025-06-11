using Microsoft.Extensions.Logging;
using SecureLink.Shared.Models;
using SecureLink.Api.Data.Interfaces;
using System;
using System.Threading.Tasks;
using Serilog;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using SecureLink.Api.Services;
using Microsoft.IdentityModel.Tokens;

namespace SecureLink.Web.Services
{
    public class LinkService
    {
        private readonly ILinkRepository _linkRepository;
        private readonly ILogger<LinkService> _logger;
        private readonly UserManager _userManager;
        private readonly IConfiguration _configuration;

        public LinkService(
      ILinkRepository linkRepository,
      ILogger<LinkService> logger,
      UserManager userManager,
      IConfiguration configuration)
        {
            _linkRepository = linkRepository;
            _logger = logger;
            _userManager = userManager;
            _configuration = configuration;
        }



        public async Task<SecureLinkData> ValidateLinkAsync(string encryptedKey, string clientIPAddress)
        {
            try
            {
                _logger.LogInformation("[LinkService - ValidateLinkAsync] Validace klíče: {EncryptedKey} pro IP: {ClientIPAddress}", encryptedKey, clientIPAddress);

                if (await _linkRepository.IsIpBlockedAsync(clientIPAddress))
                {
                    _logger.LogWarning("[LinkService - ValidateLinkAsync] - IP {ClientIPAddress} je blokována", clientIPAddress);
                    return new SecureLinkData { IsValid = false, Message = "Vaše IP byla dočasně zablokována kvůli opakovaným neúspěšným pokusům." };
                }

                var response = await _linkRepository.GetLinkDataAsync(encryptedKey, clientIPAddress);
                bool isSuccess = response != null && response.IsValid;

                if (!isSuccess)
                {
                    await _linkRepository.RegisterFailedAttemptAsync(clientIPAddress, "Neplatný klíč nebo vypršel časový limit.");
                }
                else
                {
                    await _linkRepository.ResetFailedAttemptsAsync(clientIPAddress);
                }

                return response ?? new SecureLinkData { IsValid = false, Message = "Neplatný klíč nebo IP adresa." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LinkService - ValidateLinkAsync] - Chyba validace secureLink odkazu");
                return new SecureLinkData { IsValid = false, Message = "Došlo k chybě při ověřování odkazu." };
            }
        }

        public async Task<ConfirmActionResult> ConfirmActionAsync(string encryptedKey, ActionType action, string clientIPAddress, string? comment)
        {
            try
            {
                _logger.LogInformation("[LinkService - ConfirmActionAsync] - Zahájení zpracování akce: {Action} pro klíč: {EncryptedKey} z IP: {ClientIPAddress}",
                    action, encryptedKey, clientIPAddress);

                var settings = await _linkRepository.GetLinkSettingsAsync(encryptedKey);

                if (settings is null)
                    return LogAndFail("Nastavení odkazu (SecureLinkSettings) nenalezeno", encryptedKey, null, "Odkaz nebyl nalezen nebo expiroval.");

                if (settings.Processed)
                    return LogAndFail("Akce již byla zpracována", encryptedKey, settings.DatabaseName, "Tato akce již byla zpracována.");

                if (string.IsNullOrWhiteSpace(settings.DatabaseName))
                    return LogAndFail("Není nastavena žádná databáze", encryptedKey, settings.DatabaseName, "Databáze nebyla nastavena pro tento odkaz.");

                if (string.IsNullOrWhiteSpace(_configuration.GetConnectionString(settings.DatabaseName)))
                    return LogAndFail("Databáze není nalezena v konfiguraci", encryptedKey, settings.DatabaseName,
                        $"Databáze '{settings.DatabaseName}' není definována v konfiguraci.");

                ConfirmActionResult LogAndFail(string logMessage, string key, string? dbName, string userMessage)
                {
                    _logger.LogWarning("[LinkService - ConfirmActionAsync] - {LogMessage}. Klíč: {Key}, Databáze: {DatabaseName}", logMessage, key, dbName ?? "(není určeno)");
                    return new ConfirmActionResult { Success = false, Message = userMessage };
                }

                var actionOption = await _linkRepository.GetActionOptionAsync(encryptedKey, action);
                if (actionOption == null)
                {
                    _logger.LogWarning("[LinkService - ConfirmActionAsync] - Akce nebyla nalezena pro klíč: {EncryptedKey}, Databáze: {DatabaseName}", encryptedKey, settings.DatabaseName);
                    await _linkRepository.LogSecureLinkRequestAsync(encryptedKey, "Confirm", clientIPAddress, false, "Akce nenalezena");
                    return new ConfirmActionResult { Success = false, Message = "Akce nenalezena." };
                }

                object? parameters = null;
                if (actionOption.SqlCommand.Contains("@comment", StringComparison.OrdinalIgnoreCase))
                {
                    parameters = new { comment = string.IsNullOrWhiteSpace(comment) ? (object)DBNull.Value : comment };

                    _logger.LogInformation("[LinkService - ConfirmActionAsync] - SQL obsahuje parametr @comment. Hodnota komentáře: {Comment}", comment ?? "NULL");
                }

                _logger.LogInformation("[LinkService - ConfirmActionAsync] - Spouštím SQL pro klíč: {EncryptedKey}, Databáze: {DatabaseName}. SQL: {SqlCommand}, Parametry: {Parameters}",
                    encryptedKey, settings.DatabaseName, actionOption.SqlCommand, parameters ?? "Žádné");

                await _linkRepository.ConfirmActionInPohodaAsync(actionOption.SqlCommand, settings.DatabaseName, parameters);

                _logger.LogInformation("[LinkService - ConfirmActionAsync] - Akce úspěšně provedena pro klíč: {EncryptedKey}, Databáze: {DatabaseName}", encryptedKey, settings.DatabaseName);

                bool markedProcessed = await _linkRepository.MarkAsProcessedAsync(encryptedKey);
                if (!markedProcessed)
                {
                    _logger.LogWarning("[LinkService - ConfirmActionAsync] - Záznam nebyl označen jako zpracovaný, přestože SQL bylo úspěšně spuštěno. Klíč: {EncryptedKey}",
                        encryptedKey);
                }

                await _linkRepository.LogSecureLinkRequestAsync(encryptedKey, "Confirm", clientIPAddress, true, "Akce potvrzena");

                _logger.LogInformation("[LinkService - ConfirmActionAsync] - Akce byla úspěšně potvrzena pro klíč: {EncryptedKey}, Databáze: {DatabaseName}",
                    encryptedKey, settings.DatabaseName);

                return new ConfirmActionResult { Success = true, Message = "Akce byla potvrzena." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LinkService - ConfirmActionAsync] - Chyba při potvrzování akce pro klíč: {EncryptedKey}", encryptedKey);
                return new ConfirmActionResult { Success = false, Message = "Došlo k chybě při potvrzování akce." };
            }
        }


        public async Task<string> CreateSecureLinkAsync(SecureLinkInsertModel model, string username)
        {
            try
            {
                string secretKey = GenerateSecretKey();
                Log.Information("[LinkService - CreateSecureLinkAsync] - Vygenerován tajný klíč: {SecretKey}", secretKey);

                var user = await _userManager.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    Log.Warning("[LinkService - CreateSecureLinkAsync] - Uživatel '{Username}' nebyl nalezen.", username);
                    throw new InvalidOperationException($"Uživatel '{username}' nebyl nalezen.");
                }

                Log.Information("[LinkService - CreateSecureLinkAsync] - Načten uživatel '{Username}', databáze: {DatabaseName}", user.Username, user.DatabaseName);

                var settings = new SecureLinkSettings
                {
                    EncryptedKey = secretKey,
                    Message = model.SecureLinkText,
                    ExpirationDate = model.ExpirationDate,
                    ShowCommentBox = model.ShowCommentBox,
                    Processed = false,
                    DatabaseName = user.DatabaseName
                };

                int settingsId = await _linkRepository.InsertSecureLinkSettingsAsync(settings);
                Log.Information("[LinkService - CreateSecureLinkAsync] - SecureLinkSettings vložen s ID: {SettingsId}", settingsId);

                var actionOptions = new List<ActionOption>();

                if (model.ConfirmActionEnabled)
                {
                    if (!IsSqlCommandSafe(model.ConfirmActionSQL))
                    {
                        Log.Warning("[LinkService - CreateSecureLinkAsync] - ConfirmActionSQL obsahuje nepovolené příkazy.");
                        throw new InvalidOperationException("ConfirmActionSQL obsahuje nepovolené příkazy.");
                    }
                    actionOptions.Add(new ActionOption
                    {
                        SecureLinkSettingsId = settingsId,
                        Action = ActionType.Confirm,
                        ButtonText = model.ConfirmActionButtonText,
                        SqlCommand = model.ConfirmActionSQL
                    });
                }

                if (model.RejectActionEnabled)
                {
                    if (!IsSqlCommandSafe(model.RejectActionSQL))
                    {
                        Log.Warning("[LinkService - CreateSecureLinkAsync] - RejectActionSQL obsahuje nepovolené příkazy.");
                        throw new InvalidOperationException("RejectActionSQL obsahuje nepovolené příkazy.");
                    }
                    actionOptions.Add(new ActionOption
                    {
                        SecureLinkSettingsId = settingsId,
                        Action = ActionType.Reject,
                        ButtonText = model.RejectActionButtonText,
                        SqlCommand = model.RejectActionSQL
                    });
                }

                if (model.CustomActionEnabled)
                {
                    if (!IsSqlCommandSafe(model.CustomActionSQL))
                    {
                        Log.Warning("[LinkService - CreateSecureLinkAsync] - CustomActionSQL obsahuje nepovolené příkazy.");
                        throw new InvalidOperationException("CustomActionSQL obsahuje nepovolené příkazy.");
                    }
                    actionOptions.Add(new ActionOption
                    {
                        SecureLinkSettingsId = settingsId,
                        Action = ActionType.Other,
                        ButtonText = model.CustomActionButtonText,
                        SqlCommand = model.CustomActionSQL
                    });
                }

                await _linkRepository.InsertActionOptionsAsync(actionOptions);
                Log.Information("[LinkService - CreateSecureLinkAsync] - Akční možnosti vloženy: {Count}", actionOptions.Count);

                Log.Information("[LinkService - CreateSecureLinkAsync] - Secure link úspěšně vytvořen. EncryptedKey: {Key}, Uživatel: {User}, DB: {DB}", secretKey, username, user.DatabaseName);

                return secretKey;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LinkService - CreateSecureLinkAsync] - Chyba při vytváření secure linku pro uživatele: {Username}", username);
                throw;
            }
        }


        private string GenerateSecretKey()
        {
            // GUID ve formátu "N" (32 znaků, bez pomlček).
            string guidPart = Guid.NewGuid().ToString("N");

            long unixMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Převod ba Unix timestamp do Base36.
            string timestampPart = Base36Encode(unixMillis);

            int insertPosition = guidPart.Length / 2;
            string rawKey = guidPart.Substring(0, insertPosition) + timestampPart + guidPart.Substring(insertPosition);

            // kontrolní součet (hash posledních 16 znaků).
            string checksum = ComputeChecksum(rawKey);

            // Finální klíč s kontrolním součtem na konci.
            string secretKey = rawKey + checksum;
            return secretKey;
        }

        private string ComputeChecksum(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8);
            }
        }

        private string Base36Encode(long input)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (input == 0)
                return "0";

            var result = "";
            while (input > 0)
            {
                int remainder = (int)(input % 36);
                result = chars[remainder] + result;
                input /= 36;
            }
            return result;
        }

        public bool IsSqlCommandSafe(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;

            var normalized = sql.Trim();

            if (!Regex.IsMatch(normalized, @"^(insert|update|merge)\b", RegexOptions.IgnoreCase))
            {
                return false;
            }

            string disallowedPattern = @"\b(drop|truncate|delete|alter|create|exec(ute)?)\b";

            if (Regex.IsMatch(normalized, disallowedPattern, RegexOptions.IgnoreCase))
            {
                return false;
            }

            if (normalized.Contains(";"))
            {
                return false;
            }

            return true;
        }

    }
}
