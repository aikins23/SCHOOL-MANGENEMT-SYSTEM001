using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmEmpView : Form
    {
        private readonly EmployeeService _employeeService;
        private TextBox searchBox;
        private ComboBox departmentFilter;
        private DataGridView employeesGrid;
        private Label resultLabel;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color SidebarBackColor = Color.FromArgb(17, 35, 58);
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        public frmEmpView()
        {
            InitializeComponent();

            // Initialize modern architecture
            var repository = new EmployeeRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(repository);

            BuildModernEmployeeView();
            NavigationSidebar.AddTo(this);
        }

        private void BuildModernEmployeeView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Employees";
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
                Text = "Employees",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Search, filter, and open staff records",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 0, 0)
            };
            actions.Controls.Add(CreatePrimaryButton("Add Employee", () => new frmEmployee().Show()));
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () =>
            {
                Close();
                new frmDashboard().Show();
            }));

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
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

            departmentFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            departmentFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();

            filters.Controls.Add(searchBox, 0, 0);
            filters.Controls.Add(departmentFilter, 1, 0);
            filters.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadEmployees()), 2, 0);
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

            employeesGrid = new DataGridView
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
                EnableHeadersVisualStyles = false
            };
            UiTheme.StyleDataGrid(employeesGrid);
            employeesGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            employeesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            employeesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            employeesGrid.DefaultCellStyle.BackColor = SurfaceColor;
            employeesGrid.DefaultCellStyle.ForeColor = TextColor;
            employeesGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            employeesGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            employeesGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            employeesGrid.GridColor = BorderColor;
            employeesGrid.CellDoubleClick += EmployeesGrid_CellDoubleClick;

            shell.Controls.Add(employeesGrid);
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
                Width = 112,
                Height = 36,
                Margin = new Padding(8, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private async void LoadDepartments()
        {
            departmentFilter.Items.Clear();
            departmentFilter.Items.Add("All departments");
            
            var departments = await _employeeService.GetDepartmentsAsync();
            foreach (var dept in departments)
            {
                departmentFilter.Items.Add(dept);
            }

            departmentFilter.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task LoadEmployees(string filterId = null, string filterDept = null)
        {
            try
            {
                resultLabel.Text = "Loading staff...";
                DataTable table = await _employeeService.GetEmployeesTableAsync(filterId, filterDept);
                
                employeesGrid.DataSource = table;
                resultLabel.Text = table.Rows.Count + " employee record(s)";

                if (employeesGrid.Columns["PICTURE"] is DataGridViewImageColumn imageCol)
                {
                    imageCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                }

                if (employeesGrid.Columns["ID"] != null)
                {
                    employeesGrid.Columns["ID"].FillWeight = 45;
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("An error occurred while loading employees: " + ex.Message, "Employees");
            }
        }

        private async void ApplyFilters()
        {
            if (employeesGrid == null || departmentFilter == null || searchBox == null || departmentFilter.Items.Count == 0)
            {
                return;
            }

            string filterId = null;
            string filterDept = null;

            if (!string.IsNullOrWhiteSpace(searchBox.Text))
            {
                if (int.TryParse(searchBox.Text.Trim(), out _))
                {
                    filterId = searchBox.Text.Trim();
                }
                else
                {
                    employeesGrid.DataSource = null;
                    resultLabel.Text = "Enter a numeric employee ID";
                    return;
                }
            }

            if (departmentFilter.SelectedIndex > 0)
            {
                filterDept = departmentFilter.Text;
            }

            await LoadEmployees(filterId, filterDept);
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            if (departmentFilter.Items.Count > 0)
            {
                departmentFilter.SelectedIndex = 0;
            }
            ApplyFilters();
        }

        private async void OpenSelectedEmployee()
        {
            if (employeesGrid.CurrentRow == null || employeesGrid.CurrentRow.Cells["ID"].Value == null)
            {
                return;
            }

            string id = employeesGrid.CurrentRow.Cells["ID"].Value.ToString();
            DataTable employeeTable = await _employeeService.GetEmployeesTableAsync(id);

            using (frmEmpDetails detailViewForm = new frmEmpDetails(employeeTable))
            {
                detailViewForm.ShowDialog();
            }
        }

        private void EmployeesGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                OpenSelectedEmployee();
            }
        }

        private async void frmEmpView_Load(object sender, EventArgs e)
        {
            LoadDepartments();
            await LoadEmployees();
        }

        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            OpenSelectedEmployee();
        }

        private void gunaPictureBox6_Click(object sender, EventArgs e)
        {
            Close();
            new frmDashboard().Show();
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
        private void menuStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
        private void aPPLICATIONToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { }
        private void gunaButton1_Click(object sender, EventArgs e) { new frmEmployee().Show(); }
        private void gunaButton2_Click(object sender, EventArgs e) { }
        private async void gunaButton2_Click_1(object sender, EventArgs e) { await LoadEmployees(); }
    }
}
