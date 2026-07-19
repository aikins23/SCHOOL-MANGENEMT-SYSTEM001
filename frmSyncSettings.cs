using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmSyncSettings : Form
    {
        private bool CanManageSyncSettings => AuthService.CanWrite("Settings.Sync.Manage");
        private TextBox _endpointTextBox;
        private TextBox _apiKeyTextBox;
        private TextBox _schoolIdTextBox;
        private TextBox _deviceIdTextBox;
        private Label _statusLabel;
        private Button _saveButton;
        private Button _testButton;
        private Guid _currentSchoolId;
        private Guid _currentDeviceId;

        public frmSyncSettings()
        {
            if (!AuthService.RequireAccess("frmSyncSettings", this)) return;
            InitializeModernLayout();
            LoadSettings();
        }

        private void InitializeModernLayout()
        {
            Text = "Sync Settings";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(820, 590);
            MinimumSize = new Size(760, 540);
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            Icon = Branding.AppIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                Padding = new Padding(30, 26, 30, 24),
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildSettingsPanel(), 0, 1);
            root.Controls.Add(BuildActionBar(), 0, 2);
        }

        private Control BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Connect this desktop installation to the school's web portal sync API.",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.75F)
            });
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "Sync Settings",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold)
            });
            return panel;
        }

        private Control BuildSettingsPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(26, 24, 26, 24)
            };
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiTheme.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 344,
                ColumnCount = 1,
                RowCount = 9,
                BackColor = UiTheme.Surface
            };
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            _schoolIdTextBox = CreateReadOnlyTextBox();
            _deviceIdTextBox = CreateReadOnlyTextBox();
            _endpointTextBox = CreateTextBox();

            _apiKeyTextBox = CreateTextBox();
            _apiKeyTextBox.UseSystemPasswordChar = true;

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.25F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            fields.Controls.Add(CreateLabel("Permanent school ID"), 0, 0);
            fields.Controls.Add(_schoolIdTextBox, 0, 1);
            fields.Controls.Add(CreateLabel("Registered desktop device ID"), 0, 2);
            fields.Controls.Add(_deviceIdTextBox, 0, 3);
            fields.Controls.Add(CreateLabel("Web portal URL"), 0, 4);
            fields.Controls.Add(_endpointTextBox, 0, 5);
            fields.Controls.Add(CreateLabel("Sync API key for this school and device"), 0, 6);
            fields.Controls.Add(_apiKeyTextBox, 0, 7);
            fields.Controls.Add(_statusLabel, 0, 8);

            var note = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                Text = "The web server should register this exact School ID and Device ID before accepting uploads. Never reuse one school's key for another school.",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.25F),
                TextAlign = ContentAlignment.BottomLeft
            };

            panel.Controls.Add(note);
            panel.Controls.Add(fields);
            return panel;
        }

        private Control BuildActionBar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = UiTheme.Page,
                Padding = new Padding(0, 16, 0, 0),
                WrapContents = false
            };

            _saveButton = CreateButton("Save Settings", UiTheme.Success, Color.White);
            _saveButton.Click += (s, e) => SaveSettings();

            _testButton = CreateButton("Test Connection", UiTheme.Navy, Color.White);
            _testButton.Click += async (s, e) => await TestConnectionAsync();

            var closeButton = CreateButton("Close", Color.White, UiTheme.Text);
            closeButton.FlatAppearance.BorderColor = UiTheme.Border;
            closeButton.FlatAppearance.BorderSize = 1;
            closeButton.Click += (s, e) => Close();

            bar.Controls.Add(_saveButton);
            bar.Controls.Add(_testButton);
            bar.Controls.Add(closeButton);
            ApplyWriteAccess();
            return bar;
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = UiTheme.Text,
                BackColor = UiTheme.Surface,
                Margin = new Padding(0, 0, 0, 8)
            };
        }

        private static TextBox CreateReadOnlyTextBox()
        {
            var textBox = CreateTextBox();
            textBox.ReadOnly = true;
            textBox.BackColor = UiTheme.SurfaceAlt;
            textBox.ForeColor = UiTheme.Muted;
            return textBox;
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var button = new Button
            {
                Width = 158,
                Height = 42,
                Margin = new Padding(10, 0, 0, 0),
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void LoadSettings()
        {
            LoadActivationIdentity();
            _endpointTextBox.Text = AppConfig.Sync.EndpointBaseUrl;
            _apiKeyTextBox.Text = AppConfig.Sync.ApiKey;
            SetStatus(AppConfig.Sync.IsConfigured
                ? "Sync is configured for this desktop user."
                : "Sync is not fully configured yet.", AppConfig.Sync.IsConfigured);
        }

        private void SaveSettings()
        {
            if (!AuthService.RequireWriteAccess("Settings.Sync.Manage", "Save sync settings")) return;
            if (!TryValidateInputs(out var endpoint, out var apiKey))
                return;

            AppConfig.Sync.EndpointBaseUrl = endpoint;
            AppConfig.Sync.ApiKey = apiKey;
            SetStatus("Sync settings saved.", true);
            DialogResult = DialogResult.OK;
        }

        private async Task TestConnectionAsync()
        {
            if (!AuthService.RequireWriteAccess("Settings.Sync.Manage", "Test sync settings")) return;
            if (!TryValidateInputs(out var endpoint, out var apiKey))
                return;

            if (_currentSchoolId == Guid.Empty || _currentDeviceId == Guid.Empty)
            {
                SetStatus("School identity and device identity are required before testing sync.", false);
                return;
            }

            SetButtons(false);
            SetStatus("Testing web sync endpoint...", true);

            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint.TrimEnd('/') + "/api/sync/status"))
                {
                    request.Headers.TryAddWithoutValidation("X-Sync-Key", apiKey);
                    request.Headers.TryAddWithoutValidation("X-School-Id", _currentSchoolId.ToString("D"));
                    request.Headers.TryAddWithoutValidation("X-Device-Id", _currentDeviceId.ToString("D"));

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            SetStatus("Connection successful. This desktop can reach the web sync API.", true);
                            return;
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            SetStatus("Connection reached, but this device is not registered for the school.", false);
                        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            SetStatus("Connection reached, but this school's sync license is inactive or expired.", false);
                        else
                            SetStatus("Connection failed: " + (int)response.StatusCode + " " + response.ReasonPhrase, false);
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus("Connection failed: " + ex.Message, false);
            }
            finally
            {
                SetButtons(true);
            }
        }

        private bool TryValidateInputs(out string endpoint, out string apiKey)
        {
            endpoint = (_endpointTextBox.Text ?? "").Trim().TrimEnd('/');
            apiKey = _apiKeyTextBox.Text ?? "";

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                SetStatus("Enter the web portal URL.", false);
                _endpointTextBox.Focus();
                return false;
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                SetStatus("Enter a valid URL beginning with http:// or https://.", false);
                _endpointTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                SetStatus("Enter the sync API key for this school.", false);
                _apiKeyTextBox.Focus();
                return false;
            }

            return true;
        }

        private void LoadActivationIdentity()
        {
            _currentSchoolId = TenantContext.CurrentSchoolId;
            _currentDeviceId = Guid.Empty;

            if (_currentSchoolId == Guid.Empty)
            {
                _schoolIdTextBox.Text = "School identity has not been created yet.";
                _deviceIdTextBox.Text = "Device identity unavailable.";
                return;
            }

            _schoolIdTextBox.Text = _currentSchoolId.ToString("D");

            try
            {
                var device = new SyncOutboxRepository(AppConfig.ConnectionString)
                    .EnsureDeviceAsync(_currentSchoolId)
                    .GetAwaiter()
                    .GetResult();
                _currentDeviceId = device.DeviceId;
                _deviceIdTextBox.Text = _currentDeviceId.ToString("D");
            }
            catch (Exception ex)
            {
                _deviceIdTextBox.Text = "Could not read local device identity.";
                SetStatus("Could not read local device identity: " + ex.Message, false);
            }
        }

        private void SetButtons(bool enabled)
        {
            bool canWrite = CanManageSyncSettings;
            _saveButton.Enabled = enabled && canWrite;
            _testButton.Enabled = enabled && canWrite;
        }

        private void SetStatus(string message, bool ok)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = ok ? UiTheme.Success : Color.FromArgb(190, 18, 60);
        }

        private void ApplyWriteAccess()
        {
            bool canWrite = CanManageSyncSettings;
            if (_endpointTextBox != null) _endpointTextBox.ReadOnly = !canWrite;
            if (_apiKeyTextBox != null) _apiKeyTextBox.ReadOnly = !canWrite;
            if (_saveButton != null) { _saveButton.Enabled = canWrite; _saveButton.Text = canWrite ? "Save Settings" : "Read only"; }
            if (_testButton != null) _testButton.Enabled = canWrite;
        }
    }
}
