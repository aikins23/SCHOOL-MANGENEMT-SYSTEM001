using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmTimetable : Form
    {
        private readonly TimetableRepository _repo;
        private readonly TimetableGeneratorService _generator;
        private readonly ClassRepository _classRepo;
        private readonly EmployeeRepository _empRepo;
        private readonly SubjectRepository _subjectRepo;

        private Guna2DataGridView _periodGrid, _configGrid, _viewerGrid;
        private Guna2ComboBox _cmbDepartmentSelect, _cmbViewDepartmentSelect, _cmbClassSelect, _cmbViewClass, _cmbSubject, _cmbTeacher, _cmbTeacherMode;
        private Guna2NumericUpDown _periodsPerWeek;
        private Guna2Button _btnAddRequirement, _btnDeleteRequirement, _btnSavePeriodEdits;
        private TabControl _tabs;
        private Label _statusLabel, _periodSummaryLabel, _allocationSummaryLabel, _viewerSummaryLabel;
        private List<Employee> _teachers = new List<Employee>();
        private List<string> _allClassNames = new List<string>();
        private List<TimePeriod> _printPeriods = new List<TimePeriod>();
        private List<TimetableEntry> _printEntries = new List<TimetableEntry>();
        private List<string> _departmentPrintClasses;
        private Dictionary<string, List<TimetableEntry>> _departmentPrintEntries;
        private int _departmentPrintIndex;
        private string _printClassName = "";
        private bool _isInitializing;
        private bool _isBusy;
        private const string SubjectBasedMode = "Subject-based teachers";
        private const string OneTeacherMode = "Class teacher for all subjects";

        public frmTimetable()
        {
            InitializeComponent();
            this.Icon = Branding.AppIcon;
            _repo = new TimetableRepository(AppConfig.ConnectionString);
            _generator = new TimetableGeneratorService(_repo);
            _classRepo = new ClassRepository(AppConfig.ConnectionString);
            _empRepo = new EmployeeRepository(AppConfig.ConnectionString);
            _subjectRepo = new SubjectRepository(AppConfig.ConnectionString);
            if (!AuthService.RequireAccess("frmTimetable", this)) return;

            BuildUi();
            SubjectCatalog.Changed += OnSubjectCatalogChanged;
            FormClosed += (s, e) => SubjectCatalog.Changed -= OnSubjectCatalogChanged;
            Load += async (s, e) => await InitializeFormAsync();
        }

        private void BuildUi()
        {
            Text = "Automated Timetable System";
            Size = new Size(1240, 820);
            MinimumSize = new Size(1060, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.5F);
            Padding = new Padding(18, 18, 18, 24);

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            Controls.Add(shell);

            var header = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };
            header.Controls.Add(new Label
            {
                Text = "Timetable Hub",
                ForeColor = UiTheme.Text,
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                Location = new Point(0, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            header.Controls.Add(new Label
            {
                Text = "Configure teaching workload, generate clash-aware timetables, and review the weekly class grid.",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(2, 48),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            shell.Controls.Add(header, 0, 0);

            var workspace = CreateCardPanel();
            workspace.Padding = new Padding(0);
            shell.Controls.Add(workspace, 0, 1);

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(220, 42),
                SizeMode = TabSizeMode.Fixed,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Padding = new Point(14, 6)
            };
            _tabs.DrawItem += DrawTimetableTab;
            _tabs.TabPages.Add(CreatePeriodSetupTab());
            _tabs.TabPages.Add(CreateConfigTab());
            _tabs.TabPages.Add(CreateViewerTab());
            workspace.Controls.Add(_tabs);

            _statusLabel = new Label
            {
                Text = "Ready.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.Muted,
                BackColor = UiTheme.Page,
                Padding = new Padding(2, 10, 0, 0)
            };
            shell.Controls.Add(_statusLabel, 0, 2);
        }

        private TabPage CreatePeriodSetupTab()
        {
            var page = new TabPage("Period Setup") { BackColor = Color.White, Padding = new Padding(24, 18, 24, 22) };
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            main.Controls.Add(CreateTabHeader("School Periods", "Set the school's actual teaching periods, break times, and print order."), 0, 0);

            _periodGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _periodGrid.Columns.Add("PeriodName", "Period");
            _periodGrid.Columns.Add("StartTime", "Start Time");
            _periodGrid.Columns.Add("EndTime", "End Time");
            _periodGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsBreak", HeaderText = "Break" });
            _periodGrid.Columns.Add("SortOrder", "Order");
            _periodGrid.Columns["StartTime"].DefaultCellStyle.NullValue = "08:00";
            _periodGrid.Columns["EndTime"].DefaultCellStyle.NullValue = "08:40";
            _periodGrid.Columns["IsBreak"].Width = 90;
            _periodGrid.Columns["SortOrder"].Width = 90;
            StyleTimetableGrid(_periodGrid);
            main.Controls.Add(_periodGrid, 0, 1);

            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 0)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            _periodSummaryLabel = new Label
            {
                Text = "Use 24-hour time, e.g. 08:00 and 08:40.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.Muted,
                BackColor = Color.White
            };
            var btnDefaults = CreateButton("Use Common Defaults", UiTheme.Navy, Color.White);
            btnDefaults.Click += (s, e) => LoadCommonDefaultPeriodsIntoGrid(confirm: true);
            var btnReload = CreateButton("Reload Periods", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnReload.Click += async (s, e) => await LoadPeriodsAsync();
            var btnSave = CreateButton("Save Periods", UiTheme.Success, Color.White);
            btnSave.Click += async (s, e) => await SavePeriodsAsync();
            btnDefaults.Visible = AuthService.CanWrite("Academics.Timetable.Manage");
            btnSave.Visible = AuthService.CanWrite("Academics.Timetable.Manage");
            bottom.Controls.Add(_periodSummaryLabel, 0, 0);
            bottom.Controls.Add(btnDefaults, 1, 0);
            bottom.Controls.Add(btnReload, 2, 0);
            bottom.Controls.Add(btnSave, 3, 0);
            main.Controls.Add(bottom, 0, 2);

            page.Controls.Add(main);
            return page;
        }

        private TabPage CreateConfigTab()
        {
            var page = new TabPage("Workload Setup") { BackColor = Color.White, Padding = new Padding(24, 18, 24, 22) };
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0)
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            var header = CreateTabHeader("Department Workload", "Select a department, choose the teaching mode, then prepare the setup used to generate that department.");
            main.Controls.Add(header, 0, 0);

            var inputs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 8,
                RowCount = 2,
                Padding = new Padding(0, 6, 0, 8)
            };
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            _cmbDepartmentSelect = CreateCombo();
            _cmbDepartmentSelect.SelectedIndexChanged += async (s, e) => await ApplyDepartmentSelectionAsync();

            _cmbClassSelect = CreateCombo();
            _cmbClassSelect.SelectedIndexChanged += async (s, e) =>
            {
                if (_isInitializing) return;
                await SelectAssignedTeacherForClassAsync();
                await RefreshAllocationsAsync();
            };

            _cmbTeacherMode = CreateCombo();
            _cmbTeacherMode.Items.AddRange(new object[] { SubjectBasedMode, OneTeacherMode });
            _cmbTeacherMode.SelectedIndex = 0;
            _cmbTeacherMode.SelectedIndexChanged += async (s, e) =>
            {
                UpdateTeacherModeUi();
                if (IsOneTeacherMode())
                    await SelectAssignedTeacherForClassAsync();
                if (!_isInitializing)
                    await RefreshAllocationsAsync();
            };

            _cmbSubject = CreateCombo();
            _cmbTeacher = CreateCombo();
            _periodsPerWeek = new Guna2NumericUpDown
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 0),
                Minimum = 1,
                Maximum = 20,
                Value = 3,
                BorderRadius = 4,
                BorderColor = UiTheme.Border,
                FillColor = Color.White,
                ForeColor = UiTheme.Text
            };

            _btnAddRequirement = CreateButton("Add Requirement", UiTheme.Navy, Color.White);
            _btnAddRequirement.Click += (s, e) => AddAllocation();
            _btnAddRequirement.Visible = AuthService.CanWrite("Academics.Timetable.Manage");
            var btnRefresh = CreateButton("Refresh", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnRefresh.Click += async (s, e) => await RefreshAllocationsAsync();

            AddInputLabel(inputs, "Department", 0);
            AddInputLabel(inputs, "Teaching Mode", 1);
            AddInputLabel(inputs, "Setup Class", 2);
            AddInputLabel(inputs, "Subject", 3);
            AddInputLabel(inputs, "Teacher", 4);
            AddInputLabel(inputs, "Periods", 5);
            inputs.Controls.Add(_cmbDepartmentSelect, 0, 1);
            inputs.Controls.Add(_cmbTeacherMode, 1, 1);
            inputs.Controls.Add(_cmbClassSelect, 2, 1);
            inputs.Controls.Add(_cmbSubject, 3, 1);
            inputs.Controls.Add(_cmbTeacher, 4, 1);
            inputs.Controls.Add(_periodsPerWeek, 5, 1);
            inputs.Controls.Add(_btnAddRequirement, 6, 1);
            inputs.Controls.Add(btnRefresh, 7, 1);
            main.Controls.Add(inputs, 0, 1);

            _configGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            StyleTimetableGrid(_configGrid);
            main.Controls.Add(_configGrid, 0, 2);

            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 0)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            _allocationSummaryLabel = new Label
            {
                Text = "Select a class to view workload requirements.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.Muted,
                BackColor = Color.White
            };
            _btnSavePeriodEdits = CreateButton("Save Period Edits", UiTheme.Navy, Color.White);
            _btnSavePeriodEdits.Click += async (s, e) => await SaveSubjectBasedPeriodEditsAsync(showSuccess: true);
            _btnSavePeriodEdits.Visible = AuthService.CanWrite("Academics.Timetable.Manage");
            _btnDeleteRequirement = CreateButton("Delete Selected", Color.FromArgb(210, 55, 70), Color.White);
            _btnDeleteRequirement.Click += async (s, e) => await DeleteSelectedRequirementAsync();
            _btnDeleteRequirement.Visible = AuthService.CanWrite("Academics.Timetable.Manage");
            var btnGenerate = CreateButton("Generate Timetable", UiTheme.Success, Color.White);
            btnGenerate.Click += async (s, e) => await GenerateTimetableAsync();
            btnGenerate.Visible = AuthService.CanWrite("Academics.Timetable.Manage");
            bottom.Controls.Add(_allocationSummaryLabel, 0, 0);
            bottom.Controls.Add(_btnSavePeriodEdits, 1, 0);
            bottom.Controls.Add(_btnDeleteRequirement, 2, 0);
            bottom.Controls.Add(btnGenerate, 3, 0);
            main.Controls.Add(bottom, 0, 3);
            page.Controls.Add(main);
            return page;
        }

        private TabPage CreateViewerTab()
        {
            var page = new TabPage("Weekly Timetable") { BackColor = Color.White, Padding = new Padding(24, 18, 24, 22) };
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            main.Controls.Add(CreateTabHeader("Weekly Grid", "Review generated timetables by department and class."), 0, 0);

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 8,
                RowCount = 1,
                Padding = new Padding(0, 6, 0, 8)
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            _cmbViewDepartmentSelect = CreateCombo();
            _cmbViewDepartmentSelect.SelectedIndexChanged += async (s, e) => await ApplyViewDepartmentSelectionAsync();
            _cmbViewClass = CreateCombo();
            _cmbViewClass.SelectedIndexChanged += async (s, e) =>
            {
                if (_isInitializing) return;
                await LoadTimetableGridAsync();
            };
            _viewerSummaryLabel = new Label
            {
                Text = "Choose a class to load its timetable.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.Muted,
                BackColor = Color.White,
                Padding = new Padding(12, 0, 0, 0)
            };
            var btnLoad = CreateButton("Load Timetable", UiTheme.Navy, Color.White);
            btnLoad.Click += async (s, e) => await LoadTimetableGridAsync();
            var btnPreview = CreateButton("Print Preview", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnPreview.Click += async (s, e) => await PreviewTimetableAsync();
            var btnPrint = CreateButton("Print", UiTheme.Success, Color.White);
            btnPrint.Click += async (s, e) => await PrintTimetableAsync();
            var btnPreviewDept = CreateButton("Preview Dept", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnPreviewDept.Click += async (s, e) => await PreviewDepartmentTimetableAsync();
            var btnExportDept = CreateButton("Export Dept PDF", UiTheme.Success, Color.White);
            btnExportDept.Click += async (s, e) => await ExportDepartmentTimetablePdfAsync();
            top.Controls.Add(_cmbViewDepartmentSelect, 0, 0);
            top.Controls.Add(_cmbViewClass, 1, 0);
            top.Controls.Add(_viewerSummaryLabel, 2, 0);
            top.Controls.Add(btnLoad, 3, 0);
            top.Controls.Add(btnPreview, 4, 0);
            top.Controls.Add(btnPrint, 5, 0);
            top.Controls.Add(btnPreviewDept, 6, 0);
            top.Controls.Add(btnExportDept, 7, 0);
            main.Controls.Add(top, 0, 1);

            _viewerGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            StyleTimetableGrid(_viewerGrid);
            main.Controls.Add(_viewerGrid, 0, 2);

            page.Controls.Add(main);
            return page;
        }

        private Guna2Panel CreateCardPanel()
        {
            return new Guna2Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                FillColor = Color.White,
                BackColor = UiTheme.Page,
                BorderColor = UiTheme.Border,
                BorderThickness = 1,
                BorderRadius = 6,
                ShadowDecoration = { Enabled = true, Color = Color.FromArgb(226, 232, 240), Depth = 6 }
            };
        }

        private Panel CreateTabHeader(string title, string subtitle)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            panel.Controls.Add(new Label
            {
                Text = title,
                ForeColor = UiTheme.Navy,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                Location = new Point(0, 2),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            panel.Controls.Add(new Label
            {
                Text = subtitle,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(1, 34),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            return panel;
        }

        private Guna2ComboBox CreateCombo()
        {
            return new Guna2ComboBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text,
                ItemHeight = 32
            };
        }

        private Guna2Button CreateButton(string text, Color fill, Color fore)
        {
            return new Guna2Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0),
                Height = 42,
                BorderRadius = 4,
                FillColor = fill,
                ForeColor = fore,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                HoverState = { FillColor = fill == UiTheme.Gold ? Color.FromArgb(232, 190, 0) : UiTheme.NavyHover }
            };
        }

        private void AddInputLabel(TableLayoutPanel table, string text, int column)
        {
            table.Controls.Add(new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.White,
                TextAlign = ContentAlignment.BottomLeft
            }, column, 0);
        }

        private void StyleTimetableGrid(DataGridView grid)
        {
            UiTheme.StyleDataGrid(grid, true);
            grid.ColumnHeadersHeight = 44;
            grid.RowTemplate.Height = 40;
            grid.DefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 12, 0);
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 251);
        }

        private void DrawTimetableTab(object sender, DrawItemEventArgs e)
        {
            bool selected = e.Index == _tabs.SelectedIndex;
            Rectangle bounds = e.Bounds;
            using (var back = new SolidBrush(selected ? UiTheme.Navy : Color.White))
            using (var border = new Pen(UiTheme.Border))
            {
                e.Graphics.FillRectangle(back, bounds);
                e.Graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    _tabs.TabPages[e.Index].Text,
                    _tabs.Font,
                    bounds,
                    selected ? Color.White : UiTheme.Navy,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private async System.Threading.Tasks.Task InitializeFormAsync()
        {
            _isInitializing = true;
            try
            {
                _statusLabel.Text = "Loading timetable data...";
                await _classRepo.EnsureTableExistsAsync();
                await LoadPeriodsAsync();

                _teachers = (await _empRepo.GetAllAsync()).ToList();
                _cmbTeacher.Items.Clear();
                _cmbTeacher.Items.Add("-- No Teacher Assigned --");
                foreach (var teacher in _teachers)
                {
                    _cmbTeacher.Items.Add($"{teacher.FullName} ({teacher.EmployeeID})");
                }
                _cmbTeacher.SelectedIndex = 0;
                UpdateTeacherModeUi();

                var classesTable = await _classRepo.GetAllClassesTableAsync();
                _allClassNames.Clear();
                foreach (DataRow row in classesTable.Rows)
                {
                    string className = row["ClassName"].ToString();
                    _allClassNames.Add(className);
                }

                PopulateDepartmentSelect();
                PopulateViewDepartmentSelect();
                if (_cmbDepartmentSelect.Items.Count > 0)
                {
                    var firstMappedClass = _allClassNames.FirstOrDefault(c => !string.IsNullOrWhiteSpace(Common.TimetableDepartments.GetDepartmentForClass(c)));
                    var initialDepartment = Common.TimetableDepartments.GetDepartmentForClass(firstMappedClass);
                    _cmbDepartmentSelect.SelectedItem = string.IsNullOrWhiteSpace(initialDepartment)
                        ? _cmbDepartmentSelect.Items[0]
                        : initialDepartment;
                    PopulateClassSelectForDepartment(_cmbDepartmentSelect.Text);
                }

                if (_cmbViewDepartmentSelect.Items.Count > 0)
                {
                    var department = _cmbDepartmentSelect.SelectedIndex >= 0 ? _cmbDepartmentSelect.Text : _cmbViewDepartmentSelect.Items[0]?.ToString();
                    _cmbViewDepartmentSelect.SelectedItem = department;
                    if (_cmbViewDepartmentSelect.SelectedIndex < 0)
                        _cmbViewDepartmentSelect.SelectedIndex = 0;
                    PopulateViewClassSelectForDepartment(_cmbViewDepartmentSelect.Text);
                }

                if (_cmbClassSelect.Items.Count > 0)
                {
                    _cmbClassSelect.SelectedIndex = 0;
                    await LoadSubjectsForSelectedClassAsync();
                }

                if (_cmbViewClass.Items.Count > 0 && _cmbViewClass.SelectedIndex < 0)
                    _cmbViewClass.SelectedIndex = 0;

                _statusLabel.Text = "Ready.";
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Timetable form initialization failed", ex);
                _statusLabel.Text = "Timetable data could not be loaded. Check the database connection and try Reload.";
                UIHelper.ShowError("Could not load timetable data: " + ex.Message, "Timetable");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void PopulateDepartmentSelect()
        {
            _cmbDepartmentSelect.Items.Clear();
            foreach (var department in GetAvailableDepartmentNames())
                _cmbDepartmentSelect.Items.Add(department);
        }

        private void PopulateViewDepartmentSelect()
        {
            _cmbViewDepartmentSelect.Items.Clear();
            foreach (var department in GetAvailableDepartmentNames())
                _cmbViewDepartmentSelect.Items.Add(department);
        }

        private IEnumerable<string> GetAvailableDepartmentNames()
        {
            var departments = Common.TimetableDepartments.Names
                .Where(department => _allClassNames.Any(className => Common.TimetableDepartments.BelongsToDepartment(className, department)))
                .ToList();

            return departments.Count > 0 ? departments : Common.TimetableDepartments.Names;
        }

        private void PopulateClassSelectForDepartment(string department)
        {
            var previousClass = _cmbClassSelect.Text;
            _cmbClassSelect.Items.Clear();

            var classNames = GetDepartmentClassNames(department);

            foreach (var className in classNames)
                _cmbClassSelect.Items.Add(className);

            if (_cmbClassSelect.Items.Count == 0)
                return;

            var match = _cmbClassSelect.Items
                .Cast<object>()
                .FirstOrDefault(item => string.Equals(item?.ToString(), previousClass, StringComparison.OrdinalIgnoreCase));
            _cmbClassSelect.SelectedItem = match ?? _cmbClassSelect.Items[0];
        }

        private void PopulateViewClassSelectForDepartment(string department)
        {
            var previousClass = _cmbViewClass.Text;
            _cmbViewClass.Items.Clear();

            var classNames = GetDepartmentClassNames(department);

            foreach (var className in classNames)
                _cmbViewClass.Items.Add(className);

            if (_cmbViewClass.Items.Count == 0)
                return;

            var match = _cmbViewClass.Items
                .Cast<object>()
                .FirstOrDefault(item => string.Equals(item?.ToString(), previousClass, StringComparison.OrdinalIgnoreCase));
            _cmbViewClass.SelectedItem = match ?? _cmbViewClass.Items[0];
        }

        private async System.Threading.Tasks.Task ApplyDepartmentSelectionAsync()
        {
            if (_isInitializing || _cmbDepartmentSelect.SelectedIndex < 0)
                return;

            _isInitializing = true;
            try
            {
                PopulateClassSelectForDepartment(_cmbDepartmentSelect.Text);
            }
            finally
            {
                _isInitializing = false;
            }

            if (_cmbClassSelect.SelectedIndex >= 0)
            {
                await SelectAssignedTeacherForClassAsync();
                await LoadSubjectsForSelectedClassAsync();
                await RefreshAllocationsAsync();
            }
        }

        private async System.Threading.Tasks.Task ApplyViewDepartmentSelectionAsync()
        {
            if (_isInitializing || _cmbViewDepartmentSelect.SelectedIndex < 0)
                return;

            _isInitializing = true;
            try
            {
                PopulateViewClassSelectForDepartment(_cmbViewDepartmentSelect.Text);
            }
            finally
            {
                _isInitializing = false;
            }

            if (_cmbViewClass.SelectedIndex >= 0)
                await LoadTimetableGridAsync();
        }

        private string GetSelectedDepartmentName()
        {
            var department = _cmbDepartmentSelect?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(department))
                return department;

            return Common.TimetableDepartments.GetDepartmentForClass(_cmbClassSelect?.Text);
        }

        private string GetSelectedViewDepartmentName()
        {
            var department = _cmbViewDepartmentSelect?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(department))
                return department;

            return Common.TimetableDepartments.GetDepartmentForClass(_cmbViewClass?.Text);
        }

        private List<string> GetDepartmentClassNames(string department)
        {
            var result = new List<string>();

            foreach (var className in _allClassNames ?? new List<string>())
            {
                if (Common.TimetableDepartments.BelongsToDepartment(className, department))
                    AddClassName(result, className);
            }

            foreach (var className in Common.TimetableDepartments.GetClasses(department))
            {
                if (Common.AppConfig.ClassNames.Any(c => SameClassName(c, className)))
                    AddClassName(result, className);
            }

            return result
                .OrderBy(GetClassSortOrder)
                .ThenBy(c => c)
                .ToList();
        }

        private static void AddClassName(List<string> target, string className)
        {
            className = (className ?? "").Trim();
            if (string.IsNullOrWhiteSpace(className)) return;
            if (target.Any(existing => SameClassName(existing, className))) return;
            target.Add(className);
        }

        private static bool SameClassName(string left, string right)
        {
            return NormalizeClassName(left) == NormalizeClassName(right);
        }

        private static string NormalizeClassName(string value)
        {
            return (value ?? "").Trim().Replace(".", "").Replace("-", " ").ToUpperInvariant();
        }

        private static int GetClassSortOrder(string className)
        {
            for (int i = 0; i < Common.AppConfig.ClassNames.Length; i++)
            {
                if (SameClassName(Common.AppConfig.ClassNames[i], className))
                    return i;
            }

            return 999;
        }

        private async System.Threading.Tasks.Task LoadPeriodsAsync()
        {
            var periods = await _repo.GetPeriodsAsync();
            if (periods.Count == 0)
            {
                PopulatePeriodGrid(DefaultTimetablePeriods.Create());
                _periodSummaryLabel.Text = AuthService.CanWrite("Academics.Timetable.Manage")
                    ? "Common default periods loaded. Click Save Periods to apply them, or generate a timetable to save them automatically."
                    : "Common default periods loaded for preview. A timetable manager must save them before generation.";
                return;
            }

            PopulatePeriodGrid(periods);
            _periodSummaryLabel.Text = periods.Count == 0
                ? "No periods configured. Add the school's periods before generating timetables."
                : periods.Count + " period(s) configured. Saving changes will clear generated timetable entries.";
        }

        private void LoadCommonDefaultPeriodsIntoGrid(bool confirm)
        {
            if (confirm && _periodGrid.Rows.Cast<DataGridViewRow>().Any(row => !row.IsNewRow))
            {
                var answer = UIHelper.ShowConfirmation(
                    "This will replace the period rows on this screen with the common school-day structure. Existing saved periods will not change until you click Save Periods. Continue?",
                    "Timetable");
                if (answer != DialogResult.Yes) return;
            }

            PopulatePeriodGrid(DefaultTimetablePeriods.Create());
            _periodSummaryLabel.Text = "Common default periods loaded. Click Save Periods to apply them.";
            _statusLabel.Text = "Common timetable period structure loaded.";
        }

        private void PopulatePeriodGrid(IEnumerable<TimePeriod> periods)
        {
            _periodGrid.Rows.Clear();
            foreach (var period in periods.OrderBy(p => p.SortOrder))
            {
                _periodGrid.Rows.Add(
                    period.PeriodName,
                    period.StartTime.ToString(@"hh\:mm"),
                    period.EndTime.ToString(@"hh\:mm"),
                    period.IsBreak,
                    period.SortOrder);
            }
        }

        private async System.Threading.Tasks.Task SavePeriodsAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Timetable.Manage", "Save Timetable Periods"))
                return;

            _periodGrid.EndEdit();

            var periods = new List<TimePeriod>();
            int fallbackOrder = 1;
            TimeSpan? previousEnd = null;
            foreach (DataGridViewRow row in _periodGrid.Rows)
            {
                if (row.IsNewRow) continue;

                string name = row.Cells["PeriodName"].Value?.ToString()?.Trim();
                string startText = row.Cells["StartTime"].Value?.ToString()?.Trim();
                string endText = row.Cells["EndTime"].Value?.ToString()?.Trim();
                string orderText = row.Cells["SortOrder"].Value?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!TryParseSchoolDayTime(startText, previousEnd, out var start))
                {
                    UIHelper.ShowWarning("Enter a valid start time for " + name + ". Use 24-hour time, for example 08:00.", "Timetable");
                    return;
                }
                if (!TryParseSchoolDayTime(endText, start, out var end))
                {
                    UIHelper.ShowWarning("Enter a valid end time for " + name + ". Use 24-hour time, for example 08:40.", "Timetable");
                    return;
                }
                if (end <= start)
                {
                    UIHelper.ShowWarning(name + " has an invalid time range. The end time must be later than the start time.", "Timetable");
                    return;
                }

                bool isBreak = Convert.ToBoolean(row.Cells["IsBreak"].Value ?? false);
                int sortOrder = int.TryParse(orderText, out int parsedOrder) && parsedOrder > 0
                    ? parsedOrder
                    : fallbackOrder;

                periods.Add(new TimePeriod
                {
                    PeriodName = name,
                    StartTime = start,
                    EndTime = end,
                    IsBreak = isBreak,
                    SortOrder = sortOrder
                });
                previousEnd = end;
                fallbackOrder++;
            }

            if (periods.Count == 0)
            {
                UIHelper.ShowWarning("Add at least one teaching period or break before saving.", "Timetable");
                return;
            }

            if (UIHelper.ShowConfirmation("Saving periods will clear generated timetable entries so they can be regenerated using the new school times. Continue?", "Timetable") != DialogResult.Yes)
            {
                return;
            }

            await _repo.SetPeriodsAsync(periods.OrderBy(p => p.SortOrder).ToList());
            await LoadPeriodsAsync();
            if (_cmbViewClass.SelectedIndex >= 0)
            {
                await LoadTimetableGridAsync();
            }
            _statusLabel.Text = "School periods saved. Regenerate class timetables with the new times.";
            UIHelper.ShowSuccess("School periods saved successfully.", "Timetable");
        }

        private static bool TryParseSchoolDayTime(string text, TimeSpan? reference, out TimeSpan value)
        {
            value = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (!TimeSpan.TryParse(text.Trim(), CultureInfo.CurrentCulture, out value) &&
                !TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out value))
            {
                if (DateTime.TryParse(text.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var date) ||
                    DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out date))
                {
                    value = date.TimeOfDay;
                }
                else
                {
                    return false;
                }
            }

            if (reference.HasValue && value <= reference.Value)
            {
                var afternoonValue = value.Add(TimeSpan.FromHours(12));
                if (afternoonValue > reference.Value && afternoonValue < TimeSpan.FromHours(19))
                {
                    value = afternoonValue;
                }
            }

            return true;
        }

        private async System.Threading.Tasks.Task RefreshAllocationsAsync()
        {
            if (_cmbClassSelect.SelectedIndex < 0) return;
            try
            {
                _statusLabel.Text = "Loading workload requirements...";
                await LoadSubjectsForSelectedClassAsync();

                if (IsOneTeacherMode())
                {
                    await RefreshOneTeacherPreviewAsync();
                    _statusLabel.Text = "Ready.";
                    return;
                }

                var allocs = await _repo.GetAllocationsAsync(_cmbClassSelect.Text);
                _configGrid.ReadOnly = false;
                _configGrid.AllowUserToDeleteRows = false;
                _configGrid.DataSource = BuildAllocationsTable(allocs);
                ConfigureSubjectBasedAllocationGrid();
                _allocationSummaryLabel.Text = allocs.Count == 0
                    ? "No workload requirements saved for " + _cmbClassSelect.Text + "."
                    : allocs.Count + " workload requirement(s) saved for " + _cmbClassSelect.Text + ". Edit Periods, then click Save Period Edits.";
                FitGridColumns(_configGrid);
                _statusLabel.Text = "Ready.";
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Timetable workload load failed", ex);
                _allocationSummaryLabel.Text = "Workload requirements could not be loaded.";
                _statusLabel.Text = "Could not load workload requirements.";
                UIHelper.ShowError("Could not load workload requirements: " + ex.Message, "Timetable");
            }
        }

        private DataTable BuildAllocationsTable(IEnumerable<SubjectAllocation> allocations)
        {
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("ClassID", typeof(string));
            table.Columns.Add("Subject", typeof(string));
            table.Columns.Add("Teacher", typeof(string));
            table.Columns.Add("TeacherID", typeof(int));
            table.Columns.Add("Periods", typeof(int));

            foreach (var allocation in allocations ?? Enumerable.Empty<SubjectAllocation>())
            {
                var row = table.NewRow();
                row["ID"] = allocation.AllocationID;
                row["ClassID"] = allocation.ClassID ?? "";
                row["Subject"] = allocation.SubjectName ?? "";
                row["Teacher"] = allocation.TeacherName ?? "";
                row["TeacherID"] = allocation.TeacherID.HasValue ? (object)allocation.TeacherID.Value : DBNull.Value;
                row["Periods"] = allocation.PeriodsPerWeek;
                table.Rows.Add(row);
            }

            return table;
        }

        private void ConfigureSubjectBasedAllocationGrid()
        {
            foreach (DataGridViewColumn column in _configGrid.Columns)
            {
                column.ReadOnly = true;
            }

            if (_configGrid.Columns["ID"] != null) _configGrid.Columns["ID"].Visible = false;
            if (_configGrid.Columns["ClassID"] != null) _configGrid.Columns["ClassID"].Visible = false;
            if (_configGrid.Columns["TeacherID"] != null) _configGrid.Columns["TeacherID"].Visible = false;
            if (_configGrid.Columns["Periods"] != null)
            {
                _configGrid.Columns["Periods"].ReadOnly = false;
                _configGrid.Columns["Periods"].HeaderText = "Periods";
                _configGrid.Columns["Periods"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private async System.Threading.Tasks.Task<bool> SaveSubjectBasedPeriodEditsAsync(bool showSuccess)
        {
            if (IsOneTeacherMode())
                return true;

            if (!AuthService.RequireWriteAccess("Academics.Timetable.Manage", "Save Timetable Requirement Periods"))
                return false;

            _configGrid.EndEdit();
            if (_configGrid.Rows.Count == 0)
                return true;

            int savedCount = 0;
            foreach (DataGridViewRow gridRow in _configGrid.Rows)
            {
                if (gridRow.IsNewRow) continue;
                if (!(gridRow.DataBoundItem is DataRowView rowView)) continue;

                var row = rowView.Row;
                if (!int.TryParse(row["ID"]?.ToString(), out int allocationId) || allocationId <= 0)
                    continue;

                if (!int.TryParse(row["Periods"]?.ToString(), out int periods) || periods <= 0 || periods > 20)
                {
                    UIHelper.ShowWarning("Enter a valid period count from 1 to 20 for " + row["Subject"] + ".", "Timetable");
                    return false;
                }

                int? teacherId = null;
                if (row["TeacherID"] != DBNull.Value && int.TryParse(row["TeacherID"].ToString(), out int parsedTeacherId))
                    teacherId = parsedTeacherId;

                var allocation = new SubjectAllocation
                {
                    AllocationID = allocationId,
                    ClassID = row["ClassID"]?.ToString() ?? _cmbClassSelect.Text,
                    SubjectName = row["Subject"]?.ToString() ?? "",
                    TeacherID = teacherId,
                    TeacherName = row["Teacher"]?.ToString() ?? "",
                    PeriodsPerWeek = periods
                };

                if (!await _repo.SaveAllocationAsync(allocation))
                {
                    UIHelper.ShowError("Could not save period changes for " + allocation.SubjectName + ".", "Timetable");
                    return false;
                }
                savedCount++;
            }

            if (showSuccess)
            {
                _statusLabel.Text = "Saved " + savedCount + " workload period edit(s). Regenerate the timetable to apply them.";
                UIHelper.ShowSuccess("Period edits saved. Regenerate the timetable to apply the new workload.", "Timetable");
                await RefreshAllocationsAsync();
            }

            return true;
        }

        private async System.Threading.Tasks.Task RefreshOneTeacherPreviewAsync()
        {
            var subjects = await GetSubjectsForSelectedClassAsync();
            var teacherName = _cmbTeacher.SelectedIndex > 0
                ? _teachers[_cmbTeacher.SelectedIndex - 1].FullName
                : "Select one teacher";
            int periods = (int)_periodsPerWeek.Value;

            var table = new DataTable();
            table.Columns.Add("Subject", typeof(string));
            table.Columns.Add("Teacher", typeof(string));
            table.Columns.Add("Periods", typeof(int));

            foreach (var subject in subjects)
            {
                table.Rows.Add(subject, teacherName, periods);
            }

            _configGrid.DataSource = table;
            _configGrid.ReadOnly = true;
            _configGrid.AllowUserToDeleteRows = false;
            if (_configGrid.Columns["Subject"] != null) _configGrid.Columns["Subject"].ReadOnly = true;
            if (_configGrid.Columns["Teacher"] != null) _configGrid.Columns["Teacher"].ReadOnly = true;
            if (_configGrid.Columns["Periods"] != null) _configGrid.Columns["Periods"].ReadOnly = true;
            _allocationSummaryLabel.Text = subjects.Count == 0
                ? "No subjects found for " + _cmbClassSelect.Text + "."
                : "Class-teacher mode: all subjects use the selected teacher and the same period count. Switch to subject-based mode for individual subject changes.";
            FitGridColumns(_configGrid);
        }

        private async void AddAllocation()
        {
            if (!AuthService.RequireWriteAccess("Academics.Timetable.Manage", "Save Timetable Requirement"))
                return;

            if (IsOneTeacherMode())
            {
                if (!EnsureOneTeacherSelected())
                    return;

                await RefreshOneTeacherPreviewAsync();
                _statusLabel.Text = "One-teacher setup prepared. Click Generate Timetable.";
                return;
            }

            if (_cmbClassSelect.SelectedIndex < 0) { UIHelper.ShowWarning("Please select a class first."); return; }
            if (_cmbSubject.SelectedIndex < 0) { UIHelper.ShowWarning("Choose a subject first."); return; }

            int? teacherId = null;
            if (_cmbTeacher.SelectedIndex > 0 && int.TryParse(_teachers[_cmbTeacher.SelectedIndex - 1].EmployeeID, out int id))
            {
                teacherId = id;
            }

            var allocation = new SubjectAllocation
            {
                ClassID = _cmbClassSelect.Text,
                SubjectName = _cmbSubject.Text,
                TeacherID = teacherId,
                PeriodsPerWeek = (int)_periodsPerWeek.Value
            };

            if (await _repo.SaveAllocationAsync(allocation))
            {
                _statusLabel.Text = "Saved workload requirement for " + allocation.SubjectName + ".";
                await RefreshAllocationsAsync();
            }
            else
            {
                UIHelper.ShowError("Could not save the subject requirement.", "Timetable");
            }
        }

        private async System.Threading.Tasks.Task DeleteSelectedRequirementAsync()
        {
            if (!AuthService.RequireWriteAccess("Academics.Timetable.Manage", "Delete Timetable Requirement"))
                return;

            if (_configGrid.CurrentRow == null || _configGrid.CurrentRow.IsNewRow)
            {
                UIHelper.ShowWarning("Select a workload row first.", "Timetable");
                return;
            }

            if (IsOneTeacherMode())
            {
                UIHelper.ShowWarning(
                    "Class-teacher mode uses one shared subject list and one period count for the whole class.\n\n" +
                    "Requirements cannot be deleted here. Switch to subject-based mode if this class needs individual subject changes.",
                    "Timetable");
                return;
            }

            var idValue = _configGrid.CurrentRow.Cells["ID"]?.Value;
            if (idValue == null || !int.TryParse(idValue.ToString(), out int allocationId) || allocationId <= 0)
            {
                UIHelper.ShowWarning("This workload row cannot be deleted because its record ID is missing.", "Timetable");
                return;
            }

            var name = _configGrid.CurrentRow.Cells["Subject"]?.Value?.ToString() ?? "this requirement";
            if (UIHelper.ShowConfirmation("Delete the workload requirement for " + name + "?", "Timetable") != DialogResult.Yes)
                return;

            if (await _repo.DeleteAllocationAsync(allocationId))
            {
                _statusLabel.Text = "Deleted workload requirement for " + name + ".";
                await RefreshAllocationsAsync();
            }
            else
            {
                UIHelper.ShowError("Could not delete the selected workload requirement.", "Timetable");
            }
        }

        private async System.Threading.Tasks.Task GenerateTimetableAsync()
        {
            if (_isBusy) return;

            if (!AuthService.RequireWriteAccess("Academics.Timetable.Manage", "Generate Timetable"))
                return;

            try
            {
                SetTimetableBusy(true, "Checking timetable setup...");

                if (_cmbDepartmentSelect.SelectedIndex < 0)
                {
                    UIHelper.ShowWarning("Select the department you want to generate.", "Timetable");
                    return;
                }

                if (_cmbClassSelect.SelectedIndex < 0)
                {
                    UIHelper.ShowWarning("Select the setup class for this department.", "Timetable");
                    return;
                }
                string classId = _cmbClassSelect.Text;

                if (!await EnsureTeachingPeriodsConfiguredAsync())
                    return;

                if (!IsOneTeacherMode() && !await SaveSubjectBasedPeriodEditsAsync(showSuccess: false))
                    return;

                if (IsOneTeacherMode())
                    await GenerateOneTeacherDepartmentTimetableAsync(classId);
                else
                    await GenerateDepartmentTimetableAsync(classId);
            }
            finally
            {
                SetTimetableBusy(false);
            }
        }

        private async System.Threading.Tasks.Task GenerateDepartmentTimetableAsync(string selectedClassId)
        {
            var department = GetSelectedDepartmentName();
            _statusLabel.Text = "Generating " + department + " timetable...";

            var availableClassNames = GetDepartmentClassNames(department);

            if (availableClassNames.Count == 0)
            {
                UIHelper.ShowWarning("No classes were found under " + department + ".", "Timetable Setup Required");
                _statusLabel.Text = "No classes found for selected department.";
                return;
            }

            var allAllocations = await _repo.GetAllocationsAsync();
            var selectedAllocations = allAllocations
                .Where(a => string.Equals(a.ClassID, selectedClassId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (selectedAllocations.Count == 0)
            {
                UIHelper.ShowWarning(
                    "No workload requirements have been saved for " + selectedClassId + ".\n\n" +
                    "Add at least one subject requirement before generating the " + department + " timetable.",
                    "Timetable Setup Required");
                _statusLabel.Text = "Add workload requirements before generating.";
                return;
            }

            var allocationsByClass = new Dictionary<string, List<SubjectAllocation>>(StringComparer.OrdinalIgnoreCase);
            foreach (var className in availableClassNames)
            {
                allocationsByClass[className] = selectedAllocations
                    .Select(a => CloneAllocationForClass(a, className))
                    .ToList();
            }

            var result = await _generator.GenerateForDepartmentAsync(department, allocationsByClass);
            if (!result.Success)
            {
                ShowGenerationMessage(result.Message);
                return;
            }

            await _repo.SaveTimetableBatchesAsync(result.Batches);
            UIHelper.ShowSuccess(
                department + " timetable generated and saved for " + result.Batches.Count + " class(es).\n\n" +
                selectedClassId + " was used as the department setup template.",
                "Timetable");
            _tabs.SelectedIndex = 2;
            _cmbViewClass.SelectedItem = selectedClassId;
            await LoadTimetableGridAsync();
            _statusLabel.Text = department + " timetable generated.";
        }

        private SubjectAllocation CloneAllocationForClass(SubjectAllocation source, string classId)
        {
            return new SubjectAllocation
            {
                ClassID = classId,
                SubjectName = source.SubjectName,
                TeacherID = source.TeacherID,
                TeacherName = source.TeacherName,
                PeriodsPerWeek = source.PeriodsPerWeek
            };
        }

        private async System.Threading.Tasks.Task<bool> EnsureTeachingPeriodsConfiguredAsync()
        {
            var teachingPeriods = (await _repo.GetPeriodsAsync()).Where(p => !p.IsBreak).ToList();
            if (teachingPeriods.Count > 0)
                return true;

            if (AuthService.CanWrite("Academics.Timetable.Manage"))
            {
                var defaults = DefaultTimetablePeriods.Create();
                await _repo.SetPeriodsAsync(defaults);
                PopulatePeriodGrid(defaults);
                _periodSummaryLabel.Text = defaults.Count + " common default period(s) saved. You can edit them later from Period Setup.";
                _statusLabel.Text = "Common default teaching periods saved.";
                return true;
            }

            const string message =
                "The timetable cannot be generated because no teaching periods have been saved yet.\n\n" +
                "Go to Period Setup, add the school's teaching periods, and click Save Periods. " +
                "Break periods are not counted as teaching periods.";
            UIHelper.ShowWarning(message, "Timetable Setup Required");
            _tabs.SelectedIndex = 0;
            _statusLabel.Text = "Add and save teaching periods before generating a timetable.";
            return false;
        }

        private void ShowGenerationMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                UIHelper.ShowError("Generation failed.", "Timetable");
                _statusLabel.Text = "Generation failed.";
                return;
            }

            bool setupIssue =
                message.IndexOf("teaching periods", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("selected workload", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("subject allocations", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Add at least one subject", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("No classes or workload", StringComparison.OrdinalIgnoreCase) >= 0;

            if (setupIssue)
            {
                UIHelper.ShowWarning(message, "Timetable Setup Required");
                _statusLabel.Text = "Review the timetable setup before generating.";
            }
            else
            {
                UIHelper.ShowError(message, "Timetable Could Not Be Generated");
                _statusLabel.Text = "Generation failed.";
            }
        }

        private void SetTimetableBusy(bool busy, string status = null)
        {
            _isBusy = busy;
            UseWaitCursor = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (_periodGrid != null) _periodGrid.Enabled = !busy;
            if (_configGrid != null) _configGrid.Enabled = !busy;
            if (_viewerGrid != null) _viewerGrid.Enabled = !busy;
            if (_btnAddRequirement != null) _btnAddRequirement.Enabled = !busy;
            if (_btnDeleteRequirement != null) _btnDeleteRequirement.Enabled = !busy;
            if (_btnSavePeriodEdits != null) _btnSavePeriodEdits.Enabled = !busy && !IsOneTeacherMode();
            if (_tabs != null) _tabs.Enabled = !busy;
            if (_statusLabel != null && !string.IsNullOrWhiteSpace(status))
                _statusLabel.Text = status;
        }

        private async System.Threading.Tasks.Task<(bool Success, string Message, List<TimetableEntry> Entries)> GenerateOneTeacherTimetableAsync(string classId)
        {
            if (_cmbTeacher.SelectedIndex <= 0)
                return (false, "Select the teacher who handles all subjects for this class before generating.", null);

            int teacherId;
            if (!int.TryParse(_teachers[_cmbTeacher.SelectedIndex - 1].EmployeeID, out teacherId))
                return (false, "The selected teacher has an invalid employee ID.", null);

            var subjects = await GetSubjectsForSelectedClassAsync();
            if (subjects.Count == 0)
                return (false, "No subjects found for this class.", null);

            _configGrid.EndEdit();
            var allocations = BuildOneTeacherAllocationsFromGrid(classId, teacherId, _teachers[_cmbTeacher.SelectedIndex - 1].FullName);
            if (allocations.Count == 0)
                return (false, "Add at least one subject with one or more periods before generating.", null);

            return await _generator.GenerateForClassAsync(classId, allocations);
        }

        private async System.Threading.Tasks.Task GenerateOneTeacherDepartmentTimetableAsync(string selectedClassId)
        {
            if (!EnsureOneTeacherSelected())
                return;

            _configGrid.EndEdit();
            var department = GetSelectedDepartmentName();
            _statusLabel.Text = "Generating " + department + " timetable...";

            var departmentClasses = GetDepartmentClassNames(department);

            if (departmentClasses.Count == 0)
            {
                UIHelper.ShowWarning("No classes were found under " + department + ".", "Timetable Setup Required");
                _statusLabel.Text = "No classes found for selected department.";
                return;
            }

            var assignments = (await _classRepo.GetAllClassAssignmentsAsync()).ToList();
            var selectedTeacher = _teachers[_cmbTeacher.SelectedIndex - 1];
            if (!int.TryParse(selectedTeacher.EmployeeID, out int selectedTeacherId))
            {
                UIHelper.ShowWarning("The selected teacher has an invalid employee ID.", "Timetable");
                return;
            }

            int commonPeriods = (int)_periodsPerWeek.Value;
            var missingTeachers = new List<string>();
            var allocationsByClass = new Dictionary<string, List<SubjectAllocation>>(StringComparer.OrdinalIgnoreCase);

            foreach (var className in departmentClasses)
            {
                int? teacherId = null;
                string teacherName = "";
                var assignment = assignments.FirstOrDefault(a => string.Equals(a.ClassName, className, StringComparison.OrdinalIgnoreCase));
                if (assignment.CurrentTeacherID.HasValue)
                {
                    teacherId = assignment.CurrentTeacherID.Value;
                    var teacher = _teachers.FirstOrDefault(t => int.TryParse(t.EmployeeID, out int id) && id == teacherId.Value);
                    teacherName = teacher?.FullName ?? "Assigned Teacher";
                }
                else if (string.Equals(className, selectedClassId, StringComparison.OrdinalIgnoreCase))
                {
                    teacherId = selectedTeacherId;
                    teacherName = selectedTeacher.FullName;
                }

                if (!teacherId.HasValue)
                {
                    missingTeachers.Add(className);
                    continue;
                }

                var subjects = await GetSubjectsForClassAsync(className);
                var classAllocations = subjects
                    .Select(subject => new SubjectAllocation
                    {
                        ClassID = className,
                        SubjectName = subject,
                        TeacherID = teacherId,
                        TeacherName = teacherName,
                        PeriodsPerWeek = commonPeriods
                    })
                    .Where(a => a.PeriodsPerWeek > 0)
                    .ToList();

                if (classAllocations.Count > 0)
                    allocationsByClass[className] = classAllocations;
            }

            if (missingTeachers.Count > 0)
            {
                UIHelper.ShowWarning(
                    "The department timetable cannot be generated because these classes do not have a class teacher assigned:\n\n" +
                    string.Join(", ", missingTeachers) +
                    "\n\nAssign a class teacher to each class, then generate the department timetable again.",
                    "Timetable Setup Required");
                _statusLabel.Text = "Assign class teachers before generating.";
                return;
            }

            var result = await _generator.GenerateForDepartmentAsync(department, allocationsByClass, enforceTeacherConflicts: false);
            if (!result.Success)
            {
                ShowGenerationMessage(result.Message);
                return;
            }

            await _repo.SaveTimetableBatchesAsync(result.Batches);
            UIHelper.ShowSuccess(
                department + " class-teacher timetable generated and saved for " + result.Batches.Count + " class(es).",
                "Timetable");
            _tabs.SelectedIndex = 2;
            _cmbViewClass.SelectedItem = selectedClassId;
            await LoadTimetableGridAsync();
            _statusLabel.Text = department + " timetable generated.";
        }

        private async System.Threading.Tasks.Task<IReadOnlyList<string>> GetSubjectsForClassAsync(string className)
        {
            try
            {
                var configured = await _subjectRepo.GetSubjectsForClassAsync(className);
                return configured.Count > 0 && !SubjectCatalog.IsLegacyDefaultList(configured)
                    ? (IReadOnlyList<string>)configured
                    : SubjectCatalog.StandardSubjectsForClass(className);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Timetable subjects fell back to legacy list: " + ex.Message);
                return SubjectCatalog.StandardSubjectsForClass(className);
            }
        }

        private List<SubjectAllocation> BuildOneTeacherAllocationsFromGrid(string classId, int teacherId, string teacherName)
        {
            var allocations = new List<SubjectAllocation>();
            foreach (DataGridViewRow row in _configGrid.Rows)
            {
                if (row.IsNewRow) continue;
                var subject = row.Cells["Subject"]?.Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(subject)) continue;

                var periodsValue = row.Cells["Periods"]?.Value;
                if (!int.TryParse(periodsValue?.ToString(), out int periods) || periods <= 0)
                    continue;

                allocations.Add(new SubjectAllocation
                {
                    ClassID = classId,
                    SubjectName = subject,
                    TeacherID = teacherId,
                    PeriodsPerWeek = periods,
                    TeacherName = teacherName
                });
            }
            return allocations;
        }

        private async System.Threading.Tasks.Task LoadTimetableGridAsync()
        {
            if (_cmbViewClass.SelectedIndex < 0) return;
            try
            {
                _statusLabel.Text = "Loading weekly timetable...";
                var entries = await _repo.GetTimetableAsync(_cmbViewClass.Text);
                var periods = await _repo.GetPeriodsAsync();

                // Transform into a 5-day grid for display
                var table = new DataTable();
                table.Columns.Add("Period");
                table.Columns.Add("Monday");
                table.Columns.Add("Tuesday");
                table.Columns.Add("Wednesday");
                table.Columns.Add("Thursday");
                table.Columns.Add("Friday");

                foreach (var p in periods)
                {
                    var row = table.NewRow();
                    row["Period"] = p.PeriodName + " (" + p.DisplayTime + ")";

                    if (p.IsBreak)
                    {
                        row["Monday"] = row["Tuesday"] = row["Wednesday"] = row["Thursday"] = row["Friday"] = "--- BREAK ---";
                    }
                    else
                    {
                        for (int d = 1; d <= 5; d++)
                        {
                            var entry = entries.FirstOrDefault(e => e.PeriodID == p.PeriodID && e.DayOfWeek == d);
                            row[d] = entry != null ? $"{entry.SubjectName}\n({entry.TeacherName})" : "FREE";
                        }
                    }
                    table.Rows.Add(row);
                }

                _viewerGrid.DataSource = table;
                foreach (DataGridViewColumn col in _viewerGrid.Columns) col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                _viewerGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                _viewerSummaryLabel.Text = table.Rows.Count == 0
                    ? "No timetable entries found for " + _cmbViewClass.Text + "."
                    : "Showing weekly timetable for " + _cmbViewClass.Text + ".";
                FitGridColumns(_viewerGrid);
                _statusLabel.Text = "Ready.";
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Weekly timetable load failed", ex);
                _viewerSummaryLabel.Text = "Weekly timetable could not be loaded.";
                _statusLabel.Text = "Could not load weekly timetable.";
                UIHelper.ShowError("Could not load weekly timetable: " + ex.Message, "Timetable");
            }
        }

        private async System.Threading.Tasks.Task<bool> PreparePrintDataAsync()
        {
            ClearDepartmentPrintState();
            if (_cmbViewClass.SelectedIndex < 0)
            {
                UIHelper.ShowWarning("Choose a class before printing.", "Timetable");
                return false;
            }

            _printClassName = _cmbViewClass.Text;
            _printPeriods = await _repo.GetPeriodsAsync();
            _printEntries = await _repo.GetTimetableAsync(_printClassName);

            if (_printPeriods.Count == 0)
            {
                UIHelper.ShowWarning("No school periods have been configured for printing.", "Timetable");
                return false;
            }

            if (_printEntries.Count == 0)
            {
                UIHelper.ShowWarning("No generated timetable entries found for " + _printClassName + ".", "Timetable");
                return false;
            }

            return true;
        }

        private async System.Threading.Tasks.Task<bool> PrepareDepartmentPrintDataAsync()
        {
            ClearDepartmentPrintState();
            string department = GetSelectedViewDepartmentName();
            if (string.IsNullOrWhiteSpace(department))
            {
                UIHelper.ShowWarning("Choose the department you want to export.", "Timetable");
                return false;
            }

            _printPeriods = await _repo.GetPeriodsAsync();
            if (_printPeriods.Count == 0)
            {
                UIHelper.ShowWarning("No school periods have been configured for printing.", "Timetable");
                return false;
            }

            var departmentClasses = GetDepartmentClassNames(department);

            var entriesByClass = new Dictionary<string, List<TimetableEntry>>(StringComparer.OrdinalIgnoreCase);
            var missing = new List<string>();
            foreach (var className in departmentClasses)
            {
                var entries = await _repo.GetTimetableAsync(className);
                if (entries.Count == 0)
                {
                    missing.Add(className);
                    continue;
                }
                entriesByClass[className] = entries;
            }

            if (entriesByClass.Count == 0)
            {
                UIHelper.ShowWarning("No generated timetable entries were found for " + department + ". Generate the department timetable first.", "Timetable");
                return false;
            }

            if (missing.Count > 0)
            {
                var answer = UIHelper.ShowConfirmation(
                    "These class(es) do not have generated timetable entries and will be skipped:\n\n" +
                    string.Join(", ", missing) +
                    "\n\nContinue with the available class timetables?",
                    "Department Timetable");
                if (answer != DialogResult.Yes)
                    return false;
            }

            _departmentPrintClasses = entriesByClass.Keys.OrderBy(c => c).ToList();
            _departmentPrintEntries = entriesByClass;
            _departmentPrintIndex = 0;
            _printClassName = department + " Department";
            _printEntries = entriesByClass[_departmentPrintClasses[0]];
            return true;
        }

        private async System.Threading.Tasks.Task PreviewTimetableAsync()
        {
            if (!await PreparePrintDataAsync()) return;

            using (var doc = CreateTimetablePrintDocument())
            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = doc;
                preview.Width = 1200;
                preview.Height = 800;
                preview.StartPosition = FormStartPosition.CenterParent;
                preview.Text = "Timetable Print Preview";
                preview.ShowDialog(this);
            }
        }

        private async System.Threading.Tasks.Task PrintTimetableAsync()
        {
            if (!await PreparePrintDataAsync()) return;

            using (var doc = CreateTimetablePrintDocument())
            using (var dialog = new PrintDialog())
            {
                dialog.Document = doc;
                dialog.AllowSomePages = false;
                dialog.UseEXDialog = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        SetTimetableBusy(true, "Sending timetable to printer...");
                        await System.Threading.Tasks.Task.Run(() => doc.Print());
                        _statusLabel.Text = "Timetable sent to printer.";
                    }
                    finally
                    {
                        SetTimetableBusy(false);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task PreviewDepartmentTimetableAsync()
        {
            if (!await PrepareDepartmentPrintDataAsync()) return;

            using (var doc = CreateTimetablePrintDocument())
            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = doc;
                preview.Width = 1200;
                preview.Height = 800;
                preview.StartPosition = FormStartPosition.CenterParent;
                preview.Text = "Department Timetable Preview";
                preview.ShowDialog(this);
            }

            ClearDepartmentPrintState();
        }

        private async System.Threading.Tasks.Task ExportDepartmentTimetablePdfAsync()
        {
            if (!await PrepareDepartmentPrintDataAsync()) return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save Department Timetable PDF";
                dialog.Filter = "PDF files (*.pdf)|*.pdf";
                dialog.FileName = SafeFileName(_printClassName) + " Timetables.pdf";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    ClearDepartmentPrintState();
                    return;
                }

                using (var doc = CreateTimetablePrintDocument())
                {
                    if (!ConfigurePdfPrinter(doc, dialog.FileName))
                    {
                        ClearDepartmentPrintState();
                        UIHelper.ShowWarning("Microsoft Print to PDF is not available. Use Preview Dept and choose a PDF printer manually.", "Timetable");
                        return;
                    }

                    try
                    {
                        doc.PrintController = new StandardPrintController();
                        SetTimetableBusy(true, "Exporting department timetable PDF...");
                        await System.Threading.Tasks.Task.Run(() => doc.Print());
                    }
                    finally
                    {
                        SetTimetableBusy(false);
                    }
                }

                _statusLabel.Text = "Department timetable PDF exported.";
                UIHelper.ShowSuccess("Department timetable PDF exported successfully.", "Timetable");
            }

            ClearDepartmentPrintState();
        }

        private PrintDocument CreateTimetablePrintDocument()
        {
            var doc = new PrintDocument
            {
                DocumentName = "Timetable - " + _printClassName
            };
            doc.DefaultPageSettings.Landscape = true;
            doc.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);
            doc.BeginPrint += (s, e) =>
            {
                if (_departmentPrintClasses != null)
                    _departmentPrintIndex = 0;
            };
            doc.PrintPage += PrintTimetablePage;
            return doc;
        }

        private bool ConfigurePdfPrinter(PrintDocument doc, string fileName)
        {
            string pdfPrinter = null;
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (string.Equals(printer, "Microsoft Print to PDF", StringComparison.OrdinalIgnoreCase))
                {
                    pdfPrinter = printer;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(pdfPrinter))
                return false;

            doc.PrinterSettings.PrinterName = pdfPrinter;
            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = fileName;
            return doc.PrinterSettings.IsValid;
        }

        private string SafeFileName(string value)
        {
            var name = string.IsNullOrWhiteSpace(value) ? "Department" : value.Trim();
            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(ch, '-');
            }
            return name;
        }

        private void ClearDepartmentPrintState()
        {
            _departmentPrintClasses = null;
            _departmentPrintEntries = null;
            _departmentPrintIndex = 0;
        }

        private void PrintTimetablePage(object sender, PrintPageEventArgs e)
        {
            if (_departmentPrintClasses != null &&
                _departmentPrintEntries != null &&
                _departmentPrintIndex >= 0 &&
                _departmentPrintIndex < _departmentPrintClasses.Count)
            {
                _printClassName = _departmentPrintClasses[_departmentPrintIndex];
                _printEntries = _departmentPrintEntries.TryGetValue(_printClassName, out var entries)
                    ? entries
                    : new List<TimetableEntry>();
            }

            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle bounds = e.MarginBounds;
            Color navy = UiTheme.Navy;
            Color gold = UiTheme.Gold;
            Color border = Color.FromArgb(80, 92, 112);
            Color headerFill = Color.FromArgb(235, 244, 255);
            Color fixedFill = Color.FromArgb(245, 247, 251);

            var periods = _printPeriods.OrderBy(p => p.SortOrder).ToList();

            using (var schoolFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
            using (var titleFont = new Font("Segoe UI Semibold", 20F, FontStyle.Bold))
            using (var classFont = new Font("Segoe UI Semibold", 18F, FontStyle.Bold))
            using (var metaFont = new Font("Segoe UI", 7.4F))
            using (var headerFont = new Font("Segoe UI Semibold", 7.4F, FontStyle.Bold))
            using (var dayFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold))
            using (var fixedFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
            using (var cellFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold))
            using (var teacherFont = new Font("Segoe UI", 5.8F))
            using (var footerFont = new Font("Segoe UI", 6.4F))
            using (var navyBrush = new SolidBrush(navy))
            using (var textBrush = new SolidBrush(UiTheme.Text))
            using (var mutedBrush = new SolidBrush(UiTheme.Muted))
            using (var goldBrush = new SolidBrush(gold))
            using (var headerBrush = new SolidBrush(headerFill))
            using (var fixedBrush = new SolidBrush(fixedFill))
            using (var borderPen = new Pen(border, 1))
            using (var thickPen = new Pen(navy, 2))
            {
                string school = SchoolProfile.DisplayName.ToUpperInvariant();
                string contact = BuildSchoolContactLine();
                int logoSize = 42;
                int headerTop = bounds.Top;
                int titleLeft = bounds.Left + logoSize + 12;
                Image logo = null;
                try { logo = Branding.GetLogo(false); } catch { logo = null; }
                if (logo != null)
                    g.DrawImage(logo, new Rectangle(bounds.Left, headerTop + 2, logoSize, logoSize));

                DrawPrintText(g, school, schoolFont, textBrush, new Rectangle(titleLeft, headerTop, bounds.Width - logoSize - 24, 18), ContentAlignment.MiddleCenter);
                DrawPrintText(g, _printClassName.ToUpperInvariant(), classFont, textBrush, new Rectangle(titleLeft, headerTop + 17, bounds.Width - logoSize - 24, 30), ContentAlignment.MiddleCenter);
                DrawPrintText(g, contact, metaFont, mutedBrush, new Rectangle(titleLeft, headerTop + 48, bounds.Width - logoSize - 24, 14), ContentAlignment.MiddleCenter);
                DrawPrintText(g, "Generated: " + DateTime.Now.ToString("dd MMM yyyy, h:mm tt"), metaFont, mutedBrush, new Rectangle(bounds.Right - 210, headerTop + 3, 210, 16), ContentAlignment.MiddleRight);

                int gridTop = bounds.Top + 76;
                int footerHeight = 34;
                int headerHeight = 48;
                int bodyHeight = Math.Max(240, bounds.Bottom - gridTop - footerHeight - headerHeight);
                int dayColumnWidth = 42;
                int rowHeight = Math.Max(58, bodyHeight / 5);
                int tableHeight = headerHeight + (rowHeight * 5);
                int tableWidth = bounds.Width;

                var widths = CalculateTimetableColumnWidths(periods, tableWidth - dayColumnWidth);
                var gridRect = new Rectangle(bounds.Left, gridTop, tableWidth, tableHeight);
                g.FillRectangle(Brushes.White, gridRect);
                g.DrawRectangle(thickPen, gridRect);

                DrawPrintHeaderCell(g, "", new Rectangle(bounds.Left, gridTop, dayColumnWidth, headerHeight), headerFont, navyBrush, Brushes.White, borderPen);

                int x = bounds.Left + dayColumnWidth;
                for (int i = 0; i < periods.Count; i++)
                {
                    var period = periods[i];
                    var rect = new Rectangle(x, gridTop, widths[i], headerHeight);
                    g.FillRectangle(IsFixedScheduleColumn(period) ? fixedBrush : headerBrush, rect);
                    g.DrawRectangle(borderPen, rect);
                    DrawPrintText(g, period.PeriodName, headerFont, textBrush, new Rectangle(rect.Left + 2, rect.Top + 4, rect.Width - 4, 22), ContentAlignment.MiddleCenter);
                    DrawPrintText(g, period.DisplayTime, metaFont, mutedBrush, new Rectangle(rect.Left + 2, rect.Top + 26, rect.Width - 4, 16), ContentAlignment.MiddleCenter);
                    x += widths[i];
                }

                int bodyTop = gridTop + headerHeight;
                for (int day = 1; day <= 5; day++)
                {
                    int y = bodyTop + ((day - 1) * rowHeight);
                    var dayRect = new Rectangle(bounds.Left, y, dayColumnWidth, rowHeight);
                    g.FillRectangle(Brushes.White, dayRect);
                    g.DrawRectangle(borderPen, dayRect);
                    DrawPrintText(g, ShortDayName(day), dayFont, mutedBrush, dayRect, ContentAlignment.MiddleCenter);
                }

                x = bounds.Left + dayColumnWidth;
                for (int i = 0; i < periods.Count; i++)
                {
                    var period = periods[i];
                    int colWidth = widths[i];
                    bool fixedColumn = IsFixedScheduleColumn(period);

                    if (fixedColumn)
                    {
                        var fixedRect = new Rectangle(x, bodyTop, colWidth, rowHeight * 5);
                        g.FillRectangle(fixedBrush, fixedRect);
                        g.DrawRectangle(borderPen, fixedRect);
                        DrawRotatedCenterText(g, FixedScheduleLabel(period), fixedFont, mutedBrush, fixedRect, -90F);
                        x += colWidth;
                        continue;
                    }

                    for (int day = 1; day <= 5; day++)
                    {
                        int y = bodyTop + ((day - 1) * rowHeight);
                        var cell = new Rectangle(x, y, colWidth, rowHeight);
                        g.FillRectangle(Brushes.White, cell);
                        g.DrawRectangle(borderPen, cell);

                        var entry = _printEntries.FirstOrDefault(t => t.PeriodID == period.PeriodID && t.DayOfWeek == day);
                        if (entry != null)
                        {
                            DrawPrintText(g, CompactSubjectName(entry.SubjectName), cellFont, textBrush, new Rectangle(cell.Left + 3, cell.Top + 8, cell.Width - 6, Math.Max(26, cell.Height - 26)), ContentAlignment.MiddleCenter);
                            DrawPrintText(g, CompactTeacherName(entry.TeacherName), teacherFont, mutedBrush, new Rectangle(cell.Left + 3, cell.Bottom - 17, cell.Width - 6, 13), ContentAlignment.MiddleCenter);
                        }
                    }
                    x += colWidth;
                }

                DrawPrintText(g, "Prepared by: ____________________", footerFont, mutedBrush, new Rectangle(bounds.Left, bounds.Bottom - 30, 210, 15), ContentAlignment.MiddleLeft);
                DrawPrintText(g, "Approved by: ____________________", footerFont, mutedBrush, new Rectangle(bounds.Right - 230, bounds.Bottom - 30, 230, 15), ContentAlignment.MiddleRight);
                Common.PrintBranding.DrawGraphicsFooter(g, bounds);
            }

            if (_departmentPrintClasses != null)
            {
                _departmentPrintIndex++;
                e.HasMorePages = _departmentPrintIndex < _departmentPrintClasses.Count;
            }
            else
            {
                e.HasMorePages = false;
            }
        }

        private int[] CalculateTimetableColumnWidths(List<TimePeriod> periods, int availableWidth)
        {
            if (periods == null || periods.Count == 0) return new int[0];

            double totalWeight = periods.Sum(p => IsFixedScheduleColumn(p) ? 0.72D : 1D);
            var widths = new int[periods.Count];
            int used = 0;

            for (int i = 0; i < periods.Count; i++)
            {
                double weight = IsFixedScheduleColumn(periods[i]) ? 0.72D : 1D;
                widths[i] = Math.Max(36, (int)Math.Floor(availableWidth * weight / totalWeight));
                used += widths[i];
            }

            widths[widths.Length - 1] += availableWidth - used;
            return widths;
        }

        private bool IsFixedScheduleColumn(TimePeriod period)
        {
            if (period == null) return false;
            string name = (period.PeriodName ?? "").Trim().ToUpperInvariant();
            return period.IsBreak
                || name.Contains("BREAK")
                || name.Contains("LUNCH")
                || name.Contains("SILENCE")
                || name.Contains("ASSEMBLY")
                || name.Contains("REGISTRATION")
                || name.Contains("CLOSING");
        }

        private string FixedScheduleLabel(TimePeriod period)
        {
            string name = (period?.PeriodName ?? "").Trim();
            string upper = name.ToUpperInvariant();
            if (upper.Contains("SILENCE")) return "SILENCE HOUR";
            if (upper.Contains("ASSEMBLY") && upper.Contains("REGISTRATION")) return "ASSEMBLY / REGISTRATION";
            if (upper.Contains("ASSEMBLY")) return "ASSEMBLY";
            if (upper.Contains("REGISTRATION")) return "REGISTRATION";
            if (upper.Contains("LUNCH")) return "LUNCH TIME";
            if (upper.Contains("BREAK")) return "BREAK TIME";
            if (upper.Contains("CLOSING")) return "CLOSING";
            return string.IsNullOrWhiteSpace(name) ? "BREAK" : name.ToUpperInvariant();
        }

        private string CompactSubjectName(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return "";
            return subject.Trim()
                .Replace("ENGLISH LANGUAGE", "ENG.\nLAN")
                .Replace("MATHEMATICS", "MATH\nS")
                .Replace("SOCIAL STUDIES", "SOCIAL\nSTUD.")
                .Replace("INTEGRATED SCIENCE", "INT.\nSCI")
                .Replace("GHANAIAN LANGUAGE", "GHAN.\nLANG")
                .Replace("REL. & MORAL EDU.", "RME")
                .Replace("CAREER TECHNOLOGY", "CAREER\nTECH.")
                .Replace("CREATIVE ART", "CREA.\nT. ART");
        }

        private string CompactTeacherName(string teacher)
        {
            if (string.IsNullOrWhiteSpace(teacher)) return "";
            string value = teacher.Trim();
            return value.Length <= 18 ? value : value.Substring(0, 18) + "...";
        }

        private void DrawRotatedCenterText(Graphics g, string text, Font font, Brush brush, Rectangle rect, float angle)
        {
            var state = g.Save();
            try
            {
                g.TranslateTransform(rect.Left + rect.Width / 2F, rect.Top + rect.Height / 2F);
                g.RotateTransform(angle);
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    var rotated = new RectangleF(-rect.Height / 2F, -rect.Width / 2F, rect.Height, rect.Width);
                    g.DrawString(text ?? "", font, brush, rotated, format);
                }
            }
            finally
            {
                g.Restore(state);
            }
        }

        private void DrawPrintHeaderCell(Graphics g, string text, Rectangle rect, Font font, Brush back, Brush fore, Pen border)
        {
            g.FillRectangle(back, rect);
            g.DrawRectangle(border, rect);
            DrawPrintText(g, text, font, fore, rect, ContentAlignment.MiddleCenter);
        }

        private void DrawPrintText(Graphics g, string text, Font font, Brush brush, Rectangle rect, ContentAlignment align)
        {
            using (var format = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.LineLimit
            })
            {
                switch (align)
                {
                    case ContentAlignment.MiddleCenter:
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.MiddleRight:
                        format.Alignment = StringAlignment.Far;
                        format.LineAlignment = StringAlignment.Center;
                        break;
                    case ContentAlignment.TopCenter:
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Near;
                        break;
                    default:
                        format.Alignment = StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Center;
                        break;
                }

                g.DrawString(text ?? "", font, brush, rect, format);
            }
        }

        private string BuildSchoolContactLine()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(SchoolProfile.Address)) parts.Add(SchoolProfile.Address);
            if (!string.IsNullOrWhiteSpace(SchoolProfile.Phones)) parts.Add(SchoolProfile.Phones);
            if (!string.IsNullOrWhiteSpace(SchoolProfile.Email)) parts.Add(SchoolProfile.Email);
            return parts.Count == 0 ? "Weekly academic schedule" : string.Join(" | ", parts);
        }

        private void DrawHeaderCell(Graphics g, string text, Rectangle rect, Font font, Brush back, Brush fore, Pen border)
        {
            g.FillRectangle(back, rect);
            g.DrawRectangle(border, rect);
            DrawText(g, text, font, fore, rect, ContentAlignment.MiddleCenter);
        }

        private void DrawText(Graphics g, string text, Font font, Brush brush, Rectangle rect, ContentAlignment align)
        {
            var flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
            switch (align)
            {
                case ContentAlignment.MiddleCenter:
                    flags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                    break;
                case ContentAlignment.MiddleRight:
                    flags |= TextFormatFlags.Right | TextFormatFlags.VerticalCenter;
                    break;
                case ContentAlignment.TopCenter:
                    flags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.Top;
                    break;
                default:
                    flags |= TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                    break;
            }
            TextRenderer.DrawText(g, text ?? "", font, rect, ((SolidBrush)brush).Color, flags);
        }

        private string DayName(int day)
        {
            switch (day)
            {
                case 1: return "Monday";
                case 2: return "Tuesday";
                case 3: return "Wednesday";
                case 4: return "Thursday";
                case 5: return "Friday";
                default: return "";
            }
        }

        private string ShortDayName(int day)
        {
            switch (day)
            {
                case 1: return "Mo";
                case 2: return "Tu";
                case 3: return "We";
                case 4: return "Th";
                case 5: return "Fr";
                default: return "";
            }
        }

        private async System.Threading.Tasks.Task LoadSubjectsForSelectedClassAsync()
        {
            string previousSubject = _cmbSubject.Text;
            _cmbSubject.Items.Clear();
            if (_cmbClassSelect.SelectedIndex < 0) return;

            var subjects = await GetSubjectsForSelectedClassAsync();

            foreach (var subject in subjects)
            {
                _cmbSubject.Items.Add(subject);
            }

            if (_cmbSubject.Items.Count > 0)
            {
                var match = _cmbSubject.Items.Cast<object>()
                    .FirstOrDefault(item => string.Equals(item?.ToString(), previousSubject, StringComparison.OrdinalIgnoreCase));
                _cmbSubject.SelectedItem = match ?? _cmbSubject.Items[0];
            }
        }

        private async System.Threading.Tasks.Task<IReadOnlyList<string>> GetSubjectsForSelectedClassAsync()
        {
            if (_cmbClassSelect.SelectedIndex < 0)
                return new List<string>();

            try
            {
                var configured = await _subjectRepo.GetSubjectsForClassAsync(_cmbClassSelect.Text);
                return configured.Count > 0 && !SubjectCatalog.IsLegacyDefaultList(configured)
                    ? (IReadOnlyList<string>)configured
                    : SubjectCatalog.StandardSubjectsForClass(_cmbClassSelect.Text);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Timetable subjects fell back to legacy list: " + ex.Message);
                return SubjectCatalog.StandardSubjectsForClass(_cmbClassSelect.Text);
            }
        }

        private bool IsOneTeacherMode()
        {
            return string.Equals(_cmbTeacherMode?.Text, OneTeacherMode, StringComparison.OrdinalIgnoreCase);
        }

        private bool EnsureOneTeacherSelected()
        {
            if (!IsOneTeacherMode() || _cmbTeacher.SelectedIndex > 0)
                return true;

            UIHelper.ShowWarning("Select the teacher who handles all subjects for this class before preparing or generating.", "Timetable");
            _statusLabel.Text = "Select one teacher for this class.";
            _cmbTeacher.Focus();
            return false;
        }

        private async System.Threading.Tasks.Task SelectAssignedTeacherForClassAsync()
        {
            if (!IsOneTeacherMode() || _cmbClassSelect.SelectedIndex < 0 || _cmbTeacher == null)
                return;

            try
            {
                var assignments = await _classRepo.GetAllClassAssignmentsAsync();
                var assigned = assignments.FirstOrDefault(a => string.Equals(a.ClassName, _cmbClassSelect.Text, StringComparison.OrdinalIgnoreCase));
                if (assigned.CurrentTeacherID.HasValue)
                {
                    SelectTeacherByEmployeeId(assigned.CurrentTeacherID.Value);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning("Could not auto-select class teacher for timetable: " + ex.Message);
            }
        }

        private void SelectTeacherByEmployeeId(int employeeId)
        {
            for (int i = 0; i < _teachers.Count; i++)
            {
                if (int.TryParse(_teachers[i].EmployeeID, out int id) && id == employeeId)
                {
                    _cmbTeacher.SelectedIndex = i + 1;
                    return;
                }
            }
        }

        private void UpdateTeacherModeUi()
        {
            bool oneTeacher = IsOneTeacherMode();
            if (_cmbTeacher != null && _cmbTeacher.Items.Count > 0)
            {
                _cmbTeacher.Items[0] = oneTeacher ? "-- Select Teacher --" : "-- No Teacher Assigned --";
            }
            if (_cmbSubject != null)
            {
                _cmbSubject.Enabled = !oneTeacher;
                _cmbSubject.Visible = !oneTeacher;
            }
            if (_btnAddRequirement != null)
                _btnAddRequirement.Text = oneTeacher ? "Prepare" : "Add Requirement";
            if (_btnSavePeriodEdits != null)
            {
                _btnSavePeriodEdits.Enabled = !oneTeacher;
                _btnSavePeriodEdits.Visible = AuthService.CanWrite("Academics.Timetable.Manage") && !oneTeacher;
            }
            if (_btnDeleteRequirement != null)
            {
                _btnDeleteRequirement.Enabled = !oneTeacher;
                _btnDeleteRequirement.Visible = AuthService.CanWrite("Academics.Timetable.Manage") && !oneTeacher;
            }
            if (_periodsPerWeek != null)
                _periodsPerWeek.Maximum = oneTeacher ? 10 : 20;
            if (_allocationSummaryLabel != null)
            {
                _allocationSummaryLabel.Text = oneTeacher
                    ? "Select a department, setup class, class teacher, and periods; then generate the department timetable."
                    : "Select a department and setup class to prepare subject-based workload requirements.";
            }
        }

        private async void OnSubjectCatalogChanged(object sender, SubjectCatalogChangedEventArgs e)
        {
            if (IsDisposed || _cmbClassSelect == null) return;
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<SubjectCatalogChangedEventArgs>(OnSubjectCatalogChanged), sender, e);
                return;
            }

            if (_cmbClassSelect.SelectedIndex < 0 || !e.AppliesTo(_cmbClassSelect.Text)) return;

            await LoadSubjectsForSelectedClassAsync();
            _statusLabel.Text = "Subjects refreshed for " + _cmbClassSelect.Text + ".";
        }

        private void FitGridColumns(DataGridView grid)
        {
            if (grid == null || grid.Columns.Count == 0) return;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.MinimumWidth = 110;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }
    }
}
