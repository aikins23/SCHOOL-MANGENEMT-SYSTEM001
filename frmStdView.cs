using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmStdView : Form
    {
        private readonly kum Aikins = new kum();
        private TextBox searchBox;
        private ComboBox classFilter;
        private DataGridView studentsGrid;
        private Label resultLabel;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color SidebarBackColor = Color.FromArgb(17, 35, 58);
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        private const string StudentListQuery = @"
SELECT 
    StudentID AS [ID],
    CONCAT('KPS', RIGHT('000' + CAST(StudentID AS VARCHAR), 3)) AS [UNIQUE ID],
    FirstName AS [FIRST NAME],
    LastName AS [LAST NAME],
    DOB AS [DATE OF BIRTH],
    Gender AS [GENDER],
    Email AS [EMAIL],
    ClassID AS [CLASS ID],
    HomeTown AS [HOME TOWN],
    Residence AS [RESIDENCE],
    Allegies AS [ALLERGIES],
    EmergencyConatct AS [EMERGENCY CONTACT],
    GuidanceName AS [GUARDIAN NAME],
    GuidianceEmail AS [GUARDIAN EMAIL],
    Guidiance_Location AS [GUARDIAN LOCATION],
    admission_date AS [ADMISSION DATE],
    Std_pic AS [STUDENT PIC]
FROM Students";

        public frmStdView()
        {
            InitializeComponent();
            BuildModernStudentView();
        }

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
                BackColor = PageBackColor,
                Padding = new Padding(0, 12, 0, 0)
            };
            actions.Controls.Add(CreatePrimaryButton("Add Student", () => new frmAddStd().Show()));
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

            classFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            classFilter.SelectedIndexChanged += (sender, args) => ApplyFilters();

            filters.Controls.Add(searchBox, 0, 0);
            filters.Controls.Add(classFilter, 1, 0);
            filters.Controls.Add(CreateSecondaryButton("Refresh", LoadStudents), 2, 0);
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            studentsGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            studentsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            studentsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            studentsGrid.DefaultCellStyle.BackColor = SurfaceColor;
            studentsGrid.DefaultCellStyle.ForeColor = TextColor;
            studentsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            studentsGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            studentsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            studentsGrid.GridColor = BorderColor;
            studentsGrid.CellDoubleClick += StudentsGrid_CellDoubleClick;

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
                Width = 110,
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

        private void LoadClasses()
        {
            classFilter.Items.Clear();
            classFilter.Items.Add("All classes");

            using (OleDbConnection connection = new OleDbConnection(Aikins.constr))
            using (OleDbCommand command = new OleDbCommand("SELECT Class_name FROM rooms ORDER BY ClassID", connection))
            {
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        classFilter.Items.Add(reader["Class_name"].ToString());
                    }
                }
            }

            classFilter.SelectedIndex = 0;
        }

        private void LoadStudents()
        {
            searchBox.Text = "";
            if (classFilter.Items.Count == 0)
            {
                LoadClasses();
                return;
            }

            classFilter.SelectedIndex = 0;
            LoadStudents(StudentListQuery);
        }

        private void ApplyFilters()
        {
            if (studentsGrid == null || classFilter == null || searchBox == null || classFilter.Items.Count == 0)
            {
                return;
            }

            string query = StudentListQuery + " WHERE 1 = 1";
            var parameters = new System.Collections.Generic.List<OleDbParameter>();

            if (int.TryParse(searchBox.Text.Trim(), out int studentId))
            {
                query += " AND StudentID = ?";
                parameters.Add(new OleDbParameter("@StudentID", OleDbType.Integer) { Value = studentId });
            }
            else if (!string.IsNullOrWhiteSpace(searchBox.Text))
            {
                studentsGrid.DataSource = null;
                resultLabel.Text = "Enter a numeric student ID";
                return;
            }

            if (classFilter.SelectedIndex > 0)
            {
                query += " AND ClassID = ?";
                parameters.Add(new OleDbParameter("@ClassID", OleDbType.VarChar) { Value = classFilter.Text });
            }

            LoadStudents(query, parameters.ToArray());
        }

        private void ClearFilters()
        {
            searchBox.Text = "";
            if (classFilter.Items.Count > 0)
            {
                classFilter.SelectedIndex = 0;
            }
            LoadStudents(StudentListQuery);
        }

        private void LoadStudents(string query, params OleDbParameter[] parameters)
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
                    studentsGrid.DataSource = table;
                    resultLabel.Text = table.Rows.Count + " student record(s)";

                    if (studentsGrid.Columns["STUDENT PIC"] is DataGridViewImageColumn imageCol)
                    {
                        imageCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                    }

                    if (studentsGrid.Columns["ID"] != null)
                    {
                        studentsGrid.Columns["ID"].FillWeight = 45;
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
            using (OleDbCommand command = new OleDbCommand("SELECT * FROM Students WHERE StudentID = ?", connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                command.Parameters.AddWithValue("?", id);
                adapter.Fill(dataTable);
            }

            return dataTable;
        }

        private void OpenSelectedStudent()
        {
            if (studentsGrid.CurrentRow == null || studentsGrid.CurrentRow.Cells["ID"].Value == null)
            {
                return;
            }

            int id = Convert.ToInt32(studentsGrid.CurrentRow.Cells["ID"].Value);
            using (frmStdDetails detailViewForm = new frmStdDetails(GetDataFromDatabase(id)))
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

        private void frmStdView_Load(object sender, EventArgs e)
        {
            LoadClasses();
        }

        private void data_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            OpenSelectedStudent();
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
        private void gunaButton1_Click(object sender, EventArgs e) { }
        private void gunaButton1_Click_1(object sender, EventArgs e) { new frmAddStd().Show(); }
        private void gunaButton2_Click(object sender, EventArgs e) { LoadStudents(); }
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
