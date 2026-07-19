using KingdomPrep.Shared.Models;
using System;
using KingdomPrep.Desktop.Sync.Models;
using KingdomPrep.Desktop.Sync.Services;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class LocalSyncConfigProvider : ISyncConfigProvider
    {
        public SyncConfig GetConfig()
        {
            var config = new SyncConfig
            {
                CloudBaseUrl = kingdom_Preparatory_School_Management_System.Common.AppConfig.Sync.EndpointBaseUrl,
                SyncApiKey = kingdom_Preparatory_School_Management_System.Common.AppConfig.Sync.ApiKey,
                LocalConnectionString = kingdom_Preparatory_School_Management_System.Common.AppConfig.ConnectionString
            };

            // Get SchoolId and stable local device identity from the desktop database.
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(kingdom_Preparatory_School_Management_System.Common.SqlCommandExtensions.StripProvider(config.LocalConnectionString)))
                {
                    connection.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP 1 SchoolId FROM SchoolInformation", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && Guid.TryParse(result.ToString(), out var id))
                        {
                            config.SchoolId = id;
                        }
                    }
                }

                if (config.SchoolId != Guid.Empty)
                {
                    var device = new kingdom_Preparatory_School_Management_System.Data.SyncOutboxRepository(config.LocalConnectionString)
                        .EnsureDeviceAsync(config.SchoolId)
                        .GetAwaiter()
                        .GetResult();
                    config.DeviceId = device.DeviceId;
                }
            }
            catch { }

            return config;
        }

        public void SaveConfig(SyncConfig config)
        {
            if (config == null) return;
            kingdom_Preparatory_School_Management_System.Common.AppConfig.Sync.EndpointBaseUrl = config.CloudBaseUrl;
            kingdom_Preparatory_School_Management_System.Common.AppConfig.Sync.ApiKey = config.SyncApiKey;
        }
    }
}
