using KingdomPrep.Shared.Models;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public static class AcademicSessionSchema
    {
        public static Task EnsureAsync() => EnsureAsync(AppConfig.ConnectionString);

        public static async Task EnsureAsync(string connectionString)
        {
            using (var connection = new SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(connectionString)))
            {
                await connection.OpenAsync();
                await EnsureTablesAsync(connection);
                await EnsureOperationalColumnsAsync(connection);
                await EnsureLegacyAcademicSessionAsync(connection);
                await BackfillLegacyTermReferencesAsync(connection);
                await EnsureIndexesAsync(connection);
            }
        }

        private static async Task EnsureTablesAsync(SqlConnection connection)
        {
            await ExecuteAsync(connection, @"
IF OBJECT_ID(N'AcademicYears', N'U') IS NULL
BEGIN
    CREATE TABLE AcademicYears (
        AcademicYearID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicYears PRIMARY KEY,
        YearName nvarchar(30) NOT NULL,
        StartDate date NOT NULL,
        EndDate date NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AcademicYears_IsActive DEFAULT 0,
        SchoolId uniqueidentifier NULL
    );
END");

            await ExecuteAsync(connection, @"
IF OBJECT_ID(N'AcademicTerms', N'U') IS NULL
BEGIN
    CREATE TABLE AcademicTerms (
        TermID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicTerms PRIMARY KEY,
        AcademicYearID int NOT NULL,
        TermName nvarchar(60) NOT NULL,
        StartDate date NOT NULL,
        EndDate date NOT NULL,
        ReopeningDate date NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AcademicTerms_IsActive DEFAULT 0,
        IsClosed bit NOT NULL CONSTRAINT DF_AcademicTerms_IsClosed DEFAULT 0,
        ClosedAt datetime NULL,
        ClosureReportPath nvarchar(260) NULL,
        SchoolId uniqueidentifier NULL
    );
END");

            await ExecuteAsync(connection, @"
IF OBJECT_ID(N'StudentEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE StudentEnrollments (
        EnrollmentID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentEnrollments PRIMARY KEY,
        StudentID varchar(50) NOT NULL,
        ClassID varchar(50) NOT NULL,
        AcademicYearID int NOT NULL,
        EnrollmentDate datetime NOT NULL CONSTRAINT DF_StudentEnrollments_Date DEFAULT GETDATE(),
        [Status] varchar(30) NOT NULL CONSTRAINT DF_StudentEnrollments_Status DEFAULT 'Active',
        SchoolId uniqueidentifier NULL
    );
END");

            await ExecuteAsync(connection, @"
IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NULL
BEGIN
    CREATE TABLE StudentFeeLedger (
        LedgerID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StudentFeeLedger PRIMARY KEY,
        StudentID varchar(50) NOT NULL,
        TermID int NOT NULL,
        PreviousBalance decimal(18,2) NOT NULL CONSTRAINT DF_StudentFeeLedger_Previous DEFAULT 0,
        CurrentTermCharge decimal(18,2) NOT NULL CONSTRAINT DF_StudentFeeLedger_Current DEFAULT 0,
        TotalExpectedAmount decimal(18,2) NOT NULL CONSTRAINT DF_StudentFeeLedger_Expected DEFAULT 0,
        TotalPaidAmount decimal(18,2) NOT NULL CONSTRAINT DF_StudentFeeLedger_Paid DEFAULT 0,
        CarriedForwardFromTermID int NULL,
        Balance AS (TotalExpectedAmount - TotalPaidAmount),
        SchoolId uniqueidentifier NULL
    );
END");

            await ExecuteAsync(connection, @"
IF OBJECT_ID(N'TermReminderSchedule', N'U') IS NULL
BEGIN
    CREATE TABLE TermReminderSchedule (
        ReminderID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TermReminderSchedule PRIMARY KEY,
        TermID int NOT NULL,
        ReminderType varchar(20) NOT NULL,
        SendOnDate date NOT NULL,
        Message nvarchar(500) NOT NULL,
        [Status] varchar(20) NOT NULL CONSTRAINT DF_TermReminderSchedule_Status DEFAULT 'Pending',
        SentAt datetime NULL,
        SchoolId uniqueidentifier NULL
    );
END");
        }

        private static async Task EnsureOperationalColumnsAsync(SqlConnection connection)
        {
            await AddColumnIfMissingAsync(connection, "payment_record", "TermID", "int NULL");
            await AddColumnIfMissingAsync(connection, "examss", "TermID", "int NULL");
            await AddColumnIfMissingAsync(connection, "Attendance", "TermID", "int NULL");
            await AddColumnIfMissingAsync(connection, "AcademicTerms", "ReopeningDate", "date NULL");
            await AddColumnIfMissingAsync(connection, "AcademicCalendar", "AcademicYearID", "int NULL");
            await AddColumnIfMissingAsync(connection, "StudentFeeLedger", "PreviousBalance", "decimal(18,2) NOT NULL CONSTRAINT DF_StudentFeeLedger_Previous_Upgrade DEFAULT 0");
            await AddColumnIfMissingAsync(connection, "StudentFeeLedger", "CurrentTermCharge", "decimal(18,2) NOT NULL CONSTRAINT DF_StudentFeeLedger_Current_Upgrade DEFAULT 0");
            await AddColumnIfMissingAsync(connection, "StudentFeeLedger", "CarriedForwardFromTermID", "int NULL");
            await ExecuteAsync(connection, "IF OBJECT_ID(N'StudentFeeLedger', N'U') IS NOT NULL UPDATE StudentFeeLedger SET CurrentTermCharge = TotalExpectedAmount WHERE CurrentTermCharge = 0 AND PreviousBalance = 0 AND TotalExpectedAmount > 0");
        }

        private static async Task EnsureLegacyAcademicSessionAsync(SqlConnection connection)
        {
            var schoolId = TenantContext.CurrentSchoolId;
            if (schoolId == Guid.Empty) schoolId = Guid.NewGuid();

            int legacyYearId = await GetScalarIntAsync(connection, @"
SELECT TOP 1 AcademicYearID
FROM AcademicYears
WHERE YearName = 'Legacy Data (Pre-2026)' AND (SchoolId = @p0 OR SchoolId IS NULL)
ORDER BY AcademicYearID", schoolId);

            if (legacyYearId == 0)
            {
                await ExecuteWithSchoolAsync(connection, @"
INSERT INTO AcademicYears (YearName, StartDate, EndDate, IsActive, SchoolId)
VALUES ('Legacy Data (Pre-2026)', '2000-01-01', '2025-12-31', 0, @p0)", schoolId);
                legacyYearId = await GetScalarIntAsync(connection, @"
SELECT TOP 1 AcademicYearID
FROM AcademicYears
WHERE YearName = 'Legacy Data (Pre-2026)' AND (SchoolId = @p0 OR SchoolId IS NULL)
ORDER BY AcademicYearID", schoolId);
            }

            int legacyTermId = await GetScalarIntAsync(connection, @"
SELECT TOP 1 TermID
FROM AcademicTerms
WHERE TermName = 'Legacy Term' AND (SchoolId = @p0 OR SchoolId IS NULL)
ORDER BY TermID", schoolId);

            if (legacyTermId == 0)
            {
                await ExecuteWithSchoolAsync(connection, @"
INSERT INTO AcademicTerms (AcademicYearID, TermName, StartDate, EndDate, IsActive, IsClosed, SchoolId)
VALUES (@p0, 'Legacy Term', '2000-01-01', '2025-12-31', 0, 1, @p1)", legacyYearId, schoolId);
            }

            await MigrateCalendarTermsAsync(connection, schoolId, legacyYearId);
        }

        private static async Task MigrateCalendarTermsAsync(SqlConnection connection, Guid schoolId, int defaultYearId)
        {
            if (await GetScalarIntAsync(connection, "SELECT CASE WHEN OBJECT_ID(N'AcademicCalendar', N'U') IS NULL THEN 0 ELSE 1 END") == 0)
                return;

            using (var cmd = new SqlCommand(@"
SELECT TermID, TermName, StartDate, EndDate, IsActive
FROM AcademicCalendar
WHERE NOT EXISTS (
    SELECT 1 FROM AcademicTerms t
    WHERE t.TermName = AcademicCalendar.TermName
      AND t.StartDate = AcademicCalendar.StartDate
      AND t.EndDate = AcademicCalendar.EndDate
      AND (t.SchoolId = @SchoolId OR t.SchoolId IS NULL)
)", connection))
            {
                cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var termName = reader["TermName"].ToString();
                        var start = Convert.ToDateTime(reader["StartDate"]);
                        var end = Convert.ToDateTime(reader["EndDate"]);
                        var active = Convert.ToBoolean(reader["IsActive"]);
                        await ExecuteWithSchoolAsync(connection, @"
INSERT INTO AcademicTerms (AcademicYearID, TermName, StartDate, EndDate, IsActive, IsClosed, SchoolId)
VALUES (@p0, @p1, @p2, @p3, @p4, 0, @p5)", defaultYearId, termName, start, end, active, schoolId);
                    }
                }
            }
        }

        private static async Task BackfillLegacyTermReferencesAsync(SqlConnection connection)
        {
            var schoolId = TenantContext.CurrentSchoolId;
            int legacyTermId = await GetScalarIntAsync(connection, @"
SELECT TOP 1 TermID FROM AcademicTerms
WHERE TermName = 'Legacy Term' AND (SchoolId = @p0 OR SchoolId IS NULL)
ORDER BY TermID", schoolId);
            if (legacyTermId == 0) return;

            await ExecuteAsync(connection, $"IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL UPDATE payment_record SET TermID = {legacyTermId} WHERE TermID IS NULL");
            await ExecuteAsync(connection, $"IF OBJECT_ID(N'examss', N'U') IS NOT NULL UPDATE examss SET TermID = {legacyTermId} WHERE TermID IS NULL");
            await ExecuteAsync(connection, $"IF OBJECT_ID(N'Attendance', N'U') IS NOT NULL UPDATE Attendance SET TermID = {legacyTermId} WHERE TermID IS NULL");

            await ExecuteWithSchoolAsync(connection, @"
IF OBJECT_ID(N'Students', N'U') IS NOT NULL
BEGIN
    INSERT INTO StudentEnrollments (StudentID, ClassID, AcademicYearID, EnrollmentDate, [Status], SchoolId)
    SELECT CONVERT(varchar(50), s.StudentID), ISNULL(s.ClassID, ''), y.AcademicYearID, ISNULL(s.admission_date, GETDATE()), 'Active', @p0
    FROM Students s
    CROSS JOIN (SELECT TOP 1 AcademicYearID FROM AcademicYears WHERE YearName = 'Legacy Data (Pre-2026)' AND (SchoolId = @p1 OR SchoolId IS NULL) ORDER BY AcademicYearID) y
    WHERE NOT EXISTS (
        SELECT 1 FROM StudentEnrollments e
        WHERE e.StudentID = CONVERT(varchar(50), s.StudentID)
          AND e.AcademicYearID = y.AcademicYearID
          AND (e.SchoolId = @p2 OR e.SchoolId IS NULL)
    );
END", schoolId, schoolId, schoolId);

            await ExecuteWithSchoolAsync(connection, @"
IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL
BEGIN
    ;WITH latest AS (
        SELECT StudentID, MAX(CONVERT(decimal(18,2), Balance)) AS Balance
        FROM payment_record
        WHERE TermID = @p0
        GROUP BY StudentID
    )
    INSERT INTO StudentFeeLedger (StudentID, TermID, PreviousBalance, CurrentTermCharge, TotalExpectedAmount, TotalPaidAmount, SchoolId)
    SELECT CONVERT(varchar(50), StudentID), @p1, 0, CASE WHEN Balance > 0 THEN Balance ELSE 0 END, CASE WHEN Balance > 0 THEN Balance ELSE 0 END, 0, @p2
    FROM latest l
    WHERE Balance > 0
      AND NOT EXISTS (
        SELECT 1 FROM StudentFeeLedger f
        WHERE f.StudentID = CONVERT(varchar(50), l.StudentID)
          AND f.TermID = @p3
          AND (f.SchoolId = @p4 OR f.SchoolId IS NULL)
      );
END", legacyTermId, legacyTermId, schoolId, legacyTermId, schoolId);
        }

        private static async Task EnsureIndexesAsync(SqlConnection connection)
        {
            await ExecuteAsync(connection, "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AcademicYears_School_Active') CREATE INDEX IX_AcademicYears_School_Active ON AcademicYears(SchoolId, IsActive)");
            await ExecuteAsync(connection, "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AcademicTerms_School_Active') CREATE INDEX IX_AcademicTerms_School_Active ON AcademicTerms(SchoolId, IsActive, IsClosed)");
            await ExecuteAsync(connection, "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StudentFeeLedger_Term_Student') CREATE INDEX IX_StudentFeeLedger_Term_Student ON StudentFeeLedger(SchoolId, TermID, StudentID)");
            await ExecuteAsync(connection, "IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL AND COL_LENGTH('payment_record','TermID') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payment_record_TermID') CREATE INDEX IX_payment_record_TermID ON payment_record(TermID)");
        }

        private static async Task AddColumnIfMissingAsync(SqlConnection connection, string table, string column, string definition)
        {
            await ExecuteAsync(connection, $@"
IF OBJECT_ID(N'{table}', N'U') IS NOT NULL AND COL_LENGTH('{table}', '{column}') IS NULL
BEGIN
    ALTER TABLE [{table}] ADD [{column}] {definition};
END");
        }

        private static async Task ExecuteAsync(SqlConnection connection, string sql)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task ExecuteWithSchoolAsync(SqlConnection connection, string sql, params object[] values)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                for (int i = 0; i < values.Length; i++)
                    cmd.Parameters.AddWithValue("@p" + i, values[i] ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task<int> GetScalarIntAsync(SqlConnection connection, string sql, params object[] values)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                for (int i = 0; i < values.Length; i++)
                    cmd.Parameters.AddWithValue("@p" + i, values[i] ?? DBNull.Value);
                var result = await cmd.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
    }
}
