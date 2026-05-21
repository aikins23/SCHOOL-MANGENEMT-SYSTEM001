using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Dashboard Charts Form — Feature #9
    /// Shows visual analytics: Fees Collected vs Outstanding (bar),
    /// Student Enrollment by Class (bar), Exam Performance by Subject (bar),
    /// and Monthly Fee Collection Trend (line).
    /// Uses System.Windows.Forms.DataVisualization.Charting (built-in, no extra dependencies).
    /// </summary>
    public class frmDashboardCharts : Form
    {
        private readonly kum Aikins = new kum();

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
            InitializeForm();
            BuildUI();
            Shown += FrmDashboardCharts_Shown;
        }

        private void FrmDashboardCharts_Shown(object sender, EventArgs e)
        {
            if (chartsLoaded)
            {
                return;
            }

            chartsLoaded = true;
            LoadAllCharts();
        }

        // ──────────────────────────────────────────────────────────────
        //  FORM SETUP
        // ──────────────────────────────────────────────────────────────

        private void InitializeForm()
        {
            Text            = "Dashboard Charts — Kingdom Preparatory School";
            BackColor       = PageBackColor;
            Font            = new Font("Segoe UI", 9.5F);
            StartPosition   = FormStartPosition.CenterScreen;
            MinimumSize     = new Size(1200, 750);
            Size            = new Size(1300, 820);
            FormBorderStyle = FormBorderStyle.Sizable;
        }

        // ──────────────────────────────────────────────────────────────
        //  UI LAYOUT
        // ──────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            SuspendLayout();

            // Root layout: header row + 2×2 chart grid
            var root = new TableLayoutPanel
            {
                Dock       = DockStyle.Fill,
                RowCount   = 2,
                ColumnCount = 1,
                BackColor  = PageBackColor,
                Padding    = new Padding(24, 20, 24, 20)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildChartGrid(), 0, 1);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var panel = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                BackColor   = PageBackColor
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            // Title block
            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                Text      = "Dashboard Charts",
                ForeColor = TextColor,
                Font      = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 26,
                Text      = "Visual analytics — fees, enrollment, exam performance & collection trends",
                ForeColor = MutedColor,
                Font      = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Right-side actions
            var actions = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = PageBackColor,
                Padding       = new Padding(0, 14, 0, 0)
            };

            var refreshBtn = MakePrimaryButton("Refresh Charts");
            refreshBtn.Click += (s, e) => LoadAllCharts();

            statusLabel = new Label
            {
                AutoSize  = false,
                Width     = 220,
                Height    = 36,
                ForeColor = MutedColor,
                Font      = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleRight,
                Margin    = new Padding(0, 4, 8, 0)
            };

            actions.Controls.Add(refreshBtn);
            actions.Controls.Add(statusLabel);

            panel.Controls.Add(titleBlock, 0, 0);
            panel.Controls.Add(actions, 1, 0);
            return panel;
        }

        private Control BuildChartGrid()
        {
            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 2,
                BackColor   = PageBackColor
            };
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Chart 1 — Fees: Collected vs Outstanding
            chartFees = CreateChart();
            grid.Controls.Add(WrapInCard("Fees — Collected vs Outstanding", chartFees), 0, 0);

            // Chart 2 — Student Enrollment by Class
            chartEnrollment = CreateChart();
            grid.Controls.Add(WrapInCard("Student Enrollment by Class", chartEnrollment), 1, 0);

            // Chart 3 — Average Exam Score by Subject
            chartExams = CreateChart();
            grid.Controls.Add(WrapInCard("Average Exam Score by Subject", chartExams), 0, 1);

            // Chart 4 — Monthly Fee Collection Trend (line)
            chartTrend = CreateChart();
            grid.Controls.Add(WrapInCard("Monthly Fee Collection Trend", chartTrend), 1, 1);

            return grid;
        }

        /// <summary>Wraps a chart inside a titled white card panel.</summary>
        private Panel WrapInCard(string title, Chart chart)
        {
            var card = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Margin      = new Padding(0, 0, 10, 10),
                Padding     = new Padding(0)
            };

            var titleLabel = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 42,
                Padding   = new Padding(16, 0, 0, 0),
                Text      = title,
                ForeColor = TextColor,
                Font      = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SurfaceColor
            };

            chart.Dock = DockStyle.Fill;
            card.Controls.Add(chart);
            card.Controls.Add(titleLabel);
            return card;
        }

        /// <summary>Creates a blank Chart with shared styling applied.</summary>
        private Chart CreateChart()
        {
            var chart = new Chart { BackColor = SurfaceColor };
            chart.ChartAreas.Add(new ChartArea
            {
                BackColor        = SurfaceColor,
                BorderColor      = BorderColor,
                BorderWidth      = 1,
                AxisX = { MajorGrid = { LineColor = BorderColor }, LabelStyle = { Font = new Font("Segoe UI", 8F), ForeColor = MutedColor } },
                AxisY = { MajorGrid = { LineColor = Color.FromArgb(235, 238, 242) }, LabelStyle = { Font = new Font("Segoe UI", 8F), ForeColor = MutedColor } }
            });
            chart.Legends.Add(new Legend
            {
                Docking        = Docking.Bottom,
                Alignment      = StringAlignment.Center,
                BackColor      = SurfaceColor,
                BorderColor    = Color.Transparent,
                Font           = new Font("Segoe UI", 8.5F)
            });
            return chart;
        }

        // ──────────────────────────────────────────────────────────────
        //  DATA LOADING
        // ──────────────────────────────────────────────────────────────

        private void LoadAllCharts()
        {
            try
            {
                LoadFeesChart();
                LoadEnrollmentChart();
                LoadExamsChart();
                LoadTrendChart();
                statusLabel.Text = "Updated " + DateTime.Now.ToString("h:mm tt");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "DB unavailable";
                MessageBox.Show(
                    "Could not load chart data:\n\n" + ex.Message,
                    "Chart Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // ── Chart 1: Fees Collected vs Outstanding ────────────────────
        private void LoadFeesChart()
        {
            chartFees.Series.Clear();

            decimal collected  = ToDecimal(Scalar("SELECT COALESCE(SUM(Amount_paid), 0) FROM payment_record"));
            decimal outstanding = ToDecimal(Scalar("SELECT COALESCE(SUM(Balance), 0)   FROM payment_record WHERE Balance > 0"));

            var serCollected = new Series("Collected")
            {
                ChartType  = SeriesChartType.Bar,
                Color      = GreenColor,
                IsValueShownAsLabel = true,
                LabelFormat = "GHS #,##0.00",
                Font        = new Font("Segoe UI", 8F)
            };
            var serOutstanding = new Series("Outstanding")
            {
                ChartType  = SeriesChartType.Bar,
                Color      = RedColor,
                IsValueShownAsLabel = true,
                LabelFormat = "GHS #,##0.00",
                Font        = new Font("Segoe UI", 8F)
            };

            serCollected.Points.AddXY("Fees", (double)collected);
            serOutstanding.Points.AddXY("Fees", (double)outstanding);

            chartFees.Series.Add(serCollected);
            chartFees.Series.Add(serOutstanding);

            var area = chartFees.ChartAreas[0];
            area.AxisY.LabelStyle.Format = "GHS #,##0";
            area.AxisX.IsMarginVisible    = false;
        }

        // ── Chart 2: Enrollment by Class ─────────────────────────────
        private void LoadEnrollmentChart()
        {
            chartEnrollment.Series.Clear();

            DataTable dt = FetchTable("SELECT ClassID, COUNT(*) AS Total FROM Students GROUP BY ClassID ORDER BY ClassID");

            var series = new Series("Students")
            {
                ChartType           = SeriesChartType.Column,
                Color               = PrimaryColor,
                IsValueShownAsLabel = true,
                Font                = new Font("Segoe UI", 8F)
            };

            if (dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 0);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                    series.Points.AddXY(row["ClassID"].ToString(), Convert.ToInt32(row["Total"]));
            }

            chartEnrollment.Series.Add(series);

            var area = chartEnrollment.ChartAreas[0];
            area.AxisX.Interval        = 1;
            area.AxisX.IsMarginVisible = false;
            area.AxisY.Minimum         = 0;
        }

        // ── Chart 3: Average Exam Score by Subject ────────────────────
        private void LoadExamsChart()
        {
            chartExams.Series.Clear();

            // The gt column holds the combined CAT + Exam score.
            DataTable dt = FetchTable(
                "SELECT [subject], AVG(gt) AS AvgScore " +
                "FROM examss GROUP BY [subject] ORDER BY AVG(gt) DESC");

            var series = new Series("Avg Score")
            {
                ChartType           = SeriesChartType.Column,
                Color               = AmberColor,
                IsValueShownAsLabel = true,
                LabelFormat         = "0.0",
                Font                = new Font("Segoe UI", 8F)
            };

            if (dt.Rows.Count == 0)
            {
                series.Points.AddXY("No data", 0);
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    string subject = row["subject"].ToString();
                    // Shorten long subject names for readability
                    if (subject.Length > 10) subject = subject.Substring(0, 10) + "...";
                    series.Points.AddXY(subject, Convert.ToDouble(row["AvgScore"]));
                }
            }

            chartExams.Series.Add(series);

            var area = chartExams.ChartAreas[0];
            area.AxisX.Interval        = 1;
            area.AxisX.IsMarginVisible = false;
            area.AxisY.Minimum         = 0;
            area.AxisY.Maximum         = 100;
            area.AxisY.Title           = "Score / 100";
            area.AxisY.TitleFont       = new Font("Segoe UI", 8F);
        }

        // ── Chart 4: Monthly Fee Collection Trend (line) ──────────────
        private void LoadTrendChart()
        {
            chartTrend.Series.Clear();

            // Pull monthly totals for the current year
            int year = DateTime.Now.Year;
            DataTable dt = FetchTable(
                $"SELECT MONTH([Date]) AS Mo, SUM(Amount_paid) AS Total " +
                $"FROM payment_record " +
                $"WHERE YEAR([Date]) = {year} " +
                $"GROUP BY MONTH([Date]) " +
                $"ORDER BY MONTH([Date])");

            var series = new Series($"Collection {year}")
            {
                ChartType   = SeriesChartType.Line,
                Color       = PurpleColor,
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize  = 8,
                MarkerColor = PurpleColor,
                IsValueShownAsLabel = true,
                LabelFormat = "GHS #,##0",
                Font        = new Font("Segoe UI", 8F)
            };

            string[] monthNames = { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };

            if (dt.Rows.Count == 0)
            {
                // Show flat zero line so the chart area isn't empty
                for (int m = 1; m <= 12; m++)
                    series.Points.AddXY(monthNames[m - 1], 0);
            }
            else
            {
                // Build a lookup from month → amount
                var monthly = new decimal[13]; // index 1–12
                foreach (DataRow row in dt.Rows)
                {
                    int mo = Convert.ToInt32(row["Mo"]);
                    monthly[mo] = Convert.ToDecimal(row["Total"]);
                }

                // Only plot up to current month so the line doesn't show future zeros
                int lastMonth = DateTime.Now.Month;
                for (int m = 1; m <= lastMonth; m++)
                    series.Points.AddXY(monthNames[m - 1], (double)monthly[m]);
            }

            chartTrend.Series.Add(series);

            var area = chartTrend.ChartAreas[0];
            area.AxisX.Interval        = 1;
            area.AxisX.IsMarginVisible = false;
            area.AxisY.LabelStyle.Format = "GHS #,##0";
            area.AxisY.Minimum         = 0;
        }

        // ──────────────────────────────────────────────────────────────
        //  DB HELPERS
        // ──────────────────────────────────────────────────────────────

        private object Scalar(string sql)
        {
            using (var con = new OleDbConnection(Aikins.constr))
            using (var cmd = new OleDbCommand(sql, con))
            {
                con.Open();
                object v = cmd.ExecuteScalar();
                return (v == null || v == DBNull.Value) ? 0 : v;
            }
        }

        private DataTable FetchTable(string sql)
        {
            var dt = new DataTable();
            using (var con = new OleDbConnection(Aikins.constr))
            using (var adp = new OleDbDataAdapter(sql, con))
            {
                con.Open();
                adp.Fill(dt);
            }
            return dt;
        }

        private decimal ToDecimal(object v)
        {
            try { return Convert.ToDecimal(v); }
            catch { return 0; }
        }

        // ──────────────────────────────────────────────────────────────
        //  BUTTON FACTORY
        // ──────────────────────────────────────────────────────────────

        private Button MakePrimaryButton(string text)
        {
            var btn = new Button
            {
                Text      = text,
                Height    = 36,
                Width     = 148,
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 4, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 78, 160);
            return btn;
        }
    }
}
