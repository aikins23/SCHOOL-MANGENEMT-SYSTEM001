using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Local durable SMS queue. Every triggered SMS is enqueued (Pending) and sent by the flusher;
    /// rows carry a SyncId so SP-3 can later sync the outbox to the cloud. SQL Server (LocalDB) via
    /// OleDb. All callers treat this as best-effort.
    /// </summary>
    public class SmsOutboxRepository
    {
        private readonly string _connectionString;
        private const string OUTBOX_TABLE = "SmsOutbox";

        public SmsOutboxRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"IF OBJECT_ID(N'SmsOutbox', N'U') IS NULL
                    CREATE TABLE SmsOutbox (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        SyncId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                        Recipient NVARCHAR(40) NOT NULL,
                        SenderId NVARCHAR(20) NOT NULL,
                        Message NVARCHAR(800) NOT NULL,
                        Status NVARCHAR(12) NOT NULL DEFAULT ('Pending'),
                        Attempts INT NOT NULL DEFAULT (0),
                        DedupKey NVARCHAR(64) NOT NULL,
                        LastError NVARCHAR(255) NULL,
                        CreatedAt DATETIME NOT NULL,
                        SentAt DATETIME NULL);";
                using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
            }

            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                await EnsureSchoolIdColumnAsync(c);
            }
        }

        private static async Task EnsureSchoolIdColumnAsync(OleDbConnection c)
        {
            var schoolId = TenantContext.CurrentSchoolId;
            if (schoolId == Guid.Empty) return;

            var school = schoolId.ToString("D");
            var sql = $@"
IF OBJECT_ID(N'SmsOutbox', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('SmsOutbox','SchoolId') IS NULL
        EXEC('ALTER TABLE [SmsOutbox] ADD SchoolId UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SmsOutbox_SchoolId] DEFAULT (''{school}'')');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SmsOutbox_SchoolId' AND object_id = OBJECT_ID(N'SmsOutbox'))
        EXEC('CREATE INDEX [IX_SmsOutbox_SchoolId] ON [SmsOutbox](SchoolId)');
END";
            using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Enqueues a Pending row; if an identical Pending row already exists, returns its Id.</summary>
        public async Task<int> EnqueueAsync(string recipient, string senderId, string message)
        {
            await EnsureTableAsync();
            string key = SmsOutboxKey.Compute(recipient, senderId, message);
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                var dupSql = "SELECT TOP 1 Id FROM SmsOutbox WHERE DedupKey=? AND Status='Pending'";
                if (tenant)
                {
                    dupSql += TenantContext.FilterClause();
                }

                using (var dup = new OleDbCommand(dupSql, c))
                {
                    dup.Parameters.AddWithValue("?", key);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(dup);
                    }

                    var ex = await dup.ExecuteScalarAsync();
                    if (ex != null && ex != DBNull.Value) return Convert.ToInt32(ex);
                }

                var ins = tenant
                    ? @"INSERT INTO SmsOutbox (Recipient,SenderId,Message,Status,Attempts,DedupKey,CreatedAt,SchoolId)
                    VALUES (?,?,?, 'Pending', 0, ?, ?, ?)"
                    : @"INSERT INTO SmsOutbox (Recipient,SenderId,Message,Status,Attempts,DedupKey,CreatedAt)
                    VALUES (?,?,?, 'Pending', 0, ?, ?)";
                using (var cmd = new OleDbCommand(ins, c))
                {
                    cmd.Parameters.AddWithValue("?", recipient ?? "");
                    cmd.Parameters.AddWithValue("?", senderId ?? "");
                    cmd.Parameters.AddWithValue("?", message ?? "");
                    cmd.Parameters.AddWithValue("?", key);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
                using (var idc = new OleDbCommand("SELECT @@IDENTITY", c))
                {
                    var id = await idc.ExecuteScalarAsync();
                    return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                }
            }
        }

        public async Task<List<SmsOutboxItem>> GetPendingAsync(int maxAttempts, int batch)
        {
            await EnsureTableAsync();
            var list = new List<SmsOutboxItem>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                string sql = $"SELECT TOP {batch} Id, Recipient, SenderId, Message, Attempts " +
                             "FROM SmsOutbox WHERE Status='Pending' AND Attempts < ?";
                if (tenant)
                {
                    sql += TenantContext.FilterClause();
                }

                sql += " ORDER BY Id";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", maxAttempts);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync())
                            list.Add(new SmsOutboxItem
                            {
                                Id = Convert.ToInt32(r["Id"]),
                                Recipient = S(r["Recipient"]),
                                SenderId = S(r["SenderId"]),
                                Message = S(r["Message"]),
                                Attempts = Convert.ToInt32(r["Attempts"])
                            });
                }
            }
            return list;
        }

        public async Task MarkSentAsync(int id)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                var sql = "UPDATE SmsOutbox SET Status='Sent', SentAt=? WHERE Id=?";
                if (tenant)
                {
                    sql += TenantContext.FilterClause();
                }

                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    cmd.Parameters.AddWithValue("?", id);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task MarkAttemptFailedAsync(int id, string error, int maxAttempts)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                var sql = @"UPDATE SmsOutbox
                    SET Attempts = Attempts + 1,
                        LastError = ?,
                        Status = CASE WHEN Attempts + 1 >= ? THEN 'Failed' ELSE 'Pending' END
                    WHERE Id = ?";
                if (tenant)
                {
                    sql += TenantContext.FilterClause();
                }

                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", (error ?? "").Length > 255 ? error.Substring(0, 255) : (error ?? ""));
                    cmd.Parameters.AddWithValue("?", maxAttempts);
                    cmd.Parameters.AddWithValue("?", id);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);
        public async Task<DataTable> GetLogTableAsync()
        {
            var table = new DataTable();
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, OUTBOX_TABLE);
                    var query = "SELECT Recipient, SenderId AS [From], Message, Status, Attempts, CreatedAt AS [Date], SentAt AS [Delivered] FROM SmsOutbox WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClause();
                    }

                    query += " ORDER BY CreatedAt DESC";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogError("Error retrieving SMS log table", ex);
            }
            return table;
        }

        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
    }
}
