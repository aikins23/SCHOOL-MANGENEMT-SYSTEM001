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
        private bool chartsLoaded;

        public frmDashboardCharts()
        {
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
            dashboardBtn.Click += (s, e) => { Close(); new frmDashboard().Show(); };

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
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2, BackColor = PageBackColor };
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            chartFees = CreateChart();
            grid.Controls.Add(WrapInCard("Fees — Collected vs Outstanding", chartFees), 0, 0);
            chartEnrollment = CreateChart();
            grid.Controls.Add(WrapInCard("Student Enrollment by Class", chartEnrollment), 1, 0);
            chartExams = CreateChart();
            grid.Controls.Add(WrapInCard("Average Exam Score by Subject", chartExams), 0, 1);
            chartTrend = CreateChart();
            grid.Controls.Add(WrapInCard("Monthly Fee Collection Trend", chartTrend), 1, 1);

            return grid;
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
            else foreach (DataRow row in dt.Rows) series.Points.AddXY(row["Class"].ToString(), Convert.ToInt32(row["Students"]));
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
