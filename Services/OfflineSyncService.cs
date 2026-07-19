using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class OfflineSyncService
    {
        private readonly SyncOutboxRepository _repository;

        public OfflineSyncService()
            : this(new SyncOutboxRepository(AppConfig.ConnectionString))
        {
        }

        public OfflineSyncService(SyncOutboxRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<SyncUploadBatch> PrepareUploadBatchAsync(int batchSize = 100)
        {
            var schoolId = TenantContext.RequireSchoolId();

            var device = await _repository.EnsureDeviceAsync(schoolId);
            var changes = await _repository.GetPendingAsync(batchSize);
            return new SyncUploadBatch
            {
                SchoolId = schoolId,
                DeviceId = device.DeviceId,
                PreparedAtUtc = DateTime.UtcNow,
                Changes = changes
            };
        }

        public Task MarkUploadSucceededAsync(SyncUploadBatch batch)
        {
            if (batch == null || batch.Changes == null) return Task.CompletedTask;
            return _repository.MarkSentAsync(batch.Changes.Select(c => c.OutboxId));
        }

        public Task MarkUploadItemsSucceededAsync(IEnumerable<long> outboxIds)
        {
            return _repository.MarkSentAsync(outboxIds);
        }

        public Task MarkUploadFailedAsync(SyncUploadBatch batch, string error)
        {
            if (batch == null || batch.Changes == null) return Task.CompletedTask;
            return _repository.MarkFailedAsync(batch.Changes.Select(c => c.OutboxId), error);
        }

        public Task MarkUploadItemsFailedAsync(IEnumerable<long> outboxIds, string error)
        {
            return _repository.MarkFailedAsync(outboxIds, error);
        }

        public Task<int> CountPendingUploadsAsync()
        {
            return _repository.CountPendingAsync();
        }

        public Task<SyncOutboxSummary> GetOutboxSummaryAsync()
        {
            return _repository.GetSummaryAsync();
        }

        public Task<System.Collections.Generic.List<SyncOutboxIssueRow>> GetOutboxIssueRowsAsync(int take = 200)
        {
            return _repository.GetIssueRowsAsync(take);
        }

        public async Task<bool> HasPendingUploadsAsync()
        {
            return await CountPendingUploadsAsync() > 0;
        }
    }
}
