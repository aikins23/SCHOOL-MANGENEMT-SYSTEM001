using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmSystemRecovery : Form
    {
        private readonly SchoolInfoRepository _repo = new SchoolInfoRepository(AppConfig.ConnectionString);
        private dynamic _health;

        private Label _schoolName;
        private Label _schoolId;
        private Label _setupDate;
        private Label _status;
        private TextBox _username;
        private TextBox _password;
        private TextBox _confirmPassword;
        private TextBox _confirmation;
        private Button _recoverButton;

        public frmSystemRecovery()
        {
            BuildView();
            Load += async (s, e) => await LoadHealthAsync();
        }

        private void BuildView()
        {
            Text = "System Recovery";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(860, 560);
            MinimumSize = new Size(860, 560);
            BackColor = AppConfig.Colors.PageBackColor;
            Font = new Font("Segoe UI", 10F);
            Icon = Branding.AppIcon;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(BuildBrandPanel(), 0, 0);
            root.Controls.Add(BuildRecoveryPanel(), 1, 0);
        }

        private Control BuildBrandPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(34, 42, 30, 34) };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(panel.ClientRectangle, UiTheme.NavyDark, Color.FromArgb(2, 6, 23), LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(brush, panel.ClientRectangle);
                using (var gold = new SolidBrush(UiTheme.Gold))
                    e.Graphics.FillRectangle(gold, panel.Width - 3, 0, 3, panel.Height);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            panel.Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "NYANSAPO ERP",
                ForeColor = UiTheme.Gold,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "System\nRecovery",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Create a replacement administrator without changing the permanent School ID or touching school records.",
                ForeColor = Color.FromArgb(191, 203, 224),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.TopLeft
            }, 0, 2);

            layout.Controls.Add(new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Branding.GetLogo(onBlue: true),
                BackColor = Color.Transparent,
                Padding = new Padding(18)
            }, 0, 3);

            var note = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Recovery is audited. Use it only when all login accounts are missing.",
                ForeColor = Color.FromArgb(191, 203, 224),
                BackColor = Color.FromArgb(24, 33, 78),
                Padding = new Padding(18, 12, 18, 12),
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(note, 0, 4);

            return panel;
        }

        private Control BuildRecoveryPanel()
        {
            var shell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(46, 38, 46, 34), BackColor = AppConfig.Colors.PageBackColor };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            shell.Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Account Recovery",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);

            layout.Controls.Add(BuildHealthCard(), 0, 1);
            layout.Controls.Add(BuildAccountCard(), 0, 2);

            _status = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(_status, 0, 3);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            _recoverButton = ActionButton("Create Admin", UiTheme.Navy, Color.White);
            _recoverButton.Width = 180;
            _recoverButton.Click += async (s, e) => await RecoverAsync();
            actions.Controls.Add(_recoverButton);

            var cancel = ActionButton("Cancel", Color.White, UiTheme.Text, true);
            cancel.Width = 120;
            cancel.Click += (s, e) => Close();
            actions.Controls.Add(cancel);
            layout.Controls.Add(actions, 0, 4);

            return shell;
        }

        private Control BuildHealthCard()
        {
            var card = Card(3);
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            _schoolName = ValueLabel();
            _schoolId = ValueLabel();
            _setupDate = ValueLabel();

            AddInfoRow(card, 0, "School", _schoolName);
            AddInfoRow(card, 1, "School ID", _schoolId);
            AddInfoRow(card, 2, "Setup completed", _setupDate);
            return card;
        }

        private Control BuildAccountCard()
        {
            var card = Card(6);
            card.Padding = new Padding(32, 28, 32, 26);
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            card.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Replacement Administrator",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            _username = TextInput();
            _password = TextInput();
            _confirmPassword = TextInput();
            _confirmation = TextInput();
            _password.UseSystemPasswordChar = true;
            _confirmPassword.UseSystemPasswordChar = true;

            AddField(card, "Admin username", _username, 1);
            AddField(card, "Password", _password, 2);
            AddField(card, "Confirm password", _confirmPassword, 3);
            AddField(card, "Type the school name to confirm", _confirmation, 4);

            return card;
        }

        private async Task LoadHealthAsync()
        {
            try
            {
                _health = await _repo.GetIdentityHealthAsync();
                _schoolName.Text = string.IsNullOrWhiteSpace(_health.SchoolName) ? "(unnamed school)" : _health.SchoolName;
                _schoolId.Text = _health.SchoolId == Guid.Empty ? "Missing" : _health.SchoolId.ToString("D").ToUpperInvariant();
                _setupDate.Text = _health.SetupCompletedAt.HasValue
                    ? _health.SetupCompletedAt.Value.ToString("dd MMM yyyy, HH:mm")
                    : "Not recorded";

                bool allowed = _health.SetupAuditFound && _health.UserCount == 0 && _health.SchoolId != Guid.Empty;
                _recoverButton.Enabled = allowed;
                _status.Text = allowed
                    ? "Recovery is available because setup is locked and no user accounts exist."
                    : "Recovery is not available for the current database state.";
            }
            catch (Exception ex)
            {
                _status.Text = "Recovery status could not be loaded.";
                _recoverButton.Enabled = false;
                UIHelper.ShowError("Could not load recovery status: " + ex.Message, "System Recovery");
            }
        }

        private async Task RecoverAsync()
        {
            if (!ValidateRecovery()) return;

            _recoverButton.Enabled = false;
            _status.Text = "Creating recovery checkpoint backup...";

            try
            {
                var latest = await _repo.GetIdentityHealthAsync();
                if (!latest.SetupAuditFound || latest.UserCount != 0 || latest.SchoolId == Guid.Empty)
                {
                    UIHelper.ShowWarning("Recovery is no longer available for this database state.", "System Recovery");
                    await LoadHealthAsync();
                    return;
                }

                var checkpoint = await DatabaseBackupService.CreateRecoveryCheckpointAsync();
                if (!checkpoint.Success)
                {
                    _status.Text = "Recovery stopped because the checkpoint backup failed.";
                    UIHelper.ShowError(
                        "Recovery cannot continue until a database checkpoint is created.\n\n" + checkpoint.Message,
                        "System Recovery");
                    _recoverButton.Enabled = true;
                    return;
                }

                _status.Text = "Checkpoint created. Repairing tenant data...";
                await _repo.RepairTenantDataAsync();

                _status.Text = "Creating replacement administrator...";

                var result = await AuthService.RegisterAsync(
                    _username.Text.Trim(),
                    _password.Text,
                    _confirmPassword.Text,
                    "Administrator");

                if (!result.Success)
                {
                    _status.Text = result.Message;
                    UIHelper.ShowWarning(result.Message, "System Recovery");
                    _recoverButton.Enabled = true;
                    return;
                }

                await _repo.RecordSystemRecoveryAsync(
                    _username.Text.Trim(),
                    "All user accounts missing after completed first-run setup. Checkpoint: " + checkpoint.BackupPath);
                UIHelper.ShowSuccess("Replacement administrator created. Sign in with the new account.", "System Recovery");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("System recovery failed", ex);
                _status.Text = "Recovery failed.";
                UIHelper.ShowError("Recovery failed: " + ex.Message, "System Recovery");
                _recoverButton.Enabled = true;
            }
        }

        private bool ValidateRecovery()
        {
            string validation = AuthService.ValidateRegistration(
                _username.Text.Trim(),
                _password.Text,
                _confirmPassword.Text,
                "Administrator");

            if (!string.IsNullOrEmpty(validation))
            {
                UIHelper.ShowWarning(validation, "System Recovery");
                return false;
            }

            string expected = ConfirmationPhrase();
            if (!string.Equals(Normalize(_confirmation.Text), Normalize(expected), StringComparison.OrdinalIgnoreCase))
            {
                UIHelper.ShowWarning("Type the school name exactly as shown before recovery can continue.", "System Recovery");
                _confirmation.Focus();
                return false;
            }

            return true;
        }

        private string ConfirmationPhrase()
        {
            if (_health != null && !string.IsNullOrWhiteSpace(_health.SchoolName))
                return _health.SchoolName.Trim();
            return _health == null ? "" : _health.SchoolId.ToString("D");
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Trim();
        }

        private static TableLayoutPanel Card(int rows)
        {
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = rows,
                ColumnCount = 1,
                BackColor = Color.White,
                Padding = new Padding(26, 18, 26, 18),
                Margin = new Padding(0, 0, 0, 18)
            };
            return card;
        }

        private static Label ValueLabel()
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
        }

        private static void AddInfoRow(TableLayoutPanel card, int row, string label, Control value)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            grid.Controls.Add(value, 1, 0);
            card.Controls.Add(grid, 0, row);
        }

        private static TextBox TextInput()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.2F),
                ForeColor = UiTheme.Text,
                BackColor = Color.White,
                Margin = new Padding(0)
            };
        }

        private static void AddField(TableLayoutPanel card, string label, TextBox input, int row)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 12)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            panel.Controls.Add(input, 0, 1);
            card.Controls.Add(panel, 0, row);
        }

        private static Button ActionButton(string text, Color back, Color fore, bool outlined = false)
        {
            var button = new Button
            {
                Text = text,
                Height = 42,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };
            button.FlatAppearance.BorderSize = outlined ? 1 : 0;
            if (outlined) button.FlatAppearance.BorderColor = UiTheme.Border;
            return button;
        }
    }
}
