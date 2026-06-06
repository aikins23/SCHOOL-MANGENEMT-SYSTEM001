# Configurable Grading Scheme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the report-card grade bands editable in-app (Director/Administrator) instead of hardcoded, with all grade computation and the report-card legend reading from the configured bands.

**Architecture:** A `GradingScheme` table of bands + a fail-safe cached `Common/GradingScheme` accessor (same pattern as the School Information feature's `SchoolInfoRepository`/`SchoolProfile`). The primary report-card generator reads grade code, remark, and legend from it. A `frmGradingScheme` settings form edits the bands.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-configurable-grading-scheme-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) PowerShell reflection probe for pure logic; (3) render harness / user run for the form.
- **Explicit-include csproj:** every new `.cs` MUST get a `<Compile Include="..." />` entry (code-only forms self-closing).
- **`datetime` caveat** does not apply here (no datetime columns).
- `AppConfig.ConnectionString`, `AppConfig.Colors`, `UiTheme`, `UIHelper.Show{Success,Error,Warning}`, `AuthService.RequireAccess`, `LoggerHelper.LogError` all exist and are used by the School Information feature already.
- **Scope note (verified):** `Services/ReportCardPdfService.cs` has `Export(...)` with `useOfficialTemplate = true`, which delegates to `ReportCardPDFGenerator` and returns; its own draw path (and its hardcoded legends at ~lines 372 and 530) is **unreachable dead code** and is intentionally left untouched. The spec assumed both generators were live; only `ReportCardPDFGenerator` is.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Create** `Models/GradeBand.cs`
- **Create** `Data/IGradingSchemeRepository.cs`, `Data/GradingSchemeRepository.cs`
- **Create** `Common/GradingScheme.cs`
- **Create** `frmGradingScheme.cs`
- **Modify** `Services/ReportCardPDFGenerator.cs`, `Services/AuthService.cs`, `frmDashboard.cs`, `kingdom_Preparatory_School_Management_System.csproj`

---

### Task 1: `GradeBand` model + repository

**Files:**
- Create: `Models/GradeBand.cs`, `Data/IGradingSchemeRepository.cs`, `Data/GradingSchemeRepository.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Models/GradeBand.cs`**

```csharp
namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>One grade band: scores &gt;= MinScore (and below the next higher band's
    /// MinScore) get this Code (printed in the subject Grade column) and Label (remark + legend).</summary>
    public class GradeBand
    {
        public int MinScore { get; set; }
        public string Code { get; set; } = "";
        public string Label { get; set; } = "";
    }
}
```

- [ ] **Step 2: Create `Data/IGradingSchemeRepository.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IGradingSchemeRepository
    {
        Task EnsureTableAsync();
        Task<List<GradeBand>> GetBandsAsync();
        Task SaveBandsAsync(IEnumerable<GradeBand> bands);
    }
}
```

- [ ] **Step 3: Create `Data/GradingSchemeRepository.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Persists the school-wide grading scheme (ordered grade bands). Created and seeded
    /// from the legacy 5-band scheme on first use. SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class GradingSchemeRepository : IGradingSchemeRepository
    {
        private readonly string _connectionString;

        public GradingSchemeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string create = @"IF OBJECT_ID(N'GradingScheme', N'U') IS NULL
                    CREATE TABLE GradingScheme (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        MinScore INT NOT NULL,
                        Code NVARCHAR(10),
                        Label NVARCHAR(80));";
                using (var cmd = new OleDbCommand(create, c)) await cmd.ExecuteNonQueryAsync();

                bool hasRows;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM GradingScheme", c))
                    hasRows = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                if (!hasRows)
                {
                    foreach (var b in LegacyBands())
                    {
                        using (var cmd = new OleDbCommand(
                            "INSERT INTO GradingScheme (MinScore, Code, Label) VALUES (?, ?, ?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", b.MinScore);
                            cmd.Parameters.AddWithValue("?", b.Code);
                            cmd.Parameters.AddWithValue("?", b.Label);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        public async Task<List<GradeBand>> GetBandsAsync()
        {
            var list = new List<GradeBand>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT MinScore, Code, Label FROM GradingScheme ORDER BY MinScore DESC", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new GradeBand
                        {
                            MinScore = Convert.ToInt32(r["MinScore"]),
                            Code = AsString(r["Code"]),
                            Label = AsString(r["Label"])
                        });
            }
            return list;
        }

        public async Task SaveBandsAsync(IEnumerable<GradeBand> bands)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var del = new OleDbCommand("DELETE FROM GradingScheme", c))
                    await del.ExecuteNonQueryAsync();
                foreach (var b in bands)
                {
                    using (var cmd = new OleDbCommand(
                        "INSERT INTO GradingScheme (MinScore, Code, Label) VALUES (?, ?, ?)", c))
                    {
                        cmd.Parameters.AddWithValue("?", b.MinScore);
                        cmd.Parameters.AddWithValue("?", b.Code ?? "");
                        cmd.Parameters.AddWithValue("?", b.Label ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>The hardcoded scheme used to seed the table and as the fail-safe default.</summary>
        public static List<GradeBand> LegacyBands() => new List<GradeBand>
        {
            new GradeBand { MinScore = 80, Code = "1", Label = "Advance(A)" },
            new GradeBand { MinScore = 75, Code = "2", Label = "Proficiency(P)" },
            new GradeBand { MinScore = 70, Code = "3", Label = "Approaching Proficiency(AP)" },
            new GradeBand { MinScore = 65, Code = "4", Label = "Developing" },
            new GradeBand { MinScore = 0,  Code = "5", Label = "Beginning" }
        };

        private static string AsString(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
    }
}
```

- [ ] **Step 4: Register the three files in csproj**

After `<Compile Include="Data\SchoolInfoRepository.cs" />` add:

```xml
    <Compile Include="Data\IGradingSchemeRepository.cs" />
    <Compile Include="Data\GradingSchemeRepository.cs" />
```

After `<Compile Include="Models\SchoolInformation.cs" />` add:

```xml
    <Compile Include="Models\GradeBand.cs" />
```

- [ ] **Step 5: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Models/GradeBand.cs Data/IGradingSchemeRepository.cs Data/GradingSchemeRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(grading): GradeBand model + GradingSchemeRepository (table + seed)"
```

---

### Task 2: `Common/GradingScheme` cached accessor

**Files:**
- Create: `Common/GradingScheme.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Common/GradingScheme.cs`**

```csharp
using System;
using System.Collections.Generic;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Single source of truth for the grading scheme, read synchronously by the report-card
    /// generator. Loads once from the database and caches; any DB error / empty table falls
    /// back to the legacy 5-band scheme so report cards never break. Call Refresh() after a save.
    /// </summary>
    public static class GradingScheme
    {
        private static readonly object _lock = new object();
        private static List<GradeBand> _bands;

        private static void EnsureLoaded()
        {
            if (_bands != null) return;
            lock (_lock)
            {
                if (_bands != null) return;
                try
                {
                    var repo = new GradingSchemeRepository(AppConfig.ConnectionString);
                    repo.EnsureTablesAsyncSafe();
                    var bands = repo.GetBandsAsync().GetAwaiter().GetResult();
                    _bands = (bands != null && bands.Count > 0)
                        ? bands
                        : GradingSchemeRepository.LegacyBands();
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("GradingScheme load failed; using legacy bands", ex);
                    _bands = GradingSchemeRepository.LegacyBands();
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock) { _bands = null; }
        }

        /// <summary>Bands ordered high-&gt;low by MinScore.</summary>
        public static IReadOnlyList<GradeBand> Bands
        {
            get { EnsureLoaded(); return _bands; }
        }

        public static string CodeForScore(decimal score) => BandForScore(score)?.Code ?? "";
        public static string LabelForScore(decimal score) => BandForScore(score)?.Label ?? "";

        private static GradeBand BandForScore(decimal score)
        {
            EnsureLoaded();
            GradeBand match = null;
            foreach (var b in _bands)            // high -> low
            {
                if (score >= b.MinScore) { match = b; break; }
            }
            return match ?? (_bands.Count > 0 ? _bands[_bands.Count - 1] : null);
        }
    }
}
```

- [ ] **Step 2: Add the `EnsureTablesAsyncSafe` helper to the repository**

`GradingScheme` calls `EnsureTablesAsyncSafe()` so a missing DB never throws past the accessor's
own catch. Add this method to `Data/GradingSchemeRepository.cs` (just below `EnsureTableAsync`):

```csharp
        /// <summary>EnsureTableAsync but swallows errors (used by the fail-safe accessor).</summary>
        public void EnsureTablesAsyncSafe()
        {
            try { EnsureTableAsync().GetAwaiter().GetResult(); } catch { /* accessor falls back */ }
        }
```

- [ ] **Step 3: Register in csproj**

After `<Compile Include="Common\SchoolProfile.cs" />` add:

```xml
    <Compile Include="Common\GradingScheme.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Probe (reflection, needs LocalDB; defer if it won't start)**

Create `tmp_probe_grading.ps1`:

```powershell
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$bin = Join-Path $PSScriptRoot 'bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($s,$e)
  $n=(New-Object Reflection.AssemblyName($e.Name)).Name
  $p=Join-Path $bin "$n.dll"; if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null} })
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Common.GradingScheme')
"Code(82) = $($t.GetMethod('CodeForScore').Invoke($null,@([decimal]82)))"
"Label(82)= $($t.GetMethod('LabelForScore').Invoke($null,@([decimal]82)))"
"Code(60) = $($t.GetMethod('CodeForScore').Invoke($null,@([decimal]60)))"
"Label(60)= $($t.GetMethod('LabelForScore').Invoke($null,@([decimal]60)))"
```

Run: `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tmp_probe_grading.ps1`
Expected (with seeded/legacy bands):
```
Code(82) = 1
Label(82)= Advance(A)
Code(60) = 5
Label(60)= Beginning
```
If LocalDB will not start, the accessor's fail-safe still returns the legacy bands, so the same output is expected. Delete the script after: `Remove-Item tmp_probe_grading.ps1`.

- [ ] **Step 6: Commit**

```bash
git add Common/GradingScheme.cs Data/GradingSchemeRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(grading): GradingScheme fail-safe cached accessor"
```

---

### Task 3: Drive `ReportCardPDFGenerator` from the scheme

**Files:**
- Modify: `Services/ReportCardPDFGenerator.cs` (lines ~44-51, ~567-583)

- [ ] **Step 1: Replace the hardcoded `GradeLevels` array with a computed builder**

Replace (lines ~44-51):

```csharp
        private static readonly GradeLevel[] GradeLevels =
        {
            new GradeLevel("80+", "1", "Advance(A)"),
            new GradeLevel("75-79", "2", "Proficiency(P)"),
            new GradeLevel("70-74", "3", "Approaching\nProficiency(AP)"),
            new GradeLevel("65-69", "4", "Developing"),
            new GradeLevel("64% -", "5", "Beginning")
        };
```

with:

```csharp
        // Built from the configurable grading scheme. The displayed score range is derived
        // from consecutive MinScores: top band "{min}+", middle "{min}-{nextHigherMin-1}",
        // floor band "0-{nextHigherMin-1}".
        private static GradeLevel[] GradeLevels => BuildGradeLevels();

        private static GradeLevel[] BuildGradeLevels()
        {
            var bands = Common.GradingScheme.Bands; // high -> low
            var levels = new GradeLevel[bands.Count];
            for (int i = 0; i < bands.Count; i++)
            {
                int min = bands[i].MinScore;
                string range;
                if (i == 0) range = min + "+";
                else range = min + "-" + (bands[i - 1].MinScore - 1);
                levels[i] = new GradeLevel(range, bands[i].Code, bands[i].Label);
            }
            return levels;
        }
```

(All existing consumers — `GradeLevels.Length`, `GradeLevels[gradeIndex]`, `.ScoreRange/.Grade/.Remarks` — keep working unchanged.)

- [ ] **Step 2: Drive per-subject grade + remark from the scheme**

Replace `GetGradeForScore` (lines ~567-574):

```csharp
        private string GetGradeForScore(decimal score)
        {
            if (score >= 80) return "1";
            if (score >= 75) return "2";
            if (score >= 70) return "3";
            if (score >= 65) return "4";
            return "5";
        }
```

with:

```csharp
        private string GetGradeForScore(decimal score)
        {
            return Common.GradingScheme.CodeForScore(score);
        }
```

Replace `GetRemarkForScore` (lines ~576-583):

```csharp
        private string GetRemarkForScore(decimal score)
        {
            if (score >= 80) return "Advance";
            if (score >= 75) return "Proficiency";
            if (score >= 70) return "Approaching Proficiency";
            if (score >= 65) return "Developing";
            return "Beginning";
        }
```

with:

```csharp
        private string GetRemarkForScore(decimal score)
        {
            return Common.GradingScheme.LabelForScore(score);
        }
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Services/ReportCardPDFGenerator.cs
git commit -m "feat(grading): report card grade, remark, and legend read from configured scheme"
```

---

### Task 4: RBAC + `frmGradingScheme` settings form

**Files:**
- Create: `frmGradingScheme.cs`
- Modify: `Services/AuthService.cs`, `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Add RBAC entry**

In `Services/AuthService.cs`, after `["frmSchoolInfo"] = new[] { UserRole.Director, UserRole.Administrator },` add:

```csharp
            ["frmGradingScheme"]          = new[] { UserRole.Director, UserRole.Administrator },
```

- [ ] **Step 2: Create `frmGradingScheme.cs`**

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
    /// Grading scheme settings: edit the grade bands (Min %, Code, Label). Director/Administrator.
    /// Saving persists the bands and refreshes the GradingScheme cache.
    /// </summary>
    public class frmGradingScheme : Form
    {
        private readonly GradingSchemeRepository _repo = new GradingSchemeRepository(AppConfig.ConnectionString);
        private DataGridView _grid;
        private Button _addBtn, _removeBtn, _saveBtn, _cancelBtn;
        private Label _status;

        public frmGradingScheme()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmGradingScheme", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "Grading Scheme";
            Size = new Size(620, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowIcon = false;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Grading Scheme",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            var hint = new Label
            {
                Dock = DockStyle.Top, Height = 24,
                Text = "  Score >= Min % maps to this Code (grade) and Label (remark). Include one row with Min % = 0.",
                ForeColor = AppConfig.Colors.MutedTextColor, Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Color.White,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MinScore", HeaderText = "Min %", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Code", FillWeight = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label / Remark", FillWeight = 180 });

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 92, BackColor = AppConfig.Colors.PageBackColor, Padding = new Padding(12) };
            _addBtn = new Button { Text = "Add Row", Left = 12, Top = 8, Width = 100, Height = 32, FlatStyle = FlatStyle.Flat };
            _removeBtn = new Button { Text = "Remove Row", Left = 120, Top = 8, Width = 110, Height = 32, FlatStyle = FlatStyle.Flat };
            _saveBtn = new Button { Text = "Save", Left = 12, Top = 48, Width = 120, Height = 34, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cancelBtn = new Button { Text = "Cancel", Left = 140, Top = 48, Width = 100, Height = 34, FlatStyle = FlatStyle.Flat };
            _status = new Label { Left = 250, Top = 54, Width = 340, Height = 24, ForeColor = AppConfig.Colors.MutedTextColor };
            _addBtn.Click += (s, e) => _grid.Rows.Add("0", "", "");
            _removeBtn.Click += (s, e) => { if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow) _grid.Rows.Remove(_grid.CurrentRow); };
            _saveBtn.Click += async (s, e) => await SaveAsync();
            _cancelBtn.Click += (s, e) => Close();
            bar.Controls.Add(_addBtn); bar.Controls.Add(_removeBtn);
            bar.Controls.Add(_saveBtn); bar.Controls.Add(_cancelBtn); bar.Controls.Add(_status);

            Controls.Add(_grid);
            Controls.Add(bar);
            Controls.Add(hint);
            Controls.Add(title);
        }

        private async Task LoadAsync()
        {
            try
            {
                await _repo.EnsureTableAsync();
                var bands = await _repo.GetBandsAsync();
                if (bands.Count == 0) bands = GradingSchemeRepository.LegacyBands();
                _grid.Rows.Clear();
                foreach (var b in bands)
                    _grid.Rows.Add(b.MinScore.ToString(), b.Code, b.Label);
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load grading scheme: " + ex.Message, "Grading Scheme");
            }
        }

        private async Task SaveAsync()
        {
            var bands = new List<GradeBand>();
            var seenMins = new HashSet<int>();
            bool hasFloor = false;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                string minText = row.Cells["MinScore"].Value?.ToString();
                if (!int.TryParse(minText, out int min) || min < 0 || min > 100)
                { UIHelper.ShowWarning("Each Min % must be a whole number between 0 and 100.", "Grading Scheme"); return; }
                if (!seenMins.Add(min))
                { UIHelper.ShowWarning($"Duplicate Min % {min}. Each band needs a distinct Min %.", "Grading Scheme"); return; }
                if (min == 0) hasFloor = true;
                bands.Add(new GradeBand
                {
                    MinScore = min,
                    Code = row.Cells["Code"].Value?.ToString() ?? "",
                    Label = row.Cells["Label"].Value?.ToString() ?? ""
                });
            }
            if (bands.Count == 0)
            { UIHelper.ShowWarning("Add at least one grade band.", "Grading Scheme"); return; }
            if (!hasFloor)
            { UIHelper.ShowWarning("Include one catch-all band with Min % = 0.", "Grading Scheme"); return; }

            _saveBtn.Enabled = false;
            try
            {
                await _repo.SaveBandsAsync(bands.OrderByDescending(b => b.MinScore));
                Common.GradingScheme.Refresh();
                _status.Text = "Saved " + DateTime.Now.ToString("HH:mm:ss") + ".";
                UIHelper.ShowSuccess("Grading scheme saved.", "Grading Scheme");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save: " + ex.Message, "Grading Scheme");
            }
            finally { _saveBtn.Enabled = true; }
        }
    }
}
```

- [ ] **Step 3: Register in csproj**

After `<Compile Include="frmSchoolInfo.cs" />` add:

```xml
    <Compile Include="frmGradingScheme.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Render-verify (offline harness)**

Render `frmGradingScheme` (set `AuthService.CurrentUser` backing field to `UserSession { Role = Director }`, off-screen `Show()`, `DrawToBitmap`). Confirm: title, hint, the bands grid (Min %, Code, Label), Add/Remove/Save/Cancel. If LocalDB is down the grid may be empty (Load fails) — layout still renders; acceptable.

- [ ] **Step 6: Commit**

```bash
git add frmGradingScheme.cs Services/AuthService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(grading): frmGradingScheme settings form + RBAC"
```

---

### Task 5: Dashboard nav entry

**Files:**
- Modify: `frmDashboard.cs`

- [ ] **Step 1: Add the nav button**

In `frmDashboard.cs`, in the Director/Administrator settings block, after the
`nav.Controls.Add(CreateNavButton("School Information", () => new frmSchoolInfo().ShowDialog()));`
line add:

```csharp
                nav.Controls.Add(CreateNavButton("Grading Scheme", () => new frmGradingScheme().ShowDialog()));
```

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(grading): add Grading Scheme to dashboard settings nav"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app):**
  1. Director/Admin → dashboard → **Grading Scheme**: the 5 bands load (80/75/70/65/0).
  2. Change a band (e.g. set the top band's Min % to 85) + Save.
  3. Generate a report card → grades/remarks/legend reflect the new threshold (a 82 now grades as the second band).
  4. Reopen Grading Scheme → the edit persists. Re-running does not duplicate rows.
  5. Validation: blank/duplicate Min %, no 0-floor band → blocked with a message.

## Self-review notes

- **Spec coverage:** table + seed (Task 1) ✓; GradeBand model (Task 1) ✓; repository (Task 1) ✓; fail-safe cached accessor (Task 2) ✓; report-card grade/remark/legend from scheme (Task 3) ✓; form + validation (Task 4) ✓; RBAC (Task 4) ✓; dashboard nav (Task 5) ✓.
- **Deviation (documented):** spec said unify *both* generators; `ReportCardPdfService`'s legend path is unreachable dead code (`useOfficialTemplate` always true → delegates to `ReportCardPDFGenerator`), so it is intentionally left untouched. If desired, deleting that dead draw path is a separate cleanup.
- **Type/name consistency:** `GradeBand {MinScore,Code,Label}`; repo `EnsureTableAsync/GetBandsAsync/SaveBandsAsync/LegacyBands/EnsureTablesAsyncSafe`; accessor `Bands/CodeForScore/LabelForScore/Refresh` — all consistent across tasks and matched by the form and generator call sites.
- **No placeholders:** every code step has complete code; the probe step has exact expected output.
