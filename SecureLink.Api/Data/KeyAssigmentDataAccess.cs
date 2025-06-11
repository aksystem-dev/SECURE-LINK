using SecureLink.Api.Data.Core;
using SecureLink.Api.Data.Interfaces;
using SecureLink.Shared.Models;

namespace SecureLink.Api.Data
{
    public class KeyAssigmentDataAccess : AbstractSqlDataAccess, IKeyAssigmentDataAccess
    {
        public KeyAssigmentDataAccess(DatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task InsertKeyAssigmentsAsync(KeyAssigment assigment)
        {
            const string sql = @"
                INSERT INTO KeyAssigments (Username, IpAddress, KeyUsed, Nonce, CreatedAt)
                VALUES (@Username, @IpAddress, @JwtKey, @Nonce, @CreatedAt)";
            await ExecuteNonQueryAsync(sql, "InsertKeyAssigments", assigment);
        }
    }
}
