using System;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory()
        {
            _connectionString = SqlCommandExtensions.StripProvider(AppConfig.ConnectionString);
        }

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = SqlCommandExtensions.StripProvider(connectionString ?? throw new ArgumentNullException(nameof(connectionString)));
        }

        public string ConnectionString => _connectionString;

        public async Task<SqlConnection> CreateOpenAsync(CancellationToken ct = default)
        {
            var conn = new SqlConnection(_connectionString);
            int retries = 0;
            int maxRetries = 3;
            int delayMs = 1000;

            while (true)
            {
                try
                {
                    await conn.OpenAsync(ct);
                    return conn;
                }
                catch (SqlException ex) when (IsTransient(ex) && retries < maxRetries)
                {
                    retries++;
                    conn.Dispose();
                    conn = new SqlConnection(_connectionString);
                    await Task.Delay(delayMs * retries, ct);
                }
                catch
                {
                    conn.Dispose();
                    throw;
                }
            }
        }

        private static bool IsTransient(SqlException ex)
        {
            return ex.Number switch
            {
                4060 or 40197 or 40501 or 40613 or 49918 or 49919 or 49920 or 11001 or 1205 or -2 => true,
                _ => false
            };
        }
    }
}
