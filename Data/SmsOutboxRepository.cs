using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Local durable SMS queue. Every triggered SMS is enqueued (Pending) and sent by the flusher;
    /// rows carry a SyncId so SP-3 can later sync the outbox to the cloud. SQL Server via
    /// Microsoft.Data.SqlClient. All callers treat this as best-effort.
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
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
                using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
            }

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                await EnsureSchoolIdColumnAsync(c);
            }
        }

        private static async Task EnsureSchoolIdColumnAsync(SqlConnection c)
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
            using (var cmd = new SqlCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Enqueues a Pending row; if an identical Pending row already exists, returns its Id.</summary>
        public async Task<int> EnqueueAsync(string recipient, string senderId, string message)
        {
            await EnsureTableAsync();
            string key = SmsOutboxKey.Compute(recipient, senderId, message);
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                var dupSql = "SELECT TOP 1 Id FROM SmsOutbox WHERE DedupKey=? AND Status='Pending'";
                if (tenant)
                {
                    dupSql += TenantContext.FilterClauseSql();
                }

                using (var dup = new SqlCommand(dupSql, c))
                {
                    dup.AddPositionalParameter(key);
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
                using (var cmd = new SqlCommand(ins, c))
                {
                    cmd.AddPositionalParameter(recipient ?? "");
                    cmd.AddPositionalParameter(senderId ?? "");
                    cmd.AddPositionalParameter(message ?? "");
                    cmd.AddPositionalParameter(key);
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
                using (var idc = new SqlCommand("SELECT @@IDENTITY", c))
                {
                    var id = await idc.ExecuteScalarAsync();
                    var outboxId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    if (outboxId > 0)
                    {
                        await TryRecordSyncUpsertAsync(outboxId, "Insert");
                    }
                    return outboxId;
                }
            }
        }

        /// <summary>
        /// Enqueues several Pending SMS rows using one database connection. Duplicate Pending
        /// messages are ignored, matching EnqueueAsync behavior.
        /// </summary>
        public async Task<int> EnqueueBatchAsync(IEnumerable<Tuple<string, string, string>> messages)
        {
            var pending = messages == null
                ? new List<Tuple<string, string, string>>()
                : messages.Where(m => m != null && !string.IsNullOrWhiteSpace(m.Item1) && !string.IsNullOrWhiteSpace(m.Item3)).ToList();

            if (pending.Count == 0) return 0;

            await EnsureTableAsync();
            var insertedIds = new List<int>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                var now = TruncateSeconds(DateTime.Now);

                foreach (var message in pending)
                {
                    string recipient = message.Item1 ?? "";
                    string senderId = message.Item2 ?? "";
                    string body = message.Item3 ?? "";
                    string key = SmsOutboxKey.Compute(recipient, senderId, body);

                    var dupSql = "SELECT TOP 1 Id FROM SmsOutbox WHERE DedupKey=? AND Status='Pending'";
                    if (tenant) dupSql += TenantContext.FilterClauseSql();

                    using (var dup = new SqlCommand(dupSql, c))
                    {
                        dup.AddPositionalParameter(key);
                        if (tenant) TenantContext.AddSchoolParameter(dup);
                        var existing = await dup.ExecuteScalarAsync();
                        if (existing != null && existing != DBNull.Value) continue;
                    }

                    var insertSql = tenant
                        ? @"INSERT INTO SmsOutbox (Recipient,SenderId,Message,Status,Attempts,DedupKey,CreatedAt,SchoolId)
                            VALUES (?,?,?, 'Pending', 0, ?, ?, ?);
                            SELECT CONVERT(INT, SCOPE_IDENTITY());"
                        : @"INSERT INTO SmsOutbox (Recipient,SenderId,Message,Status,Attempts,DedupKey,CreatedAt)
                            VALUES (?,?,?, 'Pending', 0, ?, ?);
                            SELECT CONVERT(INT, SCOPE_IDENTITY());";

                    using (var cmd = new SqlCommand(insertSql, c))
                    {
                        cmd.AddPositionalParameter(recipient);
                        cmd.AddPositionalParameter(senderId);
                        cmd.AddPositionalParameter(body);
                        cmd.AddPositionalParameter(key);
                        cmd.AddPositionalParameter(now);
                        if (tenant) TenantContext.AddSchoolParameter(cmd);

                        var id = await cmd.ExecuteScalarAsync();
                        if (id != null && id != DBNull.Value) insertedIds.Add(Convert.ToInt32(id));
                    }
                }
            }

            QueueSyncCapture(insertedIds);
            return insertedIds.Count;
        }

        public async Task<List<SmsOutboxItem>> GetPendingAsync(int maxAttempts, int batch)
        {
            await EnsureTableAsync();
            var list = new List<SmsOutboxItem>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                string sql = $"SELECT TOP {batch} Id, Recipient, SenderId, Message, Attempts " +
                             "FROM SmsOutbox WHERE Status='Pending' AND Attempts < ?";
                if (tenant)
                {
                    sql += TenantContext.FilterClauseSql();
                }

                sql += " ORDER BY Id";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(maxAttempts);
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, OUTBOX_TABLE);
                var sql = "UPDATE SmsOutbox SET Status='Sent', SentAt=? WHERE Id=?";
                if (tenant)
                {
                    sql += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    cmd.AddPositionalParameter(id);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    if (await cmd.ExecuteNonQueryAsync() > 0)
                    {
                        await TryRecordSyncUpsertAsync(id, "Update");
                    }
                }
            }
        }

        public async Task MarkAttemptFailedAsync(int id, string error, int maxAttempts)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
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
                    sql += TenantContext.FilterClauseSql();
                }

                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter((error ?? "").Length > 255 ? error.Substring(0, 255) : (error ?? ""));
                    cmd.AddPositionalParameter(maxAttempts);
                    cmd.AddPositionalParameter(id);
                    if (tenant)
                    {
                        TenantContext.AddSchoolParameter(cmd);
                    }

                    if (await cmd.ExecuteNonQueryAsync() > 0)
                    {
                        await TryRecordSyncUpsertAsync(id, "Update");
                    }
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
                using (var connection = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
                {
                    await connection.OpenAsync();
                    var tenant = await TenantContext.HasSchoolIdColumnAsync(connection, OUTBOX_TABLE);
                    var query = "SELECT Recipient, SenderId AS [From], Message, Status, Attempts, CreatedAt AS [Date], SentAt AS [Delivered] FROM SmsOutbox WHERE 1=1";
                    if (tenant)
                    {
                        query += TenantContext.FilterClauseSql();
                    }

                    query += " ORDER BY CreatedAt DESC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (tenant)
                        {
                            TenantContext.AddSchoolParameter(command);
                        }

                        using (var adapter = new SqlDataAdapter(command))
                        {
                            await Task.Run(() => adapter.Fill(table));
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

        private async Task TryRecordSyncUpsertAsync(int id, string operation)
        {
            if (id <= 0) return;
            try
            {
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(OUTBOX_TABLE, "Id", id, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("SMS outbox sync capture skipped: " + ex.Message);
            }
        }

        private void QueueSyncCapture(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            Task.Run(async () =>
            {
                foreach (var id in ids)
                {
                    await TryRecordSyncUpsertAsync(id, "Insert");
                }
            });
        }
    }
}
