using System;
using System.Data.OleDb;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kingdom.Tests
{
    internal sealed class LocalDbTestDatabase : IDisposable
    {
        private const string InstanceName = "KingdomTests";
        private readonly string _databaseName;
        private bool _disposed;

        private LocalDbTestDatabase(string databaseName)
        {
            _databaseName = databaseName;
            ConnectionString = BuildConnectionString(databaseName);
        }

        public string ConnectionString { get; }

        public static async Task<LocalDbTestDatabase> CreateAsync()
        {
            EnsureLocalDbInstanceStarted();

            string databaseName = "Kingdom_Test_" + Guid.NewGuid().ToString("N");
            var database = new LocalDbTestDatabase(databaseName);

            try
            {
                await ExecuteMasterAsync($"CREATE DATABASE [{databaseName}]");
                await database.InitializeSchemaAsync();
                return database;
            }
            catch (Exception ex)
            {
                database.Dispose();
                throw new InvalidOperationException("LocalDB integration test database could not be created: " + ex.Message, ex);
            }
        }

        private async Task InitializeSchemaAsync()
        {
            using (var connection = new OleDbConnection(ConnectionString))
            {
                await connection.OpenAsync();

                await ExecuteAsync(connection, @"
                    CREATE TABLE Employee (
                        employmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        fullName NVARCHAR(100) NULL,
                        gender NVARCHAR(20) NULL,
                        dOB DATETIME NULL,
                        conatct NVARCHAR(30) NULL,
                        email NVARCHAR(100) NULL,
                        department NVARCHAR(100) NULL,
                        position NVARCHAR(100) NULL,
                        homeTown NVARCHAR(100) NULL,
                        residence NVARCHAR(100) NULL,
                        date_of_Emplyment DATETIME NULL,
                        employment_Mode NVARCHAR(50) NULL,
                        employment_Status NVARCHAR(50) NULL,
                        emergency_Contact_Person NVARCHAR(100) NULL,
                        emergency_contact NVARCHAR(30) NULL,
                        Employees_Reviews NVARCHAR(MAX) NULL,
                        salary DECIMAL(18,2) NULL,
                        pic VARBINARY(MAX) NULL
                    )");

                await ExecuteAsync(connection, @"
                    INSERT INTO Employee
                    (fullName, gender, dOB, conatct, email, department, position, homeTown, residence,
                     date_of_Emplyment, employment_Mode, employment_Status, emergency_Contact_Person,
                     emergency_contact, Employees_Reviews, salary, pic)
                    VALUES
                    ('Ama Mensah', 'FEMALE', '1990-01-02', '0241234567', 'ama@example.com', 'Administration', 'Secretary',
                     'Kumasi', 'Accra', '2024-01-01', 'Full Time', 'Active', 'Kojo Mensah', '0241111111', '', 2500.00, 0x)");

                await ExecuteAsync(connection, @"
                    INSERT INTO Employee
                    (fullName, gender, dOB, conatct, email, department, position, homeTown, residence,
                     date_of_Emplyment, employment_Mode, employment_Status, emergency_Contact_Person,
                     emergency_contact, Employees_Reviews, salary, pic)
                    VALUES
                    ('Kofi Boateng', 'MALE', '1988-05-10', '0551234567', 'kofi@example.com', 'Teaching', 'Teacher',
                     'Cape Coast', 'Accra', '2024-02-01', 'Full Time', 'Active', 'Efua Boateng', '0551111111', '', 3200.00, 0x)");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                ExecuteMasterAsync($@"
                    IF DB_ID(N'{_databaseName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{_databaseName}];
                    END").GetAwaiter().GetResult();
            }
            catch
            {
                // Best-effort cleanup; test result should reflect the actual assertion failure, not drop cleanup.
            }
        }

        private static string BuildConnectionString(string databaseName)
        {
            return $"Provider=MSOLEDBSQL;Data Source=(localdb)\\{InstanceName};Integrated Security=SSPI;Initial Catalog={databaseName};Encrypt=False;TrustServerCertificate=True";
        }

        private static string MasterConnectionString =>
            $"Provider=MSOLEDBSQL;Data Source=(localdb)\\{InstanceName};Integrated Security=SSPI;Initial Catalog=master;Encrypt=False;TrustServerCertificate=True";

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

                using (var process = Process.Start(startInfo))
                {
                    if (!process.WaitForExit(10000))
                        return false;

                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static async Task ExecuteMasterAsync(string sql)
        {
            using (var connection = new OleDbConnection(MasterConnectionString))
            {
                await connection.OpenAsync();
                await ExecuteAsync(connection, sql);
            }
        }

        private static async Task ExecuteAsync(OleDbConnection connection, string sql)
        {
            using (var command = new OleDbCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
