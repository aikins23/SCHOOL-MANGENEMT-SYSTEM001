# Fees Payment UI — 3-Step Wizard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the dense single-panel receipt form in `frmFessPayment` with a 3-step wizard (Student Lookup → Payment Details → Receipt Preview), keeping all existing business logic and print output unchanged.

**Architecture:** `BuildPaymentPanel()` is replaced by `BuildWizardPanel()`, which contains a dot-and-line progress indicator and three step sub-panels (only one visible at a time). All data/service methods (`LookupStudent`, `RecordPayment`, `LoadPaymentHistory`, `DrawReceipt`, `BuildReceiptPrintData`) are preserved; only their UI wiring and post-action UI updates change. The `ReceiptPrintData` struct and `DrawReceipt()` print path are untouched.

**Tech Stack:** C# WinForms (.NET Framework), existing `UiTheme`, `StudentService`, `IFeeRepository`, `UIHelper`, `ConfirmationHelper`

---

## File Map

| File | Change |
|---|---|
| `frmFessPayment.cs` | All changes — new wizard methods added, `BuildPaymentPanel` replaced, `LookupStudent`/`RecordPayment`/`ClearPaymentForm` tweaked |

---

### Task 1: Declare new wizard control fields

**Files:**
- Modify: `frmFessPayment.cs` — insert new fields after `private ReceiptPrintData lastPrintedReceipt;`

- [ ] **Step 1: Add fields**

Open `frmFessPayment.cs`. After the line `private ReceiptPrintData lastPrintedReceipt;` (around line 33), insert:

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add frmFessPayment.cs
git commit -m "refactor(fees-wizard): declare wizard state and receipt-preview control fields"
```

---

### Task 2: Build progress indicator

**Files:**
- Modify: `frmFessPayment.cs` — add `BuildProgressIndicator()` and `RefreshProgressIndicator()` after `BuildHeader()`

- [ ] **Step 1: Add methods**

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(fees-wizard): add progress indicator with paint handler"
```

---

### Task 3: Build Step 1 — Student Lookup + Fee Type

**Files:**
- Modify: `frmFessPayment.cs` — add `BuildStep1Panel()`, `UpdateContinueButton()`, `GoToStep2()`

- [ ] **Step 1: Add BuildStep1Panel and helpers**

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(fees-wizard): add Step 1 — student lookup + editable fee type selector"
```

---

### Task 4: Build Step 2 — Payment Details

**Files:**
- Modify: `frmFessPayment.cs` — add `BuildStep2Panel()`, `UpdatePreviewButton()`

- [ ] **Step 1: Add BuildStep2Panel**

```csharp
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
    var previewBtn = CreatePrimaryButton("Preview Receipt →", GoToStep3);
    previewBtn.Name = "previewReceiptBtn";
    previewBtn.Enabled = false;
    btnRow.Controls.Add(backBtn2, 0, 0);
    btnRow.Controls.Add(new Panel { BackColor = SurfaceColor }, 1, 0);
    btnRow.Controls.Add(previewBtn, 2, 0);
    layout.Controls.Add(btnRow, 0, 4);

    _step2Panel.Controls.Add(layout);
    return _step2Panel;
}

private void UpdatePreviewButton()
{
    if (_step2Panel == null) return;
    var btn = _step2Panel.Controls.Find("previewReceiptBtn", true).FirstOrDefault() as Button;
    if (btn == null) return;
    decimal amount;
    btn.Enabled = decimal.TryParse(amountBox?.Text, out amount) && amount > 0;
}

private void GoToStep3()
{
    RefreshReceiptPreview();
    ShowStep(3);
}
```

- [ ] **Step 2: Commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(fees-wizard): add Step 2 — payment details with preview-button guard"
```

---

### Task 5: Build Step 3 — Receipt Preview + Action Swap

**Files:**
- Modify: `frmFessPayment.cs` — add `BuildStep3Panel()`, `BuildReceiptPreviewControl()`, `CreateReceiptTile()`, `RefreshReceiptPreview()`, `BuildPreRecordActions()`, `BuildPostRecordActions()`

- [ ] **Step 1: Add all Step 3 methods**

