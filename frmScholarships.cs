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
    public partial class frmScholarships : Form
    {
        private readonly ScholarshipRepository _repo;
        private readonly StudentRepository _studentRepo;

        private TabControl _tabs;
        private Guna2DataGridView _categoryGrid, _assignmentGrid;
        private Guna2TextBox _txtCategoryName, _txtDiscountValue;
        private Guna2ComboBox _cmbDiscountType, _cmbStudentSelect, _cmbCategorySelect;

        public frmScholarships()
        {
            InitializeComponent();
            this.Icon = Branding.AppIcon;
            _repo = new ScholarshipRepository(AppConfig.ConnectionString);
            _studentRepo = new StudentRepository(AppConfig.ConnectionString);

            BuildUi();
            Load += async (s, e) => await InitializeFormAsync();
        }

        private void BuildUi()
        {
            Text = "Scholarships & Discounts";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Page;

            var titlePanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.Navy, Padding = new Padding(230, 0, 20, 0) };
            titlePanel.Controls.Add(new Label { Text = "SCHOLARSHIP HUB", ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft });
            Controls.Add(titlePanel);

            _tabs = new TabControl { Dock = DockStyle.Fill, Location = new Point(230, 70) };
            _tabs.Width = ClientSize.Width - 250;
            _tabs.Height = ClientSize.Height - 100;
            
            _tabs.TabPages.Add(CreateCategoriesTab());
            _tabs.TabPages.Add(CreateAssignmentsTab());

            Controls.Add(_tabs);
            NavigationSidebar.AddTo(this);
        }

        private TabPage CreateCategoriesTab()
        {
            var page = new TabPage("Manage Discount Categories");
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60 };
            
            _txtCategoryName = new Guna2TextBox { PlaceholderText = "Category Name (e.g. Sibling)", Width = 200 };
            _cmbDiscountType = new Guna2ComboBox { Width = 150 };
            _cmbDiscountType.Items.AddRange(new[] { "PERCENTAGE", "FIXED" });
            _cmbDiscountType.SelectedIndex = 0;
            _txtDiscountValue = new Guna2TextBox { PlaceholderText = "Value", Width = 100 };

            var btnAdd = new Guna2Button { Text = "Add Category", Width = 150, FillColor = UiTheme.Navy };
            btnAdd.Click += async (s, e) => await SaveCategoryAsync();

            top.Controls.Add(new Label { Text = "Name:", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
            top.Controls.Add(_txtCategoryName);
            top.Controls.Add(new Label { Text = "Type:", AutoSize = true, Margin = new Padding(10, 10, 5, 0) });
            top.Controls.Add(_cmbDiscountType);
            top.Controls.Add(new Label { Text = "Value:", AutoSize = true, Margin = new Padding(10, 10, 5, 0) });
            top.Controls.Add(_txtDiscountValue);
            top.Controls.Add(btnAdd);

            _categoryGrid = new Guna2DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, ReadOnly = true };
            UiTheme.StyleDataGrid(_categoryGrid);

            main.Controls.Add(_categoryGrid);
            main.Controls.Add(top);
            page.Controls.Add(main);
            return page;
        }

        private TabPage CreateAssignmentsTab()
        {
            var page = new TabPage("Assign to Students");
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60 };
            
            _cmbStudentSelect = new Guna2ComboBox { Width = 250 };
            _cmbCategorySelect = new Guna2ComboBox { Width = 200 };

            var btnAssign = new Guna2Button { Text = "Assign", Width = 120, FillColor = UiTheme.Success };
            btnAssign.Click += async (s, e) => await AssignScholarshipAsync();
            
            var btnRemove = new Guna2Button { Text = "Remove Selected", Width = 160, FillColor = Color.Crimson };
            btnRemove.Click += async (s, e) => await RemoveScholarshipAsync();

            top.Controls.Add(new Label { Text = "Student:", AutoSize = true, Margin = new Padding(0, 10, 5, 0) });
            top.Controls.Add(_cmbStudentSelect);
            top.Controls.Add(new Label { Text = "Category:", AutoSize = true, Margin = new Padding(10, 10, 5, 0) });
            top.Controls.Add(_cmbCategorySelect);
            top.Controls.Add(btnAssign);
            top.Controls.Add(btnRemove);

            if (AuthService.CurrentUser.Role == AuthService.UserRole.Director)
            {
                var btnApprove = new Guna2Button { Text = "Approve", Width = 120, FillColor = Color.SeaGreen };
                btnApprove.Click += async (s, e) => await SetApprovalStatusAsync("Approved");
                
                var btnReject = new Guna2Button { Text = "Reject", Width = 120, FillColor = Color.OrangeRed };
                btnReject.Click += async (s, e) => await SetApprovalStatusAsync("Rejected");

                top.Controls.Add(btnApprove);
                top.Controls.Add(btnReject);
            }

            _assignmentGrid = new Guna2DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, ReadOnly = true };
            UiTheme.StyleDataGrid(_assignmentGrid);

            main.Controls.Add(_assignmentGrid);
            main.Controls.Add(top);
            page.Controls.Add(main);
            return page;
        }

        private async Task InitializeFormAsync()
        {
            await RefreshCategoriesAsync();
            await RefreshAssignmentsAsync();

            // Load students for assignment dropdown
            var studentsTable = await _studentRepo.GetAsTableAsync();
            foreach (DataRow row in studentsTable.Rows)
            {
                string id = row["ID"].ToString();
                string name = row["FIRST NAME"] + " " + row["LAST NAME"];
                _cmbStudentSelect.Items.Add(new { ID = id, Display = $"{id} - {name}" });
            }
            _cmbStudentSelect.DisplayMember = "Display";
            _cmbStudentSelect.ValueMember = "ID";
        }

        private async Task RefreshCategoriesAsync()
        {
            var categories = await _repo.GetCategoriesAsync();
            _categoryGrid.DataSource = categories.Select(c => new { c.CategoryID, c.Name, Type = c.DiscountType, Value = c.DiscountValue, c.IsActive }).ToList();

            _cmbCategorySelect.Items.Clear();
            foreach (var c in categories.Where(x => x.IsActive))
            {
                _cmbCategorySelect.Items.Add(new { ID = c.CategoryID, Display = $"{c.Name} ({c.DiscountValue}{(c.DiscountType == "PERCENTAGE" ? "%" : " GHS")})" });
            }
            _cmbCategorySelect.DisplayMember = "Display";
            _cmbCategorySelect.ValueMember = "ID";
        }

        private async Task RefreshAssignmentsAsync()
        {
            var assignments = await _repo.GetStudentScholarshipsAsync();
            _assignmentGrid.DataSource = assignments.Select(a => new { a.AssignmentID, a.StudentID, a.StudentName, a.CategoryName, a.DiscountType, a.DiscountValue, Status = a.ApprovalStatus, Date = a.AssignedDate.ToShortDateString() }).ToList();
        }

        private async Task SetApprovalStatusAsync(string status)
        {
            if (_assignmentGrid.CurrentRow == null) return;
            
            int assignmentId = (int)_assignmentGrid.CurrentRow.Cells["AssignmentID"].Value;
            if (await _repo.UpdateScholarshipStatusAsync(assignmentId, status))
            {
                await RefreshAssignmentsAsync();
                UIHelper.ShowSuccess($"Scholarship marked as {status}.");
            }
        }

        private async Task SaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(_txtCategoryName.Text) || string.IsNullOrWhiteSpace(_txtDiscountValue.Text))
            {
                UIHelper.ShowWarning("Please enter name and discount value.");
                return;
            }

            if (!decimal.TryParse(_txtDiscountValue.Text, out decimal val))
            {
                UIHelper.ShowError("Invalid discount value.");
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
                UIHelper.ShowSuccess("Category saved.");
            }
        }

        private async Task AssignScholarshipAsync()
        {
            if (_cmbStudentSelect.SelectedItem == null || _cmbCategorySelect.SelectedItem == null)
            {
                UIHelper.ShowWarning("Select a student and category.");
                return;
            }

            dynamic student = _cmbStudentSelect.SelectedItem;
            dynamic category = _cmbCategorySelect.SelectedItem;

            if (await _repo.AssignScholarshipAsync(student.ID, category.ID))
            {
                await RefreshAssignmentsAsync();
                UIHelper.ShowSuccess("Scholarship assigned successfully.");
            }
        }

        private async Task RemoveScholarshipAsync()
        {
            if (_assignmentGrid.CurrentRow == null) return;
            
            int assignmentId = (int)_assignmentGrid.CurrentRow.Cells["AssignmentID"].Value;
            string student = _assignmentGrid.CurrentRow.Cells["StudentName"].Value.ToString();

            if (ConfirmationHelper.ConfirmDelete("Scholarship", student))
            {
                if (await _repo.RemoveScholarshipAsync(assignmentId))
                {
                    await RefreshAssignmentsAsync();
                    UIHelper.ShowSuccess("Scholarship removed.");
                }
            }
        }
    }
}
