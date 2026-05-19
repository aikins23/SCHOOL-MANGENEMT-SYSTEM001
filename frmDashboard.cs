using System;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmDashboard : Form
    {
        private readonly kum Aikins = new kum();
        private Label studentCountLabel;
        private Label employeeCountLabel;
        private Label feesBalanceLabel;
        private Label pendingLeaveLabel;
        private Label statusLabel;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SidebarBackColor = Color.FromArgb(17, 35, 58);
        private static readonly Color SidebarHoverColor = Color.FromArgb(31, 55, 86);
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        public frmDashboard()
        {
            InitializeComponent();
            BuildModernDashboard();
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
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                Padding = new Padding(28)
            };

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 76,
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
                Text = "Dashboard",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Text = "Operational overview for students, staff, fees, and exams",
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
                Dock = DockStyle.Top,
                Height = 142,
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
            feesBalanceLabel = new Label();
            pendingLeaveLabel = new Label();

            metricGrid.Controls.Add(CreateMetricCard("Students", studentCountLabel, "Active student records"), 0, 0);
            metricGrid.Controls.Add(CreateMetricCard("Employees", employeeCountLabel, "Current staff records"), 1, 0);
            metricGrid.Controls.Add(CreateMetricCard("Outstanding Fees", feesBalanceLabel, "Positive fee balances"), 2, 0);
            metricGrid.Controls.Add(CreateMetricCard("Pending Leave", pendingLeaveLabel, "Awaiting approval"), 3, 0);

            var actionPanel = CreateSectionPanel("Quick Actions");
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(18, 52, 18, 18),
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
            actions.Controls.Add(CreateActionButton("Leave Details", () => OpenForm(new frmLeaveDetails())), 2, 1);
            actionPanel.Controls.Add(actions);

            content.Controls.Add(actionPanel);
            content.Controls.Add(metricGrid);
            content.Controls.Add(header);

            return content;
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
            if (action != null)
            {
                button.Click += (sender, args) => action();
            }
            return button;
        }

        private Panel CreateMetricCard(string title, Label valueLabel, string caption)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 14, 0),
                BackColor = Color.White,
                Padding = new Padding(18),
                BorderStyle = BorderStyle.FixedSingle
            };

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

        private Panel CreateSectionPanel(string title)
        {
            var section = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            section.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(18, 0, 0, 0),
                Text = title,
                BackColor = Color.White,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return section;
        }

        private Button CreateActionButton(string text, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                Text = text,
                BackColor = Color.White,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 242, 255);
            button.Click += (sender, args) => action();
            return button;
        }

        private void OpenForm(Form form, bool hideDashboard = false)
        {
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Show();
            if (hideDashboard)
            {
                Hide();
            }
        }

        private void RefreshDashboardMetrics()
        {
            try
            {
                studentCountLabel.Text = ExecuteScalar("SELECT COUNT(*) FROM Students").ToString();
                employeeCountLabel.Text = ExecuteScalar("SELECT COUNT(*) FROM Employee").ToString();
                pendingLeaveLabel.Text = ExecuteScalar("SELECT COUNT(*) FROM emp_leave WHERE [status] = 'PENDING'").ToString();

                object feeResult = ExecuteScalar("SELECT COALESCE(SUM(Balance), 0) FROM payment_record WHERE Balance > 0");
                decimal balance = Convert.ToDecimal(feeResult);
                feesBalanceLabel.Text = "GHS " + balance.ToString("0.00");
                statusLabel.Text = "Connected to Neat_Academy | " + DateTime.Now.ToString("dd MMM yyyy, h:mm tt");
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Database unavailable";
                MessageBox.Show("Dashboard could not load live metrics: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
