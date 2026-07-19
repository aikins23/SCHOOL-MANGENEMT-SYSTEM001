using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using Microsoft.Data.SqlClient;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class SyncPullClient
    {
        private static readonly Regex Identifier = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public SyncPullClient()
            : this(new HttpClient())
        {
        }

        public SyncPullClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(45);
        }

        public async Task<SyncPullAttemptResult> PullAllAsync(int batchSize = 100)
        {
            if (!AppConfig.Sync.IsConfigured)
                return SyncPullAttemptResult.Skipped("Sync is not configured.");

            Guid schoolId;
            Guid deviceId;
            try
            {
                schoolId = TenantContext.RequireSchoolId();
                var device = await new SyncOutboxRepository(AppConfig.ConnectionString).EnsureDeviceAsync(schoolId);
                deviceId = device.DeviceId;
            }
            catch (Exception ex)
            {
                return SyncPullAttemptResult.Failed("Could not prepare sync pull: " + ex.Message);
            }

            var totalApplied = 0;
            var tableCount = 0;

            foreach (var table in SyncSchema.SyncTables)
            {
                var result = await PullTableAsync(schoolId, deviceId, table, batchSize);
                if (!result.Ok) return result;
                totalApplied += result.AppliedCount;
                if (result.AppliedCount > 0) tableCount++;
            }

            return new SyncPullAttemptResult
            {
                Ok = true,
                AppliedCount = totalApplied,
                TableCount = tableCount,
                Message = totalApplied == 0
                    ? "No server changes to pull."
                    : $"Pulled {totalApplied:N0} server change(s) into the desktop database."
            };
        }

        private async Task<SyncPullAttemptResult> PullTableAsync(Guid schoolId, Guid deviceId, string tableName, int batchSize)
        {
            var applied = 0;
            var guard = 0;

            while (guard++ < 20)
            {
                var cursor = await GetCheckpointAsync(schoolId, tableName);
                var response = await RequestTableChangesAsync(schoolId, deviceId, tableName, cursor, batchSize);
                if (!response.Ok) return response;
                if (response.Changes.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(response.ServerCursor))
                        await SaveCheckpointAsync(schoolId, tableName, response.ServerCursor);
                    break;
                }

                foreach (var change in response.Changes)
                {
                    await ApplyServerChangeAsync(tableName, schoolId, change);
                    applied++;
                }

                if (!string.IsNullOrWhiteSpace(response.ServerCursor))
                    await SaveCheckpointAsync(schoolId, tableName, response.ServerCursor);

                if (response.Changes.Count < Math.Max(1, batchSize)) break;
            }

            return new SyncPullAttemptResult { Ok = true, AppliedCount = applied, Message = "" };
        }

        private async Task<PullTableResponse> RequestTableChangesAsync(Guid schoolId, Guid deviceId, string tableName, string cursor, int batchSize)
        {
            try
            {
                var url = AppConfig.Sync.EndpointBaseUrl.TrimEnd('/') + "/api/sync/pull";
                var body = _serializer.Serialize(new
                {
                    schoolId,
                    deviceId,
                    tableName,
                    serverCursor = cursor ?? "",
                    batchSize = Math.Max(1, Math.Min(batchSize, 500))
                });

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.TryAddWithoutValidation("X-Sync-Key", AppConfig.Sync.ApiKey);
                    request.Headers.TryAddWithoutValidation("X-School-Id", schoolId.ToString("D"));
                    request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId.ToString("D"));
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                            return PullTableResponse.Failed("Sync pull failed: " + response.StatusCode);

                        return ParsePullResponse(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                return PullTableResponse.Failed("Sync pull failed: " + ex.Message);
            }
        }

        private PullTableResponse ParsePullResponse(string body)
        {
            var parsed = _serializer.DeserializeObject(body) as IDictionary<string, object>;
            if (parsed == null) return PullTableResponse.Failed("Sync pull response was not valid JSON.");

            var result = new PullTableResponse
            {
                Ok = true,
                ServerCursor = GetString(parsed, "serverCursor")
            };

            if (parsed.TryGetValue("changes", out var rawChanges) && rawChanges is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (!(item is IDictionary<string, object> row)) continue;
                    result.Changes.Add(new ServerChange
                    {
                        RecordSyncId = GetNullableGuid(row, "recordSyncId"),
                        Operation = GetString(row, "operation"),
                        Payload = GetString(row, "payload"),
                        UpdatedAtUtc = GetString(row, "updatedAtUtc")
                    });
                }
            }

            return result;
        }

        private async Task ApplyServerChangeAsync(string tableName, Guid schoolId, ServerChange change)
        {
            EnsureSafeTable(tableName);
            if (change.RecordSyncId == null || change.RecordSyncId == Guid.Empty) return;

            if (string.Equals(change.Operation, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                await DeleteLocalAsync(tableName, schoolId, change.RecordSyncId.Value);
                return;
            }

            var payload = _serializer.DeserializeObject(change.Payload ?? "{}") as IDictionary<string, object>;
            if (payload == null || payload.Count == 0) return;
            await UpsertLocalAsync(tableName, schoolId, change.RecordSyncId.Value, payload);
        }

        private async Task UpsertLocalAsync(string tableName, Guid schoolId, Guid syncId, IDictionary<string, object> payload)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);

                var columns = await GetColumnsAsync(c, tableName);
                if (columns.Count == 0 || !columns.Any(x => Same(x.Name, "SyncId"))) return;

                var exists = await ExistsBySyncIdAsync(c, tableName, schoolId, syncId, columns.Any(x => Same(x.Name, "SchoolId")));
                var preserveIdentity = ShouldPreserveIdentity(tableName);
                var writable = columns
                    .Where(x => (!x.IsIdentity || preserveIdentity) && !x.IsComputed && !x.IsRowVersion)
                    .Where(x => payload.ContainsKey(x.Name) || Same(x.Name, "SchoolId") || Same(x.Name, "SyncId"))
                    .ToList();

                if (!writable.Any(x => Same(x.Name, "SyncId"))) return;

                if (exists)
                    await UpdateLocalAsync(c, tableName, schoolId, syncId, payload, writable);
                else
                    await InsertLocalAsync(c, tableName, schoolId, syncId, payload, writable);
            }
        }

        private static async Task InsertLocalAsync(SqlConnection c, string tableName, Guid schoolId, Guid syncId, IDictionary<string, object> payload, List<ColumnInfo> writable)
        {
            var columns = writable
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            if (columns.Count == 0) return;
            var columnNames = columns.Select(x => x.Name).ToList();

            var insertSql = "INSERT INTO " + Quote(tableName) + " (" + string.Join(", ", columnNames.Select(Quote)) + ") VALUES (" + string.Join(", ", columnNames.Select(ParameterName)) + ")";
            var identityInsert = ShouldPreserveIdentity(tableName) && columns.Any(x => x.IsIdentity);
            var sql = identityInsert
                ? "SET IDENTITY_INSERT " + Quote(tableName) + " ON; " + insertSql + "; SET IDENTITY_INSERT " + Quote(tableName) + " OFF;"
                : insertSql;

            using (var cmd = new SqlCommand(sql, c))
            {
                AddParameters(cmd, columns, payload, schoolId, syncId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task UpdateLocalAsync(SqlConnection c, string tableName, Guid schoolId, Guid syncId, IDictionary<string, object> payload, List<ColumnInfo> writable)
        {
            var columns = writable
                .Where(x => !x.IsIdentity)
                .Where(x => !Same(x.Name, "SchoolId"))
                .Where(x => !Same(x.Name, "SyncId"))
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            if (columns.Count == 0) return;

            using (var cmd = new SqlCommand(
                "UPDATE " + Quote(tableName) + " SET " + string.Join(", ", columns.Select(x => Quote(x.Name) + " = " + ParameterName(x.Name))) + " WHERE SyncId = @WhereSyncId",
                c))
            {
                AddParameters(cmd, columns, payload, schoolId, syncId);
                cmd.Parameters.AddWithValue("@WhereSyncId", syncId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task DeleteLocalAsync(string tableName, Guid schoolId, Guid syncId)
        {
            EnsureSafeTable(tableName);
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await c.OpenAsync();
                var tenant = await TenantContext.HasSchoolIdColumnAsync(c, tableName);
                using (var cmd = new SqlCommand("DELETE FROM " + Quote(tableName) + " WHERE SyncId = @SyncId" + (tenant ? " AND SchoolId = @SchoolId" : ""), c))
                {
                    cmd.Parameters.AddWithValue("@SyncId", syncId);
                    if (tenant) cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task<bool> ExistsBySyncIdAsync(SqlConnection c, string tableName, Guid schoolId, Guid syncId, bool tenant)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM " + Quote(tableName) + " WHERE SyncId = @SyncId" + (tenant ? " AND SchoolId = @SchoolId" : ""), c))
            {
                cmd.Parameters.AddWithValue("@SyncId", syncId);
                if (tenant) cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                var result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
            }
        }

        private static void AddParameters(SqlCommand cmd, IEnumerable<ColumnInfo> columns, IDictionary<string, object> payload, Guid schoolId, Guid syncId)
        {
            foreach (var column in columns)
            {
                object value;
                if (Same(column.Name, "SchoolId")) value = schoolId;
                else if (Same(column.Name, "SyncId")) value = syncId;
                else value = payload.TryGetValue(column.Name, out var raw) ? NormalizeValue(raw, column.TypeName) : DBNull.Value;

                cmd.Parameters.AddWithValue(ParameterName(column.Name), value ?? DBNull.Value);
            }
        }

        private static object NormalizeValue(object value, string typeName)
        {
            if (value == null) return DBNull.Value;
            if (value is string s)
            {
                if (IsBinaryType(typeName))
                {
                    if (string.IsNullOrEmpty(s)) return new byte[0];
                    try { return Convert.FromBase64String(s); }
                    catch { return new byte[0]; }
                }
                if (Guid.TryParse(s, out var guid)) return guid;
                if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)) return date;
                return s;
            }

            if (value is IDictionary || value is IEnumerable && !(value is string))
                return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(value);

            return value;
        }

        private static bool IsBinaryType(string typeName) =>
            Same(typeName, "binary") || Same(typeName, "varbinary") || Same(typeName, "image");

        private async Task<string> GetCheckpointAsync(Guid schoolId, string tableName)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);
                using (var cmd = new SqlCommand("SELECT LastPulledAt, ServerCursor FROM SyncCheckpoints WHERE SchoolId = @SchoolId AND TableName = @TableName", c))
                {
                    cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync()) return "";
                        if (reader["ServerCursor"] != DBNull.Value && !string.IsNullOrWhiteSpace(Convert.ToString(reader["ServerCursor"])))
                            return Convert.ToString(reader["ServerCursor"]);
                        if (reader["LastPulledAt"] == DBNull.Value) return "";
                        return Convert.ToDateTime(reader["LastPulledAt"]).ToString("o");
                    }
                }
            }
        }

        private async Task SaveCheckpointAsync(Guid schoolId, string tableName, string serverCursor)
        {
            if (string.IsNullOrWhiteSpace(serverCursor)) return;
            var cursorText = serverCursor.Trim();
            var cursorDateText = cursorText.Split('|')[0];
            if (!DateTime.TryParse(cursorDateText, null, System.Globalization.DateTimeStyles.RoundtripKind, out var cursor)) return;

            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await c.OpenAsync();
                await SyncSchema.EnsureSyncInfrastructureAsync(c, schoolId);
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'SyncCheckpoints', N'U') IS NOT NULL AND COL_LENGTH('SyncCheckpoints', 'ServerCursor') IS NULL
    ALTER TABLE SyncCheckpoints ADD ServerCursor NVARCHAR(160) NULL;

