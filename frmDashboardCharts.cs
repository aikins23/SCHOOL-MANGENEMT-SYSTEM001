using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Dashboard Charts Form — Feature #9
    /// Updated to use modern 4-layer architecture with DashboardService.
    /// </summary>
    public class frmDashboardCharts : Form
    {
        private readonly DashboardService _dashboardService;

        // Colors matching the school dashboard palette
        private static readonly Color PageBackColor  = UiTheme.Page;
        private static readonly Color SurfaceColor   = Color.White;
        private static readonly Color NavyColor      = UiTheme.Navy;
        private static readonly Color PrimaryColor   = UiTheme.Navy;
        private static readonly Color GreenColor     = Color.FromArgb(22, 163, 74);
        private static readonly Color RedColor       = Color.FromArgb(190, 18, 60);
        private static readonly Color AmberColor     = UiTheme.Gold;
        private static readonly Color PurpleColor    = Color.FromArgb(83, 76, 167);
        private static readonly Color TextColor      = UiTheme.Text;
        private static readonly Color MutedColor     = UiTheme.Muted;
        private static readonly Color BorderColor    = UiTheme.Border;

        private Label statusLabel;

        // Chart controls
        private Chart chartFees;
        private Chart chartEnrollment;
        private Chart chartExams;
        private Chart chartTrend;
        private Chart chartAttendance;
        private Chart chartIncomeExpense;
        private Chart chartLeave;
        private Chart chartGrade;
        private Chart chartAttendanceByClass;
        private Chart chartOutstandingByClass;
        private Chart chartPaymentMode;
        private Chart chartStaffDept;
        private Chart chartExpenseCategory;
        private Chart chartTopAbsent;
        private Chart chartClassAvg;
        private Chart chartGender;
        private Chart chartTermPerf;
        private Chart chartAdmissions;
        private Chart chartActiveRollout;
        private Chart chartSalaryDept;
        private Chart chartSubjectPassFail;
        private bool chartsLoaded;

        public frmDashboardCharts()
        {
            if (!AuthService.RequireAccess("frmDashboardCharts", this)) return;

            // Initialize modern architecture
            var repository = new DashboardRepository(AppConfig.ConnectionString);
            _dashboardService = new DashboardService(repository);

            InitializeForm();
            this.Size = new Size(1300, 820);
            BuildUI();
            Shown += FrmDashboardCharts_Shown;
        }

        private async void FrmDashboardCharts_Shown(object sender, EventArgs e)
        {
            if (chartsLoaded) return;
            chartsLoaded = true;
            await LoadAllCharts();
        }

        private void InitializeForm()
        {
            Text            = "Analytics Dashboard - Kingdom Preparatory School";
            BackColor       = PageBackColor;
            Font            = new Font("Segoe UI", 9.5F);
            StartPosition   = FormStartPosition.CenterScreen;
            MinimumSize     = new Size(1280, 780);
            Size            = new Size(1380, 860);
            FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void BuildUI()
        {
            SuspendLayout();
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = PageBackColor, Padding = new Padding(24, 20, 24, 20) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildChartGrid(), 0, 1);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label { Dock = DockStyle.Top, Height = 40, Text = "Analytics Dashboard", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            titleBlock.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 26, Text = "Visual analytics for fees, enrollment, exam performance, and collection trends", ForeColor = MutedColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 14, 0, 0) };
            var refreshBtn = MakePrimaryButton("Refresh Charts");
            refreshBtn.Click += async (s, e) => await LoadAllCharts();
            var resultsBtn = MakeSecondaryButton("Exam Results");
            resultsBtn.Click += (s, e) => new EXAMSVIEW().Show();
            var entryBtn = MakeSecondaryButton("Enter Scores");
            entryBtn.Click += (s, e) => new EXAMS().Show();
            var dashboardBtn = MakeSecondaryButton("Dashboard");
            dashboardBtn.Click += (s, e) => { Close(); Common.FormManager.GoToDashboard(); };

            statusLabel = new Label { AutoSize = false, Width = 220, Height = 36, ForeColor = MutedColor, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(0, 4, 8, 0) };

            actions.Controls.Add(refreshBtn);
            actions.Controls.Add(resultsBtn);
            actions.Controls.Add(entryBtn);
            actions.Controls.Add(dashboardBtn);
            actions.Controls.Add(statusLabel);

            panel.Controls.Add(titleBlock, 0, 0);
            panel.Controls.Add(actions, 1, 0);
            return panel;
        }

        private Control BuildChartGrid()
        {
            var scrollHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = PageBackColor };

            var grid = new TableLayoutPanel { Dock = DockStyle.Top, RowCount = 11, ColumnCount = 2, BackColor = PageBackColor, AutoSize = true };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 11; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));

            chartFees = CreateChart();
            grid.Controls.Add(WrapInCard("Fees — Collected vs Outstanding", chartFees), 0, 0);
            chartEnrollment = CreateChart();
            grid.Controls.Add(WrapInCard("Student Enrollment by Class", chartEnrollment), 1, 0);
            chartExams = CreateChart();
            grid.Controls.Add(WrapInCard("Average Exam Score by Subject", chartExams), 0, 1);
            chartTrend = CreateChart();
            grid.Controls.Add(WrapInCard("Monthly Fee Collection Trend", chartTrend), 1, 1);
            chartAttendance = CreateChart();
            grid.Controls.Add(WrapInCard("Monthly Attendance Rate (%)", chartAttendance), 0, 2);
            chartIncomeExpense = CreateChart();
            grid.Controls.Add(WrapInCard("Income vs Expenses (Monthly)", chartIncomeExpense), 1, 2);
            chartLeave = CreateChart();
            grid.Controls.Add(WrapInCard("Staff Leave Status", chartLeave), 0, 3);
            chartGrade = CreateChart();
            grid.Controls.Add(WrapInCard("Exam Grade Distribution", chartGrade), 1, 3);
            chartAttendanceByClass = CreateChart();
            grid.Controls.Add(WrapInCard("Attendance Rate by Class (%)", chartAttendanceByClass), 0, 4);
            chartOutstandingByClass = CreateChart();
            grid.Controls.Add(WrapInCard("Outstanding Fees by Class", chartOutstandingByClass), 1, 4);
            chartPaymentMode = CreateChart();
            grid.Controls.Add(WrapInCard("Payment Method Breakdown", chartPaymentMode), 0, 5);
            chartStaffDept = CreateChart();
            grid.Controls.Add(WrapInCard("Staff by Department", chartStaffDept), 1, 5);
            chartExpenseCategory = CreateChart();
            grid.Controls.Add(WrapInCard("Expense Breakdown by Category", chartExpenseCategory), 0, 6);
            chartTopAbsent = CreateChart();
            grid.Controls.Add(WrapInCard("Top 10 Most-Absent Students", chartTopAbsent), 1, 6);
            chartClassAvg = CreateChart();
            grid.Controls.Add(WrapInCard("Class Average Score Comparison", chartClassAvg), 0, 7);
            chartGender = CreateChart();
            grid.Controls.Add(WrapInCard("Student Gender Distribution", chartGender), 1, 7);
            chartTermPerf = CreateChart();
            grid.Controls.Add(WrapInCard("Term-over-Term Performance Trend", chartTermPerf), 0, 8);
            chartAdmissions = CreateChart();
            grid.Controls.Add(WrapInCard("Admissions per Year", chartAdmissions), 1, 8);
            chartActiveRollout = CreateChart();
            grid.Controls.Add(WrapInCard("Active vs Rolled-out Students", chartActiveRollout), 0, 9);
            chartSalaryDept = CreateChart();
            grid.Controls.Add(WrapInCard("Salary Spend by Department", chartSalaryDept), 1, 9);
            chartSubjectPassFail = CreateChart();
            var passFailCard = WrapInCard("Subject Pass/Fail Rate (Pass ≥ 50)", chartSubjectPassFail);
            grid.Controls.Add(passFailCard, 0, 10);
            grid.SetColumnSpan(passFailCard, 2);

            // Make every wrapper card fill its cell.
            foreach (Control c in grid.Controls) c.Dock = DockStyle.Fill;

            scrollHost.Controls.Add(grid);
            return scrollHost;
        }

        private Panel WrapInCard(string title, Chart chart)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 10, 10), MinimumSize = new Size(100, 100) };
            var titleLabel = new Label { Dock = DockStyle.Top, Height = 42, Padding = new Padding(16, 0, 0, 0), Text = title, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, BackColor = SurfaceColor };
            chart.Dock = DockStyle.Fill;
            card.Controls.Add(chart);
            card.Controls.Add(titleLabel);
            return card;
        }

        private Chart CreateChart()
        {
            var chart = new Chart { BackColor = SurfaceColor, Size = new Size(100, 100), AntiAliasing = AntiAliasingStyles.All, TextAntiAliasingQuality = TextAntiAliasingQuality.High };
            chart.ChartAreas.Add(new ChartArea
            {
                BackColor = SurfaceColor, BorderColor = BorderColor, BorderWidth = 1,
                AxisX = { MajorGrid = { LineColor = BorderColor }, LabelStyle = { Font = new Font("Segoe UI", 8F), ForeColor = MutedColor, Angle = -25 } },
                AxisY = { MajorGrid = { LineColor = Color.FromArgb(235, 238, 242) }, LabelStyle = { Font = new Font("Segoe UI", 8F), ForeColor = MutedColor } }
            });
            chart.Legends.Add(new Legend { Docking = Docking.Bottom, Alignment = StringAlignment.Center, BackColor = SurfaceColor, BorderColor = Color.Transparent, Font = new Font("Segoe UI", 8.5F) });
            return chart;
        }

        private async System.Threading.Tasks.Task LoadAllCharts()
        {
            try
            {
                statusLabel.Text = "Loading analytics...";
                var metrics = await _dashboardService.GetMetricsAsync();

                PopulateFeesChart(metrics.TotalFeesCollected, metrics.TotalFeesBalance);
                PopulateEnrollmentChart(metrics.ClassSummary);
                PopulateExamsChart(metrics.AverageScoresBySubject);
                PopulateTrendChart(metrics.CollectionTrend);
                PopulateAttendanceChart(metrics.AttendanceTrend);
                PopulateIncomeExpenseChart(metrics.IncomeVsExpenses);
                PopulateLeaveChart(metrics.LeaveSummary);
                PopulateGradeChart(metrics.GradeDistribution);
                PopulateAttendanceByClassChart(metrics.AttendanceByClass);
                PopulateOutstandingByClassChart(metrics.OutstandingByClass);
                PopulatePaymentModeChart(metrics.PaymentModeBreakdown);
                PopulateStaffDeptChart(metrics.StaffByDepartment);
                PopulateExpenseCategoryChart(metrics.ExpenseByCategory);
                PopulateTopAbsentChart(metrics.TopAbsentStudents);
                PopulateClassAvgChart(metrics.ClassAverageScore);
                PopulateGenderChart(metrics.StudentGenderDistribution);
                PopulateTermPerfChart(metrics.TermOverTermPerformance);
                PopulateAdmissionsChart(metrics.AdmissionsPerYear);
                PopulateActiveRolloutChart(metrics.ActiveVsRolledOut);
                PopulateSalaryDeptChart(metrics.SalaryByDepartment);
                PopulateSubjectPassFailChart(metrics.SubjectPassFail);

                statusLabel.Text = "Updated " + DateTime.Now.ToString("h:mm tt");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Refresh failed";
                UIHelper.ShowWarning("Could not load chart data:\n\n" + ex.Message, "Analytics Dashboard");
            }
        }

        private void PopulateFeesChart(decimal collected, decimal outstanding)
        {
            chartFees.Series.Clear();
            var serCollected = new Series("Collected") { ChartType = SeriesChartType.Bar, Color = GreenColor, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0.00", Font = new Font("Segoe UI", 8F) };
            var serOutstanding = new Series("Outstanding") { ChartType = SeriesChartType.Bar, Color = RedColor, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0.00", Font = new Font("Segoe UI", 8F) };
            serCollected.Points.AddXY("Fees", (double)collected);
            serOutstanding.Points.AddXY("Fees", (double)outstanding);
            chartFees.Series.Add(serCollected);
            chartFees.Series.Add(serOutstanding);
            chartFees.ChartAreas[0].AxisY.LabelStyle.Format = "GHS #,##0";
        }

        private void PopulateEnrollmentChart(DataTable dt)
        {
            chartEnrollment.Series.Clear();
            var series = new Series("Students") { ChartType = SeriesChartType.Column, Color = PrimaryColor, IsValueShownAsLabel = true, Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows) series.Points.AddXY(row["Class"].ToString(), Convert.ToInt32(row["Enrollment"]));
            chartEnrollment.Series.Add(series);
            chartEnrollment.ChartAreas[0].AxisX.Interval = 1;
        }

        private void PopulateExamsChart(DataTable dt)
        {
            chartExams.Series.Clear();
            var series = new Series("Avg Score") { ChartType = SeriesChartType.Column, Color = AmberColor, IsValueShownAsLabel = true, LabelFormat = "0.0", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows)
            {
                string subject = row["subject"].ToString();
                if (subject.Length > 10) subject = subject.Substring(0, 10) + "...";
                series.Points.AddXY(subject, Convert.ToDouble(row["AvgScore"]));
            }
            chartExams.Series.Add(series);
            chartExams.ChartAreas[0].AxisY.Maximum = 100;
        }

        private void PopulateTrendChart(DataTable dt)
        {
            chartTrend.Series.Clear();
            int year = DateTime.Now.Year;
            var series = new Series($"Collection {year}") { ChartType = SeriesChartType.Line, Color = PurpleColor, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle, MarkerSize = 8, MarkerColor = PurpleColor, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0", Font = new Font("Segoe UI", 8F) };
            string[] monthNames = { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
            if (dt == null || dt.Rows.Count == 0) for (int m = 1; m <= 12; m++) series.Points.AddXY(monthNames[m - 1], 0);
            else
            {
                var monthly = new decimal[13];
                foreach (DataRow row in dt.Rows) monthly[Convert.ToInt32(row["Mo"])] = Convert.ToDecimal(row["Total"]);
                for (int m = 1; m <= DateTime.Now.Month; m++) series.Points.AddXY(monthNames[m - 1], (double)monthly[m]);
            }
            chartTrend.Series.Add(series);
            chartTrend.ChartAreas[0].AxisY.LabelStyle.Format = "GHS #,##0";
        }

        private void PopulateAttendanceChart(DataTable dt)
        {
            chartAttendance.Series.Clear();
            string[] monthNames = { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
            var series = new Series("Attendance %") { ChartType = SeriesChartType.Line, Color = GreenColor, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle, MarkerSize = 8, MarkerColor = GreenColor, IsValueShownAsLabel = true, LabelFormat = "0.0", Font = new Font("Segoe UI", 8F) };
            var monthly = new double[13];
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    int mo = Convert.ToInt32(row["Mo"]);
                    if (mo >= 1 && mo <= 12) monthly[mo] = Convert.ToDouble(row["RatePct"]);
                }
            }
            for (int m = 1; m <= DateTime.Now.Month; m++) series.Points.AddXY(monthNames[m - 1], monthly[m]);
            chartAttendance.Series.Add(series);
            chartAttendance.ChartAreas[0].AxisY.Maximum = 100;
            chartAttendance.ChartAreas[0].AxisY.LabelStyle.Format = "0'%'";
        }

        private void PopulateIncomeExpenseChart(DataTable dt)
        {
            chartIncomeExpense.Series.Clear();
            string[] monthNames = { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
            var income = new Series("Income") { ChartType = SeriesChartType.Column, Color = GreenColor, IsValueShownAsLabel = false, Font = new Font("Segoe UI", 8F) };
            var expense = new Series("Expenses") { ChartType = SeriesChartType.Column, Color = RedColor, IsValueShownAsLabel = false, Font = new Font("Segoe UI", 8F) };
            var inc = new decimal[13];
            var exp = new decimal[13];
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    int mo = Convert.ToInt32(row["Mo"]);
                    if (mo < 1 || mo > 12) continue;
                    inc[mo] = row["Income"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Income"]);
                    exp[mo] = row["Expense"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Expense"]);
                }
            }
            for (int m = 1; m <= DateTime.Now.Month; m++)
            {
                income.Points.AddXY(monthNames[m - 1], (double)inc[m]);
                expense.Points.AddXY(monthNames[m - 1], (double)exp[m]);
            }
            chartIncomeExpense.Series.Add(income);
            chartIncomeExpense.Series.Add(expense);
            chartIncomeExpense.ChartAreas[0].AxisY.LabelStyle.Format = "GHS #,##0";
        }

        private void PopulateLeaveChart(DataTable dt)
        {
            chartLeave.Series.Clear();
            var series = new Series("Leave") { ChartType = SeriesChartType.Doughnut, IsValueShownAsLabel = true, LabelFormat = "0", Font = new Font("Segoe UI Semibold", 9F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 1);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string status = (row["Status"] ?? "").ToString().Trim();
                    if (string.IsNullOrEmpty(status)) status = "Unknown";
                    int total = Convert.ToInt32(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(status, total)];
                    pt.LegendText = status;
                    string up = status.ToUpperInvariant();
                    if (up.Contains("APPROVE")) pt.Color = GreenColor;
                    else if (up.Contains("PEND")) pt.Color = AmberColor;
                    else if (up.Contains("REJECT") || up.Contains("DENI")) pt.Color = RedColor;
                    else pt.Color = PurpleColor;
                }
            }
            chartLeave.Series.Add(series);
        }

        private void PopulateGradeChart(DataTable dt)
        {
            chartGrade.Series.Clear();
            var series = new Series("Students") { ChartType = SeriesChartType.Column, Color = PurpleColor, IsValueShownAsLabel = true, Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 0);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string grade = (row["Grade"] ?? "").ToString().Trim();
                    if (string.IsNullOrEmpty(grade)) grade = "—";
                    int total = Convert.ToInt32(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(grade, total)];
                    string up = grade.ToUpperInvariant();
                    if (up.StartsWith("A")) pt.Color = GreenColor;
                    else if (up.StartsWith("B")) pt.Color = Color.FromArgb(80, 160, 80);
                    else if (up.StartsWith("C")) pt.Color = AmberColor;
                    else if (up.StartsWith("D")) pt.Color = Color.FromArgb(220, 130, 40);
                    else if (up.StartsWith("F") || up.StartsWith("E")) pt.Color = RedColor;
                    else pt.Color = PrimaryColor;
                }
            }
            chartGrade.Series.Add(series);
            chartGrade.ChartAreas[0].AxisX.Interval = 1;
        }

        private void PopulateAttendanceByClassChart(DataTable dt)
        {
            chartAttendanceByClass.Series.Clear();
            var series = new Series("Attendance %") { ChartType = SeriesChartType.Bar, Color = GreenColor, IsValueShownAsLabel = true, LabelFormat = "0.0", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows)
            {
                string cls = (row["Class"] ?? "—").ToString();
                series.Points.AddXY(cls, Convert.ToDouble(row["RatePct"]));
            }
            chartAttendanceByClass.Series.Add(series);
            chartAttendanceByClass.ChartAreas[0].AxisX.Interval = 1;
            chartAttendanceByClass.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
            chartAttendanceByClass.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
            chartAttendanceByClass.ChartAreas[0].AxisY.Maximum = 100;
            chartAttendanceByClass.ChartAreas[0].AxisY.LabelStyle.Format = "0'%'";
        }

        private void PopulateOutstandingByClassChart(DataTable dt)
        {
            chartOutstandingByClass.Series.Clear();
            var series = new Series("Outstanding") { ChartType = SeriesChartType.Bar, Color = RedColor, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows)
            {
                string cls = (row["Class"] ?? "—").ToString();
                decimal amt = row["Outstanding"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Outstanding"]);
                series.Points.AddXY(cls, (double)amt);
            }
            chartOutstandingByClass.Series.Add(series);
            chartOutstandingByClass.ChartAreas[0].AxisX.Interval = 1;
            chartOutstandingByClass.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
            chartOutstandingByClass.ChartAreas[0].AxisY.LabelStyle.Format = "GHS #,##0";
        }

        private void PopulatePaymentModeChart(DataTable dt)
        {
            chartPaymentMode.Series.Clear();
            var series = new Series("Payments") { ChartType = SeriesChartType.Doughnut, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0", Font = new Font("Segoe UI Semibold", 8.5F) };
            Color[] palette = { PrimaryColor, GreenColor, AmberColor, PurpleColor, RedColor, Color.FromArgb(0, 150, 180) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 1);
            }
            else
            {
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    string mode = (row["Mode"] ?? "Unknown").ToString();
                    decimal total = row["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(mode, (double)total)];
                    pt.LegendText = mode;
                    pt.Color = palette[i % palette.Length];
                    i++;
                }
            }
            chartPaymentMode.Series.Add(series);
        }

        private void PopulateStaffDeptChart(DataTable dt)
        {
            chartStaffDept.Series.Clear();
            var series = new Series("Staff") { ChartType = SeriesChartType.Pie, IsValueShownAsLabel = true, LabelFormat = "0", Font = new Font("Segoe UI Semibold", 9F) };
            Color[] palette = { PrimaryColor, GreenColor, AmberColor, PurpleColor, RedColor, Color.FromArgb(0, 150, 180), Color.FromArgb(120, 80, 200) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 1);
            }
            else
            {
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    string dept = (row["Department"] ?? "Unassigned").ToString();
                    int total = Convert.ToInt32(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(dept, total)];
                    pt.LegendText = dept + " (" + total + ")";
                    pt.Color = palette[i % palette.Length];
                    i++;
                }
            }
            chartStaffDept.Series.Add(series);
        }

        private void PopulateExpenseCategoryChart(DataTable dt)
        {
            chartExpenseCategory.Series.Clear();
            var series = new Series("Expenses") { ChartType = SeriesChartType.Doughnut, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0", Font = new Font("Segoe UI Semibold", 8.5F) };
            Color[] palette = { RedColor, AmberColor, PurpleColor, PrimaryColor, GreenColor, Color.FromArgb(0, 150, 180), Color.FromArgb(220, 130, 40), Color.FromArgb(120, 80, 200) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 1);
            }
            else
            {
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    string cat = (row["Category"] ?? "Uncategorized").ToString();
                    decimal total = row["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(cat, (double)total)];
                    pt.LegendText = cat;
                    pt.Color = palette[i % palette.Length];
                    i++;
                }
            }
            chartExpenseCategory.Series.Add(series);
        }

        private void PopulateTopAbsentChart(DataTable dt)
        {
            chartTopAbsent.Series.Clear();
            var series = new Series("Absences") { ChartType = SeriesChartType.Bar, Color = RedColor, IsValueShownAsLabel = true, Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No absences recorded", 0);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string name = (row["Student"] ?? "—").ToString();
                    if (name.Length > 22) name = name.Substring(0, 22) + "...";
                    int absences = Convert.ToInt32(row["Absences"]);
                    series.Points.AddXY(name, absences);
                }
            }
            chartTopAbsent.Series.Add(series);
            chartTopAbsent.ChartAreas[0].AxisX.Interval = 1;
            chartTopAbsent.ChartAreas[0].AxisX.LabelStyle.Angle = 0;
            chartTopAbsent.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
        }

        private void PopulateClassAvgChart(DataTable dt)
        {
            chartClassAvg.Series.Clear();
            var series = new Series("Avg Score") { ChartType = SeriesChartType.Column, Color = PrimaryColor, IsValueShownAsLabel = true, LabelFormat = "0.0", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows)
            {
                string cls = (row["Class"] ?? "—").ToString();
                double avg = row["AvgScore"] == DBNull.Value ? 0 : Convert.ToDouble(row["AvgScore"]);
                var pt = series.Points[series.Points.AddXY(cls, avg)];
                if (avg >= 70) pt.Color = GreenColor;
                else if (avg >= 50) pt.Color = AmberColor;
                else pt.Color = RedColor;
            }
            chartClassAvg.Series.Add(series);
            chartClassAvg.ChartAreas[0].AxisX.Interval = 1;
            chartClassAvg.ChartAreas[0].AxisY.Maximum = 100;
        }

        private void PopulateGenderChart(DataTable dt)
        {
            chartGender.Series.Clear();
            var series = new Series("Students") { ChartType = SeriesChartType.Doughnut, IsValueShownAsLabel = true, LabelFormat = "0", Font = new Font("Segoe UI Semibold", 10F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 1);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string gender = (row["Gender"] ?? "Unknown").ToString().Trim();
                    int total = Convert.ToInt32(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(gender, total)];
                    pt.LegendText = gender + " (" + total + ")";
                    string up = gender.ToUpperInvariant();
                    if (up.StartsWith("M")) pt.Color = PrimaryColor;
                    else if (up.StartsWith("F")) pt.Color = Color.FromArgb(219, 39, 119);
                    else pt.Color = MutedColor;
                }
            }
            chartGender.Series.Add(series);
        }

        private void PopulateTermPerfChart(DataTable dt)
        {
            chartTermPerf.Series.Clear();
            var series = new Series("Avg Score") { ChartType = SeriesChartType.Line, Color = PurpleColor, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle, MarkerSize = 8, MarkerColor = PurpleColor, IsValueShownAsLabel = true, LabelFormat = "0.0", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 0);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string label = (row["Yr"] ?? "").ToString() + " " + (row["Term"] ?? "").ToString();
                    double avg = row["AvgScore"] == DBNull.Value ? 0 : Convert.ToDouble(row["AvgScore"]);
                    series.Points.AddXY(label, avg);
                }
            }
            chartTermPerf.Series.Add(series);
            chartTermPerf.ChartAreas[0].AxisX.Interval = 1;
            chartTermPerf.ChartAreas[0].AxisY.Maximum = 100;
        }

        private void PopulateAdmissionsChart(DataTable dt)
        {
            chartAdmissions.Series.Clear();
            var series = new Series("Admissions") { ChartType = SeriesChartType.Column, Color = PrimaryColor, IsValueShownAsLabel = true, Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows)
            {
                string yr = row["Yr"] == DBNull.Value ? "—" : row["Yr"].ToString();
                int total = Convert.ToInt32(row["Total"]);
                series.Points.AddXY(yr, total);
            }
            chartAdmissions.Series.Add(series);
            chartAdmissions.ChartAreas[0].AxisX.Interval = 1;
        }

        private void PopulateActiveRolloutChart(DataTable dt)
        {
            chartActiveRollout.Series.Clear();
            var series = new Series("Students") { ChartType = SeriesChartType.Doughnut, IsValueShownAsLabel = true, LabelFormat = "0", Font = new Font("Segoe UI Semibold", 10F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 1);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string bucket = (row["Bucket"] ?? "—").ToString();
                    int total = Convert.ToInt32(row["Total"]);
                    var pt = series.Points[series.Points.AddXY(bucket, total)];
                    pt.LegendText = bucket + " (" + total + ")";
                    pt.Color = bucket.StartsWith("Active") ? GreenColor : MutedColor;
                }
            }
            chartActiveRollout.Series.Add(series);
        }

        private void PopulateSalaryDeptChart(DataTable dt)
        {
            chartSalaryDept.Series.Clear();
            var series = new Series("Salary") { ChartType = SeriesChartType.Bar, Color = AmberColor, IsValueShownAsLabel = true, LabelFormat = "GHS #,##0", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0) series.Points.AddXY("No data", 0);
            else foreach (DataRow row in dt.Rows)
            {
                string dept = (row["Department"] ?? "Unassigned").ToString();
                decimal total = row["TotalSalary"] == DBNull.Value ? 0m : Convert.ToDecimal(row["TotalSalary"]);
                series.Points.AddXY(dept, (double)total);
            }
            chartSalaryDept.Series.Add(series);
            chartSalaryDept.ChartAreas[0].AxisX.Interval = 1;
            chartSalaryDept.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
            chartSalaryDept.ChartAreas[0].AxisY.LabelStyle.Format = "GHS #,##0";
        }

        private void PopulateSubjectPassFailChart(DataTable dt)
        {
            chartSubjectPassFail.Series.Clear();
            var pass = new Series("Pass") { ChartType = SeriesChartType.StackedColumn100, Color = GreenColor, IsValueShownAsLabel = true, LabelFormat = "0'%'", Font = new Font("Segoe UI", 8F) };
            var fail = new Series("Fail") { ChartType = SeriesChartType.StackedColumn100, Color = RedColor, IsValueShownAsLabel = true, LabelFormat = "0'%'", Font = new Font("Segoe UI", 8F) };
            if (dt == null || dt.Rows.Count == 0)
            {
                pass.Points.AddXY("No data", 0);
                fail.Points.AddXY("No data", 0);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string subject = (row["Subject"] ?? "—").ToString();
                    if (subject.Length > 14) subject = subject.Substring(0, 14) + "...";
                    int p = Convert.ToInt32(row["PassCount"]);
                    int f = Convert.ToInt32(row["FailCount"]);
                    pass.Points.AddXY(subject, p);
                    fail.Points.AddXY(subject, f);
                }
            }
            chartSubjectPassFail.Series.Add(pass);
            chartSubjectPassFail.Series.Add(fail);
            chartSubjectPassFail.ChartAreas[0].AxisX.Interval = 1;
            chartSubjectPassFail.ChartAreas[0].AxisX.LabelStyle.Angle = -25;
            chartSubjectPassFail.ChartAreas[0].AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
            chartSubjectPassFail.ChartAreas[0].AxisY.LabelStyle.Format = "0'%'";
            chartSubjectPassFail.ChartAreas[0].AxisY.Maximum = 100;
        }

        private Button MakePrimaryButton(string text)
        {
            var btn = new Button { Text = text, Height = 36, Width = 148, BackColor = PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(0, 4, 0, 0) };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 78, 160);
            return btn;
        }

        private Button MakeSecondaryButton(string text)
        {
            var btn = new Button { Text = text, Height = 36, Width = 112, BackColor = SurfaceColor, ForeColor = TextColor, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(8, 4, 0, 0) };
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.MouseOverBackColor = UiTheme.GoldSoft;
            return btn;
        }
    }
}
