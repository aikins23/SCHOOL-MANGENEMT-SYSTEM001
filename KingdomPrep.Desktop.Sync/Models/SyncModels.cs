using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KingdomPrep.Desktop.Sync.Models
{
    public class SyncConfig
    {
        public string CloudBaseUrl { get; set; } = "https://kingdomprep.azurewebsites.net";
        public string SyncApiKey { get; set; } = "";
        public Guid SchoolId { get; set; }
        public Guid DeviceId { get; set; }
        public string LocalConnectionString { get; set; } = "";
    }

    public class SyncStatusResponse
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("serverTimeUtc")]
        public DateTime ServerTimeUtc { get; set; }

        [JsonProperty("allowedTables")]
        public List<string> AllowedTables { get; set; } = new List<string>();
    }

    public class SyncUploadRequest
    {
        [JsonProperty("schoolId")]
        public Guid SchoolId { get; set; }

        [JsonProperty("deviceId")]
        public Guid DeviceId { get; set; }

        [JsonProperty("tables")]
        public List<SyncTableData> Tables { get; set; } = new List<SyncTableData>();
    }

    public class SyncTableData
    {
        [JsonProperty("tableName")]
        public string TableName { get; set; } = "";

        [JsonProperty("rows")]
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
    }

    public class SyncPullRequest
    {
        [JsonProperty("schoolId")]
        public Guid SchoolId { get; set; }

        [JsonProperty("deviceId")]
        public Guid DeviceId { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; } = "";

        [JsonProperty("since")]
        public DateTime Since { get; set; }
    }

    public class SyncPullResponse
    {
        [JsonProperty("tableName")]
        public string TableName { get; set; } = "";

        [JsonProperty("rows")]
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
    }
}
