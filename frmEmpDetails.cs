using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmEmpDetails : Form
    {
        private readonly EmployeeService _employeeService;
        private readonly DataTable data;
        private Label statusLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color Navy = UiTheme.Navy;
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        public frmEmpDetails() : this(new DataTable()) { }

        public frmEmpDetails(DataTable data)
        {
            InitializeComponent();
            this.data = data;
            
            // Initialize modern architecture
            var repository = new EmployeeRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(repository);

            BuildModernDetailsView();
        }

        private void BuildModernDetailsView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Employee Details";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 720);

            PrepareInputs();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = PageBackColor, Padding = new Padding(26) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFormBody(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private void PrepareInputs()
        {
            txtEMdID.Enabled = true;
            txtEMdID.ReadOnly = true;

            foreach (Control control in new Control[] { txtEMdID, txtFN, txtCN, txtHT, txtRD, empCN, empEC, empSA, cmbGN, cmbDPT, CmbPs, empMD, empST, empRV, dateDOB, empdate, DATE })
            {
                StyleInput(control);
            }

            txtEMdID.FillColor = UiTheme.SurfaceAlt;
            ResetCombo(cmbGN, AppConfig.GenderOptions);
            ResetCombo(cmbDPT, new object[] { "ADMINISTRATION", "SANITATION & CLEANING", "CRECHE", "NURSERY", "KINDERGARTEN", "LOWER PRIMARY", "UPPER PRIMARY", "JHS (JUNIOR HIGH SCHOOL)" });
            ResetCombo(CmbPs, new object[] { "NON-POSITIONAL", "HEAD", "DEPUTY", "SECRETARY" });
            ResetCombo(empMD, new object[] { "FULL-TIME", "PART-TIME", "CONTRACT" });
            ResetCombo(empST, new object[] { "ACTIVE", "IN-ACTIVE" });
            ResetCombo(empRV, new object[] { "A: EXCELLENT", "B: GOOD", "C: SATISFACTORY", "D: UNSATISFACTORY" });

            dateDOB.Value = DateTime.Today.AddYears(-25);
            empdate.Value = DateTime.Today;
            DATE.Value = DateTime.Today;
            std_pic.SizeMode = PictureBoxSizeMode.Zoom;
            std_pic.BackColor = UiTheme.SurfaceAlt;
            std_pic.BorderStyle = BorderStyle.None;
            upload.Text = "Change Photo";
            upload.FillColor = UiTheme.Gold;
            upload.ForeColor = TextColor;
            upload.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            upload.BorderColor = BorderColor;
            upload.BorderThickness = 1;
            upload.BorderRadius = 4;
        }

        private void StyleInput(Control control)
        {
            control.Font = new Font("Segoe UI", 10F);
            control.Margin = Padding.Empty;

            if (control is Guna.UI2.WinForms.Guna2TextBox textBox)
            {
                textBox.FillColor = SurfaceColor;
                textBox.BorderColor = BorderColor;
                textBox.FocusedState.BorderColor = UiTheme.Gold;
                textBox.BorderRadius = 4;
                textBox.BorderThickness = 1;
                textBox.ForeColor = TextColor;
                textBox.Height = 36;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2ComboBox comboBox)
            {
                comboBox.FillColor = SurfaceColor;
                comboBox.BorderColor = BorderColor;
                comboBox.FocusedState.BorderColor = UiTheme.Gold;
                comboBox.BorderRadius = 4;
                comboBox.ForeColor = TextColor;
                comboBox.ItemHeight = 30;
                comboBox.Height = 36;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2DateTimePicker datePicker)
            {
                datePicker.FillColor = SurfaceColor;
                datePicker.BorderColor = BorderColor;
                datePicker.BorderRadius = 4;
                datePicker.BorderThickness = 1;
                datePicker.ForeColor = TextColor;
                datePicker.Height = 36;
            }
        }

        private void ResetCombo(ComboBox combo, object[] items)
        {
            combo.Items.Clear();
            combo.Items.AddRange(items);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            var title = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 38, Text = "Employee Details", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Review, update, or terminate a staff record", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreatePrimaryButton("Employee List", () => { Close(); new frmEmpView().Show(); }));
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () => { Close(); new frmDashboard().Show(); }));
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildFormBody()
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = PageBackColor };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            var detailsStack = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = PageBackColor, Margin = new Padding(0, 0, 14, 0) };
            detailsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            detailsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            detailsStack.Controls.Add(BuildPersonalPanel(), 0, 0);
            detailsStack.Controls.Add(BuildEmploymentPanel(), 0, 1);
            body.Controls.Add(detailsStack, 0, 0);
            body.Controls.Add(BuildPhotoPanel(), 1, 0);
            return body;
        }

        private Control BuildPersonalPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), new Padding(0, 0, 0, 14));
            var layout = CreateSectionLayout("Personal Details", 5, 2);
            layout.Controls.Add(CreateField("Employee ID", txtEMdID), 0, 1);
            layout.Controls.Add(CreateField("Full Name", txtFN), 1, 1);
            layout.Controls.Add(CreateField("Gender", cmbGN), 0, 2);
            layout.Controls.Add(CreateField("Date of Birth", dateDOB), 1, 2);
            layout.Controls.Add(CreateField("Contact", txtCN), 0, 3);
            layout.Controls.Add(CreateField("Department", cmbDPT), 1, 3);
            layout.Controls.Add(CreateField("Home Town", txtHT), 0, 4);
            layout.Controls.Add(CreateField("Residence", txtRD), 1, 4);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildEmploymentPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = CreateSectionLayout("Employment & Emergency", 5, 2);
            layout.Controls.Add(CreateField("Position", CmbPs), 0, 1);
            layout.Controls.Add(CreateField("Employment Date", empdate), 1, 1);
            layout.Controls.Add(CreateField("Employment Mode", empMD), 0, 2);
            layout.Controls.Add(CreateField("Employment Status", empST), 1, 2);
            layout.Controls.Add(CreateField("Emergency Contact Person", empCN), 0, 3);
            layout.Controls.Add(CreateField("Emergency Contact", empEC), 1, 3);
            layout.Controls.Add(CreateField("Performance Review", empRV), 0, 4);
            layout.Controls.Add(CreateField("Salary", empSA), 1, 4);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildPhotoPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, BackColor = SurfaceColor };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Photo", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            std_pic.Dock = DockStyle.Fill;
            upload.Dock = DockStyle.Fill;
            upload.Click -= upload_Click;
            upload.Click += upload_Click;
            layout.Controls.Add(std_pic, 0, 1);
            layout.Controls.Add(upload, 0, 2);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Termination date", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft }, 0, 3);
            layout.Controls.Add(CreateField("Date", DATE), 0, 4);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Termination moves the employee to rolled-out records before removing them from active staff.", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.TopLeft }, 0, 5);
            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, BorderStyle = BorderStyle.FixedSingle, Padding = padding, Margin = margin };
        }

        private TableLayoutPanel CreateSectionLayout(string title, int rows, int columns)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = rows, ColumnCount = columns, BackColor = SurfaceColor };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            for (int i = 1; i < rows; i++) layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / (rows - 1)));
            for (int i = 0; i < columns; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            var titleLabel = new Label { Dock = DockStyle.Fill, Text = title, ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(titleLabel, 0, 0);
            layout.SetColumnSpan(titleLabel, columns);
            return layout;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 0, 12, 10), BackColor = SurfaceColor, Margin = Padding.Empty };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = labelText, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 8.75F), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            input.Dock = DockStyle.Fill;
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private Control BuildStatusBar()
        {
            statusLabel = new Label { Dock = DockStyle.Fill, BackColor = PageBackColor, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft, Text = "Ready." };
            return statusLabel;
        }

        private Control BuildActions()
        {
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = PageBackColor };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.Controls.Add(CreatePrimaryButton("Update", UpdateEmployee), 0, 0);
            actions.Controls.Add(CreateDangerButton("Terminate", TerminateEmployee), 1, 0);
            actions.Controls.Add(CreateSecondaryButton("Employee List", () => { Close(); new frmEmpView().Show(); }), 2, 0);
            return actions;
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

        private Button CreateDangerButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = DangerColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 241, 242);
            return button;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button { Dock = DockStyle.Fill, Height = 38, Margin = new Padding(8, 0, 0, 0), Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            button.Click += (sender, args) => action();
            return button;
        }

        private void frmEmpDetails_Load(object sender, EventArgs e)
        {
            PopulateFromData();
        }

        private void PopulateFromData()
        {
            if (data == null || data.Rows.Count == 0)
            {
                statusLabel.Text = "No employee data loaded.";
                return;
            }

            DataRow row = data.Rows[0];
            txtEMdID.Text = row["ID"].ToString();
            txtFN.Text = row["FULL NAME"].ToString();
            cmbGN.Text = row["GENDER"].ToString();
            dateDOB.Value = SafeDate(row["DATE OF BIRTH"], DateTime.Today.AddYears(-25));
            txtCN.Text = row["CONTACT"].ToString();
            cmbDPT.Text = row["DEPARTMENT"].ToString();
            CmbPs.Text = row["POSITION"].ToString();
            txtHT.Text = row["HOME TOWN"].ToString();
            txtRD.Text = row["RESIDENCE"].ToString();
            empdate.Value = SafeDate(row["DATE OF EMPLOYMENT"], DateTime.Today);
            empMD.Text = row["EMPLOYMENT MODE"].ToString();
            empST.Text = row["EMPLOYMENT STATUS"].ToString();
            empCN.Text = row["EMERGENCY CONTACT PERSON"].ToString();
            empEC.Text = row["EMERGENCY CONTACT"].ToString();
            empRV.Text = row["EMPLOYEE REVIEWS"].ToString();
            empSA.Text = row["SALARY"].ToString();
            DATE.Value = DateTime.Today;

            if (row.Table.Columns.Contains("PICTURE") && row["PICTURE"] != DBNull.Value)
            {
                using (MemoryStream stream = new MemoryStream((byte[])row["PICTURE"]))
                {
                    std_pic.Image = Image.FromStream(stream);
                }
            }
            statusLabel.Text = "Employee record loaded.";
        }

        private DateTime SafeDate(object value, DateTime fallback)
        {
            return value == null || value == DBNull.Value || !DateTime.TryParse(value.ToString(), out DateTime parsed) ? fallback : parsed;
        }

        private void upload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    std_pic.Image = new Bitmap(dialog.FileName);
                }
            }
        }

        private Employee MapFormToEmployee()
        {
            decimal.TryParse(empSA.Text.Trim(), out decimal salary);
            return new Employee
            {
                EmployeeID = txtEMdID.Text,
                FullName = txtFN.Text.Trim(),
                Gender = cmbGN.Text.Trim(),
                DateOfBirth = dateDOB.Value.Date,
                Contact = txtCN.Text.Trim(),
                Department = cmbDPT.Text.Trim(),
                Position = CmbPs.Text.Trim(),
                HomeTown = txtHT.Text.Trim(),
                Residence = txtRD.Text.Trim(),
                EmploymentDate = empdate.Value.Date,
                EmploymentMode = empMD.Text.Trim(),
                EmploymentStatus = empST.Text.Trim(),
                EmergencyContactPerson = empCN.Text.Trim(),
                EmergencyContact = empEC.Text.Trim(),
                PerformanceReview = empRV.Text.Trim(),
                Salary = salary,
                ProfilePhoto = ImageHelper.ImageToBytes(std_pic.Image)
            };
        }

        private async void UpdateEmployee()
        {
            statusLabel.Text = "Updating employee...";
            var (success, message) = await _employeeService.UpdateEmployeeAsync(MapFormToEmployee());

            if (success)
            {
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Employee Details");
            }
            else
            {
                statusLabel.Text = "Update failed.";
                UIHelper.ShowWarning(message, "Employee Details");
            }
        }

        private async void TerminateEmployee()
        {
            if (UIHelper.ShowConfirmation("Terminate this employee contract?", "Confirm Termination") != DialogResult.Yes) return;

            statusLabel.Text = "Terminating contract...";
            var (success, message) = await _employeeService.TerminateEmployeeAsync(txtEMdID.Text, DATE.Value.Date);

            if (success)
            {
                UIHelper.ShowSuccess(message, "Employee Details");
                Close();
                new frmEmpView().Show();
            }
            else
            {
                statusLabel.Text = "Termination failed.";
                UIHelper.ShowError(message, "Employee Details");
            }
        }

        private void gunaPictureBox2_Click(object sender, EventArgs e) { Close(); }
    }
}
