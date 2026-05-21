using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmDashboard : Form
    {
        private readonly kum Aikins = new kum();
        private Label studentCountLabel;
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

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SidebarBackColor = UiTheme.Navy;
        private static readonly Color SidebarHoverColor = UiTheme.NavyHover;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public frmDashboard()
        {
            InitializeComponent();
            BuildModernDashboard();

            // Set this as the main dashboard in FormManager
            FormManager.SetMainDashboard(this);

            // Handle form closing to keep app alive
            this.FormClosing += FrmDashboard_FormClosing;
        }

        private void FrmDashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            // When dashboard close button is clicked, close all child forms and exit
            if (e.CloseReason == CloseReason.UserClosing)
            {
                FormManager.CloseAllForms();
                Application.Exit();
            }
        }

        private void BuildModernDashboard()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Kingdom Preparatory School Management System";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 680);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBackColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildSidebar(), 0, 0);
            root.Controls.Add(BuildContent(), 1, 0);
            Controls.Add(root);

            ResumeLayout(true);
        }

        private Control BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SidebarBackColor,
                Padding = new Padding(18, 22, 18, 18)
            };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Text = "KPS Admin",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var subtitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "School management",
                ForeColor = Color.FromArgb(182, 194, 210),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopLeft
            };

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Height = 470,
                Padding = new Padding(0, 16, 0, 0),
                BackColor = SidebarBackColor
            };

            nav.Controls.Add(CreateNavButton("Dashboard", null, true));
            nav.Controls.Add(CreateNavButton("Add Student", () => OpenForm(new frmAddStd(), true)));
            nav.Controls.Add(CreateNavButton("View Students", () => OpenForm(new frmStdView())));
            nav.Controls.Add(CreateNavButton("Add Employee", () => OpenForm(new frmEmployee())));
            nav.Controls.Add(CreateNavButton("View Employees", () => OpenForm(new frmEmpView())));
            nav.Controls.Add(CreateNavButton("Fees Payment", () => OpenForm(new frmFessPayment())));
            nav.Controls.Add(CreateNavButton("Exams", () => OpenForm(new EXAMS())));
            nav.Controls.Add(CreateNavButton("Exam Reports", () => OpenForm(new EXAMSVIEW())));
            nav.Controls.Add(CreateNavButton("Analytics", OpenAnalyticsDashboard));
            nav.Controls.Add(CreateNavButton("Leave Requests", () => OpenForm(new frmLeaveDetails())));

            var exitButton = CreateNavButton("Exit", Application.Exit);
            exitButton.Dock = DockStyle.Bottom;

            sidebar.Controls.Add(exitButton);
            sidebar.Controls.Add(nav);
            sidebar.Controls.Add(subtitle);
            sidebar.Controls.Add(title);

            return sidebar;
        }

        private Control BuildContent()
        {
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                Padding = new Padding(28),
                ColumnCount = 1,
                RowCount = 4
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Analytical Dashboard",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Text = "Live overview for students, staff, fees, exams, and leave",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            });

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleRight
            };

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(statusLabel, 1, 0);

            var metricGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                BackColor = PageBackColor,
                Padding = new Padding(0, 8, 0, 10)
            };
            for (int i = 0; i < 4; i++)
            {
                metricGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            studentCountLabel = new Label();
            employeeCountLabel = new Label();
            feesCollectedLabel = new Label();
            feesBalanceLabel = new Label();

            metricGrid.Controls.Add(CreateMetricCard("Students", studentCountLabel, "Active student records"), 0, 0);
            metricGrid.Controls.Add(CreateMetricCard("Employees", employeeCountLabel, "Current staff records"), 1, 0);
            metricGrid.Controls.Add(CreateMetricCard("Fees Collected", feesCollectedLabel, "Total recorded payments"), 2, 0);
            metricGrid.Controls.Add(CreateMetricCard("Outstanding Fees", feesBalanceLabel, "Positive fee balances"), 3, 0);

            var analyticsGrid = BuildAnalyticsGrid();
            var actionPanel = BuildQuickActionsPanel();

            content.Controls.Add(header, 0, 0);
            content.Controls.Add(metricGrid, 0, 1);
            content.Controls.Add(analyticsGrid, 0, 2);
            content.Controls.Add(actionPanel, 0, 3);

            return content;
        }

        private Control BuildAnalyticsGrid()
        {
            var analyticsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(0, 0, 0, 12)
            };
            analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            analyticsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

            recentPaymentsGrid = CreateAnalyticsGrid();
            classSummaryGrid = CreateAnalyticsGrid();
            leaveSummaryGrid = CreateAnalyticsGrid();
            recentPaymentsGrid.ScrollBars = ScrollBars.Both;
            classSummaryGrid.ScrollBars = ScrollBars.Vertical;
            leaveSummaryGrid.ScrollBars = ScrollBars.Vertical;

            var paymentsPanel = CreateSectionPanel("Recent Payments");
            paymentsPanel.Margin = new Padding(0, 0, 14, 0);
            paymentsPanel.Controls.Add(recentPaymentsGrid);

            var rightStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = PageBackColor
            };
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

            var classPanel = CreateSectionPanel("Class Enrollment");
            classPanel.Margin = new Padding(0, 0, 0, 12);
            classPanel.Controls.Add(classSummaryGrid);

            var insightPanel = CreateSectionPanel("Academic And Leave Insight");
            averageExamLabel = new Label();
            topClassLabel = new Label();
            pendingLeaveLabel = new Label();

            var insightBody = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 250,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 14)
            };
            insightBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            insightBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var academicSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };
            academicSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            academicSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            academicSummary.Controls.Add(CreateInsightTile("Average Score", averageExamLabel), 0, 0);
            academicSummary.Controls.Add(CreateInsightTile("Top Class", topClassLabel), 1, 0);

            var leaveSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            leaveSummary.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            leaveSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leaveSummary.Controls.Add(CreateInsightTile("Pending Leave", pendingLeaveLabel), 0, 0);
            leaveSummary.Controls.Add(leaveSummaryGrid, 0, 1);

            insightBody.Controls.Add(academicSummary, 0, 0);
            insightBody.Controls.Add(leaveSummary, 0, 1);

            var insightScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            insightScrollPanel.Controls.Add(insightBody);
            insightScrollPanel.Resize += (sender, args) => insightBody.Width = insightScrollPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            insightBody.Width = insightScrollPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            insightPanel.Controls.Add(insightScrollPanel);

            rightStack.Controls.Add(classPanel, 0, 0);
            rightStack.Controls.Add(insightPanel, 0, 1);
            analyticsGrid.Controls.Add(paymentsPanel, 0, 0);
            analyticsGrid.Controls.Add(rightStack, 1, 0);

            return analyticsGrid;
        }

        private Control BuildQuickActionsPanel()
        {
            var actionPanel = CreateSectionPanel("Quick Actions");
            actionPanel.Margin = new Padding(0);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(18, 10, 18, 14),
                BackColor = Color.White
            };
            for (int i = 0; i < 3; i++)
            {
                actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            }
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            actions.Controls.Add(CreateActionButton("Register Student", () => OpenForm(new frmAddStd(), true)), 0, 0);
            actions.Controls.Add(CreateActionButton("Register Employee", () => OpenForm(new frmEmployee())), 1, 0);
            actions.Controls.Add(CreateActionButton("Record Fees", () => OpenForm(new frmFessPayment())), 2, 0);
            actions.Controls.Add(CreateActionButton("Enter Exams", () => OpenForm(new EXAMS())), 0, 1);
            actions.Controls.Add(CreateActionButton("View Reports", () => OpenForm(new EXAMSVIEW())), 1, 1);
            actions.Controls.Add(CreateActionButton("Analytics Charts", OpenAnalyticsDashboard), 2, 1);
            actionPanel.Controls.Add(actions);

            return actionPanel;
        }

        private Button CreateNavButton(string text, Action action, bool selected = false)
        {
            var button = new Button
            {
                Width = 204,
                Height = 42,
                Margin = new Padding(0, 0, 0, 8),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = selected ? PrimaryColor : SidebarBackColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = selected ? PrimaryColor : SidebarHoverColor;
            button.FlatAppearance.MouseDownBackColor = Color.Black;
            if (action != null)
            {
                button.Click += (sender, args) => action();
            }
            return button;
        }

        private Control CreateMetricCard(string title, Label valueLabel, string caption)
        {
            var card = CreateModernPanel(8);
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 14, 0);
            card.Padding = new Padding(18);

            var titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Text = title,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel.Dock = DockStyle.Top;
            valueLabel.Height = 42;
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;

            var captionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = caption,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                TextAlign = ContentAlignment.BottomLeft
            };

            card.Controls.Add(captionLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            return card;
        }

        private Panel CreateCompactInsight(string title, Label valueLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 6)
            };

            valueLabel.Dock = DockStyle.Bottom;
            valueLabel.Height = 32;
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;

            panel.Controls.Add(valueLabel);
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = title,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            });

            return panel;
        }

        private Control CreateInsightTile(string title, Label valueLabel)
        {
            var tile = CreateModernPanel(7, UiTheme.SurfaceAlt, UiTheme.SurfaceAlt);
            tile.Dock = DockStyle.Fill;
            tile.Margin = new Padding(0, 0, 8, 0);
            tile.Padding = new Padding(12, 8, 12, 8);

            var titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = title,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Text = "--";
            valueLabel.ForeColor = TextColor;
            valueLabel.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.AutoEllipsis = true;

            tile.Controls.Add(valueLabel);
            tile.Controls.Add(titleLabel);
            return tile;
        }

        private Control CreateSectionPanel(string title)
        {
            var section = CreateModernPanel(8);
            section.Dock = DockStyle.Fill;
            section.Padding = new Padding(0, 46, 0, 0);
            var titleLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height = 46,
                Width = section.ClientSize.Width,
                Location = new Point(0, 0),
                Padding = new Padding(18, 0, 0, 0),
                Text = title,
                BackColor = Color.White,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            section.Resize += (sender, args) => titleLabel.Width = section.ClientSize.Width;
            section.Layout += (sender, args) => titleLabel.Width = section.ClientSize.Width;
            section.Controls.Add(titleLabel);
            titleLabel.BringToFront();
            return section;
        }

        private Guna.UI2.WinForms.Guna2Panel CreateModernPanel(int radius, Color? fill = null, Color? border = null)
        {
            var panel = new Guna.UI2.WinForms.Guna2Panel
            {
                BackColor = Color.Transparent,
                FillColor = fill ?? Color.White,
                BorderColor = border ?? UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = radius
            };
            panel.ShadowDecoration.Enabled = true;
            panel.ShadowDecoration.Depth = 4;
            panel.ShadowDecoration.Color = Color.FromArgb(28, 25, 25, 112);
            return panel;
        }

        private DataGridView CreateAnalyticsGrid()
        {
            var grid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 30 },
                GridColor = BorderColor,
                ScrollBars = ScrollBars.Both
            };
            grid.ThemeStyle.AlternatingRowsStyle.BackColor = UiTheme.SurfaceAlt;
            grid.ThemeStyle.BackColor = Color.White;
            grid.ThemeStyle.GridColor = BorderColor;
            grid.ThemeStyle.HeaderStyle.BackColor = PrimaryColor;
            grid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            grid.ThemeStyle.HeaderStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ThemeStyle.HeaderStyle.Height = 34;
            grid.ThemeStyle.RowsStyle.BackColor = Color.White;
            grid.ThemeStyle.RowsStyle.ForeColor = TextColor;
            grid.ThemeStyle.RowsStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.ThemeStyle.RowsStyle.SelectionForeColor = TextColor;
            grid.ThemeStyle.RowsStyle.Height = 30;

            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PrimaryColor;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = TextColor;
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;

            return grid;
        }

        private Control CreateActionButton(string text, Action action)
        {
            var button = new Guna.UI2.WinForms.Guna2Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                Text = text,
                FillColor = Color.White,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BorderColor = UiTheme.Border,
                BorderRadius = 6,
                BorderThickness = 1,
                PressedColor = Color.Black
            };
            button.HoverState.FillColor = UiTheme.GoldSoft;
            button.HoverState.BorderColor = UiTheme.Gold;
            button.HoverState.ForeColor = TextColor;
            button.Click += (sender, args) => action();
            return button;
        }

        private void OpenForm(Form form, bool hideDashboard = false)
        {
            if (form == null)
            {
                return;
            }

            try
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Show();
                form.BringToFront();
                form.Focus();
                if (hideDashboard)
                {
                    Hide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open " + form.Text + ": " + ex.Message, "Open Form Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenAnalyticsDashboard()
        {
            OpenForm(new frmDashboardCharts());
        }

        private void RefreshDashboardMetrics()
        {
            try
            {
                studentCountLabel.Text = ExecuteScalar("SELECT COUNT(*) FROM Students").ToString();
                employeeCountLabel.Text = ExecuteScalar("SELECT COUNT(*) FROM Employee").ToString();
                pendingLeaveLabel.Text = ExecuteScalar("SELECT COUNT(*) FROM emp_leave WHERE UPPER([status]) = 'PENDING'").ToString();

                decimal collected = Convert.ToDecimal(ExecuteScalar("SELECT COALESCE(SUM(Amount_paid), 0) FROM payment_record"));
                feesCollectedLabel.Text = FormatCurrency(collected);

                object feeResult = ExecuteScalar("SELECT COALESCE(SUM(Balance), 0) FROM payment_record WHERE Balance > 0");
                decimal balance = Convert.ToDecimal(feeResult);
                feesBalanceLabel.Text = FormatCurrency(balance);

                decimal averageExam = Convert.ToDecimal(ExecuteScalar("SELECT COALESCE(AVG(gt), 0) FROM examss"));
                averageExamLabel.Text = averageExam.ToString("0.0") + "%";
                string topClass = Convert.ToString(ExecuteScalar("SELECT TOP 1 ClassID FROM Students GROUP BY ClassID ORDER BY COUNT(*) DESC"));
                topClassLabel.Text = string.IsNullOrWhiteSpace(topClass) || topClass == "0" ? "No data" : topClass;

                recentPaymentsGrid.DataSource = FetchTable(
                    "SELECT TOP 8 StudentID AS [ID], student_name AS [Student], classID AS [Class], Amount_paid AS [Paid], Balance, [Date] " +
                    "FROM payment_record ORDER BY [Date] DESC, tm DESC");
                classSummaryGrid.DataSource = FetchTable(
                    "SELECT ClassID AS [Class], COUNT(*) AS [Students] FROM Students GROUP BY ClassID ORDER BY ClassID");
                leaveSummaryGrid.DataSource = FetchTable(
                    "SELECT [status] AS [Status], COUNT(*) AS [Total] FROM emp_leave GROUP BY [status] ORDER BY [status]");

                statusLabel.Text = "Connected to Neat_Academy | " + DateTime.Now.ToString("dd MMM yyyy, h:mm tt");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Database unavailable";
                MessageBox.Show("Dashboard could not load live metrics: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatCurrency(decimal amount)
        {
            return "GHS " + amount.ToString("#,##0.00");
        }

        private object ExecuteScalar(string query)
        {
            using (OleDbConnection con = new OleDbConnection(Aikins.constr))
            using (OleDbCommand cmd = new OleDbCommand(query, con))
            {
                con.Open();
                object value = cmd.ExecuteScalar();
                return value == DBNull.Value || value == null ? 0 : value;
            }
        }

        private DataTable FetchTable(string query)
        {
            var table = new DataTable();
            using (OleDbConnection con = new OleDbConnection(Aikins.constr))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, con))
            {
                con.Open();
                adapter.Fill(table);
            }

            return table;
        }

        private void gunaPictureBox1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void gunaPictureBox2_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void gunaPictureBox3_Click(object sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void bindingNavigatorCountItem_Click(object sender, EventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }
        private void guna2Panel1_Paint(object sender, PaintEventArgs e) { }
        private void gunaPanel3_Paint(object sender, PaintEventArgs e) { }
        private void tStd_Click(object sender, EventArgs e) { }
        private void gunaLabel5_Click(object sender, EventArgs e) { }
        private void sendComplaintsToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void summaryToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void classToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void classToolStripMenuItem1_Click(object sender, EventArgs e) { }
        private void gunaButton11_Click(object sender, EventArgs e) { }
        private void gunaButton9_Click(object sender, EventArgs e) { }

        private void gunaButton1_Click(object sender, EventArgs e) { OpenForm(new frmAddStd(), true); }
        private void gunaButton2_Click(object sender, EventArgs e) { OpenForm(new frmStdView()); }
        private void gunaButton3_Click(object sender, EventArgs e) { OpenForm(new frmEmployee()); }
        private void gunaButton4_Click(object sender, EventArgs e) { OpenForm(new frmEmpView()); }
        private void gunaButton5_Click(object sender, EventArgs e) { OpenForm(new EXAMSVIEW()); }
        private void gunaButton6_Click(object sender, EventArgs e) { OpenForm(new EXAMS()); }
        private void gunaButton7_Click(object sender, EventArgs e) { OpenForm(new frmFessPayment()); }
        private void gunaButton8_Click(object sender, EventArgs e) { OpenForm(new frmFess()); }
        private void gunaButton10_Click(object sender, EventArgs e) { OpenForm(new frmEmpLeave()); }
        private void gunaButton13_Click(object sender, EventArgs e) { OpenForm(new frmLeaveDetails()); }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmAddStd()); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmEmpLeave()); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { OpenForm(new frmStdView()); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { OpenForm(new frmEmpView()); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmFessPayment()); }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) { OpenForm(new frmAbout()); }

        private void frmDashboard_Load(object sender, EventArgs e)
        {
            RefreshDashboardMetrics();
        }

        private void gunaButton12_Click(object sender, EventArgs e)
        {
            RefreshDashboardMetrics();
        }
    }
}
