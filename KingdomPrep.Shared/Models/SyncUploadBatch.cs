using System;
using System.Collections.Generic;

namespace KingdomPrep.Shared.Models
{
    public class SyncUploadBatch
    {
        public Guid SchoolId { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime PreparedAtUtc { get; set; }
        public List<SyncOutboxEntry> Changes { get; set; } = new List<SyncOutboxEntry>();
    }
}
