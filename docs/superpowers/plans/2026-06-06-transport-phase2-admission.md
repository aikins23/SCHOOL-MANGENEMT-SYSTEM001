# Transport Phase 2 (Admission Bus Choice) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Capture a student's bus route at admission and record it on the student (informational), with view/change in student details.

**Architecture:** A `StudentTransport(StudentID, RouteId)` link table in the Transport module; the draft carries a nullable `BusRouteId` that's written to the link on bursar approval. `frmAddStd` gets a "School bus?" radio + route combo; `frmStdDetails` shows/edits the route. No `Students`-table or fee-table changes.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-transport-phase2-admission-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gate: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors. DB-dependent checks deferred to the user (LocalDB).
- **`datetime` caveat** truncate-to-seconds already handled in the draft repo. `MONEY` ↔ `decimal`.
- Phase 1 (`TransportRepository`, `Bus`, `BusRoute`, `frmTransport`) is already built and committed.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Modify** `Models/DraftAdmission.cs` (add `BusRouteId`)
- **Modify** `Data/DraftAdmissionRepository.cs` (column ALTER + store/read)
- **Modify** `Data/ITransportRepository.cs`, `Data/TransportRepository.cs` (StudentTransport table + methods)
- **Modify** `Services/DraftAdmissionService.cs` (write link on approval)
- **Modify** `frmAddStd.cs` (radio + route combo → draft)
- **Modify** `frmStdDetails.cs` (view/change route)

---

### Task 1: Draft carries `BusRouteId`

**Files:** Modify `Models/DraftAdmission.cs`, `Data/DraftAdmissionRepository.cs`.

- [ ] **Step 1: Add the property** — in `Models/DraftAdmission.cs`, after `public DateTime SubmittedDate { get; set; }` add:

```csharp
        public int? BusRouteId { get; set; }   // chosen transport route, null = no bus
```

- [ ] **Step 2: Add the idempotent column** — in `Data/DraftAdmissionRepository.cs` `EnsureTableAsync`, immediately after the `CREATE TABLE DraftAdmissions (...)` command executes, add a second command:

```csharp
                using (var alter = new OleDbCommand(
                    "IF COL_LENGTH('DraftAdmissions','BusRouteId') IS NULL ALTER TABLE DraftAdmissions ADD BusRouteId INT NULL", c))
                    await alter.ExecuteNonQueryAsync();
```

(Place it inside the same `using (var c = new OleDbConnection...)` block, after the existing `CREATE TABLE` `ExecuteNonQueryAsync()`.)

- [ ] **Step 3: Persist it in `AddAsync`** — the INSERT currently lists 21 columns ending `...SubmittedBy,SubmittedDate) VALUES (?,?,…21…)`. Add `BusRouteId` as a 22nd column + parameter. Change the column list to end `...,SubmittedDate,BusRouteId)`, the VALUES to have 22 `?`, and after the `TruncateSeconds(d.SubmittedDate)` parameter add:

```csharp
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = (object)d.BusRouteId ?? DBNull.Value;
```

(Match the existing column/`?`/parameter ordering exactly: `BusRouteId` is appended last, so its `?` and parameter are last.)

- [ ] **Step 4: Read it in `Map`** — in the `Map(IDataRecord r)` initializer, after `SubmittedDate = Convert.ToDateTime(r["SubmittedDate"])` add a comma and:

```csharp
            BusRouteId = HasCol(r, "BusRouteId") && r["BusRouteId"] != DBNull.Value ? Convert.ToInt32(r["BusRouteId"]) : (int?)null
```

Then add this helper to the class (next to `TruncateSeconds`):

```csharp
        private static bool HasCol(IDataRecord r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
```

- [ ] **Step 5: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Models/DraftAdmission.cs Data/DraftAdmissionRepository.cs
git commit -m "feat(transport): draft admission carries BusRouteId"
```

---

### Task 2: `StudentTransport` table + repository methods

**Files:** Modify `Data/ITransportRepository.cs`, `Data/TransportRepository.cs`.

- [ ] **Step 1: Extend the interface** — in `Data/ITransportRepository.cs`, before the closing brace of the interface add:

```csharp
        Task SetStudentRouteAsync(int studentId, int? routeId);
        Task<BusRoute> GetStudentRouteAsync(int studentId);
```

- [ ] **Step 2: Create the table** — in `TransportRepository.EnsureTablesAsync`, after the `BusRoutes` `CREATE TABLE` command, add:

```csharp
                const string stTransport = @"IF OBJECT_ID(N'StudentTransport', N'U') IS NULL
                    CREATE TABLE StudentTransport (
                        StudentID INT NOT NULL PRIMARY KEY,
                        RouteId INT NOT NULL);";
                using (var cmd = new OleDbCommand(stTransport, c)) await cmd.ExecuteNonQueryAsync();
