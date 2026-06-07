using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmOutstandingFees : Form
    {
        // ── Services ─────────────────────────────────────────────────────────
        private readonly IFeeRepository  _feeRepository;
        private readonly StudentService  _studentService;

        // ── UI controls (plain WinForms — no Guna dependency) ────────────────
        private DataGridView feesGrid;
        private ComboBox     classFilter;
        private TextBox      searchBox;
        private Label        lblCount;
        private Label        lblTotal;
        private DataTable    feesTable;

        // ── Palette (matches frmStdView / frmAttendance) ─────────────────────
        private static readonly Color PageBack  = Color.FromArgb(246, 248, 251);
        private static readonly Color Surface   = Color.White;
        private static readonly Color NavyHead  = Color.FromArgb(17, 35, 58);
        private static readonly Color Primary   = Color.FromArgb(31, 99, 198);
        private static readonly Color TextCol   = Color.FromArgb(25, 36, 49);
        private static readonly Color Muted     = Color.FromArgb(93, 108, 123);
        private static readonly Color Border    = Color.FromArgb(219, 226, 236);
        private static readonly Color DangerRed = Color.FromArgb(190, 18, 60);
        private static readonly Color AmberBg   = Color.FromArgb(255, 251, 235);
        private static readonly Color AmberFg   = Color.FromArgb(146, 64, 14);

        // ── Win32 placeholder helper ──────────────────────────────────────────
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // ─────────────────────────────────────────────────────────────────────
        public frmOutstandingFees()
        {
            InitializeComponent();
            if (!AuthService.RequireAccess("frmOutstandingFees", this)) return;

            _feeRepository = new FeeRepository(AppConfig.ConnectionString);
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, new FeeRepository(AppConfig.ConnectionString));

            BuildModernLayout();
            NavigationSidebar.AddTo(this);
        }

        private async void frmOutstandingFees_Load(object sender, EventArgs e)
        {
            await LoadClasses();
            await ApplyTeacherScopeAsync();
            await LoadOutstandingData();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Layout
        // ─────────────────────────────────────────────────────────────────────

        private void BuildModernLayout()
        {
            SuspendLayout();
            Controls.Clear();

            Text          = $"Outstanding Fees — {Common.AppConfig.ProductName}";
            BackColor     = PageBack;
            Font          = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize   = new Size(1100, 680);
            Size          = new Size(1260, 760);

            var root = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 4,
                ColumnCount = 1,
                BackColor   = PageBack,
                Padding     = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // summary bar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));   // filter bar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // grid

            root.Controls.Add(BuildHeader(),     0, 0);
            root.Controls.Add(BuildSummaryBar(), 0, 1);
            root.Controls.Add(BuildFilterBar(),  0, 2);
            root.Controls.Add(BuildGridShell(),  0, 3);

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
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            // Left: title block
            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBack };
            titleBlock.Controls.Add(new Label
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                Text      = "Outstanding Fees",
                ForeColor = TextCol,
                Font      = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 26,
                Text      = "Monitor unpaid balances and export defaulters list",
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

            var btnExport = MakePrimaryBtn("Export CSV", DangerRed, 108);
            btnExport.Click += (s, e) => ExportDefaulters();

            var btnRefresh = MakeSecondaryBtn("Refresh", 90);
            btnRefresh.Click += async (s, e) => await LoadOutstandingData();

            actions.Controls.Add(btnExport);
            actions.Controls.Add(btnRefresh);

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions,    1, 0);
            return header;
        }

        private Control BuildSummaryBar()
        {
            var bar = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                BackColor   = PageBack,
                Padding     = new Padding(0, 0, 0, 10)
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            bar.Controls.Add(BuildStatCard("Defaulters", ref lblCount, "Students with an outstanding balance", DangerRed), 0, 0);
            bar.Controls.Add(BuildStatCard("Total Outstanding", ref lblTotal, "Sum of all unpaid balances (GHS)", NavyHead), 1, 0);

            return bar;
        }

        private Control BuildStatCard(string title, ref Label valueLabel, string caption, Color accentColor)
        {
            var card = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Surface,
                BorderStyle = BorderStyle.FixedSingle,
                Padding     = new Padding(16),
                Margin      = new Padding(0, 0, 12, 0)
            };

            card.Controls.Add(new Label
            {
                Dock      = DockStyle.Top,
                Height    = 20,
                Text      = title,
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var lbl = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 34,
                Text      = "—",
                ForeColor = accentColor,
                Font      = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lbl);
            valueLabel = lbl;

            card.Controls.Add(new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 18,
                Text      = caption,
                ForeColor = Muted,
                Font      = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.BottomLeft
            });

            return card;
        }

        private Control BuildFilterBar()
        {
            var card = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Surface,
                Padding     = new Padding(16, 0, 16, 0),
                Margin      = new Padding(0, 0, 0, 8)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawLine(pen, 0, card.Height - 1, card.Width, card.Height - 1);
            };

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                BackColor   = Surface
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // classFilter
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100)); // searchBox (stretch)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,   8)); // gap
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  96)); // clear btn
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            grid.Padding = new Padding(0, 20, 0, 0);

            classFilter = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 10F),
                Margin        = new Padding(0, 0, 8, 0)
            };
            classFilter.Items.Add("All Classes");
            classFilter.SelectedIndex = 0;
            classFilter.SelectedIndexChanged += (s, e) => ApplyFilters();

            searchBox = new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Margin      = new Padding(0, 0, 0, 0)
            };
            searchBox.HandleCreated += (s, e) =>
                SendMessage(searchBox.Handle, EM_SETCUEBANNER, 1, "Search by student name or ID…");
            searchBox.TextChanged += (s, e) => ApplyFilters();

            var btnClear = MakeSecondaryBtn("Clear", 88);
            btnClear.Click += (s, e) => { searchBox.Text = ""; classFilter.SelectedIndex = 0; };

            grid.Controls.Add(classFilter, 0, 0);
            grid.Controls.Add(searchBox,   1, 0);
            // col 2 = gap
            grid.Controls.Add(btnClear,    3, 0);

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

            feesGrid = new DataGridView
            {
                Dock                      = DockStyle.Fill,
                BackgroundColor           = Surface,
                BorderStyle               = BorderStyle.None,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight       = 42,
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect               = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor                 = Border
            };

            Common.StudentId.AttachGridFormatting(feesGrid, "ID");
            feesGrid.ColumnHeadersDefaultCellStyle.BackColor         = NavyHead;
            feesGrid.ColumnHeadersDefaultCellStyle.ForeColor         = Color.White;
            feesGrid.ColumnHeadersDefaultCellStyle.Font              = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            feesGrid.ColumnHeadersDefaultCellStyle.Padding           = new Padding(10, 0, 0, 0);
            feesGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = NavyHead;

            feesGrid.DefaultCellStyle.BackColor          = Surface;
            feesGrid.DefaultCellStyle.ForeColor          = TextCol;
            feesGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            feesGrid.DefaultCellStyle.SelectionForeColor = TextCol;
            feesGrid.DefaultCellStyle.Padding            = new Padding(6, 0, 0, 0);
            feesGrid.DefaultCellStyle.Font               = new Font("Segoe UI", 9.5F);
            feesGrid.RowTemplate.Height                  = 36;
            feesGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            feesGrid.DataBindingComplete += (s, e) => HighlightHighBalances();

            shell.Controls.Add(feesGrid);
            return shell;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Button factories
        // ─────────────────────────────────────────────────────────────────────

        private Button MakePrimaryBtn(string text, Color bgColor, int width = 120)
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

        private Button MakeSecondaryBtn(string text, int width = 100)
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
        // Business logic
        // ─────────────────────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task ApplyTeacherScopeAsync()
        {
            if (!AuthService.IsTeacher) return;
            string myClass = await AuthService.GetCurrentTeacherClassAsync();
            if (string.IsNullOrEmpty(myClass)) return;
            int idx = classFilter.Items.IndexOf(myClass);
            if (idx >= 0)
            {
                classFilter.SelectedIndex = idx;
                classFilter.Enabled       = false;
            }
        }

        private async System.Threading.Tasks.Task LoadClasses()
        {
            try
            {
                var students     = await _studentService.GetAllStudentsAsync();
                var uniqueClasses = students
                    .Select(s => s.ClassID)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c);

                foreach (var cls in uniqueClasses)
                    if (!classFilter.Items.Contains(cls))
                        classFilter.Items.Add(cls);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Failed to load classes for filtering in frmOutstandingFees", ex);
            }
        }

        private async System.Threading.Tasks.Task LoadOutstandingData()
        {
            try
            {
                if (lblCount  != null) lblCount.Text  = "…";
                if (lblTotal  != null) lblTotal.Text  = "…";

                feesTable = await _feeRepository.GetOutstandingBalancesTableAsync();
                feesGrid.DataSource = feesTable;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading fee data: " + ex.Message, "Outstanding Fees");
            }
        }

        private void ApplyFilters()
        {
            if (feesTable == null) return;

            var filters = new List<string>();

            string search = searchBox?.Text.Trim().Replace("'", "''") ?? "";
            if (!string.IsNullOrEmpty(search))
                filters.Add($"([Student Name] LIKE '%{search}%' OR Convert(ID, 'System.String') LIKE '%{search}%')");

            if (classFilter?.SelectedIndex > 0)
                filters.Add($"Class = '{classFilter.Text.Replace("'", "''")}'");

            feesTable.DefaultView.RowFilter = string.Join(" AND ", filters);
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (feesTable == null) return;

            decimal total = 0;
            int count = feesTable.DefaultView.Count;
            foreach (DataRowView row in feesTable.DefaultView)
                total += Convert.ToDecimal(row["Balance Owed"]);

            if (lblCount != null) lblCount.Text = count.ToString("N0");
            if (lblTotal != null) lblTotal.Text = $"GHS {total:N2}";
        }

        /// <summary>Highlights rows where balance owed is above GHS 1,000 in amber.</summary>
        private void HighlightHighBalances()
        {
            if (!feesGrid.Columns.Contains("Balance Owed")) return;
            foreach (DataGridViewRow row in feesGrid.Rows)
            {
                var v = row.Cells["Balance Owed"].Value;
                if (v == null || v == DBNull.Value) continue;
                if (Convert.ToDecimal(v) >= 1000m)
                {
                    row.DefaultCellStyle.BackColor = AmberBg;
                    row.DefaultCellStyle.ForeColor = AmberFg;
                }
            }
        }

        private void ExportDefaulters()
        {
            try
            {
                if (feesTable == null || feesTable.DefaultView.Count == 0)
                {
                    ConfirmationHelper.ShowInfo("No data to export.", "Outstanding Fees");
                    return;
                }

                int count = feesTable.DefaultView.Count;
                if (!ConfirmationHelper.ConfirmSave($"Export defaulters list ({count} records) to Desktop?")) return;

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path    = Path.Combine(desktop, $"Defaulters_List_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(path))
                {
                    sw.WriteLine("ID,Student Name,Class,Balance Owed,Last Payment");
                    foreach (DataRowView row in feesTable.DefaultView)
                        sw.WriteLine($"{Common.StudentId.Display(row["ID"])},{row["Student Name"]},{row["Class"]},{row["Balance Owed"]},{row["Last Payment"]}");
                }

                UIHelper.ShowSuccess($"Defaulters list saved to Desktop:\n{Path.GetFileName(path)}", "Export Success");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("ExportDefaulters failed", ex);
                UIHelper.ShowError("Export failed: " + ex.Message, "Outstanding Fees");
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1164, 681);
            Name       = "frmOutstandingFees";
            Load      += new EventHandler(frmOutstandingFees_Load);
            ResumeLayout(false);
        }
    }
}
