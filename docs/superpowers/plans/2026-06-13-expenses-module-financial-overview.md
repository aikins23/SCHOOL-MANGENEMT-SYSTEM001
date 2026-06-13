# Expenses Module + Financial Overview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A CRUD screen to record/manage expenses (reusing the existing `Expenses` table) plus a selectable-window Total Income / Expenses / Fund summary on the Expenses form and as role-gated tiles on the dashboard.

**Architecture:** Pure helpers `ExpenseAmount` (varchar↔decimal) and `FinancePeriod` (window→date range, reusing the term calendar) are unit-tested. `ExpenseRepository` does CRUD + configurable categories. `DashboardRepository`/`DashboardService` gain date-range income/expense totals. `frmExpenses` and three dashboard tiles consume them.

**Tech Stack:** C#/.NET 4.7.2 WinForms, OleDb/MSOLEDBSQL, explicit-include csproj.

**Spec:** `docs/superpowers/specs/2026-06-13-expenses-module-financial-overview-design.md`

---

## Conventions
- Gates: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors (use `-p:OutputPath=bin/Verify/` if the exe is locked by a debug session); `dotnet run --project "Tests/Kingdom.Tests/Kingdom.Tests.csproj"` → all pass.
- Reused: `Expenses` columns `ID,Expenses_name,Purpose,Description,Date_Time,Amount(varchar),Payee,payer`; income = `SUM(payment_record.Amount_paid)` over `payment_record.[Date]`; `AppConfig.Leave.GetTerm(date)`/`CurrentTerm` → `(TermName,Start,End)`; `AuthService.CurrentUser.DisplayName`; `DashboardRepository.ExecuteScalarDecimalAsync`.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: Pure helpers `ExpenseAmount` + `FinancePeriod` (+ tests)

**Files:** Create `Common/ExpenseAmount.cs`, `Common/FinancePeriod.cs`; modify csproj, `Tests/Kingdom.Tests/Program.cs`.

- [ ] **Step 1: `Common/ExpenseAmount.cs`**
```csharp
using System.Globalization;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>Bridges the legacy varchar Expenses.Amount column and decimal. Mirrors the
    /// dashboard SQL (REPLACE commas, TRY_CAST) so UI totals and chart totals agree.</summary>
    public static class ExpenseAmount
    {
        public static decimal Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            string cleaned = raw.Replace(",", "").Replace("GHS", "").Replace("₵", "").Trim();
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }

        public static string Store(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
```

- [ ] **Step 2: `Common/FinancePeriod.cs`**
```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public enum FinanceWindow { Term, Year, AllTime, Custom }

    /// <summary>Resolves a reporting window to an inclusive [From,To] date range + a label.</summary>
    public static class FinancePeriod
    {
        public static (DateTime From, DateTime To, string Label) Resolve(
            FinanceWindow window, DateTime today, DateTime? customFrom = null, DateTime? customTo = null)
        {
            switch (window)
            {
                case FinanceWindow.Year:
                    return (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31), today.Year.ToString());
                case FinanceWindow.AllTime:
                    return (new DateTime(2000, 1, 1), new DateTime(2100, 12, 31), "All time");
                case FinanceWindow.Custom:
                {
                    DateTime f = (customFrom ?? today).Date, t = (customTo ?? today).Date;
                    if (f > t) { var tmp = f; f = t; t = tmp; }
                    return (f, t, f.ToString("dd MMM yyyy") + " - " + t.ToString("dd MMM yyyy"));
                }
                default:
                    var term = AppConfig.Leave.GetTerm(today);
                    return (term.Start, term.End, term.TermName);
            }
        }
    }
}
```

- [ ] **Step 3: csproj** — next to `<Compile Include="Common\ImportCredentials.cs" />` add:
```xml
    <Compile Include="Common\ExpenseAmount.cs" />
    <Compile Include="Common\FinancePeriod.cs" />
```

