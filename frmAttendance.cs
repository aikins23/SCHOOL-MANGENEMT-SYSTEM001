using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAttendance : Form
    {
        private readonly kum Aikins = new kum();
        private Guna.UI2.WinForms.Guna2DataGridView dataGrid;
        private Guna.UI2.WinForms.Guna2ComboBox comboType;
        private Guna.UI2.WinForms.Guna2DateTimePicker datePicker;
        private Guna.UI.WinForms.GunaLabel lblStats;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color Navy = UiTheme.Navy;

        public frmAttendance()
        {
            InitializeComponent();
            EnsureDatabaseTableExists();
        }

        private void frmAttendance_Load(object sender, EventArgs e)
        {
            BuildModernLayout();
            NavigationSidebar.AddTo(this);
            UiTheme.Apply(this);
        }

        private void EnsureDatabaseTableExists()
        {
            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    string checkSql = "SELECT 1 FROM sys.tables WHERE name = 'Attendance'";
                    OleDbCommand checkCmd = new OleDbCommand(checkSql, con);
                    var exists = checkCmd.ExecuteScalar();

                    if (exists == null)
                    {
                        string createSql = @"
CREATE TABLE Attendance
(
    AttendanceID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReferenceID int NOT NULL,
    ReferenceType varchar(20) NOT NULL,
    FullName varchar(120) NOT NULL,
    [Date] date NOT NULL,
    [Status] varchar(20) NOT NULL,
    Remarks varchar(200) NULL,
    [CreatedDate] datetime NOT NULL DEFAULT GETDATE()
);
CREATE INDEX IX_Attendance_RefDate ON Attendance(ReferenceID, ReferenceType, [Date]);";
                        OleDbCommand createCmd = new OleDbCommand(createSql, con);
                        createCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database setup error: " + ex.Message);
            }
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            Text = "Attendance Management";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackColor;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(220, 20, 20, 20) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var pnlHeader = new Panel { Dock = DockStyle.Fill };
            pnlHeader.Controls.Add(new Label { Text = "Attendance Records", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Navy, AutoSize = true, Location = new Point(0, 5) });
            pnlHeader.Controls.Add(new Label { Text = "Mark and monitor daily presence of students and staff", Font = new Font("Segoe UI", 10), ForeColor = UiTheme.Muted, AutoSize = true, Location = new Point(2, 40) });
            root.Controls.Add(pnlHeader, 0, 0);

            var pnlFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 10, 0, 0) };
            comboType = new Guna.UI2.WinForms.Guna2ComboBox { Width = 150, Height = 36 };
            comboType.Items.AddRange(new object[] { "STUDENT", "STAFF" });
            comboType.SelectedIndex = 0;
            comboType.SelectedIndexChanged += (s, e) => LoadTargetList();

            datePicker = new Guna.UI2.WinForms.Guna2DateTimePicker { Width = 200, Height = 36, Value = DateTime.Today };
            datePicker.ValueChanged += (s, e) => LoadTargetList();

            lblStats = new Guna.UI.WinForms.GunaLabel { Text = "Loading...", Font = new Font("Segoe UI", 10), ForeColor = Navy, AutoSize = true, Margin = new Padding(20, 10, 0, 0) };
            pnlFilters.Controls.Add(new Label { Text = "Type:", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
            pnlFilters.Controls.Add(comboType);
            pnlFilters.Controls.Add(new Label { Text = "Date:", AutoSize = true, Margin = new Padding(15, 10, 5, 0) });
            pnlFilters.Controls.Add(datePicker);
            pnlFilters.Controls.Add(lblStats);
            root.Controls.Add(pnlFilters, 0, 1);

            dataGrid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 40,
                ReadOnly = false,
                AllowUserToAddRows = false
            };
            dataGrid.ThemeStyle.HeaderStyle.BackColor = Navy;
            dataGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dataGrid.ThemeStyle.RowsStyle.Height = 35;
            root.Controls.Add(dataGrid, 0, 2);

            var btnSave = new Guna.UI.WinForms.GunaButton { Text = "Save Attendance", BaseColor = Navy, ForeColor = Color.White, Width = 160, Height = 40, Location = new Point(900, 15), Cursor = Cursors.Hand };
            btnSave.Click += (s, e) => SaveAttendance();
            pnlHeader.Controls.Add(btnSave);

            Controls.Add(root);
            ResumeLayout(true);
            LoadTargetList();
        }

        private void LoadTargetList()
        {
            string type = comboType.Text;
            DateTime date = datePicker.Value.Date;
            string query = type == "STUDENT" ? @"
SELECT s.StudentID AS [ID], s.FirstName + ' ' + s.LastName AS [Full Name], s.ClassID AS [Class], COALESCE(a.Status, 'PRESENT') AS [Status], COALESCE(a.Remarks, '') AS [Remarks]
FROM Students s LEFT JOIN Attendance a ON s.StudentID = a.ReferenceID AND a.ReferenceType = 'STUDENT' AND a.[Date] = ?
ORDER BY s.ClassID, s.FirstName" : @"
SELECT e.employmentID AS [ID], e.fullName AS [Full Name], e.position AS [Position], COALESCE(a.Status, 'PRESENT') AS [Status], COALESCE(a.Remarks, '') AS [Remarks]
FROM Employee e LEFT JOIN Attendance a ON e.employmentID = a.ReferenceID AND a.ReferenceType = 'STAFF' AND a.[Date] = ?
ORDER BY e.fullName";

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    OleDbCommand cmd = new OleDbCommand(query, con);
                    cmd.Parameters.AddWithValue("?", date);
                    OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGrid.DataSource = dt;

                    if (!dataGrid.Columns.Contains("StatusCol"))
                    {
                        var col = new DataGridViewComboBoxColumn { Name = "StatusCol", HeaderText = "Mark Status", DataPropertyName = "Status", DataSource = new string[] { "PRESENT", "ABSENT", "LATE" }, FlatStyle = FlatStyle.Flat };
                        dataGrid.Columns.Add(col);
                        dataGrid.Columns["Status"].Visible = false;
                    }
                    UpdateStats(dt);
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading list: " + ex.Message); }
        }

        private void UpdateStats(DataTable dt)
        {
            int total = dt.Rows.Count;
            int present = 0;
            foreach (DataRow row in dt.Rows) if (row["Status"].ToString() == "PRESENT") present++;
            lblStats.Text = $"Total: {total} | Present: {present} | Absent: {total - present}";
        }

        private void SaveAttendance()
        {
            string type = comboType.Text;
            DateTime date = datePicker.Value.Date;

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    foreach (DataGridViewRow row in dataGrid.Rows)
                    {
                        int id = Convert.ToInt32(row.Cells["ID"].Value);
                        string name = row.Cells["Full Name"].Value.ToString();
                        string status = row.Cells["StatusCol"].Value.ToString();
                        string remarks = row.Cells["Remarks"].Value?.ToString() ?? "";

                        string checkQuery = "SELECT COUNT(*) FROM Attendance WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
                        OleDbCommand checkCmd = new OleDbCommand(checkQuery, con);
                        checkCmd.Parameters.AddWithValue("?", id);
                        checkCmd.Parameters.AddWithValue("?", type);
                        checkCmd.Parameters.AddWithValue("?", date);
                        
                        if ((int)checkCmd.ExecuteScalar() > 0)
                        {
                            string updateQuery = "UPDATE Attendance SET [Status] = ?, Remarks = ? WHERE ReferenceID = ? AND ReferenceType = ? AND [Date] = ?";
                            OleDbCommand upCmd = new OleDbCommand(updateQuery, con);
                            upCmd.Parameters.AddWithValue("?", status);
                            upCmd.Parameters.AddWithValue("?", remarks);
                            upCmd.Parameters.AddWithValue("?", id);
                            upCmd.Parameters.AddWithValue("?", type);
                            upCmd.Parameters.AddWithValue("?", date);
                            upCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            string insertQuery = "INSERT INTO Attendance (ReferenceID, ReferenceType, FullName, [Date], [Status], Remarks) VALUES (?, ?, ?, ?, ?, ?)";
                            OleDbCommand insCmd = new OleDbCommand(insertQuery, con);
                            insCmd.Parameters.AddWithValue("?", id);
                            insCmd.Parameters.AddWithValue("?", type);
                            insCmd.Parameters.AddWithValue("?", name);
                            insCmd.Parameters.AddWithValue("?", date);
                            insCmd.Parameters.AddWithValue("?", status);
                            insCmd.Parameters.AddWithValue("?", remarks);
                            insCmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Attendance saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTargetList();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error saving attendance: " + ex.Message); }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1084, 661);
            this.Name = "frmAttendance";
            this.Load += new System.EventHandler(this.frmAttendance_Load);
            this.ResumeLayout(false);
        }
    }
}
