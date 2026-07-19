using System;

namespace KingdomPrep.Shared.Models
{
    public class SyncDeviceInfo
    {
        public Guid DeviceId { get; set; }
        public Guid SchoolId { get; set; }
        public string DeviceName { get; set; }
        public string MachineName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public bool IsActive { get; set; }
    }
}
