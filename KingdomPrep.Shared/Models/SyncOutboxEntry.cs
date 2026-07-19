using System;

namespace KingdomPrep.Shared.Models
{
    public class SyncOutboxEntry
    {
        public long OutboxId { get; set; }
        public Guid SchoolId { get; set; }
        public Guid DeviceId { get; set; }
        public string TableName { get; set; }
        public Guid? RecordSyncId { get; set; }
        public string PrimaryKeyName { get; set; }
        public string PrimaryKeyValue { get; set; }
        public string Operation { get; set; }
        public string Payload { get; set; }
        public string Status { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
