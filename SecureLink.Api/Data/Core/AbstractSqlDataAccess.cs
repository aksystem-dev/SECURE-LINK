using Dapper;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SecureLink.Api.Data.Core
{
    public abstract class AbstractSqlDataAccess
    {
        private const int CommandTimeout = 120;
        private readonly DatabaseConnectionFactory _connectionFactory;

        protected AbstractSqlDataAccess(DatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected IDbConnection CreateConnection(string connectionName = "DefaultConnection")
        {
            return _connectionFactory.CreateConnection(connectionName);
        }

        public async Task<T> ExecuteSqlQuerySingleAsync<T>(string sql, string sqlName, object parameters = null, string connectionName = "DefaultConnection")
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var connection = CreateConnection(connectionName))
                {
                    var result = await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, commandTimeout: CommandTimeout);
                    stopwatch.Stop();
                    Log.Debug("[AbstractSqlDataAccess/ExecuteSqlQuerySingleAsync] - Query executed successfully. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                        stopwatch.ElapsedMilliseconds, sqlName, parameters);
                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "[AbstractSqlDataAccess/ExecuteSqlQuerySingleAsync] - Error executing query. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
                throw;
            }
        }

        public async Task<List<T>> ExecuteSqlQueryAsync<T>(string sql, string sqlName, object parameters = null, string connectionName = "DefaultConnection")
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var connection = CreateConnection(connectionName))
                {
                    var result = (await connection.QueryAsync<T>(sql, parameters, commandTimeout: CommandTimeout)).ToList();
                    stopwatch.Stop();
                    Log.Debug("[AbstractSqlDataAccess/ExecuteSqlQueryAsync] - Query executed successfully. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                        stopwatch.ElapsedMilliseconds, sqlName, parameters);
                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "[AbstractSqlDataAccess/ExecuteSqlQueryAsync] - Error executing query. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
                throw;
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, string sqlName, object parameters = null, string connectionName = "DefaultConnection")
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var connection = CreateConnection(connectionName))
                {
                    var result = await connection.ExecuteScalarAsync<T>(sql, parameters, commandTimeout: CommandTimeout);
                    stopwatch.Stop();
                    Log.Debug("[AbstractSqlDataAccess/ExecuteScalarAsync] - Scalar executed successfully. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}. Result: {Result}",
                        stopwatch.ElapsedMilliseconds, sqlName, parameters, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "[AbstractSqlDataAccess/ExecuteScalarAsync] - Error executing scalar. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
                throw;
            }
        }

        public async Task ExecuteNonQueryAsync(string sql, string sqlName, object parameters = null, string connectionName = "DefaultConnection")
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var connection = CreateConnection(connectionName))
                {
                    var affectedRows = await connection.ExecuteAsync(sql, parameters, commandTimeout: CommandTimeout);
                    stopwatch.Stop();
                    Log.Debug("[AbstractSqlDataAccess/ExecuteNonQueryAsync] - Command executed successfully. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}. Rows affected: {AffectedRows}",
                        stopwatch.ElapsedMilliseconds, sqlName, parameters, affectedRows);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "[AbstractSqlDataAccess/ExecuteNonQueryAsync] - Error executing command. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryWithResultAsync(string sql, string sqlName, object parameters = null, string connectionName = "DefaultConnection")
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var connection = CreateConnection(connectionName))
                {
                    int affectedRows = await connection.ExecuteAsync(sql, parameters, commandTimeout: CommandTimeout);
                    stopwatch.Stop();
                    Log.Debug("[AbstractSqlDataAccess/ExecuteNonQueryWithResultAsync] - Command executed successfully. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}. Rows affected: {AffectedRows}",
                        stopwatch.ElapsedMilliseconds, sqlName, parameters, affectedRows);
                    return affectedRows;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "[AbstractSqlDataAccess/ExecuteNonQueryWithResultAsync] - Error executing command. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
                throw;
            }
        }



        public async Task ExecuteSqlCommandBatchAsync<T>(string sql, string sqlName, IEnumerable<T> parameters, string connectionName = "DefaultConnection")
        {
            if (parameters == null || !parameters.Any())
            {
                Log.Warning("[AbstractSqlDataAccess/ExecuteSqlCommandBatchAsync] - No parameters provided for batch execution. SQL: {Sql}", sql);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var connection = CreateConnection(connectionName))
                {
                    await connection.ExecuteAsync(sql, parameters, commandTimeout: CommandTimeout);
                }

                stopwatch.Stop();
                Log.Debug("[AbstractSqlDataAccess/ExecuteSqlCommandBatchAsync] - Batch executed successfully. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Log.Error(ex, "[AbstractSqlDataAccess/ExecuteSqlCommandBatchAsync] - Error executing batch. Duration: {Duration} ms. SQL: {Sql}. Parameters: {@Parameters}",
                    stopwatch.ElapsedMilliseconds, sqlName, parameters);
                throw;
            }
        }
    }
}