```csharp
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
    _preRecordActions.Dock  = DockStyle.Fill;
    _postRecordActions.Dock = DockStyle.Fill;
    _postRecordActions.Visible = false;
    actionContainer.Controls.Add(_preRecordActions);
    actionContainer.Controls.Add(_postRecordActions);
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
    if (!string.IsNullOrWhiteSpace(logoPath)) { try { _rpLogoPictureBox.Image = Image.FromFile(logoPath); } catch { } }
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

/// <summary>
/// Creates a labelled tile: caption on top, value label filling the rest.
/// Returns the value label; tile is the out Panel to add to the parent.
/// </summary>
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

    _rpStudentIdLbl.Text  = studentIdBox?.Text.Trim() ?? "";
    _rpClassLbl.Text      = classBox?.Text.Trim() ?? "";
    _rpNameLbl.Text       = studentNameBox?.Text.Trim() ?? "";
    _rpAmountWordsLbl.Text = amountWordsBox?.Text.Trim() ?? "";
    _rpBeingLbl.Text      = beingBox?.Text.Trim() ?? "";
    _rpModeLbl.Text       = paymentModeBox?.Text ?? "";
    _rpAmountLbl.Text     = amount > 0 ? ((int)Math.Floor(amount)).ToString("N0") : "—";
    _rpBalanceLbl.Text    = "GHc " + projected.ToString("N2");
    _rpBursarLbl.Text     = bursarBox?.Text.Trim() ?? "";
    _rpReceiptNumLbl.Text = receiptNumberLabel?.Text ?? "";
    _rpDateLbl.Text       = paymentDatePicker?.Value.ToString("dd/MM/yyyy") ?? "";
}
```

- [ ] **Step 2: Commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(fees-wizard): add Step 3 receipt preview, action swap, success banner"
```

---

### Task 6: Wire ShowStep navigation; update LookupStudent, RecordPayment, ClearPaymentForm

**Files:**
- Modify: `frmFessPayment.cs`

- [ ] **Step 1: Add ShowStep method** (add after `BuildWizardPanel` in task 7, but declare it now)

```csharp
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
```

- [ ] **Step 2: Update LookupStudent**

Replace the entire `LookupStudent()` method with:

```csharp
private async void LookupStudent()
{
    if (studentIdBox == null || string.IsNullOrWhiteSpace(studentIdBox.Text))
    {
        studentNameBox.Text = "";
        classBox.Text = "";
        balanceBox.Text = "";
        if (amountWordsBox != null) amountWordsBox.Text = "";
        if (_studentInfoCard   != null) _studentInfoCard.Visible   = false;
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
            if (amountWordsBox     != null) amountWordsBox.Text      = "";
            if (_studentInfoCard   != null) _studentInfoCard.Visible   = false;
            if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = true;
            UpdateContinueButton();
            statusLabel.Text = "Student not found";
            return;
        }

        if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = false;
        studentNameBox.Text = student.FullName;
        classBox.Text = student.ClassID;

        decimal? balance = await _feeRepository.GetLatestBalanceAsync(studentId);
        if (!balance.HasValue)
            balance = await _feeRepository.GetDefaultBalanceAsync(studentId, student.ClassID);
        balanceBox.Text = (balance ?? 0m).ToString("0.00");

        if (_studentInfoNameLbl    != null) _studentInfoNameLbl.Text    = $"{student.FullName}  ·  {student.ClassID}  ·  ID: {studentId}";
        if (_studentInfoBalanceLbl != null) _studentInfoBalanceLbl.Text = "Balance: GHc " + (balance ?? 0m).ToString("N2");
        if (_studentInfoCard       != null) _studentInfoCard.Visible    = true;

        UpdateContinueButton();
        statusLabel.Text = "Student details loaded";
    }
    catch (Exception ex)
    {
        if (_studentInfoCard    != null) _studentInfoCard.Visible    = false;
        if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = false;
        UpdateContinueButton();
        statusLabel.Text = "Lookup failed";
        UIHelper.ShowError("Lookup error: " + ex.Message, "Payment");
    }
}
```

- [ ] **Step 3: Update RecordPayment success block**

Inside `RecordPayment()`, find the `if (success)` block and replace it with:

```csharp
if (success)
{
    decimal amountPaid2 = 0m;
    decimal.TryParse(amountBox.Text, out amountPaid2);
    decimal newBal = Math.Max(0m, currentBalance - amountPaid2);

    balanceBox.Text = newBal.ToString("0.00");
    lastPrintedReceipt = BuildReceiptPrintData();

    // Update balance label in receipt preview to the actual saved value
    if (_rpBalanceLbl != null)
        _rpBalanceLbl.Text = "GHc " + newBal.ToString("N2");

    // Show inline success state (no dialog)
    if (_successBannerLbl != null)
        _successBannerLbl.Text = $"Payment Recorded  ·  GHc {amountPaid2:N2} from {studentNameBox.Text}  ·  New balance: GHc {newBal:N2}";
    if (_successBanner     != null) _successBanner.Visible     = true;
    if (_preRecordActions  != null) _preRecordActions.Visible  = false;
    if (_postRecordActions != null) _postRecordActions.Visible = true;

    receiptNumberLabel.Text = "No. " + CreateReceiptNumber();
    statusLabel.Text = "Payment recorded";
    await LoadPaymentHistory();
    // (Remove the UIHelper.ShowSuccess call that was here — success is shown inline)
}
```

- [ ] **Step 4: Update ClearPaymentForm**

At the end of `ClearPaymentForm()`, after the existing reset lines, add:

```csharp
if (feeTypeBox        != null) feeTypeBox.Text = "";
_lastFeeTypeAutoFilled = "";
if (_studentInfoCard   != null) _studentInfoCard.Visible    = false;
if (_studentNotFoundLbl != null) _studentNotFoundLbl.Visible = false;
UpdateContinueButton();
ShowStep(1);
```

Also remove `beingBox.Text = "School fees payment";` from `ClearPaymentForm()` — Being is now pre-filled from fee type, not hardcoded on clear.

- [ ] **Step 5: Commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(fees-wizard): wire ShowStep, update LookupStudent/RecordPayment/ClearPaymentForm"
```

