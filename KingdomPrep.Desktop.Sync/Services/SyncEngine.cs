using KingdomPrep.Shared.Models;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using KingdomPrep.Desktop.Sync.Models;

namespace KingdomPrep.Desktop.Sync.Services
{
    public class SyncEngine
    {
        private readonly ISyncConfigProvider _configProvider;
        private readonly HttpClient _httpClient;
        private bool _isSyncing;
        private readonly object _syncLock = new object();

        // The tables that we are going to sync in this first iteration.
        private readonly string[] _syncTables = new[]
        {
            "Students",
            "Employees",
            "StudentFeeLedger",
            "payment_record",
            "Attendance",
            "ParentRequests",
            "ClassSubjects"
        };

        public SyncEngine(ISyncConfigProvider configProvider)
        {
            _configProvider = configProvider;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(2); // generous timeout for large payloads
        }

        public async Task TriggerSyncAsync()
        {
            lock (_syncLock)
            {
                if (_isSyncing) return;
                _isSyncing = true;
            }

            try
            {
                var config = _configProvider.GetConfig();
                if (string.IsNullOrEmpty(config.CloudBaseUrl) || string.IsNullOrEmpty(config.SyncApiKey))
                {
                    return; // Not configured
                }

                // 1. Push Outbound Changes
                await PushChangesAsync(config);

                // 2. Pull Inbound Changes
                await PullChangesAsync(config);
            }
            catch (Exception ex)
            {
                // Log exception (to be implemented)
                Console.WriteLine($"Sync error: {ex.Message}");
            }
            finally
            {
                lock (_syncLock)
                {
                    _isSyncing = false;
                }
            }
        }

        private async Task PushChangesAsync(SyncConfig config)
        {
            var uploadRequest = new SyncUploadRequest
            {
                SchoolId = config.SchoolId,
                DeviceId = config.DeviceId
            };

            using (var connection = new SqlConnection(config.LocalConnectionString))
            {
                await connection.OpenAsync();

                // Collect pending changes for each table
                foreach (var table in _syncTables)
                {
                    var tableData = await GetPendingChangesForTableAsync(connection, table);
                    if (tableData.Rows.Count > 0)
                    {
                        uploadRequest.Tables.Add(tableData);
                    }
                }

                if (uploadRequest.Tables.Count == 0) return; // Nothing to push

                // Send to cloud
                var url = $"{config.CloudBaseUrl.TrimEnd('/')}/api/sync/upload";
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(uploadRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    requestMessage.Headers.Add("X-Sync-Key", config.SyncApiKey);
                    requestMessage.Headers.Add("X-School-Id", config.SchoolId.ToString());
                    requestMessage.Content = content;

                    var response = await _httpClient.SendAsync(requestMessage);
                    if (response.IsSuccessStatusCode)
                    {
                        // Mark as synced locally
                        foreach (var table in uploadRequest.Tables)
                        {
                            await MarkAsSyncedAsync(connection, table);
                        }
                    }
                }
            }
        }

