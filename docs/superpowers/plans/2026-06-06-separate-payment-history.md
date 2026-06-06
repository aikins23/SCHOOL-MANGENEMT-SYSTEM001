# Separate Payment History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the payment-history browser out of `frmFessPayment` into a dedicated `frmPaymentHistory` screen, opened from the dashboard and from a button on the Fees Payment form.

**Architecture:** A new standalone WinForms form owns the history grid/search/refresh and reuses the existing `FeeRepository.GetPaymentHistoryTableAsync()` and `UiTheme` styling. The history section is removed from `frmFessPayment` so it's just the 3-step wizard. RBAC + a dashboard nav entry gate access. No data-layer changes.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-separate-payment-history-design.md`

---

## Conventions (read first)

- **No unit-test framework** exists. Verification gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) offline reflection render harness for forms (see user memory `offline-winforms-render-harness.md`).
- **Explicit-include csproj:** every new `.cs` MUST get a `<Compile Include="..." />` entry (code-only forms use a self-closing entry, no `<SubType>`).
- **`frmFessPayment.cs` carries unrelated WIP.** Task 3 edits are surgical and each is followed by a build. Recommended execution is **inline** (not subagents) because the removals are interconnected in a ~2900-line file.
- `UiTheme` statics used by the grid: `UiTheme.Surface`, `UiTheme.Navy`, `UiTheme.Text`, `UiTheme.GoldSoft`, `UiTheme.Border`, `UiTheme.SurfaceAlt`, and `UiTheme.StyleDataGrid(grid, true)`.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Create** `frmPaymentHistory.cs` — the standalone history screen.
- **Modify** `Services/AuthService.cs` — RBAC entry.
- **Modify** `frmDashboard.cs` — nav entry.
- **Modify** `frmFessPayment.cs` — remove embedded history; repoint header button.
- **Modify** `kingdom_Preparatory_School_Management_System.csproj` — register the new form.

---

### Task 1: New `frmPaymentHistory` form + RBAC

**Files:**
- Create: `frmPaymentHistory.cs`
- Modify: `Services/AuthService.cs` (after the `frmEmailSettings`/`frmSchoolInfo` access lines)
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `frmPaymentHistory.cs`**

