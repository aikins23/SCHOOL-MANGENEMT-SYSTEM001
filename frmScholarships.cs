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
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmScholarships : Form
    {
        private readonly ScholarshipRepository _repo;
        private readonly StudentRepository _studentRepo;

        private Panel _contentArea;
        private Panel _categoriesPanel;
        private Panel _assignmentsPanel;
        private Guna2Button _btnTabCategories;
        private Guna2Button _btnTabAssignments;

        private Guna2DataGridView _categoryGrid;
        private Guna2DataGridView _assignmentGrid;
        private Guna2TextBox _txtCategoryName;
        private Guna2TextBox _txtDiscountValue;
        private Guna2ComboBox _cmbDiscountType;
        private Guna2ComboBox _cmbStudentSelect;
        private Guna2ComboBox _cmbCategorySelect;
        private Label _lblAssignmentCount;

        private class ComboItem
        {
            public string ID { get; set; }
            public string Display { get; set; }
        }

        private class ComboItemInt
        {
            public int ID { get; set; }
            public string Display { get; set; }
        }

        public frmScholarships()
        {
            InitializeComponent();
            this.Icon = Branding.AppIcon;
            _repo = new ScholarshipRepository(AppConfig.ConnectionString);
            _studentRepo = new StudentRepository(AppConfig.ConnectionString);
            if (!AuthService.RequireAccess("frmScholarships", this)) return;

            BuildUi();
            Load += async (s, e) => await InitializeFormAsync();
        }

        private void BuildUi()
        {
            Text = $"Scholarships & Discounts — {AppConfig.ProductName}";
            Size = new Size(1240, 800);
            MinimumSize = new Size(1060, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Page;
            Font = new Font("Segoe UI", 9.5F);

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24, 16, 24, 20)
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));  // Header
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));  // View Switcher Tabs
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Main Cards Area
            Controls.Add(shell);

            // Add NavigationSidebar after adding shell to avoid WinForms docking overlap
            NavigationSidebar.AddTo(this);

            // 1. Page Header
            var pageHeader = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Page };
            pageHeader.Controls.Add(new Label
            {
                Text = "Scholarships & Discounts Hub",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                Location = new Point(0, 4),
                AutoSize = true
            });
            pageHeader.Controls.Add(new Label
            {
                Text = "Manage fee discount categories (sibling, merit, staff child) and award scholarships to students.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = UiTheme.Muted,
                Location = new Point(2, 42),
                AutoSize = true
            });
            shell.Controls.Add(pageHeader, 0, 0);

            // 2. View Switcher Bar
            var switcherPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 6),
                WrapContents = false
            };

            _btnTabCategories = CreateTabButton("📁  Discount Categories", true);
            _btnTabCategories.Width = 200;
            _btnTabCategories.Click += (s, e) => SwitchTab(true);

            _btnTabAssignments = CreateTabButton("🎓  Student Assignments", false);
            _btnTabAssignments.Width = 210;
            _btnTabAssignments.Click += (s, e) => SwitchTab(false);

            switcherPanel.Controls.Add(_btnTabCategories);
            switcherPanel.Controls.Add(_btnTabAssignments);
            shell.Controls.Add(switcherPanel, 0, 1);

            // 3. Content Area
            _contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                Padding = new Padding(0, 8, 0, 0)
            };
            shell.Controls.Add(_contentArea, 0, 2);

            _categoriesPanel = CreateCategoriesView();
            _assignmentsPanel = CreateAssignmentsView();

            _contentArea.Controls.Add(_categoriesPanel);
            _contentArea.Controls.Add(_assignmentsPanel);

            _categoriesPanel.BringToFront();
        }

        private Panel CreateCategoriesView()
        {
            var pnl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            pnl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Top Card: Create Category
            var topCard = CreateCardPanel();
            topCard.Margin = new Padding(0, 0, 0, 16);
            topCard.Controls.Add(CreateSectionHeader("Create Discount Category", "Add a new fee scholarship or discount rule (e.g. Sibling Discount, Merit Award)."));

            var formTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(24, 8, 24, 16)
            };
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            formTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            formTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            formTable.Controls.Add(CreateFieldLabel("Category Name (e.g. Sibling)"), 0, 0);
            formTable.Controls.Add(CreateFieldLabel("Discount Type"), 1, 0);
            formTable.Controls.Add(CreateFieldLabel("Value"), 2, 0);

            _txtCategoryName = CreateInput("Enter category name...");
            _cmbDiscountType = new Guna2ComboBox
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 16, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text
            };
            _cmbDiscountType.Items.AddRange(new[] { "PERCENTAGE", "FIXED" });
            _cmbDiscountType.SelectedIndex = 0;

            _txtDiscountValue = CreateInput("e.g. 10 or 500");

            var btnAdd = CreateActionBtn("+ Add Category", UiTheme.Navy, Color.White);
            btnAdd.Width = 160;
            btnAdd.Dock = DockStyle.Left;
            btnAdd.Enabled = AuthService.CanWrite("Finance.Scholarship.Manage");
            btnAdd.Click += async (s, e) => await SaveCategoryAsync();

            formTable.Controls.Add(_txtCategoryName, 0, 1);
            formTable.Controls.Add(_cmbDiscountType, 1, 1);
            formTable.Controls.Add(_txtDiscountValue, 2, 1);
            formTable.Controls.Add(btnAdd, 3, 1);

            topCard.Controls.Add(formTable);
            formTable.BringToFront();
            pnl.Controls.Add(topCard, 0, 0);

            // Bottom Card: Categories Grid
            var bottomCard = CreateCardPanel();
            bottomCard.Controls.Add(CreateSectionHeader("Discount Categories Catalog", "All active and inactive scholarship rules configured in the system."));

            var gridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 8, 16, 16) };
            _categoryGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            StyleManagementGrid(_categoryGrid);
            gridContainer.Controls.Add(_categoryGrid);

            bottomCard.Controls.Add(gridContainer);
            gridContainer.BringToFront();
            pnl.Controls.Add(bottomCard, 0, 1);

            return pnl;
        }

        private Panel CreateAssignmentsView()
        {
            var pnl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Page,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            pnl.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
            pnl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Top Card: Assign Scholarship
            var topCard = CreateCardPanel();
            topCard.Margin = new Padding(0, 0, 0, 16);
            topCard.Controls.Add(CreateSectionHeader("Award Scholarship or Discount", "Select a student and award a discount category from your active catalog."));

            var formTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(24, 8, 24, 16)
            };
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            formTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            formTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            formTable.Controls.Add(CreateFieldLabel("Select Student"), 0, 0);
            formTable.Controls.Add(CreateFieldLabel("Select Discount Category"), 1, 0);

            _cmbStudentSelect = new Guna2ComboBox
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 16, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text
            };

            _cmbCategorySelect = new Guna2ComboBox
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 16, 0),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text
            };

            var btnAssign = CreateActionBtn("✓  Award Scholarship", UiTheme.Success, Color.White);
            btnAssign.Width = 170;
            btnAssign.Dock = DockStyle.Left;
            btnAssign.Enabled = AuthService.CanWrite("Finance.Scholarship.Manage");
            btnAssign.Click += async (s, e) => await AssignScholarshipAsync();

            formTable.Controls.Add(_cmbStudentSelect, 0, 1);
            formTable.Controls.Add(_cmbCategorySelect, 1, 1);
            formTable.Controls.Add(btnAssign, 2, 1);

            topCard.Controls.Add(formTable);
            formTable.BringToFront();
            pnl.Controls.Add(topCard, 0, 0);

            // Bottom Card: Roster
            var bottomCard = CreateCardPanel();
            bottomCard.Controls.Add(CreateSectionHeader("Student Scholarship Roster", "Review assigned scholarships and manage approvals or removals."));

            var rosterContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 8, 16, 68) };
            _assignmentGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            StyleManagementGrid(_assignmentGrid);
            rosterContainer.Controls.Add(_assignmentGrid);

            var footerBar = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(24, 10, 24, 10)
            };
            footerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            footerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _lblAssignmentCount = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No student scholarships assigned.",
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var actionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0),
                BackColor = Color.White
            };

            var btnRemove = CreateActionBtn("✕  Remove", UiTheme.Danger, Color.White);
            btnRemove.Width = 120;
            btnRemove.Margin = new Padding(8, 0, 0, 0);
            btnRemove.Enabled = AuthService.CanWrite("Finance.Scholarship.Manage");
            btnRemove.Click += async (s, e) => await RemoveScholarshipAsync();
            actionsFlow.Controls.Add(btnRemove);

            if (AuthService.CanWrite("Finance.Scholarship.Approve"))
            {
                var btnReject = CreateActionBtn("✕  Reject", Color.FromArgb(225, 29, 72), Color.White);
                btnReject.Width = 110;
                btnReject.Margin = new Padding(8, 0, 0, 0);
                btnReject.Click += async (s, e) => await SetApprovalStatusAsync("Rejected");

                var btnApprove = CreateActionBtn("✓  Approve", Color.FromArgb(16, 185, 129), Color.White);
                btnApprove.Width = 120;
                btnApprove.Margin = new Padding(8, 0, 0, 0);
                btnApprove.Click += async (s, e) => await SetApprovalStatusAsync("Approved");

                actionsFlow.Controls.Add(btnReject);
                actionsFlow.Controls.Add(btnApprove);
            }

            footerBar.Controls.Add(_lblAssignmentCount, 0, 0);
            footerBar.Controls.Add(actionsFlow, 1, 0);

            bottomCard.Controls.Add(rosterContainer);
            bottomCard.Controls.Add(footerBar);
            rosterContainer.BringToFront();
            pnl.Controls.Add(bottomCard, 0, 1);

            return pnl;
        }

        private void SwitchTab(bool showCategories)
        {
            if (showCategories)
            {
                _categoriesPanel.BringToFront();
                HighlightTabButton(_btnTabCategories, true);
                HighlightTabButton(_btnTabAssignments, false);
            }
            else
            {
                _assignmentsPanel.BringToFront();
                HighlightTabButton(_btnTabCategories, false);
                HighlightTabButton(_btnTabAssignments, true);
            }
        }

        private Guna2Button CreateTabButton(string text, bool active)
        {
            var btn = new Guna2Button
            {
                Text = text,
                Height = 40,
                Margin = new Padding(0, 0, 12, 0),
                BorderRadius = 6,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            HighlightTabButton(btn, active);
            return btn;
        }

        private void HighlightTabButton(Guna2Button btn, bool active)
        {
            if (active)
            {
                btn.FillColor = UiTheme.Navy;
                btn.ForeColor = Color.White;
                btn.BorderThickness = 0;
            }
            else
            {
                btn.FillColor = Color.White;
                btn.ForeColor = UiTheme.Text;
                btn.BorderColor = UiTheme.Border;
                btn.BorderThickness = 1;
                btn.HoverState.FillColor = Color.FromArgb(246, 248, 251);
            }
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

        private Panel CreateSectionHeader(string title, string subtitle)
        {
            int height = string.IsNullOrWhiteSpace(subtitle) ? 46 : 64;
            var p = new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 18, 0)
            };

            var accent = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = UiTheme.Gold
            };
            p.Controls.Add(accent);

            var titleLabel = new Label
            {
                Text = title.ToUpperInvariant(),
                ForeColor = UiTheme.Navy,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Location = new Point(20, 10),
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
                    Location = new Point(20, 34),
                    Size = new Size(650, 20),
                    BackColor = Color.Transparent
                });
            }

            return p;
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.Muted,
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent
            };
        }

        private Guna2TextBox CreateInput(string hint)
        {
            return new Guna2TextBox
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 16, 0),
                PlaceholderText = hint,
                BorderColor = UiTheme.Border,
                BorderRadius = 4,
                FillColor = Color.White,
                ForeColor = UiTheme.Text,
                FocusedState = { BorderColor = UiTheme.Gold }
            };
        }

        private Guna2Button CreateActionBtn(string text, Color fill, Color fore)
        {
            return new Guna2Button
            {
                Text = text,
                Height = 40,
                FillColor = fill,
                ForeColor = fore,
                BorderRadius = 4,
                Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                HoverState = { FillColor = fill == UiTheme.Gold ? Color.FromArgb(232, 190, 0) : ControlPaint.Dark(fill, 0.08F) }
            };
        }

        private void StyleManagementGrid(DataGridView grid)
        {
            UiTheme.StyleDataGrid(grid, true);
            grid.ColumnHeadersHeight = 44;
            grid.RowTemplate.Height = 40;
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 251);
        }

        private async Task InitializeFormAsync()
        {
            await RefreshCategoriesAsync();
            await RefreshAssignmentsAsync();

            var studentsTable = await _studentRepo.GetAsTableAsync();
            _cmbStudentSelect.Items.Clear();
            foreach (DataRow row in studentsTable.Rows)
            {
                string id = row["ID"].ToString();
                string name = row["FIRST NAME"] + " " + row["LAST NAME"];
                _cmbStudentSelect.Items.Add(new ComboItem { ID = id, Display = $"{id} — {name}" });
            }
            _cmbStudentSelect.DisplayMember = "Display";
            _cmbStudentSelect.ValueMember = "ID";
        }

        private async Task RefreshCategoriesAsync()
        {
            var categories = await _repo.GetCategoriesAsync();
            _categoryGrid.DataSource = categories.Select(c => new
            {
                ID = c.CategoryID,
                Name = c.Name,
                Type = c.DiscountType,
                Value = c.DiscountValue.ToString("G") + (c.DiscountType == "PERCENTAGE" ? "%" : " GHS"),
                Status = c.IsActive ? "Active" : "Inactive"
            }).ToList();

            _cmbCategorySelect.Items.Clear();
            foreach (var c in categories.Where(x => x.IsActive))
            {
                _cmbCategorySelect.Items.Add(new ComboItemInt { ID = c.CategoryID, Display = $"{c.Name} ({c.DiscountValue}{(c.DiscountType == "PERCENTAGE" ? "%" : " GHS")})" });
            }
            _cmbCategorySelect.DisplayMember = "Display";
            _cmbCategorySelect.ValueMember = "ID";
        }

        private async Task RefreshAssignmentsAsync()
        {
            var assignments = await _repo.GetStudentScholarshipsAsync();
            _assignmentGrid.DataSource = assignments.Select(a => new
            {
                ID = a.AssignmentID,
                StudentID = a.StudentID,
                Student = a.StudentName,
                Category = a.CategoryName,
                Type = a.DiscountType,
                Value = a.DiscountValue.ToString("G") + (a.DiscountType == "PERCENTAGE" ? "%" : " GHS"),
                Status = a.ApprovalStatus,
                Date = a.AssignedDate.ToShortDateString()
            }).ToList();

            if (_lblAssignmentCount != null)
            {
                int count = assignments.Count;
                _lblAssignmentCount.Text = count == 0 ? "No student scholarships assigned." : $"Showing {count} assigned scholarship{(count == 1 ? "" : "s")}.";
            }
        }

        private async Task SetApprovalStatusAsync(string status)
        {
            if (!AuthService.RequireWriteAccess("Finance.Scholarship.Approve", $"{status} scholarship")) return;
            if (_assignmentGrid.CurrentRow == null)
            {
                UIHelper.ShowWarning("Please select an assignment row from the table first.");
                return;
            }

            int assignmentId = (int)_assignmentGrid.CurrentRow.Cells["ID"].Value;
            if (await _repo.UpdateScholarshipStatusAsync(assignmentId, status))
            {
                await RefreshAssignmentsAsync();
                UIHelper.ShowSuccess($"Scholarship marked as {status}.");
            }
        }

        private async Task SaveCategoryAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.Scholarship.Manage", "Save scholarship category")) return;
            if (string.IsNullOrWhiteSpace(_txtCategoryName.Text) || string.IsNullOrWhiteSpace(_txtDiscountValue.Text))
            {
                UIHelper.ShowWarning("Please enter both category name and discount value.");
                return;
            }

            if (!decimal.TryParse(_txtDiscountValue.Text, out decimal val))
            {
                UIHelper.ShowError("Invalid discount value entered.");
                return;
            }

            var category = new ScholarshipCategory
            {
                Name = _txtCategoryName.Text.Trim(),
                DiscountType = _cmbDiscountType.Text,
                DiscountValue = val,
                IsActive = true
            };

            if (await _repo.SaveCategoryAsync(category))
            {
                _txtCategoryName.Clear();
                _txtDiscountValue.Clear();
                await RefreshCategoriesAsync();
                UIHelper.ShowSuccess("Discount category saved successfully.");
            }
        }

        private async Task AssignScholarshipAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.Scholarship.Manage", "Assign scholarship")) return;
            if (_cmbStudentSelect.SelectedItem == null || _cmbCategorySelect.SelectedItem == null)
            {
                UIHelper.ShowWarning("Please select both a student and a discount category.");
                return;
            }

            var student = (ComboItem)_cmbStudentSelect.SelectedItem;
            var category = (ComboItemInt)_cmbCategorySelect.SelectedItem;

            if (await _repo.AssignScholarshipAsync(student.ID, category.ID))
            {
                await RefreshAssignmentsAsync();
                UIHelper.ShowSuccess("Scholarship assigned successfully.");
            }
        }

        private async Task RemoveScholarshipAsync()
        {
            if (!AuthService.RequireWriteAccess("Finance.Scholarship.Manage", "Remove scholarship")) return;
            if (_assignmentGrid.CurrentRow == null)
            {
                UIHelper.ShowWarning("Please select an assignment row from the table first.");
                return;
            }

            int assignmentId = (int)_assignmentGrid.CurrentRow.Cells["ID"].Value;
            string student = _assignmentGrid.CurrentRow.Cells["Student"].Value.ToString();

            if (ConfirmationHelper.ConfirmDelete("Scholarship", student))
            {
                if (await _repo.RemoveScholarshipAsync(assignmentId))
                {
                    await RefreshAssignmentsAsync();
                    UIHelper.ShowSuccess("Scholarship assignment removed.");
                }
            }
        }
    }
}
