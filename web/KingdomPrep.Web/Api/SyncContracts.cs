namespace KingdomPrep.Web.Api;

public sealed class SyncUploadRequest
{
    public Guid SchoolId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime PreparedAtUtc { get; set; }
    public List<SyncChangeDto> Changes { get; set; } = [];
}

public sealed class SyncChangeDto
{
    public long OutboxId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid DeviceId { get; set; }
    public string TableName { get; set; } = "";
    public Guid? RecordSyncId { get; set; }
    public string PrimaryKeyName { get; set; } = "";
    public string PrimaryKeyValue { get; set; } = "";
    public string Operation { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SyncUploadResponse
{
    public Guid SchoolId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public List<long> AcceptedOutboxIds { get; set; } = [];
    public List<SyncRejectedChange> RejectedChanges { get; set; } = [];
    public List<SyncConflictDto> Conflicts { get; set; } = [];
}

public sealed class SyncRejectedChange
{
    public long OutboxId { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class SyncConflictDto
{
    public long OutboxId { get; set; }
    public string TableName { get; set; } = "";
    public Guid? RecordSyncId { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class SyncPullRequest
{
    public Guid SchoolId { get; set; }
    public Guid DeviceId { get; set; }
    public string TableName { get; set; } = "";
    public string ServerCursor { get; set; } = "";
    public int BatchSize { get; set; } = 100;
}

public sealed class SyncPullResponse
{
    public Guid SchoolId { get; set; }
    public string TableName { get; set; } = "";
    public string ServerCursor { get; set; } = "";
    public List<SyncServerChangeDto> Changes { get; set; } = [];
}

public sealed class SyncServerChangeDto
{
    public Guid? RecordSyncId { get; set; }
    public string Operation { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class SyncStatusResponse
{
    public bool Enabled { get; set; }
    public bool DeviceAuthorized { get; set; }
    public string AuthMode { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime ServerTimeUtc { get; set; }
    public string[] AllowedTables { get; set; } = [];
}

public sealed class SyncDeviceRegistrationRequest
{
    public Guid SchoolId { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = "";
    public string SyncApiKey { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string LicenseStatus { get; set; } = "Active";
    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class SyncDeviceRegistrationResponse
{
    public bool Ok { get; set; }
    public Guid SchoolId { get; set; }
    public Guid DeviceId { get; set; }
    public string ApiKeyLast4 { get; set; } = "";
    public string Message { get; set; } = "";

    public static SyncDeviceRegistrationResponse Failed(string message) =>
        new() { Ok = false, Message = message };
}
