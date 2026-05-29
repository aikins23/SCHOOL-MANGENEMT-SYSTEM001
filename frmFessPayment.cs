using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Printing;
using System.Linq;
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

            InitializeFormControls();

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
            root.Controls.Add(BuildWizardPanel(), 0, 1);
            root.Controls.Add(BuildHistoryPanel(), 0, 2);

            Controls.Add(root);
            ResumeLayout(true);
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
            bursarBox      = CreateTextBox();
            cashChequeBox  = CreateTextBox();

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
            amountBox.TextChanged    += (sender, args) => UpdateReceiptAmountWords();
            beingBox.Text = "School fees payment";
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
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(24, 12, 24, 12),
                Margin = new Padding(0, 0, 0, 14)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));   // Progress indicator
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Step container
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // Status label

            layout.Controls.Add(BuildProgressIndicator(), 0, 0);

            _stepContainer = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };
            _stepContainer.Controls.Add(BuildStep1Panel());
            _stepContainer.Controls.Add(BuildStep2Panel());
            _stepContainer.Controls.Add(BuildStep3Panel());
            layout.Controls.Add(_stepContainer, 0, 1);

            layout.Controls.Add(statusLabel, 0, 2);

            shell.Controls.Add(layout);
            return shell;
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
                var bar    = _step2Panel.Controls.Find("step2SummaryBar", true).FirstOrDefault() as TableLayoutPanel;
                var namLbl = bar?.Controls.Find("step2NameLbl",    true).FirstOrDefault() as Label;
                var balLbl = bar?.Controls.Find("step2BalanceLbl", true).FirstOrDefault() as Label;
                if (namLbl != null) namLbl.Text = $"{studentNameBox?.Text.Trim()}  ·  {classBox?.Text.Trim()}";
                if (balLbl != null) balLbl.Text = "Balance: GHc " + (balanceBox?.Text ?? "0.00");
            }

            // Reset Step 3 to pre-record state when entering from step 2
            if (step == 3)
            {
                if (_successBanner     != null) _successBanner.Visible     = false;
                if (_preRecordActions  != null) _preRecordActions.Visible  = true;
                if (_postRecordActions != null) _postRecordActions.Visible = false;
                if (_receiptPreviewShell != null) _receiptPreviewShell.BorderStyle = BorderStyle.FixedSingle;
            }

            RefreshProgressIndicator();
        }

        private Panel BuildStep1Panel()
        {
            _step1Panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Visible = true };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 8, 0, 0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));   // Student ID field
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));   // Inline error
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));   // Student info card
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));   // Fee type
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Continue button + filler

            // Row 0: Student ID
            layout.Controls.Add(CreateField("STUDENT ID", studentIdBox), 0, 0);

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
                Padding = new Padding(12, 0, 12, 0),
                Margin = new Padding(0, 0, 0, 4)
            };
            var infoRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(238, 242, 251)
            };
            infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            _studentInfoNameLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _studentInfoBalanceLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(192, 57, 43),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            infoRow.Controls.Add(_studentInfoNameLbl, 0, 0);
            infoRow.Controls.Add(_studentInfoBalanceLbl, 1, 0);
            _studentInfoCard.Controls.Add(infoRow);
            layout.Controls.Add(_studentInfoCard, 0, 2);

            // Row 3: Fee Type (editable ComboBox)
            feeTypeBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10.5F),
                Height = 32,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            feeTypeBox.Items.AddRange(new object[]
            {
                "School Fees", "Examination Fees", "PTA Levy",
                "Uniform / Clothing", "Sports / Activity Fees", "Registration Fees"
            });
            feeTypeBox.TextChanged += (s, e) => UpdateContinueButton();
            layout.Controls.Add(CreateField("FEE TYPE  (select or type)", feeTypeBox), 0, 3);

            // Row 4: Continue button right-aligned
            _continueToPaymentBtn = CreatePrimaryButton("Continue to Payment →", GoToStep2);
            _continueToPaymentBtn.Enabled = false;
            _continueToPaymentBtn.Dock = DockStyle.None;
            _continueToPaymentBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _continueToPaymentBtn.Size = new Size(220, 36);
            var btnWrap = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };
            btnWrap.Controls.Add(_continueToPaymentBtn);
            btnWrap.Resize += (s, e) =>
                _continueToPaymentBtn.Location = new Point(btnWrap.Width - 220, 8);
            layout.Controls.Add(btnWrap, 0, 4);

            _step1Panel.Controls.Add(layout);
            return _step1Panel;
        }

        private void UpdateContinueButton()
        {
            if (_continueToPaymentBtn == null) return;
            bool hasStudent = !string.IsNullOrWhiteSpace(studentNameBox?.Text);
            bool hasFeeType = !string.IsNullOrWhiteSpace(feeTypeBox?.Text);
            _continueToPaymentBtn.Enabled = hasStudent && hasFeeType;
        }

        private void GoToStep2()
        {
            string feeType = feeTypeBox?.Text.Trim() ?? "";
            if (beingBox != null &&
                (string.IsNullOrWhiteSpace(beingBox.Text) || beingBox.Text == _lastFeeTypeAutoFilled))
            {
                beingBox.Text = feeType;
                _lastFeeTypeAutoFilled = feeType;
            }
            ShowStep(2);
        }

        private Panel BuildStep2Panel()
        {
            _step2Panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Visible = false };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 8, 0, 0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // Student summary bar
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));   // Amount + Mode + Ref
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // Amount in words
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // Being + Bursar + Date
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Back + Preview buttons

            // Row 0: Student summary bar
            var summaryBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(238, 242, 251),
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(10, 0, 10, 0),
                Name = "step2SummaryBar"
            };
            summaryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            summaryBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            summaryBar.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Name = "step2NameLbl"
            }, 0, 0);
            summaryBar.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(192, 57, 43),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Name = "step2BalanceLbl"
            }, 1, 0);
            layout.Controls.Add(summaryBar, 0, 0);

            // Row 1: Amount + Payment Mode + Cheque/Ref
            var row1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = SurfaceColor };
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            amountBox.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            amountBox.TextAlign = HorizontalAlignment.Right;
            amountBox.TextChanged += (s, e) => UpdatePreviewButton();
            row1.Controls.Add(CreateField("AMOUNT (GHc)", amountBox));
            row1.Controls.Add(CreateField("PAYMENT MODE", paymentModeBox));
            row1.Controls.Add(CreateField("CHEQUE / REF NO.", cashChequeBox));
            layout.Controls.Add(row1, 0, 1);

            // Row 2: Amount in words (read-only)
            layout.Controls.Add(CreateField("AMOUNT IN WORDS", amountWordsBox), 0, 2);

            // Row 3: Being + Bursar + Date
            var row3 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = SurfaceColor };
            row3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            row3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            row3.Controls.Add(CreateField("BEING (REASON)", beingBox));
            row3.Controls.Add(CreateField("BURSAR / CASHIER", bursarBox));
            row3.Controls.Add(CreateField("DATE", paymentDatePicker));
            layout.Controls.Add(row3, 0, 3);

            // Row 4: Back + Preview Receipt
            var btnRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = SurfaceColor, Padding = new Padding(0, 8, 0, 0) };
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            var backBtn2 = CreateSecondaryButton("← Back", () => ShowStep(1));
            _previewReceiptBtn = CreatePrimaryButton("Preview Receipt →", GoToStep3);
            _previewReceiptBtn.Name = "previewReceiptBtn";
            _previewReceiptBtn.Enabled = false;
            btnRow.Controls.Add(backBtn2, 0, 0);
            btnRow.Controls.Add(new Panel { BackColor = SurfaceColor }, 1, 0);
            btnRow.Controls.Add(_previewReceiptBtn, 2, 0);
            layout.Controls.Add(btnRow, 0, 4);

            _step2Panel.Controls.Add(layout);
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
            RefreshReceiptPreview();
            ShowStep(3);
        }

        private Panel BuildStep3Panel()
        {
            _step3Panel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Visible = false };

            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 4, 0, 0)
            };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));   // Success banner
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Receipt preview
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // Action buttons

            // Row 0: Success banner
            _successBanner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(232, 245, 233),
                Visible = false,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0, 0, 0, 4)
            };
            var successRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(232, 245, 233)
            };
            successRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
            successRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            successRow.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "✓",
                ForeColor = Color.FromArgb(76, 175, 80),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
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
            outer.Controls.Add(_successBanner, 0, 0);

            // Row 1: Receipt preview
            outer.Controls.Add(BuildReceiptPreviewControl(), 0, 1);

            // Row 2: Action buttons container (pre/post record panels stacked)
            var actionContainer = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor };
            _preRecordActions  = BuildPreRecordActions();
            _postRecordActions = BuildPostRecordActions();
            _preRecordActions.Dock  = DockStyle.None;
            _postRecordActions.Dock = DockStyle.None;
            _postRecordActions.Visible = false;
            actionContainer.Controls.Add(_preRecordActions);
            actionContainer.Controls.Add(_postRecordActions);
            actionContainer.Resize += (s, e) =>
            {
                var sz = ((Panel)s).ClientSize;
                _preRecordActions.Bounds  = new Rectangle(Point.Empty, sz);
                _postRecordActions.Bounds = new Rectangle(Point.Empty, sz);
            };
            outer.Controls.Add(actionContainer, 0, 2);

            _step3Panel.Controls.Add(outer);
            return _step3Panel;
        }

        private Panel BuildPreRecordActions()
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 8, 0, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            row.Controls.Add(CreateSecondaryButton("← Back", () => ShowStep(2)), 0, 0);
            row.Controls.Add(new Panel { BackColor = SurfaceColor }, 1, 0);
            row.Controls.Add(CreatePrimaryButton("Record Payment", RecordPayment), 2, 0);
            row.Controls.Add(CreateSecondaryButton("Clear", ClearPaymentForm), 3, 0);
            return row;
        }

        private Panel BuildPostRecordActions()
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = SurfaceColor,
                Padding = new Padding(0, 8, 0, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
            row.Controls.Add(new Panel { BackColor = SurfaceColor }, 0, 0);
            row.Controls.Add(CreatePrimaryButton("Print Receipt", PrintReceiptPreview), 1, 0);
            var newPayBtn = CreateSecondaryButton("+ New Payment", ClearPaymentForm);
            newPayBtn.BackColor = Color.FromArgb(232, 245, 233);
            newPayBtn.ForeColor = Color.FromArgb(46, 125, 50);
            newPayBtn.FlatAppearance.BorderColor = Color.FromArgb(165, 214, 167);
            row.Controls.Add(newPayBtn, 2, 0);
            return row;
        }

        private Control BuildReceiptPreviewControl()
        {
            _receiptPreviewShell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12, 8, 12, 8)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // School header
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // Receipt title + number + date
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // Student row
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // Sum of
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // Being + mode
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // Amount box + balance
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // Bursar + signature
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Filler

            // ── Row 0: School header (logo + name/address) ──────────────────────────
            var schoolOuter = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Padding = new Padding(0, 0, 0, 2) };
            schoolOuter.Paint += (s, e) =>
            {
                int y = ((Panel)s).Height - 2;
                using (var pen = new Pen(PrimaryColor, 2))
                    e.Graphics.DrawLine(pen, 0, y, ((Panel)s).Width, y);
            };
            var schoolRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor };
            schoolRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            schoolRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _rpLogoPictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = SurfaceColor, Margin = new Padding(0, 0, 8, 0) };
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
            var schoolText = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = SurfaceColor };
            schoolText.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            schoolText.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
            schoolText.RowStyles.Add(new RowStyle(SizeType.Percent, 27));
            schoolText.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "KINGDOM PREPARATORY J.H.S",    ForeColor = PrimaryColor,     Font = new Font("Arial Narrow", 13F, FontStyle.Bold),    TextAlign = ContentAlignment.BottomCenter  }, 0, 0);
            schoolText.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "P. O. BOX 7 AKIM ODA",         ForeColor = PrimaryColor,     Font = new Font("Georgia", 9F, FontStyle.Bold),          TextAlign = ContentAlignment.MiddleCenter  }, 0, 1);
            schoolText.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Tel: 0548 050 141 | 0200 369 762 | 0201 455 533", ForeColor = MutedTextColor, Font = new Font("Georgia", 8F), TextAlign = ContentAlignment.TopCenter }, 0, 2);
            schoolRow.Controls.Add(_rpLogoPictureBox, 0, 0);
            schoolRow.Controls.Add(schoolText, 1, 0);
            schoolOuter.Controls.Add(schoolRow);
            layout.Controls.Add(schoolOuter, 0, 0);

            // ── Row 1: Receipt title + number + date ────────────────────────────────
            var titleRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor };
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var titleBox = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Official Receipt",
                ForeColor = PrimaryColor,
                Font = new Font("Arial Narrow", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 8, 4)
            };
            titleRow.Controls.Add(titleBox, 0, 0);
            var numDatePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = SurfaceColor };
            numDatePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            numDatePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _rpReceiptNumLbl = new Label { Dock = DockStyle.Fill, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.MiddleRight };
            _rpDateLbl       = new Label { Dock = DockStyle.Fill, ForeColor = MutedTextColor, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.MiddleRight };
            numDatePanel.Controls.Add(_rpReceiptNumLbl, 0, 0);
            numDatePanel.Controls.Add(_rpDateLbl, 0, 1);
            titleRow.Controls.Add(numDatePanel, 1, 0);
            layout.Controls.Add(titleRow, 0, 1);

            // ── Row 2: Student row ───────────────────────────────────────────────────
            var studentRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = SurfaceColor };
            studentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            studentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            studentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            Panel idTile, clsTile, namTile;
            _rpStudentIdLbl = CreateReceiptTile("STUDENT ID",  out idTile);
            _rpClassLbl     = CreateReceiptTile("CLASS",       out clsTile);
            _rpNameLbl      = CreateReceiptTile("RECEIVED FROM", out namTile);
            studentRow.Controls.Add(idTile,  0, 0);
            studentRow.Controls.Add(clsTile, 1, 0);
            studentRow.Controls.Add(namTile, 2, 0);
            layout.Controls.Add(studentRow, 0, 2);

            // ── Row 3: The sum of (amount in words) ──────────────────────────────────
            Panel sumTile;
            _rpAmountWordsLbl = CreateReceiptTile("THE SUM OF", out sumTile);
            _rpAmountWordsLbl.Font = new Font("Georgia", 9.5F, FontStyle.Italic);
            sumTile.BackColor = Color.FromArgb(247, 249, 255);
            sumTile.Paint += (s, e) =>
            {
                using (var pen = new Pen(PrimaryColor, 3))
                    e.Graphics.DrawLine(pen, 0, 0, 0, ((Panel)s).Height);
            };
            layout.Controls.Add(sumTile, 0, 3);

            // ── Row 4: Being + Payment mode ──────────────────────────────────────────
            var beingRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor };
            beingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            beingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            Panel beiTile, modTile;
            _rpBeingLbl = CreateReceiptTile("BEING",        out beiTile);
            _rpModeLbl  = CreateReceiptTile("PAYMENT MODE", out modTile);
            beingRow.Controls.Add(beiTile, 0, 0);
            beingRow.Controls.Add(modTile, 1, 0);
            layout.Controls.Add(beingRow, 0, 4);

            // ── Row 5: Amount box + Balance After ────────────────────────────────────
            var amountRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor };
            amountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            amountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

            var amtBoxPanel = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Margin = new Padding(0, 2, 6, 2), Padding = new Padding(0) };
            amtBoxPanel.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using (var pen = new Pen(PrimaryColor, 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, p.Width - 3, p.Height - 3);
            };
            var amtInner = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
            amtInner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
            amtInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            amtInner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
            amtInner.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "GHc", ForeColor = PrimaryColor, Font = new Font("Arial Narrow", 15F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
            _rpAmountLbl = new Label { Dock = DockStyle.Fill, ForeColor = PrimaryColor, Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            amtInner.Controls.Add(_rpAmountLbl, 1, 0);
            amtInner.Controls.Add(new Label { Dock = DockStyle.Fill, Text = ".00", ForeColor = PrimaryColor, Font = new Font("Arial Narrow", 15F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            amtBoxPanel.Controls.Add(amtInner);
            amountRow.Controls.Add(amtBoxPanel, 0, 0);

            var balPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 245, 233), Margin = new Padding(0, 2, 0, 2), Padding = new Padding(8, 2, 8, 2) };
            var balCaption = new Label { Text = "BALANCE AFTER", Dock = DockStyle.Top, Height = 14, ForeColor = Color.FromArgb(46, 125, 50), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold) };
            _rpBalanceLbl = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(46, 125, 50), Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            balPanel.Controls.Add(_rpBalanceLbl);
            balPanel.Controls.Add(balCaption);
            amountRow.Controls.Add(balPanel, 1, 0);
            layout.Controls.Add(amountRow, 0, 5);

            // ── Row 6: Bursar + Signature ────────────────────────────────────────────
            var bursarRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = SurfaceColor };
            bursarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            bursarRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            Panel burTile;
            _rpBursarLbl = CreateReceiptTile("BURSAR / CASHIER", out burTile);
            bursarRow.Controls.Add(burTile, 0, 0);
            bursarRow.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "........................\r\nSignature",
                ForeColor = MutedTextColor,
                Font = new Font("Georgia", 9F),
                TextAlign = ContentAlignment.MiddleCenter
            }, 1, 0);
            layout.Controls.Add(bursarRow, 0, 6);

            _receiptPreviewShell.Controls.Add(layout);
            return _receiptPreviewShell;
        }

        private Label CreateReceiptTile(string caption, out Panel tile)
        {
            tile = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 249, 255),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 0, 3, 0)
            };
            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(247, 249, 255)
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            inner.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = caption,
                ForeColor = MutedTextColor,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
            }, 0, 0);
            var valueLbl = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = PrimaryColor,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            inner.Controls.Add(valueLbl, 0, 1);
            tile.Controls.Add(inner);
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
            _rpBeingLbl.Text       = beingBox?.Text.Trim() ?? "";
            _rpModeLbl.Text        = paymentModeBox?.Text ?? "";
            _rpAmountLbl.Text      = amount > 0 ? ((int)Math.Floor(amount)).ToString("N0") : "—";
            _rpBalanceLbl.Text     = "GHc " + projected.ToString("N2");
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
                if (_studentInfoCard    != null) _studentInfoCard.Visible    = false;
                if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = false;
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
                    if (_studentInfoCard    != null) _studentInfoCard.Visible    = false;
                    if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = true;
                    UpdateContinueButton();
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
                if (_studentInfoNameLbl    != null) _studentInfoNameLbl.Text    = $"{student.FullName}  ·  {student.ClassID}  ·  ID: {studentId}";
                if (_studentInfoBalanceLbl != null) _studentInfoBalanceLbl.Text = "Balance: GHc " + (balance ?? 0m).ToString("N2");
                if (_studentInfoCard       != null) _studentInfoCard.Visible    = true;
                if (_studentNotFoundLbl    != null) _studentNotFoundLbl.Visible = false;
                UpdateContinueButton();
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Lookup failed";
                UIHelper.ShowError("Lookup error: " + ex.Message, "Payment");
                if (_studentInfoCard    != null) _studentInfoCard.Visible    = false;
                if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = false;
                UpdateContinueButton();
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

                    // Update balance label in receipt preview to the actual saved value
                    if (_rpBalanceLbl != null)
                        _rpBalanceLbl.Text = "GHc " + newBalance.ToString("N2");

                    // Show inline success state (no dialog)
                    if (_successBannerLbl != null)
                        _successBannerLbl.Text = $"Payment Recorded  ·  GHc {amountPaid:N2} from {studentNameBox.Text}  ·  New balance: GHc {newBalance:N2}";
                    if (_successBanner     != null) _successBanner.Visible     = true;
                    if (_preRecordActions  != null) _preRecordActions.Visible  = false;
                    if (_postRecordActions != null) _postRecordActions.Visible = true;
                    if (_receiptPreviewShell != null) _receiptPreviewShell.BorderStyle = BorderStyle.FixedSingle;

                    amountBox.Text = "";
                    receiptNumberLabel.Text = "No. " + CreateReceiptNumber();
                    statusLabel.Text = "Payment recorded";
                    await LoadPaymentHistory();
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
            bursarBox.Text = "";
            cashChequeBox.Text = "";
            paymentModeBox.SelectedIndex = 0;
            paymentDatePicker.Value = DateTime.Today;
            receiptNumberLabel.Text = "No. " + CreateReceiptNumber();
            lastPrintedReceipt = null;
            statusLabel.Text = "Ready.";
            if (feeTypeBox        != null) feeTypeBox.Text = "";
            _lastFeeTypeAutoFilled = "";
            if (_studentInfoCard    != null) _studentInfoCard.Visible    = false;
            if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = false;
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