```

- [ ] **Step 3: Add the methods** — in `TransportRepository`, before the private helpers (`TruncateSeconds`), add:

```csharp
        public async Task SetStudentRouteAsync(int studentId, int? routeId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var del = new OleDbCommand("DELETE FROM StudentTransport WHERE StudentID=?", c))
                {
                    del.Parameters.AddWithValue("?", studentId);
                    await del.ExecuteNonQueryAsync();
                }
                if (routeId.HasValue)
                {
                    using (var ins = new OleDbCommand("INSERT INTO StudentTransport (StudentID, RouteId) VALUES (?, ?)", c))
                    {
                        ins.Parameters.AddWithValue("?", studentId);
                        ins.Parameters.AddWithValue("?", routeId.Value);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<BusRoute> GetStudentRouteAsync(int studentId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT r.*, b.Label AS BusLabel FROM StudentTransport st
                    INNER JOIN BusRoutes r ON st.RouteId = r.RouteId
                    LEFT JOIN Buses b ON r.BusId = b.BusId WHERE st.StudentID = ?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", studentId);
                    using (var rd = await cmd.ExecuteReaderAsync())
                        return await rd.ReadAsync() ? MapRoute(rd) : null;
                }
            }
        }
```

(`MapRoute` already exists from Phase 1.)

- [ ] **Step 4: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Data/ITransportRepository.cs Data/TransportRepository.cs
git commit -m "feat(transport): StudentTransport link table + set/get student route"
```

---

### Task 3: Write the route on approval

**Files:** Modify `Services/DraftAdmissionService.cs`.

- [ ] **Step 1: Add a transport repo field** — in `DraftAdmissionService`, after the `_fees` field add:

```csharp
        private readonly TransportRepository _transport = new TransportRepository(Common.AppConfig.ConnectionString);
```

(The class is in `Services`; it already uses `Data` types. `TransportRepository` resolves via the existing `using ...Data;` — if not present, fully-qualify as `Data.TransportRepository`. `Common.AppConfig` is fully-qualified here.)

- [ ] **Step 2: Persist the link after the student is created** — in `ApproveAsync`, after the two `AddPaymentRecordAsync` calls and before `await _drafts.DeleteAsync(draftId);`, add:

```csharp
            if (d.BusRouteId.HasValue && int.TryParse(student.StudentID, out int sidForBus))
            {
                try { await _transport.SetStudentRouteAsync(sidForBus, d.BusRouteId); }
                catch (Exception ex) { LoggerHelper.LogWarning("Transport link not saved for student " + student.StudentID + ": " + ex.Message); }
            }
```

(`LoggerHelper` is already used in this file.)

- [ ] **Step 3: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Services/DraftAdmissionService.cs
git commit -m "feat(transport): record student's bus route on admission approval"
```

---

### Task 4: Admission "School bus?" radio + route combo (`frmAddStd`)

**Files:** Modify `frmAddStd.cs`.

- [ ] **Step 1: Add fields** — in `frmAddStd.cs`, near the other private field declarations (top of the class, e.g. after `private Panel pageHost;`) add:

```csharp
        private RadioButton _rbBusYes, _rbBusNo;
        private ComboBox _cmbRoute;
```

- [ ] **Step 2: Add the bus row to the Guardian panel** — in `BuildGuardianPanel`, change `CreateSectionLayout("Guardian and Admission", 4, 2)` to `CreateSectionLayout("Guardian and Admission", 5, 2)`, then after the Allergies line (the `layout.SetColumnSpan(...)` line) add:

```csharp
            var busPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = SurfaceColor };
            _rbBusNo = new RadioButton { Text = "No", Checked = true, AutoSize = true, Margin = new Padding(0, 8, 16, 0), ForeColor = TextColor };
            _rbBusYes = new RadioButton { Text = "Yes", AutoSize = true, Margin = new Padding(0, 8, 0, 0), ForeColor = TextColor };
            busPanel.Controls.Add(_rbBusNo);
            busPanel.Controls.Add(_rbBusYes);
            _cmbRoute = new Guna.UI2.WinForms.Guna2ComboBox { Enabled = false };
            _rbBusYes.CheckedChanged += (s, e) => _cmbRoute.Enabled = _rbBusYes.Checked;
            layout.Controls.Add(CreateField("School Bus?", busPanel), 0, 4);
            layout.Controls.Add(CreateField("Bus Route", _cmbRoute), 1, 4);
