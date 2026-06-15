using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
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
        private Guna2ComboBox _cmbClassSelect, _cmbViewClass, _cmbSubject, _cmbTeacher;
        private Guna2NumericUpDown _periodsPerWeek;
        private TabControl _tabs;
        private Label _statusLabel, _periodSummaryLabel, _allocationSummaryLabel, _viewerSummaryLabel;
        private List<Employee> _teachers = new List<Employee>();
        private List<TimePeriod> _printPeriods = new List<TimePeriod>();
        private List<TimetableEntry> _printEntries = new List<TimetableEntry>();
        private string _printClassName = "";

        public frmTimetable()
        {
            InitializeComponent();
            this.Icon = Branding.AppIcon;
            _repo = new TimetableRepository(AppConfig.ConnectionString);
            _generator = new TimetableGeneratorService(_repo);
            _classRepo = new ClassRepository(AppConfig.ConnectionString);
            _empRepo = new EmployeeRepository(AppConfig.ConnectionString);
            _subjectRepo = new SubjectRepository(AppConfig.ConnectionString);

            BuildUi();
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
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 0)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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
            var btnReload = CreateButton("Reload Periods", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnReload.Click += async (s, e) => await LoadPeriodsAsync();
            var btnSave = CreateButton("Save Periods", UiTheme.Success, Color.White);
            btnSave.Click += async (s, e) => await SavePeriodsAsync();
            bottom.Controls.Add(_periodSummaryLabel, 0, 0);
            bottom.Controls.Add(btnReload, 1, 0);
            bottom.Controls.Add(btnSave, 2, 0);
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

            var header = CreateTabHeader("Class Workload", "Select a class, choose a subject and teacher, then set how many periods it needs each week.");
            main.Controls.Add(header, 0, 0);

            var inputs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 6,
                RowCount = 2,
                Padding = new Padding(0, 6, 0, 8)
            };
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            _cmbClassSelect = CreateCombo();
            _cmbClassSelect.SelectedIndexChanged += async (s, e) => await RefreshAllocationsAsync();

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

            var btnAdd = CreateButton("Add Requirement", UiTheme.Navy, Color.White);
            btnAdd.Click += (s, e) => AddAllocation();
            var btnRefresh = CreateButton("Refresh", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnRefresh.Click += async (s, e) => await RefreshAllocationsAsync();

            AddInputLabel(inputs, "Class", 0);
            AddInputLabel(inputs, "Subject", 1);
            AddInputLabel(inputs, "Teacher", 2);
            AddInputLabel(inputs, "Periods", 3);
            inputs.Controls.Add(_cmbClassSelect, 0, 1);
            inputs.Controls.Add(_cmbSubject, 1, 1);
            inputs.Controls.Add(_cmbTeacher, 2, 1);
            inputs.Controls.Add(_periodsPerWeek, 3, 1);
            inputs.Controls.Add(btnAdd, 4, 1);
            inputs.Controls.Add(btnRefresh, 5, 1);
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
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 12, 0, 0)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            _allocationSummaryLabel = new Label
            {
                Text = "Select a class to view workload requirements.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UiTheme.Muted,
                BackColor = Color.White
            };
            var btnGenerate = CreateButton("Generate Timetable", UiTheme.Success, Color.White);
            btnGenerate.Click += async (s, e) => await GenerateTimetableAsync();
            bottom.Controls.Add(_allocationSummaryLabel, 0, 0);
            bottom.Controls.Add(btnGenerate, 1, 0);
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

            main.Controls.Add(CreateTabHeader("Weekly Grid", "Review generated timetables by class."), 0, 0);

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0, 6, 0, 8)
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            _cmbViewClass = CreateCombo();
            _cmbViewClass.SelectedIndexChanged += async (s, e) => await LoadTimetableGridAsync();
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
            top.Controls.Add(_cmbViewClass, 0, 0);
            top.Controls.Add(_viewerSummaryLabel, 1, 0);
            top.Controls.Add(btnLoad, 2, 0);
            top.Controls.Add(btnPreview, 3, 0);
            top.Controls.Add(btnPrint, 4, 0);
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
            _statusLabel.Text = "Loading timetable data...";
            await _classRepo.EnsureTableExistsAsync();
            await _subjectRepo.EnsureTableAsync();
            await LoadPeriodsAsync();

            _teachers = (await _empRepo.GetAllAsync()).ToList();
            _cmbTeacher.Items.Clear();
            _cmbTeacher.Items.Add("-- No Teacher Assigned --");
            foreach (var teacher in _teachers)
            {
                _cmbTeacher.Items.Add($"{teacher.FullName} ({teacher.EmployeeID})");
            }
            _cmbTeacher.SelectedIndex = 0;

            var classesTable = await _classRepo.GetAllClassesTableAsync();
            _cmbClassSelect.Items.Clear();
            _cmbViewClass.Items.Clear();
            foreach (DataRow row in classesTable.Rows)
            {
                string className = row["ClassName"].ToString();
                _cmbClassSelect.Items.Add(className);
                _cmbViewClass.Items.Add(className);
            }

            if (_cmbClassSelect.Items.Count > 0)
            {
                _cmbClassSelect.SelectedIndex = 0;
                _cmbViewClass.SelectedIndex = 0;
            }

            _statusLabel.Text = "Ready.";
        }

        private async System.Threading.Tasks.Task LoadPeriodsAsync()
        {
            var periods = await _repo.GetPeriodsAsync();
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

            _periodSummaryLabel.Text = periods.Count == 0
                ? "No periods configured. Add the school's periods before generating timetables."
                : periods.Count + " period(s) configured. Saving changes will clear generated timetable entries.";
        }

        private async System.Threading.Tasks.Task SavePeriodsAsync()
        {
            _periodGrid.EndEdit();

            var periods = new List<TimePeriod>();
            int fallbackOrder = 1;
            foreach (DataGridViewRow row in _periodGrid.Rows)
            {
                if (row.IsNewRow) continue;

                string name = row.Cells["PeriodName"].Value?.ToString()?.Trim();
                string startText = row.Cells["StartTime"].Value?.ToString()?.Trim();
                string endText = row.Cells["EndTime"].Value?.ToString()?.Trim();
                string orderText = row.Cells["SortOrder"].Value?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!TimeSpan.TryParse(startText, out var start))
                {
                    UIHelper.ShowWarning("Enter a valid start time for " + name + " (example: 08:00).", "Timetable");
                    return;
                }
                if (!TimeSpan.TryParse(endText, out var end))
                {
                    UIHelper.ShowWarning("Enter a valid end time for " + name + " (example: 08:40).", "Timetable");
                    return;
                }
                if (end <= start)
                {
                    UIHelper.ShowWarning(name + " must end after it starts.", "Timetable");
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

        private async System.Threading.Tasks.Task RefreshAllocationsAsync()
        {
            if (_cmbClassSelect.SelectedIndex < 0) return;
            LoadSubjectsForSelectedClass();

            var allocs = await _repo.GetAllocationsAsync(_cmbClassSelect.Text);
            _configGrid.DataSource = allocs.Select(a => new
            {
                ID = a.AllocationID,
                Subject = a.SubjectName,
                Teacher = a.TeacherName,
                Periods = a.PeriodsPerWeek
            }).ToList();
            _allocationSummaryLabel.Text = allocs.Count == 0
                ? "No workload requirements saved for " + _cmbClassSelect.Text + "."
                : allocs.Count + " workload requirement(s) saved for " + _cmbClassSelect.Text + ".";
            FitGridColumns(_configGrid);
        }

        private async void AddAllocation()
        {
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

        private async System.Threading.Tasks.Task GenerateTimetableAsync()
        {
            if (_cmbClassSelect.SelectedIndex < 0) return;
            string classId = _cmbClassSelect.Text;

            _statusLabel.Text = "Generating timetable for " + classId + "...";
            
            var result = await _generator.GenerateForClassAsync(classId);
            if (result.Success)
            {
                await _repo.SaveTimetableBatchAsync(classId, result.Entries);
                UIHelper.ShowSuccess("Success! Timetable generated and saved.");
                _tabs.SelectedIndex = 1;
                _cmbViewClass.SelectedItem = classId;
                await LoadTimetableGridAsync();
                _statusLabel.Text = "Timetable generated for " + classId + ".";
            }
            else
            {
                UIHelper.ShowError("Generation Failed: " + result.Message);
                _statusLabel.Text = "Generation failed.";
            }
        }

        private async System.Threading.Tasks.Task LoadTimetableGridAsync()
        {
            if (_cmbViewClass.SelectedIndex < 0) return;
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
        }

        private async System.Threading.Tasks.Task<bool> PreparePrintDataAsync()
        {
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
                    doc.Print();
                    _statusLabel.Text = "Timetable sent to printer.";
                }
            }
        }

        private PrintDocument CreateTimetablePrintDocument()
        {
            var doc = new PrintDocument
            {
                DocumentName = "Timetable - " + _printClassName
            };
            doc.DefaultPageSettings.Landscape = true;
            doc.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);
            doc.PrintPage += PrintTimetablePage;
            return doc;
        }

        private void PrintTimetablePage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            g.PageUnit = GraphicsUnit.Pixel;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle bounds = e.MarginBounds;
            Color navy = UiTheme.Navy;
            Color gold = UiTheme.Gold;
            Color border = Color.FromArgb(80, 92, 112);
            Color soft = Color.FromArgb(244, 247, 251);
            Color breakFill = Color.FromArgb(255, 248, 204);

            using (var titleFont = new Font("Segoe UI Semibold", 20F, FontStyle.Bold))
            using (var schoolFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold))
            using (var metaFont = new Font("Segoe UI", 8.5F))
            using (var headerFont = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold))
            using (var cellFont = new Font("Segoe UI Semibold", 8.2F, FontStyle.Bold))
            using (var teacherFont = new Font("Segoe UI", 7.2F))
            using (var smallFont = new Font("Segoe UI", 7F))
            using (var navyBrush = new SolidBrush(navy))
            using (var whiteBrush = new SolidBrush(Color.White))
            using (var textBrush = new SolidBrush(UiTheme.Text))
            using (var mutedBrush = new SolidBrush(UiTheme.Muted))
            using (var goldBrush = new SolidBrush(gold))
            using (var softBrush = new SolidBrush(soft))
            using (var breakBrush = new SolidBrush(breakFill))
            using (var borderPen = new Pen(border, 1))
            using (var thickPen = new Pen(navy, 2))
            {
                var headerRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width, 92);
                g.FillRectangle(navyBrush, headerRect);
                g.FillRectangle(goldBrush, bounds.Left, bounds.Top + 88, bounds.Width, 4);

                string school = SchoolProfile.DisplayName;
                string contact = BuildSchoolContactLine();
                DrawText(g, school, schoolFont, whiteBrush, new Rectangle(bounds.Left + 18, bounds.Top + 12, bounds.Width - 36, 22), ContentAlignment.MiddleLeft);
                DrawText(g, "CLASS TIMETABLE", titleFont, whiteBrush, new Rectangle(bounds.Left + 18, bounds.Top + 34, bounds.Width - 36, 34), ContentAlignment.MiddleCenter);
                DrawText(g, contact, metaFont, whiteBrush, new Rectangle(bounds.Left + 18, bounds.Top + 66, bounds.Width - 36, 18), ContentAlignment.MiddleLeft);

                int metaY = bounds.Top + 106;
                DrawText(g, "Class: " + _printClassName, headerFont, textBrush, new Rectangle(bounds.Left, metaY, 240, 22), ContentAlignment.MiddleLeft);
                DrawText(g, "Generated: " + DateTime.Now.ToString("dd MMM yyyy, h:mm tt"), metaFont, mutedBrush, new Rectangle(bounds.Left + 250, metaY, 260, 22), ContentAlignment.MiddleLeft);
                DrawText(g, "Nyansapo School ERP", metaFont, mutedBrush, new Rectangle(bounds.Right - 220, metaY, 220, 22), ContentAlignment.MiddleRight);

                int gridTop = metaY + 32;
                int footerHeight = 30;
                int gridHeight = Math.Max(120, bounds.Bottom - gridTop - footerHeight);
                int periodColWidth = 150;
                int dayColWidth = (bounds.Width - periodColWidth) / 5;
                int rowCount = Math.Max(1, _printPeriods.Count);
                int headerHeight = 34;
                int rowHeight = Math.Max(42, (gridHeight - headerHeight) / rowCount);

                var gridRect = new Rectangle(bounds.Left, gridTop, periodColWidth + (dayColWidth * 5), headerHeight + (rowHeight * rowCount));
                g.FillRectangle(Brushes.White, gridRect);
                g.DrawRectangle(thickPen, gridRect);

                DrawHeaderCell(g, "Time", new Rectangle(bounds.Left, gridTop, periodColWidth, headerHeight), headerFont, navyBrush, whiteBrush, borderPen);
                for (int d = 1; d <= 5; d++)
                {
                    DrawHeaderCell(g, DayName(d), new Rectangle(bounds.Left + periodColWidth + ((d - 1) * dayColWidth), gridTop, dayColWidth, headerHeight), headerFont, navyBrush, whiteBrush, borderPen);
                }

                int y = gridTop + headerHeight;
                foreach (var period in _printPeriods.OrderBy(p => p.SortOrder))
                {
                    var timeRect = new Rectangle(bounds.Left, y, periodColWidth, rowHeight);
                    g.FillRectangle(period.IsBreak ? breakBrush : softBrush, timeRect);
                    g.DrawRectangle(borderPen, timeRect);
                    DrawText(g, period.PeriodName, headerFont, textBrush, new Rectangle(timeRect.Left + 6, timeRect.Top + 6, timeRect.Width - 12, 18), ContentAlignment.MiddleCenter);
                    DrawText(g, period.DisplayTime, smallFont, mutedBrush, new Rectangle(timeRect.Left + 6, timeRect.Top + 25, timeRect.Width - 12, 18), ContentAlignment.MiddleCenter);

                    for (int day = 1; day <= 5; day++)
                    {
                        var cell = new Rectangle(bounds.Left + periodColWidth + ((day - 1) * dayColWidth), y, dayColWidth, rowHeight);
                        g.FillRectangle(period.IsBreak ? breakBrush : Brushes.White, cell);
                        g.DrawRectangle(borderPen, cell);

                        if (period.IsBreak)
                        {
                            DrawText(g, "BREAK", headerFont, mutedBrush, cell, ContentAlignment.MiddleCenter);
                        }
                        else
                        {
                            var entry = _printEntries.FirstOrDefault(t => t.PeriodID == period.PeriodID && t.DayOfWeek == day);
                            if (entry != null)
                            {
                                DrawText(g, entry.SubjectName, cellFont, textBrush, new Rectangle(cell.Left + 6, cell.Top + 7, cell.Width - 12, 24), ContentAlignment.MiddleCenter);
                                DrawText(g, entry.TeacherName, teacherFont, mutedBrush, new Rectangle(cell.Left + 6, cell.Top + 31, cell.Width - 12, Math.Max(14, cell.Height - 34)), ContentAlignment.TopCenter);
                            }
                        }
                    }
                    y += rowHeight;
                }

                DrawText(g, "Prepared by: ____________________", smallFont, mutedBrush, new Rectangle(bounds.Left, bounds.Bottom - 20, 260, 18), ContentAlignment.MiddleLeft);
                DrawText(g, "Approved by: ____________________", smallFont, mutedBrush, new Rectangle(bounds.Right - 280, bounds.Bottom - 20, 280, 18), ContentAlignment.MiddleRight);
            }

            e.HasMorePages = false;
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

        private void LoadSubjectsForSelectedClass()
        {
            _cmbSubject.Items.Clear();
            if (_cmbClassSelect.SelectedIndex < 0) return;

            foreach (var subject in SubjectCatalog.SubjectsForClass(_cmbClassSelect.Text))
            {
                _cmbSubject.Items.Add(subject);
            }

            if (_cmbSubject.Items.Count > 0)
            {
                _cmbSubject.SelectedIndex = 0;
            }
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