IF EXISTS (SELECT 1 FROM SyncCheckpoints WHERE SchoolId = @SchoolId AND TableName = @TableName)
    UPDATE SyncCheckpoints SET LastPulledAt = @LastPulledAt, ServerCursor = @ServerCursor, UpdatedAt = SYSUTCDATETIME() WHERE SchoolId = @SchoolId AND TableName = @TableName
ELSE
    INSERT INTO SyncCheckpoints (SchoolId, TableName, LastPulledAt, ServerCursor, UpdatedAt) VALUES (@SchoolId, @TableName, @LastPulledAt, @ServerCursor, SYSUTCDATETIME())", c))
                {
                    cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@LastPulledAt", cursor);
                    cmd.Parameters.AddWithValue("@ServerCursor", cursorText.Length <= 160 ? cursorText : cursorText.Substring(0, 160));
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task<List<ColumnInfo>> GetColumnsAsync(SqlConnection c, string tableName)
        {
            using (var cmd = new SqlCommand(@"
SELECT c.name, t.name AS TypeName, c.is_identity, c.is_computed
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(@TableName)
ORDER BY c.column_id", c))
            {
                cmd.Parameters.AddWithValue("@TableName", tableName);
                var result = new List<ColumnInfo>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var typeName = Convert.ToString(reader["TypeName"]) ?? "";
                        result.Add(new ColumnInfo
                        {
                            Name = Convert.ToString(reader["name"]) ?? "",
                            TypeName = typeName,
                            IsIdentity = Convert.ToBoolean(reader["is_identity"]),
                            IsComputed = Convert.ToBoolean(reader["is_computed"]),
                            IsRowVersion = Same(typeName, "timestamp") || Same(typeName, "rowversion")
                        });
                    }
                }
                return result;
            }
        }

        private static string GetString(IDictionary<string, object> row, string key) =>
            row.TryGetValue(key, out var value) && value != null ? value.ToString() : "";

        private static Guid? GetNullableGuid(IDictionary<string, object> row, string key) =>
            row.TryGetValue(key, out var value) && Guid.TryParse(Convert.ToString(value), out var id) ? id : (Guid?)null;

        private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool ShouldPreserveIdentity(string tableName) =>
            Same(tableName, "ExamTypes")
            || Same(tableName, "ApprovalWorkflows")
            || Same(tableName, "ClassPerformanceReports")
            || Same(tableName, "StudentPerformanceEntries");

        private static void EnsureSafeTable(string tableName)
        {
            EnsureSafeIdentifier(tableName);
            if (!SyncSchema.SyncTables.Any(t => Same(t, tableName)))
                throw new InvalidOperationException("Table is not configured for sync: " + tableName);
        }

        private static void EnsureSafeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Identifier.IsMatch(value))
                throw new ArgumentException("Invalid SQL identifier.");
        }

        private static string Quote(string identifier)
        {
            EnsureSafeIdentifier(identifier);
            return "[" + identifier + "]";
        }

        private static string ParameterName(string column)
        {
            EnsureSafeIdentifier(column);
            return "@" + column;
        }

        private sealed class ColumnInfo
        {
            public string Name { get; set; }
            public string TypeName { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsComputed { get; set; }
            public bool IsRowVersion { get; set; }
        }

        private sealed class ServerChange
        {
            public Guid? RecordSyncId { get; set; }
            public string Operation { get; set; }
            public string Payload { get; set; }
            public string UpdatedAtUtc { get; set; }
        }

        private sealed class PullTableResponse : SyncPullAttemptResult
        {
            public string ServerCursor { get; set; }
            public List<ServerChange> Changes { get; } = new List<ServerChange>();

            public new static PullTableResponse Failed(string message) =>
                new PullTableResponse { Ok = false, Message = message };
        }
    }

    public class SyncPullAttemptResult
    {
        public bool Ok { get; set; }
        public int AppliedCount { get; set; }
        public int TableCount { get; set; }
        public string Message { get; set; }

        public static SyncPullAttemptResult Skipped(string message) =>
            new SyncPullAttemptResult { Ok = true, Message = message ?? "", AppliedCount = 0 };

        public static SyncPullAttemptResult Failed(string message) =>
            new SyncPullAttemptResult { Ok = false, Message = message ?? "Sync pull failed." };
    }
}