- [ ] **Step 4: Tests** — in `Tests/Kingdom.Tests/Program.cs` register after the ImportCredentials TestCase:
```csharp
                new TestCase("ExpenseAmount parses legacy varchar and round-trips", ExpenseAmount_ParsesAndStores),
                new TestCase("FinancePeriod resolves term/year/all-time/custom windows", FinancePeriod_ResolvesWindows),
```
and add the methods after `ImportCredentials_DerivesUsernamesAndPasswords`:
```csharp
        private static void ExpenseAmount_ParsesAndStores()
        {
            AssertEx.Equal(1500.00m, ExpenseAmount.Parse("1,500.00"));
            AssertEx.Equal(1500.00m, ExpenseAmount.Parse("GHS 1500"));
            AssertEx.Equal(0m, ExpenseAmount.Parse(""));
            AssertEx.Equal(0m, ExpenseAmount.Parse("abc"));
            AssertEx.Equal("1500.00", ExpenseAmount.Store(1500m));
            AssertEx.Equal(1234.50m, ExpenseAmount.Parse(ExpenseAmount.Store(1234.5m))); // round-trip
        }

        private static void FinancePeriod_ResolvesWindows()
        {
            var d = new DateTime(2026, 6, 15);
            var year = FinancePeriod.Resolve(FinanceWindow.Year, d);
            AssertEx.Equal(new DateTime(2026, 1, 1), year.From);
            AssertEx.Equal(new DateTime(2026, 12, 31), year.To);
            AssertEx.Equal("2026", year.Label);

            var all = FinancePeriod.Resolve(FinanceWindow.AllTime, d);
            AssertEx.True(all.From <= new DateTime(2000, 1, 1) && all.To >= new DateTime(2099, 12, 31), "all-time spans wide");

            var cust = FinancePeriod.Resolve(FinanceWindow.Custom, d, new DateTime(2026, 5, 10), new DateTime(2026, 5, 1));
            AssertEx.Equal(new DateTime(2026, 5, 1), cust.From);   // swapped because from > to
            AssertEx.Equal(new DateTime(2026, 5, 10), cust.To);

            var term = FinancePeriod.Resolve(FinanceWindow.Term, d); // May-Aug term for June
            AssertEx.True(term.From <= d && d <= term.To, "today falls within the resolved term");
        }
```

- [ ] **Step 5: Build + test** → new tests PASS. **Commit**: `feat(expenses): ExpenseAmount + FinancePeriod helpers + tests`.

---

### Task 2: `Expense` model + `ExpenseRepository`

**Files:** Create `Models/Expense.cs`, `Data/ExpenseRepository.cs`; modify csproj.

- [ ] **Step 1: `Models/Expense.cs`**
```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";      // -> Purpose column
        public string Description { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string Payee { get; set; } = "";
        public string Payer { get; set; } = "";
    }
}
```

- [ ] **Step 2: `Data/ExpenseRepository.cs`** — OleDb, positional params. Methods:
  - `EnsureTablesAsync()`: `IF OBJECT_ID(N'ExpenseCategories',N'U') IS NULL CREATE TABLE ExpenseCategories (Name NVARCHAR(60) NOT NULL PRIMARY KEY);` then, if `SELECT COUNT(*) FROM ExpenseCategories` == 0, insert seeds `Salaries, Utilities, Supplies, Maintenance, Transport, Rent, Repairs, Miscellaneous`.
  - `GetCategoriesAsync()` → `SELECT Name FROM ExpenseCategories ORDER BY Name` → `List<string>`.
  - `AddCategoryAsync(string name)`: trim; skip if blank; `IF NOT EXISTS(SELECT 1 FROM ExpenseCategories WHERE Name=?) INSERT ...`.
  - `GetByRangeAsync(DateTime from, DateTime to, string category=null)`:
    `SELECT ID,Expenses_name,Purpose,Description,Date_Time,Amount,Payee,payer FROM Expenses WHERE Date_Time BETWEEN ? AND ?` (+ `AND Purpose=?` when category non-blank) `ORDER BY Date_Time DESC, ID DESC`; map `Amount` via `Common.ExpenseAmount.Parse`.
  - `AddAsync(Expense e)`: `INSERT INTO Expenses (Expenses_name,Purpose,Description,Date_Time,Amount,Payee,payer) VALUES (?,?,?,?,?,?,?)` — Amount = `Common.ExpenseAmount.Store(e.Amount)`; Payee/Payer trimmed to 30 chars.
  - `UpdateAsync(Expense e)`: `UPDATE Expenses SET Expenses_name=?,Purpose=?,Description=?,Date_Time=?,Amount=?,Payee=?,payer=? WHERE ID=?`.
  - `DeleteAsync(int id)`: `DELETE FROM Expenses WHERE ID=?`.
  - `GetTotalExpensesBetweenAsync(from,to)`: `SELECT ISNULL(SUM(TRY_CAST(REPLACE(ISNULL(Amount,'0'),',','') AS DECIMAL(18,2))),0) FROM Expenses WHERE Date_Time BETWEEN ? AND ?` → decimal.

  Use a `private static string Trim30(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 30 ? s.Substring(0,30) : s);` and the `S`/`I` null helpers as in other repos.

