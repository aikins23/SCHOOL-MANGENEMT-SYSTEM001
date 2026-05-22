using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmEmployee : Form
    {
        private readonly EmployeeService _employeeService;
        private Label statusLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color AccentColor = UiTheme.GoldSoft;
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        // Drag state variables
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point formStartPoint;

        public frmEmployee()
        {
            InitializeComponent();

            // Initialize modern architecture
            var repository = new EmployeeRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(repository);

            BuildModernEmployeeForm();
            EnableFormDragging();
        }

        private void BuildModernEmployeeForm()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Employee Registration";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 720);

            PrepareInputs();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
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

            foreach (Control control in new Control[] { txtEMdID, txtFN, txtCN, txtHT, txtRD, empCN, empEC, empSA, cmbGN, cmbDPT, CmbPs, empMD, empST, empRV, dateDOB, empdate })
            {
                StyleInput(control);
            }

            txtEMdID.FillColor = UiTheme.SurfaceAlt;

            cmbGN.Items.Clear();
            cmbGN.Items.AddRange(AppConfig.GenderOptions);
            cmbGN.SelectedIndex = 0;

            cmbDPT.Items.Clear();
            cmbDPT.Items.AddRange(new object[]
            {
                "ADMINISTRATION",
                "SANITATION & CLEANING",
                "CRECHE",
                "NURSERY",
                "KINDERGARTEN",
                "LOWER PRIMARY",
                "UPPER PRIMARY",
                "JHS (JUNIOR HIGH SCHOOL)"
            });
            cmbDPT.SelectedIndex = 0;

            CmbPs.Items.Clear();
            CmbPs.Items.AddRange(new object[] { "NON-POSITIONAL", "HEAD", "DEPUTY", "SECRETARY" });
            CmbPs.SelectedIndex = 0;

            empMD.Items.Clear();
            empMD.Items.AddRange(new object[] { "FULL-TIME", "PART-TIME", "CONTRACT" });
            empMD.SelectedIndex = 0;

            empST.Items.Clear();
            empST.Items.AddRange(new object[] { "ACTIVE", "IN-ACTIVE" });
            empST.SelectedIndex = 0;

            empRV.Items.Clear();
            empRV.Items.AddRange(new object[] { "A: EXCELLENT", "B: GOOD", "C: SATISFACTORY", "D: UNSATISFACTORY" });
            empRV.SelectedIndex = 1;

            dateDOB.Value = DateTime.Today.AddYears(-25);
            empdate.Value = DateTime.Today;
            emp_pic.SizeMode = PictureBoxSizeMode.Zoom;
            emp_pic.BackColor = AccentColor;
            upload.Text = "Upload Photo";
            upload.FillColor = UiTheme.Gold;
            upload.ForeColor = TextColor;
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

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor };
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                Text = "Employee Registration",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Add staff records, employment details, emergency contacts, and salary",
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
            actions.Controls.Add(CreatePrimaryButton("View Employees", () =>
            {
                Close();
                new frmEmpView().Show();
            }));
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () =>
            {
                Close();
                new frmDashboard().Show();
            }));

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildFormBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = PageBackColor
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            var detailsStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Margin = new Padding(0, 0, 14, 0)
            };
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
            var panel = CreateSurfacePanel(new Padding(18), new Padding(0, 0, 0, 14));
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
            var panel = CreateSurfacePanel(new Padding(18), Padding.Empty);
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
            var panel = CreateSurfacePanel(new Padding(18), Padding.Empty);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Photo",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            emp_pic.Dock = DockStyle.Fill;
            emp_pic.BorderStyle = BorderStyle.None;
            upload.Dock = DockStyle.Fill;
            upload.Click -= upload_Click;
            upload.Click += upload_Click;

            layout.Controls.Add(emp_pic, 0, 1);
            layout.Controls.Add(upload, 0, 2);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Record quality",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 3);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Use current contact and employment status so payroll and leave screens stay reliable.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.TopLeft
            }, 0, 4);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Upload a clear portrait photo for quick identification.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 5);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateSurfacePanel(Padding padding, Padding margin)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = padding,
                Margin = margin
            };
        }

        private TableLayoutPanel CreateSectionLayout(string title, int rows, int columns)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = rows,
                ColumnCount = columns,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            for (int i = 1; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / (rows - 1)));
            }
            for (int i = 0; i < columns; i++)
            {
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
            }

            var titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(titleLabel, 0, 0);
            layout.SetColumnSpan(titleLabel, columns);
            return layout;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0, 0, 10, 10),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            }, 0, 0);

            input.Dock = DockStyle.Fill;
            input.Height = 36;
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private Control BuildStatusBar()
        {
            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackColor,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Ready."
            };
            return statusLabel;
        }

        private Control BuildActions()
        {
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                BackColor = PageBackColor
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            actions.Controls.Add(CreateSecondaryButton("New", async () => await NewEmployee()), 0, 0);
            actions.Controls.Add(CreatePrimaryButton("Save", SaveEmployee), 1, 0);
            actions.Controls.Add(CreateSecondaryButton("Update", UpdateEmployee), 2, 0);
            actions.Controls.Add(CreateDangerButton("Delete", DeleteEmployee), 3, 0);
            return actions;
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = AccentColor;
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
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Height = 38,
                Margin = new Padding(8, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.Click += (sender, args) => action();
            return button;
        }

        private async void frmEmployee_Load(object sender, EventArgs e)
        {
            await SetNextEmployeeId();
        }

        private async System.Threading.Tasks.Task SetNextEmployeeId()
        {
            try
            {
                txtEMdID.Text = await _employeeService.GenerateNextEmployeeIdAsync();
                statusLabel.Text = "Ready for a new employee record.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Could not prepare the next employee ID.";
                UIHelper.ShowError("Error: " + ex.Message, "Employee Registration");
            }
        }

        private void upload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        emp_pic.Image = new Bitmap(dialog.FileName);
                        statusLabel.Text = "Photo selected.";
                    }
                    catch (Exception ex)
                    {
                        statusLabel.Text = "Photo error.";
                        UIHelper.ShowWarning(ex.Message, "Employee Registration");
                    }
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
                ProfilePhoto = ImageHelper.ImageToBytes(emp_pic.Image)
            };
        }

        private async void SaveEmployee()
        {
            statusLabel.Text = "Saving employee...";
            var employee = MapFormToEmployee();
            var (success, message) = await _employeeService.AddEmployeeAsync(employee);

            if (success)
            {
                txtEMdID.Text = employee.EmployeeID;
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Employee Registration");
            }
            else
            {
                statusLabel.Text = "Save failed.";
                UIHelper.ShowWarning(message, "Employee Registration");
            }
        }

        private async void UpdateEmployee()
        {
            if (string.IsNullOrWhiteSpace(txtEMdID.Text))
            {
                UIHelper.ShowWarning("Please select an employee to update.", "Employee Registration");
                return;
            }

            statusLabel.Text = "Updating employee...";
            var employee = MapFormToEmployee();
            var (success, message) = await _employeeService.UpdateEmployeeAsync(employee);

            if (success)
            {
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Employee Registration");
            }
            else
            {
                statusLabel.Text = "Update failed.";
                UIHelper.ShowWarning(message, "Employee Registration");
            }
        }

        private async void DeleteEmployee()
        {
            if (string.IsNullOrWhiteSpace(txtEMdID.Text))
            {
                UIHelper.ShowWarning("Please select an employee to delete.", "Employee Registration");
                return;
            }

            if (UIHelper.ShowConfirmation("Delete this employee record?", "Employee Registration") != DialogResult.Yes)
            {
                return;
            }

            statusLabel.Text = "Deleting employee...";
            var (success, message) = await _employeeService.DeleteEmployeeAsync(txtEMdID.Text);

            if (success)
            {
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Employee Registration");
                await NewEmployee();
            }
            else
            {
                statusLabel.Text = "Delete failed.";
                UIHelper.ShowWarning(message, "Employee Registration");
            }
        }

        private async System.Threading.Tasks.Task NewEmployee()
        {
            txtFN.Text = "";
            txtCN.Text = "";
            txtHT.Text = "";
            txtRD.Text = "";
            empCN.Text = "";
            empEC.Text = "";
            empSA.Text = "";
            cmbGN.SelectedIndex = 0;
            cmbDPT.SelectedIndex = 0;
            CmbPs.SelectedIndex = 0;
            empMD.SelectedIndex = 0;
            empST.SelectedIndex = 0;
            empRV.SelectedIndex = 1;
            dateDOB.Value = DateTime.Today.AddYears(-25);
            empdate.Value = DateTime.Today;
            emp_pic.Image = null;
            await SetNextEmployeeId();
        }

        private void btnSave_Click(object sender, EventArgs e) { SaveEmployee(); }
        private void btnSave_Click_1(object sender, EventArgs e) { SaveEmployee(); }
        private void btn_Update_Click(object sender, EventArgs e) { UpdateEmployee(); }
        private void btnDel_Click(object sender, EventArgs e) { DeleteEmployee(); }
        private async void btnNew_Click(object sender, EventArgs e) { await NewEmployee(); }
        private void btnEdit_Click(object sender, EventArgs e) { }
        private void pay_Click(object sender, EventArgs e) { Close(); new frmEmpView().Show(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Application.Exit(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
        private void gunaButton1_Click(object sender, EventArgs e) { }

        /// <summary>
        /// Enables form dragging functionality by subscribing to mouse events on all controls
        /// </summary>
        private void EnableFormDragging()
        {
            // Subscribe to form's own mouse events
            SubscribeToDragEvents(this);

            // Subscribe to all child controls recursively
            foreach (Control control in GetAllControls(this))
            {
                if (!IsInteractiveControl(control))
                {
                    SubscribeToDragEvents(control);
                }
            }
        }

        /// <summary>
        /// Subscribe a control to drag mouse events
        /// </summary>
        private void SubscribeToDragEvents(Control control)
        {
            control.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStartPoint = e.Location;
                    formStartPoint = this.Location;
                }
            };

            control.MouseMove += (s, e) =>
            {
                if (isDragging && e.Button == MouseButtons.Left)
                {
                    int deltaX = e.X - dragStartPoint.X;
                    int deltaY = e.Y - dragStartPoint.Y;
                    this.Location = new Point(formStartPoint.X + deltaX, formStartPoint.Y + deltaY);
                }
            };

            control.MouseUp += (s, e) =>
            {
                isDragging = false;
            };
        }

        /// <summary>
        /// Recursively gets all controls on the form
        /// </summary>
        private System.Collections.Generic.List<Control> GetAllControls(Control container)
        {
            var controls = new System.Collections.Generic.List<Control>();
            foreach (Control control in container.Controls)
            {
                controls.Add(control);
                controls.AddRange(GetAllControls(control));
            }
            return controls;
        }

        /// <summary>
        /// Checks if a control should not have drag enabled
        /// </summary>
        private bool IsInteractiveControl(Control control)
        {
            Type controlType = control.GetType();
            return controlType == typeof(TextBox) ||
                   controlType == typeof(ComboBox) ||
                   controlType == typeof(Button) ||
                   controlType == typeof(CheckBox) ||
                   controlType == typeof(RadioButton) ||
                   controlType == typeof(DataGridView) ||
                   controlType == typeof(ListBox) ||
                   controlType == typeof(TreeView) ||
                   controlType == typeof(RichTextBox) ||
                   controlType.Name.Contains("NumericUpDown") ||
                   controlType.Name.Contains("DateTimePicker");
        }
    }
}
