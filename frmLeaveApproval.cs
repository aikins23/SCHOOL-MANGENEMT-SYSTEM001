using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmLeaveApproval : Form
    {
        private readonly kum Aikins = new kum();
        private DataGridView leaveGrid;
        private ComboBox statusFilter;
        private TextBox searchBox;
        private Label resultLabel;
        private DataTable leaveTable;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color SidebarBackColor = Color.FromArgb(17, 35, 58);
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        public frmLeaveApproval()
        {
            InitializeComponent();
            BuildModernApprovalView();
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
            actions.Controls.Add(CreatePrimaryButton("Apply Leave", () => new frmEmpLeave().Show()));
            actions.Controls.Add(CreateSecondaryButton("Refresh", LoadLeaveRequests));
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
            leaveGrid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = SurfaceColor, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, EnableHeadersVisualStyles = false };
            leaveGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            leaveGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            leaveGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            leaveGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            leaveGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            leaveGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            leaveGrid.GridColor = BorderColor;
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
            actions.Controls.Add(CreatePrimaryButton("Approve", () => UpdateSelectedStatus("APPROVED")), 0, 0);
            actions.Controls.Add(CreateDangerButton("Reject", () => UpdateSelectedStatus("REJECTED")), 1, 0);
            actions.Controls.Add(CreateSecondaryButton("Refresh", LoadLeaveRequests), 2, 0);
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
            button.Click += (sender, args) => action();
            return button;
        }

        private void LoadLeaveRequests()
        {
            try
            {
                string query = @"SELECT employmentID AS [EMPLOYEE ID], name AS [NAME], department AS [DEPARTMENT], position AS [POSITION], Leave_op AS [PAY OPTION], Reasons AS [REASON], Start_Date AS [START DATE], End_Date AS [END DATE], status AS [STATUS] FROM emp_leave ORDER BY Start_Date DESC";
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand(query, con))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                {
                    leaveTable = new DataTable();
                    adapter.Fill(leaveTable);
                    leaveGrid.DataSource = leaveTable;
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Leave Approval", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilters()
        {
            if (leaveTable == null) return;
            var filters = new System.Collections.Generic.List<string>();
            string search = searchBox.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(search))
            {
                filters.Add("[NAME] LIKE '%" + search + "%' OR Convert([EMPLOYEE ID], 'System.String') LIKE '%" + search + "%'");
            }
            if (statusFilter.SelectedIndex > 0)
            {
                filters.Add("[STATUS] = '" + statusFilter.Text + "'");
            }
            leaveTable.DefaultView.RowFilter = string.Join(" AND ", filters);
            resultLabel.Text = leaveTable.DefaultView.Count + " leave request(s)";
        }

        private void UpdateSelectedStatus(string status)
        {
            if (leaveGrid.CurrentRow == null)
            {
                MessageBox.Show("Select a leave request first.", "Leave Approval", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int employeeId = Convert.ToInt32(leaveGrid.CurrentRow.Cells["EMPLOYEE ID"].Value);
            DateTime startDate = Convert.ToDateTime(leaveGrid.CurrentRow.Cells["START DATE"].Value);
            DateTime endDate = Convert.ToDateTime(leaveGrid.CurrentRow.Cells["END DATE"].Value);

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand("UPDATE emp_leave SET status = ? WHERE employmentID = ? AND Start_Date = ? AND End_Date = ?", con))
                {
                    command.Parameters.AddWithValue("?", status);
                    command.Parameters.AddWithValue("?", employeeId);
                    command.Parameters.AddWithValue("?", startDate.Date);
                    command.Parameters.AddWithValue("?", endDate.Date);
                    con.Open();
                    command.ExecuteNonQuery();
                }
                LoadLeaveRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Status could not be updated: " + ex.Message, "Leave Approval", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmLeaveApproval_Load(object sender, EventArgs e) { LoadLeaveRequests(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Close(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
        private void menuStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
    }
}
