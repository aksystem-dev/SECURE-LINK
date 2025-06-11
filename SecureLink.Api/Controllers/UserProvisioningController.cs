using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SecureLink.Api.Services;
using SecureLink.Shared.Models;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecureLink.Api.Controllers
{
    [ApiController]
    [Route("userprovisioning")]
    [Authorize]
    [NonceAuthorize]
    public class UserProvisioningController : ControllerBase
    {
        private readonly UserManager _userManager;
        private readonly IConfiguration _configuration;

        public UserProvisioningController(UserManager userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("register-or-verify")]
        [UserTypeAuthorize("Admin")]
        public async Task<IActionResult> RegisterOrVerify([FromBody] UserProvisioningRequest request)
        {
            var response = new UserProvisioningResponse();

            try
            {
                Log.Information("[RegisterOrVerify] Příchozí požadavek: Username={Username}, DatabaseName={DatabaseName}", request.Username, request.DatabaseName);

                if (string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.DatabaseName))
                {
                    Log.Warning("[RegisterOrVerify] Chybí povinná pole (Username, Password nebo DatabaseName).");
                    response.Success = false;
                    response.Message = "Všechna pole (Username, Password, DatabaseName) jsou povinná.";
                    return Ok(response);
                }

                var connectionStrings = _configuration.GetSection("ConnectionStrings").GetChildren().Select(x => x.Key).ToList();

                if (!connectionStrings.Contains(request.DatabaseName))
                {
                    Log.Warning("[RegisterOrVerify] Databáze '{DatabaseName}' není definována v appsettings.json.", request.DatabaseName);
                    response.Success = false;
                    response.Message = $"Zadaná databáze '{request.DatabaseName}' není definována v appsettings.json.";
                    return Ok(response);
                }
                else
                {
                    Log.Information("[RegisterOrVerify] Databáze '{DatabaseName}' nalezena v konfiguraci. Testuji připojení...", request.DatabaseName);
                }

                var connectionString = _configuration.GetConnectionString(request.DatabaseName);

                if (!await _userManager.TestUserCustomDatabaseConnectionAsync(request.DatabaseName))
                {
                    Log.Warning("[RegisterOrVerify] Test připojení k databázi '{DatabaseName}' selhal.", request.DatabaseName);
                    response.Success = false;
                    response.Message = "Nepodařilo se připojit k databázi. Zkontrolujte prosím název databáze a konfiguraci v SecureLink API.";
                    return Ok(response);
                }

                Log.Information("[RegisterOrVerify] Připojení k databázi '{DatabaseName}' bylo úspěšné.", request.DatabaseName);

                var existingUser = await _userManager.GetUserByUsernameAsync(request.Username);

                if (existingUser == null)
                {
                    Log.Information("[RegisterOrVerify] Uživatel '{Username}' neexistuje. Pokus o vytvoření nového účtu.", request.Username);

                    var created = await _userManager.CreateUserAsync(request.Username, request.Password, request.DatabaseName);
                    if (!created)
                    {
                        Log.Warning("[RegisterOrVerify] Nepodařilo se vytvořit uživatele '{Username}'.", request.Username);
                        response.Success = false;
                        response.Message = "Nepodařilo se vytvořit uživatele.";
                        return Ok(response);
                    }

                    Log.Information("[RegisterOrVerify] Uživatel '{Username}' úspěšně vytvořen.", request.Username);
                    response.Success = true;
                    response.Message = "Uživatel úspěšně vytvořen.";
                    return Ok(response);
                }

                Log.Information("[RegisterOrVerify] Uživatel '{Username}' již existuje. Provádím ověření hesla...", request.Username);

                if (!_userManager.VerifyPassword(request.Password, existingUser.PasswordHash))
                {
                    Log.Warning("[RegisterOrVerify] Špatné heslo pro uživatele '{Username}'.", request.Username);
                    response.Success = false;
                    response.Message = "Špatné heslo pro existující účet.";
                    return Ok(response);
                }

                if (!string.Equals(existingUser.DatabaseName, request.DatabaseName, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("[RegisterOrVerify] Uživatel '{Username}' má přiřazenou jinou databázi: {UserDbName} vs {RequestDbName}",
                        request.Username, existingUser.DatabaseName, request.DatabaseName);

                    response.Success = false;
                    response.Message = "Existující uživatel má přiřazenou jinou databázi.";
                    return Ok(response);
                }

                Log.Information("[RegisterOrVerify] Uživatel '{Username}' úspěšně ověřen.", request.Username);

                response.Success = true;
                response.Message = "Uživatel úspěšně ověřen.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserProvisioningController] Výjimka během registrace nebo ověření.");
                response.Success = false;
                response.Message = "Nastala neočekávaná chyba při zpracování požadavku.";
                return BadRequest(response);
            }
        }

    }
}
