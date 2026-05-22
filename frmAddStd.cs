using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAddStd : Form
    {
        private readonly StudentService _studentService;
        private Label statusLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color AccentColor = UiTheme.GoldSoft;
        private static readonly Color GoldColor = UiTheme.Gold;
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        // Drag state variables
        private bool isDragging = false;
        private Point dragStartPoint;
        private Point formStartPoint;

        public frmAddStd()
        {
            InitializeComponent();
            
            // Initialize modern architecture
            var studentRepository = new StudentRepository(AppConfig.ConnectionString);
            var feeRepository = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepository, feeRepository);

            BuildModernAdmissionView();
            EnableFormDragging();
        }

        private void BuildModernAdmissionView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Student Admission";
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
                Padding = new Padding(26, 22, 26, 18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildFormBody(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);
            root.Controls.Add(BuildActions(), 0, 3);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private void PrepareInputs()
        {
            txtStdID.ReadOnly = true;

            foreach (Control control in new Control[] { txtStdID, txtFN, txtLN, txtEM, txtHT, txtRD, txtAG, txtEC, txtGN, txtGE, txtGL, cmbGN, cmbCID, dateDOB, dateAD })
            {
                StyleInput(control);
            }

            txtStdID.FillColor = UiTheme.SurfaceAlt;

            cmbGN.Items.Clear();
            cmbGN.Items.AddRange(AppConfig.GenderOptions);
            cmbGN.SelectedIndex = 0;

            cmbCID.Items.Clear();
            cmbCID.Items.AddRange(AppConfig.ClassNames);
            cmbCID.SelectedIndex = 0;

            dateDOB.Value = DateTime.Today.AddYears(-5);
            dateAD.Value = DateTime.Today;
            std_pic.SizeMode = PictureBoxSizeMode.Zoom;
            std_pic.BackColor = AccentColor;
            std_pic.BorderStyle = BorderStyle.None;
            upload.Text = "Upload Photo";
            upload.FillColor = GoldColor;
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
                textBox.FocusedState.BorderColor = GoldColor;
                textBox.BorderRadius = 4;
                textBox.BorderThickness = 1;
                textBox.ForeColor = TextColor;
                textBox.PlaceholderForeColor = MutedTextColor;
                textBox.Height = 36;
                return;
            }

            if (control is Guna.UI2.WinForms.Guna2ComboBox comboBox)
            {
                comboBox.FillColor = SurfaceColor;
                comboBox.BorderColor = BorderColor;
                comboBox.FocusedState.BorderColor = GoldColor;
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
                Text = "Student Admission",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Register learners, assign class, guardian details, and opening fee records",
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
            actions.Controls.Add(CreatePrimaryButton("View Students", () =>
            {
                Close();
                new frmStdView().Show();
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
            detailsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            detailsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            detailsStack.Controls.Add(BuildPersonalPanel(), 0, 0);
            detailsStack.Controls.Add(BuildGuardianPanel(), 0, 1);

            body.Controls.Add(detailsStack, 0, 0);
            body.Controls.Add(BuildPhotoPanel(), 1, 0);
            return body;
        }

        private Control BuildPersonalPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), new Padding(0, 0, 0, 14));
            var layout = CreateSectionLayout("Learner Details", 6, 2);

            layout.Controls.Add(CreateField("Student ID", txtStdID), 0, 1);
            layout.Controls.Add(CreateField("Class", cmbCID), 1, 1);
            layout.Controls.Add(CreateField("First Name", txtFN), 0, 2);
            layout.Controls.Add(CreateField("Last Name", txtLN), 1, 2);
            layout.Controls.Add(CreateField("Date of Birth", dateDOB), 0, 3);
            layout.Controls.Add(CreateField("Gender", cmbGN), 1, 3);
            layout.Controls.Add(CreateField("Email", txtEM), 0, 4);
            layout.Controls.Add(CreateField("Emergency Contact", txtEC), 1, 4);
            layout.Controls.Add(CreateField("Home Town", txtHT), 0, 5);
            layout.Controls.Add(CreateField("Residence", txtRD), 1, 5);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildGuardianPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = CreateSectionLayout("Guardian and Admission", 4, 2);

            layout.Controls.Add(CreateField("Guardian Name", txtGN), 0, 1);
            layout.Controls.Add(CreateField("Guardian Email", txtGE), 1, 1);
            layout.Controls.Add(CreateField("Guardian Location", txtGL), 0, 2);
            layout.Controls.Add(CreateField("Admission Date", dateAD), 1, 2);
            layout.Controls.Add(CreateField("Allergies / Medical Notes", txtAG), 0, 3);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 3), 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildPhotoPanel()
        {
            var panel = CreateSurfacePanel(new Padding(24, 20, 24, 22), Padding.Empty);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Photo",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            std_pic.Dock = DockStyle.Fill;
            std_pic.Margin = new Padding(0, 0, 0, 10);
            upload.Dock = DockStyle.Fill;
            upload.Margin = new Padding(0, 0, 0, 12);
            upload.Click -= upload_Click;
            upload.Click += upload_Click;

            layout.Controls.Add(std_pic, 0, 1);
            layout.Controls.Add(upload, 0, 2);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Fee setup",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 3);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "The selected class creates the opening tuition fee and payment balance.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = false
            }, 0, 4);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Use a clear portrait photo for easier identification in student records.",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomLeft,
                AutoEllipsis = false
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
                UseMnemonic = false,
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
                Padding = new Padding(0, 0, 12, 10),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.75F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            input.Dock = DockStyle.Fill;
            input.Height = 36;
            panel.Controls.Add(label, 0, 0);
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
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            actions.Controls.Add(CreateSecondaryButton("New", async () => await NewStudent()), 0, 0);
            actions.Controls.Add(CreatePrimaryButton("Save", SaveStudent), 1, 0);
            actions.Controls.Add(CreateSecondaryButton("Update", UpdateStudent), 2, 0);
            actions.Controls.Add(CreateDangerButton("Roll Out", RollOutStudent), 3, 0);
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

        private async void frmAddStd_Load(object sender, EventArgs e)
        {
            await SetNextStudentId();
        }

        private async System.Threading.Tasks.Task SetNextStudentId()
        {
            try
            {
                txtStdID.Text = await _studentService.GenerateNextStudentIdAsync();
                statusLabel.Text = "Ready for a new admission.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Could not prepare the next student ID.";
                UIHelper.ShowError("Error: " + ex.Message, "Student Admission");
            }
        }

        public void upload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (new System.IO.FileInfo(dialog.FileName).Length > AppConfig.MaxPhotoSizeBytes)
                        {
                            UIHelper.ShowWarning($"Photo exceeds maximum size of {AppConfig.MaxPhotoSizeMB}MB.");
                            return;
                        }

                        using (Image selectedImage = Image.FromFile(dialog.FileName))
                        {
                            Image previousImage = std_pic.Image;
                            std_pic.Image = new Bitmap(selectedImage);
                            previousImage?.Dispose();
                        }

                        statusLabel.Text = "Photo selected.";
                    }
                    catch (Exception ex)
                    {
                        statusLabel.Text = "Photo was not accepted.";
                        UIHelper.ShowWarning(ex.Message, "Student Admission");
                    }
                }
            }
        }

        private Student MapFormToStudent()
        {
            return new Student
            {
                StudentID = txtStdID.Text,
                FirstName = txtFN.Text.Trim(),
                LastName = txtLN.Text.Trim(),
                DateOfBirth = dateDOB.Value.Date,
                Gender = cmbGN.Text.Trim(),
                Email = txtEM.Text.Trim(),
                ClassID = cmbCID.Text.Trim(),
                HomeTown = txtHT.Text.Trim(),
                Residence = txtRD.Text.Trim(),
                Allergies = txtAG.Text.Trim(),
                EmergencyContact = txtEC.Text.Trim(),
                GuardianName = txtGN.Text.Trim(),
                GuardianEmail = txtGE.Text.Trim(),
                GuardianLocation = txtGL.Text.Trim(),
                AdmissionDate = dateAD.Value.Date,
                ProfilePhoto = ImageHelper.ImageToBytes(std_pic.Image)
            };
        }

        private async void SaveStudent()
        {
            statusLabel.Text = "Saving student...";
            var student = MapFormToStudent();
            var (success, message) = await _studentService.AddStudentAsync(student);

            if (success)
            {
                txtStdID.Text = student.StudentID;
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Student Admission");
            }
            else
            {
                statusLabel.Text = "Save failed.";
                UIHelper.ShowWarning(message, "Student Admission");
            }
        }

        private async void UpdateStudent()
        {
            if (string.IsNullOrWhiteSpace(txtStdID.Text))
            {
                UIHelper.ShowWarning("Please select a student to update.", "Student Admission");
                return;
            }

            statusLabel.Text = "Updating student...";
            var student = MapFormToStudent();
            var (success, message) = await _studentService.UpdateStudentAsync(student);

            if (success)
            {
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Student Admission");
            }
            else
            {
                statusLabel.Text = "Update failed.";
                UIHelper.ShowWarning(message, "Student Admission");
            }
        }

        private async void RollOutStudent()
        {
            if (string.IsNullOrWhiteSpace(txtStdID.Text))
            {
                UIHelper.ShowWarning("Please select a student to roll out.", "Student Admission");
                return;
            }

            if (UIHelper.ShowConfirmation("Roll out this student and remove them from active students?", "Student Admission") != DialogResult.Yes)
            {
                return;
            }

            statusLabel.Text = "Rolling out student...";
            var (success, message) = await _studentService.DeleteStudentAsync(txtStdID.Text);

            if (success)
            {
                statusLabel.Text = message;
                UIHelper.ShowSuccess(message, "Student Admission");
                await NewStudent();
            }
            else
            {
                statusLabel.Text = "Roll out failed.";
                UIHelper.ShowWarning(message, "Student Admission");
            }
        }

        private async System.Threading.Tasks.Task NewStudent()
        {
            txtFN.Text = "";
            txtLN.Text = "";
            txtEM.Text = "";
            txtHT.Text = "";
            txtRD.Text = "";
            txtAG.Text = "";
            txtEC.Text = "";
            txtGN.Text = "";
            txtGE.Text = "";
            txtGL.Text = "";
            cmbGN.SelectedIndex = 0;
            cmbCID.SelectedIndex = 0;
            dateDOB.Value = DateTime.Today.AddYears(-5);
            dateAD.Value = DateTime.Today;
            std_pic.Image = null;
            await SetNextStudentId();
        }

        private async void btnNew_Click(object sender, EventArgs e) { await NewStudent(); }
        private void btnSave_Click_1(object sender, EventArgs e) { SaveStudent(); }
        private void btn_Update_Click(object sender, EventArgs e) { UpdateStudent(); }
        private void btnDel_Click(object sender, EventArgs e) { RollOutStudent(); }
        private void btnEdit_Click(object sender, EventArgs e) { }
        private void txtStdID_TextChanged(object sender, EventArgs e) { }
        private void gunaButton1_Click(object sender, EventArgs e) { Close(); new frmStdView().Show(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Application.Exit(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { new frmAddStd().Show(); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { new frmEmployee().Show(); }
        private void classToolStripMenuItem_Click(object sender, EventArgs e) { new EXAMS().Show(); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { new frmStdView().Show(); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { new frmEmpView().Show(); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { new frmFessPayment().Show(); }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) { new frmAbout().Show(); }

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
            // Check standard controls
            if (controlType == typeof(TextBox) ||
                   controlType == typeof(ComboBox) ||
                   controlType == typeof(Button) ||
                   controlType == typeof(CheckBox) ||
                   controlType == typeof(RadioButton) ||
                   controlType == typeof(DataGridView) ||
                   controlType == typeof(ListBox) ||
                   controlType == typeof(TreeView) ||
                   controlType == typeof(RichTextBox) ||
                   controlType.Name.Contains("NumericUpDown") ||
                   controlType.Name.Contains("DateTimePicker"))
            {
                return true;
            }

            // Check Guna2 UI components
            string typeName = controlType.Name;
            return typeName.Contains("Guna2TextBox") ||
                   typeName.Contains("Guna2ComboBox") ||
                   typeName.Contains("Guna2Button") ||
                   typeName.Contains("Guna2DateTimePicker") ||
                   typeName.Contains("Guna2CheckBox") ||
                   typeName.Contains("Guna2RadioButton") ||
                   typeName.Contains("Guna2DataGridView");
        }

    }
}

