using System;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kingdom.Tests
{
    internal sealed class LocalDbTestDatabase : IDisposable
    {
        private const string InstanceName = "KingdomTests";
        private const string MasterConnectionStringEnvironmentVariable = "KINGDOM_TEST_SQL_MASTER_CONNECTION_STRING";
        private static string _cachedUnavailableMessage;
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
            if (!string.IsNullOrWhiteSpace(_cachedUnavailableMessage))
                throw new InvalidOperationException(_cachedUnavailableMessage);

            if (!HasConfiguredMasterConnectionString())
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
                _cachedUnavailableMessage = BuildUnavailableMessage(ex);
                throw new InvalidOperationException(_cachedUnavailableMessage, ex);
            }
        }

        private static string BuildUnavailableMessage(Exception ex)
        {
            string detail = ex == null ? "" : ex.Message;
            string hint =
                $"Set {MasterConnectionStringEnvironmentVariable} to a working SQL Server master connection string " +
                "that can create and drop temporary Kingdom_Test_* databases. " +
                "Example: Data Source=localhost;Initial Catalog=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";

            if (detail.IndexOf("Cannot generate SSPI context", StringComparison.OrdinalIgnoreCase) >= 0 ||
                detail.IndexOf("target principal name is incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint += " The current SQL Server Windows-authentication setup is failing with SSPI/Kerberos. " +
                        "Use a SQL login test connection string or repair the SQL Server SPN/Windows authentication configuration.";
            }

            if (detail.IndexOf("LocalDB", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint += " LocalDB is unavailable or unhealthy on this machine.";
            }

            return "SQL integration test database could not be created. " + hint + " Details: " + detail;
        }

        private async Task InitializeSchemaAsync()
        {
            using (var connection = new SqlConnection(ConnectionString))
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

                await ExecuteAsync(connection, @"
                    CREATE TABLE Students (
                        StudentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        FirstName NVARCHAR(100) NULL,
                        LastName NVARCHAR(100) NULL,
                        DOB DATETIME NULL,
                        Gender NVARCHAR(20) NULL,
                        Email NVARCHAR(100) NULL,
                        ClassID NVARCHAR(50) NULL,
                        HomeTown NVARCHAR(100) NULL,
                        Residence NVARCHAR(100) NULL,
                        Allegies NVARCHAR(MAX) NULL,
                        EmergencyConatct NVARCHAR(30) NULL,
                        GuidanceName NVARCHAR(100) NULL,
                        GuidianceEmail NVARCHAR(100) NULL,
                        Guidiance_Location NVARCHAR(100) NULL,
                        admission_date DATETIME NULL,
                        Std_pic VARBINARY(MAX) NULL
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE fees (
                        FeeID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        StudentID NVARCHAR(50) NOT NULL,
                        ClassID NVARCHAR(50) NOT NULL,
                        FeeName NVARCHAR(100) NULL,
                        Amount DECIMAL(18,2) NOT NULL
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE payment_record (
                        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        StudentID NVARCHAR(50) NOT NULL,
                        classID NVARCHAR(50) NULL,
                        Balance DECIMAL(18,2) NULL,
                        student_name NVARCHAR(200) NULL,
                        Amount_paid DECIMAL(18,2) NULL,
                        [Date] DATE NULL,
                        tm TIME(0) NULL,
                        payment_mode NVARCHAR(50) NULL,
                        Bursor_name NVARCHAR(100) NULL
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE examss (
                        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        std_name NVARCHAR(200) NULL,
                        std_class NVARCHAR(50) NULL,
                        cat1 DECIMAL(18,2) NULL,
                        cat2 DECIMAL(18,2) NULL,
                        cat3 DECIMAL(18,2) NULL,
                        tl_cat DECIMAL(18,2) NULL,
                        exam_score DECIMAL(18,2) NULL,
                        gt DECIMAL(18,2) NULL,
                        grade NVARCHAR(10) NULL,
                        remark NVARCHAR(100) NULL,
                        std_id NVARCHAR(50) NULL,
                        subject NVARCHAR(100) NULL,
                        term NVARCHAR(50) NULL,
                        year NVARCHAR(20) NULL
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE Attendance (
                        AttendanceID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        ReferenceID NVARCHAR(50) NOT NULL,
                        ReferenceType NVARCHAR(20) NOT NULL,
                        FullName NVARCHAR(120) NOT NULL,
                        [Date] DATE NOT NULL,
                        [Status] NVARCHAR(20) NOT NULL,
                        Remarks NVARCHAR(200) NULL,
                        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE emp_leave (
                        leaveID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        employeeID NVARCHAR(50) NULL,
                        employeeName NVARCHAR(100) NULL,
                        startDate DATE NULL,
                        endDate DATE NULL,
                        [status] NVARCHAR(50) NULL
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE Expenses (
                        ID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Expenses_name NVARCHAR(150) NULL,
                        Purpose NVARCHAR(100) NULL,
                        Description NVARCHAR(200) NULL,
                        Amount NVARCHAR(50) NULL,
                        Date_Time DATETIME NULL,
                        Payee NVARCHAR(100) NULL,
                        payer NVARCHAR(100) NULL
                    )");

                await ExecuteAsync(connection, @"
                    CREATE TABLE ClassAssignments (
                        AssignmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        ClassName NVARCHAR(50) NOT NULL,
                        ClassTeacherID INT NULL,
                        AssignedDate DATETIME NULL
                    )");
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
            var builder = new SqlConnectionStringBuilder(MasterConnectionString);
            builder["Initial Catalog"] = databaseName;
            return builder.ConnectionString;
        }

        private static string MasterConnectionString =>
            GetMasterConnectionString();

        private static string GetMasterConnectionString()
        {
            string configured = Environment.GetEnvironmentVariable(MasterConnectionStringEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;

            return $"Data Source=(localdb)\\{InstanceName};Integrated Security=SSPI;Initial Catalog=master;Encrypt=False;TrustServerCertificate=True";
        }

        private static bool HasConfiguredMasterConnectionString()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(MasterConnectionStringEnvironmentVariable));
        }

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
            using (var connection = new SqlConnection(MasterConnectionString))
            {
                await connection.OpenAsync();
                await ExecuteAsync(connection, sql);
            }
        }

        private static async Task ExecuteAsync(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
