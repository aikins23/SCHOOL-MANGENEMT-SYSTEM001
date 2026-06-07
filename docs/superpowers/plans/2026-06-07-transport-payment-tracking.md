# Transport Payment Tracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A dedicated transport-payment ledger with a cashier screen (record + history + arrears) and an attendance-driven overdue SMS reminder, separate from the tuition ledger.

**Architecture:** A pure `TransportPeriod` helper maps a route's term to the current period. `TransportRepository` gains two idempotent tables (`TransportPayment`, `TransportReminderLog`) and methods for recording/paid/history/arrears/reminder-candidates. A new code-built `frmTransportPayments` (Record + Arrears tabs) records payments and shows balances. The dashboard's existing reminder pass gains a sibling `CheckTransportRemindersAsync` that SMSes guardians of unpaid bus students who have ≥2 PRESENT days this period, once per period.

**Tech Stack:** C#/.NET 4.7.2, WinForms, OleDb/MSOLEDBSQL. Explicit-include csproj.

**Spec:** `docs/superpowers/specs/2026-06-07-transport-payment-tracking-design.md`

---

## Conventions

- **Gates:** `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; `dotnet run --project Tests/Kingdom.Tests` → all pass; offline render of `frmTransportPayments`.
- OleDb uses **positional `?`** params (no named params). MONEY↔decimal; truncate DateTime to whole seconds.
- Existing & reused: `Common.AppConfig.ConnectionString`, `Common.StudentId`, `Common.SessionUi.AttachSignOut`, `Services.AuthService`, `Services.StudentService`/`StudentRepository.GetStudentAsync`, `Services.SmsService.SendSmsAsync`, `Services.SmsSenderIds.FeeReminder`, `Services.LoggerHelper`. Attendance table `Attendance` (`ReferenceID` varchar, `ReferenceType='STUDENT'`, `[Status]='PRESENT'`, `[Date]`). Students table `Students` (`StudentID` int, `FirstName`, `LastName`, `EmergencyContact`).
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: `TransportPeriod` helper (+ unit tests)

**Files:** Create `Common/TransportPeriod.cs`; modify `kingdom_Preparatory_School_Management_System.csproj`; modify `Tests/Kingdom.Tests/Program.cs`.

- [ ] **Step 1: Create the helper**

`Common/TransportPeriod.cs`:

```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Maps a route's payment term to the current billing period. Monthly = calendar month;
    /// Weekly = a 2-week (fortnight) block anchored to a fixed Monday; Daily = a single day.
    /// Pure and deterministic (no DB / no clock) so it is unit-testable.
    /// </summary>
    public static class TransportPeriod
    {
        private static readonly DateTime FortnightEpoch = new DateTime(2024, 1, 1); // a Monday

        private static bool Is(string term, string name) =>
            string.Equals((term ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase);

        /// <summary>Monthly and Weekly routes get auto-reminders; Daily routes do not.</summary>
        public static bool SupportsReminders(string term) => Is(term, "Monthly") || Is(term, "Weekly");

        /// <summary>(Key, Start, End) for the period containing <paramref name="today"/>.</summary>
        public static (string Key, DateTime Start, DateTime End) Current(string term, DateTime today)
        {
            today = today.Date;

            if (Is(term, "Monthly"))
            {
                var start = new DateTime(today.Year, today.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                return (today.ToString("yyyy-MM"), start, end);
            }

            if (Is(term, "Weekly"))
            {
                int days = (int)(today - FortnightEpoch).TotalDays;
                if (days < 0) days = 0;
                var start = FortnightEpoch.AddDays((days / 14) * 14);
                var end = start.AddDays(13);
                return (start.ToString("yyyy-MM-dd") + "/FN", start, end);
            }

            // Daily (and any unknown term) -> a single day.
            return (today.ToString("yyyy-MM-dd"), today, today);
        }
    }
}
```

- [ ] **Step 2: Register in csproj**

Next to `<Compile Include="Common\SessionUi.cs" />` add:

```xml
    <Compile Include="Common\TransportPeriod.cs" />
```

- [ ] **Step 3: Add unit tests**

In `Tests/Kingdom.Tests/Program.cs`, register after the `StudentId parses...` line:

```csharp
                new TestCase("TransportPeriod maps route terms to current period", TransportPeriod_MapsTermsToPeriod),
```

Add these methods after `StudentId_ParsesDisplayIds`:

```csharp
        private static void TransportPeriod_MapsTermsToPeriod()
        {
            var m = TransportPeriod.Current("Monthly", new DateTime(2026, 6, 7));
            AssertEx.Equal("2026-06", m.Key);
            AssertEx.Equal(new DateTime(2026, 6, 1), m.Start);
            AssertEx.Equal(new DateTime(2026, 6, 30), m.End);

            var d = TransportPeriod.Current("Daily", new DateTime(2026, 6, 7));
            AssertEx.Equal("2026-06-07", d.Key);
            AssertEx.Equal(new DateTime(2026, 6, 7), d.Start);
            AssertEx.Equal(new DateTime(2026, 6, 7), d.End);

            // Weekly = fixed 2-week block (length 14, deterministic, end = start+13).
            var w = TransportPeriod.Current("Weekly", new DateTime(2026, 6, 7));
            AssertEx.Equal(14, (int)(w.End - w.Start).TotalDays + 1);
            AssertEx.True(w.Start <= new DateTime(2026, 6, 7) && new DateTime(2026, 6, 7) <= w.End,
                "today must fall within its fortnight block");

            AssertEx.True(TransportPeriod.SupportsReminders("Monthly"));
            AssertEx.True(TransportPeriod.SupportsReminders("Weekly"));
            AssertEx.False(TransportPeriod.SupportsReminders("Daily"));
        }
```

- [ ] **Step 4: Build + test**

`dotnet build -clp:ErrorsOnly -nologo` → 0 errors. `dotnet run --project "Tests/Kingdom.Tests/Kingdom.Tests.csproj"` → the new test passes.

- [ ] **Step 5: Commit**

```bash
git add Common/TransportPeriod.cs kingdom_Preparatory_School_Management_System.csproj Tests/Kingdom.Tests/Program.cs docs/superpowers/plans/2026-06-07-transport-payment-tracking.md docs/superpowers/specs/2026-06-07-transport-payment-tracking-design.md
git commit -m "feat(transport): add TransportPeriod helper + tests"
```

---

### Task 2: `TransportArrear` model + repository ledger

**Files:** Create `Models/TransportArrear.cs`; modify `Data/ITransportRepository.cs`, `Data/TransportRepository.cs`, `kingdom_Preparatory_School_Management_System.csproj`.

- [ ] **Step 1: Model**

`Models/TransportArrear.cs`:

```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A bus student's transport standing for the current period.</summary>
    public class TransportArrear
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = "";
        public string RouteName { get; set; } = "";
        public string Term { get; set; } = "";
        public string Period { get; set; } = "";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Fee { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance => Math.Max(0m, Fee - Paid);
        public int PresentDays { get; set; }
        public string GuardianPhone { get; set; } = "";
    }
}
```

Register in csproj next to `<Compile Include="Models\BusRoute.cs" />` (or any Models entry):

```xml
    <Compile Include="Models\TransportArrear.cs" />
