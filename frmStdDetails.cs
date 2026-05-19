using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmStdDetails : Form
    {
        private readonly kum Aikins = new kum();
        private readonly DataTable data;
        private Label statusLabel;

        private static readonly Color PageBackColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color PrimaryColor = Color.FromArgb(31, 99, 198);
        private static readonly Color DangerColor = Color.FromArgb(190, 18, 60);
        private static readonly Color TextColor = Color.FromArgb(25, 36, 49);
        private static readonly Color MutedTextColor = Color.FromArgb(93, 108, 123);
        private static readonly Color BorderColor = Color.FromArgb(219, 226, 236);

        private readonly string[] classNames =
        {
            "CRECHE", "NURSERY 1", "NURSERY 2", "KINDERGARTEN 1", "KINDERGARTEN 2",
            "BASIC 1", "BASIC 2", "BASIC 3", "BASIC 4", "BASIC 5", "BASIC 6", "BASIC 7", "BASIC 8", "BASIC 9"
        };

        public frmStdDetails()
            : this(new DataTable())
        {
        }

        public frmStdDetails(DataTable data)
        {
            InitializeComponent();
            this.data = data;
            BuildModernStudentDetailsView();
        }

        private void BuildModernStudentDetailsView()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "Student Details";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F);
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
            txtStdID.ReadOnly = true;

            foreach (Control control in new Control[] { txtStdID, txtFN, txtLN, txtEM, txtHT, txtRD, txtAG, txtEC, txtGN, txtGE, txtGL, cmbGN, cmbCID, dateDOB, dateAD })
            {
                StyleInput(control);
            }

            txtStdID.FillColor = UiTheme.SurfaceAlt;
            ResetCombo(cmbGN, new object[] { "MALE", "FEMALE" });
            ResetCombo(cmbCID, classNames);
            dateDOB.Value = DateTime.Today.AddYears(-5);
            dateAD.Value = DateTime.Today;
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
                textBox.PlaceholderForeColor = MutedTextColor;
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
            title.Controls.Add(new Label { Dock = DockStyle.Top, Height = 38, Text = "Student Details", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
            title.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Review, update, report, or roll out a student record", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.MiddleLeft });
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = PageBackColor, Padding = new Padding(0, 12, 0, 0) };
            actions.Controls.Add(CreatePrimaryButton("Student List", () => { Close(); new frmStdView().Show(); }));
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
            detailsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
            detailsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            detailsStack.Controls.Add(BuildLearnerPanel(), 0, 0);
            detailsStack.Controls.Add(BuildGuardianPanel(), 0, 1);
            body.Controls.Add(detailsStack, 0, 0);
            body.Controls.Add(BuildPhotoPanel(), 1, 0);
            return body;
        }

        private Control BuildLearnerPanel()
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
            var allergies = CreateField("Allergies / Medical Notes", txtAG);
            layout.Controls.Add(allergies, 0, 3);
            layout.SetColumnSpan(allergies, 2);
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
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Fee setup", ForeColor = TextColor, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft }, 0, 3);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "The selected class keeps tuition fees and payment balances aligned.", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.TopLeft }, 0, 4);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Use a clear portrait photo for reports and quick identification.", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.TopLeft }, 0, 5);
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
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, BackColor = PageBackColor };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.Controls.Add(CreatePrimaryButton("Update", UpdateStudent), 0, 0);
            actions.Controls.Add(CreateSecondaryButton("Report", ExportPdf), 1, 0);
            actions.Controls.Add(CreateDangerButton("Roll Out", RollOutStudent), 2, 0);
            actions.Controls.Add(CreateSecondaryButton("Student List", () => { Close(); new frmStdView().Show(); }), 3, 0);
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

        private void frmStdDetails_Load(object sender, EventArgs e)
        {
            PopulateFromData();
        }

        private void PopulateFromData()
        {
            if (data == null || data.Rows.Count == 0)
            {
                statusLabel.Text = "No student data loaded.";
                return;
            }

            DataRow row = data.Rows[0];
            txtStdID.Text = row["StudentID"].ToString();
            txtFN.Text = row["FirstName"].ToString();
            txtLN.Text = row["LastName"].ToString();
            dateDOB.Value = SafeDate(row["DOB"], DateTime.Today.AddYears(-5));
            cmbGN.Text = row["Gender"].ToString();
            txtEM.Text = row["Email"].ToString();
            cmbCID.Text = row["ClassID"].ToString();
            txtHT.Text = row["HomeTown"].ToString();
            txtRD.Text = row["Residence"].ToString();
            txtAG.Text = row["Allegies"].ToString();
            txtEC.Text = row["EmergencyConatct"].ToString();
            txtGN.Text = row["GuidanceName"].ToString();
            txtGE.Text = row["GuidianceEmail"].ToString();
            txtGL.Text = row["Guidiance_Location"].ToString();
            dateAD.Value = SafeDate(row["admission_date"], DateTime.Today);

            if (row.Table.Columns.Contains("std_pic") && row["std_pic"] != DBNull.Value)
            {
                using (MemoryStream ms = new MemoryStream((byte[])row["std_pic"]))
                {
                    std_pic.Image = Image.FromStream(ms);
                }
            }
            statusLabel.Text = "Student record loaded.";
        }

        private DateTime SafeDate(object value, DateTime fallback)
        {
            return value == null || value == DBNull.Value || !DateTime.TryParse(value.ToString(), out DateTime parsed) ? fallback : parsed;
        }

        public void upload_Click(object sender, EventArgs e)
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

        public byte[] Picture()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Image image = std_pic.Image ?? CreateBlankPhoto();
                image.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private Image CreateBlankPhoto()
        {
            Bitmap bitmap = new Bitmap(1, 1);
            using (Graphics graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.White);
            return bitmap;
        }

        private decimal GetFeeForClass()
        {
            switch (cmbCID.Text.Trim().ToUpperInvariant())
            {
                case "CRECHE": return 2000m;
                case "NURSERY 1": return 3450m;
                case "NURSERY 2": return 3750m;
                case "KINDERGARTEN 1": return 3654m;
                case "KINDERGARTEN 2":
                case "BASIC 1":
                case "BASIC 2":
                case "BASIC 3":
                case "BASIC 4":
                case "BASIC 5":
                case "BASIC 6":
                case "BASIC 7":
                case "BASIC 8": return 2423m;
                default: return 1200m;
            }
        }

        private bool ValidateForm()
        {
            if (!int.TryParse(txtStdID.Text, out _))
            {
                MessageBox.Show("Student ID is not valid.", "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtFN.Text) || string.IsNullOrWhiteSpace(txtLN.Text))
            {
                MessageBox.Show("Enter first and last name.", "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void UpdateStudent()
        {
            if (!ValidateForm()) return;
            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    using (OleDbCommand command = new OleDbCommand("UPDATE Students SET FirstName=?, LastName=?, DOB=?, Gender=?, Email=?, ClassID=?, HomeTown=?, Residence=?, Allegies=?, EmergencyConatct=?, GuidanceName=?, GuidianceEmail=?, Guidiance_Location=?, admission_date=?, Std_pic=? WHERE StudentID=?", con))
                    {
                        AddStudentParameters(command, includeStudentId: true);
                        int rows = command.ExecuteNonQuery();
                        if (rows == 0)
                        {
                            MessageBox.Show("No matching student found.", "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    decimal fee = GetFeeForClass();
                    using (OleDbCommand feeCommand = new OleDbCommand("UPDATE dbo.fees SET ClassID=?, FeeName=?, Amount=? WHERE StudentID=?", con))
                    {
                        feeCommand.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                        feeCommand.Parameters.AddWithValue("?", "Tuition Fee");
                        feeCommand.Parameters.AddWithValue("?", fee);
                        feeCommand.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
                        feeCommand.ExecuteNonQuery();
                    }

                    using (OleDbCommand paymentCommand = new OleDbCommand("UPDATE dbo.payment_record SET classID=?, FeeName=?, Balance=?, student_name=? WHERE StudentID=?", con))
                    {
                        paymentCommand.Parameters.AddWithValue("?", cmbCID.Text.Trim());
                        paymentCommand.Parameters.AddWithValue("?", "Tuition Fee");
                        paymentCommand.Parameters.AddWithValue("?", fee);
                        paymentCommand.Parameters.AddWithValue("?", txtLN.Text.Trim() + " " + txtFN.Text.Trim());
                        paymentCommand.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
                        paymentCommand.ExecuteNonQuery();
                    }
                }
                statusLabel.Text = "Student updated.";
                MessageBox.Show("Student updated successfully.", "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (includeStudentId) command.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
        }

        private void RollOutStudent()
        {
            if (!ValidateForm()) return;
            if (MessageBox.Show("Roll out this student?", "Student Details", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    using (OleDbCommand command = new OleDbCommand("INSERT INTO Rolled_Out_Students(StudentID, FirstName, LastName, DOB, Gender, Email, ClassID, HomeTown, Residence, Allegies, EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location, admission_date, [date], Std_pic) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", con))
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
                    using (OleDbCommand deleteCommand = new OleDbCommand("DELETE FROM Students WHERE StudentID = ?", con))
                    {
                        deleteCommand.Parameters.AddWithValue("?", Convert.ToInt32(txtStdID.Text));
                        deleteCommand.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Student rolled out successfully.", "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                new frmStdView().Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportPdf()
        {
            try
            {
                string safeName = string.Join("_", (txtFN.Text + "_" + txtLN.Text).Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), safeName + "_StudentReport.pdf");
                PdfDocument pdf = new PdfDocument();
                PdfPage page = pdf.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont titleFont = new XFont("Arial", 16);
                XFont bodyFont = new XFont("Arial", 10);
                double y = 46;
                gfx.DrawString("Student Record", titleFont, XBrushes.Black, 40, y);
                y += 30;
                string[] lines =
                {
                    "Student ID: " + txtStdID.Text,
                    "Name: " + txtFN.Text + " " + txtLN.Text,
                    "Class: " + cmbCID.Text,
                    "Gender: " + cmbGN.Text,
                    "Date of Birth: " + dateDOB.Value.ToShortDateString(),
                    "Email: " + txtEM.Text,
                    "Residence: " + txtRD.Text,
                    "Guardian: " + txtGN.Text,
                    "Emergency Contact: " + txtEC.Text,
                    "Admission Date: " + dateAD.Value.ToShortDateString()
                };
                foreach (string line in lines)
                {
                    gfx.DrawString(line, bodyFont, XBrushes.Black, 40, y);
                    y += 18;
                }
                pdf.Save(filePath);
                pdf.Close();
                MessageBox.Show("PDF report saved to: " + filePath, "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating PDF: " + ex.Message, "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_Update_Click(object sender, EventArgs e) { UpdateStudent(); }
        private void btnDel_Click(object sender, EventArgs e) { RollOutStudent(); }
        private void btn_view_Click(object sender, EventArgs e) { ExportPdf(); }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Hide(); }
        private void gunaPictureBox6_Click(object sender, EventArgs e) { Close(); new frmStdView().Show(); }
        private void gunaButton1_Click(object sender, EventArgs e) { }
        private void pay_Click(object sender, EventArgs e) { new frmStdView().Show(); }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { new frmAddStd().Show(); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { new frmEmployee().Show(); }
        private void classToolStripMenuItem_Click(object sender, EventArgs e) { new EXAMS().Show(); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { new frmStdView().Show(); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { new frmEmpView().Show(); }
        private void classToolStripMenuItem1_Click(object sender, EventArgs e) { new EXAMSVIEW().Show(); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { new frmFessPayment().Show(); }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) { new frmAbout().Show(); }
    }
}
