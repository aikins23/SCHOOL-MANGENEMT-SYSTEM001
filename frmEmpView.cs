using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmEmpView : Form
    {
        private readonly kum Aikins = new kum();
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

        private const string EmployeeListQuery = @"
SELECT 
    employmentID AS [ID],
    CONCAT('EMP', RIGHT('0000' + CAST(employmentID AS VARCHAR), 4)) AS [EMPLOYEE ID],
    fullName AS [FULL NAME],
    dOB AS [DATE OF BIRTH],
    gender AS [GENDER],
    conatct AS [CONTACT],
    department AS [DEPARTMENT],
    position AS [POSITION],
    homeTown AS [HOME TOWN],
    residence AS [RESIDENCE],
    date_of_Emplyment AS [DATE OF EMPLOYMENT],
    employment_Mode AS [EMPLOYMENT MODE],
    employment_Status AS [EMPLOYMENT STATUS],
    emergency_Contact_Person AS [EMERGENCY CONTACT PERSON],
    emergency_contact AS [EMERGENCY CONTACT],
    employees_Reviews AS [EMPLOYEE REVIEWS],
    salary AS [SALARY],
    pic AS [PICTURE]
FROM [dbo].[Employee]";

        public frmEmpView()
        {
            InitializeComponent();
            BuildModernEmployeeView();
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
            filters.Controls.Add(CreateSecondaryButton("Refresh", LoadEmployees), 2, 0);
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
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

        private void LoadDepartments()
        {
            departmentFilter.Items.Clear();
            departmentFilter.Items.Add("All departments");

            using (OleDbConnection connection = new OleDbConnection(Aikins.constr))
            using (OleDbCommand command = new OleDbCommand("SELECT DISTINCT department FROM Employee WHERE department IS NOT NULL ORDER BY department", connection))
            {
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        departmentFilter.Items.Add(reader["department"].ToString());
                    }
                }
            }

            departmentFilter.SelectedIndex = 0;
        }

        private void LoadEmployees()
        {
            searchBox.Text = "";
            if (departmentFilter.Items.Count == 0)
            {
                LoadDepartments();
                return;
            }

            departmentFilter.SelectedIndex = 0;
            LoadEmployees(EmployeeListQuery);
        }

        private void ApplyFilters()
        {
            if (employeesGrid == null || departmentFilter == null || searchBox == null || departmentFilter.Items.Count == 0)
            {
                return;
            }

            string query = EmployeeListQuery + " WHERE 1 = 1";
            var parameters = new System.Collections.Generic.List<OleDbParameter>();

            if (int.TryParse(searchBox.Text.Trim(), out int employeeId))
            {
                query += " AND employmentID = ?";
                parameters.Add(new OleDbParameter("@employmentID", OleDbType.Integer) { Value = employeeId });
            }
            else if (!string.IsNullOrWhiteSpace(searchBox.Text))
            {
                employeesGrid.DataSource = null;
                resultLabel.Text = "Enter a numeric employee ID";
                return;
            }

            if (departmentFilter.SelectedIndex > 0)
            {
                query += " AND department = ?";
                parameters.Add(new OleDbParameter("@department", OleDbType.VarChar) { Value = departmentFilter.Text });
            }

            LoadEmployees(query, parameters.ToArray());
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            if (departmentFilter.Items.Count > 0)
            {
                departmentFilter.SelectedIndex = 0;
            }
            LoadEmployees(EmployeeListQuery);
        }

        private void LoadEmployees(string query, params OleDbParameter[] parameters)
        {
            try
            {
                using (OleDbConnection connection = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand(query, connection))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                {
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    DataTable table = new DataTable();
                    adapter.Fill(table);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable GetDataFromDatabase(int id)
        {
            DataTable dataTable = new DataTable();

            using (OleDbConnection connection = new OleDbConnection(Aikins.constr))
            using (OleDbCommand command = new OleDbCommand("SELECT * FROM Employee WHERE employmentID = ?", connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                command.Parameters.AddWithValue("?", id);
                adapter.Fill(dataTable);
            }

            return dataTable;
        }

        private void OpenSelectedEmployee()
        {
            if (employeesGrid.CurrentRow == null || employeesGrid.CurrentRow.Cells["ID"].Value == null)
            {
                return;
            }

            int id = Convert.ToInt32(employeesGrid.CurrentRow.Cells["ID"].Value);
            using (frmEmpDetails detailViewForm = new frmEmpDetails(GetDataFromDatabase(id)))
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

        private void frmEmpView_Load(object sender, EventArgs e)
        {
            LoadDepartments();
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
        private void gunaButton2_Click_1(object sender, EventArgs e) { LoadEmployees(); }
    }
}
