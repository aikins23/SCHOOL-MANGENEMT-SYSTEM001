using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmClassManager : Form
    {
        private readonly IClassRepository _classRepo;
        private readonly ISubjectRepository _subjectRepo;
        private readonly EmployeeService _employeeService;

        private Guna2DataGridView _classGrid;
        private Guna2DataGridView _subjectGrid;
        private Guna2ComboBox _teacherCombo;
        private Guna2TextBox _classNameTxt;
        private Guna2TextBox _tuitionFeeTxt;
        private Guna2NumericUpDown _promotionLevelNum;
        private Label _selectedClassLabel;
        private Label _teacherSummaryLabel;
        private Label _subjectSummaryLabel;
        private List<Employee> _teachers = new List<Employee>();
        private string _selectedClass = null;
        private bool _loadingSubjects;
        private bool _subjectsDirty;

        public frmClassManager()
        {
            InitializeComponent();
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;
            _classRepo = new ClassRepository(AppConfig.ConnectionString);
            _subjectRepo = new SubjectRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(new EmployeeRepository(AppConfig.ConnectionString));

            SetupRobustUI();
            _ = LoadDataAsync();
            Activated += async (s, e) => await RefreshActiveClassAsync();
        }

        private void SetupRobustUI()
        {
            this.Text = "Academic Structure & Class Manager";
            this.Size = new Size(1220, 820);
            this.MinimumSize = new Size(1060, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = UiTheme.Page;
            this.Font = new Font("Segoe UI", 9.5F);
            this.Padding = new Padding(14);

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330F));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.Controls.Add(shell);

            var sidebar = CreateCardPanel();
            sidebar.Margin = new Padding(0, 0, 16, 0);
            sidebar.Padding = new Padding(0);
            shell.Controls.Add(sidebar, 0, 0);

            sidebar.Controls.Add(CreatePanelHeader("AVAILABLE CLASSES", UiTheme.Navy, "Select a class to manage fees, teacher and subjects."));

            _classGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _classGrid.Columns.Add("ClassName", "Class");
            _classGrid.Columns.Add("PromotionLevel", "Order");
            _classGrid.Columns["PromotionLevel"].Width = 74;
            _classGrid.Columns["PromotionLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _classGrid.SelectionChanged += OnClassSelected;
            StyleManagementGrid(_classGrid, true);
            sidebar.Controls.Add(_classGrid);

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            shell.Controls.Add(main, 1, 0);

            var pageHeader = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };
            pageHeader.Controls.Add(new Label
            {
                Text = "Academic Structure",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                Location = new Point(0, 8),
                AutoSize = true
            });
            pageHeader.Controls.Add(new Label
            {
                Text = "Manage class fees, class teacher assignment, and curriculum subjects from one screen.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = UiTheme.Muted,
                Location = new Point(2, 48),
                AutoSize = true
            });
            main.Controls.Add(pageHeader, 0, 0);

            var topTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            topTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            main.Controls.Add(topTable, 0, 1);

            var classSettings = CreateCard("Class Settings");
            classSettings.Margin = new Padding(0, 0, 8, 16);
            var classBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(24, 12, 24, 20)
            };
            classBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            classBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            classBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
            classBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            classBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            classBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            _selectedClassLabel = new Label
            {
                Text = "Select a class or enter a new class name.",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };
            _classNameTxt = CreateInput("Class Name (e.g. BASIC 1)");
            _tuitionFeeTxt = CreateInput("Tuition Fee");
            _promotionLevelNum = new Guna2NumericUpDown
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 12, 6),
                Minimum = 1,
                Maximum = 20,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text
            };

            var btnSaveClass = CreateButton("Save Class", UiTheme.Navy, Color.White);
            btnSaveClass.Click += async (s, e) => await SaveClassAsync();
            classBody.Controls.Add(_selectedClassLabel, 0, 0);
            classBody.SetColumnSpan(_selectedClassLabel, 3);
            classBody.Controls.Add(_classNameTxt, 0, 1);
            classBody.SetColumnSpan(_classNameTxt, 3);
            classBody.Controls.Add(_tuitionFeeTxt, 0, 2);
            classBody.Controls.Add(_promotionLevelNum, 1, 2);
            classBody.Controls.Add(btnSaveClass, 2, 2);
            classSettings.Controls.Add(classBody);
            classBody.BringToFront();
            topTable.Controls.Add(classSettings, 0, 0);

            var teacherCard = CreateCard("Class Teacher");
            teacherCard.Margin = new Padding(8, 0, 0, 16);
            var teacherBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(24, 14, 24, 18)
            };
            teacherBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            teacherBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            teacherBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            teacherBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            teacherBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            _teacherSummaryLabel = new Label
            {
                Text = "Choose a class first",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };
            _teacherCombo = new Guna2ComboBox
            {
                Dock = DockStyle.Fill,
                Height = 40,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text,
                ItemHeight = 32
            };
            var btnAssign = CreateButton("Update Assignment", UiTheme.Gold, Color.FromArgb(42, 36, 0));
            btnAssign.Click += async (s, e) => await AssignTeacherAsync();
            teacherBody.Controls.Add(_teacherSummaryLabel, 0, 0);
            teacherBody.SetColumnSpan(_teacherSummaryLabel, 2);
            teacherBody.Controls.Add(_teacherCombo, 0, 1);
            teacherBody.SetColumnSpan(_teacherCombo, 2);
            teacherBody.Controls.Add(btnAssign, 0, 2);
            teacherCard.Controls.Add(teacherBody);
            teacherBody.BringToFront();
            topTable.Controls.Add(teacherCard, 1, 0);

            var curriculumCard = CreateCard("Curriculum Subjects", "Add, edit or remove subjects for the selected class.");
            curriculumCard.Margin = new Padding(0);
            main.Controls.Add(curriculumCard, 0, 2);

            _subjectSummaryLabel = new Label
            {
                Text = "Select a class to load curriculum subjects.",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(24, 8, 24, 0),
                BackColor = Color.White
            };

            var curriculumBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24, 8, 24, 12)
            };

            _subjectGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _subjectGrid.Columns.Add("SubjectName", "Subject Name");
            StyleManagementGrid(_subjectGrid, true);
            _subjectGrid.CellValueChanged += (s, e) => MarkSubjectsDirty();
            _subjectGrid.UserAddedRow += (s, e) => MarkSubjectsDirty();
            _subjectGrid.UserDeletedRow += (s, e) => MarkSubjectsDirty();
            _subjectGrid.RowsRemoved += (s, e) => MarkSubjectsDirty();
            _subjectGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_subjectGrid.IsCurrentCellDirty)
                {
                    _subjectGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            curriculumBody.Controls.Add(_subjectGrid);

            var subButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 24, 8),
                BackColor = Color.White
            };
            var btnSaveSubs = CreateButton("Save Curriculum", UiTheme.Success, Color.White, 0, 0, 176);
            btnSaveSubs.Margin = Padding.Empty;
            btnSaveSubs.Click += async (s, e) => await SaveSubjectsAsync();
            subButtons.Controls.Add(btnSaveSubs);
            curriculumCard.Controls.Add(curriculumBody);
            curriculumCard.Controls.Add(subButtons);
            curriculumCard.Controls.Add(_subjectSummaryLabel);
        }

        private Guna2Panel CreateCard(string title)
        {
            var p = CreateCardPanel();
            p.Controls.Add(CreateSectionHeader(title, null));
            return p;
        }

        private Guna2Panel CreateCard(string title, string subtitle)
        {
            var p = CreateCardPanel();
            p.Controls.Add(CreateSectionHeader(title, subtitle));
            return p;
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

        private Guna2TextBox CreateInput(string hint, int x, int y, int width)
        {
            return new Guna2TextBox
            {
                PlaceholderText = hint,
                Location = new Point(x, y),
                Width = width,
                Height = 40,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text,
                FocusedState = { BorderColor = UiTheme.Gold }
            };
        }

        private Guna2TextBox CreateInput(string hint)
        {
            var input = CreateInput(hint, 0, 0, 120);
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 0, 12, 6);
            return input;
        }

        private Label CreateFieldLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 18),
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.Transparent
            };
        }

        private Guna2Button CreateButton(string text, Color fill, Color fore, int x, int y, int width)
        {
            return new Guna2Button
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                Height = 40,
                FillColor = fill,
                ForeColor = fore,
                BorderRadius = 4,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                HoverState = { FillColor = fill == UiTheme.Gold ? Color.FromArgb(232, 190, 0) : UiTheme.NavyHover }
            };
        }

        private Guna2Button CreateButton(string text, Color fill, Color fore)
        {
            var button = CreateButton(text, fill, fore, 0, 0, 120);
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(12, 0, 0, 6);
            return button;
        }

        private Panel CreatePanelHeader(string title, Color back, string subtitle)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = back, Padding = new Padding(24, 12, 18, 10) };
            p.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Location = new Point(24, 12),
                Size = new Size(360, 22),
                BackColor = Color.Transparent
            });
            p.Controls.Add(new Label
            {
                Text = subtitle,
                ForeColor = Color.FromArgb(204, 213, 235),
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(24, 36),
                Size = new Size(460, 22),
                BackColor = Color.Transparent
            });
            return p;
        }

        private Panel CreateCompactPanelHeader(string title, Color back)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = back, Padding = new Padding(24, 0, 18, 0) };
            p.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });
            return p;
        }

        private Panel CreateSectionHeader(string title, string subtitle)
        {
            var height = string.IsNullOrWhiteSpace(subtitle) ? 52 : 74;
            var p = new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = Color.White,
                Padding = new Padding(24, 0, 18, 0)
            };

            var accent = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = UiTheme.Gold
            };
            p.Controls.Add(accent);

            var titleLabel = new Label
            {
                Text = title.ToUpperInvariant(),
                ForeColor = UiTheme.Navy,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Location = new Point(24, 13),
                Size = new Size(420, 22),
                BackColor = Color.Transparent
            };
            p.Controls.Add(titleLabel);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                p.Controls.Add(new Label
                {
                    Text = subtitle,
                    ForeColor = UiTheme.Muted,
                    Font = new Font("Segoe UI", 8.5F),
                    Location = new Point(24, 38),
                    Size = new Size(520, 22),
                    BackColor = Color.Transparent
                });
            }

            return p;
        }

        private void StyleManagementGrid(DataGridView grid, bool fillColumns)
        {
            UiTheme.StyleDataGrid(grid, fillColumns);
            grid.ColumnHeadersHeight = 42;
            grid.RowTemplate.Height = 38;
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 251);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await _classRepo.EnsureTableExistsAsync();
                await _subjectRepo.EnsureTableAsync();

                _teachers = (await _employeeService.GetAllEmployeesAsync()).ToList();
                _teacherCombo.Items.Clear();
                _teacherCombo.Items.Add("-- No Teacher Assigned --");
                foreach (var t in _teachers) _teacherCombo.Items.Add($"{t.FullName} ({t.EmployeeID})");
                _teacherCombo.SelectedIndex = 0;

                await RefreshClassGridAsync();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("LoadDataAsync failed", ex);
            }
        }

        private async Task RefreshClassGridAsync()
        {
            _classGrid.Rows.Clear();
            var classes = await _classRepo.GetAllClassesTableAsync();
            foreach (DataRow row in classes.Rows)
            {
                _classGrid.Rows.Add(row["ClassName"], row["PromotionLevel"]);
            }
            if (_classGrid.Rows.Count > 0 && _classGrid.SelectedRows.Count == 0)
            {
                _classGrid.Rows[0].Selected = true;
            }
        }

        private async void OnClassSelected(object sender, EventArgs e)
        {
            if (_classGrid.SelectedRows.Count == 0) return;
            _selectedClass = _classGrid.SelectedRows[0].Cells["ClassName"].Value.ToString();
            _selectedClassLabel.Text = "Editing " + _selectedClass;
            _subjectSummaryLabel.Text = "Subjects assigned to " + _selectedClass;

            // Populate Form
            var config = await _classRepo.GetByClassNameAsync(_selectedClass);
            if (config != null)
            {
                _classNameTxt.Text = config.ClassName;
                _tuitionFeeTxt.Text = config.TuitionFee.ToString("N2");
                _promotionLevelNum.Value = config.PromotionLevel;
            }

            // Populate Teacher
            var assignments = await _classRepo.GetAllDetailedAssignmentsAsync();
            var current = assignments.FirstOrDefault(a => a.ClassName == _selectedClass);
            if (current != null && current.ClassTeacherID.HasValue)
            {
                var idx = _teachers.FindIndex(t => t.EmployeeID == current.ClassTeacherID.Value.ToString());
                _teacherCombo.SelectedIndex = idx + 1; // +1 because of "Unassigned"
                _teacherSummaryLabel.Text = "Current teacher: " + current.ClassTeacherName;
            }
            else
            {
                _teacherCombo.SelectedIndex = 0;
                _teacherSummaryLabel.Text = "No teacher assigned";
            }

            // Populate Subjects
            await RefreshSubjectGridAsync();
        }

        private async Task RefreshSubjectGridAsync()
        {
            _loadingSubjects = true;
            _subjectGrid.Rows.Clear();
            try
            {
                if (string.IsNullOrEmpty(_selectedClass)) return;
                var subjects = await _subjectRepo.GetSubjectsForClassAsync(_selectedClass);
                foreach (var s in subjects) _subjectGrid.Rows.Add(s);
                _subjectSummaryLabel.Text = subjects.Any()
                    ? subjects.Count() + " subjects assigned to " + _selectedClass
                    : "No subjects added yet for " + _selectedClass;
                _subjectsDirty = false;
            }
            finally
            {
                _loadingSubjects = false;
            }
        }

        private async Task AssignTeacherAsync()
        {
            if (string.IsNullOrEmpty(_selectedClass)) return;
            int? empId = null;
            if (_teacherCombo.SelectedIndex > 0)
            {
                if (int.TryParse(_teachers[_teacherCombo.SelectedIndex - 1].EmployeeID, out int id))
                    empId = id;
            }

            if (await _classRepo.AssignTeacherToClassAsync(_selectedClass, empId))
            {
                ConfirmationHelper.ShowInfo("Teacher assigned successfully");
                await RefreshClassGridAsync();
            }
        }

        private async Task SaveClassAsync()
        {
            string className = _classNameTxt.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(className))
            {
                UIHelper.ShowError("Enter the class name before saving.", "Class Manager");
                return;
            }

            if (!decimal.TryParse(_tuitionFeeTxt.Text, out decimal fee))
            {
                UIHelper.ShowError("Enter a valid tuition fee.", "Class Manager");
                return;
            }

            var config = new ClassConfig
            {
                ClassName = className,
                TuitionFee = fee,
                PromotionLevel = (int)_promotionLevelNum.Value
            };

            string originalClassName = string.IsNullOrWhiteSpace(_selectedClass) ? null : _selectedClass;
            var result = await _classRepo.SaveClassAsync(config, originalClassName);
            if (result)
            {
                UIHelper.ShowSuccess("Class saved successfully");
                _selectedClass = config.ClassName;
                await RefreshClassGridAsync();
                SelectClassInGrid(config.ClassName);
            }
            else
            {
                UIHelper.ShowError("Failed to save class. Check that the class name is not already used.", "Class Manager");
            }
        }

        private void SelectClassInGrid(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            foreach (DataGridViewRow row in _classGrid.Rows)
            {
                if (string.Equals(row.Cells["ClassName"].Value?.ToString(), className, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    _classGrid.CurrentCell = row.Cells["ClassName"];
                    break;
                }
            }
        }

        private async Task SaveSubjectsAsync()
        {
            if (string.IsNullOrEmpty(_selectedClass)) return;
            _subjectGrid.EndEdit();

            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _subjectGrid.Rows)
            {
                if (row.IsNewRow) continue;
                var val = row.Cells[0].Value?.ToString();
                if (string.IsNullOrWhiteSpace(val)) continue;

                var subject = val.Trim();
                if (seen.Add(subject))
                {
                    list.Add(subject);
                }
            }

            if (list.Count == 0)
            {
                UIHelper.ShowWarning("Add at least one subject before saving.", "Class Manager");
                return;
            }

            await _subjectRepo.SetSubjectsForClassAsync(_selectedClass, list);
            SubjectCatalog.Refresh();
            _subjectsDirty = false;
            await RefreshSubjectGridAsync();
            UIHelper.ShowSuccess("Subjects updated successfully");
        }

        private void MarkSubjectsDirty()
        {
            if (_loadingSubjects) return;
            _subjectsDirty = true;
        }

        private async Task RefreshActiveClassAsync()
        {
            if (string.IsNullOrWhiteSpace(_selectedClass) || _subjectsDirty) return;

            try
            {
                SubjectCatalog.Refresh();
                await RefreshSubjectGridAsync();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("RefreshActiveClassAsync failed", ex);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1150, 800);
            this.Name = "frmClassManager";
            this.ResumeLayout(false);
        }
    }
}
