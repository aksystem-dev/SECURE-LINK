using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SecureLink.Shared;

public class DatabaseConnectionFactory
{
    private readonly Dictionary<string, string> _connectionStrings;

    public DatabaseConnectionFactory(IConfiguration configuration)
    {
        string salt = configuration["Salt"] ?? throw new InvalidOperationException("Salt není definován v konfiguraci.");

        _connectionStrings = configuration.GetSection("ConnectionStrings")
            .GetChildren()
            .ToDictionary(
                x => x.Key,
                x => DecryptPasswordIfNeeded(x.Value, salt, x.Key));
    }

    private string DecryptPasswordIfNeeded(string connString, string salt, string name)
    {
        if (string.IsNullOrWhiteSpace(connString))
            throw new ArgumentException($"Connection string '{name}' nebyl nalezen nebo je prázdný.");

        var builder = new SqlConnectionStringBuilder(connString);
        if (!string.IsNullOrWhiteSpace(builder.Password) && builder.Password.StartsWith("ENC$"))
        {
            builder.Password = EncryptionUtility.Decrypt(builder.Password, salt);
        }

        return builder.ConnectionString;
    }


    public IDbConnection CreateConnection(string connectionName = "DefaultConnection")
    {
        if (!_connectionStrings.TryGetValue(connectionName, out var connectionString))
        {
            throw new ArgumentException($"Connection string '{connectionName}' nebyl nalezen.");
        }

        return new SqlConnection(connectionString);
    }
}
