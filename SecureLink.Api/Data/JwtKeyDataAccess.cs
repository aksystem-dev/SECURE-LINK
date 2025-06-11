using SecureLink.Api.Data.Core;
using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecureLink.Api.Data
{
    public class JwtKeyDataAccess : AbstractSqlDataAccess, IJwtKeyDataAccess
    {
        public JwtKeyDataAccess(DatabaseConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<List<JwtKey>> GetActiveKeysAsync()
        {
            const string sql = "SELECT * FROM JwtKeys WHERE IsActive = 1 ORDER BY ValidFrom";
            return await ExecuteSqlQueryAsync<JwtKey>(sql, "GetActiveKeys");
        }

        public async Task UpdateKeyAsync(JwtKey key)
        {
            const string sql = @"
                UPDATE JwtKeys
                SET KeyValue = @KeyValue, ValidFrom = @ValidFrom, ExpiresAt = @ExpiresAt, IsActive = @IsActive, KeyType = @KeyType
                WHERE Id = @Id";
            await ExecuteSqlQuerySingleAsync<int>(sql, "UpdateKey", key);
        }

        public async Task InsertKeyAsync(JwtKey key)
        {
            const string sql = @"
                INSERT INTO JwtKeys (KeyValue, ValidFrom, ExpiresAt, IsActive, KeyType)
                VALUES (@KeyValue, @ValidFrom, @ExpiresAt, @IsActive, @KeyType)";
            await ExecuteSqlQuerySingleAsync<int>(sql, "InsertKey", key);
        }

        public async Task InsertKeysAsync(IEnumerable<JwtKey> keys)
        {
            const string sql = @"
                INSERT INTO JwtKeys (KeyValue, ValidFrom, ExpiresAt, IsActive, KeyType)
                VALUES (@KeyValue, @ValidFrom, @ExpiresAt, @IsActive, @KeyType)";

            await ExecuteSqlCommandBatchAsync(sql, "InsertKeys", keys);
        }

        public async Task DeactivateExpiredKeysAsync()
        {
            const string sql = "UPDATE JwtKeys SET IsActive = 0 WHERE ExpiresAt < GETDATE()";
            await ExecuteSqlQuerySingleAsync<int>(sql, "DeactivateExpiredKeys");
        }

        public async Task CleanUpOldKeysAsync()
        {
            const string sql = @"
                DELETE FROM JwtKeys
                WHERE Id NOT IN (
                    SELECT TOP 10 Id FROM JwtKeys ORDER BY ExpiresAt DESC
                )";
            await ExecuteSqlQuerySingleAsync<int>(sql, "CleanUpOldKeys");
        }
    }
}