```csharp
using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Standalone, searchable payment-history browser. Split out of frmFessPayment so the
    /// payment wizard isn't cramped. Read-only; reuses FeeRepository.GetPaymentHistoryTableAsync.
    /// Access: Accountant / Director / Administrator / Headmaster.
    /// </summary>
    public class frmPaymentHistory : Form
    {
        private readonly FeeRepository _feeRepository = new FeeRepository(AppConfig.ConnectionString);
        private DataGridView _grid;
        private TextBox _searchBox;
        private Label _countLbl;
        private Button _refreshBtn;

        public frmPaymentHistory()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmPaymentHistory", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "Payment History";
            Size = new Size(1020, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.Surface;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 46, Text = "  Payment History",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = UiTheme.Navy, TextAlign = ContentAlignment.MiddleLeft
            };

            var bar = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(12, 7, 12, 7), BackColor = UiTheme.Surface };
            var searchLbl = new Label { Dock = DockStyle.Left, Width = 56, Text = "Search:", TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10F), ForeColor = UiTheme.Text };
            _searchBox = new TextBox { Dock = DockStyle.Left, Width = 360, Font = new Font("Segoe UI", 10F) };
            _searchBox.TextChanged += (s, e) => Filter();
            _refreshBtn = new Button { Dock = DockStyle.Right, Width = 100, Text = "Refresh", FlatStyle = FlatStyle.Flat };
            _refreshBtn.Click += async (s, e) => await LoadAsync();
            _countLbl = new Label
            {
                Dock = DockStyle.Right, Width = 116, Text = "0 records", ForeColor = Color.White,
                BackColor = UiTheme.Navy, TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
            };
            bar.Controls.Add(_searchBox);
            bar.Controls.Add(searchLbl);
            bar.Controls.Add(_countLbl);
            bar.Controls.Add(_refreshBtn);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = UiTheme.Surface, BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                EnableHeadersVisualStyles = false
            };
            UiTheme.StyleDataGrid(_grid, true);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.Navy;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            _grid.ColumnHeadersHeight = 32;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.DefaultCellStyle.BackColor = UiTheme.Surface;
            _grid.DefaultCellStyle.ForeColor = UiTheme.Text;
            _grid.DefaultCellStyle.SelectionBackColor = UiTheme.GoldSoft;
            _grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
            _grid.GridColor = UiTheme.Border;
            _grid.CellFormatting += Grid_CellFormatting;

            Controls.Add(_grid);
            Controls.Add(bar);
            Controls.Add(title);
        }

        private async Task LoadAsync()
        {
            try
            {
                DataTable table = await _feeRepository.GetPaymentHistoryTableAsync();
                _grid.DataSource = table;
                ApplyColumnLayout();
                _countLbl.Text = table.Rows.Count + " records";
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("History load error: " + ex.Message, "Payment History");
            }
        }

        private void Filter()
        {
            if (!(_grid.DataSource is DataTable table)) return;
            string filter = _searchBox.Text.Trim().Replace("'", "''");
            table.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                ? ""
                : string.Format(
                    "[STUDENT ID] LIKE '%{0}%' OR [STUDENT NAME] LIKE '%{0}%' OR [CLASS ID] LIKE '%{0}%' OR [BURSAR NAME] LIKE '%{0}%'",
                    filter);
            _countLbl.Text = table.DefaultView.Count + " records";
        }

        private void ApplyColumnLayout()
        {
            if (_grid.Columns.Count == 0) return;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SetColumn("STUDENT ID", 76, 70);
            SetColumn("CLASS ID", 82, 75);
            SetColumn("STUDENT NAME", 170, 150);
            SetColumn("AMOUNT PAID", 104, 95, DataGridViewContentAlignment.MiddleRight);
            SetColumn("BALANCE", 96, 90, DataGridViewContentAlignment.MiddleRight);
            SetColumn("PAYMENT DATE", 104, 95, DataGridViewContentAlignment.MiddleCenter);
            SetColumn("PAYMENT TIME", 96, 90, DataGridViewContentAlignment.MiddleCenter);
            SetColumn("PAYMENT MODE", 118, 105);
            SetColumn("BURSAR NAME", 136, 120);
        }

        private void SetColumn(string name, int fillWeight, int minWidth,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            if (!_grid.Columns.Contains(name)) return;
            var c = _grid.Columns[name];
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            c.FillWeight = fillWeight;
            c.MinimumWidth = minWidth;
            c.DefaultCellStyle.Alignment = alignment;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null || e.Value == DBNull.Value || e.ColumnIndex < 0) return;
            string columnName = _grid.Columns[e.ColumnIndex].Name;

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
    }
}
```

- [ ] **Step 2: Register in csproj**

In `kingdom_Preparatory_School_Management_System.csproj`, after `<Compile Include="frmSchoolInfo.cs" />` add:

```xml
    <Compile Include="frmPaymentHistory.cs" />
```

- [ ] **Step 3: Add RBAC entry**

In `Services/AuthService.cs`, after the `["frmSchoolInfo"] = new[] { UserRole.Director, UserRole.Administrator },` line add:

