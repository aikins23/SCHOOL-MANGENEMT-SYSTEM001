using KingdomPrep.Shared.Models;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using KingdomPrep.Desktop.Sync.Models;

namespace KingdomPrep.Desktop.Sync.Services
{
    public class ConnectivityService : IDisposable
    {
        private readonly ISyncConfigProvider _configProvider;
        private readonly HttpClient _httpClient;
        private Timer _timer;
        private bool _isChecking;

        public event EventHandler<bool> ConnectivityChanged;
        public bool IsOnline { get; private set; }

        public ConnectivityService(ISyncConfigProvider configProvider)
        {
            _configProvider = configProvider;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public void Start(TimeSpan checkInterval)
        {
            _timer = new Timer(async _ => await CheckConnectivityAsync(), null, TimeSpan.Zero, checkInterval);
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        private async Task CheckConnectivityAsync()
        {
            if (_isChecking) return;
            _isChecking = true;

            try
            {
                var config = _configProvider.GetConfig();
                if (string.IsNullOrEmpty(config.CloudBaseUrl))
                {
                    UpdateStatus(false);
                    return;
                }

                var url = $"{config.CloudBaseUrl.TrimEnd('/')}/api/sync/status";

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var status = Newtonsoft.Json.JsonConvert.DeserializeObject<SyncStatusResponse>(content);

                        UpdateStatus(status != null && status.Enabled);
                    }
                    else
                    {
                        UpdateStatus(false);
                    }
                }
            }
            catch
            {
                UpdateStatus(false);
            }
            finally
            {
                _isChecking = false;
            }
        }

        private void UpdateStatus(bool isOnline)
        {
            if (IsOnline != isOnline)
            {
                IsOnline = isOnline;
                ConnectivityChanged?.Invoke(this, isOnline);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