```

(`Guna.UI2.WinForms.Guna2ComboBox` is the combo type used elsewhere in this form, e.g. `cmbCID`. If a plain `System.Windows.Forms.ComboBox` is simpler/compiles, that is acceptable too — but match `cmbCID`'s type to keep the theme.)

- [ ] **Step 3: Load routes on form load** — in `frmAddStd_Load`, inside the `try`, after `LoadGenderDropdown();` add:

```csharp
                await LoadBusRoutesAsync();
```

Then add the method (anywhere in the class):

```csharp
        private async Task LoadBusRoutesAsync()
        {
            try
            {
                var repo = new Data.TransportRepository(AppConfig.ConnectionString);
                await repo.EnsureTablesAsync();
                var routes = await repo.GetRoutesAsync();
                _cmbRoute.Items.Clear();
                foreach (var r in routes)
                    _cmbRoute.Items.Add(new RouteItem(r));
                if (_cmbRoute.Items.Count > 0) _cmbRoute.SelectedIndex = 0;
            }
            catch (Exception ex) { LoggerHelper.LogWarning("Could not load bus routes: " + ex.Message); }
        }

        private sealed class RouteItem
        {
            public Models.BusRoute Route { get; }
            public RouteItem(Models.BusRoute r) { Route = r; }
            public override string ToString() =>
                Route.RouteName + " — GHS " + Route.Fee.ToString("N2") + " / " + Route.PaymentTerm;
        }
```

- [ ] **Step 4: Validate + set `BusRouteId` on the draft** — in the `isNew` branch (the `new Models.DraftAdmission { ... }` initializer, ~line 974), first add validation immediately BEFORE building the draft:

```csharp
                    if (_rbBusYes.Checked && !(_cmbRoute.SelectedItem is RouteItem))
                    { UIHelper.ShowWarning("Select a bus route, or choose No.", "Student Registration"); return; }
```

Then add this line inside the `new Models.DraftAdmission { ... }` initializer (e.g. after `SubmittedBy = AuthService.CurrentUser.Username`):

```csharp
                        ,BusRouteId = (_rbBusYes.Checked && _cmbRoute.SelectedItem is RouteItem ri) ? ri.Route.RouteId : (int?)null
```

(Place the leading comma correctly so the initializer stays valid — append it as the last member.)

- [ ] **Step 5: Reset in `ClearStudentDetails`** — at the end of `ClearStudentDetails`, add:

```csharp
            if (_rbBusNo != null) _rbBusNo.Checked = true;
            if (_cmbRoute != null) { _cmbRoute.SelectedIndex = _cmbRoute.Items.Count > 0 ? 0 : -1; _cmbRoute.Enabled = false; }
```

- [ ] **Step 6: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 7: Render-verify** — render `frmAddStd`; on the Guardian/Admission step confirm the "School Bus?" Yes/No radios + a "Bus Route" combo (disabled until Yes).

- [ ] **Step 8: Commit**

```bash
git add frmAddStd.cs
git commit -m "feat(transport): admission 'takes the bus?' radio + route selection"
```

---

### Task 5: View/change route in `frmStdDetails`

**Files:** Modify `frmStdDetails.cs`.

- [ ] **Step 1: Add fields** — near the top of `frmStdDetails`'s class fields add:

```csharp
        private Label _busRouteLabel;
        private int _currentStudentNumericId;
```

- [ ] **Step 2: Add a bottom bus-route bar** — at the END of the constructor (after `InitializeComponent()` / the existing build calls, after the access check) add:

```csharp
            var busBar = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12, 6, 12, 6) };
            _busRouteLabel = new Label { Dock = DockStyle.Left, Width = 420, Text = "Bus route: —", TextAlign = ContentAlignment.MiddleLeft, ForeColor = AppConfig.Colors.TextColor, Font = new Font("Segoe UI", 10F) };
            var changeBtn = new Button { Dock = DockStyle.Right, Width = 130, Text = "Change Bus Route", FlatStyle = FlatStyle.Flat };
            changeBtn.Click += async (s, e) => await ChangeBusRouteAsync();
            busBar.Controls.Add(_busRouteLabel);
            busBar.Controls.Add(changeBtn);
            Controls.Add(busBar);
```

(If the constructor returns early on `RequireAccess` failure, add this before that early-return guard, or it's fine after since a denied form is closed.)

- [ ] **Step 3: Load the route when a student is shown** — find the line that sets `txtStdID.Text = Common.StudentId.Display(row["ID"]);` and add immediately after it:

```csharp
            _currentStudentNumericId = Convert.ToInt32(row["ID"]);
            _ = LoadBusRouteAsync();
