using System;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Database Backup and Restore Management Form
    /// Allows users to create backups, restore from backups, and manage backup files.
    /// </summary>
    public class frmBackupManager : Form
    {
        private GroupBox grpBackupOps;
        private Button btnBackupNow;
        private Button btnOpenFolder;
        private Label lblBackupStatus;

        private GroupBox grpBackupList;
        private ListBox lstBackups;
        private Label lblBackupInfo;

        private GroupBox grpRestoreOps;
        private Button btnRestoreSelected;
        private Button btnDeleteSelected;

        private GroupBox grpAutoBackup;
        private CheckBox chkEnableAutoBackup;
        private Label lblAutoBackupTime;
        private NumericUpDown numAutoBackupHour;
        private Label lblHour;

        private Label lblLastBackup;
        private ProgressBar progressBar;

        private Button btnImport;

        public frmBackupManager()
        {
            InitializeComponent();
            ApplyTheme();
            RefreshBackupList();
            UpdateLastBackupLabel();
        }

        private void InitializeComponent()
        {
            this.Text = "Database Backup Manager";
            this.Size = new System.Drawing.Size(700, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            int padding = 15;
            int x = padding;
            int y = padding;
            int groupWidth = this.ClientSize.Width - (padding * 2);
            int labelHeight = 25;
            int buttonHeight = 40;
            int controlWidth = (groupWidth - 30) / 2;

            // ─── Backup Operations Group ───────────────────────────
            grpBackupOps = new GroupBox();
            grpBackupOps.Text = "Backup Operations";
            grpBackupOps.Location = new System.Drawing.Point(x, y);
            grpBackupOps.Size = new System.Drawing.Size(groupWidth, 150);
            grpBackupOps.Padding = new Padding(15);

            int btnWidth3 = (groupWidth - 60) / 3;

            btnBackupNow = new Button();
            btnBackupNow.Text = "📦 Create Backup";
            btnBackupNow.Location = new System.Drawing.Point(15, 30);
            btnBackupNow.Size = new System.Drawing.Size(btnWidth3, buttonHeight);
            btnBackupNow.Click += BtnBackupNow_Click;
            btnBackupNow.BackColor = System.Drawing.Color.FromArgb(22, 163, 74);
            btnBackupNow.ForeColor = System.Drawing.Color.White;
            btnBackupNow.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnBackupNow.FlatStyle = FlatStyle.Flat;
            btnBackupNow.FlatAppearance.BorderSize = 0;
            grpBackupOps.Controls.Add(btnBackupNow);

            btnImport = new Button();
            btnImport.Text = "📥 Import Backup";
            btnImport.Location = new System.Drawing.Point(15 + btnWidth3 + 15, 30);
            btnImport.Size = new System.Drawing.Size(btnWidth3, buttonHeight);
            btnImport.Click += BtnImport_Click;
            btnImport.BackColor = System.Drawing.Color.FromArgb(245, 158, 11);
            btnImport.ForeColor = System.Drawing.Color.White;
            btnImport.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnImport.FlatStyle = FlatStyle.Flat;
            btnImport.FlatAppearance.BorderSize = 0;
            grpBackupOps.Controls.Add(btnImport);

            btnOpenFolder = new Button();
            btnOpenFolder.Text = "📁 Open Folder";
            btnOpenFolder.Location = new System.Drawing.Point(15 + (btnWidth3 + 15) * 2, 30);
            btnOpenFolder.Size = new System.Drawing.Size(btnWidth3, buttonHeight);
            btnOpenFolder.Click += BtnOpenFolder_Click;
            btnOpenFolder.BackColor = System.Drawing.Color.FromArgb(59, 130, 246);
            btnOpenFolder.ForeColor = System.Drawing.Color.White;
            btnOpenFolder.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            grpBackupOps.Controls.Add(btnOpenFolder);

            lblBackupStatus = new Label();
            lblBackupStatus.Text = "Ready to backup";
            lblBackupStatus.Location = new System.Drawing.Point(15, 85);
            lblBackupStatus.Size = new System.Drawing.Size(groupWidth - 45, 35);
            lblBackupStatus.AutoSize = false;
            lblBackupStatus.Font = new System.Drawing.Font("Arial", 9);
            grpBackupOps.Controls.Add(lblBackupStatus);

            progressBar = new ProgressBar();
            progressBar.Location = new System.Drawing.Point(15, 45);
            progressBar.Size = new System.Drawing.Size(groupWidth - 45, 20);
            progressBar.Visible = false;
            grpBackupOps.Controls.Add(progressBar);

            this.Controls.Add(grpBackupOps);
            y += grpBackupOps.Height + padding;

            // ─── Backup List Group ───────────────────────────────
            grpBackupList = new GroupBox();
            grpBackupList.Text = "Available Backups";
            grpBackupList.Location = new System.Drawing.Point(x, y);
            grpBackupList.Size = new System.Drawing.Size(groupWidth, 250);
            grpBackupList.Padding = new Padding(15);

            lstBackups = new ListBox();
            lstBackups.Location = new System.Drawing.Point(15, 30);
            lstBackups.Size = new System.Drawing.Size(groupWidth - 45, 180);
            lstBackups.SelectedIndexChanged += LstBackups_SelectedIndexChanged;
            lstBackups.ItemHeight = 20;
            grpBackupList.Controls.Add(lstBackups);

            lblBackupInfo = new Label();
            lblBackupInfo.Text = "Select a backup to view details";
            lblBackupInfo.Location = new System.Drawing.Point(15, 215);
            lblBackupInfo.Size = new System.Drawing.Size(groupWidth - 45, 20);
            lblBackupInfo.Font = new System.Drawing.Font("Arial", 9);
            lblBackupInfo.AutoSize = false;
            grpBackupList.Controls.Add(lblBackupInfo);

            this.Controls.Add(grpBackupList);
            y += grpBackupList.Height + padding;

            // ─── Restore Operations Group ──────────────────────────
            grpRestoreOps = new GroupBox();
            grpRestoreOps.Text = "Restore Operations";
            grpRestoreOps.Location = new System.Drawing.Point(x, y);
            grpRestoreOps.Size = new System.Drawing.Size(groupWidth, 100);
            grpRestoreOps.Padding = new Padding(15);

            btnRestoreSelected = new Button();
            btnRestoreSelected.Text = "↩️  Restore Selected Backup";
            btnRestoreSelected.Location = new System.Drawing.Point(15, 30);
            btnRestoreSelected.Size = new System.Drawing.Size(controlWidth, buttonHeight);
            btnRestoreSelected.Click += BtnRestoreSelected_Click;
            btnRestoreSelected.BackColor = System.Drawing.Color.FromArgb(168, 85, 247);
            btnRestoreSelected.ForeColor = System.Drawing.Color.White;
            btnRestoreSelected.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnRestoreSelected.FlatStyle = FlatStyle.Flat;
            btnRestoreSelected.FlatAppearance.BorderSize = 0;
            grpRestoreOps.Controls.Add(btnRestoreSelected);

            btnDeleteSelected = new Button();
            btnDeleteSelected.Text = "🗑️  Delete Selected";
            btnDeleteSelected.Location = new System.Drawing.Point(15 + controlWidth + 15, 30);
            btnDeleteSelected.Size = new System.Drawing.Size(controlWidth, buttonHeight);
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            btnDeleteSelected.BackColor = System.Drawing.Color.FromArgb(220, 38, 38);
            btnDeleteSelected.ForeColor = System.Drawing.Color.White;
            btnDeleteSelected.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            btnDeleteSelected.FlatStyle = FlatStyle.Flat;
            btnDeleteSelected.FlatAppearance.BorderSize = 0;
            grpRestoreOps.Controls.Add(btnDeleteSelected);

            this.Controls.Add(grpRestoreOps);
            y += grpRestoreOps.Height + padding;

            // ─── Auto Backup Group ────────────────────────────────
            grpAutoBackup = new GroupBox();
            grpAutoBackup.Text = "Automatic Backup Schedule";
            grpAutoBackup.Location = new System.Drawing.Point(x, y);
            grpAutoBackup.Size = new System.Drawing.Size(groupWidth, 100);
            grpAutoBackup.Padding = new Padding(15);

            chkEnableAutoBackup = new CheckBox();
            chkEnableAutoBackup.Text = "Enable automatic daily backup";
            chkEnableAutoBackup.Location = new System.Drawing.Point(15, 30);
            chkEnableAutoBackup.Size = new System.Drawing.Size(300, 25);
            chkEnableAutoBackup.CheckedChanged += ChkEnableAutoBackup_CheckedChanged;
            grpAutoBackup.Controls.Add(chkEnableAutoBackup);

            lblAutoBackupTime = new Label();
            lblAutoBackupTime.Text = "Backup time:";
            lblAutoBackupTime.Location = new System.Drawing.Point(15, 60);
            lblAutoBackupTime.Size = new System.Drawing.Size(100, 25);
            lblAutoBackupTime.AutoSize = false;
            grpAutoBackup.Controls.Add(lblAutoBackupTime);

            numAutoBackupHour = new NumericUpDown();
            numAutoBackupHour.Location = new System.Drawing.Point(120, 60);
            numAutoBackupHour.Size = new System.Drawing.Size(60, 25);
            numAutoBackupHour.Minimum = 0;
            numAutoBackupHour.Maximum = 23;
            numAutoBackupHour.Value = 22; // Default 10 PM
            grpAutoBackup.Controls.Add(numAutoBackupHour);

            lblHour = new Label();
            lblHour.Text = ":00 (24-hour format)";
            lblHour.Location = new System.Drawing.Point(185, 60);
            lblHour.Size = new System.Drawing.Size(150, 25);
            lblHour.AutoSize = false;
            grpAutoBackup.Controls.Add(lblHour);

            this.Controls.Add(grpAutoBackup);
            y += grpAutoBackup.Height + padding;

            // ─── Last Backup Info ─────────────────────────────────
            lblLastBackup = new Label();
            lblLastBackup.Text = "Last backup: Never";
            lblLastBackup.Location = new System.Drawing.Point(x, y);
            lblLastBackup.Size = new System.Drawing.Size(groupWidth, 20);
            lblLastBackup.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Italic);
            this.Controls.Add(lblLastBackup);

            this.Padding = new Padding(0);
            this.AutoScroll = true;
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
                else if (ctrl is ListBox lb)
                {
                    lb.BackColor = System.Drawing.Color.White;
                    lb.ForeColor = UiTheme.Text;
                }
                else if (ctrl is CheckBox chk)
                {
                    chk.BackColor = System.Drawing.Color.Transparent;
                    chk.ForeColor = UiTheme.Text;
                }
            }
        }

        private void frmBackupManager_Load(object sender, EventArgs e)
        {
            RefreshBackupList();
            UpdateLastBackupLabel();
        }

        private async void BtnBackupNow_Click(object sender, EventArgs e)
        {
            btnBackupNow.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblBackupStatus.Text = "Creating backup... Please wait";

            try
            {
                var result = await DatabaseBackupService.CreateBackupAsync();

                if (result.Success)
                {
                    lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(22, 163, 74);
                    lblBackupStatus.Text = result.Message;
                    MessageBox.Show(result.Message, "Backup Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshBackupList();
                    UpdateLastBackupLabel();
                }
                else
                {
                    lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
                    lblBackupStatus.Text = result.Message;
                    MessageBox.Show(result.Message, "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
                lblBackupStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Backup error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                progressBar.Style = ProgressBarStyle.Continuous;
                btnBackupNow.Enabled = true;
            }
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            DatabaseBackupService.OpenBackupFolder();
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select backup file to import";
                dialog.Filter = "SQL Server backup files (*.bak)|*.bak|All files (*.*)|*.*";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var result = DatabaseBackupService.ImportBackup(dialog.FileName);

                if (result.Success)
                {
                    lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(22, 163, 74);
                    lblBackupStatus.Text = result.Message;
                    RefreshBackupList();
                    UpdateLastBackupLabel();
                }
                else
                {
                    lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
                    lblBackupStatus.Text = result.Message;
                    MessageBox.Show(result.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RefreshBackupList()
        {
            lstBackups.Items.Clear();
            var backups = DatabaseBackupService.GetBackupFiles();

            if (backups.Count == 0)
            {
                lstBackups.Items.Add("No backups available");
                lblBackupInfo.Text = "Create a backup to get started";
                return;
            }

            foreach (var backup in backups)
            {
                lstBackups.Items.Add(backup);
            }

            if (lstBackups.Items.Count > 0)
                lstBackups.SelectedIndex = 0;
        }

        private void LstBackups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstBackups.SelectedItem is Services.BackupFileInfo backup)
            {
                lblBackupInfo.Text = $"Size: {backup.SizeFormatted} | Created: {backup.CreatedDate:yyyy-MM-dd HH:mm:ss}";
            }
        }

        private async void BtnRestoreSelected_Click(object sender, EventArgs e)
        {
            var backup = lstBackups.SelectedItem as Services.BackupFileInfo;
            if (backup == null)
            {
                MessageBox.Show("Please select a backup to restore.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to restore from this backup?\n\n" +
                $"File: {backup.FileName}\n" +
                $"Created: {backup.CreatedDate:yyyy-MM-dd HH:mm:ss}\n\n" +
                $"This will replace the current database with the backup.",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            btnRestoreSelected.Enabled = false;
            lblBackupStatus.Text = "Restoring database... Please wait";

            try
            {
                var restoreResult = await DatabaseBackupService.RestoreBackupAsync(backup.FilePath);

                if (restoreResult.Success)
                {
                    lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(22, 163, 74);
                    lblBackupStatus.Text = restoreResult.Message;
                    MessageBox.Show(restoreResult.Message + "\n\nApplication will exit.", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                }
                else
                {
                    lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
                    lblBackupStatus.Text = restoreResult.Message;
                    MessageBox.Show(restoreResult.Message, "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblBackupStatus.ForeColor = System.Drawing.Color.FromArgb(220, 38, 38);
                lblBackupStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Restore error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRestoreSelected.Enabled = true;
            }
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            var backup = lstBackups.SelectedItem as Services.BackupFileInfo;
            if (backup == null)
            {
                MessageBox.Show("Please select a backup to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete this backup?\n\n{backup.FileName}\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            var deleteResult = DatabaseBackupService.DeleteBackup(backup.FilePath);

            if (deleteResult.Success)
            {
                MessageBox.Show("Backup deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshBackupList();
            }
            else
            {
                MessageBox.Show($"Could not delete backup: {deleteResult.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkEnableAutoBackup_CheckedChanged(object sender, EventArgs e)
        {
            numAutoBackupHour.Enabled = chkEnableAutoBackup.Checked;

            if (chkEnableAutoBackup.Checked)
            {
                DatabaseBackupService.InitializeAutoBackup((int)numAutoBackupHour.Value);
                lblAutoBackupTime.Text = $"Auto-backup enabled at {(int)numAutoBackupHour.Value}:00 daily";
            }
            else
            {
                DatabaseBackupService.StopAutoBackup();
                lblAutoBackupTime.Text = "Auto-backup disabled";
            }
        }

        private void UpdateLastBackupLabel()
        {
            var backups = DatabaseBackupService.GetBackupFiles();
            if (backups.Count > 0)
            {
                var lastBackup = backups[0];
                lblLastBackup.Text = $"Last backup: {lastBackup.CreatedDate:yyyy-MM-dd HH:mm:ss} ({lastBackup.SizeFormatted})";
            }
        }
    }
}
