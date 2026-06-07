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
