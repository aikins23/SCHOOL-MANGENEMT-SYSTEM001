using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
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
        private readonly PaymentService _paymentService;

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
        private TextBox historySearchBox;

        // Wizard navigation
        private int _currentStep = 1;
        private string _lastFeeTypeAutoFilled = "";

        // Placeholder shown in the "Being" field before a fee type is chosen.
        // Treated as an auto-filled value so the selected fee type can replace it.
        private const string DefaultBeingText = "School fees payment";

        // Overpayment: set only when the cashier approves an amount over the balance.
        // Reset whenever the amount changes so a new figure must be re-approved.
        private bool _overpaymentApproved;
        private decimal _approvedOverpayment;

        // Step container panels
        private Panel _stepContainer;
        private Panel _step1Panel;
        private Panel _step2Panel;
        private Panel _step3Panel;
        private TableLayoutPanel _step1RowLayout;
        private Label _step2NameLbl;
        private Label _step2BalanceLbl;

        // Progress indicator
        private Panel _progressPanel;

        // Step 1
        private ComboBox feeTypeBox;
        private Panel _studentInfoCard;
        private Label _studentInfoNameLbl;
        private Label _studentInfoClassLbl;
        private Label _studentInfoBalanceLbl;
        private Label _studentNotFoundLbl;
        private Button _continueToPaymentBtn;
        private Button _lookupStudentBtn;
        private Button _previewReceiptBtn;

        // Step 3 receipt preview value labels
        private Label _rpStudentIdLbl;
        private Label _rpClassLbl;
        private Label _rpNameLbl;
        private Label _rpAmountWordsLbl;
        private Label _rpBeingLbl;
        private Label _rpModeLbl;
        private Label _rpAmountLbl;
        private Label _rpBalanceLbl;
        private Label _rpCreditLbl;
        private Label _rpBursarLbl;
        private Label _rpReceiptNumLbl;
        private Label _rpDateLbl;
        private Label _historyCountLbl;
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

        private Models.DraftAdmission _admissionDraft;
        private Button _recordBtn;

        public frmFessPayment() : this(true) { }

        /// <summary>
        /// Admission mode: the administrator submits a draft admission for bursar
        /// approval. Skips the Accountant-only access check used for normal fee
        /// payments (the admin's authority comes from the admission screen).
        /// </summary>
        public frmFessPayment(Models.DraftAdmission draft) : this(false)
        {
            _admissionDraft = draft;
            EnterAdmissionMode();
        }

        private frmFessPayment(bool enforceAccess)
        {
            InitializeComponent();

            // Initialize modern architecture
            var studentRepo = new StudentRepository(AppConfig.ConnectionString);
            _feeRepository = new FeeRepository(AppConfig.ConnectionString);
            _studentService = new StudentService(studentRepo, _feeRepository);
            _paymentService = new PaymentService(_feeRepository);

            // Build the wizard UI BEFORE running the permission check.
            BuildModernPaymentView();

            // Wire events commented-out in designer
            btn_Re.Click              += btn_Re_Click;
            pay.Click                 += pay_Click;
            txtStdID.TextChanged      += txtStdID_TextChanged;
            gunaPictureBox1.Click     += gunaPictureBox1_Click_1;
            Load                      += frmFessPayment_Load;

            if (enforceAccess) AuthService.RequireAccess("frmFessPayment", this);
        }

        private void BuildModernPaymentView()
        {
            SuspendLayout();

            Controls.Clear();
            Text = "Fees Payment";
            BackColor = PageBackColor;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 880);
            ClientSize = new Size(1200, 1000);

            InitializeFormControls();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));

            root.Controls.Add(BuildWizardPanel(), 0, 0);
            root.Controls.Add(BuildHistoryPanel(), 0, 1);

            Controls.Add(root);
            ResumeLayout(true);
        }

        private Control BuildNotePanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 148, // Increased from 120
                BackColor = Color.FromArgb(238, 242, 251),
                Margin = new Padding(0, 16, 0, 0),
                Padding = new Padding(24, 16, 24, 16)
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(PrimaryColor, 4))
                    e.Graphics.DrawLine(pen, 0, 0, 0, panel.Height);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); // Increased from 22
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23)); // Increased from 18
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23)); // Increased from 18
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23)); // Increased from 18
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23)); // Increased from 18

            layout.Controls.Add(new Label { Text = "Note on Fee Type behavior", Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold), ForeColor = PrimaryColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(CreateBulletPoint("Preset options: School Fees, Examination Fees, PTA Levy, Uniform / Clothing, Sports / Activity Fees, Registration Fees"), 0, 1);
            layout.Controls.Add(CreateBulletPoint("Cashier can type freely — any custom text is accepted"), 0, 2);
            layout.Controls.Add(CreateBulletPoint("The selected fee type auto-populates the \"Being\" field on the receipt"), 0, 3);
            layout.Controls.Add(CreateBulletPoint("Fee Type is required before \"Continue to Payment\" is enabled"), 0, 4);

            panel.Controls.Add(layout);
            return panel;
        }

        private Control CreateBulletPoint(string text)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.Controls.Add(new Label { Text = "•", ForeColor = MutedTextColor, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter }, 0, 0);
            row.Controls.Add(new Label { Text = text, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 9F), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
            return row;
        }

        private void InitializeFormControls()
        {
            studentIdBox   = CreateTextBox();
            studentNameBox = CreateTextBox(true);
            classBox       = CreateTextBox(true);
            balanceBox     = CreateTextBox(true);
            amountBox      = CreateTextBox();
            amountWordsBox = CreateTextBox(true);
            beingBox       = CreateTextBox();
            // Bursar/cashier is the signed-in user — auto-filled and read-only so the
            // receipt always reflects who actually recorded the payment.
            bursarBox      = CreateTextBox(true);
            bursarBox.Text = AuthService.CurrentUser.DisplayName;
            cashChequeBox  = CreateTextBox();

            paymentModeBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F),
                Height = 38
            };
            paymentModeBox.Items.AddRange(new object[] { "Cash", "Mobile Money", "Bank Transfer", "Cheque" });
            paymentModeBox.SelectedIndex = 0;

            paymentDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 11F),
                Height = 38
            };

            receiptNumberLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No. " + CreateReceiptNumber(),
                ForeColor = Color.Black,
                Font = new Font("Consolas", 21F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5F)
            };

            studentIdBox.TextChanged += (sender, args) => LookupStudent();
            amountBox.TextChanged    += (sender, args) => {
                // Any change to the amount invalidates a prior overpayment approval.
                _overpaymentApproved = false;
                _approvedOverpayment = 0m;
                UpdateReceiptAmountWords();
                UpdatePreviewButton();
                UpdateStep2Balance();
                if (!string.IsNullOrEmpty(amountBox.Text) && !decimal.TryParse(amountBox.Text, out _))
                    amountBox.ForeColor = Color.Red;
                else
                    amountBox.ForeColor = TextColor;
            };
            beingBox.Text = DefaultBeingText;

            historySearchBox = CreateTextBox();
            SetPlaceholder(historySearchBox, "Search history (Name, ID, Class, Bursar)...");
            historySearchBox.TextChanged += (s, e) => FilterHistory();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private void SetPlaceholder(TextBox control, string text)
        {
            SendMessage(control.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
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

        private Control BuildWizardPanel()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(45, 16, 45, 14),
                Margin = new Padding(0, 0, 0, 12)
            };
            shell.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // Title row
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // Progress
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Step container
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // Status
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // Footer

            layout.Controls.Add(BuildWizardTitleRow(), 0, 0);
            layout.Controls.Add(BuildProgressIndicator(), 0, 1);

            _stepContainer = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = SurfaceColor,
                AutoScroll = true // Fail-safe for small screens or high scaling
            };
            _stepContainer.Controls.Add(BuildStep1Panel());
            _stepContainer.Controls.Add(BuildStep2Panel());
            _stepContainer.Controls.Add(BuildStep3Panel());
            layout.Controls.Add(_stepContainer, 0, 2);

            layout.Controls.Add(statusLabel, 0, 3);
            layout.Controls.Add(BuildHistoryFooter(), 0, 4);

            shell.Controls.Add(layout);
            return shell;
        }

        private Control BuildWizardTitleRow()
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 2, 0, 4)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            var titleBlock = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "Fees Payment",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            });
            titleBlock.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                Height = 16,
                Text = "Follow the steps to record a new student payment",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 10, 0, 0)
            };
            actions.Controls.Add(CreateSecondaryButton("Dashboard", () =>
            {
                Close();
                new frmDashboard().Show();
            }));
            actions.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadPaymentHistory()));

            row.Controls.Add(titleBlock, 0, 0);
            row.Controls.Add(actions, 1, 0);
            return row;
        }

        private Control BuildHistoryFooter()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Padding = new Padding(2, 6, 2, 0)
            };
            panel.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            };
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = SurfaceColor
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Payment History ↓",
                ForeColor = TextColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            _historyCountLbl = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Text = "0 records",
                ForeColor = Color.White,
                BackColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Padding = new Padding(9, 3, 9, 3),
                Margin = new Padding(0, 7, 0, 0)
            };
            row.Controls.Add(_historyCountLbl, 1, 0);
            panel.Controls.Add(row);
            return panel;
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
            if (panel.Width < 40) return;
            int w = panel.Width;
            int cy = 24;
            int r  = 14;
            int[] xs = { w / 2 - 104, w / 2, w / 2 + 104 };
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
                        g.DrawString(stepLabels[i], labelFont, brush,
                            new RectangleF(xs[i] - 40f, cy + r + 3, 80f, 18f), sfTop);
                }
            }
        }

        private void RefreshProgressIndicator()
        {
            _progressPanel?.Invalidate();
        }

        private void ShowStep(int step)
        {
            _currentStep = step;

            if (_step1Panel != null) _step1Panel.Visible = (step == 1);
            if (_step2Panel != null) _step2Panel.Visible = (step == 2);
            if (_step3Panel != null) _step3Panel.Visible = (step == 3);

            // Refresh Step 2 summary bar
            if (step == 2 && _step2Panel != null)
            {
                if (_step2NameLbl != null)
                    _step2NameLbl.Text = $"{studentNameBox?.Text.Trim()}  -  {classBox?.Text.Trim()}";
                UpdateStep2Balance();
            }

            // Reset Step 3 to pre-record state when entering from step 2
            if (step == 3)
            {
                if (_successBanner     != null) { _successBanner.Visible = false; _successBanner.Height = 0; }
                if (_preRecordActions  != null) _preRecordActions.Visible  = true;
                if (_postRecordActions != null) _postRecordActions.Visible = false;
                RefreshReceiptPreview();
            }

            RefreshProgressIndicator();
        }

        private Panel BuildStep1Panel()
        {
            _step1Panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Visible = true };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = SurfaceColor,
                Padding = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));   // Student ID field (label + input + padding)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));    // Inline error
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));    // Student info card
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Fee type (fills remaining)
            _step1RowLayout = layout;

            // Row 0: Student ID + explicit lookup action
            var lookupRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = SurfaceColor
            };
            lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lookupRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            lookupRow.Controls.Add(CreateModernField("STUDENT ID", studentIdBox), 0, 0);
            _lookupStudentBtn = CreateRoundedPrimaryButton("Look Up", LookupStudent);
            _lookupStudentBtn.Margin = new Padding(8, 22, 4, 8);
            _lookupStudentBtn.MinimumSize = new Size(0, 32);
            _lookupStudentBtn.MaximumSize = new Size(0, 32);
            lookupRow.Controls.Add(_lookupStudentBtn, 1, 0);
            layout.Controls.Add(lookupRow, 0, 0);

            // Row 1: Inline error label (hidden by default)
            _studentNotFoundLbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No student found with this ID",
                ForeColor = Color.FromArgb(192, 57, 43),
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            layout.Controls.Add(_studentNotFoundLbl, 0, 1);

            // Row 2: Student info card (shown after successful lookup)
            _studentInfoCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(238, 242, 251),
                Visible = false,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(16, 10, 16, 10),
                Margin = new Padding(0, 0, 0, 8)
            };
            var infoRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(238, 242, 251)
            };
            infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            // Left: NAME on top, CLASS/ID below — explicit Y positioning
            var leftStack = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _studentInfoNameLbl = new Label
            {
                Location = new Point(0, 2),
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _studentInfoClassLbl = new Label
            {
                Location = new Point(0, 32),
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            leftStack.Resize += (s, e) =>
            {
                _studentInfoNameLbl.Width = leftStack.Width;
                _studentInfoClassLbl.Width = leftStack.Width;
            };
            leftStack.Controls.Add(_studentInfoNameLbl);
            leftStack.Controls.Add(_studentInfoClassLbl);
            infoRow.Controls.Add(leftStack, 0, 0);

            // Right: CAPTION on top, AMOUNT below — explicit Y positioning
            var rightStack = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var balanceCaption = new Label
            {
                Location = new Point(0, 4),
                Text = "Outstanding Balance",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Height = 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _studentInfoBalanceLbl = new Label
            {
                Location = new Point(0, 28),
                ForeColor = Color.FromArgb(192, 57, 43),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            rightStack.Resize += (s, e) =>
            {
                balanceCaption.Width = rightStack.Width;
                _studentInfoBalanceLbl.Width = rightStack.Width;
            };
            rightStack.Controls.Add(balanceCaption);
            rightStack.Controls.Add(_studentInfoBalanceLbl);
            infoRow.Controls.Add(rightStack, 1, 0);
            _studentInfoCard.Controls.Add(infoRow);
            layout.Controls.Add(_studentInfoCard, 0, 2);

            // Row 3: Fee Type (editable ComboBox — default style for visible boundary)
            feeTypeBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10.5F),
                Height = 32,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = SurfaceColor,
                ForeColor = TextColor
            };
            feeTypeBox.Items.AddRange(new object[]
            {
                "School Fees", "Examination Fees", "PTA Levy",
                "Uniform / Clothing", "Sports / Activity Fees", "Registration Fees"
            });
            feeTypeBox.TextChanged += (s, e) => UpdateContinueButton();

            var feePicker = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 2, 0, 0)
            };
            feePicker.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // FEE TYPE field
            feePicker.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));  // Options list (compact)
            feePicker.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // "Or type your own" hint
            feePicker.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Filler — absorbs slack
            feePicker.Controls.Add(CreateField("FEE TYPE", feeTypeBox), 0, 0);

            var feeOptionsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                IntegralHeight = false,
                BackColor = SurfaceColor,
                ForeColor = TextColor,
                ItemHeight = 24, // Slightly smaller item height
                DrawMode = DrawMode.OwnerDrawFixed
            };
            feeOptionsList.DrawItem += (s, ev) =>
            {
                var lb = (ListBox)s;
                bool selected = (ev.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = selected ? Color.FromArgb(238, 242, 251) : SurfaceColor;
                Color fg = selected ? PrimaryColor : TextColor;
                ev.Graphics.FillRectangle(new SolidBrush(bg), ev.Bounds);
                if (ev.Index >= 0 && ev.Index < lb.Items.Count)
                    ev.Graphics.DrawString(lb.Items[ev.Index].ToString(), ev.Font,
                        new SolidBrush(fg),
                        new RectangleF(ev.Bounds.X + 10, ev.Bounds.Y + 3, ev.Bounds.Width - 10, ev.Bounds.Height - 3));
            };
            feeOptionsList.Items.AddRange(new object[]
            {
                "School Fees", "Examination Fees", "PTA Levy",
                "Uniform / Clothing", "Sports / Activity Fees", "Registration Fees"
            });
            feeOptionsList.SelectedIndexChanged += (s, e) =>
            {
                if (feeOptionsList.SelectedItem != null)
                {
                    feeTypeBox.Text = feeOptionsList.SelectedItem.ToString();
                }
            };
            // Pre-select first item so the list scrolls to the top and "School Fees" is highlighted
            feeOptionsList.SelectedIndex = 0;
            feePicker.Controls.Add(feeOptionsList, 0, 1);
            feePicker.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Or type your own above ↑",
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);
            layout.Controls.Add(feePicker, 0, 3);

            // Continue button — pinned to a docked bottom bar so it stays visible
            // no matter how tall the student-info card / fee picker above it grows.
            _continueToPaymentBtn = CreateRoundedPrimaryButton("Continue to Payment →", GoToStep2);
            _continueToPaymentBtn.Enabled = false;
            _continueToPaymentBtn.Dock = DockStyle.None;
            _continueToPaymentBtn.Size = new Size(240, 36);
            var btnWrap = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = SurfaceColor };
            btnWrap.Controls.Add(_continueToPaymentBtn);
            btnWrap.Resize += (s, e) =>
                _continueToPaymentBtn.Location = new Point(btnWrap.Width - 240, 10);

            card.Controls.Add(layout);          // Fill (added first → lowest dock priority)
            _step1Panel.Controls.Add(card);
            _step1Panel.Controls.Add(btnWrap);  // Bottom (added last → reserves its space)
            return _step1Panel;
        }

        private void UpdateContinueButton()
        {
            if (_continueToPaymentBtn == null) return;
            bool hasStudent = !string.IsNullOrWhiteSpace(studentNameBox?.Text);
            bool hasFeeType = !string.IsNullOrWhiteSpace(feeTypeBox?.Text);
            _continueToPaymentBtn.Enabled = hasStudent && hasFeeType;
        }

        private void SetStudentInfoCardVisible(bool visible)
        {
            if (_studentInfoCard != null) _studentInfoCard.Visible = visible;
            if (_step1RowLayout != null && _step1RowLayout.RowStyles.Count > 2)
            {
                _step1RowLayout.RowStyles[2].Height = visible ? 88 : 0;
                _step1RowLayout.PerformLayout();
            }
        }

        private void SetStudentNotFoundVisible(bool visible)
        {
            if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = visible;
            if (_step1RowLayout != null && _step1RowLayout.RowStyles.Count > 1)
            {
                _step1RowLayout.RowStyles[1].Height = visible ? 22 : 0;
                _step1RowLayout.PerformLayout();
            }
        }

        private void GoToStep2()
        {
            string feeType = feeTypeBox?.Text.Trim() ?? "";
            // Let the chosen fee type populate "Being" unless the user has typed
            // their own purpose. The default placeholder and any previous auto-fill
            // are both considered replaceable.
            if (beingBox != null && feeType.Length > 0 &&
                (string.IsNullOrWhiteSpace(beingBox.Text)
                 || beingBox.Text == _lastFeeTypeAutoFilled
                 || beingBox.Text == DefaultBeingText))
            {
                beingBox.Text = feeType;
                _lastFeeTypeAutoFilled = feeType;
            }
            ShowStep(2);
        }

        private Panel BuildStep2Panel()
        {
            _step2Panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Visible = false };

            var btnRow = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = SurfaceColor };
            var backBtn2 = CreateSecondaryButton("← Back", () => ShowStep(1));
            backBtn2.Dock = DockStyle.None;
            backBtn2.Size = new Size(120, 40);
            backBtn2.Location = new Point(0, 15);
            backBtn2.Margin = Padding.Empty;

            _previewReceiptBtn = CreateRoundedPrimaryButton("Preview Receipt →", GoToStep3);
            _previewReceiptBtn.Dock = DockStyle.None;
            _previewReceiptBtn.Size = new Size(240, 40);
            _previewReceiptBtn.Margin = Padding.Empty;
            _previewReceiptBtn.Name = "_previewReceiptBtn";
            _previewReceiptBtn.Enabled = false;

            btnRow.Resize += (s, e) =>
            {
                _previewReceiptBtn.Location = new Point(btnRow.Width - 240, 15);
            };
            btnRow.Controls.Add(backBtn2);
            btnRow.Controls.Add(_previewReceiptBtn);
            _step2Panel.Controls.Add(btnRow);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                BackColor = SurfaceColor,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Student summary bar
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));   // Amount | Payment Mode
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));   // Quick buttons row
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));   // Cheque Ref | Bursar
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));   // Amount in words
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));   // Being | Date
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Filler

            // Row 0: Student summary bar
            var summaryBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(238, 242, 251),
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(15, 0, 15, 0),
                Name = "step2SummaryBar"
            };
            summaryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            summaryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            _step2NameLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Name = "step2NameLbl"
            };
            summaryBar.Controls.Add(_step2NameLbl, 0, 0);
            _step2BalanceLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(192, 57, 43),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Name = "step2BalanceLbl"
            };
            summaryBar.Controls.Add(_step2BalanceLbl, 1, 0);
            layout.Controls.Add(summaryBar, 0, 0);
            layout.SetColumnSpan(summaryBar, 2);

            // Row 1: Amount | Payment Mode
            amountBox.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            layout.Controls.Add(CreateModernField("PAYMENT AMOUNT (GHc)", amountBox), 0, 1);
            layout.Controls.Add(CreateField("PAYMENT MODE", paymentModeBox), 1, 1);

            // Row 2: Quick buttons
            var quickBtnWrapper = BuildQuickAmountButtons();
            quickBtnWrapper.Margin = new Padding(0, -5, 0, 5);
            layout.Controls.Add(quickBtnWrapper, 0, 2);
            layout.SetColumnSpan(quickBtnWrapper, 2);

            // Row 3: Cheque Ref | Bursar
            layout.Controls.Add(CreateModernField("CHEQUE / REFERENCE NO.", cashChequeBox), 0, 3);
            layout.Controls.Add(CreateModernField("BURSAR / CASHIER NAME", bursarBox), 1, 3);

            // Row 4: Amount in words
            var wordsField = CreateField("AMOUNT IN WORDS (AUTO-GENERATED)", amountWordsBox);
            layout.Controls.Add(wordsField, 0, 4);
            layout.SetColumnSpan(wordsField, 2);

            // Row 5: Being | Date
            layout.Controls.Add(CreateField("BEING (PURPOSE OF PAYMENT)", beingBox), 0, 5);
            layout.Controls.Add(CreateField("PAYMENT DATE", paymentDatePicker), 1, 5);

            card.Controls.Add(layout);
            _step2Panel.Controls.Add(card);
            return _step2Panel;
        }

        private void UpdatePreviewButton()
        {
            if (_previewReceiptBtn == null) return;
            _previewReceiptBtn.Enabled =
                decimal.TryParse(amountBox?.Text, out decimal amount) && amount > 0;
        }

        private void GoToStep3()
        {
            if (!ConfirmOverpaymentIfNeeded()) return;
            RefreshReceiptPreview();
            ShowStep(3);
        }

        // When the payment amount exceeds the outstanding balance, ask the cashier
        // to approve the overpayment. The excess is only computed/accepted on "Yes";
        // a "No" keeps the user on Step 2 to correct the amount.
        private bool ConfirmOverpaymentIfNeeded()
        {
            decimal balance = 0m;
            decimal.TryParse(balanceBox?.Text, out balance);
            decimal amount = 0m;
            decimal.TryParse(amountBox?.Text, out amount);

            if (amount <= balance) return true;

            decimal excess = amount - balance;
            var result = UIHelper.ShowConfirmation(
                $"The payment amount (GHc {amount:N2}) is more than the outstanding " +
                $"balance (GHc {balance:N2}).\n\n" +
                $"Overpayment / excess: GHc {excess:N2}\n\n" +
                "Do you want to continue and record this overpayment as a credit?",
                "Overpayment");

            if (result != DialogResult.Yes) return false;

            _overpaymentApproved = true;
            _approvedOverpayment = excess;
            UpdateStep2Balance();
            return true;
        }

        private Panel BuildStep3Panel()
        {
            _step3Panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Visible = false };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.None,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Padding = Padding.Empty
            };

            // Action buttons container (pre/post record panels overlaid)
            var actionContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 2, 0, 0)
            };
            actionContainer.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(Color.FromArgb(210, 213, 220), 1))
                    e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            };
            _preRecordActions  = BuildPreRecordActions();
            _postRecordActions = BuildPostRecordActions();
            _preRecordActions.Dock  = DockStyle.Fill;
            _postRecordActions.Dock = DockStyle.Fill;
            _postRecordActions.Visible = false;
            actionContainer.Controls.Add(_preRecordActions);
            actionContainer.Controls.Add(_postRecordActions);

            // Success banner — docks top with fixed height
            _successBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 0,
                BackColor = Color.FromArgb(232, 245, 233),
                Visible = false,
                Padding = new Padding(14, 4, 14, 4)
            };
            var successRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(232, 245, 233)
            };
            successRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
            successRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            successRow.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "✓",
                ForeColor = Color.FromArgb(76, 175, 80),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 0);
            _successBannerLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(46, 125, 50),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            successRow.Controls.Add(_successBannerLbl, 1, 0);
            _successBanner.Controls.Add(successRow);

            var receipt = BuildReceiptPreviewControl();
            receipt.Dock = DockStyle.Fill;

            outer.Controls.Add(receipt);
            outer.Controls.Add(_successBanner);
            outer.Controls.Add(actionContainer);

            card.Controls.Add(outer);
            _step3Panel.Controls.Add(card);
            return _step3Panel;
        }

        private Panel BuildPreRecordActions()
        {
            var row = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };

            var backBtn = CreateSecondaryButton("← Back", () => ShowStep(2));
            backBtn.Dock = DockStyle.None;
            backBtn.Size = new Size(110, 36);
            backBtn.Location = new Point(0, 10);
            backBtn.Margin = Padding.Empty;

            var recordBtn = CreateRoundedPrimaryButton("Record Payment", RecordPayment);
            recordBtn.Dock = DockStyle.None;
            recordBtn.Size = new Size(380, 36);
            recordBtn.Margin = Padding.Empty;
            _recordBtn = recordBtn;

            var clearBtn = CreateSecondaryButton("Clear", ClearPaymentForm);
            clearBtn.Dock = DockStyle.None;
            clearBtn.Size = new Size(100, 36);
            clearBtn.Margin = Padding.Empty;

            row.Resize += (s, e) =>
            {
                int w = row.Width;
                recordBtn.Location = new Point((w - 380) / 2, 10);
                clearBtn.Location = new Point(w - 100, 10);
            };
            row.Controls.Add(backBtn);
            row.Controls.Add(recordBtn);
            row.Controls.Add(clearBtn);
            return row;
        }

        private Panel BuildPostRecordActions()
        {
            var row = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };

            var printBtn = CreateRoundedPrimaryButton("Print Receipt", PrintReceiptPreview);
            printBtn.Dock = DockStyle.None;
            printBtn.Size = new Size(240, 36);
            printBtn.Margin = Padding.Empty;

            var newPayBtn = CreateSecondaryButton("+ New Payment", ClearPaymentForm);
            newPayBtn.Dock = DockStyle.None;
            newPayBtn.Size = new Size(240, 36);
            newPayBtn.Margin = Padding.Empty;
            newPayBtn.BackColor = Color.FromArgb(232, 245, 233);
            newPayBtn.ForeColor = Color.FromArgb(46, 125, 50);
            newPayBtn.FlatAppearance.BorderColor = Color.FromArgb(165, 214, 167);

            row.Resize += (s, e) =>
            {
                int w = row.Width;
                int totalWidth = 240 + 16 + 240; // print + gap + newPay
                int startX = (w - totalWidth) / 2;
                printBtn.Location = new Point(startX, 10);
                newPayBtn.Location = new Point(startX + 240 + 16, 10);
            };
            row.Controls.Add(printBtn);
            row.Controls.Add(newPayBtn);
            return row;
        }

        // A scroll container that paints all children through a single back buffer
        // (WS_EX_COMPOSITED). Without this, scrolling the custom-painted receipt
        // panels bit-blits without a full repaint and leaves ghosted/overlapping text.
        private sealed class SmoothScrollPanel : Panel
        {
            public SmoothScrollPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }
            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                    return cp;
                }
            }
        }

        private Control BuildReceiptPreviewControl()
        {
            _receiptPreviewShell = new SmoothScrollPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                AutoScroll = true
            };

            var card = new Panel
            {
                BackColor = Color.White,
                Size = new Size(780, 560),
                MinimumSize = new Size(500, 520)
            };
            card.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(Color.FromArgb(200, 205, 215), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                using (var accent = new Pen(Color.FromArgb(192, 165, 65), 3))
                    e.Graphics.DrawLine(accent, 40, 0, p.Width - 40, 0);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9,
                BackColor = Color.White,
                Padding = new Padding(40, 16, 40, 16)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // School header
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));   // Spacer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Receipt title + number + date
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));   // Spacer
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));   // Student row
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));   // Sum of (amount in words)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));   // Being + mode
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));   // Amount box + balance
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));   // Bursar + signature

            // ── Row 0: School header (logo + name/address) ──────────────────────────
            var schoolRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 10)
            };
            schoolRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            schoolRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            schoolRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            schoolRow.Paint += (s, e) =>
            {
                var tlp = (TableLayoutPanel)s;
                int y = tlp.Height - 2;
                using (var pen = new Pen(PrimaryColor, 2))
                    e.Graphics.DrawLine(pen, 0, y, tlp.Width, y);
            };
            _rpLogoPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 16, 0)
            };
            string logoPath = GetSchoolLogoPath();
            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                try
                {
                    using (var tmp = Image.FromFile(logoPath))
                        _rpLogoPictureBox.Image = new Bitmap(tmp);
                }
                catch { }
            }
            var schoolText = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White
            };
            schoolText.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
            schoolText.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
            schoolText.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
            schoolText.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "KINGDOM PREPARATORY J.H.S",
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomCenter
            }, 0, 0);
            schoolText.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "P. O. BOX 7 AKIM ODA",
                ForeColor = PrimaryColor,
                Font = new Font("Georgia", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 1);
            schoolText.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Tel: 0548 050 141 | 0200 369 762 | 0201 455 533",
                ForeColor = MutedTextColor,
                Font = new Font("Georgia", 9F),
                TextAlign = ContentAlignment.TopCenter
            }, 0, 2);
            schoolRow.Controls.Add(_rpLogoPictureBox, 0, 0);
            schoolRow.Controls.Add(schoolText, 1, 0);
            layout.Controls.Add(schoolRow, 0, 0);

            // ── Row 1: Spacer ────────────────────────────────────────────────────────
            layout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.White }, 0, 1);

            // ── Row 2: Receipt title + number + date ────────────────────────────────
            var titleRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 8)
            };
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            titleRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            titleRow.Paint += (s, e) =>
            {
                var tlp = (TableLayoutPanel)s;
                int y = tlp.Height - 2;
                using (var pen = new Pen(Color.FromArgb(210, 213, 220), 1))
                    e.Graphics.DrawLine(pen, 0, y, tlp.Width, y);
            };
            var titleBox = new Label
            {
                Dock = DockStyle.Fill,
                Text = "OFFICIAL RECEIPT",
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            titleRow.Controls.Add(titleBox, 0, 0);
            var numDatePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            _rpReceiptNumLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(192, 57, 43),
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            _rpDateLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            numDatePanel.Controls.Add(_rpReceiptNumLbl, 0, 0);
            numDatePanel.Controls.Add(_rpDateLbl, 0, 1);
            titleRow.Controls.Add(numDatePanel, 1, 0);
            layout.Controls.Add(titleRow, 0, 2);

            // ── Row 3: Spacer ────────────────────────────────────────────────────────
            layout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.White }, 0, 3);

            // ── Row 4: Student row (tiles) ───────────────────────────────────────────
            var studentRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = Color.White
            };
            studentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            studentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            studentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            studentRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Panel idTile, clsTile, namTile;
            _rpStudentIdLbl = CreateReceiptTile("STUDENT ID",  out idTile);
            _rpClassLbl     = CreateReceiptTile("CLASS",       out clsTile);
            _rpNameLbl      = CreateReceiptTile("RECEIVED FROM", out namTile);
            foreach (var t in new[] { idTile, clsTile, namTile })
                t.BackColor = Color.FromArgb(248, 249, 251);
            studentRow.Controls.Add(idTile,  0, 0);
            studentRow.Controls.Add(clsTile, 1, 0);
            studentRow.Controls.Add(namTile, 2, 0);
            layout.Controls.Add(studentRow, 0, 4);

            // ── Row 5: The sum of (amount in words) ──────────────────────────────────
            Panel sumTile;
            _rpAmountWordsLbl = CreateReceiptTile("THE SUM OF", out sumTile);
            _rpAmountWordsLbl.Font = new Font("Georgia", 12F, FontStyle.Italic);
            sumTile.BackColor = Color.FromArgb(245, 247, 252);
            sumTile.Paint += (s, e) =>
            {
                using (var pen = new Pen(PrimaryColor, 3))
                    e.Graphics.DrawLine(pen, 0, 0, 0, ((Panel)s).Height);
            };
            layout.Controls.Add(sumTile, 0, 5);

            // ── Row 6: Being + Payment mode ──────────────────────────────────────────
            var beingRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.White
            };
            beingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            beingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            beingRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Panel beiTile, modTile;
            _rpBeingLbl = CreateReceiptTile("BEING",        out beiTile);
            _rpModeLbl  = CreateReceiptTile("PAYMENT MODE", out modTile);
            beiTile.BackColor = Color.FromArgb(248, 249, 251);
            modTile.BackColor = Color.FromArgb(248, 249, 251);
            beingRow.Controls.Add(beiTile, 0, 0);
            beingRow.Controls.Add(modTile, 1, 0);
            layout.Controls.Add(beingRow, 0, 6);

            // ── Row 7: Amount box + Balance After ────────────────────────────────────
            var amountRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.White
            };
            amountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            amountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            amountRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var amtBoxPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(238, 242, 251),
                Padding = new Padding(20, 8, 20, 8),
                Margin = new Padding(0, 3, 12, 3)
            };
            amtBoxPanel.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(PrimaryColor, 2))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            var amtInner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = Color.Transparent
            };
            // Side labels are AutoSize + Anchor (not Dock=Fill) so "GHc" and ".00"
            // can never wrap to a second line when the box is narrow.
            amtInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            amtInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            amtInner.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            amtInner.Controls.Add(new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 6, 0),
                Text = "GHc",
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold)
            }, 0, 0);
            _rpAmountLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Consolas", 24F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            };
            amtInner.Controls.Add(_rpAmountLbl, 1, 0);
            amtInner.Controls.Add(new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(4, 0, 0, 0),
                Text = ".00",
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold)
            }, 2, 0);
            amtBoxPanel.Controls.Add(amtInner);
            amountRow.Controls.Add(amtBoxPanel, 0, 0);

            var balPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(232, 245, 233),
                Padding = new Padding(18, 8, 18, 8),
                Margin = new Padding(0, 3, 0, 3)
            };
            var balCaption = new Label
            {
                Text = "OUTSTANDING BALANCE AFTER",
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = Color.FromArgb(46, 125, 50),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            _rpBalanceLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(46, 125, 50),
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _rpCreditLbl = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 16,
                ForeColor = Color.FromArgb(176, 90, 0),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            balPanel.Controls.Add(_rpBalanceLbl);
            balPanel.Controls.Add(balCaption);
            balPanel.Controls.Add(_rpCreditLbl);
            amountRow.Controls.Add(balPanel, 1, 0);
            layout.Controls.Add(amountRow, 0, 7);

            // ── Row 8: Bursar + Signature ────────────────────────────────────────────
            var bursarRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.White
            };
            bursarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            bursarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            bursarRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Panel burTile;
            _rpBursarLbl = CreateReceiptTile("BURSAR / CASHIER", out burTile);
            burTile.BackColor = Color.FromArgb(248, 249, 251);
            bursarRow.Controls.Add(burTile, 0, 0);
            bursarRow.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "........................\r\nSignature / Stamp",
                ForeColor = MutedTextColor,
                Font = new Font("Georgia", 10F),
                TextAlign = ContentAlignment.MiddleCenter
            }, 1, 0);
            layout.Controls.Add(bursarRow, 0, 8);

            card.Controls.Add(layout);

            void CenterCard()
            {
                int cw = card.Width;
                int ch = card.Height;
                int sw = _receiptPreviewShell.ClientSize.Width;
                int sh = _receiptPreviewShell.ClientSize.Height;
                if (sw >= cw && sh >= ch)
                {
                    card.Location = new Point((sw - cw) / 2, (sh - ch) / 2);
                    _receiptPreviewShell.AutoScrollMinSize = Size.Empty;
                }
                else
                {
                    card.Location = new Point(0, 0);
                    _receiptPreviewShell.AutoScrollMinSize = card.Size;
                }
            }
            _receiptPreviewShell.Resize += (s, e) => CenterCard();
            _receiptPreviewShell.Controls.Add(card);
            CenterCard();

            return _receiptPreviewShell;
        }

        private Label CreateReceiptTile(string caption, out Panel tile)
        {
            var tilePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 249, 255),
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(14, 8, 10, 6)
            };

            var valueLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                AutoEllipsis = true
            };

            var captionLbl = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Text = caption,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                AutoSize = false,
                AutoEllipsis = true
            };

            tilePanel.Controls.Add(valueLbl);
            tilePanel.Controls.Add(captionLbl);
            tile = tilePanel;
            return valueLbl;
        }

        private void RefreshReceiptPreview()
        {
            if (_rpStudentIdLbl == null) return;

            decimal amount = 0m;
            decimal.TryParse(amountBox?.Text, out amount);
            decimal balance = 0m;
            decimal.TryParse(balanceBox?.Text, out balance);
            decimal projected = Math.Max(0m, balance - amount);

            _rpStudentIdLbl.Text   = studentIdBox?.Text.Trim() ?? "";
            _rpClassLbl.Text       = classBox?.Text.Trim() ?? "";
            _rpNameLbl.Text        = studentNameBox?.Text.Trim() ?? "";
            _rpAmountWordsLbl.Text = amountWordsBox?.Text.Trim() ?? "";

            // Being: prefer the typed/auto value, but fall back to the fee type
            // chosen in Step 1 so the receipt is never blank.
            string being = beingBox?.Text.Trim() ?? "";
            if (being.Length == 0) being = feeTypeBox?.Text.Trim() ?? "";
            _rpBeingLbl.Text       = being;

            // Payment mode: a DropDownList combo can read blank before its handle
            // exists — fall back to the selected item, then to "Cash".
            string mode = paymentModeBox?.Text;
            if (string.IsNullOrWhiteSpace(mode))
                mode = paymentModeBox?.SelectedItem?.ToString();
            _rpModeLbl.Text        = string.IsNullOrWhiteSpace(mode) ? "Cash" : mode;
            _rpAmountLbl.Text      = amount > 0 ? ((int)Math.Floor(amount)).ToString("N0") : "—";
            _rpBalanceLbl.Text     = "GHc " + projected.ToString("N2");

            // Show the approved overpayment as a credit beneath the balance-after.
            if (_rpCreditLbl != null)
            {
                bool hasCredit = _overpaymentApproved && _approvedOverpayment > 0m;
                _rpCreditLbl.Visible = hasCredit;
                _rpCreditLbl.Text = hasCredit
                    ? "Incl. overpayment credit: GHc " + _approvedOverpayment.ToString("N2")
                    : "";
            }

            _rpBursarLbl.Text      = bursarBox?.Text.Trim() ?? "";
            _rpReceiptNumLbl.Text  = receiptNumberLabel?.Text ?? "";
            _rpDateLbl.Text        = paymentDatePicker?.Value.ToString("dd/MM/yyyy") ?? "";
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

            // [shared control: now owned by wizard panels] receiptNumberLabel → row 1, col 2
            // [shared control: now owned by wizard panels] paymentDatePicker  → row 1, col 4

            // [shared control: now owned by wizard panels] studentIdBox   → row 2, col 0
            // [shared control: now owned by wizard panels] classBox        → row 2, col 1
            // [shared control: now owned by wizard panels] studentNameBox  → row 2, col 2..5

            // [shared control: now owned by wizard panels] amountWordsBox → row 3, col 0..5

            // [shared control: now owned by wizard panels] beingBox       → row 4, col 0..5

            // [shared control: now owned by wizard panels] paymentModeBox → row 5, col 0..1
            // [shared control: now owned by wizard panels] cashChequeBox  → row 5, col 2..3
            // [shared control: now owned by wizard panels] balanceBox     → row 5, col 4..5

            // [shared control: now owned by wizard panels] amountBox (via BuildAmountBox) → row 6, col 0..2
            // BuildAmountBox() omitted here: BuildStep2Panel already configures amountBox font/alignment

            // [shared control: now owned by wizard panels] bursarBox      → row 6, col 3..4

            var signature = new Label
            {
                Dock = DockStyle.Fill,
                Text = "........................\r\nSignature",
                ForeColor = PrimaryColor,
                Font = new Font("Georgia", 12F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(signature, 5, 6);

            // [shared control: now owned by wizard panels] statusLabel → row 7, col 0..1
            // Action buttons below are omitted: wizard panels (BuildPreRecordActions/BuildPostRecordActions) own them

            receipt.Controls.Add(layout);
            return receipt;
        }

        private Control BuildQuickAmountButtons()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 0, 0, 0),
                Margin = new Padding(0, -10, 0, 10)
            };

            var amounts = new[] { 50, 100, 200, 500 };
            foreach (var amt in amounts)
            {
                var btn = new Button
                {
                    Text = "GHc " + amt,
                    AutoSize = true,
                    Height = 26,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(240, 242, 245),
                    ForeColor = TextColor,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 8F),
                    Margin = new Padding(0, 0, 6, 0)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => amountBox.Text = amt.ToString();
                panel.Controls.Add(btn);
            }

            var payFullBtn = new Button
            {
                Text = "Pay Full Balance",
                AutoSize = true,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(232, 245, 233),
                ForeColor = Color.FromArgb(46, 125, 50),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Margin = new Padding(10, 0, 0, 0)
            };
            payFullBtn.FlatAppearance.BorderSize = 0;
            payFullBtn.Click += (s, e) => {
                if (decimal.TryParse(balanceBox.Text, out decimal bal))
                    amountBox.Text = bal.ToString("0.00");
            };
            panel.Controls.Add(payFullBtn);

            return panel;
        }

        private void FilterHistory()
        {
            if (paymentGrid.DataSource is DataTable table)
            {
                string filter = historySearchBox.Text.Trim().Replace("'", "''");
                if (string.IsNullOrEmpty(filter))
                {
                    table.DefaultView.RowFilter = "";
                }
                else
                {
                    table.DefaultView.RowFilter = string.Format(
                        "[STUDENT ID] LIKE '%{0}%' OR [STUDENT NAME] LIKE '%{0}%' OR [CLASS ID] LIKE '%{0}%' OR [BURSAR NAME] LIKE '%{0}%'",
                        filter);
                }
                if (_historyCountLbl != null)
                    _historyCountLbl.Text = table.DefaultView.Count + " records";
            }
        }

        private Control BuildHistoryPanel()
        {
            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = PageBackColor
            };
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 8) };
            historySearchBox.Dock = DockStyle.Left;
            historySearchBox.Width = 350;
            searchPanel.Controls.Add(historySearchBox);
            container.Controls.Add(searchPanel, 0, 0);

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
            paymentGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            paymentGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            paymentGrid.ColumnHeadersHeight = 32;
            paymentGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            paymentGrid.DefaultCellStyle.BackColor = SurfaceColor;
            paymentGrid.DefaultCellStyle.ForeColor = TextColor;
            paymentGrid.DefaultCellStyle.SelectionBackColor = AccentColor;
            paymentGrid.DefaultCellStyle.SelectionForeColor = TextColor;
            paymentGrid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
            paymentGrid.GridColor = BorderColor;
            paymentGrid.CellFormatting += PaymentGrid_CellFormatting;

            container.Controls.Add(paymentGrid, 0, 1);
            return container;
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
            amountWordsBox.Text = text.ToUpperInvariant();
        }

        // Live-update the Step 2 summary balance to reflect the payment amount
        // being deducted from the student's outstanding balance.
        private void UpdateStep2Balance()
        {
            if (_step2BalanceLbl == null) return;
            decimal balance = 0m;
            decimal.TryParse(balanceBox?.Text, out balance);
            decimal amount = 0m;
            decimal.TryParse(amountBox?.Text, out amount);
            decimal remaining = Math.Max(0m, balance - amount);

            if (_overpaymentApproved && _approvedOverpayment > 0m)
                _step2BalanceLbl.Text = "Balance: GHc 0.00  ·  Credit: GHc " +
                                        _approvedOverpayment.ToString("N2");
            else
                _step2BalanceLbl.Text = "Balance: GHc " + remaining.ToString("N2");
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
                Padding = new Padding(0, 0, 15, 12),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft,
                AutoEllipsis = true,
                Margin = new Padding(0, 0, 0, 2)
            }, 0, 0);

            // Use Anchor instead of Dock so the input keeps its fixed height
            input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            input.Dock = DockStyle.None;
            input.Height = 38; // Increased from 32
            input.Margin = new Padding(0, 0, 0, 0);
            
            var inputHost = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = SurfaceColor, 
                Margin = Padding.Empty, 
                Padding = Padding.Empty,
                Height = 40
            };
            inputHost.Resize += (s, e) =>
            {
                input.Location = new Point(0, 0);
                input.Width = inputHost.Width;
            };
            inputHost.Controls.Add(input);
            panel.Controls.Add(inputHost, 0, 1);
            return panel;
        }

        private TextBox CreateTextBox(bool readOnly = false)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = false,
                Height = 38,
                ReadOnly = readOnly,
                BackColor = readOnly ? Color.FromArgb(248, 249, 251) : SurfaceColor,
                ForeColor = TextColor
            };
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = PrimaryColor;
            button.FlatAppearance.MouseOverBackColor = UiTheme.NavyHover;

            Color disabledBack = Color.FromArgb(220, 224, 232);
            Color disabledFore = Color.FromArgb(130, 138, 155);
            void Sync() {
                button.BackColor = button.Enabled ? PrimaryColor : disabledBack;
                button.ForeColor = button.Enabled ? Color.White : disabledFore;
                button.FlatAppearance.BorderColor = button.Enabled ? PrimaryColor : disabledBack;
            }
            button.EnabledChanged += (s, e) => Sync();
            Sync();
            return button;
        }

        private Button CreateSecondaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.BackColor = SurfaceColor;
            button.ForeColor = TextColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 247, 252);
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
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 1;
            button.Click += (sender, args) => action();
            return button;
        }

        // Modern, custom-painted primary action button with rounded corners and a
        // smooth gold hover. Used only for the key wizard actions.
        private sealed class RoundedButton : Button
        {
            public int CornerRadius { get; set; } = 10;
            public Color FillColor { get; set; } = Color.FromArgb(25, 25, 112);
            public Color HoverFillColor { get; set; } = Color.FromArgb(18, 18, 86);
            public Color DisabledFillColor { get; set; } = Color.FromArgb(220, 224, 232);
            public Color DisabledTextColor { get; set; } = Color.FromArgb(130, 138, 155);
            private bool _hover;

            public RoundedButton()
            {
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                FlatAppearance.MouseOverBackColor = Color.Transparent;
                FlatAppearance.MouseDownBackColor = Color.Transparent;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                         ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
                EnabledChanged += (s, e) => Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                // Fill with the parent's colour so the rounded corners blend in.
                pevent.Graphics.Clear(Parent != null ? Parent.BackColor : Color.White);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
                Color fill = !Enabled ? DisabledFillColor : (_hover ? HoverFillColor : FillColor);
                using (var path = RoundedRectPath(rect, CornerRadius))
                {
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);
                    // Subtle border so the button always reads as a button — including
                    // the disabled state on a white surface.
                    Color borderClr = !Enabled ? Color.FromArgb(196, 202, 214) : fill;
                    using (var pen = new Pen(borderClr, 1f))
                        g.DrawPath(pen, path);
                }

                Color fg = Enabled ? ForeColor : DisabledTextColor;
                TextRenderer.DrawText(g, Text, Font, rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private static GraphicsPath RoundedRectPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || d > r.Width || d > r.Height)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Button CreateRoundedPrimaryButton(string text, Action action)
        {
            var button = new RoundedButton
            {
                Text = text,
                Dock = DockStyle.Fill,
                Height = 36,
                Margin = new Padding(8, 8, 0, 2),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                FillColor = PrimaryColor,
                HoverFillColor = UiTheme.NavyHover,
                CornerRadius = 10,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.Click += (sender, args) => action();
            return button;
        }

        // Field with a rounded border around the input that glows gold on focus.
        // Used for the active (editable) inputs only.
        private Control CreateModernField(string labelText, TextBox input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0, 0, 15, 12),
                BackColor = SurfaceColor,
                Margin = Padding.Empty
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                UseMnemonic = false,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft,
                AutoEllipsis = true,
                Margin = new Padding(0, 0, 0, 2)
            }, 0, 0);

            input.BorderStyle = BorderStyle.None;
            input.Dock = DockStyle.None;
            input.Margin = Padding.Empty;

            var inputHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            inputHost.Paint += (s, e) =>
            {
                var p = (Panel)s;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                bool focused = input.Focused;
                using (var path = RoundedRectPath(rect, 9))
                using (var pen = new Pen(focused ? UiTheme.Gold : BorderColor, focused ? 2f : 1f))
                    e.Graphics.DrawPath(pen, path);
            };
            void LayoutInput()
            {
                input.Location = new Point(12, Math.Max(0, (inputHost.Height - input.Height) / 2));
                input.Width = inputHost.Width - 24;
            }
            inputHost.Resize += (s, e) => LayoutInput();
            input.GotFocus += (s, e) => inputHost.Invalidate();
            input.LostFocus += (s, e) => inputHost.Invalidate();
            inputHost.Controls.Add(input);
            LayoutInput();
            panel.Controls.Add(inputHost, 0, 1);
            return panel;
        }

        private async void LookupStudent()
        {
            if (studentIdBox == null || string.IsNullOrWhiteSpace(studentIdBox.Text))
            {
                studentNameBox.Text = "";
                classBox.Text = "";
                balanceBox.Text = "";
                if (amountWordsBox != null) amountWordsBox.Text = "";
                SetStudentInfoCardVisible(false);
                SetStudentNotFoundVisible(false);
                UpdateContinueButton();
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
                    SetStudentInfoCardVisible(false);
                    SetStudentNotFoundVisible(true);
                    UpdateContinueButton();
                    return;
                }

                studentNameBox.Text = student.FullName;
                classBox.Text = student.ClassID;
                if (beingBox != null && string.IsNullOrWhiteSpace(beingBox.Text))
                {
                    beingBox.Text = DefaultBeingText;
                }

                // Try to get latest payment balance, otherwise default fee
                decimal? balance = await _feeRepository.GetLatestBalanceAsync(studentId);
                if (!balance.HasValue)
                {
                    balance = await _feeRepository.GetDefaultBalanceAsync(studentId, student.ClassID);
                }

                balanceBox.Text = (balance ?? 0m).ToString("0.00");
                statusLabel.Text = "Student details loaded";
                if (_studentInfoNameLbl  != null) _studentInfoNameLbl.Text  = student.FullName;
                if (_studentInfoClassLbl != null) _studentInfoClassLbl.Text = $"Class: {student.ClassID}  ·  ID: {studentId}";
                if (_studentInfoBalanceLbl != null) _studentInfoBalanceLbl.Text = "GHc " + (balance ?? 0m).ToString("N2");
                SetStudentInfoCardVisible(true);
                SetStudentNotFoundVisible(false);
                UpdateContinueButton();
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Lookup failed";
                UIHelper.ShowError("Lookup error: " + ex.Message, "Payment");
                SetStudentInfoCardVisible(false);
                SetStudentNotFoundVisible(false);
                UpdateContinueButton();
            }
        }

        private void EnterAdmissionMode()
        {
            // Prefill the wizard from the draft and jump straight to the payment step.
            if (studentNameBox != null) studentNameBox.Text = _admissionDraft.FullName;
            if (classBox != null)       classBox.Text = _admissionDraft.ClassID;
            if (balanceBox != null)     balanceBox.Text = _admissionDraft.TermTotal.ToString("0.00");
            if (beingBox != null)       beingBox.Text = "Admission - School Fees";
            if (amountBox != null)      amountBox.Text = Services.DraftAdmissionService.MinSchoolFee(_admissionDraft.TermTotal).ToString("0.00");
            if (_recordBtn != null)     _recordBtn.Text = "Submit for Approval";
            this.Text = "New Admission - Payment";
            ShowStep(2);
        }

        // Admission mode: validate + save the draft (admission fee GHS 100 + school fee >= 50%)
        // for bursar approval. No real student/payment/SMS/receipt until the bursar approves.
        private async System.Threading.Tasks.Task SubmitAdmissionDraftAsync()
        {
            if (!decimal.TryParse(amountBox.Text, out decimal schoolPaid))
            {
                UIHelper.ShowError("Enter a valid school-fee amount.", "Admission");
                return;
            }
            _admissionDraft.SchoolFeePaid = schoolPaid;
            _admissionDraft.AdmissionFee = Common.AdmissionFees.Amount;
            _admissionDraft.PaymentMode = string.IsNullOrWhiteSpace(paymentModeBox?.Text) ? "Cash" : paymentModeBox.Text;

            var svc = new Services.DraftAdmissionService(
                new Data.DraftAdmissionRepository(AppConfig.ConnectionString),
                _studentService,
                _feeRepository);

            var result = await svc.CreateDraftAsync(_admissionDraft);
            if (!result.Ok)
            {
                UIHelper.ShowError(result.Message, "Admission");
                return;
            }
            UIHelper.ShowSuccess("Submitted for bursar approval. The SMS and receipts will be sent once the bursar approves.", "Admission");
            Close();
        }

        private async void RecordPayment()
        {
            try
            {
                if (_admissionDraft != null) { await SubmitAdmissionDraftAsync(); return; }
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

                string changeDescription = $"Record payment of GHS {amountPaid:N2} for {studentNameBox.Text} (ID: {studentIdBox.Text.Trim()})?";
                if (!ConfirmationHelper.ConfirmSave(changeDescription)) return;

                statusLabel.Text = "Recording payment...";

                PaymentRecordResult result = await _paymentService.RecordPaymentAsync(new PaymentRecordRequest
                {
                    StudentId = studentIdBox.Text,
                    ClassId = classBox.Text,
                    StudentName = studentNameBox.Text,
                    CurrentBalance = currentBalance,
                    AmountPaid = amountPaid,
                    PaymentMode = paymentModeBox.Text,
                    BursarName = bursarBox.Text,
                    PaymentDate = paymentDatePicker.Value.Date
                });

                if (result.Success)
                {
                    decimal newBalance = result.NewBalance;
                    balanceBox.Text = newBalance.ToString("0.00");
                    lastPrintedReceipt = BuildReceiptPrintData();

                    // Update balance label in receipt preview to the actual saved value
                    if (_rpBalanceLbl != null)
                        _rpBalanceLbl.Text = "GHc " + newBalance.ToString("N2");

                    // Show inline success state (no dialog)
                    if (_successBannerLbl != null)
                        _successBannerLbl.Text = $"Payment Recorded - GHc {amountPaid:N2} from {studentNameBox.Text} - New balance: GHc {newBalance:N2}";
                    if (_successBanner     != null) { _successBanner.Visible = true; _successBanner.Height = 34; }
                    if (_preRecordActions  != null) _preRecordActions.Visible  = false;
                    if (_postRecordActions != null) _postRecordActions.Visible = true;
                    // Leave the on-screen receipt fully populated for review/print.
                    // Inputs (amount, receipt number) reset on "+ New Payment" (ClearPaymentForm).
                    statusLabel.Text = "Payment recorded";
                    await LoadPaymentHistory();
                }
                else
                {
                    statusLabel.Text = "Payment failed";
                    UIHelper.ShowError(result.Message, "Payment");
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
                int count = table.Rows.Count;
                statusLabel.Text = "";
                if (_historyCountLbl != null)
                    _historyCountLbl.Text = count + " records";
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
            bursarBox.Text = AuthService.CurrentUser.DisplayName; // keep the signed-in cashier
            cashChequeBox.Text = "";
            paymentModeBox.SelectedIndex = 0;
            paymentDatePicker.Value = DateTime.Today;
            receiptNumberLabel.Text = "No. " + CreateReceiptNumber();
            lastPrintedReceipt = null;
            statusLabel.Text = "Ready.";
            if (feeTypeBox        != null) feeTypeBox.Text = "";
            if (beingBox          != null) beingBox.Text   = "";
            _lastFeeTypeAutoFilled = "";
            _overpaymentApproved = false;
            _approvedOverpayment = 0m;
            SetStudentInfoCardVisible(false);
            SetStudentNotFoundVisible(false);
            UpdateContinueButton();
            ShowStep(1);
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
