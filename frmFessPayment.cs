using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmFessPayment : Form
    {
        private readonly kum Aikins = new kum();
        private TextBox studentIdBox;
        private TextBox studentNameBox;
        private TextBox classBox;
        private TextBox balanceBox;
        private TextBox amountBox;
        private TextBox bursarBox;
        private ComboBox paymentModeBox;
        private DateTimePicker paymentDatePicker;
        private DataGridView paymentGrid;
        private Label statusLabel;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SidebarBackColor = UiTheme.Navy;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color AccentColor = UiTheme.GoldSoft;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        private const string PaymentHistoryQuery = @"
SELECT
    [StudentID] AS [STUDENT ID],
    [ClassID] AS [CLASS ID],
    [student_name] AS [STUDENT NAME],
    [Amount_paid] AS [AMOUNT PAID],
    [Balance] AS [BALANCE],
    [Date] AS [PAYMENT DATE],
    [tm] AS [PAYMENT TIME],
    [payment_mode] AS [PAYMENT MODE],
    [Bursor_name] AS [BURSAR NAME]
FROM [payment_record]
ORDER BY [Date] DESC, [tm] DESC";

        public frmFessPayment()
        {
            InitializeComponent();
            BuildModernPaymentView();
        }

        private void BuildModernPaymentView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Fees Payment";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1120, 700);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildPaymentPanel(), 0, 1);
            root.Controls.Add(BuildHistoryPanel(), 0, 2);

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
                Text = "Fees Payment",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Text = "Look up a student, record payment, and review payment history",
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
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () =>
            {
                Close();
                new frmDashboard().Show();
            }));
            actions.Controls.Add(CreateSecondaryButton("Refresh", LoadPaymentHistory));

            header.Controls.Add(titleBlock, 0, 0);
            header.Controls.Add(actions, 1, 0);
            return header;
        }

        private Control BuildPaymentPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(18)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                BackColor = SurfaceColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (int i = 0; i < 4; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            }

            studentIdBox = CreateTextBox();
            studentNameBox = CreateTextBox(true);
            classBox = CreateTextBox(true);
            balanceBox = CreateTextBox(true);
            amountBox = CreateTextBox();
            bursarBox = CreateTextBox();
            paymentModeBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            paymentModeBox.Items.AddRange(new object[] { "Cash", "Mobile Money", "Bank Transfer", "Cheque" });
            paymentModeBox.SelectedIndex = 0;
            paymentDatePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10F)
            };

            studentIdBox.TextChanged += (sender, args) => LookupStudent();

            layout.Controls.Add(CreateField("Student ID", studentIdBox), 0, 0);
            layout.Controls.Add(CreateField("Student Name", studentNameBox), 1, 0);
            layout.Controls.Add(CreateField("Class", classBox), 2, 0);
            layout.Controls.Add(CreateField("Balance", balanceBox), 3, 0);
            layout.Controls.Add(CreateField("Amount Paid", amountBox), 0, 1);
            layout.Controls.Add(CreateField("Payment Mode", paymentModeBox), 1, 1);
            layout.Controls.Add(CreateField("Bursar Name", bursarBox), 2, 1);
            layout.Controls.Add(CreateField("Payment Date", paymentDatePicker), 3, 1);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F)
            };
            layout.Controls.Add(statusLabel, 0, 2);
            layout.SetColumnSpan(statusLabel, 2);
            layout.Controls.Add(CreatePrimaryButton("Record Payment", RecordPayment), 2, 2);
            layout.Controls.Add(CreateSecondaryButton("Clear", ClearPaymentForm), 3, 2);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildHistoryPanel()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(1)
            };

            paymentGrid = new DataGridView
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
            paymentGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            paymentGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            paymentGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            paymentGrid.DefaultCellStyle.BackColor = SurfaceColor;
            paymentGrid.DefaultCellStyle.ForeColor = TextColor;
            paymentGrid.DefaultCellStyle.SelectionBackColor = AccentColor;
            paymentGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            paymentGrid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
            paymentGrid.GridColor = BorderColor;

            shell.Controls.Add(paymentGrid);
            return shell;
        }

        private Control CreateField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0, 0, 12, 8),
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
            input.Height = 32;
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private TextBox CreateTextBox(bool readOnly = false)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = readOnly,
                BackColor = readOnly ? UiTheme.SurfaceAlt : SurfaceColor
            };
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

        private Button CreateButton(string text, Action action)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
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

        private void LookupStudent()
        {
            if (studentIdBox == null || !int.TryParse(studentIdBox.Text.Trim(), out int studentId))
            {
                studentNameBox.Text = "";
                classBox.Text = "";
                balanceBox.Text = "";
                return;
            }

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                {
                    con.Open();
                    if (!LoadLatestPaymentBalance(con, studentId))
                    {
                        LoadStudentDefaultBalance(con, studentId);
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Lookup failed";
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private bool LoadLatestPaymentBalance(OleDbConnection con, int studentId)
        {
            string query = @"
SELECT TOP 1 [StudentID], [student_name], [classID], [Balance]
FROM [payment_record]
WHERE [StudentID] = ?
ORDER BY [Date] DESC, [tm] DESC";

            using (OleDbCommand command = new OleDbCommand(query, con))
            {
                command.Parameters.AddWithValue("?", studentId);
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return false;
                    }

                    studentNameBox.Text = reader["student_name"].ToString();
                    classBox.Text = reader["classID"].ToString();
                    balanceBox.Text = Convert.ToDecimal(reader["Balance"]).ToString("0.00");
                    statusLabel.Text = "Loaded latest balance";
                    return true;
                }
            }
        }

        private void LoadStudentDefaultBalance(OleDbConnection con, int studentId)
        {
            string studentQuery = @"
SELECT [FirstName], [LastName], [ClassID]
FROM [Students]
WHERE [StudentID] = ?";

            using (OleDbCommand studentCommand = new OleDbCommand(studentQuery, con))
            {
                studentCommand.Parameters.AddWithValue("?", studentId);
                using (OleDbDataReader studentReader = studentCommand.ExecuteReader())
                {
                    if (!studentReader.Read())
                    {
                        studentNameBox.Text = "";
                        classBox.Text = "";
                        balanceBox.Text = "";
                        statusLabel.Text = "Student not found";
                        return;
                    }

                    studentNameBox.Text = studentReader["FirstName"] + " " + studentReader["LastName"];
                    classBox.Text = studentReader["ClassID"].ToString();
                }
            }

            balanceBox.Text = GetDefaultFeeBalance(con, studentId, classBox.Text).ToString("0.00");
            statusLabel.Text = "Loaded student fee balance";
        }

        private decimal GetDefaultFeeBalance(OleDbConnection con, int studentId, string classId)
        {
            if (string.Equals(classId, "BASIC 9", StringComparison.OrdinalIgnoreCase))
            {
                return 1200m;
            }

            using (OleDbCommand feeCommand = new OleDbCommand("SELECT TOP 1 [Amount] FROM [fees] WHERE [StudentID] = ? AND [ClassID] = ?", con))
            {
                feeCommand.Parameters.AddWithValue("?", studentId);
                feeCommand.Parameters.AddWithValue("?", classId);
                object result = feeCommand.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        private void RecordPayment()
        {
            if (!int.TryParse(studentIdBox.Text.Trim(), out int studentId))
            {
                MessageBox.Show("Enter a valid student ID.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(balanceBox.Text, out decimal currentBalance) || !decimal.TryParse(amountBox.Text, out decimal amountPaid))
            {
                MessageBox.Show("Enter a valid amount.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal newBalance = Math.Max(0m, currentBalance - amountPaid);

            string query = @"
INSERT INTO dbo.payment_record
    (StudentID, classID, FeeName, Balance, student_name, Amount_paid, [Date], tm, payment_mode, Bursor_name)
VALUES
    (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand(query, con))
                {
                    command.Parameters.AddWithValue("@StudentID", studentId);
                    command.Parameters.AddWithValue("@classID", classBox.Text);
                    command.Parameters.AddWithValue("@FeeName", "School Fees");
                    command.Parameters.AddWithValue("@Balance", newBalance);
                    command.Parameters.AddWithValue("@student_name", studentNameBox.Text);
                    command.Parameters.AddWithValue("@Amount_paid", amountPaid);
                    command.Parameters.AddWithValue("@Date", paymentDatePicker.Value.Date);
                    command.Parameters.AddWithValue("@tm", DateTime.Now.ToString("HH:mm:ss"));
                    command.Parameters.AddWithValue("@payment_mode", paymentModeBox.Text);
                    command.Parameters.AddWithValue("@Bursor_name", bursarBox.Text);

                    con.Open();
                    command.ExecuteNonQuery();
                }

                balanceBox.Text = newBalance.ToString("0.00");
                amountBox.Text = "";
                statusLabel.Text = "Payment recorded";
                LoadPaymentHistory();
                MessageBox.Show("Payment recorded successfully.", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Payment could not be recorded: " + ex.Message, "Payment", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPaymentHistory()
        {
            try
            {
                using (OleDbConnection con = new OleDbConnection(Aikins.constr))
                using (OleDbCommand command = new OleDbCommand(PaymentHistoryQuery, con))
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    paymentGrid.DataSource = table;
                    statusLabel.Text = table.Rows.Count + " payment record(s)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearPaymentForm()
        {
            studentIdBox.Text = "";
            studentNameBox.Text = "";
            classBox.Text = "";
            balanceBox.Text = "";
            amountBox.Text = "";
            bursarBox.Text = "";
            paymentModeBox.SelectedIndex = 0;
            paymentDatePicker.Value = DateTime.Today;
            statusLabel.Text = "";
        }

        private void frmFessPayment_Load(object sender, EventArgs e)
        {
            LoadPaymentHistory();
        }

        private void txtStdID_TextChanged(object sender, EventArgs e) { LookupStudent(); }
        private void pay_Click(object sender, EventArgs e) { RecordPayment(); }
        private void btn_Re_Click(object sender, EventArgs e) { LoadPaymentHistory(); }
        private void gunaDateTimePicker1_ValueChanged(object sender, EventArgs e) { }
        private void guna2TextBox8_TextChanged(object sender, EventArgs e) { }
        private void txtpm_TextChanged(object sender, EventArgs e) { }
        private void gunaPictureBox1_Click(object sender, EventArgs e) { Application.Exit(); }
        private void gunaPictureBox2_Click(object sender, EventArgs e) { WindowState = FormWindowState.Minimized; }
        private void gunaPictureBox3_Click(object sender, EventArgs e) { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
        private void gunaPictureBox1_Click_1(object sender, EventArgs e) { Close(); new frmDashboard().Show(); }
        private void studentsToolStripMenuItem_Click(object sender, EventArgs e) { new frmAddStd().Show(); }
        private void employersToolStripMenuItem_Click(object sender, EventArgs e) { new frmEmployee().Show(); }
        private void classToolStripMenuItem_Click(object sender, EventArgs e) { new EXAMS().Show(); }
        private void studentsToolStripMenuItem1_Click(object sender, EventArgs e) { new frmStdView().Show(); }
        private void employersToolStripMenuItem1_Click(object sender, EventArgs e) { new frmEmpView().Show(); }
        private void classToolStripMenuItem1_Click(object sender, EventArgs e) { new EXAMSVIEW().Show(); }
        private void makePaymentToolStripMenuItem_Click(object sender, EventArgs e) { new frmFessPayment().Show(); }
    }
}
