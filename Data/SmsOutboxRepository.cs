using System;
using System.Collections.Generic;
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
        }

        /// <summary>Enqueues a Pending row; if an identical Pending row already exists, returns its Id.</summary>
        public async Task<int> EnqueueAsync(string recipient, string senderId, string message)
        {
            await EnsureTableAsync();
            string key = SmsOutboxKey.Compute(recipient, senderId, message);
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var dup = new OleDbCommand(
                    "SELECT TOP 1 Id FROM SmsOutbox WHERE DedupKey=? AND Status='Pending'", c))
                {
                    dup.Parameters.AddWithValue("?", key);
                    var ex = await dup.ExecuteScalarAsync();
                    if (ex != null && ex != DBNull.Value) return Convert.ToInt32(ex);
                }

                const string ins = @"INSERT INTO SmsOutbox (Recipient,SenderId,Message,Status,Attempts,DedupKey,CreatedAt)
                    VALUES (?,?,?, 'Pending', 0, ?, ?)";
                using (var cmd = new OleDbCommand(ins, c))
                {
                    cmd.Parameters.AddWithValue("?", recipient ?? "");
                    cmd.Parameters.AddWithValue("?", senderId ?? "");
                    cmd.Parameters.AddWithValue("?", message ?? "");
                    cmd.Parameters.AddWithValue("?", key);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
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
                string sql = $"SELECT TOP {batch} Id, Recipient, SenderId, Message, Attempts " +
                             "FROM SmsOutbox WHERE Status='Pending' AND Attempts < ? ORDER BY Id";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", maxAttempts);
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
                using (var cmd = new OleDbCommand(
                    "UPDATE SmsOutbox SET Status='Sent', SentAt=? WHERE Id=?", c))
                {
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    cmd.Parameters.AddWithValue("?", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task MarkAttemptFailedAsync(int id, string error, int maxAttempts)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE SmsOutbox
                    SET Attempts = Attempts + 1,
                        LastError = ?,
                        Status = CASE WHEN Attempts + 1 >= ? THEN 'Failed' ELSE 'Pending' END
                    WHERE Id = ?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", (error ?? "").Length > 255 ? error.Substring(0, 255) : (error ?? ""));
                    cmd.Parameters.AddWithValue("?", maxAttempts);
                    cmd.Parameters.AddWithValue("?", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);
        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
    }
}