- [ ] **Step 3: csproj** — add `<Compile Include="Models\Expense.cs" />` and `<Compile Include="Data\ExpenseRepository.cs" />`.

- [ ] **Step 4: Build** → 0 errors. **Commit**: `feat(expenses): Expense model + repository (CRUD + configurable categories)`.

---

### Task 3: Dashboard income/expense totals

**Files:** Modify `Data/IDashboardRepository.cs`, `Data/DashboardRepository.cs`, `Services/DashboardService.cs`.

- [ ] **Step 1: Interface** — add to `IDashboardRepository`:
```csharp
        Task<decimal> GetTotalIncomeBetweenAsync(System.DateTime from, System.DateTime to);
        Task<decimal> GetTotalExpensesBetweenAsync(System.DateTime from, System.DateTime to);
```

- [ ] **Step 2: Repo** — add to `DashboardRepository` (use a date-param scalar; the existing `ExecuteScalarDecimalAsync` takes no params, so add a small ranged helper):
```csharp
        public async Task<decimal> GetTotalIncomeBetweenAsync(DateTime from, DateTime to) =>
            await ScalarDecimalRangeAsync(
                "SELECT ISNULL(SUM(Amount_paid),0) FROM payment_record WHERE [Date] BETWEEN ? AND ?", from, to);

        public async Task<decimal> GetTotalExpensesBetweenAsync(DateTime from, DateTime to) =>
            await ScalarDecimalRangeAsync(
                "SELECT ISNULL(SUM(TRY_CAST(REPLACE(ISNULL(Amount,'0'),',','') AS DECIMAL(18,2))),0) FROM Expenses WHERE Date_Time BETWEEN ? AND ?", from, to);

        private async Task<decimal> ScalarDecimalRangeAsync(string query, DateTime from, DateTime to)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", from.Date);
                        command.Parameters.AddWithValue("?", to.Date);
                        var result = await command.ExecuteScalarAsync();
                        return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                    }
                }
            }
            catch { return 0m; }
        }
```
(`using System;` already present.)

- [ ] **Step 3: Service** — add to `Services/DashboardService.cs` (and its interface if one exists):
```csharp
        public async System.Threading.Tasks.Task<(decimal Income, decimal Expenses, decimal Fund)>
            GetFinanceSummaryAsync(System.DateTime from, System.DateTime to)
        {
            decimal income = await _dashboardRepository.GetTotalIncomeBetweenAsync(from, to);
            decimal expenses = await _dashboardRepository.GetTotalExpensesBetweenAsync(from, to);
            return (income, expenses, income - expenses);
        }
```
(Confirm the repo field name — match the existing constructor field, e.g. `_dashboardRepository`/`_repository`.)

- [ ] **Step 4: Build** → 0 errors. **Commit**: `feat(expenses): date-range income/expense totals in dashboard repo/service`.

---

### Task 4: `frmExpenses` screen + RBAC + nav

