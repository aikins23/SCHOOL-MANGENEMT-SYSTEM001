using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Mail;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmAddStd : Form
    {
        private readonly kum Aikins = new kum();
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
            cmbGN.Items.AddRange(Common.AppConfig.GenderOptions);
            cmbGN.SelectedIndex = 0;

            cmbCID.Items.Clear();
            cmbCID.Items.AddRange(Common.AppConfig.ClassNames);
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

            actions.Controls.Add(CreateSecondaryButton("New", NewStudent), 0, 0);
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

        private void frmAddStd_Load(object sender, EventArgs e)
        {
            SetNextStudentId();
        }

        private void SetNextStudentId()
        {
            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand("SELECT ISNULL(MAX(StudentID), 0) + 1 FROM Students", con))
                {
                    con.Open();
                    txtStdID.Text = command.ExecuteScalar().ToString();
                    statusLabel.Text = "Ready for a new admission.";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Could not prepare the next student ID.";
                MessageBox.Show("Error: " + ex.Message, "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        Common.ImageHelper.ConvertImageToBytes(dialog.FileName);
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
                        MessageBox.Show(ex.Message, "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        public byte[] Picture()
        {
            return Common.ImageHelper.ImageToBytes(std_pic.Image) ?? new byte[0];
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtFN.Text) || string.IsNullOrWhiteSpace(txtLN.Text))
            {
                MessageBox.Show("Enter the student's first and last name.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbCID.Text) || string.IsNullOrWhiteSpace(cmbGN.Text))
            {
                MessageBox.Show("Select the student's class and gender.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtHT.Text) || string.IsNullOrWhiteSpace(txtRD.Text))
            {
                MessageBox.Show("Enter the student's home town and residence.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEC.Text))
            {
                MessageBox.Show("Enter an emergency contact.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtGN.Text) || string.IsNullOrWhiteSpace(txtGL.Text))
            {
                MessageBox.Show("Enter the guardian name and location.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(txtStdID.Text, out _))
            {
                MessageBox.Show("Student ID is not valid.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int age = GetAge(dateDOB.Value.Date);
            if (age < Common.AppConfig.MinStudentAge || age > Common.AppConfig.MaxStudentAge)
            {
                MessageBox.Show($"Student age must be between {Common.AppConfig.MinStudentAge} and {Common.AppConfig.MaxStudentAge} years.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (dateAD.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Admission date cannot be in the future.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!IsValidOptionalEmail(txtEM.Text))
            {
                MessageBox.Show("Student email is not valid.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!IsValidOptionalEmail(txtGE.Text))
            {
                MessageBox.Show("Guardian email is not valid.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private int GetAge(DateTime dateOfBirth)
        {
            int age = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        private bool IsValidOptionalEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            try
            {
                var address = new MailAddress(email.Trim());
                return address.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        private decimal GetFeeForClass()
        {
            switch (cmbCID.Text.Trim().ToUpperInvariant())
            {
                case "CRECHE":
                    return 2000m;
                case "NURSERY 1":
                    return 3450m;
                case "NURSERY 2":
                    return 3750m;
                case "KINDERGARTEN 1":
                    return 3654m;
                case "KINDERGARTEN 2":
                case "BASIC 1":
                case "BASIC 2":
                case "BASIC 3":
                case "BASIC 4":
                case "BASIC 5":
                case "BASIC 6":
                case "BASIC 7":
                case "BASIC 8":
                    return 2423m;
                default:
                    return 1200m;
            }
        }

        private void SaveStudent()
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                int studentId;
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    using (OleDbTransaction transaction = con.BeginTransaction())
                    {
                        studentId = InsertStudent(con, transaction);
                        SaveFeeRecords(con, transaction, studentId);
                        transaction.Commit();
                    }
                }

                txtStdID.Text = studentId.ToString();
                statusLabel.Text = "Student saved with opening fee record.";
                MessageBox.Show("Student and fee record added successfully.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int InsertStudent(OleDbConnection con, OleDbTransaction transaction)
        {
            string query = @"
INSERT INTO Students
    (FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, Std_pic)
VALUES
    (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (OleDbCommand command = new OleDbCommand(query, con, transaction))
            {
                AddStudentParameters(command, includeStudentId: false);
                command.ExecuteNonQuery();
            }

            using (OleDbCommand identityCommand = new OleDbCommand("SELECT @@IDENTITY", con, transaction))
            {
                object result = identityCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int identity))
                {
                    return identity;
                }
            }

            return Convert.ToInt32(txtStdID.Text);
        }

        private void UpdateStudent()
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    using (OleDbTransaction transaction = con.BeginTransaction())
                    {
                        string query = @"
UPDATE Students
SET FirstName = ?, LastName = ?, DOB = ?, Gender = ?, Email = ?, ClassID = ?, HomeTown = ?, Residence = ?, Allegies = ?, EmergencyConatct = ?, GuidanceName = ?, GuidianceEmail = ?, Guidiance_Location = ?, admission_date = ?, Std_pic = ?
WHERE StudentID = ?";

                        using (OleDbCommand command = new OleDbCommand(query, con, transaction))
                        {
                            AddStudentParameters(command, includeStudentId: true);
                            int rows = command.ExecuteNonQuery();
                            if (rows == 0)
                            {
                                transaction.Rollback();
                                statusLabel.Text = "No matching student found.";
                                MessageBox.Show("No matching student found.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        SaveFeeRecords(con, transaction, Convert.ToInt32(txtStdID.Text), updateExisting: true);
                        transaction.Commit();
                    }
                }

                statusLabel.Text = "Student record updated.";
                MessageBox.Show("Student and fee records updated successfully.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddStudentParameters(OleDbCommand command, bool includeStudentId)
        {
            command.Parameters.AddWithValue("?", txtFN.Text.Trim());
            command.Parameters.AddWithValue("?", txtLN.Text.Trim());
            command.Parameters.AddWithValue("?", dateDOB.Value.Date);
            command.Parameters.AddWithValue("?", cmbGN.Text.Trim());
            command.Parameters.AddWithValue("?", txtEM.Text.Trim());
            command.Parameters.AddWithValue("?", cmbCID.Text.Trim());
            command.Parameters.AddWithValue("?", txtHT.Text.Trim());
            command.Parameters.AddWithValue("?", txtRD.Text.Trim());
            command.Parameters.AddWithValue("?", txtAG.Text.Trim());
            command.Parameters.AddWithValue("?", txtEC.Text.Trim());
            command.Parameters.AddWithValue("?", txtGN.Text.Trim());
            command.Parameters.AddWithValue("?", txtGE.Text.Trim());
            command.Parameters.AddWithValue("?", txtGL.Text.Trim());
            command.Parameters.AddWithValue("?", dateAD.Value.Date);
            command.Parameters.AddWithValue("?", Picture());

            if (includeStudentId)
            {
                command.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
            }
        }

        private void SaveFeeRecords(OleDbConnection con, OleDbTransaction transaction, int studentId, bool updateExisting = false)
        {
            decimal fee = GetFeeForClass();
            string studentName = txtLN.Text.Trim() + " " + txtFN.Text.Trim();

            if (updateExisting)
            {
                using (OleDbCommand feeCommand = new OleDbCommand("UPDATE dbo.fees SET ClassID = ?, FeeName = ?, Amount = ? WHERE StudentID = ?", con, transaction))
                {
                    feeCommand.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                    feeCommand.Parameters.AddWithValue("?", "Tuition Fee");
                    feeCommand.Parameters.AddWithValue("?", fee);
                    feeCommand.Parameters.AddWithValue("?", studentId);
                    feeCommand.ExecuteNonQuery();
                }

                using (OleDbCommand paymentCommand = new OleDbCommand("UPDATE dbo.payment_record SET classID = ?, FeeName = ?, Balance = ?, student_name = ? WHERE StudentID = ?", con, transaction))
                {
                    paymentCommand.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                    paymentCommand.Parameters.AddWithValue("?", "SCHOOLFEES");
                    paymentCommand.Parameters.AddWithValue("?", fee);
                    paymentCommand.Parameters.AddWithValue("?", studentName);
                    paymentCommand.Parameters.AddWithValue("?", studentId);
                    paymentCommand.ExecuteNonQuery();
                }

                return;
            }

            using (OleDbCommand feeCommand = new OleDbCommand("INSERT INTO dbo.fees (StudentID, ClassID, FeeName, Amount) VALUES (?, ?, ?, ?)", con, transaction))
            {
                feeCommand.Parameters.AddWithValue("?", studentId);
                feeCommand.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                feeCommand.Parameters.AddWithValue("?", "Tuition Fee");
                feeCommand.Parameters.AddWithValue("?", fee);
                feeCommand.ExecuteNonQuery();
            }

            using (OleDbCommand paymentCommand = new OleDbCommand("INSERT INTO dbo.payment_record (StudentID, classID, FeeName, Balance, student_name, Amount_paid, [Date]) VALUES (?, ?, ?, ?, ?, ?, ?)", con, transaction))
            {
                paymentCommand.Parameters.AddWithValue("?", studentId);
                paymentCommand.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                paymentCommand.Parameters.AddWithValue("?", "SCHOOLFEES");
                paymentCommand.Parameters.AddWithValue("?", fee);
                paymentCommand.Parameters.AddWithValue("?", studentName);
                paymentCommand.Parameters.AddWithValue("?", 0);
                paymentCommand.Parameters.AddWithValue("?", DateTime.Today);
                paymentCommand.ExecuteNonQuery();
            }
        }

        private void RollOutStudent()
        {
            if (!ValidateForm())
            {
                return;
            }

            DialogResult result = MessageBox.Show("Roll out this student and remove them from active students?", "Student Admission", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    using (OleDbTransaction transaction = con.BeginTransaction())
                    {
                        string insertQuery = @"
INSERT INTO Rolled_Out_Students
    (StudentID, FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, [date], Std_pic)
VALUES
    (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        using (OleDbCommand command = new OleDbCommand(insertQuery, con, transaction))
                        {
                            command.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
                            command.Parameters.AddWithValue("?", txtFN.Text.Trim());
                            command.Parameters.AddWithValue("?", txtLN.Text.Trim());
                            command.Parameters.AddWithValue("?", dateDOB.Value.Date);
                            command.Parameters.AddWithValue("?", cmbGN.Text.Trim());
                            command.Parameters.AddWithValue("?", txtEM.Text.Trim());
                            command.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                            command.Parameters.AddWithValue("?", txtHT.Text.Trim());
                            command.Parameters.AddWithValue("?", txtRD.Text.Trim());
                            command.Parameters.AddWithValue("?", txtAG.Text.Trim());
                            command.Parameters.AddWithValue("?", txtEC.Text.Trim());
                            command.Parameters.AddWithValue("?", txtGN.Text.Trim());
                            command.Parameters.AddWithValue("?", txtGE.Text.Trim());
                            command.Parameters.AddWithValue("?", txtGL.Text.Trim());
                            command.Parameters.AddWithValue("?", dateAD.Value.Date);
                            command.Parameters.AddWithValue("?", DateTime.Today);
                            command.Parameters.AddWithValue("?", Picture());
                            command.ExecuteNonQuery();
                        }

                        using (OleDbCommand deleteCommand = new OleDbCommand("DELETE FROM Students WHERE StudentID = ?", con, transaction))
                        {
                            deleteCommand.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
                            deleteCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                statusLabel.Text = "Student rolled out.";
                MessageBox.Show("Student rolled out successfully.", "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NewStudent();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Student Admission", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NewStudent()
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
            SetNextStudentId();
        }

        private void btnNew_Click(object sender, EventArgs e) { NewStudent(); }
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

