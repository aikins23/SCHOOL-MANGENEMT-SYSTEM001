using System;
using System.Drawing;
using System.Threading.Tasks;
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

        // Age constraints for date of birth
        private const int MinimumEmployeeAge = 21;
        private const int MaximumEmployeeAge = 65;
        private const int DefaultEmployeeAge = 35;

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

        // ── Layout constants (one place to tune everything) ──────────────────
        private const int InputH      = 38;   // all text/combo/date inputs
        private const int LabelH      = 19;   // field caption
        private const int FieldGap    = 10;   // bottom gap between fields
        private const int FieldRowH   = LabelH + InputH + FieldGap; // 67 px
        private const int CardPad     = 22;   // inner card padding
        private const int HeaderH     = 58;   // section header height
        private const int SectionGap  = 12;   // gap between cards

        private void BuildModernEmployeeForm()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Employee Registration";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 700);
            ClientSize = new Size(1240, 780);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(28, 20, 28, 14)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // cards
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // status
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // buttons

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFormBody(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);

            PrepareInputs();
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

            dateDOB.Value = DateTime.Today.AddYears(-DefaultEmployeeAge);
            dateDOB.MinDate = DateTime.Today.AddYears(-MaximumEmployeeAge);
            dateDOB.MaxDate = DateTime.Today.AddYears(-MinimumEmployeeAge);
            empdate.Value = DateTime.Today;
            emp_pic.SizeMode = PictureBoxSizeMode.Zoom;
            emp_pic.BackColor = AccentColor;
            upload.Text = "Upload Photo";
            upload.FillColor = UiTheme.Gold;
            upload.ForeColor = TextColor;
        }

        private void StyleInput(Control control)
        {
            control.Font  = new Font("Segoe UI", 9.75F);
            control.Margin = Padding.Empty;

            if (control is Guna.UI2.WinForms.Guna2TextBox tb)
            {
                tb.FillColor             = SurfaceColor;
                tb.BorderColor           = BorderColor;
                tb.FocusedState.BorderColor = UiTheme.Gold;
                tb.HoverState.BorderColor   = UiTheme.Gold;
                tb.BorderRadius          = 6;
                tb.BorderThickness       = 1;
                tb.ForeColor             = TextColor;
                tb.Height                = InputH;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2ComboBox cb)
            {
                cb.FillColor             = SurfaceColor;
                cb.BorderColor           = BorderColor;
                cb.FocusedState.BorderColor = UiTheme.Gold;
                cb.HoverState.BorderColor   = UiTheme.Gold;
                cb.BorderRadius          = 6;
                cb.ForeColor             = TextColor;
                cb.ItemHeight            = 32;
                cb.Height                = InputH;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2DateTimePicker dp)
            {
                dp.FillColor             = SurfaceColor;
                dp.BorderColor           = BorderColor;
                dp.BorderRadius          = 6;
                dp.BorderThickness       = 1;
                dp.ForeColor             = TextColor;
                dp.Height                = InputH;
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
            actions.Controls.Add(CreatePrimaryButton("View Employees", async () =>
            {
                Close();
                new frmEmpView().Show();
                await Task.CompletedTask;
            }));
            actions.Controls.Add(CreateSecondaryButton("Dashboard", async () =>
            {
                Close();
                new frmDashboard().Show();
                await Task.CompletedTask;
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
                ColumnCount = 3,
                RowCount = 1,
                BackColor = PageBackColor,
                Margin = Padding.Empty
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            body.Controls.Add(BuildPersonalPanel(), 0, 0);
            body.Controls.Add(BuildEmploymentPanel(), 1, 0);
            body.Controls.Add(BuildPhotoPanel(), 2, 0);
            return body;
        }

        // Personal Details card — 5 field rows × 2 columns
        private Control BuildPersonalPanel()
        {
            var card = CreateCard(new Padding(0, 0, SectionGap, 0));
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Margin = Padding.Empty,
                Padding = new Padding(CardPad)
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderH));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            for (int r = 0; r < 5; r++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, FieldRowH));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            grid.Controls.Add(CreateField("Employee ID",   txtEMdID), 0, 0);
            grid.Controls.Add(CreateField("Full Name",     txtFN),    1, 0);
            grid.Controls.Add(CreateField("Gender",        cmbGN),    0, 1);
            grid.Controls.Add(CreateField("Date of Birth", dateDOB),  1, 1);
            grid.Controls.Add(CreateField("Department",    cmbDPT),   0, 2);
            grid.Controls.Add(CreateField("Position",      CmbPs),    1, 2);
            grid.Controls.Add(CreateField("Contact",       txtCN),    0, 3);
            grid.Controls.Add(CreateField("Home Town",     txtHT),    1, 3);
            var resField = CreateField("Residence", txtRD);
            grid.Controls.Add(resField, 0, 4);
            grid.SetColumnSpan(resField, 2);

            inner.Controls.Add(BuildSectionHeader("Personal Details",
                "ID, name, placement & contact"), 0, 0);
            inner.Controls.Add(grid, 0, 1);
            card.Controls.Add(inner);
            return card;
        }

        // Employment & Emergency card — 4 field rows × 2 columns + tip strip
        private Control BuildEmploymentPanel()
        {
            var card = CreateCard(new Padding(0, 0, SectionGap, 0));
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Margin = Padding.Empty,
                Padding = new Padding(CardPad)
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderH));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // tip

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 2,
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            for (int r = 0; r < 4; r++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, FieldRowH));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            grid.Controls.Add(CreateField("Employment Date",          empdate), 0, 0);
            grid.Controls.Add(CreateField("Employment Mode",          empMD),   1, 0);
            grid.Controls.Add(CreateField("Employment Status",        empST),   0, 1);
            grid.Controls.Add(CreateField("Performance Review",       empRV),   1, 1);
            grid.Controls.Add(CreateField("Emergency Contact Person", empCN),   0, 2);
            grid.Controls.Add(CreateField("Emergency Contact",        empEC),   1, 2);
            var salField = CreateField("Salary (GHS)", empSA);
            grid.Controls.Add(salField, 0, 3);
            grid.SetColumnSpan(salField, 2);

            var tip = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(255, 251, 230),
                Margin = Padding.Empty
            };
            var tipLbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "ℹ  Keep contact and status current — payroll and leave screens depend on this data.",
                ForeColor = Color.FromArgb(113, 91, 13),
                Font = new Font("Segoe UI", 8.25F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 6, 0)
            };
            tip.Controls.Add(tipLbl);

            inner.Controls.Add(BuildSectionHeader("Employment & Emergency",
                "Contract, status & emergency contacts"), 0, 0);
            inner.Controls.Add(grid, 0, 1);
            inner.Controls.Add(tip, 0, 2);
            card.Controls.Add(inner);
            return card;
        }

        // Photo card — photo + upload + caption
        private Control BuildPhotoPanel()
        {
            var card = CreateCard(Padding.Empty);
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Margin = Padding.Empty,
                Padding = new Padding(CardPad)
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderH));  // section title
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));        // divider
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));       // photo
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));       // upload btn
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));       // caption

            inner.Controls.Add(BuildSectionHeader("Staff Photo",
                "Passport-style portrait"), 0, 0);

            inner.Controls.Add(new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BorderColor
            }, 0, 1);

            emp_pic.Dock = DockStyle.Fill;
            emp_pic.BorderStyle = BorderStyle.None;
            emp_pic.BackColor = Color.FromArgb(242, 244, 248);
            emp_pic.SizeMode = PictureBoxSizeMode.Zoom;
            emp_pic.Margin = new Padding(0, 8, 0, 8);
            inner.Controls.Add(emp_pic, 0, 2);

            upload.Dock = DockStyle.Fill;
            upload.FillColor = UiTheme.Gold;
            upload.ForeColor = TextColor;
            upload.Text = "Upload Photo";
            upload.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            upload.BorderRadius = 6;
            upload.Click -= upload_Click;
            upload.Click += upload_Click;
            inner.Controls.Add(upload, 0, 3);

            inner.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "JPG, PNG or BMP  ·  Clear background\nShown on employee profile and reports",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8F),
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 6, 0, 0)
            }, 0, 4);

            card.Controls.Add(inner);
            return card;
        }

        // Consistent section header: bold title + muted subtitle + 1px divider
        private Control BuildSectionHeader(string title, string subtitle)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            pnl.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BorderColor
            });
            pnl.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 17,
                Text = subtitle,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.25F),
                TextAlign = ContentAlignment.BottomLeft
            });
            pnl.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = title,
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            return pnl;
        }

        /// <summary>
        /// Creates a white card panel with the given outer margin (used as spacing between cards).
        /// The caller is responsible for adding a padded inner panel.
        /// </summary>
        private Panel CreateCard(Padding margin)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Margin = margin,
                Padding = Padding.Empty
            };
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

        private Control CreateField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0, 4, 8, 4),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.25F),
                TextAlign = ContentAlignment.BottomLeft,
                AutoEllipsis = true
            }, 0, 0);

            input.Dock = DockStyle.Fill;
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
            var wrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = PageBackColor
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // spacer
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560)); // button cluster

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = PageBackColor
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            actions.Controls.Add(CreateSecondaryButton("New", async () => await NewEmployee()), 0, 0);
            actions.Controls.Add(CreatePrimaryButton("Save", async () => await SaveEmployeeAsync()), 1, 0);
            actions.Controls.Add(CreateSecondaryButton("Update", async () => await UpdateEmployee()), 2, 0);
            actions.Controls.Add(CreateDangerButton("Delete", async () => await DeleteEmployee()), 3, 0);

            wrapper.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = PageBackColor }, 0, 0);
            wrapper.Controls.Add(actions, 1, 0);
            return wrapper;
        }

        private Button CreatePrimaryButton(string text, Func<Task> asyncAction)
        {
            var button = CreateButton(text, asyncAction);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;
            return button;
        }

        private Button CreateSecondaryButton(string text, Func<Task> asyncAction)
        {
            var button = CreateButton(text, asyncAction);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = AccentColor;
            return button;
        }

        private Button CreateDangerButton(string text, Func<Task> asyncAction)
        {
            var button = CreateButton(text, asyncAction);
            button.BackColor = SurfaceColor;
            button.ForeColor = DangerColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(254, 205, 211);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 241, 242);
            return button;
        }

        private Button CreateButton(string text, Func<Task> asyncAction)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0),
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1 }
            };
            button.Click += async (sender, args) => await asyncAction();
            return button;
        }

        private async void frmEmployee_Load(object sender, EventArgs e)
        {
            try
            {
                await InitializeForm();
                LoggerHelper.LogInfo("frmEmployee loaded successfully");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Error loading form: " + ex.Message, "Employee Management");
                LoggerHelper.LogError("frmEmployee_Load failed", ex);
            }
        }

        private async System.Threading.Tasks.Task InitializeForm()
        {
            try
            {
                txtEMdID.Text = await _employeeService.GenerateNextEmployeeIdAsync();
                ClearEmployeeDetails();
                statusLabel.Text = "Ready for a new employee record.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Could not prepare the next employee ID.";
                LoggerHelper.LogError("InitializeForm failed", ex);
                throw;
            }
        }

        private void ClearEmployeeDetails()
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
            dateDOB.Value = DateTime.Today.AddYears(-DefaultEmployeeAge);
            empdate.Value = DateTime.Today;
            emp_pic.Image = null;
        }

        private async void txtEmployeeID_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtEMdID.Text.Length < 3)
                {
                    ClearEmployeeDetails();
                    return;
                }

                string employeeId = txtEMdID.Text.Trim();
                var existingEmployee = await _employeeService.GetEmployeeAsync(employeeId);

                if (existingEmployee != null)
                {
                    // Employee exists - load for editing
                    txtFN.Text = existingEmployee.FullName ?? "";
                    cmbDPT.Text = existingEmployee.Department ?? "";
                    CmbPs.Text = existingEmployee.Position ?? "";
                    dateDOB.Value = existingEmployee.DateOfBirth;
                    txtCN.Text = existingEmployee.Contact ?? "";
                    empSA.Text = existingEmployee.Salary.ToString("0.00");
                    cmbGN.Text = existingEmployee.Gender ?? "";
                    txtHT.Text = existingEmployee.HomeTown ?? "";
                    txtRD.Text = existingEmployee.Residence ?? "";
                    empdate.Value = existingEmployee.EmploymentDate;
                    empMD.Text = existingEmployee.EmploymentMode ?? "";
                    empST.Text = existingEmployee.EmploymentStatus ?? "";
                    empCN.Text = existingEmployee.EmergencyContactPerson ?? "";
                    empEC.Text = existingEmployee.EmergencyContact ?? "";
                    empRV.Text = existingEmployee.PerformanceReview ?? "";

                    statusLabel.Text = $"Loaded employee: {existingEmployee.FullName}";
                }
                else
                {
                    // New employee
                    ClearEmployeeDetails();
                    statusLabel.Text = "Ready for new employee.";
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Employee ID lookup failed", ex);
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

        private async System.Threading.Tasks.Task SaveEmployeeAsync()
        {
            try
            {
                if (!ValidateEmployeeFields())
                    return;

                // Confirmation
                if (!ConfirmationHelper.ConfirmSave($"This will save employee {txtFN.Text}?"))
                    return;

                // Save to database
                var employee = MapFormToEmployee();

                bool isNew = await _employeeService.GetEmployeeAsync(employee.EmployeeID) == null;
                var (success, message) = isNew
                    ? await _employeeService.AddEmployeeAsync(employee)
                    : await _employeeService.UpdateEmployeeAsync(employee);

                if (success)
                {
                    ConfirmationHelper.ShowInfo($"Employee {(isNew ? "added" : "updated")} successfully");
                    LoggerHelper.LogInfo($"Employee {employee.EmployeeID} {(isNew ? "added" : "updated")}");
                    txtEMdID.Text = "";
                    ClearEmployeeDetails();
                    txtEMdID.Focus();
                }
                else
                {
                    UIHelper.ShowError("Could not save employee", "Error");
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Save failed: " + ex.Message, "Employee Registration");
                LoggerHelper.LogError("Employee save failed", ex);
            }
        }

        private async System.Threading.Tasks.Task UpdateEmployee()
        {
            await SaveEmployeeAsync();
        }

        private async System.Threading.Tasks.Task DeleteEmployee()
        {
            await DeleteEmployeeAsync();
        }

        private async System.Threading.Tasks.Task DeleteEmployeeAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtEMdID.Text))
                {
                    UIHelper.ShowWarning("Please select an employee to delete.", "Employee Registration");
                    return;
                }

                string employeeInfo = $"ID: {txtEMdID.Text}\nName: {txtFN.Text}";
                if (!ConfirmationHelper.ConfirmDelete("Employee", employeeInfo))
                {
                    return;
                }

                statusLabel.Text = "Deleting employee...";
                var (success, message) = await _employeeService.DeleteEmployeeAsync(txtEMdID.Text);

                if (success)
                {
                    ConfirmationHelper.ShowInfo("Employee deleted successfully");
                    LoggerHelper.LogInfo($"Employee {txtEMdID.Text} deleted");
                    statusLabel.Text = message;
                    await NewEmployee();
                }
                else
                {
                    statusLabel.Text = "Delete failed.";
                    UIHelper.ShowError(message, "Employee Registration");
                    LoggerHelper.LogError("DeleteEmployeeAsync failed: " + message, null);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Delete failed.";
                UIHelper.ShowError("Delete failed: " + ex.Message, "Employee Management");
                LoggerHelper.LogError("Employee delete failed", ex);
            }
        }

        private async System.Threading.Tasks.Task NewEmployee()
        {
            try
            {
                await InitializeForm();
                txtEMdID.Focus();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("NewEmployee failed", ex);
            }
        }

        private bool ValidateEmployeeFields()
        {
            if (!FormValidationHelper.ValidateRequired(txtEMdID, "Employee ID"))
                return false;

            if (!FormValidationHelper.ValidateRequired(txtFN, "Full Name"))
                return false;

            if (!FormValidationHelper.ValidateComboBox(cmbDPT, "Department"))
                return false;

            if (!FormValidationHelper.ValidateComboBox(CmbPs, "Position"))
                return false;

            if (!FormValidationHelper.ValidateComboBox(cmbGN, "Gender"))
                return false;

            // Validate Date of Birth (custom for Guna2DateTimePicker)
            if (dateDOB.Value.Date > DateTime.Today.AddYears(-MinimumEmployeeAge))
            {
                UIHelper.ShowWarning($"Employee must be at least {MinimumEmployeeAge} years old.", "Validation");
                dateDOB.Focus();
                return false;
            }

            if (dateDOB.Value.Date < DateTime.Today.AddYears(-MaximumEmployeeAge))
            {
                UIHelper.ShowWarning($"Employee cannot be older than {MaximumEmployeeAge} years.", "Validation");
                dateDOB.Focus();
                return false;
            }

            if (!FormValidationHelper.ValidateNumeric(empSA, "Salary", out decimal salary))
                return false;

            // Validate Contact (optional but if provided, should be valid)
            if (!string.IsNullOrWhiteSpace(txtCN.Text))
            {
                if (txtCN.Text.Length < 7)
                {
                    UIHelper.ShowWarning("Contact number must be at least 7 digits.", "Validation");
                    txtCN.Focus();
                    return false;
                }
            }

            // Validate Emergency Contact (optional but if provided, should be valid)
            if (!string.IsNullOrWhiteSpace(empEC.Text))
            {
                if (empEC.Text.Length < 7)
                {
                    UIHelper.ShowWarning("Emergency contact must be at least 7 digits.", "Validation");
                    empEC.Focus();
                    return false;
                }
            }

            return true;
        }

        private async void btnSave_Click(object sender, EventArgs e) { await SaveEmployeeAsync(); }
        private async void btn_Update_Click(object sender, EventArgs e) { await UpdateEmployee(); }
        private async void btnDel_Click(object sender, EventArgs e) { await DeleteEmployee(); }
        private async void btnNew_Click(object sender, EventArgs e) { await NewEmployee(); }
        private void pay_Click(object sender, EventArgs e) { Close(); new frmEmpView().Show(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Application.Exit(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }

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
