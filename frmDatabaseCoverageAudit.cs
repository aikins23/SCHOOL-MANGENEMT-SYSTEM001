using System;
using System.Collections.Generic;
using System.Data;

using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmDatabaseCoverageAudit : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Label _summaryLabel = new Label();
        private readonly Button _refreshButton = new Button();

        private static readonly Dictionary<string, CoverageInfo> CoverageMap =
            new Dictionary<string, CoverageInfo>(StringComparer.OrdinalIgnoreCase)
            {
                { "Students", Covered("Student Management", "frmAddStd, frmStdView, frmStdDetails") },
                { "Employee", Covered("Staff Management", "frmEmployee, frmEmpView, frmEmpDetails") },
                { "fees", Covered("Fees Setup", "frmFess, FeeRepository") },
                { "payment_record", Covered("Payments", "frmFessPayment, frmPaymentHistory, frmOutstandingFees") },
                { "Attendance", Covered("Attendance", "frmAttendance") },
                { "examss", Covered("Exams And Report Cards", "EXAMS, EXAMSVIEW, report card services") },
                { "emp_leave", Covered("Leave Management", "frmEmpLeave, frmLeaveDetails, frmLeaveApproval") },
                { "Classes", Covered("Class Management", "frmClassManager") },
                { "ClassAssignments", Covered("Class Teacher Assignment", "frmClassManager") },
                { "ClassSubjects", Covered("Curriculum Subjects", "frmClassManager, frmSubjects") },
                { "ClassFees", Covered("School Fee Settings", "frmSchoolInfo") },
                { "SchoolInformation", Covered("School Profile", "frmSchoolInfo") },
                { "GradingScheme", Covered("Grading Settings", "frmGradingScheme") },
                { "Books", Covered("Library", "frmLibrary") },
                { "BookLoans", Covered("Library Loans", "frmLibrary") },
                { "Buses", Covered("Transport", "frmTransport") },
                { "BusRoutes", Covered("Transport Routes", "frmTransport") },
                { "StudentTransport", Covered("Student Transport Assignment", "frmTransport") },
                { "TransportPayment", Covered("Transport Payments", "frmTransportPayments") },
                { "TransportReminderLog", Internal("Transport reminder log", "Background tracking only; add a log viewer later if needed.") },
                { "Expenses", Covered("Expenses", "frmExpenses") },
                { "ExpenseCategories", Covered("Expense Categories", "frmExpenses") },
                { "Notices", Covered("Notice Board", "frmNotice, frmSendNotice") },
                { "DraftAdmissions", Covered("Admission Approvals", "frmPendingApprovals") },
                { "StudentTermRemarks", Covered("Report Card Remarks", "StudentTermRemarksRepository, report card services") },
                { "Rolled_Out_Students", Partial("Student Archive", "Promotion writes records; add an archive browser/export screen.") },
                { "Rolled_Out_Employees", Partial("Staff Archive", "Employee roll-out writes records; add an archive browser/export screen.") },
                { "SmsOutbox", Partial("SMS Queue", "Background queue exists; add a failed/pending SMS monitor screen.") },
                { "Users", Partial("User Accounts", "Login/register exists; add admin user list, reset, deactivate, roles.") },
                { "rooms", Missing("Legacy Class Rooms", "Either migrate to Classes/ClassAssignments or build a room/classroom manager.") },
                { "sba", Missing("Legacy SBA Scores", "Either migrate to examss or build SBA score entry/reporting.") },
                { "suplier", Missing("Suppliers", "Build supplier/vendor management or remove this legacy table.") },
                { "supplies", Missing("Supplies/Procurement", "Build supplies inventory/procurement UI or remove this legacy table.") },
                { "sysdiagrams", Internal("SQL Server diagrams", "System table; no ERP UI needed.") }
            };

        public frmDatabaseCoverageAudit()
        {
            if (!AuthService.RequireAccess("frmDatabaseCoverageAudit", this)) return;
            InitializeComponent();
            _ = LoadCoverageAsync();
        }

        private void InitializeComponent()
        {
            Text = "Database Coverage Audit";
            Size = new Size(1180, 760);
            MinimumSize = new Size(1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.25F);
            Padding = new Padding(22);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = UiTheme.Page
            };
            Controls.Add(header);

            header.Controls.Add(new Label
            {
                Text = "Database Coverage Audit",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                Location = new Point(0, 4),
                AutoSize = true
            });

            header.Controls.Add(new Label
            {
                Text = "Live database tables compared with available modules and UI screens.",
                ForeColor = UiTheme.Muted,
                Location = new Point(2, 44),
                AutoSize = true
            });

            _refreshButton.Text = "Refresh";
            _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _refreshButton.Size = new Size(120, 38);
            _refreshButton.Location = new Point(1012, 12);
            _refreshButton.BackColor = UiTheme.Navy;
            _refreshButton.ForeColor = Color.White;
            _refreshButton.FlatStyle = FlatStyle.Flat;
            _refreshButton.FlatAppearance.BorderSize = 0;
            _refreshButton.Click += async (sender, args) => await LoadCoverageAsync();
            header.Controls.Add(_refreshButton);
            header.Resize += (sender, args) => _refreshButton.Left = header.Width - _refreshButton.Width;

            _summaryLabel.Dock = DockStyle.Top;
            _summaryLabel.Height = 34;
            _summaryLabel.ForeColor = UiTheme.Muted;
            _summaryLabel.BackColor = UiTheme.Page;
            _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_summaryLabel);

            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.CellFormatting += Grid_CellFormatting;
            Controls.Add(_grid);
            UiTheme.StyleDataGrid(_grid, true);
        }

        private async Task LoadCoverageAsync()
        {
            _refreshButton.Enabled = false;
            _summaryLabel.Text = "Checking database coverage...";

            try
            {
                DataTable table = await GetLiveTablesAsync();
                _grid.DataSource = table;

                int missing = CountStatus(table, "Missing UI");
                int partial = CountStatus(table, "Partial");
                int covered = CountStatus(table, "Covered");
                _summaryLabel.Text = $"Tables: {table.Rows.Count} | Covered: {covered} | Partial: {partial} | Missing UI: {missing}";
            }
            catch (Exception ex)
            {
                _summaryLabel.Text = "Could not audit database: " + ex.Message;
                LoggerHelper.LogError("Database coverage audit failed", ex);
            }
            finally
            {
                _refreshButton.Enabled = true;
            }
        }

        private static async Task<DataTable> GetLiveTablesAsync()
        {
            var result = CreateResultTable();

            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(SqlCommandExtensions.StripProvider(AppConfig.ConnectionString)))
            {
                await connection.OpenAsync();
                const string sql = @"
                    SELECT t.name AS TableName, SUM(p.rows) AS [TableRows]
                    FROM sys.tables t
                    INNER JOIN sys.partitions p ON t.object_id = p.object_id
                    WHERE t.is_ms_shipped = 0
                    GROUP BY t.name
                    ORDER BY t.name";

                using (var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string tableName = reader["TableName"].ToString();
                        int rowCount = reader["TableRows"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TableRows"]);
                        CoverageInfo info = CoverageMap.TryGetValue(tableName, out var mapped)
                            ? mapped
                            : Missing("Unmapped Table", "No coverage map found. Review whether this table needs a module.");

                        result.Rows.Add(tableName, rowCount, info.Status, info.Module, info.Action);
                    }
                }
            }

            return result;
        }

        private static DataTable CreateResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("Table", typeof(string));
            table.Columns.Add("Rows", typeof(int));
            table.Columns.Add("Coverage", typeof(string));
            table.Columns.Add("Module / Purpose", typeof(string));
            table.Columns.Add("Attention Needed", typeof(string));
            return table;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].Cells["Coverage"].Value == null)
            {
                return;
            }

            string status = _grid.Rows[e.RowIndex].Cells["Coverage"].Value.ToString();
            if (status == "Missing UI")
            {
                _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 238, 238);
                _grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(127, 29, 29);
            }
            else if (status == "Partial")
            {
                _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                _grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(92, 64, 16);
            }
            else if (status == "Internal")
            {
                _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
                _grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = UiTheme.Muted;
            }
        }

        private static int CountStatus(DataTable table, string status)
        {
            int count = 0;
            foreach (DataRow row in table.Rows)
            {
                if (string.Equals(row["Coverage"].ToString(), status, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        private static CoverageInfo Covered(string module, string action)
        {
            return new CoverageInfo("Covered", module, action);
        }

        private static CoverageInfo Partial(string module, string action)
        {
            return new CoverageInfo("Partial", module, action);
        }

        private static CoverageInfo Missing(string module, string action)
        {
            return new CoverageInfo("Missing UI", module, action);
        }

        private static CoverageInfo Internal(string module, string action)
        {
            return new CoverageInfo("Internal", module, action);
        }

        private class CoverageInfo
        {
            public CoverageInfo(string status, string module, string action)
            {
                Status = status;
                Module = module;
                Action = action;
            }

            public string Status { get; }
            public string Module { get; }
            public string Action { get; }
        }
    }
}
