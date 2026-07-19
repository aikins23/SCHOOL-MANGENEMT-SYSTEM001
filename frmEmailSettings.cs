using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmEmailSettings : Form
    {
        private bool CanManageEmailSettings => AuthService.CanWrite("Settings.Email.Manage");
        private Panel pnlHeader;
        private Label lblTitle;
        private Guna2ControlBox btnClose;

        private TextBox txtSmtpServer;
        private NumericUpDown numSmtpPort;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtFromEmail;
        private CheckBox chkUseSSL;

        private CheckBox chkSmsEnabled;
        private ComboBox cmbSmsProvider;
        private TextBox txtSmsApiKey;
        private TextBox txtSmsAbbr;
        private Label lblSmsSenderPreview;

        private TextBox txtHrEmail;
        private TextBox txtHrPhone;
        private TextBox txtSmsTestPhone;

        private Button btnTestEmail;
        private Button btnTestSms;
        private Button btnSave;
        private Button btnCancel;

        private Label lblStatus;

        public frmEmailSettings()
        {
            InitializeModernComponent();
            if (!AuthService.RequireAccess("frmEmailSettings", this)) return;
            LoadSettings();
        }

        private void InitializeModernComponent()
        {
            this.Size = new Size(600, 850);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = UiTheme.Page;
            this.ShowIcon = false;

            var formShadow = new Guna2ShadowForm(this);
            var elipse = new Guna2Elipse { TargetControl = this, BorderRadius = 12 };
            var dragControl = new Guna2DragControl { TargetControl = this };

            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.Navy };
            var headerDrag = new Guna2DragControl { TargetControl = pnlHeader };
            lblTitle = new Label
            {
                Text = "Notification Settings",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblTitle);

            this.Controls.Add(pnlHeader); // Add to form FIRST so it gets the correct Width of 600

            var btnMinimize = new Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox,
                FillColor = Color.Transparent,
                IconColor = Color.White,
                Location = new Point(pnlHeader.Width - 115, 12),
                Size = new Size(35, 35),
                Cursor = Cursors.Hand
            };
            pnlHeader.Controls.Add(btnMinimize);

            var btnMaximize = new Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MaximizeBox,
                FillColor = Color.Transparent,
                IconColor = Color.White,
                Location = new Point(pnlHeader.Width - 80, 12),
                Size = new Size(35, 35),
                Cursor = Cursors.Hand
            };
            pnlHeader.Controls.Add(btnMaximize);

            btnClose = new Guna2ControlBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FillColor = Color.Transparent,
                IconColor = Color.White,
                Location = new Point(pnlHeader.Width - 45, 12),
                Size = new Size(35, 35),
                Cursor = Cursors.Hand
            };
            pnlHeader.Controls.Add(btnClose);

            int x = 40;
            int y = 80;
            int w = 520;
            int inputH = 30;
            Font labelFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            Font inputFont = new Font("Segoe UI", 10F, FontStyle.Regular);

            // --- SMTP SETTINGS SECTION ---
            var lblSmtpTitle = new Label { Text = "SMTP Email Configuration", Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), ForeColor = UiTheme.NavyDark, Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent };
            this.Controls.Add(lblSmtpTitle);
            y += 40;

            this.Controls.Add(CreateLabel("SMTP Server:", x, y, labelFont));
            txtSmtpServer = CreateTextBox(x, y + 22, w, inputH, inputFont);
            this.Controls.Add(txtSmtpServer);
            y += 65;

            this.Controls.Add(CreateLabel("SMTP Port:", x, y, labelFont));
            numSmtpPort = new NumericUpDown { Location = new Point(x, y + 22), Size = new Size(120, inputH), Font = inputFont, Minimum = 1, Maximum = 65535, Value = 587 };
            this.Controls.Add(numSmtpPort);

            this.Controls.Add(CreateLabel("From Email:", x + 140, y, labelFont));
            txtFromEmail = CreateTextBox(x + 140, y + 22, w - 140, inputH, inputFont);
            this.Controls.Add(txtFromEmail);
            y += 65;

            this.Controls.Add(CreateLabel("Username:", x, y, labelFont));
            txtUsername = CreateTextBox(x, y + 22, (w - 20) / 2, inputH, inputFont);
            this.Controls.Add(txtUsername);

            this.Controls.Add(CreateLabel("Password:", x + (w / 2) + 10, y, labelFont));
            txtPassword = CreateTextBox(x + (w / 2) + 10, y + 22, (w - 20) / 2, inputH, inputFont, true);
            this.Controls.Add(txtPassword);
            y += 65;

            chkUseSSL = new CheckBox { Text = "Use SSL/TLS Encryption", Location = new Point(x, y), Size = new Size(250, 25), Font = labelFont, ForeColor = UiTheme.Text, Checked = true, Cursor = Cursors.Hand };
            this.Controls.Add(chkUseSSL);
            y += 40;

            var div1 = new Panel { Location = new Point(x, y), Size = new Size(w, 1), BackColor = UiTheme.Border };
            this.Controls.Add(div1);
            y += 20;

            // --- SMS SETTINGS SECTION ---
            var lblSmsTitle = new Label { Text = "SMS Notifications", Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), ForeColor = UiTheme.NavyDark, Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent };
            this.Controls.Add(lblSmsTitle);
            y += 40;

            chkSmsEnabled = new CheckBox { Text = "Enable SMS sending (off = log only)", Location = new Point(x, y), Size = new Size(300, 25), Font = labelFont, ForeColor = UiTheme.Text, Cursor = Cursors.Hand };
            this.Controls.Add(chkSmsEnabled);
            y += 40;

            this.Controls.Add(CreateLabel("Provider:", x, y, labelFont));
            cmbSmsProvider = new ComboBox { Location = new Point(x, y + 22), Size = new Size((w - 20) / 2, inputH), Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSmsProvider.Items.AddRange(new object[] { "Arkesel", "BulkSMSGh" });
            this.Controls.Add(cmbSmsProvider);

            this.Controls.Add(CreateLabel("School Abbreviation:", x + (w / 2) + 10, y, labelFont));
            txtSmsAbbr = CreateTextBox(x + (w / 2) + 10, y + 22, (w - 20) / 2, inputH, inputFont);
            txtSmsAbbr.MaxLength = 5;
            txtSmsAbbr.CharacterCasing = CharacterCasing.Upper;
            txtSmsAbbr.TextChanged += (s, e) => UpdateSenderPreview();
            this.Controls.Add(txtSmsAbbr);
            y += 65;

            this.Controls.Add(CreateLabel("API Key:", x, y, labelFont));
            txtSmsApiKey = CreateTextBox(x, y + 22, w, inputH, inputFont, true);
            this.Controls.Add(txtSmsApiKey);
            y += 60;

            lblSmsSenderPreview = new Label { Location = new Point(x, y), Size = new Size(w, 20), ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 8.5F), BackColor = Color.Transparent };
            this.Controls.Add(lblSmsSenderPreview);
            y += 35;

            this.Controls.Add(CreateLabel("HR Email:", x, y, labelFont));
            txtHrEmail = CreateTextBox(x, y + 22, (w - 20) / 2, inputH, inputFont);
            this.Controls.Add(txtHrEmail);

            this.Controls.Add(CreateLabel("HR Phone:", x + (w / 2) + 10, y, labelFont));
            txtHrPhone = CreateTextBox(x + (w / 2) + 10, y + 22, (w - 20) / 2, inputH, inputFont);
            this.Controls.Add(txtHrPhone);
            y += 65;

            this.Controls.Add(CreateLabel("Test Phone:", x, y, labelFont));
            txtSmsTestPhone = CreateTextBox(x, y + 22, 200, inputH, inputFont);
            this.Controls.Add(txtSmsTestPhone);

            btnTestSms = CreateButton("Test SMS", x + 215, y + 21, 120, 28, Color.White, UiTheme.NavyDark);
            btnTestSms.Click += btnTestSms_Click;
            this.Controls.Add(btnTestSms);
            y += 70;

            var div2 = new Panel { Location = new Point(x, y), Size = new Size(w, 1), BackColor = UiTheme.Border };
            this.Controls.Add(div2);
            y += 20;

            // --- ACTIONS ---
            lblStatus = new Label { Location = new Point(x, y), Size = new Size(w, 20), Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), ForeColor = UiTheme.Navy, BackColor = Color.Transparent };
            this.Controls.Add(lblStatus);
            y += 30;

            btnTestEmail = CreateButton("Test Email Configuration", x, y, w, 40, UiTheme.Navy, Color.White);
            btnTestEmail.Click += BtnTestEmail_Click;
            this.Controls.Add(btnTestEmail);
            y += 55;

            btnSave = CreateButton("Save Settings", x, y, (w - 15) / 2, 45, UiTheme.Success, Color.White);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = CreateButton("Cancel", x + (w / 2) + 8, y, (w - 15) / 2, 45, UiTheme.Muted, Color.White);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
            ApplyWriteAccess();
        }

        private Label CreateLabel(string text, int x, int y, Font font)
        {
            return new Label { Text = text, Location = new Point(x, y), Font = font, ForeColor = UiTheme.Text, AutoSize = true, BackColor = Color.Transparent };
        }

        private TextBox CreateTextBox(int x, int y, int w, int h, Font font, bool isPassword = false)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                Font = font,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = UiTheme.Text,
                BackColor = Color.White
            };
            if (isPassword)
            {
                txt.PasswordChar = '*';
                txt.UseSystemPasswordChar = true;
            }
            return txt;
        }

        private Button CreateButton(string text, int x, int y, int w, int h, Color fill, Color fore)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = fill,
                ForeColor = fore,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadSettings()
        {
            try
            {
                txtSmtpServer.Text = AppConfig.Email.SmtpServer;
                numSmtpPort.Value = AppConfig.Email.SmtpPort;
                txtUsername.Text = AppConfig.Email.SmtpUsername;
                txtPassword.Text = AppConfig.Email.SmtpPassword;
                txtFromEmail.Text = AppConfig.Email.FromEmail;
                chkUseSSL.Checked = AppConfig.Email.UseSSL;

                chkSmsEnabled.Checked = AppConfig.Sms.Enabled;
                cmbSmsProvider.SelectedItem = AppConfig.Sms.Provider == "BulkSMSGh" ? "BulkSMSGh" : "Arkesel";
                txtSmsApiKey.Text = AppConfig.Sms.ApiKey;
                txtSmsAbbr.Text = AppConfig.Sms.SchoolAbbreviation;
                UpdateSenderPreview();
                txtHrEmail.Text = AppConfig.Notify.HrEmail;
                txtHrPhone.Text = AppConfig.Notify.HrPhone;

                if (AppConfig.Email.IsConfigured)
                    lblStatus.Text = "Status: ✓ Configured";
                else
                    lblStatus.Text = "Status: ⚠ Not fully configured";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!AuthService.RequireWriteAccess("Settings.Email.Manage", "Save email and SMS settings")) return;
            try
            {
                AppConfig.Sms.Enabled = chkSmsEnabled.Checked;
                AppConfig.Sms.Provider = (cmbSmsProvider.SelectedItem?.ToString() ?? "Arkesel");
                AppConfig.Sms.ApiKey = txtSmsApiKey.Text.Trim();
                AppConfig.Sms.SchoolAbbreviation = txtSmsAbbr.Text.Trim();
                AppConfig.Notify.HrEmail = txtHrEmail.Text.Trim();
                AppConfig.Notify.HrPhone = txtHrPhone.Text.Trim();

                if (string.IsNullOrWhiteSpace(txtSmtpServer.Text))
                {
                    MessageBox.Show("SMTP Server is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Username is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Password is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Properties.Settings.Default.SmtpServer = txtSmtpServer.Text;
                Properties.Settings.Default.SmtpPort = (int)numSmtpPort.Value;
                Properties.Settings.Default.SmtpUsername = txtUsername.Text;
                Properties.Settings.Default.SmtpPassword = SecretStorage.Protect(txtPassword.Text);
                Properties.Settings.Default.FromEmail = txtFromEmail.Text;
                Properties.Settings.Default.UseSSL = chkUseSSL.Checked;
                Properties.Settings.Default.Save();

                lblStatus.Text = "Status: ✓ Settings saved successfully";
                MessageBox.Show("Email settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnTestEmail_Click(object sender, EventArgs e)
        {
            if (!AuthService.RequireWriteAccess("Settings.Email.Manage", "Test email settings")) return;
            Properties.Settings.Default.SmtpServer = txtSmtpServer.Text;
            Properties.Settings.Default.SmtpPort = (int)numSmtpPort.Value;
            Properties.Settings.Default.SmtpUsername = txtUsername.Text;
            Properties.Settings.Default.SmtpPassword = SecretStorage.Protect(txtPassword.Text);
            Properties.Settings.Default.FromEmail = txtFromEmail.Text;
            Properties.Settings.Default.UseSSL = chkUseSSL.Checked;

            btnTestEmail.Enabled = false;
            lblStatus.Text = "Status: 🔄 Sending test email...";

            try
            {
                string testEmail = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter your email address to receive the test email:",
                    "Test Email Configuration",
                    txtUsername.Text
                );

                if (string.IsNullOrWhiteSpace(testEmail))
                {
                    lblStatus.Text = "Status: Cancelled";
                    return;
                }

                var result = await NotificationService.TestEmailConfigurationAsync(testEmail);

                if (result.Success)
                {
                    lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                    lblStatus.Text = $"Status: ✓ Test email sent successfully to {testEmail}";
                    MessageBox.Show(
                        $"Test email sent successfully to {testEmail}.\n\nPlease check your inbox to verify the email configuration.",
                        "Test Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    lblStatus.Text = $"Status: ✗ {result.Message}";
                    MessageBox.Show(
                        $"Failed to send test email:\n\n{result.Message}",
                        "Test Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                lblStatus.Text = $"Status: ✗ {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestEmail.Enabled = true;
            }
        }

        private void UpdateSenderPreview()
        {
            string abbr = (txtSmsAbbr.Text ?? "").Trim().ToUpperInvariant();
            string student  = abbr + SmsSenderIds.StudentSuffix;
            string employee = abbr + SmsSenderIds.EmployeeSuffix;
            string fees     = abbr + SmsSenderIds.FeeSuffix;
            string warn = student.Length > 11 ? "  ⚠ exceeds 11 chars" : "";
            lblSmsSenderPreview.Text = $"Sender IDs: {student} · {employee} · {fees}{warn}";
        }

        private async void btnTestSms_Click(object sender, EventArgs e)
        {
            if (!AuthService.RequireWriteAccess("Settings.Email.Manage", "Test SMS settings")) return;
            AppConfig.Sms.Enabled = chkSmsEnabled.Checked;
            AppConfig.Sms.Provider = (cmbSmsProvider.SelectedItem?.ToString() ?? "Arkesel");
            AppConfig.Sms.ApiKey = txtSmsApiKey.Text.Trim();
            AppConfig.Sms.SchoolAbbreviation = txtSmsAbbr.Text.Trim();

            btnTestSms.Enabled = false;
            try
            {
                var result = await SmsService.SendTestAsync(txtSmsTestPhone.Text.Trim());
                if (lblStatus != null)
                {
                    lblStatus.Text = result.Message;
                    lblStatus.ForeColor = result.Success ? Color.Green : Color.Red;
                }
            }
            finally { btnTestSms.Enabled = true; }
        }

        private void ApplyWriteAccess()
        {
            bool canWrite = CanManageEmailSettings;
            foreach (var textBox in new[] { txtSmtpServer, txtUsername, txtPassword, txtFromEmail, txtSmsApiKey, txtSmsAbbr, txtHrEmail, txtHrPhone, txtSmsTestPhone })
                if (textBox != null) textBox.ReadOnly = !canWrite;

            if (numSmtpPort != null) numSmtpPort.Enabled = canWrite;
            if (chkUseSSL != null) chkUseSSL.Enabled = canWrite;
            if (chkSmsEnabled != null) chkSmsEnabled.Enabled = canWrite;
            if (cmbSmsProvider != null) cmbSmsProvider.Enabled = canWrite;
            if (btnSave != null) { btnSave.Enabled = canWrite; btnSave.Text = canWrite ? "Save Settings" : "Read only"; }
            if (btnTestEmail != null) btnTestEmail.Enabled = canWrite;
            if (btnTestSms != null) btnTestSms.Enabled = canWrite;
        }
    }
}
