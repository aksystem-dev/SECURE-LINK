using SecureLink.Api.Data.Core;
using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using Serilog;
using System;
using System.Threading.Tasks;

namespace SecureLink.Api.Data
{
    public class UserDataAccess : AbstractSqlDataAccess, IUserDataAccess
    {
        public UserDataAccess(DatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            const string sql = @"SELECT [Id],[Username], [PasswordHash], [UserType], [CreatedAt], [LastLoginAt], [IsBlocked], [FailedAttempts], [DatabaseName] 
                                 FROM Users WHERE Username = @Username";
            return await ExecuteSqlQuerySingleAsync<User>(sql, "GetUserByUsername", new { Username = username });
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            const string sql = @"UPDATE Users SET LastLoginAt = @LastLoginAt, FailedAttempts = 0 WHERE Id = @UserId";
            await ExecuteNonQueryAsync(sql, "UpdateLastLogin", new { UserId = userId, LastLoginAt = DateTime.Now });
        }

        public async Task BlockIPAsync(string ipAddress, DateTime? blockedUntil = null)
        {
            blockedUntil ??= DateTime.MaxValue;
            const string sql = @"
                INSERT INTO BlockedIPs (IPAddress, Blocked, BlockedUntil)
                VALUES (@IPAddress, @CurrentDate, @BlockedUntil)";
            await ExecuteNonQueryAsync(sql, "BlockIP", new
            {
                IPAddress = ipAddress,
                CurrentDate = DateTime.Now,
                BlockedUntil = blockedUntil
            });
        }

        public async Task LogLoginAttemptAsync(string username, string ipAddress, string clientIPAddress, bool success)
        {
            const string sql = @"
                INSERT INTO LoginAttempts (Username, AttemptTime, IpAddress, ClientIPAddress, Success)
                VALUES (@Username, @AttemptTime, @IpAddress, @ClientIPAddress, @Success)";
            await ExecuteNonQueryAsync(sql, "LogLoginAttempt", new
            {
                Username = username,
                AttemptTime = DateTime.Now,
                IpAddress = ipAddress,
                ClientIPAddress = clientIPAddress,
                Success = success
            });
        }

        public async Task<(int UserAttempts, int IpAttempts, bool IpBlocked)> GetRecentLoginAttemptsAsync(string username, string ipAddress, TimeSpan timeSpan)
        {
            const string sql = @"
                WITH LastBlock AS (
                    SELECT TOP 1 BlockedUntil
                    FROM BlockedIPs
                    WHERE IPAddress = @IpAddress
                      AND BlockedUntil < '9999-12-31'
                    ORDER BY Blocked DESC
                ),
                FilteredLoginAttempts AS (
                    SELECT 
                        AttemptTime,
                        IpAddress,
                        CASE
                            WHEN IpAddress = @IpAddress 
                                 AND AttemptTime > ISNULL((SELECT MAX(BlockedUntil) FROM LastBlock), DATEADD(SECOND, -@TimeSpanSeconds, @CurrentTime))
                            THEN 1
                            ELSE 0
                        END AS IsRelevant
                    FROM LoginAttempts
                    WHERE Success = 0
                )
                SELECT 
                    COALESCE((SELECT FailedAttempts 
                              FROM Users 
                              WHERE Username = @Username), 0) AS UserAttempts,
                    SUM(IsRelevant) AS IpAttempts,
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 
                            FROM BlockedIPs 
                            WHERE IPAddress = @IpAddress 
                              AND BlockedUntil > @CurrentTime
                        ) THEN 1
                        ELSE 0
                    END AS IpBlocked
                FROM FilteredLoginAttempts;";
            return await ExecuteSqlQuerySingleAsync<(int UserAttempts, int IpAttempts, bool IpBlocked)>(
                sql,
                "GetRecentLoginAttempts",
                new
                {
                    Username = username,
                    IpAddress = ipAddress,
                    TimeSpanSeconds = timeSpan.TotalSeconds,
                    CurrentTime = DateTime.Now
                });
        }

