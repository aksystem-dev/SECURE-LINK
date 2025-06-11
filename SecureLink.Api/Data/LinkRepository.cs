using Dapper;
using SecureLink.Api.Data.Core;
using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace SecureLink.Api.Data
{
    public class LinkRepository : AbstractSqlDataAccess, ILinkRepository
    {
        public LinkRepository(DatabaseConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public async Task<SecureLinkData?> GetLinkDataAsync(string encryptedKey, string clientIPAddress)
        {
            const string sqlLink = @"
                SELECT Id, EncryptedKey, Message, ExpirationDate, ShowCommentBox, Processed, DatabaseName
                FROM SecureLinkSettings 
                WHERE EncryptedKey = @EncryptedKey 
                  AND ExpirationDate >= GETDATE()
                  AND Processed = 0";

            var linkSettings = await ExecuteSqlQuerySingleAsync<SecureLinkSettings>(sqlLink, "GetSecureLinkSettings", new { EncryptedKey = encryptedKey });

            bool isSuccess = linkSettings != null;
            string message = isSuccess ? "Validace úspěšná" : "Klíč nenalezen nebo expirován";
            await LogSecureLinkRequestAsync(encryptedKey, "Validate", clientIPAddress, isSuccess, message);

            if (!isSuccess)
            {
                return new SecureLinkData
                {
                    IsValid = false,
                    Message = message,
                    Actions = new List<ActionOption>(),
                    ShowCommentBox = false,
                    DatabaseName = string.Empty
                };
            };


            const string sqlActions = @"
                SELECT ActionType AS Action, ButtonText, SqlCommand 
                FROM ActionOptions 
                WHERE SecureLinkSettingsId = @Id";

            var actions = await ExecuteSqlQueryAsync<ActionOption>(sqlActions, "GetActionOptions", new { Id = linkSettings.Id });

            return new SecureLinkData
            {
                IsValid = true,
                Message = linkSettings.Message,
                Actions = actions,
                ShowCommentBox = linkSettings.ShowCommentBox,
                DatabaseName = linkSettings.DatabaseName
            };
        }

        public async Task<ActionOption?> GetActionOptionAsync(string encryptedKey, ActionType action)
        {
            const string sql = @"
                SELECT ao.ActionType as Action, ao.ButtonText, ao.SqlCommand
                FROM ActionOptions ao
                INNER JOIN SecureLinkSettings sl ON sl.Id = ao.SecureLinkSettingsId
                WHERE sl.EncryptedKey = @EncryptedKey 
                  AND ao.ActionType = @ActionType";

            return await ExecuteSqlQuerySingleAsync<ActionOption>(sql, "GetActionOption", new { EncryptedKey = encryptedKey, ActionType = (int)action });
        }

        public async Task<SecureLinkSettings?> GetLinkSettingsAsync(string encryptedKey)
        {
            const string sql = @"
        SELECT Id, EncryptedKey, Message, DatabaseName, ExpirationDate, ShowCommentBox, Processed
        FROM SecureLinkSettings
        WHERE EncryptedKey = @EncryptedKey";

            return await ExecuteSqlQuerySingleAsync<SecureLinkSettings>(
                sql,
                "GetSecureLinkSettings",
                new { EncryptedKey = encryptedKey }
            );
        }

        public async Task LogSecureLinkRequestAsync(string encryptedKey, string requestType, string clientIPAddress, bool isSuccess, string message)
        {
            const string sql = @"
                INSERT INTO SecureLinkLogs (EncryptedKey, RequestType, ClientIPAddress, IsSuccess, Message)
                VALUES (@EncryptedKey, @RequestType, @ClientIPAddress, @IsSuccess, @Message)";

            await ExecuteNonQueryAsync(sql, "LogSecureLinkRequest", new
            {
                EncryptedKey = encryptedKey,
                RequestType = requestType,
                ClientIPAddress = clientIPAddress,
                IsSuccess = isSuccess,
                Message = message
            });
        }

        public async Task<int> GetFailedAttemptsAsync(string clientIPAddress)
        {
            const string sql = @"
        SELECT FailedCount 
        FROM FailedValidations 
        WHERE ClientIPAddress = @ClientIPAddress";

            return await ExecuteScalarAsync<int>(sql, "GetFailedAttempts", new { ClientIPAddress = clientIPAddress });
        }

        public async Task<bool> IsIpBlockedAsync(string clientIPAddress)
        {
            const string sql = @"
        SELECT COUNT(1) 
        FROM BlockedIPs 
        WHERE IPAddress = @ClientIPAddress 
          AND BlockedUntil > GETDATE()";

            return await ExecuteScalarAsync<int>(sql, "IsIpBlocked", new { ClientIPAddress = clientIPAddress }) > 0;
        }

        public async Task RegisterFailedAttemptAsync(string clientIPAddress, string reason)
        {
            const string sql = @"
        MERGE INTO FailedValidations AS target
        USING (SELECT @ClientIPAddress AS ClientIPAddress) AS source
        ON target.ClientIPAddress = source.ClientIPAddress
        WHEN MATCHED THEN
            UPDATE SET FailedCount = FailedCount + 1, LastAttempt = GETDATE(),
                       Reason = @Reason
        WHEN NOT MATCHED THEN
            INSERT (ClientIPAddress, FailedCount, LastAttempt, Reason)
            VALUES (@ClientIPAddress, 1, GETDATE(), @Reason);";

            await ExecuteNonQueryAsync(sql, "RegisterFailedAttempt", new
            {
                ClientIPAddress = clientIPAddress,
                Reason = reason
            });

            int failedAttempts = await GetFailedAttemptsAsync(clientIPAddress);

            if (failedAttempts >= 10)
            {
                await BlockIpAsync(clientIPAddress, "Příliš mnoho neúspěšných pokusů");
            }
        }

        public async Task BlockIpAsync(string clientIPAddress, string reason)
        {
            const string sql = @"
        INSERT INTO BlockedIPs (IPAddress, Blocked, BlockedUntil)
        VALUES (@ClientIPAddress, GETDATE(), DATEADD(MINUTE, 30, GETDATE()))";

            await ExecuteNonQueryAsync(sql, "BlockIp", new { ClientIPAddress = clientIPAddress });

            await ResetFailedAttemptsAsync(clientIPAddress);
        }

        public async Task ResetFailedAttemptsAsync(string clientIPAddress)
        {
            const string sql = @"
        DELETE FROM FailedValidations WHERE ClientIPAddress = @ClientIPAddress";

            await ExecuteNonQueryAsync(sql, "ResetFailedAttempts", new { ClientIPAddress = clientIPAddress });
        }

        public async Task<bool> MarkAsProcessedAsync(string encryptedKey)
        {
            string sql = @"
          UPDATE SecureLinkSettings 
        SET Processed = 1 
        WHERE EncryptedKey = @EncryptedKey";

            int rowsAffected = await ExecuteNonQueryWithResultAsync(sql, "MarkAsProcessed", new { EncryptedKey = encryptedKey });
            return rowsAffected == 1;
        }

        public async Task ConfirmActionInPohodaAsync(string sqlCommand, string connectionName, object parameters = null)
        {
            await ExecuteNonQueryAsync(sqlCommand, "ConfirmAction", parameters, connectionName);
        }

        public async Task<int> InsertSecureLinkSettingsAsync(SecureLinkSettings settings)
        {
            string sql = @"
        INSERT INTO SecureLinkSettings (EncryptedKey, Message, DatabaseName, ExpirationDate, ShowCommentBox, Processed)
        VALUES (@EncryptedKey, @Message, @DatabaseName, @ExpirationDate, @ShowCommentBox, @Processed);
        SELECT CAST(SCOPE_IDENTITY() as int);
    ";

            int id = await ExecuteSqlQuerySingleAsync<int>(sql, "InsertSecureLinkSettings", settings);
            return id;
        }

        public async Task InsertActionOptionsAsync(IEnumerable<ActionOption> options)
        {
            string sql = @"
        INSERT INTO ActionOptions (SecureLinkSettingsId, ActionType, ButtonText, SqlCommand)
        VALUES (@SecureLinkSettingsId, @Action, @ButtonText, @SqlCommand)";

            await ExecuteNonQueryAsync(sql, "InsertActionOptions", options);
        }
    }
}