```csharp
            ["frmPaymentHistory"]         = new[] { UserRole.Accountant, UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If `UiTheme` lacks any referenced member, open `UiTheme` and match the actual name — but `Surface/Navy/Text/GoldSoft/Border/SurfaceAlt/StyleDataGrid` are all used by `frmFessPayment` today.)

- [ ] **Step 5: Render-verify (offline harness)**

Render `frmPaymentHistory` to PNG (set `AuthService.CurrentUser` backing field to a `UserSession { Role = Accountant }`, off-screen `Show()`, `DrawToBitmap`). Confirm: title, Search box + count badge + Refresh, and the grid. If LocalDB is down the `Load` populates nothing — layout still renders; acceptable.

- [ ] **Step 6: Commit**

```bash
git add frmPaymentHistory.cs Services/AuthService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(payment-history): standalone frmPaymentHistory screen + RBAC"
```

---

### Task 2: Dashboard nav entry

**Files:**
- Modify: `frmDashboard.cs`

The dashboard builds nav buttons in `BuildModernDashboard`. The "Fees Payment" button is added unconditionally around line 265: `nav.Controls.Add(CreateNavButton("Fees Payment", () => OpenForm(new frmFessPayment())));`. Add a "Payment History" entry for the finance roles next to the existing role-gated entries.

- [ ] **Step 1: Add the nav button**

In `frmDashboard.cs`, locate the block that adds role-specific finance nav (near the `Admission Approvals` / `Settings` blocks, around lines 272–287). Immediately BEFORE the `if (role == UserRole.Director || role == UserRole.Administrator)` Settings block, add:

```csharp
            if (role == AuthService.UserRole.Accountant || role == AuthService.UserRole.Director ||
                role == AuthService.UserRole.Administrator || role == AuthService.UserRole.Headmaster)
            {
                nav.Controls.Add(CreateNavButton("Payment History", () => OpenForm(new frmPaymentHistory())));
            }
```

(Use the exact `AuthService.UserRole.*` qualifier the surrounding code uses — the dashboard references `AuthService.UserRole.Accountant` etc.)

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(payment-history): add Payment History to dashboard nav (finance roles)"
```

---

### Task 3: Remove the embedded history from `frmFessPayment` + add the on-form button

**Files:**
- Modify: `frmFessPayment.cs`

Each step is followed by a build so a missed reference surfaces immediately. Do them in order.

- [ ] **Step 1: Drop the history panel from the root layout**

Find (around lines 174–188):

```csharp
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
```

Replace with:

```csharp
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 1,
                BackColor = PageBackColor,
                Padding = new Padding(26)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildWizardPanel(), 0, 0);
```

- [ ] **Step 2: Drop the history footer from the wizard layout**

Find (around lines 387–415):

```csharp
                ColumnCount = 1,
                RowCount = 5,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // Title row
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // Progress
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Step container
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // Status
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // Footer
```

Replace the `RowCount = 5` with `RowCount = 4` and delete the `// Footer` RowStyle line, giving:

```csharp
                ColumnCount = 1,
                RowCount = 4,
                BackColor = SurfaceColor
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // Title row
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // Progress
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Step container
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // Status
```

Then delete the footer add line:

```csharp
            layout.Controls.Add(BuildHistoryFooter(), 0, 4);
```

- [ ] **Step 3: Repoint the wizard-header "Refresh" to "View Payment History"**

In `BuildWizardTitleRow` (around line 465) replace:

```csharp
            actions.Controls.Add(CreateSecondaryButton("Refresh", async () => await LoadPaymentHistory()));
```

with:

```csharp
            actions.Controls.Add(CreateSecondaryButton("View Payment History", () => new frmPaymentHistory().Show()));
```

- [ ] **Step 4: Remove the post-record history refresh**

In `RecordPayment` (around line 2855) delete the line:

```csharp
                    await LoadPaymentHistory();
```

- [ ] **Step 5: Remove the `btn_Re` wiring**

Near line 151 delete:

```csharp
            btn_Re.Click              += btn_Re_Click;
```

- [ ] **Step 6: Remove the `historySearchBox` initialization**

In `InitializeFormControls` (around lines 306–308) delete:

```csharp
            historySearchBox = CreateTextBox();
            SetPlaceholder(historySearchBox, "Search history (Name, ID, Class, Bursar)...");
            historySearchBox.TextChanged += (s, e) => FilterHistory();
```

- [ ] **Step 7: Delete the now-dead history methods and fields**