```

- [ ] **Step 2: Extend the interface**

In `Data/ITransportRepository.cs`, add inside the interface:

```csharp
        System.Threading.Tasks.Task<bool> AddTransportPaymentAsync(int studentId, int routeId, string periodKey, System.DateTime periodStart, System.DateTime periodEnd, decimal amountPaid, System.DateTime date, string cashier, string notes);
        System.Threading.Tasks.Task<decimal> GetPaidForPeriodAsync(int studentId, string periodKey);
        System.Threading.Tasks.Task<System.Data.DataTable> GetStudentTransportHistoryAsync(int studentId);
        System.Threading.Tasks.Task<System.Collections.Generic.List<Models.TransportArrear>> GetArrearsAsync(System.DateTime asOf, bool includeDaily);
        System.Threading.Tasks.Task<System.Collections.Generic.List<Models.TransportArrear>> GetReminderCandidatesAsync(System.DateTime asOf);
        System.Threading.Tasks.Task LogReminderSentAsync(int studentId, string periodKey);
```

- [ ] **Step 3: New tables in `EnsureTablesAsync`**

In `Data/TransportRepository.cs`, inside `EnsureTablesAsync()`, after the `StudentTransport` create block (before the connection `using` closes), add:

```csharp
                const string tPay = @"IF OBJECT_ID(N'TransportPayment', N'U') IS NULL
                    CREATE TABLE TransportPayment (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        StudentID INT NOT NULL, RouteId INT NOT NULL,
                        Period NVARCHAR(20) NOT NULL, PeriodStart DATETIME NOT NULL, PeriodEnd DATETIME NOT NULL,
                        AmountPaid MONEY NOT NULL DEFAULT (0), PaymentDate DATETIME NOT NULL,
                        Cashier NVARCHAR(120), Notes NVARCHAR(255));";
                using (var cmd = new OleDbCommand(tPay, c)) await cmd.ExecuteNonQueryAsync();

                const string tRem = @"IF OBJECT_ID(N'TransportReminderLog', N'U') IS NULL
                    CREATE TABLE TransportReminderLog (
                        StudentID INT NOT NULL, Period NVARCHAR(20) NOT NULL, SentDate DATETIME NOT NULL,
                        CONSTRAINT PK_TransportReminderLog PRIMARY KEY (StudentID, Period));";
                using (var cmd = new OleDbCommand(tRem, c)) await cmd.ExecuteNonQueryAsync();
