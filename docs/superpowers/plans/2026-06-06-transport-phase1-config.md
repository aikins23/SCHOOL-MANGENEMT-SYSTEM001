# Transport Phase 1 (Bus & Route Configuration) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A dedicated Transport settings screen to maintain buses and routes (each route with a fee and Daily/Weekly/Monthly payment term).

**Architecture:** Two tables (`Buses`, `BusRoutes`) + one `TransportRepository` + one tabbed `frmTransport` (Buses / Routes). Same shape as the Library module. RBAC + dashboard nav. No fee-table changes.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-transport-phase1-config-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) reflection probe for repo logic (needs LocalDB — defer if down); (3) render harness for the form.
- **Explicit-include csproj:** every new `.cs` needs a `<Compile Include="..." />` entry; code-only forms self-closing (anchor on `frmLibrary.cs`).
- **`datetime` caveat:** truncate to whole seconds (`TruncateSeconds`). `MONEY` ↔ `decimal`.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Create** `Models/Bus.cs`, `Models/BusRoute.cs`
- **Create** `Data/ITransportRepository.cs`, `Data/TransportRepository.cs`
- **Create** `frmTransport.cs`
- **Modify** `Services/AuthService.cs`, `frmDashboard.cs`, `kingdom_Preparatory_School_Management_System.csproj`

---

### Task 1: Models

**Files:** Create `Models/Bus.cs`, `Models/BusRoute.cs`; Modify csproj.

- [ ] **Step 1: Create `Models/Bus.cs`**

```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A school bus/vehicle.</summary>
    public class Bus
    {
        public int BusId { get; set; }
        public string Label { get; set; } = "";
        public string RegNumber { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string DriverContact { get; set; } = "";
        public int Capacity { get; set; }
        public string Notes { get; set; } = "";
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public override string ToString() => Label; // ComboBox display
    }
}
```

- [ ] **Step 2: Create `Models/BusRoute.cs`**

```csharp
namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A transport route with a fee and payment term, optionally served by a bus.</summary>
    public class BusRoute
    {
        public int RouteId { get; set; }
        public string RouteName { get; set; } = "";
        public decimal Fee { get; set; }
        public string PaymentTerm { get; set; } = "Monthly"; // 'Daily' | 'Weekly' | 'Monthly'
        public int? BusId { get; set; }
        public string BusLabel { get; set; } = "";           // joined for display
        public string Stops { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
```

- [ ] **Step 3: Register in csproj** — after `<Compile Include="Models\BookLoan.cs" />` add:

```xml
    <Compile Include="Models\Bus.cs" />
    <Compile Include="Models\BusRoute.cs" />
```