        private async Task PullChangesAsync(SyncConfig config)
        {
            using (var connection = new SqlConnection(config.LocalConnectionString))
            {
                await connection.OpenAsync();

                // We'll use a basic approach: pull changes for each table
                foreach (var table in _syncTables)
                {
                    // Get last sync date from a local tracking table or default to long ago
                    DateTime lastSync = await GetLastSyncTimeAsync(connection, table);

                    var pullReq = new SyncPullRequest
                    {
                        SchoolId = config.SchoolId,
                        DeviceId = config.DeviceId,
                        TableName = table,
                        Since = lastSync
                    };

                    var url = $"{config.CloudBaseUrl.TrimEnd('/')}/api/sync/pull";
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(pullReq);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        requestMessage.Headers.Add("X-Sync-Key", config.SyncApiKey);
                        requestMessage.Headers.Add("X-School-Id", config.SchoolId.ToString());
                        requestMessage.Content = content;

                        var response = await _httpClient.SendAsync(requestMessage);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseStr = await response.Content.ReadAsStringAsync();
                            var pullRes = Newtonsoft.Json.JsonConvert.DeserializeObject<SyncPullResponse>(responseStr);

                            if (pullRes != null && pullRes.Rows.Count > 0)
                            {
                                await MergePulledDataAsync(connection, table, pullRes.Rows);
                                await UpdateLastSyncTimeAsync(connection, table, DateTime.UtcNow);
                            }
                        }
                    }
                }
            }
        }

        private async Task<DateTime> GetLastSyncTimeAsync(SqlConnection connection, string tableName)
        {
            try
            {
                var query = "SELECT LastPulledAt FROM SyncCheckpoints WHERE TableName = @TableName AND SchoolId = @SchoolId";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@SchoolId", _configProvider.GetConfig().SchoolId);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        return (DateTime)result;
                    }
                }
            }
            catch (SqlException) { }
            return new DateTime(2020, 1, 1);
        }

        private async Task UpdateLastSyncTimeAsync(SqlConnection connection, string tableName, DateTime timeUtc)
        {
            try
            {
                var schoolId = _configProvider.GetConfig().SchoolId;
                var query = @"
                    IF EXISTS (SELECT 1 FROM SyncCheckpoints WHERE TableName = @TableName AND SchoolId = @SchoolId)
                        UPDATE SyncCheckpoints SET LastPulledAt = @TimeUtc, UpdatedAt = SYSUTCDATETIME() WHERE TableName = @TableName AND SchoolId = @SchoolId
                    ELSE
                        INSERT INTO SyncCheckpoints (SchoolId, TableName, LastPulledAt, UpdatedAt) VALUES (@SchoolId, @TableName, @TimeUtc, SYSUTCDATETIME())";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@SchoolId", schoolId);
                    cmd.Parameters.AddWithValue("@TimeUtc", timeUtc);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException) { }
        }

        private async Task MergePulledDataAsync(SqlConnection connection, string tableName, List<Dictionary<string, object>> rows)
        {
            foreach (var row in rows)
            {
                if (!row.TryGetValue("SyncId", out var syncIdObj) || syncIdObj == null) continue;
                var syncId = syncIdObj.ToString();

                // Build dynamic UPSERT logic
                var columns = new List<string>();
                var values = new List<string>();
                using (var cmd = new SqlCommand())
                {
                    cmd.Connection = connection;

                    int i = 0;
                    foreach (var kvp in row)
                    {
                        // Skip local specific columns if they come down
                        if (kvp.Key.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;

                        string paramName = $"@p{i}";
                        columns.Add(kvp.Key);
                        values.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, kvp.Value ?? DBNull.Value);
                        i++;
                    }

                    string setClause = string.Join(", ", columns.ConvertAll(c => $"{c} = source.{c}"));
                    string insertCols = string.Join(", ", columns);
                    string insertVals = string.Join(", ", values);

                    cmd.CommandText = $@"
                        UPDATE {tableName} SET SyncState = 'Synced' -- Dummy update just to verify it works for now
                        -- Full merge requires matching column schema exactly which is risky dynamically.
                        -- For MVP, we assume local schema matches web schema.
                    ";

                    // Note: A true dynamic UPSERT is complex. For this MVP, we will construct a basic check.
                    var checkCmd = new SqlCommand($"SELECT COUNT(1) FROM {tableName} WHERE SyncId = @SyncId", connection);
                    checkCmd.Parameters.AddWithValue("@SyncId", syncId);
                    int count = (int)await checkCmd.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        var updateSets = new List<string>();
                        for(int j=0; j<columns.Count; j++) {
                            if(columns[j] != "SyncId")
                                updateSets.Add($"{columns[j]} = {values[j]}");
                        }
                        cmd.CommandText = $"UPDATE {tableName} SET {string.Join(", ", updateSets)} WHERE SyncId = '{syncId}'";
                    }
                    else
                    {
                        cmd.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
                    }

                    try {
                        await cmd.ExecuteNonQueryAsync();
                    } catch (SqlException ex) {
                        Console.WriteLine($"Pull merge error: {ex.Message}");
                    }
                }
            }
        }

        private async Task<SyncTableData> GetPendingChangesForTableAsync(SqlConnection connection, string tableName)
        {
            var tableData = new SyncTableData { TableName = tableName };

            // Check if SyncState column exists first (simple try/catch or schema check)
            // For now, we assume the table has been upgraded (SP-1).
            var query = $"SELECT * FROM {tableName} WHERE SyncState = 'Pending'";

            try
            {
                using (var cmd = new SqlCommand(query, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var name = reader.GetName(i);
                                var value = reader.GetValue(i);
                                if (value == DBNull.Value) value = null;
                                row[name] = value;
                            }
                            tableData.Rows.Add(row);
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // Table might not have SyncState yet. Skip.
            }

            return tableData;
        }

        private async Task MarkAsSyncedAsync(SqlConnection connection, SyncTableData tableData)
        {
            // Update local records to 'Synced'
            foreach (var row in tableData.Rows)
            {
                if (row.TryGetValue("SyncId", out var syncIdObj) && syncIdObj != null)
                {
                    var query = $"UPDATE {tableData.TableName} SET SyncState = 'Synced' WHERE SyncId = @SyncId";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@SyncId", syncIdObj);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }
}
