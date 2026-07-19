using KingdomPrep.Shared.Models;
using KingdomPrep.Desktop.Sync.Models;

namespace KingdomPrep.Desktop.Sync.Services
{
    public interface ISyncConfigProvider
    {
        SyncConfig GetConfig();
        void SaveConfig(SyncConfig config);
    }
}