---

### Task 7: Assemble BuildWizardPanel; swap into BuildModernPaymentView

**Files:**
- Modify: `frmFessPayment.cs`

- [ ] **Step 1: Add BuildWizardPanel**

```csharp
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

    statusLabel = new Label
    {
        Dock = DockStyle.Fill,
        ForeColor = MutedTextColor,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Segoe UI", 9.5F)
    };
    layout.Controls.Add(statusLabel, 0, 2);

    shell.Controls.Add(layout);
    return shell;
}
```

- [ ] **Step 2: Replace BuildPaymentPanel call in BuildModernPaymentView**

Find:
```csharp
root.Controls.Add(BuildPaymentPanel(), 0, 1);
```

Replace with:
```csharp
root.Controls.Add(BuildWizardPanel(), 0, 1);
```

- [ ] **Step 3: Build**

```
dotnet build
```

Expected: 0 errors. Common errors and fixes:
- `'Panel' does not contain definition for 'Find'` → use `Controls.Find(name, true)` (correct, already used above)
- `Argument 2: cannot convert from 'out Panel' to ...` → ensure `CreateReceiptTile` signature matches call sites
- Duplicate `statusLabel` initialization → remove the one in `BuildPaymentPanel` (the method is no longer called)

- [ ] **Step 4: Manual walkthrough test**

Launch the app and verify each path:

| Path | Expected |
|---|---|
| Open form | Step 1 visible, progress indicator shows step 1 active, Continue disabled |
| Type valid Student ID | Student card appears, Continue still disabled |
| Select/type fee type | Continue button enables |
| Continue → Step 2 | Summary bar shows name + balance, Being pre-filled with fee type |
| Enter amount | Amount in words auto-updates, Preview Receipt button enables |
| Preview Receipt → Step 3 | Receipt renders with logo, all fields, projected balance |
| Back from Step 3 → Step 2 | All Step 2 fields preserved |
| Record Payment | Success banner appears, Print + New Payment buttons shown, balance updated |
| Print Receipt | Print preview dialog opens |
| New Payment | Wizard resets to Step 1, all fields cleared |
| Invalid student ID | Red inline message below ID field, info card hidden |
| Clear from Step 3 | Wizard resets to Step 1 |

- [ ] **Step 5: Final commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(fees-wizard): complete 3-step wizard — assemble BuildWizardPanel, retire BuildPaymentPanel"
```
