using System;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Idempotently adds the sync columns (SyncId, UpdatedAt, RowVersion) to every syncable table.
    /// Safe to run on every startup; tables that don't exist are skipped. No behaviour change — the
    /// columns default on insert and RowVersion is engine-maintained, so repositories need no edits.
    /// </summary>
    public static class SyncSchema
    {
        public static readonly string[] SyncTables =
        {
            "Students", "Employee", "fees", "payment_record", "examss", "Attendance", "emp_leave",
            "DraftAdmissions", "Buses", "BusRoutes", "StudentTransport", "TransportPayment",
            "Books", "BookLoans", "Rolled_Out_Students", "Users",
            "SchoolInformation", "ClassFees", "GradingScheme", "ClassSubjects", "SmsOutbox"
        };

        public static Task EnsureSyncColumnsAsync() => EnsureSyncColumnsAsync(AppConfig.ConnectionString);

        public static async Task EnsureSyncColumnsAsync(string connectionString)
        {
            try
            {
                using (var c = new OleDbConnection(connectionString))
                {
                    await c.OpenAsync();
                    foreach (var t in SyncTables)
                    {
                        try { await EnsureForTableAsync(c, t); }
                        catch (Exception ex) { Services.LoggerHelper.LogWarning($"SyncSchema[{t}]: {ex.Message}"); }
                    }
                }
            }
            catch (Exception ex) { Services.LoggerHelper.LogWarning("SyncSchema: " + ex.Message); }
        }

        public static async Task EnsurePerformanceIndexesAsync()
        {
            await EnsurePerformanceIndexesAsync(AppConfig.ConnectionString);
        }

        public static async Task EnsurePerformanceIndexesAsync(string connectionString)
        {
            string[] indexes =
            {
                "IF OBJECT_ID(N'Students', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id WHERE c.object_id = OBJECT_ID(N'Students') AND c.name = N'ClassID' AND c.max_length <> -1 AND t.name NOT IN (N'text', N'ntext', N'image', N'xml', N'geography', N'geometry')) AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Students_ClassID_Dashboard' AND object_id = OBJECT_ID(N'Students')) EXEC('CREATE INDEX IX_Students_ClassID_Dashboard ON Students(ClassID)')",
                "IF OBJECT_ID(N'Students', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id = t.user_type_id WHERE c.object_id = OBJECT_ID(N'Students') AND c.name = N'Gender' AND c.max_length <> -1 AND t.name NOT IN (N'text', N'ntext', N'image', N'xml', N'geography', N'geometry')) AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Students_Gender_Dashboard' AND object_id = OBJECT_ID(N'Students')) EXEC('CREATE INDEX IX_Students_Gender_Dashboard ON Students(Gender)')",
                "IF OBJECT_ID(N'Students', N'U') IS NOT NULL AND COL_LENGTH('Students','admission_date') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Students_AdmissionDate_Dashboard' AND object_id = OBJECT_ID(N'Students')) EXEC('CREATE INDEX IX_Students_AdmissionDate_Dashboard ON Students(admission_date)')",
                "IF OBJECT_ID(N'Employee', N'U') IS NOT NULL AND COL_LENGTH('Employee','department') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employee_Department_Dashboard' AND object_id = OBJECT_ID(N'Employee')) EXEC('CREATE INDEX IX_Employee_Department_Dashboard ON Employee(department)')",
                "IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL AND COL_LENGTH('payment_record','Date') IS NOT NULL AND COL_LENGTH('payment_record','tm') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payment_record_Date_Dashboard' AND object_id = OBJECT_ID(N'payment_record')) EXEC('CREATE INDEX IX_payment_record_Date_Dashboard ON payment_record([Date] DESC, tm DESC) INCLUDE (StudentID, student_name, classID, Amount_paid, Balance, payment_mode, Bursor_name)')",
                "IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL AND COL_LENGTH('payment_record','Balance') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payment_record_Balance_Dashboard' AND object_id = OBJECT_ID(N'payment_record')) EXEC('CREATE INDEX IX_payment_record_Balance_Dashboard ON payment_record(Balance) INCLUDE (StudentID, classID)')",
                "IF OBJECT_ID(N'payment_record', N'U') IS NOT NULL AND COL_LENGTH('payment_record','payment_mode') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_payment_record_Mode_Dashboard' AND object_id = OBJECT_ID(N'payment_record')) EXEC('CREATE INDEX IX_payment_record_Mode_Dashboard ON payment_record(payment_mode) INCLUDE (Amount_paid)')",
                "IF OBJECT_ID(N'emp_leave', N'U') IS NOT NULL AND COL_LENGTH('emp_leave','status') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_emp_leave_Status_Dashboard' AND object_id = OBJECT_ID(N'emp_leave')) EXEC('CREATE INDEX IX_emp_leave_Status_Dashboard ON emp_leave([status])')",
                "IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss','gt') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_gt_Dashboard' AND object_id = OBJECT_ID(N'examss')) EXEC('CREATE INDEX IX_examss_gt_Dashboard ON examss(gt)')",
                "IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss','subject') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Subject_Dashboard' AND object_id = OBJECT_ID(N'examss')) EXEC('CREATE INDEX IX_examss_Subject_Dashboard ON examss([subject]) INCLUDE (gt, grade)')",
                "IF OBJECT_ID(N'examss', N'U') IS NOT NULL AND COL_LENGTH('examss','std_class') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_examss_Class_Dashboard' AND object_id = OBJECT_ID(N'examss')) EXEC('CREATE INDEX IX_examss_Class_Dashboard ON examss(std_class) INCLUDE (gt)')",
                "IF OBJECT_ID(N'Attendance', N'U') IS NOT NULL AND COL_LENGTH('Attendance','ReferenceType') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Attendance_TypeDate_Dashboard' AND object_id = OBJECT_ID(N'Attendance')) EXEC('CREATE INDEX IX_Attendance_TypeDate_Dashboard ON Attendance(ReferenceType, [Date]) INCLUDE (ReferenceID, [Status], FullName)')",
                "IF OBJECT_ID(N'Expenses', N'U') IS NOT NULL AND COL_LENGTH('Expenses','Date_Time') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_Date_Dashboard' AND object_id = OBJECT_ID(N'Expenses')) EXEC('CREATE INDEX IX_Expenses_Date_Dashboard ON Expenses(Date_Time) INCLUDE (Amount, Purpose)')",
                "IF OBJECT_ID(N'ClassAssignments', N'U') IS NOT NULL AND COL_LENGTH('ClassAssignments','ClassName') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassAssignments_ClassName_Dashboard' AND object_id = OBJECT_ID(N'ClassAssignments')) EXEC('CREATE INDEX IX_ClassAssignments_ClassName_Dashboard ON ClassAssignments(ClassName) INCLUDE (ClassTeacherID)')",
                "IF OBJECT_ID(N'ClassSubjects', N'U') IS NOT NULL AND COL_LENGTH('ClassSubjects','ClassName') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClassSubjects_ClassName_Manager' AND object_id = OBJECT_ID(N'ClassSubjects')) EXEC('CREATE INDEX IX_ClassSubjects_ClassName_Manager ON ClassSubjects(ClassName, SortOrder) INCLUDE (Subject)')"
            };

            try
            {
                using (var c = new OleDbConnection(connectionString))
                {
                    await c.OpenAsync();
                    foreach (string sql in indexes)
                    {
                        try
                        {
                            using (var cmd = new OleDbCommand(sql, c))
                            {
                                cmd.CommandTimeout = 60;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Services.LoggerHelper.LogWarning("Performance index skipped: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Performance indexes: " + ex.Message);
            }
        }

        private static async Task EnsureForTableAsync(OleDbConnection c, string table)
        {
            string sql = $@"
IF OBJECT_ID(N'{table}', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('{table}','SyncId') IS NULL
        EXEC('ALTER TABLE [{table}] ADD SyncId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_{table}_SyncId] DEFAULT NEWID()');
    IF COL_LENGTH('{table}','UpdatedAt') IS NULL
        EXEC('ALTER TABLE [{table}] ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT [DF_{table}_UpdatedAt] DEFAULT SYSUTCDATETIME()');
    IF COL_LENGTH('{table}','RowVersion') IS NULL
        EXEC('ALTER TABLE [{table}] ADD [RowVersion] ROWVERSION');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_{table}_SyncId' AND object_id = OBJECT_ID(N'{table}'))
        EXEC('CREATE UNIQUE INDEX [UX_{table}_SyncId] ON [{table}](SyncId)');
END";
            using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Test helper: true if the table exists and has the named column.</summary>
        public static async Task<bool> TableHasColumnAsync(string connectionString, string table, string column)
        {
            using (var c = new OleDbConnection(connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand($"SELECT COL_LENGTH('{table}', ?)", c))
                {
                    cmd.Parameters.AddWithValue("?", column);
                    var o = await cmd.ExecuteScalarAsync();
                    return o != null && o != DBNull.Value;
                }
            }
        }
    }
}
