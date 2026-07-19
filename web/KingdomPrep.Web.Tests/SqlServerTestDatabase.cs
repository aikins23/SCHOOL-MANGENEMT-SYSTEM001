using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace KingdomPrep.Web.Tests;

internal sealed class SqlServerTestDatabase : IDisposable
{
    private const string InstanceName = "KingdomWebTests";
    private const string MasterConnectionStringEnvironmentVariable = "KINGDOM_WEB_TEST_SQL_MASTER_CONNECTION_STRING";
    private const int ConnectionTimeoutSeconds = 5;
    private readonly string _databaseName;
    private bool _disposed;

    private SqlServerTestDatabase(string databaseName)
    {
        _databaseName = databaseName;
        ConnectionString = BuildConnectionString(databaseName);
    }

    public string ConnectionString { get; }

    public static async Task<SqlServerTestDatabase?> TryCreateAsync()
    {
        if (!HasConfiguredMasterConnectionString())
            EnsureLocalDbInstanceStarted();

        var databaseName = "Kingdom_Web_Test_" + Guid.NewGuid().ToString("N");
        var database = new SqlServerTestDatabase(databaseName);

        try
        {
            await ExecuteMasterAsync($"CREATE DATABASE [{databaseName}]");
            return database;
        }
        catch
        {
            database.Dispose();
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            ExecuteMasterAsync($"""
IF DB_ID(N'{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
END
""").GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static string BuildConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(MasterConnectionString)
        {
            InitialCatalog = databaseName,
            ConnectTimeout = ConnectionTimeoutSeconds
        };
        return builder.ConnectionString;
    }

    private static string MasterConnectionString
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable(MasterConnectionStringEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            return $"Data Source=(localdb)\\{InstanceName};Integrated Security=SSPI;Initial Catalog=master;Encrypt=True;TrustServerCertificate=True;Connect Timeout={ConnectionTimeoutSeconds}";
        }
    }

    private static bool HasConfiguredMasterConnectionString() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(MasterConnectionStringEnvironmentVariable));

    private static void EnsureLocalDbInstanceStarted()
    {
        if (RunLocalDb("start " + InstanceName))
            return;

        RunLocalDb("create " + InstanceName + " -s");
    }

    private static bool RunLocalDb(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sqllocaldb",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null || !process.WaitForExit(10000))
                return false;

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task ExecuteMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectionTimeoutSeconds));
        await connection.OpenAsync(timeout.Token);
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = ConnectionTimeoutSeconds;
        await command.ExecuteNonQueryAsync(timeout.Token);
    }
}
