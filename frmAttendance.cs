using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAttendance : Form
    {
        private readonly AttendanceService _attendanceService;
        private Guna.UI2.WinForms.Guna2DataGridView dataGrid;
        private Guna.UI2.WinForms.Guna2ComboBox comboType;
        private Guna.UI2.WinForms.Guna2ComboBox comboClass;
        private Guna.UI2.WinForms.Guna2TextBox txtSearch;
        private Guna.UI2.WinForms.Guna2DateTimePicker datePicker;
        private Guna.UI.WinForms.GunaLabel lblStats;
        private bool isReportMode = false;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color Navy = UiTheme.Navy;

        public frmAttendance()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var repository = new AttendanceRepository(AppConfig.ConnectionString);
            _attendanceService = new AttendanceService(repository);
        }

        private async void frmAttendance_Load(object sender, EventArgs e)
        {
            BuildModernLayout();
            NavigationSidebar.AddTo(this);
            UiTheme.Apply(this);

            await _attendanceService.InitializeAsync();
            await LoadClasses();
            await LoadTargetList();
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            Text = "Attendance Management";
            Size = new Size(1180, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackColor;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(220, 20, 20, 20) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Fill };
            pnlHeader.Controls.Add(new Label { Text = "Attendance Records", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Navy, AutoSize = true, Location = new Point(0, 5) });
            pnlHeader.Controls.Add(new Label { Text = "Mark and monitor daily presence of students and staff", Font = new Font("Segoe UI", 10), ForeColor = UiTheme.Muted, AutoSize = true, Location = new Point(2, 40) });
            root.Controls.Add(pnlHeader, 0, 0);

            // Filter Bar
            var pnlFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 10, 0, 0), WrapContents = false };
            
            comboType = new Guna.UI2.WinForms.Guna2ComboBox { Width = 110, Height = 36 };
            comboType.Items.AddRange(new object[] { "STUDENT", "STAFF" });
            comboType.SelectedIndex = 0;
            comboType.SelectedIndexChanged += async (s, e) => {
                comboClass.Visible = (comboType.Text == "STUDENT");
                await LoadTargetList();
            };

            comboClass = new Guna.UI2.WinForms.Guna2ComboBox { Width = 140, Height = 36, Margin = new Padding(10, 0, 0, 0) };
            comboClass.Items.Add("All Classes");
            comboClass.SelectedIndex = 0;
            comboClass.SelectedIndexChanged += async (s, e) => await LoadTargetList();

            txtSearch = new Guna.UI2.WinForms.Guna2TextBox { Width = 150, Height = 36, PlaceholderText = "Search Name...", Margin = new Padding(10, 0, 0, 0) };
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            datePicker = new Guna.UI2.WinForms.Guna2DateTimePicker { Width = 150, Height = 36, Value = DateTime.Today, Margin = new Padding(10, 0, 0, 0) };
            datePicker.ValueChanged += async (s, e) => await LoadTargetList();

            lblStats = new Guna.UI.WinForms.GunaLabel { Text = "Loading...", Font = new Font("Segoe UI", 10), ForeColor = Navy, AutoSize = true, Margin = new Padding(20, 10, 0, 0) };

            var btnMarkAll = new Guna.UI.WinForms.GunaButton { Text = "Mark All", BaseColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, Width = 100, Height = 36, Margin = new Padding(10, 0, 0, 0), Cursor = Cursors.Hand };
            btnMarkAll.Click += (s, e) => MarkAll("PRESENT");

            var btnClear = new Guna.UI.WinForms.GunaButton { Text = "Clear All", BaseColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, Width = 90, Height = 36, Margin = new Padding(10, 0, 0, 0), Cursor = Cursors.Hand };
            btnClear.Click += (s, e) => MarkAll("");

            var btnReport = new Guna.UI.WinForms.GunaButton { Text = "Analysis", BaseColor = Navy, ForeColor = Color.White, Width = 100, Height = 36, Margin = new Padding(10, 0, 0, 0), Cursor = Cursors.Hand };
            btnReport.Click += async (s, e) => await ToggleReportMode();

            pnlFilters.Controls.Add(new Label { Text = "Type:", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
            pnlFilters.Controls.Add(comboType);
            pnlFilters.Controls.Add(comboClass);
            pnlFilters.Controls.Add(txtSearch);
            pnlFilters.Controls.Add(new Label { Text = "Date:", AutoSize = true, Margin = new Padding(10, 10, 5, 0) });
            pnlFilters.Controls.Add(datePicker);
            pnlFilters.Controls.Add(btnMarkAll);
            pnlFilters.Controls.Add(btnClear);
            pnlFilters.Controls.Add(btnReport);
            pnlFilters.Controls.Add(lblStats);

            root.Controls.Add(pnlFilters, 0, 1);

            // Grid
            dataGrid = new Guna.UI2.WinForms.Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 40,
                ReadOnly = false,
                AllowUserToAddRows = false
            };
            UiTheme.StyleDataGrid(dataGrid);
            dataGrid.ThemeStyle.HeaderStyle.BackColor = Navy;
            dataGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White;
            dataGrid.ThemeStyle.RowsStyle.Height = 35;
            dataGrid.DataBindingComplete += (s, e) => FlagLowAttendance();
            
            root.Controls.Add(dataGrid, 0, 2);

            var btnSave = new Guna.UI.WinForms.GunaButton
            {
                Text = "Save Attendance",
                BaseColor = Navy,
                ForeColor = Color.White,
                Width = 160,
                Height = 40,
                Location = new Point(980, 15),
                Cursor = Cursors.Hand
            };
            btnSave.Click += (s, e) => SaveAttendance();
            pnlHeader.Controls.Add(btnSave);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private void ApplySearchFilter()
        {
            if (dataGrid.DataSource is DataTable dt)
            {
                string search = txtSearch.Text.Trim().Replace("'", "''");
                string colName = isReportMode ? "Name" : "Full Name";
                if (string.IsNullOrEmpty(search))
                    dt.DefaultView.RowFilter = "";
                else
                    dt.DefaultView.RowFilter = string.Format("[{0}] LIKE '%{1}%'", colName, search);
                
                if (!isReportMode) UpdateStats(dt);
            }
        }

        private void MarkAll(string status)
        {
            if (isReportMode) return;
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (dataGrid.Columns.Contains("StatusCol"))
                {
                    row.Cells["StatusCol"].Value = status;
                }
            }
        }

        private async System.Threading.Tasks.Task ToggleReportMode()
        {
            isReportMode = !isReportMode;
            txtSearch.Text = "";
            if (isReportMode)
            {
                Text = "Attendance Monthly Analysis";
                await LoadReportData();
            }
            else
            {
                Text = "Attendance Management";
                await LoadTargetList();
            }
        }

        private async System.Threading.Tasks.Task LoadReportData()
        {
            try
            {
                string type = comboType.Text;
                DateTime date = datePicker.Value.Date;
                
                lblStats.Text = "Analyzing...";
                DataTable dt = await _attendanceService.GetMonthlyAnalysisAsync(type, date.Month, date.Year);

                dataGrid.Columns.Clear();
                dataGrid.DataSource = dt;
                dataGrid.ReadOnly = true;
                lblStats.Text = $"Analysis for {date:MMMM yyyy}";
                ApplySearchFilter();
            }
            catch (Exception ex) { UIHelper.ShowError("Error loading report: " + ex.Message, "Attendance"); }
        }

        private void FlagLowAttendance()
        {
            if (!isReportMode || !dataGrid.Columns.Contains("Attendance %")) return;

            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (row.Cells["Attendance %"].Value != null && row.Cells["Attendance %"].Value != DBNull.Value)
                {
                    decimal pct = Convert.ToDecimal(row.Cells["Attendance %"].Value);
                    if (pct < 80)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(198, 40, 40);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task LoadClasses()
        {
            try
            {
                var classes = await _attendanceService.GetClassesAsync();
                foreach (var cls in classes)
                {
                    if (!comboClass.Items.Contains(cls))
                        comboClass.Items.Add(cls);
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadTargetList()
        {
            if (isReportMode) { await LoadReportData(); return; }
            
            try
            {
                string type = comboType.Text;
                string selectedClass = comboClass.Text;
                DateTime date = datePicker.Value.Date;

                lblStats.Text = "Loading list...";
                DataTable dt = await _attendanceService.GetAttendanceListAsync(type, selectedClass, date);

                dataGrid.Columns.Clear();
                dataGrid.DataSource = dt;

                // Add Status ComboBox Column
                var col = new DataGridViewComboBoxColumn
                {
                    Name = "StatusCol",
                    HeaderText = "Mark Status",
                    DataPropertyName = "Status",
                    DataSource = new string[] { "PRESENT", "ABSENT", "LATE", "" },
                    FlatStyle = FlatStyle.Flat
                };
                dataGrid.Columns.Add(col);
                if (dataGrid.Columns.Contains("Status")) dataGrid.Columns["Status"].Visible = false;

                dataGrid.ReadOnly = false;
                ApplySearchFilter();
                UpdateStats(dt);
            }
            catch (Exception ex) { UIHelper.ShowError("Error loading list: " + ex.Message, "Attendance"); }
        }

        private void UpdateStats(DataTable dt)
        {
            int total = dt.Rows.Count;
            int present = 0;
            foreach (DataRow row in dt.Rows) if (row["Status"].ToString() == "PRESENT") present++;
            lblStats.Text = $"Total: {total} | Present: {present} | Absent: {total - present}";
        }

        private async void SaveAttendance()
        {
            if (isReportMode) return;
            
            var records = new List<AttendanceRecord>();
            string type = comboType.Text;
            DateTime date = datePicker.Value.Date;

            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                string status = row.Cells["StatusCol"].Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(status)) continue;

                records.Add(new AttendanceRecord
                {
                    ReferenceID = row.Cells["ID"].Value.ToString(),
                    ReferenceType = type,
                    FullName = row.Cells["Full Name"].Value.ToString(),
                    Date = date,
                    Status = status,
                    Remarks = row.Cells["Remarks"].Value?.ToString() ?? ""
                });
            }

            if (records.Count == 0)
            {
                UIHelper.ShowWarning("No attendance marks to save.", "Attendance");
                return;
            }

            lblStats.Text = "Saving...";
            var (success, message) = await _attendanceService.SaveBatchAsync(records);

            if (success)
            {
                UIHelper.ShowSuccess(message, "Attendance");
                await LoadTargetList();
            }
            else
            {
                UIHelper.ShowError(message, "Attendance");
                lblStats.Text = "Save failed.";
            }
        }
    }
}
