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
        // ── Services ─────────────────────────────────────────────────────────
        private readonly AttendanceService _attendanceService;

        // ── UI controls (plain WinForms — no Guna dependency) ────────────────
        private DataGridView   dataGrid;
        private ComboBox       comboType;
        private ComboBox       comboClass;
        private TextBox        txtSearch;
        private DateTimePicker datePicker;
        private Label          lblStats;
        private Button         btnSave;
        private Button         btnAnalysis;

        private bool _isReportMode = false;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color PageBack    = Color.FromArgb(246, 248, 251);
        private static readonly Color Surface     = Color.White;
        private static readonly Color NavyHead    = Color.FromArgb(17, 35, 58);
        private static readonly Color Primary     = Color.FromArgb(31, 99, 198);
        private static readonly Color TextCol     = Color.FromArgb(25, 36, 49);
        private static readonly Color Muted       = Color.FromArgb(93, 108, 123);
        private static readonly Color Border      = Color.FromArgb(219, 226, 236);
        private static readonly Color Success     = Color.FromArgb(22, 163, 74);
        private static readonly Color Danger      = Color.FromArgb(190, 18, 60);
        private static readonly Color AmberBg     = Color.FromArgb(255, 251, 235);
        private static readonly Color AmberFg     = Color.FromArgb(146, 64, 14);

        // ── Win32 placeholder helper ──────────────────────────────────────────
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // ─────────────────────────────────────────────────────────────────────
        public frmAttendance()
        {
            InitializeComponent();
            if (!AuthService.RequireAccess("frmAttendance", this)) return;

            var repository = new AttendanceRepository(AppConfig.ConnectionString);
            _attendanceService = new AttendanceService(repository);

            Load += frmAttendance_Load;
        }

        private async void frmAttendance_Load(object sender, EventArgs e)
        {
            BuildModernLayout();
            NavigationSidebar.AddTo(this);

            await _attendanceService.InitializeAsync();
            await LoadClasses();
            await ApplyTeacherScopeAsync();
            await LoadTargetList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Layout builders
        // ─────────────────────────────────────────────────────────────────────

        private void BuildModernLayout()
        {
            SuspendLayout();
            Controls.Clear();

            Text            = $"Attendance — {Common.AppConfig.ProductName}";
            BackColor       = PageBack;
            Font            = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition   = FormStartPosition.CenterScreen;
            MinimumSize     = new Size(1100, 680);
            Size            = new Size(1260, 760);

            var root = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = PageBack,
                Padding     = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));   // filter bar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // grid

            root.Controls.Add(BuildHeader(),    0, 0);
            root.Controls.Add(BuildFilterBar(), 0, 1);
            root.Controls.Add(BuildGridShell(), 0, 2);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                BackColor   = PageBack
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Left: title block
            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBack };
            titleBlock.Controls.Add(new Label
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                Text      = "Attendance",
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 26,
                Text      = "Mark and monitor daily presence of students and staff",
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            // Right: action buttons
            var actions = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = PageBack,
                Padding       = new Padding(0, 14, 0, 0)
            };

            btnSave = MakePrimaryBtn("Save", NavyHead, 90);
            btnSave.Click += (s, e) => SaveAttendance();

            btnAnalysis = MakeSecondaryBtn("Analysis", 96);
            btnAnalysis.Click += async (s, e) => await ToggleReportMode();

            var btnMarkAll = MakePrimaryBtn("✓ All Present", Success, 110);
            btnMarkAll.Click += (s, e) => MarkAll("PRESENT");

            var btnClear = MakePrimaryBtn("✕ Clear", Danger, 80);
            btnClear.Click += (s, e) => MarkAll("");

            // Right-to-left so Save appears rightmost
            actions.Controls.Add(btnSave);
            actions.Controls.Add(btnAnalysis);
            actions.Controls.Add(btnMarkAll);
            actions.Controls.Add(btnClear);

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions,    1, 0);
            return header;
        }

        private Control BuildFilterBar()
        {
            var card = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Surface,
                Padding     = new Padding(16, 0, 16, 0),
                Margin      = new Padding(0, 0, 0, 10)
            };
            // Draw bottom border line
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawLine(pen, 0, card.Height - 1, card.Width, card.Height - 1);
            };

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 6,
                BackColor   = Surface
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // comboType
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // comboClass
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // datePicker
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100)); // search (stretch)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  8));  // gap
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); // lblStats
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            grid.Padding = new Padding(0, 24, 0, 0);

            comboType = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 10F),
                Margin        = new Padding(0, 0, 8, 0)
            };
            comboType.Items.AddRange(new object[] { "STUDENT", "STAFF" });
            comboType.SelectedIndex = 0;
            comboType.SelectedIndexChanged += async (s, e) =>
            {
                comboClass.Visible = (comboType.Text == "STUDENT");
                await LoadTargetList();
            };

            comboClass = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 10F),
                Margin        = new Padding(0, 0, 8, 0)
            };
            comboClass.Items.Add("All Classes");
            comboClass.SelectedIndex = 0;
            comboClass.SelectedIndexChanged += async (s, e) => await LoadTargetList();

            datePicker = new DateTimePicker
            {
                Dock   = DockStyle.Fill,
                Format = DateTimePickerFormat.Long,
                Value  = DateTime.Today,
                Font   = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 8, 0)
            };
            datePicker.ValueChanged += async (s, e) => await LoadTargetList();

            txtSearch = new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin      = new Padding(0, 0, 0, 0)
            };
            txtSearch.HandleCreated += (s, e) =>
                SendMessage(txtSearch.Handle, EM_SETCUEBANNER, 1, "Search by name…");
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            lblStats = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = "Loading…",
                Font      = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = NavyHead,
                TextAlign = ContentAlignment.MiddleRight,
                Margin    = new Padding(0, 0, 0, 0)
            };

            grid.Controls.Add(comboType,   0, 0);
            grid.Controls.Add(comboClass,  1, 0);
            grid.Controls.Add(datePicker,  2, 0);
            grid.Controls.Add(txtSearch,   3, 0);
            // column 4 = gap (empty)
            grid.Controls.Add(lblStats,    5, 0);

            card.Controls.Add(grid);
            return card;
        }

        private Control BuildGridShell()
        {
            var shell = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding     = new Padding(1)
            };

            dataGrid = new DataGridView
            {
                Dock                          = DockStyle.Fill,
                BackgroundColor               = Surface,
                BorderStyle                   = BorderStyle.None,
                CellBorderStyle               = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight           = 42,
                ReadOnly                      = false,
                AllowUserToAddRows            = false,
                AllowUserToDeleteRows         = false,
                RowHeadersVisible             = false,
                SelectionMode                 = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                   = false,
                EnableHeadersVisualStyles     = false,
                AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor                     = Border
            };

            Common.StudentId.AttachGridFormatting(dataGrid, "ID");

            // Header style
            dataGrid.ColumnHeadersDefaultCellStyle.BackColor  = NavyHead;
            dataGrid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            dataGrid.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            dataGrid.ColumnHeadersDefaultCellStyle.Padding    = new Padding(10, 0, 0, 0);
            dataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = NavyHead;

            // Row style
            dataGrid.DefaultCellStyle.BackColor          = Surface;
            dataGrid.DefaultCellStyle.ForeColor          = TextCol;
            dataGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dataGrid.DefaultCellStyle.SelectionForeColor = TextCol;
            dataGrid.DefaultCellStyle.Padding            = new Padding(6, 0, 0, 0);
            dataGrid.DefaultCellStyle.Font               = new Font("Segoe UI", 9.5F);
            dataGrid.RowTemplate.Height                  = 36;
            dataGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            dataGrid.DataBindingComplete += (s, e) => ColorizeStatusRows();

            shell.Controls.Add(dataGrid);
            return shell;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Button factories (plain WinForms, no Guna)
        // ─────────────────────────────────────────────────────────────────────

        private Button MakePrimaryBtn(string text, Color bgColor, int width = 130)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Width     = width,
                Height    = 36,
                Margin    = new Padding(0, 0, 8, 0),
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(bgColor, 0.08f);
            return btn;
        }

        private Button MakeSecondaryBtn(string text, int width = 110)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Surface,
                ForeColor = TextCol,
                Width     = width,
                Height    = 36,
                Margin    = new Padding(0, 0, 8, 0),
                Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 242, 255);
            return btn;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Business logic (unchanged from original)
        // ─────────────────────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task ApplyTeacherScopeAsync()
        {
            if (!AuthService.IsTeacher) return;
            string myClass = await AuthService.GetCurrentTeacherClassAsync();
            if (string.IsNullOrEmpty(myClass)) return;

            comboType.SelectedItem = "STUDENT";
            comboType.Enabled      = false;

            int idx = comboClass.Items.IndexOf(myClass);
            if (idx >= 0)
            {
                comboClass.SelectedIndex = idx;
                comboClass.Enabled       = false;
            }
        }

        private void ApplySearchFilter()
        {
            if (!(dataGrid.DataSource is DataTable dt)) return;

            string search  = txtSearch.Text.Trim().Replace("'", "''");
            string colName = _isReportMode ? "Name" : "Full Name";

            dt.DefaultView.RowFilter = string.IsNullOrEmpty(search)
                ? ""
                : $"[{colName}] LIKE '%{search}%'";

            if (!_isReportMode) UpdateStats(dt);
        }

        private void MarkAll(string status)
        {
            if (_isReportMode) return;
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (dataGrid.Columns.Contains("StatusCol"))
                    row.Cells["StatusCol"].Value = status;
            }
        }

        private async System.Threading.Tasks.Task ToggleReportMode()
        {
            _isReportMode  = !_isReportMode;
            txtSearch.Text = "";

            // Swap button label and style
            if (_isReportMode)
            {
                btnAnalysis.Text      = "← Back";
                btnAnalysis.BackColor = Color.FromArgb(101, 75, 14);
                btnAnalysis.ForeColor = Color.White;
                btnAnalysis.FlatAppearance.BorderSize = 0;
                Text = "Attendance — Monthly Analysis";
                await LoadReportData();
            }
            else
            {
                btnAnalysis.Text      = "Analysis";
                btnAnalysis.BackColor = Surface;
                btnAnalysis.ForeColor = TextCol;
                btnAnalysis.FlatAppearance.BorderSize = 1;
                Text = $"Attendance — {Common.AppConfig.ProductName}";
                await LoadTargetList();
            }
        }

        private async System.Threading.Tasks.Task LoadReportData()
        {
            try
            {
                string   type = comboType.Text;
                DateTime date = datePicker.Value.Date;

                lblStats.Text = "Analyzing…";
                DataTable dt  = await _attendanceService.GetMonthlyAnalysisAsync(type, date.Month, date.Year);

                dataGrid.Columns.Clear();
                dataGrid.DataSource = dt;
                dataGrid.ReadOnly   = true;
                lblStats.Text       = $"Analysis · {date:MMMM yyyy}";
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading report: " + ex.Message, "Attendance");
            }
        }

        /// <summary>
        /// Highlights rows where attendance % is below 80 in a warm amber tone.
        /// Also colorises the Mark Status column cells by value.
        /// </summary>
        private void ColorizeStatusRows()
        {
            if (_isReportMode)
            {
                // Low-attendance highlighting
                if (!dataGrid.Columns.Contains("Attendance %")) return;
                foreach (DataGridViewRow row in dataGrid.Rows)
                {
                    var v = row.Cells["Attendance %"].Value;
                    if (v == null || v == DBNull.Value) continue;
                    if (Convert.ToDecimal(v) < 80)
                    {
                        row.DefaultCellStyle.BackColor = AmberBg;
                        row.DefaultCellStyle.ForeColor = AmberFg;
                    }
                }
            }
            else
            {
                // Colour individual Status cells
                if (!dataGrid.Columns.Contains("StatusCol")) return;
                foreach (DataGridViewRow row in dataGrid.Rows)
                {
                    ColorizeStatusCell(row);
                }
            }
        }

        private void ColorizeStatusCell(DataGridViewRow row)
        {
            if (!dataGrid.Columns.Contains("StatusCol")) return;
            string status = row.Cells["StatusCol"].Value?.ToString() ?? "";
            switch (status)
            {
                case "PRESENT":
                    row.Cells["StatusCol"].Style.BackColor = Color.FromArgb(220, 252, 231);
                    row.Cells["StatusCol"].Style.ForeColor = Color.FromArgb(21, 128, 61);
                    break;
                case "ABSENT":
                    row.Cells["StatusCol"].Style.BackColor = Color.FromArgb(254, 226, 226);
                    row.Cells["StatusCol"].Style.ForeColor = Color.FromArgb(153, 27, 27);
                    break;
                case "LATE":
                    row.Cells["StatusCol"].Style.BackColor = AmberBg;
                    row.Cells["StatusCol"].Style.ForeColor = AmberFg;
                    break;
                default:
                    row.Cells["StatusCol"].Style.BackColor = Color.Empty;
                    row.Cells["StatusCol"].Style.ForeColor = Color.Empty;
                    break;
            }
        }

        private async System.Threading.Tasks.Task LoadClasses()
        {
            try
            {
                var classes = await _attendanceService.GetClassesAsync();
                foreach (var cls in classes)
                    if (!comboClass.Items.Contains(cls))
                        comboClass.Items.Add(cls);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to load classes in frmAttendance", ex);
            }
        }

        private async System.Threading.Tasks.Task LoadTargetList()
        {
            if (_isReportMode) { await LoadReportData(); return; }

            try
            {
                string   type     = comboType.Text;
                string   selClass = comboClass.Text;
                DateTime date     = datePicker.Value.Date;

                lblStats.Text = "Loading…";
                DataTable dt  = await _attendanceService.GetAttendanceListAsync(type, selClass, date);

                dataGrid.Columns.Clear();
                dataGrid.DataSource = dt;

                // Append Status ComboBox column
                var col = new DataGridViewComboBoxColumn
                {
                    Name             = "StatusCol",
                    HeaderText       = "Mark Status",
                    DataPropertyName = "Status",
                    DataSource       = new string[] { "PRESENT", "ABSENT", "LATE", "" },
                    FlatStyle        = FlatStyle.Flat,
                    FillWeight       = 80
                };
                dataGrid.Columns.Add(col);
                if (dataGrid.Columns.Contains("Status"))
                    dataGrid.Columns["Status"].Visible = false;

                // Tweak column widths
                dataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                void W(string name, int w) { if (dataGrid.Columns.Contains(name)) dataGrid.Columns[name].FillWeight = w; }
                W("ID",          50);
                W("Full Name",  140);
                W("Class",       70);
                W("Remarks",    100);
                W("StatusCol",   80);

                dataGrid.ReadOnly = false;
                ApplySearchFilter();
                UpdateStats(dt);

                // Recolour on cell value change so colours update instantly
                dataGrid.CellValueChanged -= DataGrid_CellValueChanged;
                dataGrid.CellValueChanged += DataGrid_CellValueChanged;
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading list: " + ex.Message, "Attendance");
            }
        }

        private void DataGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ColorizeStatusCell(dataGrid.Rows[e.RowIndex]);

            if (dataGrid.DataSource is DataTable dt)
                UpdateStats(dt);
        }

        private void UpdateStats(DataTable dt)
        {
            int total   = dt.Rows.Count;
            int present = 0;
            foreach (DataRow row in dt.Rows)
                if (row["Status"]?.ToString() == "PRESENT") present++;

            int absent = total - present;
            lblStats.Text = $"Total: {total}   ·   Present: {present}   ·   Absent: {absent}";
        }

        private async void SaveAttendance()
        {
            try
            {
                if (_isReportMode) return;

                var records  = new List<AttendanceRecord>();
                string type  = comboType.Text;
                DateTime date = datePicker.Value.Date;

                foreach (DataGridViewRow row in dataGrid.Rows)
                {
                    string status = row.Cells["StatusCol"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(status)) continue;

                    records.Add(new AttendanceRecord
                    {
                        ReferenceID   = row.Cells["ID"].Value.ToString(),
                        ReferenceType = type,
                        FullName      = row.Cells["Full Name"].Value.ToString(),
                        Date          = date,
                        Status        = status,
                        Remarks       = row.Cells["Remarks"].Value?.ToString() ?? ""
                    });
                }

                if (records.Count == 0)
                {
                    ConfirmationHelper.ShowWarning("No attendance marks to save.", "Attendance");
                    return;
                }

                if (!ConfirmationHelper.ConfirmBulkOperation("save attendance for", records.Count)) return;

                lblStats.Text = "Saving…";
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
            catch (Exception ex)
            {
                LoggerHelper.LogError("SaveAttendance failed", ex);
                lblStats.Text = "Save error.";
                UIHelper.ShowError("Save attendance failed: " + ex.Message, "Attendance");
            }
        }
    }
}
