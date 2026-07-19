using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using KingdomPrep.Shared.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmLeaveApproval : Form
    {
        private readonly LeaveService _leaveService;
        private DataGridView leaveGrid;
        private ComboBox statusFilter;
        private TextBox searchBox;
        private Label resultLabel;
        private DataTable leaveTable;

        // Grid column constants
        private const string IdColumnName = "ID";
        private const string NameColumnName = "NAME";
        private const string LeaveTypeColumnName = "LEAVE TYPE";
        private const string StartDateColumnName = "START DATE";
        private const string StatusColumnName = "STATUS";

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public frmLeaveApproval()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            if (!AuthService.RequireAccess("frmLeaveApproval", this)) return;

            // Initialize modern architecture
            var repository = new LeaveRepository(AppConfig.ConnectionString);
            var employeeService = new EmployeeService(new EmployeeRepository(AppConfig.ConnectionString));
            _leaveService = new LeaveService(repository, employeeService);

            BuildModernApprovalView();

            // Wire events commented-out in designer
            gunaPictureBox1.Click += gunaPictureBox1_Click;
            Load                  += frmLeaveApproval_Load;
        }

        private void BuildModernApprovalView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Leave Approval";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 720);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = PageBackColor, Padding = new Padding(26) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFilterBar(), 0, 1);
            root.Controls.Add(BuildGridShell(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 38, Text = "Leave Approval", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Review pending employee leave requests and update status", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            if (AuthService.CanWrite("Leave.Submit"))
            {
                actions.Controls.Add(CreatePrimaryButton("Apply Leave", () => new frmEmpLeave().Show()));
            }
            actions.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadLeaveRequests()));
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildFilterBar()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = SurfaceColor };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            searchBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), BorderStyle = BorderStyle.FixedSingle };
            searchBox.TextChanged += (sender, args) => ApplyFilters();
            statusFilter = new ComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            statusFilter.Items.AddRange(new object[] { "All statuses", "PENDING", "APPROVED", "REJECTED" });
            statusFilter.SelectedIndex = 1;
            statusFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();
            resultLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9.5F) };
            layout.Controls.Add(searchBox, 0, 0);
            layout.Controls.Add(statusFilter, 1, 0);
            layout.Controls.Add(CreateSecondaryButton("Clear", () => { searchBox.Text = ""; statusFilter.SelectedIndex = 1; ApplyFilters(); }), 2, 0);
            layout.Controls.Add(resultLabel, 3, 0);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildGridShell()
        {
            var shell = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(1) };
            leaveGrid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = SurfaceColor, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, EnableHeadersVisualStyles = false };
            UiTheme.StyleDataGrid(leaveGrid);
            shell.Controls.Add(leaveGrid);
            return shell;
        }

        private Control BuildActions()
        {
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = PageBackColor };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var approveBtn = CreatePrimaryButton("Approve", null);
            approveBtn.Click += async (sender, args) => await ApproveLeaveAsync();

            var rejectBtn = CreateDangerButton("Reject", null);
            rejectBtn.Click += async (sender, args) => await RejectLeaveAsync();

            var refreshBtn = CreateSecondaryButton("Refresh", null);
            refreshBtn.Click += async (sender, args) => await LoadLeaveRequests();

            if (AuthService.CanWrite("Leave.Approve"))
            {
                actions.Controls.Add(approveBtn, 0, 0);
                actions.Controls.Add(rejectBtn, 1, 0);
            }
            actions.Controls.Add(refreshBtn, 2, 0);

            return actions;
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            return button;
        }

        private Button CreateDangerButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = DangerColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button { Width = 112, Height = 36, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            if (action != null)
            {
                button.Click += (sender, args) => action();
            }
            return button;
        }

        private async Task LoadPendingLeavesAsync()
        {
            try
            {
                leaveTable = await _leaveService.GetLeaveRequestsTableAsync("PENDING");
                leaveGrid.DataSource = leaveTable;
                ApplyFilters();
                LoggerHelper.LogInfo($"Loaded {leaveTable.Rows.Count} pending leave requests");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading leave requests: " + ex.Message, "Leave Approval");
                LoggerHelper.LogError("LoadPendingLeavesAsync failed", ex);
            }
        }

        private async Task LoadLeaveRequests()
        {
            try
            {
                leaveTable = await _leaveService.GetLeaveRequestsTableAsync();
                leaveGrid.DataSource = leaveTable;
                ApplyFilters();
                LoggerHelper.LogInfo("Loaded all leave requests");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading leave requests: " + ex.Message, "Leave Approval");
                LoggerHelper.LogError("LoadLeaveRequests failed", ex);
            }
        }

        private void ApplyFilters()
        {
            if (leaveTable == null) return;
            var filters = new System.Collections.Generic.List<string>();
            string search = searchBox.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(search))
            {
                filters.Add("[NAME] LIKE '%" + search + "%' OR Convert([ID], 'System.String') LIKE '%" + search + "%'");
            }
            if (statusFilter.SelectedIndex > 0)
            {
                filters.Add("[STATUS] = '" + statusFilter.Text + "'");
            }
            leaveTable.DefaultView.RowFilter = string.Join(" AND ", filters);
            resultLabel.Text = leaveTable.DefaultView.Count + " leave request(s)";
        }

        private async Task ApproveLeaveAsync()
        {
            try
            {
                if (!AuthService.RequireWriteAccess("Leave.Approve", "Approve Leave"))
                    return;

                if (leaveGrid.CurrentRow == null)
                {
                    ConfirmationHelper.ShowWarning("Please select a leave request to approve.", "Leave Approval");
                    return;
                }

                string employeeName = leaveGrid.CurrentRow.Cells[NameColumnName].Value?.ToString() ?? "Unknown";
                string leaveType = leaveGrid.CurrentRow.Cells[LeaveTypeColumnName].Value?.ToString() ?? "Unknown";

                if (!ConfirmationHelper.ConfirmSave($"Approve {leaveType} for {employeeName}?"))
                {
                    LoggerHelper.LogInfo($"User cancelled approval for {employeeName}");
                    return;
                }

                var request = new LeaveRequest
                {
                    EmployeeID = leaveGrid.CurrentRow.Cells[IdColumnName].Value?.ToString() ?? "",
                    StartDate = Convert.ToDateTime(leaveGrid.CurrentRow.Cells[StartDateColumnName].Value)
                };

                var (success, message) = await _leaveService.UpdateLeaveStatusAsync(request, "APPROVED");

                if (success)
                {
                    ConfirmationHelper.ShowInfo("Leave approved successfully.", "Leave Approval");
                    LoggerHelper.LogInfo($"Leave approved for {employeeName} ({request.EmployeeID})");
                    await LoadLeaveRequests();
                }
                else
                {
                    UIHelper.ShowError(message, "Leave Approval");
                    LoggerHelper.LogWarning($"Failed to approve leave: {message}");
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Approval failed: " + ex.Message, "Leave Approval");
                LoggerHelper.LogError("ApproveLeaveAsync failed", ex);
            }
        }

        private async Task RejectLeaveAsync()
        {
            try
            {
                if (!AuthService.RequireWriteAccess("Leave.Approve", "Reject Leave"))
                    return;

                if (leaveGrid.CurrentRow == null)
                {
                    ConfirmationHelper.ShowWarning("Please select a leave request to reject.", "Leave Approval");
                    return;
                }

                string employeeName = leaveGrid.CurrentRow.Cells[NameColumnName].Value?.ToString() ?? "Unknown";

                if (!ConfirmationHelper.ConfirmDelete("Leave Request",
                    $"Reject leave for {employeeName}?\n\nThis action cannot be undone."))
                {
                    LoggerHelper.LogInfo($"User cancelled rejection for {employeeName}");
                    return;
                }

                var request = new LeaveRequest
                {
                    EmployeeID = leaveGrid.CurrentRow.Cells[IdColumnName].Value?.ToString() ?? "",
                    StartDate = Convert.ToDateTime(leaveGrid.CurrentRow.Cells[StartDateColumnName].Value)
                };

                var (success, message) = await _leaveService.UpdateLeaveStatusAsync(request, "REJECTED");

                if (success)
                {
                    ConfirmationHelper.ShowInfo("Leave rejected successfully.", "Leave Approval");
                    LoggerHelper.LogInfo($"Leave rejected for {employeeName} ({request.EmployeeID})");
                    await LoadLeaveRequests();
                }
                else
                {
                    UIHelper.ShowError(message, "Leave Approval");
                    LoggerHelper.LogWarning($"Failed to reject leave: {message}");
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Rejection failed: " + ex.Message, "Leave Approval");
                LoggerHelper.LogError("RejectLeaveAsync failed", ex);
            }
        }

        private async void frmLeaveApproval_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadLeaveRequests();
                LoggerHelper.LogInfo("frmLeaveApproval loaded successfully");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading form: " + ex.Message, "Leave Approval");
                LoggerHelper.LogError("frmLeaveApproval_Load failed", ex);
            }
        }

        private async void btnApprove_Click(object sender, EventArgs e) { await ApproveLeaveAsync(); }
        private async void btnReject_Click(object sender, EventArgs e) { await RejectLeaveAsync(); }
        private async void btnRefresh_Click(object sender, EventArgs e) { await LoadLeaveRequests(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Close(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
    }
}