Delete these whole methods: `FilterHistory` (~1834), `BuildHistoryPanel` (~1854), `ApplyPaymentHistoryGridLayout` (~1905), `SetHistoryColumn` (~1924), `PaymentGrid_CellFormatting` (~1938), `LoadPaymentHistory` (~2871), `btn_Re_Click` (~2923), and `BuildHistoryFooter` (~472).

Delete the field declarations: `private DataGridView paymentGrid;` (~33), `private TextBox historySearchBox;` (~37), `private Label _historyCountLbl;` (~88).

- [ ] **Step 8: Handle `BuildHeader` (dead-code check)**

`BuildHeader` (~320–368) references `LoadPaymentHistory` (line 363). Search for its caller:

Run: `grep -n "BuildHeader()" frmFessPayment.cs`

- If it returns ONLY the definition line (no caller), delete the whole `BuildHeader` method.
- If it IS called somewhere, instead replace its `CreateSecondaryButton("Refresh", async () => await LoadPaymentHistory())` line with `CreateSecondaryButton("View Payment History", () => new frmPaymentHistory().Show())` and keep the method.

- [ ] **Step 9: Update the stale subtitle text**

In `BuildWizardTitleRow` (around line 345), change the subtitle from `"Look up a student, record payment, and review payment history"` to `"Look up a student and record a payment"` (the history view is now separate). If the same subtitle string also exists in the dead `BuildHeader` you deleted, ignore it.

- [ ] **Step 10: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. If the compiler reports an unresolved reference to any removed member (`paymentGrid`, `historySearchBox`, `_historyCountLbl`, `LoadPaymentHistory`, `FilterHistory`, `SetPlaceholder` use, etc.), that's a missed call site — fix it by removing/repointing that reference, then rebuild. Do not commit a failing build.

- [ ] **Step 11: Render-verify both forms**

Render `frmFessPayment` (Accountant) via the harness: confirm the wizard fills the window and there is **no** history grid/footer, and the header shows a "View Payment History" button. (`frmPaymentHistory` was verified in Task 1.)

- [ ] **Step 12: Commit**

```bash
git add frmFessPayment.cs
git commit -m "refactor(fees): remove embedded payment history; add View Payment History button"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app):** open Payment History from the dashboard and from the Fees Payment "View Payment History" button; confirm the grid loads, search filters by name/ID/class/bursar, Refresh reloads; record a payment then open history and Refresh → the new row appears; confirm Teacher/Parent cannot open Payment History; confirm the Fees Payment wizard is no longer cramped.

## Self-review notes

- **Spec coverage:** new standalone form (Task 1) ✓, reuse `GetPaymentHistoryTableAsync` + `UiTheme` (Task 1) ✓, RBAC Accountant/Director/Admin/Headmaster (Task 1) ✓, dashboard nav (Task 2) ✓, remove embedded history from `frmFessPayment` (Task 3) ✓, on-form "View Payment History" button (Task 3 Step 3) ✓, no data-layer change ✓. **Deviation:** spec said the button lives in the "post-record action bar"; this plan places it in the always-visible wizard header (`BuildWizardTitleRow`) instead — strictly better (available before and after recording) and still "a button on the Fees Payment form". Noted intentionally.
- **Type/name consistency:** new form members (`_grid/_searchBox/_countLbl/_refreshBtn`, `LoadAsync/Filter/ApplyColumnLayout/SetColumn/Grid_CellFormatting`) are self-consistent; column header names match the aliases returned by `GetPaymentHistoryTableAsync` (`STUDENT ID`, `CLASS ID`, `STUDENT NAME`, `AMOUNT PAID`, `BALANCE`, `PAYMENT DATE`, `PAYMENT TIME`, `PAYMENT MODE`, `BURSAR NAME`).
- **Placeholder scan:** no TBD/TODO; every code step shows complete code; the one conditional step (Task 3 Step 8) gives an exact grep and both branches.
