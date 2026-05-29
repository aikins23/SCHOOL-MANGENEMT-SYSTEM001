using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Printing;
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
        private TextBox amountWordsBox;
        private TextBox beingBox;
        private TextBox bursarBox;
        private TextBox cashChequeBox;
        private ComboBox paymentModeBox;
        private DateTimePicker paymentDatePicker;
        private DataGridView paymentGrid;
        private Label statusLabel;
        private Label receiptNumberLabel;
        private ReceiptPrintData lastPrintedReceipt;

        // Wizard navigation
        private int _currentStep = 1;
        private string _lastFeeTypeAutoFilled = "";

        // Step container panels
        private Panel _stepContainer;
        private Panel _step1Panel;
        private Panel _step2Panel;
        private Panel _step3Panel;

        // Progress indicator
        private Panel _progressPanel;

        // Step 1
        private ComboBox feeTypeBox;
        private Panel _studentInfoCard;
        private Label _studentInfoNameLbl;
        private Label _studentInfoBalanceLbl;
        private Label _studentNotFoundLbl;
        private Button _continueToPaymentBtn;

        // Step 3 receipt preview value labels
        private Label _rpStudentIdLbl;
        private Label _rpClassLbl;
        private Label _rpNameLbl;
        private Label _rpAmountWordsLbl;
        private Label _rpBeingLbl;
        private Label _rpModeLbl;
        private Label _rpAmountLbl;
        private Label _rpBalanceLbl;
        private Label _rpBursarLbl;
        private Label _rpReceiptNumLbl;
        private Label _rpDateLbl;
        private PictureBox _rpLogoPictureBox;
        private Panel _receiptPreviewShell;

        // Step 3 action swap
        private Panel _preRecordActions;
        private Panel _postRecordActions;
        private Panel _successBanner;
        private Label _successBannerLbl;

        private static readonly Color PageBackColor = UiTheme.Page;
        private static readonly Color SurfaceColor = UiTheme.Surface;
        private static readonly Color SidebarBackColor = UiTheme.Navy;
        private static readonly Color PrimaryColor = UiTheme.Navy;
        private static readonly Color AccentColor = UiTheme.GoldSoft;
        private static readonly Color TextColor = UiTheme.Text;
        private static readonly Color MutedTextColor = UiTheme.Muted;
        private static readonly Color BorderColor = UiTheme.Border;

        private sealed class ReceiptPrintData
        {
            public string ReceiptNumber { get; set; }
            public string ReceivedFrom { get; set; }
            public string AmountWords { get; set; }
            public string AmountPaid { get; set; }
            public string Pesewas { get; set; }
            public string Being { get; set; }
            public string CashChequeNo { get; set; }
            public string Balance { get; set; }
            public string BursarName { get; set; }
            public DateTime PaymentDate { get; set; }
        }

        public frmFessPayment()
        {
            InitializeComponent();
            if (!AuthService.RequireAccess("frmFessPayment", this)) return;

            // Initialize modern architecture
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            _feeRepository = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, _feeRepository);

            BuildModernPaymentView();

            // Wire events commented-out in designer
            btn_Re.Click              += btn_Re_Click;
            pay.Click                 += pay_Click;
            txtStdID.TextChanged      += txtStdID_TextChanged;
            gunaPictureBox1.Click     += gunaPictureBox1_Click_1;
            Load                      += frmFessPayment_Load;
        }

        private void BuildModernPaymentView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Fees Payment";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1120, 780);
            ClientSize = new Size(1240, 880);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 540));
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

        private Panel BuildProgressIndicator()
        {
            _progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 8, 0, 4)
            };
            _progressPanel.Paint += ProgressPanel_Paint;
            return _progressPanel;
        }

        private void ProgressPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var panel = (Panel)sender;
            int w = panel.Width;
            int cy = 18;
            int r  = 12;
            int[] xs = { w / 2 - 90, w / 2, w / 2 + 90 };
            string[] stepLabels = { "LOOK UP", "PAYMENT", "RECEIPT" };

            Color navyColor     = PrimaryColor;
            Color greenColor    = Color.FromArgb(76, 175, 80);
            Color greyCircle    = Color.FromArgb(210, 213, 220);
            Color greyText      = Color.FromArgb(160, 163, 172);

            using (var labelFontActive   = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold))
            using (var labelFontInactive = new Font("Segoe UI", 7.5F))
            using (var numFont           = new Font("Segoe UI Semibold", 8F, FontStyle.Bold))
            using (var sfCenter          = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var sfTop             = new StringFormat { Alignment = StringAlignment.Center })
            {
                for (int i = 0; i < 3; i++)
                {
                    bool completed = i + 1 < _currentStep;
                    bool active    = i + 1 == _currentStep;
                    Color circleColor = completed ? greenColor : (active ? navyColor : greyCircle);
                    Color lineColor   = i > 0 && i < _currentStep ? navyColor : greyCircle;

                    if (i > 0)
                    {
                        using (var pen = new Pen(lineColor, 2))
                            g.DrawLine(pen, xs[i - 1] + r, cy, xs[i] - r, cy);
                    }

                    using (var brush = new SolidBrush(circleColor))
                        g.FillEllipse(brush, xs[i] - r, cy - r, r * 2, r * 2);

                    using (var brush = new SolidBrush(Color.White))
                        g.DrawString(completed ? "✓" : (i + 1).ToString(), numFont, brush,
                            new RectangleF(xs[i] - r, cy - r, r * 2, r * 2), sfCenter);

                    Color labelColor = completed ? greenColor : (active ? navyColor : greyText);
                    var labelFont    = (completed || active) ? labelFontActive : labelFontInactive;
                    using (var brush = new SolidBrush(labelColor))
                        g.DrawString(stepLabels[i], labelFont, brush, xs[i], cy + r + 3, sfTop);
                }
            }
        }

        private void RefreshProgressIndicator()
        {
            _progressPanel?.Invalidate();
        }

        private Control BuildPaymentPanel()
        {
            var receipt = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(24, 18, 24, 18),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 9,
                BackColor = SurfaceColor
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            studentIdBox = CreateTextBox();
            studentNameBox = CreateTextBox(true);
            classBox = CreateTextBox(true);
            balanceBox = CreateTextBox(true);
            amountBox = CreateTextBox();
            amountWordsBox = CreateTextBox(true);
            beingBox = CreateTextBox();
            bursarBox = CreateTextBox();
            cashChequeBox = CreateTextBox();
            paymentModeBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F),
                Height = 32
            };
            paymentModeBox.Items.AddRange(new object[] { "Cash", "Mobile Money", "Bank Transfer", "Cheque" });
            paymentModeBox.SelectedIndex = 0;
            paymentDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10.5F),
                Height = 32
            };

            studentIdBox.TextChanged += (sender, args) => LookupStudent();
            amountBox.TextChanged += (sender, args) => UpdateReceiptAmountWords();
            beingBox.Text = "School fees payment";

            var logo = BuildReceiptLogo();
            layout.Controls.Add(logo, 0, 0);

            var schoolHeader = BuildSchoolHeader();
            layout.Controls.Add(schoolHeader, 1, 0);
            layout.SetColumnSpan(schoolHeader, 5);

            var title = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Official Receipt",
                ForeColor = PrimaryColor,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial Narrow", 22F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(title, 0, 1);
            layout.SetColumnSpan(title, 2);

            receiptNumberLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No. " + CreateReceiptNumber(),
                ForeColor = Color.Black,
                Font = new Font("Consolas", 21F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(receiptNumberLabel, 2, 1);
            layout.SetColumnSpan(receiptNumberLabel, 2);
            layout.Controls.Add(CreateReceiptField("Date", paymentDatePicker), 4, 1);
            layout.SetColumnSpan(layout.GetControlFromPosition(4, 1), 2);

            layout.Controls.Add(CreateReceiptField("Student ID", studentIdBox), 0, 2);
            layout.Controls.Add(CreateReceiptField("Class", classBox), 1, 2);
            layout.Controls.Add(CreateReceiptField("Received From", studentNameBox), 2, 2);
            layout.SetColumnSpan(layout.GetControlFromPosition(2, 2), 4);

            layout.Controls.Add(CreateReceiptField("The sum of", amountWordsBox), 0, 3);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 3), 6);

            layout.Controls.Add(CreateReceiptField("Being", beingBox), 0, 4);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 4), 6);

            layout.Controls.Add(CreateReceiptField("Payment Mode", paymentModeBox), 0, 5);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 5), 2);
            layout.Controls.Add(CreateReceiptField("Cash/Cheque No.", cashChequeBox), 2, 5);
            layout.SetColumnSpan(layout.GetControlFromPosition(2, 5), 2);
            layout.Controls.Add(CreateReceiptField("Balance GHc", balanceBox), 4, 5);
            layout.SetColumnSpan(layout.GetControlFromPosition(4, 5), 2);

            var amountBoxPanel = BuildAmountBox();
            layout.Controls.Add(amountBoxPanel, 0, 6);
            layout.SetColumnSpan(amountBoxPanel, 3);

            layout.Controls.Add(CreateReceiptField("Bursar / Cashier", bursarBox), 3, 6);
            layout.SetColumnSpan(layout.GetControlFromPosition(3, 6), 2);

            var signature = new Label
            {
                Dock = DockStyle.Fill,
                Text = "........................\r\nSignature",
                ForeColor = PrimaryColor,
                Font = new Font("Georgia", 12F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(signature, 5, 6);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F)
            };
            layout.Controls.Add(statusLabel, 0, 7);
            layout.SetColumnSpan(statusLabel, 2);
            layout.Controls.Add(CreateSecondaryButton("Print Receipt", PrintReceiptPreview), 2, 7);
            layout.Controls.Add(CreatePrimaryButton("Record Payment", RecordPayment), 3, 7);
            layout.SetColumnSpan(layout.GetControlFromPosition(3, 7), 2);
            layout.Controls.Add(CreateSecondaryButton("Clear", ClearPaymentForm), 5, 7);

            receipt.Controls.Add(layout);
            return receipt;
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
            UiTheme.StyleDataGrid(paymentGrid, true);
            paymentGrid.ColumnHeadersDefaultCellStyle.BackColor = SidebarBackColor;
            paymentGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            paymentGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            paymentGrid.DefaultCellStyle.BackColor = SurfaceColor;
            paymentGrid.DefaultCellStyle.ForeColor = TextColor;
            paymentGrid.DefaultCellStyle.SelectionBackColor = AccentColor;
            paymentGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            paymentGrid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
            paymentGrid.GridColor = BorderColor;
            paymentGrid.CellFormatting += PaymentGrid_CellFormatting;

            shell.Controls.Add(paymentGrid);
            return shell;
        }

        private void ApplyPaymentHistoryGridLayout()
        {
            if (paymentGrid == null || paymentGrid.Columns.Count == 0)
            {
                return;
            }

            paymentGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SetHistoryColumn("STUDENT ID", 76, 70);
            SetHistoryColumn("CLASS ID", 82, 75);
            SetHistoryColumn("STUDENT NAME", 170, 150);
            SetHistoryColumn("AMOUNT PAID", 104, 95, DataGridViewContentAlignment.MiddleRight);
            SetHistoryColumn("BALANCE", 96, 90, DataGridViewContentAlignment.MiddleRight);
            SetHistoryColumn("PAYMENT DATE", 104, 95, DataGridViewContentAlignment.MiddleCenter);
            SetHistoryColumn("PAYMENT TIME", 96, 90, DataGridViewContentAlignment.MiddleCenter);
            SetHistoryColumn("PAYMENT MODE", 118, 105);
            SetHistoryColumn("BURSAR NAME", 136, 120);
        }

        private void SetHistoryColumn(string columnName, int fillWeight, int minWidth, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            if (!paymentGrid.Columns.Contains(columnName))
            {
                return;
            }

            var column = paymentGrid.Columns[columnName];
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.FillWeight = fillWeight;
            column.MinimumWidth = minWidth;
            column.DefaultCellStyle.Alignment = alignment;
        }

        private void PaymentGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (paymentGrid == null || e.Value == null || e.Value == DBNull.Value || e.ColumnIndex < 0)
            {
                return;
            }

            string columnName = paymentGrid.Columns[e.ColumnIndex].Name;
            if (columnName == "AMOUNT PAID" || columnName == "BALANCE")
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal amount))
                {
                    e.Value = amount.ToString("N2");
                    e.FormattingApplied = true;
                }
                return;
            }

            if (columnName == "PAYMENT DATE")
            {
                if (DateTime.TryParse(e.Value.ToString(), out DateTime dateValue))
                {
                    e.Value = dateValue.ToString("dd/MM/yyyy");
                    e.FormattingApplied = true;
                }
                return;
            }

            if (columnName == "PAYMENT TIME")
            {
                if (e.Value is TimeSpan timeValue)
                {
                    e.Value = timeValue.ToString(@"hh\:mm");
                    e.FormattingApplied = true;
                }
                else if (DateTime.TryParse(e.Value.ToString(), out DateTime dateTimeValue))
                {
                    e.Value = dateTimeValue.ToString("HH:mm");
                    e.FormattingApplied = true;
                }
                else if (TimeSpan.TryParse(e.Value.ToString(), out TimeSpan parsedTime))
                {
                    e.Value = parsedTime.ToString(@"hh\:mm");
                    e.FormattingApplied = true;
                }
            }
        }

        private string GetSchoolLogoPath()
        {
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "school_logo.png");
            if (!File.Exists(logoPath))
            {
                logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "school logo.png");
            }
            if (!File.Exists(logoPath))
            {
                logoPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "school_logo.png"));
            }
            if (!File.Exists(logoPath))
            {
                logoPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "school logo.png"));
            }
            return File.Exists(logoPath) ? logoPath : "";
        }

        private Control BuildReceiptLogo()
        {
            var box = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = SurfaceColor,
                Margin = new Padding(0, 0, 14, 4)
            };

            string logoPath = GetSchoolLogoPath();
            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                try { box.Image = Image.FromFile(logoPath); }
                catch { }
            }

            return box;
        }

        private Control BuildSchoolHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "KINGDOM PREPARATORY & J.H.S",
                ForeColor = PrimaryColor,
                Font = new Font("Arial Narrow", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomCenter
            }, 0, 0);
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "P. O. BOX 7 AKIM ODA",
                ForeColor = PrimaryColor,
                Font = new Font("Georgia", 13.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 1);
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Tel: 0548 050 141 | 0200 369 762 | 0201 455 533",
                ForeColor = PrimaryColor,
                Font = new Font("Georgia", 11.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopCenter
            }, 0, 2);

            return header;
        }

        private Control CreateReceiptField(string labelText, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = SurfaceColor,
                Margin = new Padding(0, 0, 18, 8),
                Padding = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                ForeColor = PrimaryColor,
                Font = new Font("Georgia", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);

            StyleReceiptInput(input);
            panel.Controls.Add(input, 0, 1);
            return panel;
        }

        private void StyleReceiptInput(Control input)
        {
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 3, 0, 0);
            input.Font = new Font("Segoe UI", 10.5F);

            if (input is TextBox textBox)
            {
                textBox.AutoSize = false;
                textBox.Height = 32;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (input is ComboBox comboBox)
            {
                comboBox.Height = 32;
            }
            else if (input is DateTimePicker dateTimePicker)
            {
                dateTimePicker.Height = 32;
            }
        }

        private Control BuildAmountBox()
        {
            var box = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = SurfaceColor,
                Margin = new Padding(0, 8, 18, 8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

            box.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "GHc",
                ForeColor = PrimaryColor,
                Font = new Font("Arial Narrow", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 0);

            amountBox.BorderStyle = BorderStyle.None;
            amountBox.AutoSize = false;
            amountBox.Height = 36;
            amountBox.TextAlign = HorizontalAlignment.Right;
            amountBox.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            amountBox.Margin = new Padding(8, 14, 8, 6);
            box.Controls.Add(amountBox, 1, 0);

            box.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = ".Gp",
                ForeColor = PrimaryColor,
                Font = new Font("Arial Narrow", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            }, 2, 0);

            return box;
        }

        private string CreateReceiptNumber()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }

        private void UpdateReceiptAmountWords()
        {
            if (amountWordsBox == null) return;
            if (!decimal.TryParse(amountBox.Text, out decimal amount) || amount <= 0)
            {
                amountWordsBox.Text = "";
                return;
            }

            int cedis = (int)Math.Floor(amount);
            int pesewas = (int)Math.Round((amount - cedis) * 100m);
            string text = NumberToWords(cedis) + " Ghana cedis";
            if (pesewas > 0)
            {
                text += " and " + NumberToWords(pesewas) + " pesewas";
            }
            amountWordsBox.Text = text;
        }

        private string NumberToWords(int number)
        {
            if (number == 0) return "zero";
            if (number < 0) return "minus " + NumberToWords(Math.Abs(number));

            string words = "";
            if ((number / 1000000) > 0)
            {
                words += NumberToWords(number / 1000000) + " million ";
                number %= 1000000;
            }
            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " thousand ";
                number %= 1000;
            }
            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " hundred ";
                number %= 100;
            }
            if (number > 0)
            {
                string[] units = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                string[] tens = { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
                if (number < 20) words += units[number];
                else
                {
                    words += tens[number / 10];
                    if ((number % 10) > 0) words += "-" + units[number % 10];
                }
            }
            return words.Trim();
        }

        private void PrintReceiptPreview()
        {
            try
            {
                ReceiptPrintData receiptData = BuildReceiptPrintData();
                if (string.IsNullOrWhiteSpace(receiptData.AmountPaid) && lastPrintedReceipt != null)
                {
                    receiptData = lastPrintedReceipt;
                }

                var document = new PrintDocument
                {
                    DocumentName = "Fees Payment Receipt"
                };
                document.DefaultPageSettings.Landscape = true;
                document.PrintPage += (sender, args) =>
                {
                    DrawReceipt(args.Graphics, args.MarginBounds, receiptData);
                    args.HasMorePages = false;
                };

                using (var preview = new PrintPreviewDialog())
                {
                    preview.Document = document;
                    preview.StartPosition = FormStartPosition.CenterParent;
                    preview.Width = 1100;
                    preview.Height = 780;
                    preview.ShowDialog(this);
                }

                statusLabel.Text = "Receipt preview opened.";
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Print receipt preview failed", ex);
                UIHelper.ShowError("Could not open receipt preview: " + ex.Message, "Print Receipt");
            }
        }

        private ReceiptPrintData BuildReceiptPrintData()
        {
            decimal amount = 0m;
            decimal.TryParse(amountBox.Text, out amount);

            int cedis = (int)Math.Floor(Math.Max(0m, amount));
            int pesewas = (int)Math.Round((Math.Max(0m, amount) - cedis) * 100m);
            if (pesewas == 100)
            {
                cedis += 1;
                pesewas = 0;
            }

            return new ReceiptPrintData
            {
                ReceiptNumber = CleanReceiptNumber(receiptNumberLabel != null ? receiptNumberLabel.Text : ""),
                ReceivedFrom = studentNameBox != null ? studentNameBox.Text.Trim() : "",
                AmountWords = amountWordsBox != null ? amountWordsBox.Text.Trim() : "",
                AmountPaid = amount > 0 ? cedis.ToString("N0") : "",
                Pesewas = amount > 0 ? pesewas.ToString("00") : "",
                Being = beingBox != null ? beingBox.Text.Trim() : "",
                CashChequeNo = cashChequeBox != null ? cashChequeBox.Text.Trim() : "",
                Balance = balanceBox != null ? balanceBox.Text.Trim() : "",
                BursarName = bursarBox != null ? bursarBox.Text.Trim() : "",
                PaymentDate = paymentDatePicker != null ? paymentDatePicker.Value.Date : DateTime.Today
            };
        }

        private string CleanReceiptNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return CreateReceiptNumber();
            }

            return value.Replace("No.", "").Trim();
        }

        private void DrawReceipt(Graphics graphics, Rectangle marginBounds, ReceiptPrintData data)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(Color.White);

            const float designWidth = 1050f;
            const float designHeight = 820f;
            float scale = Math.Min(marginBounds.Width / designWidth, marginBounds.Height / designHeight);
            float left = marginBounds.Left + (marginBounds.Width - designWidth * scale) / 2f;
            float top = marginBounds.Top + (marginBounds.Height - designHeight * scale) / 2f;

            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(left, top);
            graphics.ScaleTransform(scale, scale);

            Color receiptBlue = Color.FromArgb(0, 49, 111);
            using (var bluePen = new Pen(receiptBlue, 3f))
            using (var dottedPen = new Pen(receiptBlue, 2.4f))
            using (var titleFont = new Font("Arial Narrow", 40f, FontStyle.Bold))
            using (var subTitleFont = new Font("Georgia", 21f, FontStyle.Bold))
            using (var contactFont = new Font("Georgia", 18f, FontStyle.Regular))
            using (var receiptTitleFont = new Font("Arial Narrow", 38f, FontStyle.Bold))
            using (var labelFont = new Font("Georgia", 25f, FontStyle.Regular))
            using (var normalFont = new Font("Georgia", 18f, FontStyle.Regular))
            using (var numberFont = new Font("Consolas", 36f, FontStyle.Regular))
            using (var amountLabelFont = new Font("Arial Narrow", 36f, FontStyle.Bold))
            using (var valueFont = new Font("Georgia", 18f, FontStyle.Regular))
            using (var blueBrush = new SolidBrush(receiptBlue))
            using (var blackBrush = new SolidBrush(Color.Black))
            {
                dottedPen.DashStyle = DashStyle.Dot;
                dottedPen.DashCap = DashCap.Round;

                DrawReceiptLogo(graphics, new RectangleF(28, 8, 112, 125));

                DrawCenteredText(graphics, "KINGDOM PREPARATORY & J.H.S", titleFont, blueBrush, new RectangleF(170, 18, 820, 48));
                graphics.DrawLine(bluePen, 175, 78, 985, 78);
                DrawCenteredText(graphics, "P. O. BOX 7 AKIM ODA", subTitleFont, blueBrush, new RectangleF(245, 82, 655, 32));
                DrawCenteredText(graphics, "Tel: 0548 050 141 | 0200 369 762 | 0201 455 533", contactFont, blueBrush, new RectangleF(215, 113, 720, 30));

                var receiptBox = new RectangleF(28, 154, 405, 74);
                graphics.DrawRectangle(bluePen, receiptBox.X, receiptBox.Y, receiptBox.Width, receiptBox.Height);
                DrawCenteredText(graphics, "Official Receipt", receiptTitleFont, blueBrush, receiptBox);

                graphics.DrawString("No.", labelFont, blackBrush, 438, 177);
                graphics.DrawString(data.ReceiptNumber, numberFont, blackBrush, 505, 163);
                graphics.DrawString("Date:", labelFont, blueBrush, 705, 177);
                DrawDottedLine(graphics, dottedPen, 795, 205, 1014, 205);
                DrawValueOnLine(graphics, data.PaymentDate.ToString("dd/MM/yyyy"), valueFont, blackBrush, 805, 176, 205);

                DrawLineField(graphics, "Received From", data.ReceivedFrom, labelFont, valueFont, blueBrush, blackBrush, dottedPen, 40, 285, 1010);
                DrawLineField(graphics, "The sum of:", data.AmountWords, labelFont, valueFont, blueBrush, blackBrush, dottedPen, 40, 370, 1010);

                DrawDottedLine(graphics, dottedPen, 40, 456, 710, 456);
                DrawValueOnLine(graphics, data.AmountPaid, valueFont, blackBrush, 55, 427, 710);
                graphics.DrawString("GHc", labelFont, blueBrush, 715, 429);
                DrawDottedLine(graphics, dottedPen, 785, 456, 980, 456);
                DrawValueOnLine(graphics, data.Pesewas, valueFont, blackBrush, 795, 427, 980);
                graphics.DrawString("Gp", labelFont, blueBrush, 985, 429);

                DrawLineField(graphics, "Being:", data.Being, labelFont, valueFont, blueBrush, blackBrush, dottedPen, 40, 540, 1010);
                DrawDottedLine(graphics, dottedPen, 40, 622, 1010, 622);

                graphics.DrawString("Cash/Cheque No:", labelFont, blueBrush, 40, 665);
                DrawDottedLine(graphics, dottedPen, 275, 693, 705, 693);
                DrawValueOnLine(graphics, data.CashChequeNo, valueFont, blackBrush, 285, 665, 705);
                graphics.DrawString("Balance GHc", labelFont, blueBrush, 715, 665);
                DrawDottedLine(graphics, dottedPen, 895, 693, 1010, 693);
                DrawValueOnLine(graphics, data.Balance, valueFont, blackBrush, 905, 665, 1010);

                var amountBox = new RectangleF(40, 710, 610, 88);
                graphics.DrawRectangle(bluePen, amountBox.X, amountBox.Y, amountBox.Width, amountBox.Height);
                graphics.DrawLine(bluePen, 178, 710, 178, 798);
                graphics.DrawString("GHc", amountLabelFont, blueBrush, 65, 727);
                DrawCenteredText(graphics, data.AmountPaid, numberFont, blackBrush, new RectangleF(188, 725, 345, 48));
                graphics.DrawString("." + (string.IsNullOrWhiteSpace(data.Pesewas) ? "Gp" : data.Pesewas + "Gp"), amountLabelFont, blueBrush, 540, 727);

                DrawDottedLine(graphics, dottedPen, 800, 752, 1010, 752);
                if (!string.IsNullOrWhiteSpace(data.BursarName))
                {
                    DrawCenteredText(graphics, data.BursarName, valueFont, blackBrush, new RectangleF(795, 720, 220, 26));
                }
                graphics.DrawString("Signature", labelFont, blueBrush, 830, 765);
            }

            graphics.Restore(state);
        }

        private void DrawReceiptLogo(Graphics graphics, RectangleF bounds)
        {
            string logoPath = GetSchoolLogoPath();
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return;
            }

            try
            {
                using (Image logo = Image.FromFile(logoPath))
                {
                    graphics.DrawImage(logo, bounds);
                }
            }
            catch
            {
                // Printing should still work if the logo file is unavailable.
            }
        }

        private void DrawLineField(Graphics graphics, string label, string value, Font labelFont, Font valueFont, Brush labelBrush, Brush valueBrush, Pen dottedPen, float x, float y, float right)
        {
            SizeF labelSize = graphics.MeasureString(label, labelFont);
            graphics.DrawString(label, labelFont, labelBrush, x, y - 27);
            float lineStart = x + labelSize.Width + 12;
            DrawDottedLine(graphics, dottedPen, lineStart, y, right, y);
            DrawValueOnLine(graphics, value, valueFont, valueBrush, lineStart + 8, y - 28, right);
        }

        private void DrawValueOnLine(Graphics graphics, string value, Font font, Brush brush, float x, float y, float right)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            using (var format = new StringFormat())
            {
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;
                graphics.DrawString(value, font, brush, new RectangleF(x, y, Math.Max(10, right - x), 30), format);
            }
        }

        private void DrawDottedLine(Graphics graphics, Pen pen, float x1, float y1, float x2, float y2)
        {
            graphics.DrawLine(pen, x1, y1, x2, y2);
        }

        private void DrawCenteredText(Graphics graphics, string text, Font font, Brush brush, RectangleF bounds)
        {
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                graphics.DrawString(text, font, brush, bounds, format);
            }
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
                AutoSize = false,
                Height = 32,
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
                Margin = new Padding(8, 8, 0, 2),
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
                if (amountWordsBox != null) amountWordsBox.Text = "";
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
                    if (amountWordsBox != null) amountWordsBox.Text = "";
                    statusLabel.Text = "Student not found";
                    return;
                }

                studentNameBox.Text = student.FullName;
                classBox.Text = student.ClassID;
                if (beingBox != null && string.IsNullOrWhiteSpace(beingBox.Text))
                {
                    beingBox.Text = "School fees payment";
                }

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
            try
            {
                if (!FormValidationHelper.ValidateRequired(studentIdBox, "Student ID")) return;
                if (!FormValidationHelper.ValidateNumeric(amountBox, "Amount Paid", out decimal amountPaid)) return;
                if (!FormValidationHelper.ValidateRequired(bursarBox, "Bursar Name")) return;

                if (amountPaid <= 0)
                {
                    ConfirmationHelper.ShowWarning("Payment amount must be greater than zero.", "Payment");
                    return;
                }

                if (!decimal.TryParse(balanceBox.Text, out decimal currentBalance))
                {
                    ConfirmationHelper.ShowWarning("Student balance is not available. Look up a valid student first.", "Payment");
                    return;
                }

                decimal newBalance = Math.Max(0m, currentBalance - amountPaid);

                string changeDescription = $"Record payment of GHS {amountPaid:N2} for {studentNameBox.Text} (ID: {studentIdBox.Text.Trim()})?";
                if (!ConfirmationHelper.ConfirmSave(changeDescription)) return;

                statusLabel.Text = "Recording payment...";

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
                    lastPrintedReceipt = BuildReceiptPrintData();
                    amountBox.Text = "";
                    receiptNumberLabel.Text = "No. " + CreateReceiptNumber();
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
                LoggerHelper.LogError("RecordPayment failed", ex);
                statusLabel.Text = "Payment error";
                UIHelper.ShowError("Record payment failed: " + ex.Message, "Fee Payment");
            }
        }

        private async System.Threading.Tasks.Task LoadPaymentHistory()
        {
            try
            {
                statusLabel.Text = "Refreshing history...";
                DataTable table = await _feeRepository.GetPaymentHistoryTableAsync();
                paymentGrid.DataSource = table;
                ApplyPaymentHistoryGridLayout();
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
            amountWordsBox.Text = "";
            beingBox.Text = "School fees payment";
            bursarBox.Text = "";
            cashChequeBox.Text = "";
            paymentModeBox.SelectedIndex = 0;
            paymentDatePicker.Value = DateTime.Today;
            receiptNumberLabel.Text = "No. " + CreateReceiptNumber();
            lastPrintedReceipt = null;
            statusLabel.Text = "Ready.";
        }

        private async void frmFessPayment_Load(object sender, EventArgs e)
        {
            await LoadPaymentHistory();
        }

        private void txtStdID_TextChanged(object sender, EventArgs e) { LookupStudent(); }
        private void pay_Click(object sender, EventArgs e) { RecordPayment(); }
        private void btn_Re_Click(object sender, EventArgs e) { _ = LoadPaymentHistory(); }
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
