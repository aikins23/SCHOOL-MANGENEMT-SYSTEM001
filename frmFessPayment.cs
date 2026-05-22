using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmFessPayment : Form
    {
        private readonly StudentService _studentService;
        private readonly IFeeRepository _feeRepository;

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

        public frmFessPayment()
        {
            InitializeComponent();

            // Initialize modern architecture
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            _feeRepository = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, _feeRepository);

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
            actions.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadPaymentHistory()));

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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            UiTheme.StyleDataGrid(paymentGrid);
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

        private async void LookupStudent()
        {
            if (studentIdBox == null || string.IsNullOrWhiteSpace(studentIdBox.Text))
            {
                studentNameBox.Text = "";
                classBox.Text = "";
                balanceBox.Text = "";
                return;
            }

            try
            {
                string studentId = studentIdBox.Text.Trim();
                var student = await _studentService.GetStudentAsync(studentId);
                
                if (student == null)
                {
                    studentNameBox.Text = "";
                    classBox.Text = "";
                    balanceBox.Text = "";
                    statusLabel.Text = "Student not found";
                    return;
                }

                studentNameBox.Text = student.FullName;
                classBox.Text = student.ClassID;

                // Try to get latest payment balance, otherwise default fee
                decimal? balance = await _feeRepository.GetLatestBalanceAsync(studentId);
                if (!balance.HasValue)
                {
                    balance = await _feeRepository.GetDefaultBalanceAsync(studentId, student.ClassID);
                }

                balanceBox.Text = (balance ?? 0m).ToString("0.00");
                statusLabel.Text = "Student details loaded";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Lookup failed";
                UIHelper.ShowError("Lookup error: " + ex.Message, "Payment");
            }
        }

        private async void RecordPayment()
        {
            if (string.IsNullOrWhiteSpace(studentIdBox.Text))
            {
                UIHelper.ShowWarning("Enter a student ID.", "Payment");
                return;
            }

            if (!decimal.TryParse(balanceBox.Text, out decimal currentBalance) || !decimal.TryParse(amountBox.Text, out decimal amountPaid))
            {
                UIHelper.ShowWarning("Enter a valid payment amount.", "Payment");
                return;
            }

            if (amountPaid <= 0)
            {
                UIHelper.ShowWarning("Payment amount must be greater than zero.", "Payment");
                return;
            }

            decimal newBalance = Math.Max(0m, currentBalance - amountPaid);
            statusLabel.Text = "Recording payment...";

            try
            {
                bool success = await _feeRepository.AddPaymentRecordAsync(
                    studentIdBox.Text.Trim(),
                    classBox.Text,
                    studentNameBox.Text,
                    amountPaid,
                    newBalance,
                    paymentModeBox.Text,
                    bursarBox.Text,
                    paymentDatePicker.Value.Date
                );

                if (success)
                {
                    balanceBox.Text = newBalance.ToString("0.00");
                    amountBox.Text = "";
                    statusLabel.Text = "Payment recorded";
                    await LoadPaymentHistory();
                    UIHelper.ShowSuccess("Payment recorded successfully.", "Payment");
                }
                else
                {
                    statusLabel.Text = "Payment failed";
                    UIHelper.ShowError("Could not save payment record.", "Payment");
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Payment error";
                UIHelper.ShowError("Error: " + ex.Message, "Payment");
            }
        }

        private async System.Threading.Tasks.Task LoadPaymentHistory()
        {
            try
            {
                statusLabel.Text = "Refreshing history...";
                DataTable table = await _feeRepository.GetPaymentHistoryTableAsync();
                paymentGrid.DataSource = table;
                statusLabel.Text = table.Rows.Count + " payment record(s)";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("History refresh error: " + ex.Message, "Payment");
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
            statusLabel.Text = "Ready.";
        }

        private async void frmFessPayment_Load(object sender, EventArgs e)
        {
            await LoadPaymentHistory();
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
