using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Shows leave balance per employee for the current school term.
    /// </summary>
    public class frmLeaveBalanceReport : Form
    {
        private readonly LeaveService _leaveService;
        private Label lblHeader;
        private Label lblTerm;
        private DataGridView grid;
        private Button btnRefresh;
        private Button btnClose;
        private NumericUpDown numEntitlement;
        private Label lblEntitlement;
        private Button btnSaveEntitlement;

        public frmLeaveBalanceReport()
        {
            var repo = new LeaveRepository(AppConfig.ConnectionString);
            _leaveService = new LeaveService(repo);
            BuildUI();
            LoadReport();
        }

        private void BuildUI()
        {
            Text = "Leave Balance Report";
            Size = new Size(880, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.Page;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(700, 400);

            lblHeader = new Label
            {
                Text = "📊 Leave Balance — Current Term",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                Location = new Point(20, 16),
                Size = new Size(600, 28)
            };
            Controls.Add(lblHeader);

            lblTerm = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10F),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, 48),
                Size = new Size(600, 22)
            };
            Controls.Add(lblTerm);

            lblEntitlement = new Label
            {
                Text = "Days per term:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = UiTheme.Text,
                Location = new Point(20, 80),
                Size = new Size(95, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblEntitlement);

            numEntitlement = new NumericUpDown
            {
                Location = new Point(115, 80),
                Size = new Size(60, 24),
                Minimum = 1,
                Maximum = 365,
                Value = Math.Max(1, Math.Min(365, AppConfig.Leave.DaysPerTerm))
            };
            Controls.Add(numEntitlement);

            btnSaveEntitlement = new Button
            {
                Text = "💾 Save",
                Location = new Point(180, 78),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSaveEntitlement.FlatAppearance.BorderSize = 0;
            btnSaveEntitlement.Click += (s, e) =>
            {
                AppConfig.Leave.DaysPerTerm = (int)numEntitlement.Value;
                LoadReport();
            };
            Controls.Add(btnSaveEntitlement);

            grid = new DataGridView
            {
                Location = new Point(20, 120),
                Size = new Size(820, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 99, 198);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            Controls.Add(grid);

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(20, 480),
                Size = new Size(110, 36),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadReport();
            Controls.Add(btnRefresh);

            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(730, 480),
                Size = new Size(110, 36),
                BackColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private async void LoadReport()
        {
            try
            {
                var term = AppConfig.Leave.CurrentTerm;
                lblTerm.Text =
                    $"{term.TermName}  ({term.Start:MMM dd, yyyy} – {term.End:MMM dd, yyyy})   " +
                    $"·   Entitlement: {AppConfig.Leave.DaysPerTerm} days";

                var table = await _leaveService.GetLeaveBalanceReportAsync();
                grid.DataSource = table;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load leave balance report:\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
