using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class SyncUploadClient
    {
        private readonly OfflineSyncService _syncService;
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public SyncUploadClient()
            : this(new OfflineSyncService(), new HttpClient())
        {
        }

        public SyncUploadClient(OfflineSyncService syncService, HttpClient httpClient)
        {
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<SyncUploadAttemptResult> UploadPendingAsync(int batchSize = 100)
        {
            if (!AppConfig.Sync.IsConfigured)
                return SyncUploadAttemptResult.Skipped("Sync is not configured.");

            SyncUploadBatch batch;
            try
            {
                batch = await _syncService.PrepareUploadBatchAsync(batchSize);
            }
            catch (Exception ex)
            {
                return SyncUploadAttemptResult.Failed(ex.Message);
            }

            if (batch.Changes == null || batch.Changes.Count == 0)
                return SyncUploadAttemptResult.Skipped("No pending sync changes.");

            try
            {
                var url = AppConfig.Sync.EndpointBaseUrl.TrimEnd('/') + "/api/sync/upload";
                var json = _serializer.Serialize(batch);
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.TryAddWithoutValidation("X-Sync-Key", AppConfig.Sync.ApiKey);
                    request.Headers.TryAddWithoutValidation("X-School-Id", batch.SchoolId.ToString("D"));
                    request.Headers.TryAddWithoutValidation("X-Device-Id", batch.DeviceId.ToString("D"));
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            await _syncService.MarkUploadFailedAsync(batch, response.StatusCode + ": " + body);
                            return SyncUploadAttemptResult.Failed("Sync upload failed: " + response.StatusCode);
                        }

                        var parsed = _serializer.DeserializeObject(body) as IDictionary<string, object>;
                        var accepted = GetLongList(parsed, "acceptedOutboxIds");
                        var conflicts = GetOutboxIds(parsed, "conflicts");
                        var rejected = GetOutboxIds(parsed, "rejectedChanges");
                        var failed = new HashSet<long>(conflicts.Concat(rejected));
                        var succeeded = accepted.Where(id => !failed.Contains(id)).ToList();

                        if (succeeded.Count > 0)
                            await _syncService.MarkUploadItemsSucceededAsync(succeeded);

                        if (failed.Count > 0)
                            await _syncService.MarkUploadItemsFailedAsync(failed, "Server rejected or conflicted this sync change.");

                        return new SyncUploadAttemptResult
                        {
                            Ok = failed.Count == 0,
                            SentCount = succeeded.Count,
                            ConflictCount = conflicts.Count,
                            RejectedCount = rejected.Count,
                            Message = failed.Count == 0
                                ? "Sync upload completed."
                                : "Sync upload completed with conflicts."
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                await _syncService.MarkUploadFailedAsync(batch, ex.Message);
                return SyncUploadAttemptResult.Failed("Sync upload failed: " + ex.Message);
            }
        }

        private static List<long> GetOutboxIds(IDictionary<string, object> root, string key)
        {
            var ids = new List<long>();
            if (root == null || !root.TryGetValue(key, out var raw) || !(raw is IEnumerable items)) return ids;
            foreach (var item in items)
            {
                if (item is IDictionary<string, object> row && row.TryGetValue("outboxId", out var id))
                    ids.Add(ToLong(id));
            }
            return ids;
        }

        private static List<long> GetLongList(IDictionary<string, object> root, string key)
        {
            var ids = new List<long>();
            if (root == null || !root.TryGetValue(key, out var raw) || !(raw is IEnumerable items)) return ids;
            foreach (var item in items)
                ids.Add(ToLong(item));
            return ids;
        }

        private static long ToLong(object value)
        {
            if (value == null) return 0;
            if (value is long l) return l;
            if (value is int i) return i;
            long parsed;
            return long.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }
    }
}
