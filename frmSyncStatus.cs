using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmSyncStatus : Form
    {
        private readonly OfflineSyncService _syncService = new OfflineSyncService();
        private readonly SyncUploadClient _uploadClient = new SyncUploadClient();
        private readonly SyncPullClient _pullClient = new SyncPullClient();
        private readonly SyncPreflightService _preflightService = new SyncPreflightService();

        private Label _connectionValue;
        private Label _endpointValue;
        private Label _authValue;
        private Label _pendingValue;
        private Label _problemValue;
        private Label _lastRunValue;
        private Label _nextRunValue;
        private Label _schedulerValue;
        private Label _latestErrorValue;
        private Label _lastResultValue;
        private Label _preflightValue;
        private Button _syncNowButton;
        private Button _viewProblemsButton;
        private Button _refreshButton;
        private Button _configureButton;
        private Button _preflightButton;
        private bool _syncInProgress;

        public frmSyncStatus()
        {
            if (!AuthService.RequireAccess("frmSyncStatus", this)) return;
            InitializeModernLayout();
            Load += async (s, e) => await RefreshStatusAsync();
        }

        private void InitializeModernLayout()
        {
            Text = "Sync Status";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1040, 840);
            MinimumSize = new Size(920, 680);
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.25F, FontStyle.Regular);
            Icon = Branding.AppIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                Padding = new Padding(30, 28, 30, 26),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildStatusCards(), 0, 1);
            root.Controls.Add(BuildDetailsPanel(), 0, 2);
            root.Controls.Add(BuildActionBar(), 0, 3);
        }

        private Control BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };

            var title = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 42,
                Text = "Sync Status",
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var subtitle = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Offline changes, web upload readiness, and the latest sync result.",
                Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.Add(subtitle);
            panel.Controls.Add(title);
            return panel;
        }

        private Control BuildStatusCards()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 4, 0, 16)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            _connectionValue = new Label();
            _pendingValue = new Label();
            _problemValue = new Label();
            _nextRunValue = new Label();

            grid.Controls.Add(CreateStatusCard("Connection", _connectionValue, "Web sync readiness", UiTheme.Success), 0, 0);
            grid.Controls.Add(CreateStatusCard("Pending Changes", _pendingValue, "Waiting to upload", Color.FromArgb(212, 175, 55)), 1, 0);
            grid.Controls.Add(CreateStatusCard("Problem Rows", _problemValue, "Pending rows with errors", Color.FromArgb(190, 18, 60)), 2, 0);
            grid.Controls.Add(CreateStatusCard("Next Run", _nextRunValue, "Automatic background sync", Color.FromArgb(59, 130, 246)), 3, 0);
            return grid;
        }

        private Control CreateStatusCard(string title, Label valueLabel, string caption, Color accent)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Margin = new Padding(0, 0, 16, 0),
                Padding = new Padding(22, 18, 22, 16)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(UiTheme.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using (var brush = new SolidBrush(accent))
                    e.Graphics.FillRectangle(brush, 0, 0, card.Width, 4);
            };

            var titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = accent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel.Dock = DockStyle.Top;
            valueLabel.Height = 44;
            valueLabel.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            valueLabel.ForeColor = UiTheme.Text;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.AutoEllipsis = true;

            var captionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = caption,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.TopLeft
            };

            card.Controls.Add(captionLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return card;
        }

        private Control BuildDetailsPanel()
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

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "Web Sync Details",
                Font = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold),
                ForeColor = UiTheme.Text
            };

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                ColumnCount = 2,
                RowCount = 7,
                BackColor = UiTheme.Surface,
                Padding = new Padding(0, 8, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            _endpointValue = CreateValueLabel();
            _authValue = CreateValueLabel();
            _schedulerValue = CreateValueLabel();
            _lastRunValue = CreateValueLabel();
            _lastResultValue = CreateValueLabel();
            _latestErrorValue = CreateValueLabel("No pending outbox errors.");
            _preflightValue = CreateValueLabel("Not checked in this session.");

            body.Controls.Add(CreateFieldLabel("Endpoint"), 0, 0);
            body.Controls.Add(_endpointValue, 1, 0);
            body.Controls.Add(CreateFieldLabel("Authentication"), 0, 1);
            body.Controls.Add(_authValue, 1, 1);
            body.Controls.Add(CreateFieldLabel("Scheduler"), 0, 2);
            body.Controls.Add(_schedulerValue, 1, 2);
            body.Controls.Add(CreateFieldLabel("Last Attempt"), 0, 3);
            body.Controls.Add(_lastRunValue, 1, 3);
            body.Controls.Add(CreateFieldLabel("Last Result"), 0, 4);
            body.Controls.Add(_lastResultValue, 1, 4);
            body.Controls.Add(CreateFieldLabel("Latest Error"), 0, 5);
            body.Controls.Add(_latestErrorValue, 1, 5);
            body.Controls.Add(CreateFieldLabel("Preflight"), 0, 6);
            body.Controls.Add(_preflightValue, 1, 6);

            var note = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Text = "Desktop users can keep working during network problems. Changes stay in the local outbox and upload when the web API is reachable.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.BottomLeft
            };

            panel.Controls.Add(note);
            panel.Controls.Add(body);
            panel.Controls.Add(title);
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

            _syncNowButton = CreateButton("Sync Now", UiTheme.Success, Color.White);
            _syncNowButton.Click += async (s, e) => await SyncNowAsync();
            _syncNowButton.Visible = AuthService.CanWrite("Admin.Sync.Run");

            _viewProblemsButton = CreateButton("View Problems", Color.White, UiTheme.Text);
            _viewProblemsButton.FlatAppearance.BorderColor = UiTheme.Border;
            _viewProblemsButton.FlatAppearance.BorderSize = 1;
            _viewProblemsButton.Click += async (s, e) =>
            {
                using (var form = new frmSyncOutboxIssues())
                {
                    form.ShowDialog(this);
                }
                await RefreshStatusAsync();
            };

            _refreshButton = CreateButton("Refresh", UiTheme.Navy, Color.White);
            _refreshButton.Click += async (s, e) => await RefreshStatusAsync();

            _preflightButton = CreateButton("Run Check", Color.White, UiTheme.Text);
            _preflightButton.FlatAppearance.BorderColor = UiTheme.Border;
            _preflightButton.FlatAppearance.BorderSize = 1;
            _preflightButton.Click += async (s, e) => await RunPreflightAsync();

            _configureButton = CreateButton("Configure", Color.White, UiTheme.Text);
            _configureButton.FlatAppearance.BorderColor = UiTheme.Border;
            _configureButton.FlatAppearance.BorderSize = 1;
            _configureButton.Visible = AuthService.CanWrite("Settings.Sync.Manage");
            _configureButton.Click += async (s, e) =>
            {
                using (var form = new frmSyncSettings())
                {
                    form.ShowDialog(this);
                }
                await RefreshStatusAsync();
            };

            if (AuthService.CanWrite("Admin.Sync.Run"))
            {
                bar.Controls.Add(_syncNowButton);
            }
            bar.Controls.Add(_viewProblemsButton);
            bar.Controls.Add(_preflightButton);
            bar.Controls.Add(_refreshButton);
            if (AuthService.CanWrite("Settings.Sync.Manage"))
            {
                bar.Controls.Add(_configureButton);
            }
            return bar;
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateValueLabel(string text = "")
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = UiTheme.Text,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var button = new Button
            {
                Width = 176,
                Height = 44,
                Margin = new Padding(10, 0, 0, 0),
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private async Task SyncNowAsync()
        {
            if (!AuthService.RequireWriteAccess("Admin.Sync.Run", "Run Sync"))
                return;

            if (_syncInProgress) return;

            try
            {
                _syncInProgress = true;
                SyncRuntimeState.RecordRunStarted();
                SetButtons(false);
                _lastResultValue.Text = "Uploading pending changes and pulling server updates...";

                var upload = await _uploadClient.UploadPendingAsync(100);
                SyncPullAttemptResult pull = null;
                if (upload.Ok)
                    pull = await _pullClient.PullAllAsync(100);

                var ok = upload.Ok && (pull == null || pull.Ok);
                var message = upload.Message + (pull == null ? "" : " " + pull.Message);
                if (ok)
                    SyncRuntimeState.RecordSuccess(message);
                else
                    SyncRuntimeState.RecordFailure(message);

                await RefreshStatusAsync();

                if (!ok)
                    UIHelper.ShowError(message, "Sync Status");
                else if (upload.SentCount > 0 || (pull != null && pull.AppliedCount > 0))
                    MessageBox.Show(message, "Sync Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SyncRuntimeState.RecordFailure("Manual sync failed: " + ex.Message);
                UIHelper.ShowError(ex.Message, "Sync Status");
                await RefreshStatusAsync();
            }
            finally
            {
                _syncInProgress = false;
                SetButtons(true);
            }
        }

        private async Task RunPreflightAsync()
        {
            try
            {
                SetButtons(false);
                _preflightValue.Text = "Checking local sync infrastructure...";
                _preflightValue.ForeColor = UiTheme.Muted;

                var result = await _preflightService.RunAsync();
                _preflightValue.Text = result.Summary;
                _preflightValue.ForeColor = result.IsReady ? UiTheme.Success : Color.FromArgb(190, 18, 60);

                var icon = result.IsReady ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
                MessageBox.Show(result.ToReportText(), "Sync Preflight", MessageBoxButtons.OK, icon);
            }
            catch (Exception ex)
            {
                _preflightValue.Text = "Preflight failed: " + ex.Message;
                _preflightValue.ForeColor = Color.FromArgb(190, 18, 60);
                UIHelper.ShowError(ex.Message, "Sync Preflight");
            }
            finally
            {
                SetButtons(true);
            }
        }

        private async Task RefreshStatusAsync()
        {
            var summary = new kingdom_Preparatory_School_Management_System.Data.SyncOutboxSummary();
            try
            {
                summary = await _syncService.GetOutboxSummaryAsync();
            }
            catch (Exception ex)
            {
                SyncRuntimeState.RecordFailure("Could not read sync outbox: " + ex.Message);
            }

            _connectionValue.Text = AppConfig.Sync.IsConfigured ? "Ready" : "Setup Needed";
            _connectionValue.ForeColor = AppConfig.Sync.IsConfigured ? UiTheme.Success : Color.FromArgb(190, 18, 60);
            _pendingValue.Text = summary.PendingCount.ToString("N0");
            _problemValue.Text = summary.ProblemCount.ToString("N0");
            _problemValue.ForeColor = summary.ProblemCount > 0 ? Color.FromArgb(190, 18, 60) : UiTheme.Success;
            _nextRunValue.Text = SyncRuntimeState.IsRunning
                ? "Running"
                : SyncRuntimeState.NextScheduledAt.HasValue
                    ? SyncRuntimeState.NextScheduledAt.Value.ToString("h:mm tt")
                    : "Not scheduled";
            _endpointValue.Text = string.IsNullOrWhiteSpace(AppConfig.Sync.EndpointBaseUrl)
                ? "No web endpoint configured"
                : AppConfig.Sync.EndpointBaseUrl;
            _authValue.Text = string.IsNullOrWhiteSpace(AppConfig.Sync.ApiKey)
                ? "API key missing"
                : "API key saved";
            _authValue.ForeColor = string.IsNullOrWhiteSpace(AppConfig.Sync.ApiKey)
                ? Color.FromArgb(190, 18, 60)
                : UiTheme.Text;
            _schedulerValue.Text = AppConfig.Sync.IsConfigured
                ? "Automatic every " + FormatInterval(SyncRuntimeState.DefaultInterval) + "; manual Sync Now is available."
                : "Automatic sync is paused until endpoint and API key are configured.";
            _lastRunValue.Text = SyncRuntimeState.LastAttemptAt.HasValue
                ? SyncRuntimeState.LastAttemptAt.Value.ToString("dd MMM yyyy h:mm tt")
                : "Not yet";
            _lastResultValue.Text = SyncRuntimeState.LastMessage;
            _lastResultValue.ForeColor = SyncRuntimeState.LastAttemptSucceeded ? UiTheme.Text : Color.FromArgb(190, 18, 60);
            _latestErrorValue.Text = string.IsNullOrWhiteSpace(summary.LatestError)
                ? "No pending outbox errors."
                : summary.LatestError;
            _latestErrorValue.ForeColor = string.IsNullOrWhiteSpace(summary.LatestError) ? UiTheme.Text : Color.FromArgb(190, 18, 60);
            _viewProblemsButton.Enabled = summary.ProblemCount > 0 && !_syncInProgress;
            _syncNowButton.Enabled = AuthService.CanWrite("Admin.Sync.Run") && AppConfig.Sync.IsConfigured && !_syncInProgress;
        }

        private static string FormatInterval(TimeSpan value)
        {
            if (value.TotalMinutes >= 1)
                return ((int)value.TotalMinutes).ToString("N0") + " minute(s)";
            return ((int)value.TotalSeconds).ToString("N0") + " second(s)";
        }

        private void SetButtons(bool enabled)
        {
            _refreshButton.Enabled = enabled;
            _viewProblemsButton.Enabled = enabled;
            _preflightButton.Enabled = enabled;
            _configureButton.Enabled = enabled && AuthService.CanWrite("Settings.Sync.Manage");
            _syncNowButton.Enabled = enabled && AuthService.CanWrite("Admin.Sync.Run") && AppConfig.Sync.IsConfigured;
        }
    }
}