```

Then add:

```csharp
        private async Task LoadBusRouteAsync()
        {
            try
            {
                var repo = new Data.TransportRepository(AppConfig.ConnectionString);
                await repo.EnsureTablesAsync();
                var route = await repo.GetStudentRouteAsync(_currentStudentNumericId);
                _busRouteLabel.Text = route == null
                    ? "Bus route: None"
                    : "Bus route: " + route.RouteName + " (GHS " + route.Fee.ToString("N2") + " / " + route.PaymentTerm + ")";
            }
            catch (Exception ex) { LoggerHelper.LogWarning("Load bus route failed: " + ex.Message); }
        }
```

- [ ] **Step 4: Change-route dialog** — add:

```csharp
        private async Task ChangeBusRouteAsync()
        {
            if (_currentStudentNumericId <= 0) { UIHelper.ShowWarning("Load a student first.", "Bus Route"); return; }
            var repo = new Data.TransportRepository(AppConfig.ConnectionString);
            await repo.EnsureTablesAsync();
            var routes = await repo.GetRoutesAsync();

            var dlg = new Form { Text = "Change Bus Route", Size = new Size(380, 170), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            dlg.Controls.Add(new Label { Left = 14, Top = 18, Width = 80, Text = "Route" });
            var combo = new ComboBox { Left = 100, Top = 15, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            combo.Items.Add("None");
            foreach (var r in routes) combo.Items.Add(r.RouteName + " — GHS " + r.Fee.ToString("N2") + " / " + r.PaymentTerm);
            combo.SelectedIndex = 0;
            dlg.Controls.Add(combo);
            var ok = new Button { Left = 100, Top = 70, Width = 100, Height = 30, Text = "Save", DialogResult = DialogResult.OK, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Left = 212, Top = 70, Width = 100, Height = 30, Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            dlg.Controls.Add(ok); dlg.Controls.Add(cancel); dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            int? routeId = combo.SelectedIndex <= 0 ? (int?)null : routes[combo.SelectedIndex - 1].RouteId;
            try
            {
                await repo.SetStudentRouteAsync(_currentStudentNumericId, routeId);
                await LoadBusRouteAsync();
                UIHelper.ShowSuccess("Bus route updated.", "Bus Route");
            }
            catch (Exception ex) { UIHelper.ShowError("Could not update bus route: " + ex.Message, "Bus Route"); }
        }
```

(Ensure `using System.Drawing;`, `System.Windows.Forms;`, `System.Threading.Tasks;`, and the `Common`/`Data`/`Services` namespaces are imported in `frmStdDetails.cs` — they are, since it already uses `UIHelper`, `Common.StudentId`, and WinForms controls.)

- [ ] **Step 5: Build clean** — `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.

- [ ] **Step 6: Commit**

```bash
git add frmStdDetails.cs
git commit -m "feat(transport): view/change a student's bus route in student details"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app):**
  1. Transport → add a route (e.g. "Town Route", GHS 80, Monthly).
  2. Add Student → Guardian/Admission step → School Bus? **Yes** → pick "Town Route" → pay & submit → bursar **Approve**.
  3. Open that student in View Students → details → "Bus route: Town Route (GHS 80.00 / Monthly)".
  4. **Change Bus Route** → None → label shows "None".
  5. Admit a student with **No** → details show "Bus route: None".
  6. Selecting **Yes** with no route picked → blocked with a warning.

## Self-review notes

- **Spec coverage:** `StudentTransport` table + draft `BusRouteId` (Tasks 1–2) ✓; repo set/get
  (Task 2) ✓; write link on approval (Task 3) ✓; admission radio + route combo + validation
  (Task 4) ✓; student-details view/change (Task 5) ✓; no Students/fee-table changes ✓; fail-safe
  (transport write is try/caught, non-fatal) ✓.
- **Type/name consistency:** `DraftAdmission.BusRouteId`, `TransportRepository.SetStudentRouteAsync/
  GetStudentRouteAsync`, `RouteItem` wrapper, `MapRoute` reuse — consistent. The draft INSERT must
  keep column/`?`/parameter counts aligned (Task 1 Step 3 makes it 22 each).
- **No placeholders:** every step has concrete code; one acceptable substitution noted
  (Guna2ComboBox vs ComboBox).
- **Risk:** `frmAddStd` INSERT-parameter ordering — Task 1 Step 3 calls out keeping the
  column/value/param order aligned; build will catch a mismatch only at runtime, so the executor
  must verify the 22/22/22 alignment by eye.
```