        public async Task<(int userAttemptsClient, int ipAttemptsClient, bool ipBlockedClient)> GetRecentClientLoginAttemptsAsync(
       string username, string clientIPAddress, TimeSpan timeSpan)
        {
            const string sql = @"
    WITH LastBlock AS (
        SELECT TOP 1 BlockedUntil
        FROM BlockedIPs
        WHERE IPAddress = @ClientIPAddress
          AND BlockedUntil < '9999-12-31'
        ORDER BY Blocked DESC
    ),
    FilteredLoginAttempts AS (
        SELECT 
            AttemptTime,
            ClientIPAddress,
            CASE
                WHEN ClientIPAddress = @ClientIPAddress 
                     AND AttemptTime > ISNULL((SELECT MAX(BlockedUntil) FROM LastBlock), DATEADD(SECOND, -@TimeSpanSeconds, @CurrentTime))
                THEN 1
                ELSE 0
            END AS IsRelevant
        FROM LoginAttempts
        WHERE Success = 0
    )
    SELECT 
        COALESCE((SELECT FailedAttempts 
                  FROM Users 
                  WHERE Username = @Username), 0) AS userAttemptsClient,
        SUM(IsRelevant) AS ipAttemptsClient,
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM BlockedIPs 
                WHERE IPAddress = @ClientIPAddress 
                  AND BlockedUntil > @CurrentTime
            ) THEN 1
            ELSE 0
        END AS ipBlockedClient
    FROM FilteredLoginAttempts;";

            return await ExecuteSqlQuerySingleAsync<(int userAttemptsClient, int ipAttemptsClient, bool ipBlockedClient)>(
                sql,
                "GetRecentClientLoginAttempts",
                new
                {
                    Username = username,
                    ClientIPAddress = clientIPAddress,
                    TimeSpanSeconds = timeSpan.TotalSeconds,
                    CurrentTime = DateTime.Now
                });
        }


        public async Task BlockUserAsync(string userName)
        {
            const string sql = "UPDATE Users SET IsBlocked = 1 WHERE Id = @Username";
            await ExecuteNonQueryAsync(sql, "BlockUser", new { Username = userName });
        }

        public async Task<bool> IsUserBlockedAsync(string username)
        {
            const string sql = "SELECT IsBlocked FROM Users WHERE Username = @Username";
            return await ExecuteSqlQuerySingleAsync<bool>(sql, "IsUserBlocked", new { Username = username });
        }

        public async Task<int> GetTemporaryBlockCountAsync(string ipAddress)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM BlockedIPs
                WHERE IPAddress = @IPAddress
                  AND Blocked > DATEADD(DAY, -1, GETDATE())
                  AND BlockedUntil < @PermanentBlockThreshold";
            return await ExecuteScalarAsync<int>(sql, "GetTemporaryBlockCount", new
            {
                IPAddress = ipAddress,
                PermanentBlockThreshold = DateTime.MaxValue
            });
        }

        public async Task<DateTime?> GetBlockedUntilAsync(string ipAddress)
        {
            const string sql = @"
                SELECT TOP 1 BlockedUntil
                FROM BlockedIPs
                WHERE IPAddress = @IPAddress
                ORDER BY Blocked DESC";
            return await ExecuteScalarAsync<DateTime?>(sql, "GetBlockedUntil", new { IPAddress = ipAddress });
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            const string sql = @"
        INSERT INTO Users (Username, PasswordHash, DatabaseName, CreatedAt, UserType)
        VALUES (@Username, @PasswordHash, @DatabaseName, @CreatedAt, @UserType)";

            int rows = await ExecuteNonQueryWithResultAsync(sql, "CreateUser", user);
            return rows == 1;
        }

        public async Task<bool> TestUserCustomDatabaseConnectionAsync(string connectionName)
        {
            const string sql = "SELECT 1";

                await ExecuteSqlQuerySingleAsync<int>(sql, "TestUserCustomDatabaseConnection", null, connectionName);
                return true;
        }
    }
}