**Files:** Create `frmExpenses.cs`; modify `Services/AuthService.cs`, `frmDashboard.cs` (nav), csproj.

- [ ] **Step 1: RBAC** — in `AuthService._formAccess` after the `["frmTransportPayments"]` line:
```csharp
            ["frmExpenses"]               = new[] { UserRole.Accountant, UserRole.Administrator, UserRole.Director },
```

- [ ] **Step 2: Create `frmExpenses.cs`** (code-built; pattern mirrors `frmTransportPayments`). Full implementation:
```csharp
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmExpenses : Form
    {
        private readonly ExpenseRepository _repo = new ExpenseRepository(AppConfig.ConnectionString);
        private readonly DashboardService _dash =
            new DashboardService(new DashboardRepository(AppConfig.ConnectionString));

        private ComboBox _window, _category;
        private DateTimePicker _customFrom, _customTo, _date;
        private TextBox _name, _amount, _payee, _description;
        private Label _lblIncome, _lblExpenses, _lblFund, _total;
        private DataGridView _grid;
        private Button _btnRecord, _btnDelete;
        private int _editingId;

        public frmExpenses()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmExpenses", this)) return;
            Load += async (s, e) =>
            {
                try { await _repo.EnsureTablesAsync(); await LoadCategoriesAsync(); await RefreshAsync(); }
                catch (Exception ex) { LoggerHelper.LogError("Expenses init", ex); }
            };
        }

        private (DateTime From, DateTime To, string Label) CurrentWindow()
        {
            var w = (FinanceWindow)(_window.SelectedIndex < 0 ? 0 : _window.SelectedIndex);
            return FinancePeriod.Resolve(w, DateTime.Today, _customFrom.Value, _customTo.Value);
        }

        private void BuildUi()
        {
            Text = "Expenses";
            Size = new Size(980, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label { Dock = DockStyle.Top, Height = 44, Text = "  Expenses",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft };

            // Period selector + summary strip
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 96, WrapContents = true, Padding = new Padding(12, 6, 12, 6) };
            top.Controls.Add(new Label { Text = "Window:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            _window = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 10, 0) };
            _window.Items.AddRange(new object[] { "This Term", "This Year", "All Time", "Custom" });
            _window.SelectedIndex = 0;
            _window.SelectedIndexChanged += async (s, e) => { UpdateCustomVisibility(); await RefreshAsync(); };
            _customFrom = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Visible = false, Margin = new Padding(0, 4, 6, 0) };
            _customTo = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Visible = false, Margin = new Padding(0, 4, 10, 0) };
            _customFrom.ValueChanged += async (s, e) => await RefreshAsync();
            _customTo.ValueChanged += async (s, e) => await RefreshAsync();
            top.Controls.Add(_window); top.Controls.Add(_customFrom); top.Controls.Add(_customTo);
            _lblIncome = SummaryCard("Total Income", AppConfig.Colors.SuccessColor);
            _lblExpenses = SummaryCard("Total Expenses", AppConfig.Colors.DangerColor);
            _lblFund = SummaryCard("Total Fund", AppConfig.Colors.PrimaryColor);
            top.Controls.Add(_lblIncome.Parent); top.Controls.Add(_lblExpenses.Parent); top.Controls.Add(_lblFund.Parent);

            // Entry row
            var entry = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 76, WrapContents = true, Padding = new Padding(12, 4, 12, 4), BackColor = Color.White };
            _name = Field(entry, "Name", 160);
            _category = new ComboBox { Width = 150, Margin = new Padding(0, 22, 8, 0) }; // editable: type-new-to-add
            entry.Controls.Add(Captioned("Category", _category));
            _amount = Field(entry, "Amount", 100);
            _date = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 22, 8, 0) };
            entry.Controls.Add(Captioned("Date", _date));
            _payee = Field(entry, "Payee", 130);
            _description = Field(entry, "Description", 180);
            _btnRecord = new Button { Text = "Record", Width = 100, Height = 30, Margin = new Padding(0, 20, 6, 0),
                BackColor = AppConfig.Colors.PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnRecord.Click += async (s, e) => await SaveAsync();
            var clear = new Button { Text = "Clear", Width = 70, Height = 30, Margin = new Padding(0, 20, 6, 0), FlatStyle = FlatStyle.Flat };
            clear.Click += (s, e) => ClearEntry();
            _btnDelete = new Button { Text = "Delete", Width = 80, Height = 30, Margin = new Padding(0, 20, 0, 0), Enabled = false,
                BackColor = Color.FromArgb(190, 18, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnDelete.Click += async (s, e) => await DeleteAsync();
            entry.Controls.Add(_btnRecord); entry.Controls.Add(clear); entry.Controls.Add(_btnDelete);

            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            _grid.SelectionChanged += (s, e) => LoadSelectedRow();

            _total = new Label { Dock = DockStyle.Bottom, Height = 26, TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold), Padding = new Padding(0, 0, 16, 0) };

            Controls.Add(_grid);
            Controls.Add(_total);
            Controls.Add(entry);
            Controls.Add(top);
            Controls.Add(title);
        }

        private void UpdateCustomVisibility()
        {
            bool custom = _window.SelectedIndex == 3;
            _customFrom.Visible = custom; _customTo.Visible = custom;
        }

        private static Label SummaryCard(string caption, Color accent)
        {
            var panel = new Panel { Width = 180, Height = 76, BackColor = Color.White, Margin = new Padding(6, 2, 0, 0), BorderStyle = BorderStyle.FixedSingle };
            panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top, Height = 22, ForeColor = AppConfig.Colors.MutedTextColor, Font = new Font("Segoe UI", 8.5F), Padding = new Padding(8, 4, 0, 0) });
            var val = new Label { Text = "GHS 0.00", Dock = DockStyle.Fill, ForeColor = accent, Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
            panel.Controls.Add(val);
            return val;
        }

        private static TextBox Field(FlowLayoutPanel host, string caption, int width)
        {
            var t = new TextBox { Width = width };
            host.Controls.Add(Captioned(caption, t));
            return t;
        }

        private static Control Captioned(string caption, Control c)
        {
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            p.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = AppConfig.Colors.MutedTextColor, Font = new Font("Segoe UI", 8F) });
            p.Controls.Add(c);
            return p;
        }

        private async Task LoadCategoriesAsync()
        {
            var cats = await _repo.GetCategoriesAsync();
            _category.Items.Clear();
            _category.Items.AddRange(cats.Cast<object>().ToArray());
        }

        private async Task RefreshAsync()
        {
            try
            {
                var w = CurrentWindow();
                var fin = await _dash.GetFinanceSummaryAsync(w.From, w.To);
                _lblIncome.Text = "GHS " + fin.Income.ToString("N2");
                _lblExpenses.Text = "GHS " + fin.Expenses.ToString("N2");
                _lblFund.Text = "GHS " + fin.Fund.ToString("N2");
                _lblFund.ForeColor = fin.Fund < 0 ? AppConfig.Colors.DangerColor : AppConfig.Colors.SuccessColor;

                var list = await _repo.GetByRangeAsync(w.From, w.To, null);
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("Date"); dt.Columns.Add("Name"); dt.Columns.Add("Category");
                dt.Columns.Add("Amount", typeof(decimal)); dt.Columns.Add("Payee"); dt.Columns.Add("Payer"); dt.Columns.Add("Description");
                foreach (var e in list)
                    dt.Rows.Add(e.Id, e.Date.ToString("dd MMM yyyy"), e.Name, e.Category, e.Amount, e.Payee, e.Payer, e.Description);
                _grid.DataSource = dt;
                if (_grid.Columns.Contains("Id")) _grid.Columns["Id"].Visible = false;
                _total.Text = $"Window: {w.Label}   |   {list.Count} expense(s)   |   Total: GHS {list.Sum(x => x.Amount):N2}";
            }
            catch (Exception ex) { LoggerHelper.LogError("Expenses refresh", ex); }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { UIHelper.ShowWarning("Enter an expense name.", "Expenses"); return; }
            string category = (_category.Text ?? "").Trim();
            if (category.Length == 0) { UIHelper.ShowWarning("Enter or pick a category.", "Expenses"); return; }
            if (!decimal.TryParse(_amount.Text, out decimal amount) || amount < 0) { UIHelper.ShowWarning("Amount must be a number ≥ 0.", "Expenses"); return; }

            var e = new Expense
            {
                Id = _editingId, Name = _name.Text.Trim(), Category = category, Description = _description.Text.Trim(),
                Date = _date.Value.Date, Amount = amount, Payee = _payee.Text.Trim(),
                Payer = AuthService.CurrentUser?.DisplayName ?? ""
            };
            try
            {
                await _repo.AddCategoryAsync(category);            // persist new categories
                if (_editingId > 0) await _repo.UpdateAsync(e); else await _repo.AddAsync(e);
                ClearEntry();
                await LoadCategoriesAsync();
                await RefreshAsync();
            }
            catch (Exception ex) { LoggerHelper.LogError("Expenses save", ex); UIHelper.ShowError("Save failed: " + ex.Message, "Expenses"); }
        }

        private void LoadSelectedRow()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.IsNewRow) return;
            var r = _grid.CurrentRow;
            _editingId = Convert.ToInt32(r.Cells["Id"].Value);
            _name.Text = r.Cells["Name"].Value?.ToString();
            _category.Text = r.Cells["Category"].Value?.ToString();
            _amount.Text = Convert.ToDecimal(r.Cells["Amount"].Value).ToString("0.##");
            _payee.Text = r.Cells["Payee"].Value?.ToString();
            _description.Text = r.Cells["Description"].Value?.ToString();
            DateTime dt; if (DateTime.TryParse(r.Cells["Date"].Value?.ToString(), out dt)) _date.Value = dt;
            _btnRecord.Text = "Update"; _btnDelete.Enabled = true;
        }

        private async Task DeleteAsync()
        {
            if (_editingId <= 0) return;
            if (UIHelper.ShowConfirmation("Delete this expense?", "Expenses") != DialogResult.Yes) return;
            try { await _repo.DeleteAsync(_editingId); ClearEntry(); await RefreshAsync(); }
            catch (Exception ex) { LoggerHelper.LogError("Expenses delete", ex); UIHelper.ShowError("Delete failed: " + ex.Message, "Expenses"); }
        }

        private void ClearEntry()
        {
            _editingId = 0;
            _name.Clear(); _category.Text = ""; _amount.Clear(); _payee.Clear(); _description.Clear();
            _date.Value = DateTime.Today;
            _btnRecord.Text = "Record"; _btnDelete.Enabled = false;
        }
    }
}
```
(If `UIHelper.ShowWarning/ShowConfirmation` or `DashboardService`'s constructor differ, match existing call sites — `frmSendNotice` for `UIHelper`, the dashboard for `DashboardService` construction.)

- [ ] **Step 3: csproj** — add `<Compile Include="frmExpenses.cs" />`.

- [ ] **Step 4: Nav** — in `frmDashboard.cs` nav block, in the `finance` group after the Transport Payments `Add(...)` line:
```csharp
            Add(finance, isAcct || isAdmin || isDir, "Expenses", () => OpenForm(new frmExpenses()));
```

- [ ] **Step 5: Build** → 0 errors (fix any `UIHelper`/`DashboardService` ctor mismatch). **Commit**: `feat(expenses): Expenses screen (CRUD + selectable-window income/expense/fund) + RBAC + nav`.

---

### Task 5: Dashboard finance tiles (role-gated)

**Files:** Modify `frmDashboard.cs`.

- [ ] **Step 1: Fields** — near `studentCountLabel` field declarations add:
```csharp
        private Label _incomeLabel, _expensesLabel, _fundLabel;
```

- [ ] **Step 2: Build tiles** — in `BuildContent`, after the 4 `metricGrid.Controls.Add(CreateMetricCard(...))` lines (frmDashboard.cs ~573), add a second role-gated row:
```csharp
            var role = AuthService.CurrentUser.Role;
            if (role == AuthService.UserRole.Accountant || role == AuthService.UserRole.Administrator
                || role == AuthService.UserRole.Director)
            {
                metricGrid.RowCount = 2;
                metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                metricGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                _incomeLabel = new Label(); _expensesLabel = new Label(); _fundLabel = new Label();
                metricGrid.Controls.Add(CreateMetricCard("Total Income",   _incomeLabel,   "This term", AccentGreen, "INCOME",   DashboardIconType.Fees),    0, 1);
                metricGrid.Controls.Add(CreateMetricCard("Total Expenses", _expensesLabel, "This term", AccentRed,   "EXPENSES", DashboardIconType.Balance), 1, 1);
                metricGrid.Controls.Add(CreateMetricCard("Total Fund",     _fundLabel,     "This term", AccentGold,  "FUND",     DashboardIconType.Fees),    2, 1);
                foreach (var l in new[] { _incomeLabel, _expensesLabel, _fundLabel })
                { l.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold); l.AutoEllipsis = true; }
            }
```
(`AccentGreen/AccentRed/AccentGold` and `DashboardIconType` already exist and are used by the row-0 cards.)

- [ ] **Step 3: Populate** — in `LoadDashboardStatisticsAsync`, after `topClassLabel.Text = metrics.TopClass;` add:
```csharp
                if (_fundLabel != null)
                {
                    var term = AppConfig.Leave.CurrentTerm;
                    var fin = await _dashboardService.GetFinanceSummaryAsync(term.Start, term.End);
                    _incomeLabel.Text = FormatCurrency(fin.Income);
                    _expensesLabel.Text = FormatCurrency(fin.Expenses);
                    _fundLabel.Text = FormatCurrency(fin.Fund);
                    _fundLabel.ForeColor = fin.Fund < 0 ? AccentRed : AccentGreen;
                }
```
(`FormatCurrency`, `_dashboardService`, `AppConfig.Leave.CurrentTerm` all already exist in this file.)

- [ ] **Step 4: Build** → 0 errors. **Commit**: `feat(expenses): role-gated Total Income/Expenses/Fund tiles on dashboard`.

---

### Task 6: Verify

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; `dotnet run --project "Tests/Kingdom.Tests/Kingdom.Tests.csproj"` → all pass (incl. ExpenseAmount + FinancePeriod).
- [ ] Offline render: `frmExpenses` constructs (summary strip + selector + grid); `frmDashboard` for an Accountant shows the 3 finance tiles, for a Teacher shows none.
- [ ] **User smoke test:** record an expense (category typed-new) → appears in list, total + dashboard expense chart + tiles update; switch window to This Year / All Time → totals change; Custom shows the two date pickers; Director/Accountant/Admin see the tiles, Teacher/Headmaster do not.

## Self-review notes
- **Spec coverage:** helpers+tests (T1) ✓; Expense model + repo with configurable categories + comma-free Amount (T2) ✓; date-range income/expense totals (T3) ✓; frmExpenses with window selector + summary + CRUD, RBAC, nav (T4) ✓; role-gated dashboard tiles current-term (T5) ✓.
- **Placeholders:** repo SQL described method-by-method with exact columns; only `UIHelper`/`DashboardService`-ctor matches flagged as "match existing call site".
- **Type consistency:** `ExpenseAmount.Parse/Store`, `FinancePeriod.Resolve(FinanceWindow,DateTime,DateTime?,DateTime?)→(From,To,Label)`, `GetFinanceSummaryAsync(from,to)→(Income,Expenses,Fund)`, `ExpenseRepository.GetByRangeAsync/Add/Update/Delete/GetCategoriesAsync/AddCategoryAsync`, RBAC literal `"frmExpenses"`.
- **Risk:** OleDb positional params (ranged scalar adds from,to in order); legacy varchar Amount handled by ExpenseAmount on read and the SQL TRY_CAST on aggregate.
