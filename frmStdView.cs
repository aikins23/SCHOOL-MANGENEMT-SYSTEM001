using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmStdView : Form
    {
        private readonly StudentService _studentService;
        private TextBox searchBox;
        private ComboBox classFilter;
        private DataGridView studentsGrid;
        private Label resultLabel;

        private static readonly Color PageBackColor = Color.White;
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color SidebarBackColor = Color.FromArgb(17, 35, 58);
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        public frmStdView()
        {
            InitializeComponent();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmStdView", this)) return;

            // Initialize modern architecture
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            var feeRepo = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, feeRepo);

            BuildModernStudentView();
            NavigationSidebar.AddTo(this);

            // Wire events commented-out in designer
            cmb_cd.SelectedIndexChanged             += cmb_cd_SelectedIndexChanged;
            gunaPictureBox6.Click                   += gunaPictureBox6_Click;
            gunaButton1.Click                       += gunaButton1_Click_1;
            studentsToolStripMenuItem.Click         += studentsToolStripMenuItem_Click;
            employersToolStripMenuItem.Click        += employersToolStripMenuItem_Click;
            adminstrationToolStripMenuItem.Click    += adminstrationToolStripMenuItem_Click;
            adminstratorsToolStripMenuItem.Click    += adminstratorsToolStripMenuItem_Click;
            makePaymentToolStripMenuItem.Click      += makePaymentToolStripMenuItem_Click;
            aboutToolStripMenuItem.Click            += aboutToolStripMenuItem_Click;

            // The designer never wired the Load event — without this the grid stays empty.
            this.Load += frmStdView_Load;
        }

        // ── Win32 placeholder helper (TextBox.PlaceholderText is not on .NET Framework) ──
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private void BuildModernStudentView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Students";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1120, 680);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFilterBar(), 0, 1);
            root.Controls.Add(BuildGridShell(), 0, 2);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
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
                Text = "Students",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Search, filter, and open student records",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 16, 0)
            };

            actions.Controls.Add(CreatePrimaryButton("Add Student", () => new frmAddStd().Show()));
            actions.Controls.Add(CreateSecondaryButton("Import CSV", async () => await ImportCsvAsync()));
            actions.Controls.Add(CreateSecondaryButton("Export CSV", async () => await ExportCsvAsync()));
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () =>
            {
                Close();
                Common.FormManager.GoToDashboard();
            }));

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private async System.Threading.Tasks.Task ExportCsvAsync()
        {
            using (var sfd = new SaveFileDialog { Filter = "CSV File|*.csv", Title = "Export Students", FileName = "Students_Export.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var repo = new kingdom_Preparatory_School_Management_System.Data.StudentRepository(Common.AppConfig.ConnectionString);
                    var feeRepo = new kingdom_Preparatory_School_Management_System.Data.FeeRepository(Common.AppConfig.ConnectionString);
                    var svc = new kingdom_Preparatory_School_Management_System.Services.StudentService(repo, feeRepo);
                    var csvSvc = new kingdom_Preparatory_School_Management_System.Services.CsvImportExportService(svc, repo);

                    UseWaitCursor = true;
                    var result = await System.Threading.Tasks.Task.Run(
                        () => csvSvc.ExportStudentsToCsvAsync(sfd.FileName));
                    UseWaitCursor = false;
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task ImportCsvAsync()
        {
            using (var ofd = new OpenFileDialog { Filter = "CSV File|*.csv", Title = "Import Students" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var repo = new kingdom_Preparatory_School_Management_System.Data.StudentRepository(Common.AppConfig.ConnectionString);
                    var feeRepo = new kingdom_Preparatory_School_Management_System.Data.FeeRepository(Common.AppConfig.ConnectionString);
                    var svc = new kingdom_Preparatory_School_Management_System.Services.StudentService(repo, feeRepo);
                    var csvSvc = new kingdom_Preparatory_School_Management_System.Services.CsvImportExportService(svc, repo);

                    int dataRows = System.IO.File.ReadAllLines(ofd.FileName)
                        .Skip(1).Count(l => !string.IsNullOrWhiteSpace(l));
                    if (dataRows == 0)
                    { MessageBox.Show("The file contains no data rows.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (MessageBox.Show(
                            $"{dataRows} data row(s) found. Import now?\n\nNew students get opening fee records and their guardians receive credential SMS.",
                            "Confirm Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                    UseWaitCursor = true;
                    var result = await System.Threading.Tasks.Task.Run(
                        () => csvSvc.ImportStudentsFromCsvAsync(ofd.FileName));
                    UseWaitCursor = false;

                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadStudents(); // Refresh grid
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private Control BuildFilterBar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Padding = new Padding(18),
                BorderStyle = BorderStyle.FixedSingle
            };

            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                BackColor = SurfaceColor
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.TextChanged += (sender, args) => ApplyFilters();
            searchBox.HandleCreated += (s, e) =>
                SendMessage(searchBox.Handle, EM_SETCUEBANNER, 1, "Search by ID, first name, or last name…");

            classFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            classFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();

            filters.Controls.Add(searchBox, 0, 0);
            filters.Controls.Add(classFilter, 1, 0);
            filters.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadStudents()), 2, 0);
            filters.Controls.Add(CreateSecondaryButton("Clear", ClearFilters), 3, 0);

            resultLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9.5F)
            };
            filters.Controls.Add(resultLabel, 4, 0);

            panel.Controls.Add(filters);
            return panel;
        }

        private Control BuildGridShell()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(1)
            };

            studentsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Both
            };
            UiTheme.StyleDataGrid(studentsGrid);
            Common.StudentId.AttachGridFormatting(studentsGrid, "STUDENT ID");
            studentsGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            studentsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            studentsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            studentsGrid.DefaultCellStyle.BackColor = SurfaceColor;
            studentsGrid.DefaultCellStyle.ForeColor = TextColor;
            studentsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            studentsGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            studentsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            studentsGrid.GridColor = BorderColor;
            studentsGrid.CellDoubleClick += StudentsGrid_CellDoubleClick;
            
            // Suppress the default error dialog for invalid/empty images in the grid
            studentsGrid.DataError += (s, e) =>
            {
                if (e.Exception is ArgumentException || e.Exception is FormatException)
                {
                    e.ThrowException = false;
                }
            };

            shell.Controls.Add(studentsGrid);
            return shell;
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(23, 82, 172);
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 242, 255);
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button
            {
                AutoSize = true,
                MinimumSize = new Size(110, 36),
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(8, 0, 8, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private void LoadClasses()
        {
            classFilter.Items.Clear();
            classFilter.Items.Add("All classes");
            classFilter.Items.AddRange(AppConfig.ClassNames);
            classFilter.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task LoadStudents()
        {
            try
            {
                resultLabel.Text = "Loading students…";
                DataTable table = await _studentService.GetStudentsTableAsync(null, null);

                studentsGrid.DataSource = table;
                ConfigureGridColumns();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("An error occurred while loading students: " + ex.Message, "Students");
                resultLabel.Text = "Load failed";
            }
        }

        private void ConfigureGridColumns()
        {
            if (studentsGrid.Columns.Count == 0) return;

            // Hide only the internal columns (raw numeric ID kept for lookups, and the binary
            // photo). Contact/guardian fields stay visible so imported data is verifiable.
            string[] toHide = { "ID", "STUDENT PIC" };
            foreach (var name in toHide)
            {
                if (studentsGrid.Columns.Contains(name))
                    studentsGrid.Columns[name].Visible = false;
            }

            // Move STUDENT ID to the front so it's the first visible column
            if (studentsGrid.Columns.Contains("STUDENT ID"))
                studentsGrid.Columns["STUDENT ID"].DisplayIndex = 0;

            // Use None + explicit widths so columns can exceed the viewport width,
            // which enables the horizontal scrollbar when there are many columns.
            studentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            void ColWidth(string col, int w)
            {
                if (studentsGrid.Columns.Contains(col))
                {
                    studentsGrid.Columns[col].Width = w;
                    studentsGrid.Columns[col].MinimumWidth = w;
                }
            }
            ColWidth("STUDENT ID", 110);
            ColWidth("FIRST NAME", 160);
            ColWidth("LAST NAME", 160);
            ColWidth("CLASS ID", 100);
            ColWidth("GENDER", 80);
            ColWidth("DATE OF BIRTH", 120);
            ColWidth("ADMISSION DATE", 120);
            ColWidth("EMAIL", 200);
            ColWidth("HOME TOWN", 140);
            ColWidth("RESIDENCE", 160);
            ColWidth("GUARDIAN NAME", 200);
        }

        private void ApplyFilters()
        {
            if (studentsGrid == null || classFilter == null || searchBox == null) return;
            if (!(studentsGrid.DataSource is DataTable table)) return;

            var clauses = new System.Collections.Generic.List<string>();

            string search = searchBox.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(search))
            {
                string esc = search.Replace("'", "''");
                // Match: numeric ID, prefixed Student ID (KPS####), or partial name
                if (int.TryParse(search, out _))
                    clauses.Add($"([ID] = {esc} OR [STUDENT ID] LIKE '%{esc}%' OR [FIRST NAME] LIKE '%{esc}%' OR [LAST NAME] LIKE '%{esc}%')");
                else
                    clauses.Add($"([STUDENT ID] LIKE '%{esc}%' OR [FIRST NAME] LIKE '%{esc}%' OR [LAST NAME] LIKE '%{esc}%')");
            }

            if (classFilter.SelectedIndex > 0)
            {
                string cls = classFilter.Text.Replace("'", "''");
                clauses.Add($"[CLASS ID] = '{cls}'");
            }

            try
            {
                table.DefaultView.RowFilter = string.Join(" AND ", clauses);
            }
            catch
            {
                table.DefaultView.RowFilter = string.Empty;
            }

            int count = table.DefaultView.Count;
            resultLabel.Text = count == 0
                ? "No students match the current filters"
                : $"{count} student record(s)";
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            if (classFilter.Items.Count > 0)
            {
                classFilter.SelectedIndex = 0;
            }
            ApplyFilters();
        }

        private async void OpenSelectedStudent()
        {
            if (studentsGrid.CurrentRow == null || studentsGrid.CurrentRow.Cells["ID"].Value == null)
            {
                return;
            }

            string id = studentsGrid.CurrentRow.Cells["ID"].Value.ToString();
            
            // To maintain compatibility with frmStdDetails which expects a DataTable
            DataTable studentTable = await _studentService.GetStudentsTableAsync(id);

            using (frmStdDetails detailViewForm = new frmStdDetails(studentTable))
            {
                detailViewForm.ShowDialog();
            }
        }

        private void StudentsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                OpenSelectedStudent();
            }
        }

        private async void frmStdView_Load(object sender, EventArgs e)
        {
            LoadClasses();
            await ApplyTeacherScopeAsync();
            await LoadStudents();
        }

        /// <summary>
        /// If the current user is a Teacher, lock the class filter to their assigned
        /// class so they only see their own pupils. No-op for other roles.
        /// </summary>
        private async System.Threading.Tasks.Task ApplyTeacherScopeAsync()
        {
            if (!AuthService.IsTeacher) return;
            string myClass = await AuthService.GetCurrentTeacherClassAsync();
            if (string.IsNullOrEmpty(myClass)) return;
            int idx = classFilter.Items.IndexOf(myClass);
            if (idx >= 0)
            {
                classFilter.SelectedIndex = idx;
                classFilter.Enabled = false;
            }
        }

        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            OpenSelectedStudent();
        }

        private void gunaPictureBox6_Click(object sender, EventArgs e)
        {
            Close();
            Common.FormManager.GoToDashboard();
        }

        private void gunaPictureBox5_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void gunaPictureBox4_Click(object sender, EventArgs e)
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void cmb_cd_SelectedIndexChanged(object sender, EventArgs e) { ApplyFilters(); }
        private void txtID_TextChanged(object sender, EventArgs e) { ApplyFilters(); }
        private void gunaButton1_Click(object sender, EventArgs e) { }
        private void gunaButton1_Click_1(object sender, EventArgs e) { new frmAddStd().Show(); }
        private async void gunaButton2_Click(object sender, EventArgs e) { await LoadStudents(); }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { new frmAddStd().Show(); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { new frmEmployee().Show(); }
        private void adminstrationToolStripMenuItem_Click(object sender, EventArgs e) { new EXAMS().Show(); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { new frmStdView().Show(); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { new frmEmpView().Show(); }
        private void adminstratorsToolStripMenuItem_Click(object sender, EventArgs e) { new EXAMSVIEW().Show(); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { new frmFessPayment().Show(); }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) { new frmAbout().Show(); }
    }
}
