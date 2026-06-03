using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Teacher landing page — shown after a Teacher logs in. Replaces the main
    /// frmDashboard for that role. Scoped to the teacher's assigned class
    /// (resolved via Users.EmploymentID → ClassAssignments.ClassTeacherID).
    /// </summary>
    public class frmTeacherDashboard : Form
    {
        private readonly IClassRepository _classRepository;
        private string _teacherName;
        private int _employmentId;
        private string _myClass; // null = unassigned

        private Label headerTitle;
        private Label headerSubtitle;
        private Label tileStudentsValue;
        private Label tileAvgScoreValue;
        private Label tileAttendanceValue;
        private Label tileOutstandingValue;
        private Label statusLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color NavyColor = UiTheme.Navy;
        private static readonly Color GoldColor = UiTheme.Gold;
        private static readonly Color GreenColor = Color.FromArgb(22, 163, 74);
        private static readonly Color RedColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public frmTeacherDashboard()
        {
            if (!AuthService.RequireAccess("frmTeacherDashboard", this)) return;

            _classRepository = new ClassRepository(AppConfig.ConnectionString);

            InitializeForm();
            BuildUI();
            Shown += async (s, e) => await LoadDataAsync();
            FormClosing += (s, e) =>
            {
                // If this is the main window, exit the app when it closes.
                if (Application.OpenForms.Count <= 1) Application.Exit();
            };
        }

        private void InitializeForm()
        {
            Text = "Teacher Dashboard — Kingdom Preparatory School";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 720);
            Size = new Size(1200, 780);
            FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void BuildUI()
        {
            SuspendLayout();
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140)); // KPI tiles
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // action buttons
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // status

            root.Controls.Add(BuildHeader(),    0, 0);
            root.Controls.Add(BuildKpiRow(),    0, 1);
            root.Controls.Add(BuildActions(),   0, 2);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Loading…",
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(statusLabel, 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            headerTitle = new Label
            {
                Dock = DockStyle.Top, Height = 44,
                Text = "Teacher Dashboard",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerSubtitle = new Label
            {
                Dock = DockStyle.Bottom, Height = 36,
                Text = "Welcome — loading your class details…",
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 10.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            titleBlock.Controls.Add(headerSubtitle);
            titleBlock.Controls.Add(headerTitle);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = PageBackColor,
                Padding = new Padding(0, 18, 0, 0)
            };
            var btnSignOut = MakeSecondaryButton("Sign Out");
            btnSignOut.Click += (s, e) =>
            {
                AuthService.Logout();
                this.Close();
            };
            var btnRefresh = MakePrimaryButton("Refresh");
            btnRefresh.Click += async (s, e) => await LoadDataAsync();
            actions.Controls.Add(btnSignOut);
            actions.Controls.Add(btnRefresh);

            panel.Controls.Add(titleBlock, 0, 0);
            panel.Controls.Add(actions, 1, 0);
            return panel;
        }

        private Control BuildKpiRow()
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = PageBackColor };
            for (int i = 0; i < 4; i++) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            tileStudentsValue   = AddTile(row, 0, "Students in My Class", "—", NavyColor);
            tileAvgScoreValue   = AddTile(row, 1, "Average Score", "—",      GoldColor);
            tileAttendanceValue = AddTile(row, 2, "Attendance (30d)", "—",   GreenColor);
            tileOutstandingValue= AddTile(row, 3, "Outstanding Fees", "—",   RedColor);
            return row;
        }

        private Label AddTile(TableLayoutPanel host, int col, string caption, string initialValue, Color accent)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(18)
            };
            var caps = new Label
            {
                Dock = DockStyle.Top, Height = 30,
                Text = caption, ForeColor = MutedColor,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var value = new Label
            {
                Dock = DockStyle.Fill, Text = initialValue,
                ForeColor = accent,
                Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(value);
            card.Controls.Add(caps);
            host.Controls.Add(card, col, 0);
            return value;
        }

        private Control BuildActions()
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(22),
                Margin = new Padding(0, 14, 0, 14)
            };

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 36,
                Text = "Quick Actions",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var grid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true, BackColor = SurfaceColor,
                Padding = new Padding(0, 10, 0, 0)
            };
            grid.Controls.Add(MakeActionButton("Students in My Class", () => new frmStdView().Show()));
            grid.Controls.Add(MakeActionButton("Take Attendance",     () => new frmAttendance().Show()));
            grid.Controls.Add(MakeActionButton("Enter Scores",        () => new EXAMS().Show()));
            grid.Controls.Add(MakeActionButton("View Results",        () => new EXAMSVIEW().Show()));
            grid.Controls.Add(MakeActionButton("Generate Report Cards", OpenReportCardForm));
            grid.Controls.Add(MakeActionButton("Outstanding Fees",    () => new frmOutstandingFees().Show()));
            grid.Controls.Add(MakeActionButton("Apply for Leave",     () => new frmEmpLeave().Show()));
            grid.Controls.Add(MakeActionButton("My Leave Balance",    () => new frmLeaveBalanceReport().Show()));
            grid.Controls.Add(MakeActionButton("Analytics Charts",    () => new frmDashboardCharts().Show()));

            card.Controls.Add(grid);
            card.Controls.Add(title);
            return card;
        }

        private void OpenReportCardForm()
        {
            try
            {
                var remarksRepository = new StudentTermRemarksRepository(AppConfig.ConnectionString);
                var dataService = new ReportCardDataService(AppConfig.ConnectionString, remarksRepository);
                var pdfGenerator = new ReportCardPDFGenerator();
                var printer = new ReportCardPrinter();
                var manager = new ReportCardManager(dataService, pdfGenerator, printer);
                new GenerateReportCardsForm(manager).Show();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not open Report Cards form: " + ex.Message, "Report Cards");
            }
        }

        private Button MakeActionButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 220, Height = 56,
                Margin = new Padding(0, 0, 12, 12),
                BackColor = NavyColor, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 78, 160);
            btn.Click += (s, e) =>
            {
                try { onClick(); }
                catch (Exception ex)
                {
                    UIHelper.ShowError("Could not open: " + ex.Message, text);
                }
            };
            return btn;
        }

        private Button MakePrimaryButton(string text)
        {
            var b = new Button
            {
                Text = text, Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0),
                BackColor = NavyColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Button MakeSecondaryButton(string text)
        {
            var b = new Button
            {
                Text = text, Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0),
                BackColor = SurfaceColor, ForeColor = TextColor, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = BorderColor;
            return b;
        }

        // ── Data loading ────────────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            try
            {
                statusLabel.Text = "Loading your class data…";

                var session = AuthService.CurrentUser;
                if (session == null || !session.EmploymentID.HasValue)
                {
                    headerSubtitle.Text = "This account is not linked to an employee record.";
                    statusLabel.Text = "Missing employee link — contact administration.";
                    return;
                }

                _employmentId = session.EmploymentID.Value;
                _teacherName = await GetTeacherNameAsync(_employmentId);
                _myClass = (await _classRepository.GetClassesForTeacherAsync(_employmentId)).FirstOrDefault();

                if (string.IsNullOrEmpty(_myClass))
                {
                    headerTitle.Text = $"Welcome, {_teacherName}";
                    headerSubtitle.Text = "You are not currently assigned to any class.";
                    tileStudentsValue.Text   = "—";
                    tileAvgScoreValue.Text   = "—";
                    tileAttendanceValue.Text = "—";
                    tileOutstandingValue.Text= "—";
                    statusLabel.Text = "No class assigned. Ask admin to assign you a class.";
                    return;
                }

                headerTitle.Text = $"Welcome, {_teacherName}";
                headerSubtitle.Text = $"Your class: {_myClass}";

                int students    = await GetStudentCountAsync(_myClass);
                double avg      = await GetAverageScoreAsync(_myClass);
                double attRate  = await GetAttendanceRateAsync(_myClass, 30);
                decimal owed    = await GetOutstandingForClassAsync(_myClass);

                tileStudentsValue.Text    = students.ToString();
                tileAvgScoreValue.Text    = avg > 0 ? avg.ToString("0.0") : "—";
                tileAttendanceValue.Text  = attRate >= 0 ? attRate.ToString("0") + "%" : "—";
                tileOutstandingValue.Text = "GHS " + owed.ToString("#,##0");

                statusLabel.Text = "Updated " + DateTime.Now.ToString("h:mm tt");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Could not load data.";
                LoggerHelper.LogError("frmTeacherDashboard.LoadDataAsync failed", ex);
            }
        }

        private async Task<string> GetTeacherNameAsync(int empId)
        {
            using (var conn = new OleDbConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT fullName FROM Employee WHERE employmentID = ?", conn))
                {
                    cmd.Parameters.AddWithValue("?", empId);
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? "Teacher" : result.ToString();
                }
            }
        }

        private async Task<int> GetStudentCountAsync(string className)
        {
            using (var conn = new OleDbConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM Students WHERE ClassID = ?", conn))
                {
                    cmd.Parameters.AddWithValue("?", className);
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
        }

        private async Task<double> GetAverageScoreAsync(string className)
        {
            using (var conn = new OleDbConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT AVG(gt) FROM examss WHERE std_class = ?", conn))
                {
                    cmd.Parameters.AddWithValue("?", className);
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? 0 : Convert.ToDouble(result);
                }
            }
        }

        private async Task<double> GetAttendanceRateAsync(string className, int days)
        {
            // % of "Present" rows over the last N days for students in this class.
            var query = @"
                SELECT
                    CAST(SUM(CASE WHEN UPPER(a.[Status]) = 'PRESENT' THEN 1 ELSE 0 END) * 100.0
                         / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) AS RatePct
                FROM Attendance a
                INNER JOIN Students s ON s.StudentID = a.ReferenceID
                WHERE UPPER(a.ReferenceType) = 'STUDENT'
                  AND s.ClassID = ?
                  AND a.[Date] >= ?";

            using (var conn = new OleDbConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", className);
                    cmd.Parameters.AddWithValue("?", DateTime.Today.AddDays(-days));
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? -1 : Convert.ToDouble(result);
                }
            }
        }

        private async Task<decimal> GetOutstandingForClassAsync(string className)
        {
            // Latest payment row per student, summed where Balance > 0.
            var query = @"
                SELECT SUM(Balance) AS Outstanding FROM (
                    SELECT pr.StudentID, pr.classID, pr.Balance,
                           ROW_NUMBER() OVER (PARTITION BY pr.StudentID ORDER BY pr.[Date] DESC, pr.tm DESC) AS rn
                    FROM payment_record pr
                ) latest
                WHERE rn = 1 AND Balance > 0 AND classID = ?";

            using (var conn = new OleDbConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new OleDbCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("?", className);
                    var result = await cmd.ExecuteScalarAsync();
                    return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                }
            }
        }
    }
}