```

- [ ] **Step 4: Ledger methods**

Add these methods to `TransportRepository` (before the `TruncateSeconds` helper). They need `using System.Linq;`? No. They use `Common.TransportPeriod`; reference it fully-qualified.

```csharp
        public async Task<bool> AddTransportPaymentAsync(int studentId, int routeId, string periodKey,
            DateTime periodStart, DateTime periodEnd, decimal amountPaid, DateTime date, string cashier, string notes)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO TransportPayment
                    (StudentID,RouteId,Period,PeriodStart,PeriodEnd,AmountPaid,PaymentDate,Cashier,Notes)
                    VALUES (?,?,?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", routeId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(periodStart));
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(periodEnd));
                    cmd.Parameters.AddWithValue("?", amountPaid);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(date));
                    cmd.Parameters.AddWithValue("?", cashier ?? "");
                    cmd.Parameters.AddWithValue("?", notes ?? "");
                    return (await cmd.ExecuteNonQueryAsync()) > 0;
                }
            }
        }

        public async Task<decimal> GetPaidForPeriodAsync(int studentId, string periodKey)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT ISNULL(SUM(AmountPaid),0) FROM TransportPayment WHERE StudentID=? AND Period=?", c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    var o = await cmd.ExecuteScalarAsync();
                    return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                }
            }
        }

        public async Task<DataTable> GetStudentTransportHistoryAsync(int studentId)
        {
            var dt = new DataTable();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT tp.PaymentDate AS [Date], r.RouteName AS [Route], tp.Period AS [Period],
                    tp.AmountPaid AS [Amount Paid], tp.Cashier AS [Cashier], tp.Notes AS [Notes]
                    FROM TransportPayment tp LEFT JOIN BusRoutes r ON tp.RouteId=r.RouteId
                    WHERE tp.StudentID=? ORDER BY tp.PaymentDate DESC, tp.Id DESC";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    using (var rd = await cmd.ExecuteReaderAsync()) dt.Load(rd);
                }
            }
            return dt;
        }

        // Loads every bus student's route once, then computes period/paid/present-days per row in C#
        // (avoids fragile term-dependent SQL). Daily routes are included only when includeDaily is true.
        private async Task<List<TransportArrear>> BuildArrearsAsync(DateTime asOf, bool includeDaily)
        {
            var rows = new List<TransportArrear>();
            var seed = new List<(int StudentId, int RouteId, string Name, string Phone, string Route, string Term, decimal Fee)>();

            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT st.StudentID, st.RouteId,
                    (s.FirstName + ' ' + s.LastName) AS StudentName, s.EmergencyContact AS Phone,
                    r.RouteName, r.PaymentTerm, r.Fee
                    FROM StudentTransport st
                    INNER JOIN BusRoutes r ON st.RouteId = r.RouteId
                    INNER JOIN Students s ON s.StudentID = st.StudentID";
                using (var cmd = new OleDbCommand(sql, c))
                using (var rd = await cmd.ExecuteReaderAsync())
                {
                    while (await rd.ReadAsync())
                        seed.Add((I(rd["StudentID"]), I(rd["RouteId"]), S(rd["StudentName"]), S(rd["Phone"]),
                                  S(rd["RouteName"]), S(rd["PaymentTerm"]),
                                  rd["Fee"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["Fee"])));
                }

                foreach (var x in seed)
                {
                    bool daily = !Common.TransportPeriod.SupportsReminders(x.Term)
                                 && !string.Equals(x.Term, "Weekly", StringComparison.OrdinalIgnoreCase);
                    if (daily && !includeDaily) continue;

                    var p = Common.TransportPeriod.Current(x.Term, asOf);

                    decimal paid;
                    using (var cmd = new OleDbCommand(
                        "SELECT ISNULL(SUM(AmountPaid),0) FROM TransportPayment WHERE StudentID=? AND Period=?", c))
                    {
                        cmd.Parameters.AddWithValue("?", x.StudentId);
                        cmd.Parameters.AddWithValue("?", p.Key);
                        var o = await cmd.ExecuteScalarAsync();
                        paid = o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
                    }

                    int present;
                    using (var cmd = new OleDbCommand(
                        @"SELECT COUNT(*) FROM Attendance WHERE ReferenceID=? AND ReferenceType='STUDENT'
                          AND [Status]='PRESENT' AND [Date] BETWEEN ? AND ?", c))
                    {
                        cmd.Parameters.AddWithValue("?", x.StudentId.ToString());
                        cmd.Parameters.AddWithValue("?", p.Start.Date);
                        cmd.Parameters.AddWithValue("?", asOf.Date);
                        present = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    rows.Add(new TransportArrear
                    {
                        StudentID = x.StudentId, StudentName = x.Name, GuardianPhone = x.Phone,
                        RouteName = x.Route, Term = x.Term, Fee = x.Fee, Paid = paid, PresentDays = present,
                        Period = p.Key, PeriodStart = p.Start, PeriodEnd = p.End
                    });
                }
            }
            return rows;
        }

        public async Task<List<TransportArrear>> GetArrearsAsync(DateTime asOf, bool includeDaily)
            => await BuildArrearsAsync(asOf, includeDaily);

        public async Task<List<TransportArrear>> GetReminderCandidatesAsync(DateTime asOf)
        {
            var candidates = new List<TransportArrear>();
            foreach (var a in await BuildArrearsAsync(asOf, includeDaily: false))
            {
                if (a.Balance <= 0m || a.PresentDays < 2) continue;
                using (var c = new OleDbConnection(_connectionString))
                {
                    await c.OpenAsync();
                    using (var cmd = new OleDbCommand(
                        "SELECT COUNT(*) FROM TransportReminderLog WHERE StudentID=? AND Period=?", c))
                    {
                        cmd.Parameters.AddWithValue("?", a.StudentID);
                        cmd.Parameters.AddWithValue("?", a.Period);
                        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0) continue;
                    }
                }
                candidates.Add(a);
            }
            return candidates;
        }

        public async Task LogReminderSentAsync(int studentId, string periodKey)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"IF NOT EXISTS (SELECT 1 FROM TransportReminderLog WHERE StudentID=? AND Period=?)
                    INSERT INTO TransportReminderLog (StudentID,Period,SentDate) VALUES (?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    cmd.Parameters.AddWithValue("?", studentId);
                    cmd.Parameters.AddWithValue("?", periodKey ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
```

(Note: the `daily` check — `SupportsReminders` is true for Monthly/Weekly, so "daily" = neither Monthly nor Weekly. The extra `Weekly` guard is redundant but explicit; simplify to `bool daily = !Common.TransportPeriod.SupportsReminders(x.Term);` since SupportsReminders already excludes only Daily/unknown.)

Use the simpler form:

```csharp
                    bool daily = !Common.TransportPeriod.SupportsReminders(x.Term);
                    if (daily && !includeDaily) continue;
```

- [ ] **Step 5: Build**

`dotnet build -clp:ErrorsOnly -nologo` → 0 errors. (If `TransportArrear` unresolved, the csproj entry/Models namespace is missing.)

- [ ] **Step 6: Commit**

```bash
git add Models/TransportArrear.cs Data/ITransportRepository.cs Data/TransportRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(transport): ledger tables + repository (record/paid/history/arrears/reminders)"
```

---

### Task 3: SMS reminder message

**Files:** Modify `Services/SmsService.cs`.

- [ ] **Step 1: Add the transport reminder sender**

After `SendFeeReminderAsync` (≈ line 154) add:

```csharp
        public static Task<(bool Success, string Message)> SendTransportReminderAsync(
            string recipient, string studentName, string routeName, string period, decimal balance)
        {
            string message =
$@"Dear Guardian,

{studentName} uses the school bus ({routeName}) and has an outstanding transport fee of GHS {balance:N2} for {period}.

Please settle it at your earliest convenience.

- Accounts";
            return SendSmsAsync(recipient, message, SmsSenderIds.FeeReminder);
        }
```

- [ ] **Step 2: Build + commit**

`dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

```bash
git add Services/SmsService.cs
git commit -m "feat(transport): SMS transport overdue reminder message"
```

---

### Task 4: `frmTransportPayments` screen + RBAC

**Files:** Create `frmTransportPayments.cs`; modify `Services/AuthService.cs`, `kingdom_Preparatory_School_Management_System.csproj`.

- [ ] **Step 1: RBAC entry**

In `Services/AuthService.cs` `_formAccess`, after the `["frmTransport"]` line add:

```csharp
            ["frmTransportPayments"]      = new[] { UserRole.Accountant, UserRole.Administrator, UserRole.Headmaster },
```

- [ ] **Step 2: Create the form**

`frmTransportPayments.cs`:

```csharp
using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public class frmTransportPayments : Form
    {
        private readonly TransportRepository _transport = new TransportRepository(AppConfig.ConnectionString);
        private readonly StudentService _students =
            new StudentService(new StudentRepository(AppConfig.ConnectionString), new FeeRepository(AppConfig.ConnectionString));

        private TextBox _txtId;
        private Label _lblName, _lblRoute, _lblTerm, _lblPeriod, _lblFee, _lblPaid, _lblBalance;
        private TextBox _txtAmount;
        private Button _btnRecord;
        private DataGridView _history, _arrears;
        private CheckBox _unpaidOnly;

        private int _studentId;
        private BusRoute _route;
        private (string Key, DateTime Start, DateTime End) _period;
        private decimal _paid;

        public frmTransportPayments()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
            if (!AuthService.RequireAccess("frmTransportPayments", this)) return;
            Load += async (s, e) => { try { await _transport.EnsureTablesAsync(); await RefreshArrearsAsync(); } catch (Exception ex) { LoggerHelper.LogError("Transport payments init", ex); } };
        }

        private void BuildUi()
        {
            Text = "Transport Payments";
            Size = new Size(940, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 46, Text = "  Transport Payments",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildRecordTab());
            tabs.TabPages.Add(BuildArrearsTab());

            Controls.Add(tabs);
            Controls.Add(title);
        }

        private TabPage BuildRecordTab()
        {
            var tab = new TabPage("Record Payment") { BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12) };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            top.Controls.Add(new Label { Text = "Student ID:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _txtId = new TextBox { Width = 160, Margin = new Padding(0, 6, 8, 0) };
            var find = new Button { Text = "Find", Width = 90, Height = 28, Margin = new Padding(0, 5, 0, 0) };
            find.Click += async (s, e) => await LookupAsync();
            top.Controls.Add(_txtId);
            top.Controls.Add(find);

            var info = new TableLayoutPanel { Dock = DockStyle.Top, Height = 200, ColumnCount = 2, BackColor = Color.White, Padding = new Padding(12) };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _lblName = AddInfoRow(info, "Name");
            _lblRoute = AddInfoRow(info, "Route");
            _lblTerm = AddInfoRow(info, "Term");
            _lblPeriod = AddInfoRow(info, "Current period");
            _lblFee = AddInfoRow(info, "Fee");
            _lblPaid = AddInfoRow(info, "Paid this period");
            _lblBalance = AddInfoRow(info, "Balance");

            var pay = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
            pay.Controls.Add(new Label { Text = "Amount:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _txtAmount = new TextBox { Width = 140, Margin = new Padding(0, 6, 8, 0) };
            _btnRecord = new Button { Text = "Record Payment", Width = 150, Height = 30, Enabled = false, BackColor = AppConfig.Colors.PrimaryColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _btnRecord.Click += async (s, e) => await RecordAsync();
            pay.Controls.Add(_txtAmount);
            pay.Controls.Add(_btnRecord);

            _history = MakeGrid();

            tab.Controls.Add(_history);
            tab.Controls.Add(pay);
            tab.Controls.Add(info);
            tab.Controls.Add(top);
            return tab;
        }

        private TabPage BuildArrearsTab()
        {
            var tab = new TabPage("Arrears") { BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12) };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _unpaidOnly = new CheckBox { Text = "Unpaid only", Checked = true, AutoSize = true, Margin = new Padding(0, 9, 12, 0) };
            _unpaidOnly.CheckedChanged += async (s, e) => await RefreshArrearsAsync();
            var refresh = new Button { Text = "Refresh", Width = 90, Height = 28, Margin = new Padding(0, 5, 0, 0) };
            refresh.Click += async (s, e) => await RefreshArrearsAsync();
            bar.Controls.Add(_unpaidOnly);
            bar.Controls.Add(refresh);

            _arrears = MakeGrid();
            tab.Controls.Add(_arrears);
            tab.Controls.Add(bar);
            return tab;
        }

        private static DataGridView MakeGrid() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
        };

        private static Label AddInfoRow(TableLayoutPanel t, string caption)
        {
            int row = t.RowCount;
            t.RowCount = row + 1;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            t.Controls.Add(new Label { Text = caption + ":", AutoSize = true, ForeColor = AppConfig.Colors.MutedTextColor, Margin = new Padding(0, 4, 0, 0) }, 0, row);
            var val = new Label { Text = "-", AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Margin = new Padding(0, 4, 0, 0) };
            t.Controls.Add(val, 1, row);
            return val;
        }

        private async Task LookupAsync()
        {
            try
            {
                string idText = StudentId.Parse(_txtId.Text);
                if (!int.TryParse(idText, out _studentId) || _studentId <= 0)
                { UIHelper.ShowError("Enter a valid student ID.", "Transport"); return; }

                var student = await _students.GetStudentAsync(idText);
                _route = await _transport.GetStudentRouteAsync(_studentId);
                if (student == null) { UIHelper.ShowError("Student not found.", "Transport"); ResetInfo(); return; }
                if (_route == null) { _lblName.Text = (student.FirstName + " " + student.LastName).Trim(); UIHelper.ShowInfo("This student is not assigned to a bus route.", "Transport"); ResetInfo(keepName: true); return; }

                _period = TransportPeriod.Current(_route.PaymentTerm, DateTime.Today);
                _paid = await _transport.GetPaidForPeriodAsync(_studentId, _period.Key);
                decimal balance = Math.Max(0m, _route.Fee - _paid);

                _lblName.Text = (student.FirstName + " " + student.LastName).Trim();
                _lblRoute.Text = _route.RouteName;
                _lblTerm.Text = _route.PaymentTerm;
                _lblPeriod.Text = _period.Key + "  (" + _period.Start.ToString("dd MMM") + " - " + _period.End.ToString("dd MMM") + ")";
                _lblFee.Text = "GHS " + _route.Fee.ToString("N2");
                _lblPaid.Text = "GHS " + _paid.ToString("N2");
                _lblBalance.Text = "GHS " + balance.ToString("N2");
                _btnRecord.Enabled = true;

                _history.DataSource = await _transport.GetStudentTransportHistoryAsync(_studentId);
                StudentId.AttachGridFormatting(_history); // no-op (no ID column) but consistent
            }
            catch (Exception ex) { LoggerHelper.LogError("Transport lookup", ex); UIHelper.ShowError("Lookup failed: " + ex.Message, "Transport"); }
        }

        private async Task RecordAsync()
        {
            try
            {
                if (_route == null || _studentId <= 0) return;
                if (!decimal.TryParse(_txtAmount.Text, out decimal amount) || amount <= 0m)
                { UIHelper.ShowError("Enter a payment amount greater than zero.", "Transport"); return; }

                string cashier = AuthService.CurrentUser.DisplayName;
                bool ok = await _transport.AddTransportPaymentAsync(_studentId, _route.RouteId, _period.Key,
                    _period.Start, _period.End, amount, DateTime.Now, cashier, "");
                if (!ok) { UIHelper.ShowError("Could not save the transport payment.", "Transport"); return; }

                _txtAmount.Clear();
                UIHelper.ShowInfo("Transport payment recorded.", "Transport");
                await LookupAsync();          // refresh balance + history
                await RefreshArrearsAsync();
            }
            catch (Exception ex) { LoggerHelper.LogError("Transport record", ex); UIHelper.ShowError("Save failed: " + ex.Message, "Transport"); }
        }

        private async Task RefreshArrearsAsync()
        {
            try
            {
                var list = await _transport.GetArrearsAsync(DateTime.Today, includeDaily: true);
                var dt = new DataTable();
                dt.Columns.Add("Student"); dt.Columns.Add("Route"); dt.Columns.Add("Term");
                dt.Columns.Add("Period"); dt.Columns.Add("Fee", typeof(decimal));
                dt.Columns.Add("Paid", typeof(decimal)); dt.Columns.Add("Balance", typeof(decimal));
                dt.Columns.Add("Present days", typeof(int));
                foreach (var a in list)
                {
                    if (_unpaidOnly != null && _unpaidOnly.Checked && a.Balance <= 0m) continue;
                    dt.Rows.Add(StudentId.Display(a.StudentID) + " - " + a.StudentName, a.RouteName, a.Term,
                        a.Period, a.Fee, a.Paid, a.Balance, a.PresentDays);
                }
                if (_arrears != null) _arrears.DataSource = dt;
            }
            catch (Exception ex) { LoggerHelper.LogError("Transport arrears", ex); }
        }

        private void ResetInfo(bool keepName = false)
        {
            if (!keepName) _lblName.Text = "-";
            _lblRoute.Text = _lblTerm.Text = _lblPeriod.Text = _lblFee.Text = _lblPaid.Text = _lblBalance.Text = "-";
            _btnRecord.Enabled = false;
            _history.DataSource = null;
        }
    }
}
```

(If `UIHelper.ShowInfo`/`ShowError` signatures differ, match the project's existing helper — they are used elsewhere as `UIHelper.ShowError(string, string)`. If `StudentId.AttachGridFormatting(grid)` requires column names, drop that line — it is cosmetic.)

- [ ] **Step 3: Register in csproj**

Next to the other top-level form entries (e.g. `<Compile Include="frmTransport.cs" />`) add:

```xml
    <Compile Include="frmTransportPayments.cs" />
```

- [ ] **Step 4: Build**

`dotnet build -clp:ErrorsOnly -nologo` → 0 errors. Fix any `UIHelper`/`StudentService` signature mismatches by matching existing call sites (`frmFessPayment` for `UIHelper`, `StudentService.GetStudentAsync`).

- [ ] **Step 5: Commit**

```bash
git add frmTransportPayments.cs Services/AuthService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(transport): Transport Payments screen (record + history + arrears) + RBAC"
```

---

### Task 5: Dashboard nav + overdue reminder pass

**Files:** Modify `frmDashboard.cs`.

- [ ] **Step 1: Nav entry under Finance**

In the nav-build block, in the `finance` group (after the Payment History `Add(...)` line), add:

```csharp
            Add(finance, isAcct || isAdmin || isHead, "Transport Payments", () => OpenForm(new frmTransportPayments()));
```

- [ ] **Step 2: Reminder pass**

Add a field near `_feeReminderTimer`:

```csharp
        private readonly Data.TransportRepository _transportRepo = new Data.TransportRepository(Common.AppConfig.ConnectionString);
```

Hook it onto the existing reminder timer + load. In the constructor where `_feeReminderTimer.Tick += ...` is wired, add a second handler call; and in `frmDashboard_Load` after `await CheckFeeRemindersAsync();` add `await CheckTransportRemindersAsync();`. Concretely:

- In the timer Tick lambda (line ~77) change to also run transport:
```csharp
    _feeReminderTimer.Tick += async (s, e) => { await CheckFeeRemindersAsync(); await CheckTransportRemindersAsync(); };
```
- In `frmDashboard_Load` (after the existing `await CheckFeeRemindersAsync();`):
```csharp
                await CheckTransportRemindersAsync();
```

Add the method next to `CheckFeeRemindersAsync`:

```csharp
        private async System.Threading.Tasks.Task CheckTransportRemindersAsync()
        {
            try
            {
                var role = AuthService.CurrentUser.Role;
                if (role != AuthService.UserRole.Accountant && role != AuthService.UserRole.Administrator
                    && role != AuthService.UserRole.Headmaster) return;

                await _transportRepo.EnsureTablesAsync();
                var candidates = await _transportRepo.GetReminderCandidatesAsync(DateTime.Today);
                int sent = 0;
                foreach (var a in candidates)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(a.GuardianPhone))
                        {
                            _ = SmsService.SendTransportReminderAsync(a.GuardianPhone, a.StudentName, a.RouteName, a.Period, a.Balance);
                            sent++;
                        }
                        await _transportRepo.LogReminderSentAsync(a.StudentID, a.Period);
                    }
                    catch (Exception exInner) { LoggerHelper.LogWarning("Transport reminder (row): " + exInner.Message); }
                }
                if (sent > 0) LoggerHelper.LogInfo($"Transport overdue reminders sent to {sent} guardian(s)");
            }
            catch (Exception ex) { LoggerHelper.LogError("Failed to send transport reminders", ex); }
        }
```

(`AuthService`, `SmsService`, `LoggerHelper`, `DateTime` are already in scope in `frmDashboard.cs`.)

- [ ] **Step 3: Build**

`dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 4: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(transport): Finance nav entry + attendance-driven overdue reminder pass"
```

---

### Task 6: Verify + finish

- [ ] **Step 1: Full build + test suite**

`dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
`dotnet run --project "Tests/Kingdom.Tests/Kingdom.Tests.csproj"` → all pass (incl. new TransportPeriod test).

- [ ] **Step 2: Offline render**

Render `frmTransportPayments` (role Administrator) via the harness: confirm it constructs, has the `_sharedSignOut` chip, and the Record/Arrears tabs exist. (DB up → Load runs; if down, the form still constructs.)

- [ ] **Step 3: User smoke test (manual)**

Assign a student to a Monthly route → open Transport Payments → Find the student → record a partial payment → balance drops, history shows the row → Arrears tab lists the remaining balance. Mark 2 PRESENT attendance days for that student in the period and leave a balance → on next dashboard load the guardian gets one SMS and `TransportReminderLog` has a row (no resend).

## Final verification

- [ ] Build 0 errors; suite green; render shows the screen.
- [ ] No change to the tuition ledger or other screens' RBAC.

## Self-review notes

- **Spec coverage:** period helper (T1) ✓; ledger tables + repo record/paid/history/arrears/candidates/log (T2) ✓; SMS message (T3) ✓; dedicated screen Record+History+Arrears, RBAC (T4) ✓; Finance nav + attendance-driven once-per-period reminder, Daily excluded (T5) ✓; tuition ledger untouched ✓.
- **Placeholders:** complete code given; the only judgment notes are `UIHelper`/`AttachGridFormatting` signature matches, flagged with the fallback.
- **Type consistency:** `TransportPeriod.Current → (string,DateTime,DateTime)`, `TransportArrear.Balance` derived, repo methods match the interface additions, `SmsService.SendTransportReminderAsync(string,string,string,string,decimal)`, `AuthService.CurrentUser.DisplayName`.
- **Risk:** OleDb positional params — every `?` is added in order. Arrears computes periods in C# to avoid term-dependent SQL. Reminder de-dup via `TransportReminderLog` PK.