- [ ] **Step 4: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Models/Bus.cs Models/BusRoute.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(transport): Bus and BusRoute models"
```

---

### Task 2: `TransportRepository`

**Files:** Create `Data/ITransportRepository.cs`, `Data/TransportRepository.cs`; Modify csproj.

- [ ] **Step 1: Create `Data/ITransportRepository.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ITransportRepository
    {
        Task EnsureTablesAsync();
        Task<List<Bus>> GetBusesAsync(string search);
        Task<int> AddBusAsync(Bus bus);
        Task UpdateBusAsync(Bus bus);
        Task<bool> DeleteBusAsync(int busId);
        Task<List<BusRoute>> GetRoutesAsync();
        Task<int> AddRouteAsync(BusRoute route);
        Task UpdateRouteAsync(BusRoute route);
        Task<bool> DeleteRouteAsync(int routeId);
    }
}
```

- [ ] **Step 2: Create `Data/TransportRepository.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Buses + routes configuration. SQL Server (LocalDB) via OleDb. No DB-level FK; the
    /// bus-route link is guarded in code (a bus assigned to a route cannot be deleted).
    /// </summary>
    public class TransportRepository : ITransportRepository
    {
        private readonly string _connectionString;

        public TransportRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string buses = @"IF OBJECT_ID(N'Buses', N'U') IS NULL
                    CREATE TABLE Buses (
                        BusId INT IDENTITY(1,1) PRIMARY KEY,
                        Label NVARCHAR(100) NOT NULL, RegNumber NVARCHAR(40),
                        DriverName NVARCHAR(120), DriverContact NVARCHAR(40),
                        Capacity INT NOT NULL DEFAULT (0), Notes NVARCHAR(255), AddedDate DATETIME);";
                using (var cmd = new OleDbCommand(buses, c)) await cmd.ExecuteNonQueryAsync();

                const string routes = @"IF OBJECT_ID(N'BusRoutes', N'U') IS NULL
                    CREATE TABLE BusRoutes (
                        RouteId INT IDENTITY(1,1) PRIMARY KEY,
                        RouteName NVARCHAR(120) NOT NULL, Fee MONEY NOT NULL DEFAULT (0),
                        PaymentTerm NVARCHAR(20) NOT NULL DEFAULT ('Monthly'),
                        BusId INT NULL, Stops NVARCHAR(255), Notes NVARCHAR(255));";
                using (var cmd = new OleDbCommand(routes, c)) await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<Bus>> GetBusesAsync(string search)
        {
            var list = new List<Bus>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                string sql = "SELECT * FROM Buses";
                bool s = !string.IsNullOrWhiteSpace(search);
                if (s) sql += " WHERE Label LIKE ? OR RegNumber LIKE ? OR DriverName LIKE ?";
                sql += " ORDER BY Label";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    if (s)
                    {
                        string like = "%" + search.Trim() + "%";
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                    }
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(MapBus(r));
                }
            }
            return list;
        }

        public async Task<int> AddBusAsync(Bus b)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO Buses (Label,RegNumber,DriverName,DriverContact,Capacity,Notes,AddedDate)
                    VALUES (?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", b.Label ?? "");
                    cmd.Parameters.AddWithValue("?", b.RegNumber ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverName ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverContact ?? "");
                    cmd.Parameters.AddWithValue("?", b.Capacity);
                    cmd.Parameters.AddWithValue("?", b.Notes ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task UpdateBusAsync(Bus b)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE Buses SET Label=?,RegNumber=?,DriverName=?,DriverContact=?,Capacity=?,Notes=? WHERE BusId=?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", b.Label ?? "");
                    cmd.Parameters.AddWithValue("?", b.RegNumber ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverName ?? "");
                    cmd.Parameters.AddWithValue("?", b.DriverContact ?? "");
                    cmd.Parameters.AddWithValue("?", b.Capacity);
                    cmd.Parameters.AddWithValue("?", b.Notes ?? "");
                    cmd.Parameters.AddWithValue("?", b.BusId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteBusAsync(int busId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                int used;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM BusRoutes WHERE BusId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", busId);
                    used = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                if (used > 0) return false;
                using (var cmd = new OleDbCommand("DELETE FROM Buses WHERE BusId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", busId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task<List<BusRoute>> GetRoutesAsync()
        {
            var list = new List<BusRoute>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT r.*, b.Label AS BusLabel FROM BusRoutes r
                    LEFT JOIN Buses b ON r.BusId=b.BusId ORDER BY r.RouteName";
                using (var cmd = new OleDbCommand(sql, c))
                using (var rd = await cmd.ExecuteReaderAsync())
                    while (await rd.ReadAsync()) list.Add(MapRoute(rd));
            }
            return list;
        }

        public async Task<int> AddRouteAsync(BusRoute r)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO BusRoutes (RouteName,Fee,PaymentTerm,BusId,Stops,Notes)
                    VALUES (?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", r.RouteName ?? "");
                    cmd.Parameters.AddWithValue("?", r.Fee);
                    cmd.Parameters.AddWithValue("?", r.PaymentTerm ?? "Monthly");
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = (object)r.BusId ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("?", r.Stops ?? "");
                    cmd.Parameters.AddWithValue("?", r.Notes ?? "");
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task UpdateRouteAsync(BusRoute r)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE BusRoutes SET RouteName=?,Fee=?,PaymentTerm=?,BusId=?,Stops=?,Notes=? WHERE RouteId=?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", r.RouteName ?? "");
                    cmd.Parameters.AddWithValue("?", r.Fee);
                    cmd.Parameters.AddWithValue("?", r.PaymentTerm ?? "Monthly");
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = (object)r.BusId ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("?", r.Stops ?? "");
                    cmd.Parameters.AddWithValue("?", r.Notes ?? "");
                    cmd.Parameters.AddWithValue("?", r.RouteId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteRouteAsync(int routeId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("DELETE FROM BusRoutes WHERE RouteId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", routeId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);
        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
        private static int I(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);

        private static Bus MapBus(IDataRecord r) => new Bus
        {
            BusId = I(r["BusId"]), Label = S(r["Label"]), RegNumber = S(r["RegNumber"]),
            DriverName = S(r["DriverName"]), DriverContact = S(r["DriverContact"]),
            Capacity = I(r["Capacity"]), Notes = S(r["Notes"]),
            AddedDate = r["AddedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["AddedDate"])
        };

        private static BusRoute MapRoute(IDataRecord r) => new BusRoute
        {
            RouteId = I(r["RouteId"]), RouteName = S(r["RouteName"]),
            Fee = r["Fee"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Fee"]),
            PaymentTerm = S(r["PaymentTerm"]),
            BusId = r["BusId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BusId"]),
            BusLabel = S(r["BusLabel"]), Stops = S(r["Stops"]), Notes = S(r["Notes"])
        };
    }
}
```

- [ ] **Step 3: Register in csproj** — after `<Compile Include="Data\LibraryRepository.cs" />` add:

```xml
    <Compile Include="Data\ITransportRepository.cs" />
    <Compile Include="Data\TransportRepository.cs" />
```

- [ ] **Step 4: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Data/ITransportRepository.cs Data/TransportRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(transport): TransportRepository (buses + routes CRUD)"
```

---

### Task 3: RBAC + `frmTransport`

**Files:** Create `frmTransport.cs`; Modify `Services/AuthService.cs`, csproj.

- [ ] **Step 1: Add RBAC entry** — in `Services/AuthService.cs`, after `["frmLibrary"] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },` add:

```csharp
            ["frmTransport"]              = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
```

- [ ] **Step 2: Create `frmTransport.cs`**

```csharp
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Transport configuration: buses and routes (fee + payment term). Director/Admin/Headmaster.
    /// </summary>
    public class frmTransport : Form
    {
        private readonly TransportRepository _repo = new TransportRepository(AppConfig.ConnectionString);
        private DataGridView _busGrid, _routeGrid;
        private TextBox _busSearch;
        private Label _status;

        public frmTransport()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmTransport", this)) return;
            Load += async (s, e) => await InitAsync();
        }

        private void BuildUi()
        {
            Text = "Transport";
            Size = new Size(920, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Transport",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildBusesTab());
            tabs.TabPages.Add(BuildRoutesTab());
            _status = new Label { Dock = DockStyle.Bottom, Height = 24, ForeColor = AppConfig.Colors.MutedTextColor, Padding = new Padding(12, 0, 0, 0) };

            Controls.Add(tabs);
            Controls.Add(_status);
            Controls.Add(title);
        }

        private TabPage BuildBusesTab()
        {
            var tab = new TabPage("Buses") { BackColor = Color.White };
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
            _busSearch = new TextBox { Dock = DockStyle.Left, Width = 280, Font = new Font("Segoe UI", 10F) };
            _busSearch.TextChanged += async (s, e) => await LoadBusesAsync();
            var add = new Button { Dock = DockStyle.Right, Width = 90, Text = "Add", FlatStyle = FlatStyle.Flat };
            var edit = new Button { Dock = DockStyle.Right, Width = 90, Text = "Edit", FlatStyle = FlatStyle.Flat };
            var del = new Button { Dock = DockStyle.Right, Width = 90, Text = "Delete", FlatStyle = FlatStyle.Flat };
            add.Click += async (s, e) => { var b = BusDialog(null); if (b != null) { await _repo.AddBusAsync(b); await LoadBusesAsync(); } };
            edit.Click += async (s, e) => await EditBusAsync();
            del.Click += async (s, e) => await DeleteBusAsync();
            bar.Controls.Add(_busSearch); bar.Controls.Add(del); bar.Controls.Add(edit); bar.Controls.Add(add);

            _busGrid = NewGrid();
            tab.Controls.Add(_busGrid);
            tab.Controls.Add(bar);
            return tab;
        }

        private TabPage BuildRoutesTab()
        {
            var tab = new TabPage("Routes") { BackColor = Color.White };
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
            var add = new Button { Dock = DockStyle.Right, Width = 90, Text = "Add", FlatStyle = FlatStyle.Flat };
            var edit = new Button { Dock = DockStyle.Right, Width = 90, Text = "Edit", FlatStyle = FlatStyle.Flat };
            var del = new Button { Dock = DockStyle.Right, Width = 90, Text = "Delete", FlatStyle = FlatStyle.Flat };
            add.Click += async (s, e) => { var r = await RouteDialogAsync(null); if (r != null) { await _repo.AddRouteAsync(r); await LoadRoutesAsync(); } };
            edit.Click += async (s, e) => await EditRouteAsync();
            del.Click += async (s, e) => await DeleteRouteAsync();
            bar.Controls.Add(del); bar.Controls.Add(edit); bar.Controls.Add(add);

            _routeGrid = NewGrid();
            tab.Controls.Add(_routeGrid);
            tab.Controls.Add(bar);
            return tab;
        }

        private static DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
        };

        private async Task InitAsync()
        {
            try { await _repo.EnsureTablesAsync(); await LoadBusesAsync(); await LoadRoutesAsync(); }
            catch (Exception ex) { UIHelper.ShowError("Could not load transport: " + ex.Message, "Transport"); }
        }

        private async Task LoadBusesAsync()
        {
            var buses = await _repo.GetBusesAsync(_busSearch.Text);
            _busGrid.DataSource = buses.Select(b => new { b.BusId, b.Label, Reg = b.RegNumber, Driver = b.DriverName, Contact = b.DriverContact, b.Capacity }).ToList();
            if (_busGrid.Columns.Contains("BusId")) _busGrid.Columns["BusId"].Visible = false;
            _status.Text = buses.Count + " bus(es).";
        }

        private async Task LoadRoutesAsync()
        {
            var routes = await _repo.GetRoutesAsync();
            _routeGrid.DataSource = routes.Select(r => new { r.RouteId, Route = r.RouteName, Fee = r.Fee.ToString("N2"), Term = r.PaymentTerm, Bus = r.BusLabel, r.Stops }).ToList();
            if (_routeGrid.Columns.Contains("RouteId")) _routeGrid.Columns["RouteId"].Visible = false;
        }

        private Bus SelectedBus()
        {
            if (_busGrid.CurrentRow?.Cells["BusId"].Value == null) return null;
            return new Bus
            {
                BusId = Convert.ToInt32(_busGrid.CurrentRow.Cells["BusId"].Value),
                Label = _busGrid.CurrentRow.Cells["Label"].Value?.ToString() ?? "",
                RegNumber = _busGrid.CurrentRow.Cells["Reg"].Value?.ToString() ?? "",
                DriverName = _busGrid.CurrentRow.Cells["Driver"].Value?.ToString() ?? "",
                DriverContact = _busGrid.CurrentRow.Cells["Contact"].Value?.ToString() ?? "",
                Capacity = Convert.ToInt32(_busGrid.CurrentRow.Cells["Capacity"].Value ?? 0)
            };
        }

        private async Task EditBusAsync()
        {
            var existing = SelectedBus();
            if (existing == null) { UIHelper.ShowWarning("Select a bus to edit.", "Transport"); return; }
            var edited = BusDialog(existing);
            if (edited == null) return;
            edited.BusId = existing.BusId;
            await _repo.UpdateBusAsync(edited);
            await LoadBusesAsync();
        }

        private async Task DeleteBusAsync()
        {
            var b = SelectedBus();
            if (b == null) { UIHelper.ShowWarning("Select a bus to delete.", "Transport"); return; }
            if (UIHelper.ShowConfirmation($"Delete bus \"{b.Label}\"?", "Transport") != DialogResult.Yes) return;
            if (!await _repo.DeleteBusAsync(b.BusId)) { UIHelper.ShowWarning("Cannot delete: the bus is assigned to a route.", "Transport"); return; }
            await LoadBusesAsync();
        }

        private async Task EditRouteAsync()
        {
            if (_routeGrid.CurrentRow?.Cells["RouteId"].Value == null) { UIHelper.ShowWarning("Select a route to edit.", "Transport"); return; }
            int id = Convert.ToInt32(_routeGrid.CurrentRow.Cells["RouteId"].Value);
            var existing = (await _repo.GetRoutesAsync()).FirstOrDefault(x => x.RouteId == id);
            if (existing == null) return;
            var edited = await RouteDialogAsync(existing);
            if (edited == null) return;
            edited.RouteId = id;
            await _repo.UpdateRouteAsync(edited);
            await LoadRoutesAsync();
        }

        private async Task DeleteRouteAsync()
        {
            if (_routeGrid.CurrentRow?.Cells["RouteId"].Value == null) { UIHelper.ShowWarning("Select a route to delete.", "Transport"); return; }
            int id = Convert.ToInt32(_routeGrid.CurrentRow.Cells["RouteId"].Value);
            string name = _routeGrid.CurrentRow.Cells["Route"].Value?.ToString() ?? "";
            if (UIHelper.ShowConfirmation($"Delete route \"{name}\"?", "Transport") != DialogResult.Yes) return;
            await _repo.DeleteRouteAsync(id);
            await LoadRoutesAsync();
        }

        // ── Dialogs ──────────────────────────────────────────────────────────────
        private Bus BusDialog(Bus existing)
        {
            var dlg = new Form { Text = existing == null ? "Add Bus" : "Edit Bus", Size = new Size(380, 340), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            TextBox Mk(string label, int y, string val) { dlg.Controls.Add(new Label { Left = 14, Top = y + 3, Width = 100, Text = label }); var t = new TextBox { Left = 120, Top = y, Width = 220, Text = val ?? "" }; dlg.Controls.Add(t); return t; }
            var t1 = Mk("Label", 16, existing?.Label);
            var t2 = Mk("Reg number", 52, existing?.RegNumber);
            var t3 = Mk("Driver", 88, existing?.DriverName);
            var t4 = Mk("Driver contact", 124, existing?.DriverContact);
            dlg.Controls.Add(new Label { Left = 14, Top = 163, Width = 100, Text = "Capacity" });
            var nc = new NumericUpDown { Left = 120, Top = 160, Width = 80, Minimum = 0, Maximum = 200, Value = existing == null ? 0 : Math.Min(200, existing.Capacity) };
            dlg.Controls.Add(nc);
            var t5 = Mk("Notes", 196, existing?.Notes);
            var ok = new Button { Left = 120, Top = 240, Width = 100, Height = 32, Text = "Save", DialogResult = DialogResult.OK, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Left = 232, Top = 240, Width = 100, Height = 32, Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            dlg.Controls.Add(ok); dlg.Controls.Add(cancel); dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return null;
            if (string.IsNullOrWhiteSpace(t1.Text)) { UIHelper.ShowWarning("Label is required.", "Transport"); return null; }
            return new Bus { Label = t1.Text.Trim(), RegNumber = t2.Text.Trim(), DriverName = t3.Text.Trim(), DriverContact = t4.Text.Trim(), Capacity = (int)nc.Value, Notes = t5.Text.Trim() };
        }

        private async Task<BusRoute> RouteDialogAsync(BusRoute existing)
        {
            var buses = await _repo.GetBusesAsync(null);
            var dlg = new Form { Text = existing == null ? "Add Route" : "Edit Route", Size = new Size(400, 360), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            TextBox Mk(string label, int y, string val) { dlg.Controls.Add(new Label { Left = 14, Top = y + 3, Width = 100, Text = label }); var t = new TextBox { Left = 120, Top = y, Width = 240, Text = val ?? "" }; dlg.Controls.Add(t); return t; }
            var name = Mk("Route name", 16, existing?.RouteName);
            dlg.Controls.Add(new Label { Left = 14, Top = 55, Width = 100, Text = "Fee" });
            var fee = new NumericUpDown { Left = 120, Top = 52, Width = 120, DecimalPlaces = 2, Minimum = 0, Maximum = 100000, Value = existing == null ? 0 : Math.Min(100000, existing.Fee) };
            dlg.Controls.Add(fee);
            dlg.Controls.Add(new Label { Left = 14, Top = 91, Width = 100, Text = "Payment term" });
            var term = new ComboBox { Left = 120, Top = 88, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            term.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly" });
            term.SelectedItem = existing?.PaymentTerm ?? "Monthly"; if (term.SelectedIndex < 0) term.SelectedIndex = 2;
            dlg.Controls.Add(term);
            dlg.Controls.Add(new Label { Left = 14, Top = 127, Width = 100, Text = "Bus" });
            var bus = new ComboBox { Left = 120, Top = 124, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            bus.Items.Add("(none)");
            foreach (var b in buses) bus.Items.Add(b);
            bus.SelectedIndex = 0;
            if (existing?.BusId != null) { var match = buses.FirstOrDefault(x => x.BusId == existing.BusId.Value); if (match != null) bus.SelectedItem = match; }
            var stops = Mk("Stops", 160, existing?.Stops);
            var notes = Mk("Notes", 196, existing?.Notes);
            var ok = new Button { Left = 120, Top = 250, Width = 100, Height = 32, Text = "Save", DialogResult = DialogResult.OK, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Left = 232, Top = 250, Width = 100, Height = 32, Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            dlg.Controls.Add(ok); dlg.Controls.Add(cancel); dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return null;
            if (string.IsNullOrWhiteSpace(name.Text)) { UIHelper.ShowWarning("Route name is required.", "Transport"); return null; }
            int? busId = bus.SelectedItem is Bus sb ? (int?)sb.BusId : null;
            return new BusRoute { RouteName = name.Text.Trim(), Fee = fee.Value, PaymentTerm = term.SelectedItem.ToString(), BusId = busId, Stops = stops.Text.Trim(), Notes = notes.Text.Trim() };
        }
    }
}
```

- [ ] **Step 3: Register in csproj** — after `<Compile Include="frmLibrary.cs" />` add:

```xml
    <Compile Include="frmTransport.cs" />
```

- [ ] **Step 4: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 5: Render-verify (offline harness)** — render `frmTransport` (Director); confirm Buses tab (search + grid + Add/Edit/Delete) and Routes tab (grid + Add/Edit/Delete).

- [ ] **Step 6: Commit**

```bash
git add frmTransport.cs Services/AuthService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(transport): frmTransport (buses + routes config) + RBAC"
```

---

### Task 4: Dashboard nav entry

**Files:** Modify `frmDashboard.cs`.

- [ ] **Step 1: Add the nav button** — after `nav.Controls.Add(CreateNavButton("Library", () => new frmLibrary().ShowDialog()));` add:

```csharp
                nav.Controls.Add(CreateNavButton("Transport", () => new frmTransport().ShowDialog()));
```

- [ ] **Step 2: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 3: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(transport): add Transport to dashboard settings nav"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test:** Director/Admin/Headmaster → dashboard → **Transport** → Buses tab: add 2 buses → Routes tab: add a route with a fee, term (Weekly), and an assigned bus → it shows the bus label; edit/delete work; deleting an assigned bus is blocked.

## Self-review notes

- **Spec coverage:** Buses + BusRoutes tables (Task 2) ✓; models (Task 1) ✓; repo CRUD + bus
  delete-guard + route join (Task 2) ✓; frmTransport Buses/Routes tabs with dialogs incl. fee +
  payment-term combo + bus combo (Task 3) ✓; RBAC (Task 3) ✓; dashboard nav (Task 4) ✓; no
  fee-table changes ✓.
- **Type/name consistency:** `TransportRepository` methods match the interface and the form calls;
  `Bus`/`BusRoute` property names match mappers, grids, and dialogs; nullable `BusId` bound with
  `OleDbType.Integer`/`DBNull`.
- **No placeholders:** every step shows complete code; the probe is best-effort (LocalDB-dependent).
- **Phase boundary:** Phase 2 (admission "takes the bus?" radio + recording the route on the
  student) is intentionally a separate spec, not in this plan.
```
