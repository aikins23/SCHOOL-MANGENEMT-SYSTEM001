using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmDashboard : Form
    {
        private readonly DashboardService _dashboardService;
        private System.Windows.Forms.Timer _feeReminderTimer;
        private readonly Data.TransportRepository _transportRepo = new Data.TransportRepository(Common.AppConfig.ConnectionString);
        private StudentService _studentService;
        private FeeRepository _feeRepository;

        private Label studentCountLabel;
        private Label _incomeLabel, _expensesLabel, _fundLabel, _topExpenseLabel;
        private Label employeeCountLabel;
        private Label feesCollectedLabel;
        private Label feesBalanceLabel;
        private Label pendingLeaveLabel;
        private Label averageExamLabel;
        private Label topClassLabel;
        private Label statusLabel;
        private DataGridView recentPaymentsGrid;
        private DataGridView classSummaryGrid;
        private DataGridView leaveSummaryGrid;
        private ListView financeInsightList;

        // Admission-approvals notification (Accountant): count bubble on the nav tab + sign-in alert.
        private Button _approvalsNavBtn;
        private int _pendingApprovalsCount;
        private bool _approvalAlertShown;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SidebarBackColor = UiTheme.Navy;
        private static readonly Color SidebarHoverColor = UiTheme.NavyHover;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor  = UiTheme.Border;

        // ── Per-card accent colours ────────────────────────────────────────────
        private static readonly Color AccentBlue  = Color.FromArgb( 59, 130, 246);
        private static readonly Color AccentGreen = Color.FromArgb( 16, 185, 129);
        private static readonly Color AccentGold  = Color.FromArgb(212, 175,  55);
        private static readonly Color AccentRed   = Color.FromArgb(239,  68,  68);

public frmDashboard()
{
    LoggerHelper.LogInfo("frmDashboard ctor: InitializeComponent start");
    InitializeComponent();
    LoggerHelper.LogInfo("frmDashboard ctor: InitializeComponent complete");
    this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
    if (!AuthService.RequireAccess("frmDashboard", this)) return;

    // Initialize modern architecture
    LoggerHelper.LogInfo("frmDashboard ctor: services start");
    var repository = new DashboardRepository(AppConfig.ConnectionString);
    _dashboardService = new DashboardService(repository);

    _studentService = new StudentService(
        new StudentRepository(AppConfig.ConnectionString),
        new FeeRepository(AppConfig.ConnectionString));
    _feeRepository = new FeeRepository(AppConfig.ConnectionString);
    LoggerHelper.LogInfo("frmDashboard ctor: services complete");

    LoggerHelper.LogInfo("frmDashboard ctor: BuildModernDashboard start");
    BuildModernDashboard();
    LoggerHelper.LogInfo("frmDashboard ctor: BuildModernDashboard complete");
    ApplyRolePermissions();

    // Set this as the main dashboard in FormManager
    FormManager.SetMainDashboard(this);

    // Handle form closing to keep app alive
    this.FormClosing += FrmDashboard_FormClosing;

    // Weekly fee reminder timer — every 60 minutes
    _feeReminderTimer = new System.Windows.Forms.Timer();
    _feeReminderTimer.Interval = 60 * 60 * 1000;
    _feeReminderTimer.Tick += async (s, e) => { await CheckFeeRemindersAsync(); await CheckTransportRemindersAsync(); await Services.SmsOutboxService.FlushPendingAsync(); };

    // Load event is commented-out in designer — wire it manually
    this.Load += frmDashboard_Load;

    // Refresh the admission-approvals bubble whenever the dashboard regains focus
    // (e.g. after approving/rejecting in the approvals screen).
    this.Activated += async (s, e) => await RefreshApprovalNotificationAsync();
}

        private void OpenForm(Form form, bool hideDashboard = false)
        {
            if (form == null) return;

            try
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Show();
                form.BringToFront();
                if (hideDashboard) Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open " + form.Text + ": " + ex.Message, "Open Form Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenAddStudentDialog()
        {
            try
            {
                using (var form = new frmAddStd())
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ShowInTaskbar = false;
                    form.ShowDialog(this);
                }

                _ = LoadDashboardStatisticsAsync(true);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Open Add Student dialog failed", ex);
                MessageBox.Show("Could not open Add Student: " + ex.Message, "Open Form Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenAnalyticsDashboard()
        {
            var charts = new frmDashboardCharts();
            charts.Show();
            charts.BringToFront();
        }

        private void RunBackup()
        {
            // Open the dedicated backup manager (create / restore / list backups).
            new frmBackupManager().ShowDialog();
        }

        private void ViewLogs()
        {
            try
            {
                string logsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (System.IO.Directory.Exists(logsDir))
                    System.Diagnostics.Process.Start("explorer.exe", logsDir);
                else
                    UIHelper.ShowWarning("No logs found yet.", "System Logs");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not open logs: " + ex.Message, "System Logs");
            }
        }

        private async Task LoadDashboardStatisticsAsync(bool forceRefresh = false)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                statusLabel.Text = "Refreshing school analytics...";
                var metrics = await _dashboardService.GetMetricsAsync(forceRefresh);

                studentCountLabel.Text = metrics.StudentCount.ToString();
                employeeCountLabel.Text = metrics.EmployeeCount.ToString();
                pendingLeaveLabel.Text = metrics.PendingLeaveCount.ToString();
                if (AuthService.CurrentUser.Role == AuthService.UserRole.Headmaster)
                {
                    decimal totalExpected = metrics.TotalFeesCollected + metrics.TotalFeesBalance;
                    if (totalExpected > 0)
                    {
                        decimal pctColl = (metrics.TotalFeesCollected / totalExpected) * 100m;
                        decimal pctBal = (metrics.TotalFeesBalance / totalExpected) * 100m;
                        feesCollectedLabel.Text = pctColl.ToString("0.00") + "%";
                        feesBalanceLabel.Text = pctBal.ToString("0.00") + "%";
                    }
                    else
                    {
                        feesCollectedLabel.Text = "0%";
                        feesBalanceLabel.Text = "0%";
                    }
                }
                else
                {
                    SetMetricLabelText(feesCollectedLabel, FormatCurrency(metrics.TotalFeesCollected));
                    SetMetricLabelText(feesBalanceLabel, FormatCurrency(metrics.TotalFeesBalance));
                }
                averageExamLabel.Text = metrics.AverageExamScore.ToString("0.00") + "%";
                topClassLabel.Text = metrics.TopClass;

                if (_topExpenseLabel != null)
                {
                    string cat = metrics.TopExpenseCategory;
                    _topExpenseLabel.Text = string.IsNullOrWhiteSpace(cat) ? "None" : cat;
                    _topExpenseLabel.Font = new Font("Segoe UI Semibold", cat.Length > 12 ? 13.5F : 15.5F, FontStyle.Bold);
                }

                if (_fundLabel != null)
                {
                    var term = Common.AppConfig.Leave.CurrentTerm;
                    var fin = await _dashboardService.GetFinanceSummaryAsync(term.Start, term.End);
                    SetMetricLabelText(_incomeLabel, FormatCurrency(fin.Income));
                    SetMetricLabelText(_expensesLabel, FormatCurrency(fin.Expenses));
                    SetMetricLabelText(_fundLabel, FormatCurrency(fin.Fund));
                    _fundLabel.ForeColor = fin.Fund < 0 ? AccentRed : AccentGreen;
                }

                bool hadSectionError = false;

                hadSectionError |= !TryRunDashboardSection("Recent payments", () =>
                {
                    BindGrid(recentPaymentsGrid, metrics.RecentPayments);
                    ConfigureRecentPaymentsGrid(recentPaymentsGrid);
                });
                if (AuthService.CurrentUser.Role == AuthService.UserRole.Headmaster)
                {
                    if (recentPaymentsGrid.Columns.Contains("Paid")) recentPaymentsGrid.Columns["Paid"].Visible = false;
                    if (recentPaymentsGrid.Columns.Contains("Balance")) recentPaymentsGrid.Columns["Balance"].Visible = false;
                }

                if (AuthService.CurrentUser.Role == AuthService.UserRole.Accountant)
                {
                    decimal totalExpected = metrics.TotalFeesCollected + metrics.TotalFeesBalance;
                    decimal collectionRate = totalExpected > 0 ? (metrics.TotalFeesCollected / totalExpected) * 100m : 0m;
                    averageExamLabel.Text = collectionRate.ToString("0.00") + "%";
                    topClassLabel.Text = GetTopOutstandingClass(metrics.OutstandingByClass);
                    SetMetricLabelText(pendingLeaveLabel, FormatCurrency(metrics.TotalFeesBalance));

                    hadSectionError |= !TryRunDashboardSection("Class fee position", () =>
                    {
                        BindGrid(classSummaryGrid, metrics.ClassFinanceSummary);
                        ConfigureClassFinanceGrid(classSummaryGrid);
                    });
                    hadSectionError |= !TryRunDashboardSection("Finance insight breakdown", () =>
                    {
                        PopulateFinanceInsightList(metrics.PaymentModeBreakdown);
                    });
                }
                else
                {
                    hadSectionError |= !TryRunDashboardSection("Class enrollment", () => BindGrid(classSummaryGrid, metrics.ClassSummary));
                    hadSectionError |= !TryRunDashboardSection("Leave summary", () => BindGrid(leaveSummaryGrid, metrics.LeaveSummary));
                }

                statusLabel.Text = (hadSectionError ? "Dashboard loaded with one table issue | " : "Connected to Neat_Academy | ")
                    + DateTime.Now.ToString("dd MMM yyyy, h:mm tt");
                LoggerHelper.LogInfo($"Dashboard statistics loaded in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Refresh failed";
                UIHelper.ShowError("Dashboard could not load live metrics: " + ex.Message, "Dashboard");
                LoggerHelper.LogError("LoadDashboardStatisticsAsync failed", ex);
            }
        }

        private static bool TryRunDashboardSection(string sectionName, Action action)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Dashboard section failed: {sectionName}", ex);
                return false;
            }
        }

        private static void BindGrid(DataGridView grid, DataTable table)
        {
            if (grid == null)
            {
                return;
            }

            grid.SuspendLayout();
            try
            {
                grid.DataSource = null;
                grid.DataSource = table;
                FormatDashboardGrid(grid);
                grid.ClearSelection();
                grid.CurrentCell = null;
                grid.Invalidate();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LoggerHelper.LogError("Dashboard grid bind skipped because the grid reported an invalid row index.", ex);
                grid.DataSource = CreateDashboardGridErrorTable();
            }
            finally
            {
                try
                {
                    grid.ResumeLayout(false);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    LoggerHelper.LogError("Dashboard grid layout resume skipped because the grid reported an invalid row index.", ex);
                }
            }
        }

        private static DataTable CreateDashboardGridErrorTable()
        {
            var table = new DataTable();
            table.Columns.Add("Status");
            table.Rows.Add("This table could not refresh. Other dashboard metrics remain available.");
            return table;
        }

        private void PopulateFinanceInsightList(DataTable table)
        {
            if (financeInsightList == null)
            {
                return;
            }

            financeInsightList.BeginUpdate();
            try
            {
                financeInsightList.Items.Clear();

                if (table == null || table.Rows.Count == 0)
                {
                    var empty = new ListViewItem("No payments yet");
                    empty.SubItems.Add("GHS 0.00");
                    financeInsightList.Items.Add(empty);
                    return;
                }

                foreach (DataRow row in table.Rows)
                {
                    string mode = Convert.ToString(row["Mode"]);
                    decimal total = 0m;
                    if (table.Columns.Contains("Total") && row["Total"] != DBNull.Value)
                    {
                        decimal.TryParse(Convert.ToString(row["Total"]), out total);
                    }

                    var item = new ListViewItem(string.IsNullOrWhiteSpace(mode) ? "Unknown" : mode);
                    item.SubItems.Add(FormatCurrency(total));
                    financeInsightList.Items.Add(item);
                }
            }
            finally
            {
                financeInsightList.EndUpdate();
            }
        }

        private static void FormatDashboardGrid(DataGridView grid)
        {
            if (grid?.Columns == null) return;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                string name = column.Name ?? "";
                string header = column.HeaderText ?? "";
                string key = (name + " " + header).ToUpperInvariant();

                if (key.Contains("PAID") ||
                    key.Contains("BALANCE") ||
                    key.Contains("OUTSTANDING") ||
                    key.Contains("TOTAL") ||
                    key.Contains("FEES"))
                {
                    column.DefaultCellStyle.Format = "GHS #,##0.00";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                if (key.Contains("PERCENT"))
                {
                    column.DefaultCellStyle.Format = "0.00'%'";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.HeaderText = "% Left";
                }

                if (key.Contains("DATE"))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy";
                }
            }
        }

        private static void ConfigureRecentPaymentsGrid(DataGridView grid)
        {
            if (grid?.Columns == null) return;

            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 34;
            grid.RowsDefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);

            SetFillWeight(grid, "ID", 70);
            SetFillWeight(grid, "Student", 125);
            SetFillWeight(grid, "Class", 85);
            SetFillWeight(grid, "Paid", 80);
            SetFillWeight(grid, "Balance", 90);
            SetFillWeight(grid, "Date", 90);
            SetFillWeight(grid, "Mode", 105);
            SetFillWeight(grid, "Bursar", 90);
        }

        private static void ConfigureClassFinanceGrid(DataGridView grid)
        {
            if (grid?.Columns == null) return;

            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeight = 42;
            grid.RowTemplate.Height = 34;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.DefaultCellStyle.Padding = new Padding(5, 2, 5, 2);
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            SetClassFinanceColumn(grid, "Class", "Class", 122, DataGridViewContentAlignment.MiddleLeft);
            SetClassFinanceColumn(grid, "Enrollment", "Enrol.", 58, DataGridViewContentAlignment.MiddleCenter);
            SetClassFinanceColumn(grid, "Fees Paid", "Paid\n(GHS)", 96, DataGridViewContentAlignment.MiddleRight, "#,##0.00");
            SetClassFinanceColumn(grid, "Outstanding", "Outstanding\n(GHS)", 122, DataGridViewContentAlignment.MiddleRight, "#,##0.00");
            SetClassFinanceColumn(grid, "Percent Left", "%\nLeft", 66, DataGridViewContentAlignment.MiddleRight, "0.00");
        }

        private static void SetClassFinanceColumn(
            DataGridView grid,
            string columnName,
            string headerText,
            float fillWeight,
            DataGridViewContentAlignment alignment,
            string format = null)
        {
            if (!grid.Columns.Contains(columnName))
            {
                return;
            }

            var column = grid.Columns[columnName];
            column.HeaderText = headerText;
            column.FillWeight = fillWeight;
            column.MinimumWidth = Math.Max(48, (int)Math.Round(fillWeight * 0.75));
            column.DefaultCellStyle.Alignment = alignment;
            if (!string.IsNullOrWhiteSpace(format))
            {
                column.DefaultCellStyle.Format = format;
            }
        }

        private static void ConfigurePaymentModeGrid(DataGridView grid)
        {
            if (grid?.Columns == null) return;

            grid.SuspendLayout();
            try
            {
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 34;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);

            SetFillWeight(grid, "Mode", 120);
            SetFillWeight(grid, "Total", 90);
            }
            finally
            {
                try
                {
                    grid.ResumeLayout(false);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    LoggerHelper.LogError("Payment mode grid layout resume skipped.", ex);
                }
            }
        }

        private static void SetFillWeight(DataGridView grid, string columnName, float weight)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].FillWeight = weight;
            }
        }

        private static string GetTopOutstandingClass(DataTable outstandingByClass)
        {
            if (outstandingByClass == null || outstandingByClass.Rows.Count == 0)
            {
                return "No debt";
            }

            var row = outstandingByClass.Rows[0];
            return Convert.ToString(row["Class"]) ?? "No debt";
        }

        private string FormatCurrency(decimal amount)
        {
            return "GHS " + amount.ToString("#,##0.00");
        }

        private static void SetMetricLabelText(Label label, string text)
        {
            if (label == null)
            {
                return;
            }

            label.Text = text;
            float size = text?.Length >= 16 ? 12.5F : text?.Length >= 13 ? 13.5F : 15.5F;
            label.Font = new Font("Segoe UI Semibold", size, FontStyle.Bold);
            label.AutoEllipsis = true;
        }

        private void gunaPictureBox1_Click(object sender, EventArgs e) { ExitApplication(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
        private void btnAddStudent_Click(object sender, EventArgs e)
        {
            OpenAddStudentDialog();
        }

        private void btnViewStudents_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmStdView());
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to View Students failed", ex);
            }
        }

        private void btnAddEmployee_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmEmployee());
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to Add Employee failed", ex);
            }
        }

        private void btnViewEmployees_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmEmpView());
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to View Employees failed", ex);
            }
        }

        private void btnViewExams_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new EXAMSVIEW());
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to View Exams failed", ex);
            }
        }

        private void btnExams_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new EXAMS());
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to Exams failed", ex);
            }
        }

        private void btnRecordFees_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmFessPayment());
                _ = LoadDashboardStatisticsAsync(true);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to Record Fees failed", ex);
            }
        }

        private void btnFees_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmFess());
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to Fees failed", ex);
            }
        }

        private void btnEmployeeLeave_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmEmpLeave());
                _ = LoadDashboardStatisticsAsync(true);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to Employee Leave failed", ex);
            }
        }

        private void btnLeaveDetails_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new frmLeaveDetails());
                _ = LoadDashboardStatisticsAsync(true);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Navigate to Leave Details failed", ex);
            }
        }

        private void gunaButton1_Click(object sender, EventArgs e) { OpenAddStudentDialog(); }
        private void gunaButton2_Click(object sender, EventArgs e) { OpenForm(new frmStdView()); }
        private void gunaButton3_Click(object sender, EventArgs e) { OpenForm(new frmEmployee()); }
        private void gunaButton4_Click(object sender, EventArgs e) { OpenForm(new frmEmpView()); }
        private void gunaButton5_Click(object sender, EventArgs e) { OpenForm(new EXAMSVIEW()); }
        private void gunaButton6_Click(object sender, EventArgs e) { OpenForm(new EXAMS()); }
        private void gunaButton7_Click(object sender, EventArgs e) { OpenForm(new frmFessPayment()); }
        private void gunaButton8_Click(object sender, EventArgs e) { OpenForm(new frmFess()); }
        private void gunaButton10_Click(object sender, EventArgs e) { OpenForm(new frmEmpLeave()); }
        private void gunaButton13_Click(object sender, EventArgs e) { OpenForm(new frmLeaveDetails()); }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { OpenAddStudentDialog(); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmEmpLeave()); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { OpenForm(new frmStdView()); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { OpenForm(new frmEmpView()); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmFessPayment()); }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmAbout()); }

        private async void frmDashboard_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadDashboardStatisticsAsync(true);
                LoggerHelper.LogInfo("frmDashboard loaded successfully");

                // Start weekly fee reminder timer
                _feeReminderTimer.Start();
                await CheckFeeRemindersAsync();
                await CheckTransportRemindersAsync();
                await Services.SmsOutboxService.FlushPendingAsync();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading dashboard: " + ex.Message, "Dashboard");
                LoggerHelper.LogError("frmDashboard_Load failed", ex);
            }
        }

        private async System.Threading.Tasks.Task CheckFeeRemindersAsync()
        {
            try
            {
                // Only send reminders on Mondays
                if (DateTime.Today.DayOfWeek != DayOfWeek.Monday)
                    return;

                // Check if already sent this week
                int currentWeek = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    DateTime.Today, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                int lastWeek = Properties.Settings.Default.LastFeeReminderWeek;
                if (lastWeek == currentWeek)
                    return;

                var outstandingTable = await _feeRepository.GetOutstandingBalancesTableAsync();
                if (outstandingTable == null || outstandingTable.Rows.Count == 0)
                    return;

                int sentCount = 0;
                foreach (DataRow row in outstandingTable.Rows)
                {
                    try
                    {
                        string studentId = row["ID"]?.ToString() ?? "";
                        decimal balance = Convert.ToDecimal(row["Balance Owed"] ?? 0);
                        string studentName = row["Student Name"]?.ToString() ?? "";

                        if (string.IsNullOrWhiteSpace(studentId) || balance <= 0)
                            continue;

                        var student = await _studentService.GetStudentAsync(studentId);
                        if (student == null)
                            continue;

                        // Send email reminder to guardian
                        if (!string.IsNullOrWhiteSpace(student.GuardianEmail))
                        {
                            _ = NotificationService.SendFeeReminderAsync(
                                studentName, student.GuardianEmail, balance, student.ClassID);
                        }

                        // Send SMS reminder (custom sender ID KPSFEES)
                        if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                        {
                            _ = SmsService.SendFeeReminderAsync(
                                student.EmergencyContact, studentName, balance);
                        }

                        sentCount++;
                    }
                    catch
                    {
                        // Continue with next student on error
                    }
                }

                // Persist the week number to avoid re-sending
                Properties.Settings.Default.LastFeeReminderWeek = currentWeek;
                Properties.Settings.Default.Save();

                LoggerHelper.LogInfo($"Weekly fee reminders sent to {sentCount} student(s)");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to send weekly fee reminders", ex);
            }
        }

        private async System.Threading.Tasks.Task CheckTransportRemindersAsync()
        {
            try
            {
                var role = AuthService.CurrentUser.Role;
                if (role != AuthService.UserRole.Accountant && role != AuthService.UserRole.Administrator
                    && role != AuthService.UserRole.Headmaster) return;

                await _transportRepo.EnsureTablesAsync();
                var candidates = await _transportRepo.GetReminderCandidatesAsync(DateTime.Today);
                int sent = 0;
                foreach (var a in candidates)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(a.GuardianPhone))
                        {
                            _ = SmsService.SendTransportReminderAsync(a.GuardianPhone, a.StudentName, a.RouteName, a.Period, a.Balance);
                            sent++;
                        }
                        await _transportRepo.LogReminderSentAsync(a.StudentID, a.Period);
                    }
                    catch (Exception exInner) { LoggerHelper.LogWarning("Transport reminder (row): " + exInner.Message); }
                }
                if (sent > 0) LoggerHelper.LogInfo($"Transport overdue reminders sent to {sent} guardian(s)");
            }
            catch (Exception ex) { LoggerHelper.LogError("Failed to send transport reminders", ex); }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                await LoadDashboardStatisticsAsync();
                ConfirmationHelper.ShowInfo("Dashboard updated", "Dashboard");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Dashboard refresh failed", ex);
            }
        }

        private void gunaButton12_Click(object sender, EventArgs e)
        {
            _ = LoadDashboardStatisticsAsync();
        }
    }
}
