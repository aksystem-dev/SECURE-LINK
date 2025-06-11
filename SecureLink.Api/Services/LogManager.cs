using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using System;
using System.Threading.Tasks;
using Serilog;

namespace SecureLink.Api.Services
{
    public class LogManager
    {
        private readonly IKeyAssigmentDataAccess _dataAccess;

        public LogManager(IKeyAssigmentDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task LogKeyAssigmentAsync(string username, string ipAddress, string jwtKey, string nonce)
        {
            try
            {
                var keyAssigment = new KeyAssigment
                {
                    Username = username,
                    IpAddress = ipAddress,
                    JwtKey = jwtKey,
                    Nonce = nonce,
                    CreatedAt = DateTime.Now
                };

                await _dataAccess.InsertKeyAssigmentsAsync(keyAssigment);
                Log.Information("[LogManager - LogKeyAssigmentAsync] Successfully logged key assignment for username: {Username}, IP: {IpAddress}", username, ipAddress);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[LogManager - LogKeyAssigmentAsync] Error logging key assignment for username: {Username}, IP: {IpAddress}", username, ipAddress);
                throw;
            }
        }
    }
}
