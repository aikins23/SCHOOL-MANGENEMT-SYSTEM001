using System;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Email Settings Configuration Form
    /// Allows users to configure SMTP settings for email notifications.
    /// </summary>
    public class frmEmailSettings : Form
    {
        private GroupBox grpSmtpSettings;
        private Label lblSmtpServer;
        private TextBox txtSmtpServer;
        private Label lblSmtpPort;
        private NumericUpDown numSmtpPort;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblFromEmail;
        private TextBox txtFromEmail;
        private CheckBox chkUseSSL;

        private Button btnTestEmail;
        private Button btnSave;
        private Button btnCancel;

        private Label lblStatus;

        // SMS settings
        private GroupBox grpSmsSettings;
        private CheckBox chkSmsEnabled;
        private Label lblSmsProvider;
        private ComboBox cmbSmsProvider;
        private Label lblSmsApiKey;
        private TextBox txtSmsApiKey;
        private Label lblSmsAbbr;
        private TextBox txtSmsAbbr;
        private Label lblSmsSenderPreview;
        private Label lblSmsTestPhone;
        private TextBox txtSmsTestPhone;
        private Button btnTestSms;
        private Label lblHrEmail;
        private TextBox txtHrEmail;
        private Label lblHrPhone;
        private TextBox txtHrPhone;

        public frmEmailSettings()
        {
            InitializeComponent();
            if (!AuthService.RequireAccess("frmEmailSettings", this)) return;
            ApplyTheme();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Email Settings";
            this.Size = new System.Drawing.Size(500, 877);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            int padding = 15;
            int labelWidth = 120;
            int controlWidth = this.ClientSize.Width - (padding * 2) - labelWidth - 15;
            int y = padding;
            int controlHeight = 25;

            // ─── SMTP Settings Group ───────────────────────────
            grpSmtpSettings = new GroupBox();
            grpSmtpSettings.Text = "SMTP Settings";
            grpSmtpSettings.Location = new System.Drawing.Point(padding, y);
            grpSmtpSettings.Size = new System.Drawing.Size(this.ClientSize.Width - (padding * 2), 280);
            grpSmtpSettings.Padding = new Padding(15);

            int gy = 20;

            // SMTP Server
            lblSmtpServer = new Label();
            lblSmtpServer.Text = "SMTP Server:";
            lblSmtpServer.Location = new System.Drawing.Point(15, gy);
            lblSmtpServer.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmtpSettings.Controls.Add(lblSmtpServer);

            txtSmtpServer = new TextBox();
            txtSmtpServer.Location = new System.Drawing.Point(15 + labelWidth + 10, gy);
            txtSmtpServer.Size = new System.Drawing.Size(controlWidth, controlHeight);
            txtSmtpServer.Text = "smtp.gmail.com";
            grpSmtpSettings.Controls.Add(txtSmtpServer);
            gy += controlHeight + 12;

            // SMTP Port
            lblSmtpPort = new Label();
            lblSmtpPort.Text = "SMTP Port:";
            lblSmtpPort.Location = new System.Drawing.Point(15, gy);
            lblSmtpPort.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmtpSettings.Controls.Add(lblSmtpPort);

            numSmtpPort = new NumericUpDown();
            numSmtpPort.Location = new System.Drawing.Point(15 + labelWidth + 10, gy);
            numSmtpPort.Size = new System.Drawing.Size(100, controlHeight);
            numSmtpPort.Minimum = 1;
            numSmtpPort.Maximum = 65535;
            numSmtpPort.Value = 587;
            grpSmtpSettings.Controls.Add(numSmtpPort);
            gy += controlHeight + 12;

            // Username
            lblUsername = new Label();
            lblUsername.Text = "Username:";
            lblUsername.Location = new System.Drawing.Point(15, gy);
            lblUsername.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmtpSettings.Controls.Add(lblUsername);

            txtUsername = new TextBox();
            txtUsername.Location = new System.Drawing.Point(15 + labelWidth + 10, gy);
            txtUsername.Size = new System.Drawing.Size(controlWidth, controlHeight);
            txtUsername.Text = "";
            grpSmtpSettings.Controls.Add(txtUsername);
            gy += controlHeight + 12;

            // Password
            lblPassword = new Label();
            lblPassword.Text = "Password:";
            lblPassword.Location = new System.Drawing.Point(15, gy);
            lblPassword.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmtpSettings.Controls.Add(lblPassword);

            txtPassword = new TextBox();
            txtPassword.Location = new System.Drawing.Point(15 + labelWidth + 10, gy);
            txtPassword.Size = new System.Drawing.Size(controlWidth, controlHeight);
            txtPassword.PasswordChar = '*';
            txtPassword.Text = "";
            grpSmtpSettings.Controls.Add(txtPassword);
            gy += controlHeight + 12;

            // From Email
            lblFromEmail = new Label();
            lblFromEmail.Text = "From Email:";
            lblFromEmail.Location = new System.Drawing.Point(15, gy);
            lblFromEmail.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmtpSettings.Controls.Add(lblFromEmail);

            txtFromEmail = new TextBox();
            txtFromEmail.Location = new System.Drawing.Point(15 + labelWidth + 10, gy);
            txtFromEmail.Size = new System.Drawing.Size(controlWidth, controlHeight);
            txtFromEmail.Text = "noreply@kingdomprep.edu.gh";
            grpSmtpSettings.Controls.Add(txtFromEmail);
            gy += controlHeight + 12;

            // Use SSL
            chkUseSSL = new CheckBox();
            chkUseSSL.Text = "Use SSL/TLS";
            chkUseSSL.Location = new System.Drawing.Point(15, gy);
            chkUseSSL.Size = new System.Drawing.Size(200, controlHeight);
            chkUseSSL.Checked = true;
            grpSmtpSettings.Controls.Add(chkUseSSL);

            this.Controls.Add(grpSmtpSettings);

            // ─── SMS Settings Group ───────────────────────────
            int sy = grpSmtpSettings.Bottom + 12;
            grpSmsSettings = new GroupBox();
            grpSmsSettings.Text = "SMS Notifications";
            grpSmsSettings.Location = new System.Drawing.Point(padding, sy);
            grpSmsSettings.Size = new System.Drawing.Size(this.ClientSize.Width - (padding * 2), 367);
            grpSmsSettings.Padding = new Padding(15);

            int my = 22;
            chkSmsEnabled = new CheckBox();
            chkSmsEnabled.Text = "Enable SMS sending (off = log only)";
            chkSmsEnabled.Location = new System.Drawing.Point(15, my);
            chkSmsEnabled.Size = new System.Drawing.Size(controlWidth + labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(chkSmsEnabled);
            my += controlHeight + 12;

            lblSmsProvider = new Label();
            lblSmsProvider.Text = "Provider:";
            lblSmsProvider.Location = new System.Drawing.Point(15, my);
            lblSmsProvider.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsProvider);

            cmbSmsProvider = new ComboBox();
            cmbSmsProvider.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSmsProvider.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            cmbSmsProvider.Size = new System.Drawing.Size(160, controlHeight);
            cmbSmsProvider.Items.AddRange(new object[] { "Arkesel", "BulkSMSGh" });
            grpSmsSettings.Controls.Add(cmbSmsProvider);
            my += controlHeight + 12;

            lblSmsApiKey = new Label();
            lblSmsApiKey.Text = "API Key:";
            lblSmsApiKey.Location = new System.Drawing.Point(15, my);
            lblSmsApiKey.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsApiKey);

            txtSmsApiKey = new TextBox();
            txtSmsApiKey.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtSmsApiKey.Size = new System.Drawing.Size(controlWidth, controlHeight);
            txtSmsApiKey.UseSystemPasswordChar = true;
            grpSmsSettings.Controls.Add(txtSmsApiKey);
            my += controlHeight + 12;

            lblSmsAbbr = new Label();
            lblSmsAbbr.Text = "School Abbrev.:";
            lblSmsAbbr.Location = new System.Drawing.Point(15, my);
            lblSmsAbbr.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsAbbr);

            txtSmsAbbr = new TextBox();
            txtSmsAbbr.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtSmsAbbr.Size = new System.Drawing.Size(120, controlHeight);
            txtSmsAbbr.CharacterCasing = CharacterCasing.Upper;
            txtSmsAbbr.MaxLength = 5;
            txtSmsAbbr.TextChanged += (s, e) => UpdateSenderPreview();
            grpSmsSettings.Controls.Add(txtSmsAbbr);
            my += controlHeight + 8;

            lblSmsSenderPreview = new Label();
            lblSmsSenderPreview.Location = new System.Drawing.Point(15, my);
            lblSmsSenderPreview.Size = new System.Drawing.Size(controlWidth + labelWidth, controlHeight + 6);
            lblSmsSenderPreview.ForeColor = System.Drawing.Color.DimGray;
            grpSmsSettings.Controls.Add(lblSmsSenderPreview);
            my += controlHeight + 14;

            lblHrEmail = new Label();
            lblHrEmail.Text = "HR Email:";
            lblHrEmail.Location = new System.Drawing.Point(15, my);
            lblHrEmail.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblHrEmail);

            txtHrEmail = new TextBox();
            txtHrEmail.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtHrEmail.Size = new System.Drawing.Size(controlWidth, controlHeight);
            grpSmsSettings.Controls.Add(txtHrEmail);
            my += controlHeight + 10;

            lblHrPhone = new Label();
            lblHrPhone.Text = "HR Phone:";
            lblHrPhone.Location = new System.Drawing.Point(15, my);
            lblHrPhone.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblHrPhone);

            txtHrPhone = new TextBox();
            txtHrPhone.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtHrPhone.Size = new System.Drawing.Size(140, controlHeight);
            grpSmsSettings.Controls.Add(txtHrPhone);
            my += controlHeight + 12;

            lblSmsTestPhone = new Label();
            lblSmsTestPhone.Text = "Test phone:";
            lblSmsTestPhone.Location = new System.Drawing.Point(15, my);
            lblSmsTestPhone.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsTestPhone);

            txtSmsTestPhone = new TextBox();
            txtSmsTestPhone.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtSmsTestPhone.Size = new System.Drawing.Size(140, controlHeight);
            grpSmsSettings.Controls.Add(txtSmsTestPhone);

            btnTestSms = new Button();
            btnTestSms.Text = "Send Test SMS";
            btnTestSms.Location = new System.Drawing.Point(15 + labelWidth + 10 + 150, my - 1);
            btnTestSms.Size = new System.Drawing.Size(130, controlHeight + 2);
            btnTestSms.Click += btnTestSms_Click;
            grpSmsSettings.Controls.Add(btnTestSms);

            this.Controls.Add(grpSmsSettings);

            // ─── Buttons (anchored below SMS group) ────────────
            int by = grpSmsSettings.Bottom + 15;

            btnTestEmail = new Button();
            btnTestEmail.Text = "🧪 Test Email Configuration";
            btnTestEmail.Location = new System.Drawing.Point(padding, by);
            btnTestEmail.Size = new System.Drawing.Size(this.ClientSize.Width - (padding * 2), 40);
            btnTestEmail.Click += BtnTestEmail_Click;
            btnTestEmail.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            btnTestEmail.ForeColor = System.Drawing.Color.White;
            btnTestEmail.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnTestEmail.FlatStyle = FlatStyle.Flat;
            btnTestEmail.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnTestEmail);
            by += 50;

            // Status Label
            lblStatus = new Label();
            lblStatus.Text = "Status: Not configured";
            lblStatus.Location = new System.Drawing.Point(padding, by);
            lblStatus.Size = new System.Drawing.Size(this.ClientSize.Width - (padding * 2), 25);
            lblStatus.Font = new System.Drawing.Font("Arial", 9);
            this.Controls.Add(lblStatus);
            by += 30;

            // Save Button
            btnSave = new Button();
            btnSave.Text = "💾 Save Settings";
            btnSave.Location = new System.Drawing.Point(padding, by);
            btnSave.Size = new System.Drawing.Size((this.ClientSize.Width - (padding * 3)) / 2, 40);
            btnSave.Click += BtnSave_Click;
            btnSave.BackColor = System.Drawing.Color.FromArgb(22, 163, 74);
            btnSave.ForeColor = System.Drawing.Color.White;
            btnSave.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnSave);

            // Cancel Button
            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new System.Drawing.Point(padding + (this.ClientSize.Width - (padding * 3)) / 2 + 15, by);
            btnCancel.Size = new System.Drawing.Size((this.ClientSize.Width - (padding * 3)) / 2, 40);
            btnCancel.Click += (s, e) => this.Close();
            btnCancel.BackColor = System.Drawing.Color.FromArgb(107, 114, 128);
            btnCancel.ForeColor = System.Drawing.Color.White;
            btnCancel.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnCancel);
        }

        private void ApplyTheme()
        {
            this.BackColor = UiTheme.Page;

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is GroupBox gb)
                {
                    gb.BackColor = UiTheme.Page;
                    gb.ForeColor = UiTheme.Text;
                    gb.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
                }
                else if (ctrl is Label lbl)
                {
                    lbl.BackColor = System.Drawing.Color.Transparent;
                    lbl.ForeColor = UiTheme.Text;
                }
                else if (ctrl is TextBox || ctrl is NumericUpDown)
                {
                    ctrl.BackColor = System.Drawing.Color.White;
                    ctrl.ForeColor = UiTheme.Text;
                }
                else if (ctrl is CheckBox chk)
                {
                    chk.BackColor = System.Drawing.Color.Transparent;
                    chk.ForeColor = UiTheme.Text;
                }
            }
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
            try
            {
                // Persist SMS settings first, independently of email validation, so
                // SMS-only configuration can be saved even when SMTP fields are blank.
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

                // Save to settings
                Properties.Settings.Default.SmtpServer = txtSmtpServer.Text;
                Properties.Settings.Default.SmtpPort = (int)numSmtpPort.Value;
                Properties.Settings.Default.SmtpUsername = txtUsername.Text;
                Properties.Settings.Default.SmtpPassword = SecretStorage.Protect(txtPassword.Text);
                Properties.Settings.Default.FromEmail = txtFromEmail.Text;
                Properties.Settings.Default.UseSSL = chkUseSSL.Checked;
                Properties.Settings.Default.Save();

                lblStatus.Text = "Status: ✓ Settings saved successfully";
                MessageBox.Show("Email settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnTestEmail_Click(object sender, EventArgs e)
        {
            // First save the settings temporarily
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
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(22, 163, 74);
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
                    lblStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
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
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
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
            // Reference SmsSenderIds so the preview always matches what is actually sent.
            string student  = abbr + SmsSenderIds.StudentSuffix;
            string employee = abbr + SmsSenderIds.EmployeeSuffix;
            string fees     = abbr + SmsSenderIds.FeeSuffix;
            string warn = student.Length > 11 ? "  ⚠ exceeds 11 chars" : "";
            lblSmsSenderPreview.Text = $"Sender IDs: {student} · {employee} · {fees}{warn}";
        }

        private async void btnTestSms_Click(object sender, EventArgs e)
        {
            // Persist current SMS fields first so the test uses what the user typed.
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
                    lblStatus.ForeColor = result.Success ? System.Drawing.Color.Green : System.Drawing.Color.Red;
                }
            }
            finally { btnTestSms.Enabled = true; }
        }
    }
}
