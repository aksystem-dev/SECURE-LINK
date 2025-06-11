using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using System;
using System.Threading.Tasks;
using Serilog;
using System.Net;

namespace SecureLink.Api.Services
{
    public class UserManager
    {
        private readonly IUserDataAccess _userDataAccess;

        public UserManager(IUserDataAccess userDataAccess)
        {
            _userDataAccess = userDataAccess;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _userDataAccess.GetUserByUsernameAsync(username);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - GetUserByUsernameAsync] Error in GetUserByUsernameAsync for username: {Username}", username);
                throw;
            }
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - VerifyPassword] Error verifying password.");
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                await _userDataAccess.UpdateLastLoginAsync(userId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - UpdateLastLoginAsync] Error updating last login for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task LogLoginAttemptAsync(string username, string ipAddress, string clientIPAddress, bool success)
        {
            try
            {
                await _userDataAccess.LogLoginAttemptAsync(username, ipAddress, clientIPAddress, success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - LogLoginAttemptAsync] Error logging login attempt for username: {Username}, IP: {IpAddress}, Client IP: {clientIPAddress}", username, ipAddress, clientIPAddress);
                throw;
            }
        }

        public async Task<(int UserAttempts, int IpAttempts, bool IpBlocked)> GetRecentLoginAttemptsAsync(string username, 
                                                                                                          string ipAddress, 
                                                                                                          TimeSpan timeSpan)
        {
            try
            {
                return await _userDataAccess.GetRecentLoginAttemptsAsync(username, ipAddress, timeSpan);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - GetRecentLoginAttemptsAsync] Error getting recent login attempts for username: {Username}, IP: {IpAddress}", username, ipAddress);
                throw;
            }
        }

        public async Task<(int userAttemptsClient, int ipAttemptsClient, bool ipBlockedClient)> GetRecentClientLoginAttemptsAsync(string username,
                                                                                                        string clientIpAddress,
                                                                                                        TimeSpan timeSpan)
        {
            try
            {
                return await _userDataAccess.GetRecentClientLoginAttemptsAsync(username, clientIpAddress, timeSpan);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - GetRecentClientLoginAttemptsAsync] Error getting recent login attempts for username: {Username}, ClientIP: {ClientIpAddress}", username, clientIpAddress);
                throw;
            }
        }

        public async Task BlockUserAsync(string userName)
        {
            try
            {
                await _userDataAccess.BlockUserAsync(userName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - BlockUserAsync] Error blocking user: {Username}", userName);
                throw;
            }
        }

        public async Task BlockIPAsync(string ipAddress, DateTime? blockedUntil = null)
        {
            try
            {
                await _userDataAccess.BlockIPAsync(ipAddress, blockedUntil);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - BlockIPAsync] Error blocking IP: {IpAddress}", ipAddress);
                throw;
            }
        }

        public async Task<bool> IsUserBlockedAsync(string username)
        {
            try
            {
                return await _userDataAccess.IsUserBlockedAsync(username);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - IsUserBlockedAsync] Error checking if user is blocked: {Username}", username);
                throw;
            }
        }

        public async Task<DateTime?> GetBlockedUntilAsync(string ipAddress)
        {
            try
            {
                return await _userDataAccess.GetBlockedUntilAsync(ipAddress);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - GetBlockedUntilAsync] Error getting blocked until for IP: {IpAddress}", ipAddress);
                throw;
            }
        }

        public async Task<int> GetTemporaryBlockCountAsync(string ipAddress)
        {
            try
            {
                return await _userDataAccess.GetTemporaryBlockCountAsync(ipAddress);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - GetTemporaryBlockCountAsync] Error getting temporary block count for IP: {IpAddress}", ipAddress);
                throw;
            }
        }

        public async Task<bool> CreateUserAsync(string username, string password, string databaseName)
        {
            try
            {
                Log.Information("[UserManager - CreateUserAsync] - Vytvářím uživatele: {Username} pro databázi: {DatabaseName}", username, databaseName);

                var user = new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    DatabaseName = databaseName,
                    CreatedAt = DateTime.Now,
                    UserType = UserTypeEnum.Writer
                };

                var result = await _userDataAccess.CreateUserAsync(user);

                if (result)
                {
                    Log.Information("[UserManager - CreateUserAsync] - Uživatel {Username} úspěšně vytvořen.", username);
                }
                else
                {
                    Log.Warning("[UserManager - CreateUserAsync] - Uživatel {Username} se nepodařilo vytvořit.", username);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - CreateUserAsync] - Chyba při vytváření uživatele {Username} pro databázi {DatabaseName}", username, databaseName);
                throw;
            }
        }

        public async Task<bool> TestUserCustomDatabaseConnectionAsync(string databaseName)
        {
            try
            {
                
                Log.Information("[UserManager - TestUserCustomDatabaseConnectionAsync] Testuji připojení k databázi {DatabaseName}", databaseName);

                var result = await _userDataAccess.TestUserCustomDatabaseConnectionAsync(databaseName);

                Log.Information("[UserManager - TestUserCustomDatabaseConnectionAsync] Výsledek testu připojení: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserManager - TestUserCustomDatabaseConnectionAsync] Chyba při testování připojení k databázi.");
                return false;
            }
        }

    }
}
