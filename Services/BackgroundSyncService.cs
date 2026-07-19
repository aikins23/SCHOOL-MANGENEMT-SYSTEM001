using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    internal sealed class BackgroundSyncService : IDisposable
    {
        private readonly Timer _timer;
        private readonly SyncUploadClient _uploadClient;
        private readonly SyncPullClient _pullClient;
        private bool _running;
        private bool _disposed;

        public BackgroundSyncService()
            : this(new SyncUploadClient(), new SyncPullClient())
        {
        }

        public BackgroundSyncService(SyncUploadClient uploadClient, SyncPullClient pullClient)
        {
            _uploadClient = uploadClient ?? throw new ArgumentNullException(nameof(uploadClient));
            _pullClient = pullClient ?? throw new ArgumentNullException(nameof(pullClient));
            _timer = new Timer { Interval = (int)SyncRuntimeState.DefaultInterval.TotalMilliseconds };
            _timer.Tick += async (s, e) => await TryRunAsync();
        }

        public void Start()
        {
            if (_disposed) return;
            _timer.Start();
            SyncRuntimeState.RecordNextScheduled(DateTime.Now.Add(SyncRuntimeState.InitialDelay));
            Task.Delay(SyncRuntimeState.InitialDelay).ContinueWith(async _ => await TryRunAsync());
        }

        private async Task TryRunAsync()
        {
            if (_disposed || _running) return;

            if (!AppConfig.Sync.IsConfigured)
            {
                SyncRuntimeState.RecordNextScheduled(DateTime.Now.Add(SyncRuntimeState.DefaultInterval));
                return;
            }

            try
            {
                _running = true;
                SyncRuntimeState.RecordRunStarted();
                var upload = await _uploadClient.UploadPendingAsync(100);
                if (!upload.Ok)
                {
                    SyncRuntimeState.RecordFailure(upload.Message);
                    return;
                }

                var pull = await _pullClient.PullAllAsync(100);
                if (pull.Ok)
                    SyncRuntimeState.RecordSuccess(upload.Message + " " + pull.Message);
                else
                    SyncRuntimeState.RecordFailure(pull.Message);
            }
            catch (Exception ex)
            {
                SyncRuntimeState.RecordFailure("Background sync failed: " + ex.Message);
            }
            finally
            {
                _running = false;
                if (!_disposed)
                    SyncRuntimeState.RecordNextScheduled(DateTime.Now.Add(SyncRuntimeState.DefaultInterval));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
