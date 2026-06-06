# Custom Subjects Per Class Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the exam-entry subject list configurable per class (admin-editable), instead of one hardcoded list for all classes.

**Architecture:** A `ClassSubjects` table + `SubjectRepository` + a fail-safe cached `Common/SubjectCatalog` accessor (same pattern as Grading Scheme). `EXAMS.cs` rebuilds its subject grid from the looked-up student's class subjects. A `frmSubjects` editor manages each class's list. Report cards are unchanged (built from each student's `examss` rows).

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-custom-subjects-per-class-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) reflection probe for pure logic; (3) render harness / user run for forms.
- **Explicit-include csproj:** every new `.cs` MUST get a `<Compile Include="..." />` entry (code-only forms self-closing). `frmSchoolInfo.cs` / `frmGradingScheme.cs` entries are multi-line with `<SubType>Form</SubType>`; anchor new entries on self-closing lines like `frmPaymentHistory.cs`.
- `AppConfig.ClassNames` is the `string[]` of 14 classes; `AppConfig.ConnectionString`, `AppConfig.Colors`, `UIHelper`, `AuthService.RequireAccess`, `LoggerHelper` all exist.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Create** `Data/ISubjectRepository.cs`, `Data/SubjectRepository.cs`
- **Create** `Common/SubjectCatalog.cs`
- **Create** `frmSubjects.cs`
- **Modify** `EXAMS.cs`, `Services/AuthService.cs`, `frmDashboard.cs`, `kingdom_Preparatory_School_Management_System.csproj`

---

### Task 1: `SubjectRepository` (table + seed + CRUD)

**Files:**
- Create: `Data/ISubjectRepository.cs`, `Data/SubjectRepository.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Data/ISubjectRepository.cs`**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ISubjectRepository
    {
        Task EnsureTableAsync();
        Task<List<string>> GetSubjectsForClassAsync(string className);
        Task<Dictionary<string, List<string>>> GetAllAsync();
        Task SaveSubjectsForClassAsync(string className, IEnumerable<string> subjects);
    }
}
```

- [ ] **Step 2: Create `Data/SubjectRepository.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Per-class subject lists. Created and seeded (every class gets the legacy 9 subjects) on
    /// first use. SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class SubjectRepository : ISubjectRepository
    {
        private readonly string _connectionString;

        public SubjectRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string create = @"IF OBJECT_ID(N'ClassSubjects', N'U') IS NULL
                    CREATE TABLE ClassSubjects (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        ClassName NVARCHAR(50) NOT NULL,
                        Subject NVARCHAR(80) NOT NULL,
                        SortOrder INT NOT NULL DEFAULT (0));";
                using (var cmd = new OleDbCommand(create, c)) await cmd.ExecuteNonQueryAsync();

                // Seed each class that has no rows yet (idempotent + respects admin edits).
                foreach (var className in AppConfig.ClassNames)
                {
                    bool has;
                    using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM ClassSubjects WHERE ClassName = ?", c))
                    {
                        cmd.Parameters.AddWithValue("?", className);
                        has = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (has) continue;
                    for (int i = 0; i < LegacySubjects.Length; i++)
                    {
                        using (var cmd = new OleDbCommand(
                            "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", className);
                            cmd.Parameters.AddWithValue("?", LegacySubjects[i]);
                            cmd.Parameters.AddWithValue("?", i);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        /// <summary>EnsureTableAsync but swallows errors (used by the fail-safe accessor).</summary>
        public void EnsureTableAsyncSafe()
        {
            try { EnsureTableAsync().GetAwaiter().GetResult(); } catch { /* accessor falls back */ }
        }

        public async Task<List<string>> GetSubjectsForClassAsync(string className)
        {
            var list = new List<string>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT Subject FROM ClassSubjects WHERE ClassName = ? ORDER BY SortOrder", c))
                {
                    cmd.Parameters.AddWithValue("?", className ?? "");
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync())
                            list.Add(r["Subject"]?.ToString() ?? "");
                }
            }
            return list;
        }

        public async Task<Dictionary<string, List<string>>> GetAllAsync()
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand(
                    "SELECT ClassName, Subject FROM ClassSubjects ORDER BY ClassName, SortOrder", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                    {
                        string cls = r["ClassName"]?.ToString() ?? "";
                        if (!map.TryGetValue(cls, out var list)) { list = new List<string>(); map[cls] = list; }
                        list.Add(r["Subject"]?.ToString() ?? "");
                    }
            }
            return map;
        }

        public async Task SaveSubjectsForClassAsync(string className, IEnumerable<string> subjects)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var del = new OleDbCommand("DELETE FROM ClassSubjects WHERE ClassName = ?", c))
                {
                    del.Parameters.AddWithValue("?", className ?? "");
                    await del.ExecuteNonQueryAsync();
                }
                int order = 0;
                foreach (var subject in subjects)
                {
                    using (var ins = new OleDbCommand(
                        "INSERT INTO ClassSubjects (ClassName, Subject, SortOrder) VALUES (?, ?, ?)", c))
                    {
                        ins.Parameters.AddWithValue("?", className ?? "");
                        ins.Parameters.AddWithValue("?", subject ?? "");
                        ins.Parameters.AddWithValue("?", order++);
                        await ins.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>The legacy hardcoded subjects — used to seed and as the fail-safe default.</summary>
        public static readonly string[] LegacySubjects =
        {
            "MATHEMATICS", "INT. SCIENCE", "ENGLISH LANGUAGE", "SOCIAL STUDIES",
            "COMPUTING", "REL. & MORAL EDU.", "CARRER TECH.", "CREATIVE ART", "GHANAIAN LANG."
        };
    }
}
```

- [ ] **Step 3: Register in csproj**

After `<Compile Include="Data\GradingSchemeRepository.cs" />` add:

```xml
    <Compile Include="Data\ISubjectRepository.cs" />
    <Compile Include="Data\SubjectRepository.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Data/ISubjectRepository.cs Data/SubjectRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(subjects): ClassSubjects table + SubjectRepository (seed legacy 9 per class)"
```

---

### Task 2: `Common/SubjectCatalog` cached accessor

**Files:**
- Create: `Common/SubjectCatalog.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Common/SubjectCatalog.cs`**

```csharp
using System;
using System.Collections.Generic;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Single source of truth for per-class subjects, read synchronously by the exam-entry screen.
    /// Loads once from the database and caches; any DB error / unconfigured class falls back to the
    /// legacy 9 subjects so exam entry never breaks. Call Refresh() after a save.
    /// </summary>
    public static class SubjectCatalog
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, List<string>> _byClass;

        public static string[] LegacySubjects => SubjectRepository.LegacySubjects;

        private static void EnsureLoaded()
        {
            if (_byClass != null) return;
            lock (_lock)
            {
                if (_byClass != null) return;
                try
                {
                    var repo = new SubjectRepository(AppConfig.ConnectionString);
                    repo.EnsureTableAsyncSafe();
                    _byClass = repo.GetAllAsync().GetAwaiter().GetResult()
                               ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("SubjectCatalog load failed; using legacy subjects", ex);
                    _byClass = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock) { _byClass = null; }
        }

        /// <summary>The class's ordered subjects; falls back to the legacy 9 when unconfigured.</summary>
        public static IReadOnlyList<string> SubjectsForClass(string className)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(className) &&
                _byClass.TryGetValue(className.Trim(), out var list) && list.Count > 0)
                return list;
            return LegacySubjects;
        }
    }
}
```

- [ ] **Step 2: Register in csproj**

After `<Compile Include="Common\GradingScheme.cs" />` add:

```xml
    <Compile Include="Common\SubjectCatalog.cs" />
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Common/SubjectCatalog.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(subjects): SubjectCatalog fail-safe cached accessor"
```

---

### Task 3: `EXAMS.cs` builds its grid per class

**Files:**
- Modify: `EXAMS.cs` (lines ~45-49, ~301-310, ~595 in LookupStudent)

- [ ] **Step 1: Replace the hardcoded `subjects` field**

Replace (lines ~45-49):

```csharp
        private readonly string[] subjects =
        {
            "MATHEMATICS", "INT. SCIENCE", "ENGLISH LANGUAGE", "SOCIAL STUDIES",
            "COMPUTING", "REL. & MORAL EDU.", "CARRER TECH.", "CREATIVE ART", "GHANAIAN LANG."
        };
```

with:

```csharp
        // Current subjects shown in the grid; defaults to the legacy list, replaced per class on lookup.
        private string[] subjects = Common.SubjectCatalog.LegacySubjects;
```

- [ ] **Step 2: Extract grid population into `RebuildSubjectGrid`**

In `BuildSubjectEntryPanel`, replace the inline row-build block (lines ~301-310):

```csharp
            subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            for (int i = 0; i < subjects.Length; i++)
                subjectGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / subjects.Length));

            string[] headers = { "Subject", "Test (40)", "Group (10)", "Project (10)", "Exam (100)", "Total", "Grade", "Remark" };
            for (int i = 0; i < headers.Length; i++)
                subjectGrid.Controls.Add(MakeGridHeader(headers[i]), i, 0);

            for (int i = 0; i < subjects.Length; i++)
                AddSubjectRow(subjects[i], i + 1);
```

with:

```csharp
            RebuildSubjectGrid(subjects);
```

Then add this method (e.g. directly after `BuildSubjectEntryPanel`):

```csharp
        private void RebuildSubjectGrid(IReadOnlyList<string> subjectList)
        {
            subjects = new List<string>(subjectList).ToArray();

            subjectGrid.SuspendLayout();
            subjectGrid.Controls.Clear();
            subjectGrid.RowStyles.Clear();
            subjectRows.Clear();

            subjectGrid.RowCount = subjects.Length + 1;
            subjectGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            for (int i = 0; i < subjects.Length; i++)
                subjectGrid.RowStyles.Add(new RowStyle(SizeType.Percent,
                    subjects.Length == 0 ? 100F : 100F / subjects.Length));

            string[] headers = { "Subject", "Test (40)", "Group (10)", "Project (10)", "Exam (100)", "Total", "Grade", "Remark" };
            for (int i = 0; i < headers.Length; i++)
                subjectGrid.Controls.Add(MakeGridHeader(headers[i]), i, 0);

            for (int i = 0; i < subjects.Length; i++)
                AddSubjectRow(subjects[i], i + 1);

            if (completionLabel != null)
                completionLabel.Text = "0 of " + subjects.Length + " subjects";

            subjectGrid.ResumeLayout(true);
        }
```

(The `ColumnStyles` set in `BuildSubjectEntryPanel` are not cleared by `RebuildSubjectGrid`, so the
8 columns persist. `System.Collections.Generic` is already used in this file.)

- [ ] **Step 3: Rebuild the grid for the student's class on lookup**

In `LookupStudent`, after `classBox.Text = student.ClassID;` (the success branch) and BEFORE
`await LoadExistingResults(sid);`, add:

```csharp
                    RebuildSubjectGrid(Common.SubjectCatalog.SubjectsForClass(student.ClassID));
```

(So the grid matches the class before existing scores are loaded into `subjectRows`.)

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If `completionLabel` is referenced before assignment elsewhere, the null-guard
in `RebuildSubjectGrid` covers the construction-time call.)

- [ ] **Step 5: Commit**

```bash
git add EXAMS.cs
git commit -m "feat(subjects): exam entry builds subject grid from the student's class subjects"
```

---

### Task 4: RBAC + `frmSubjects` editor

**Files:**
- Create: `frmSubjects.cs`
- Modify: `Services/AuthService.cs`, `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Add RBAC entry**

In `Services/AuthService.cs`, after `["frmGradingScheme"] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },` add:

```csharp
            ["frmSubjects"]               = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
```

- [ ] **Step 2: Create `frmSubjects.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Per-class subject editor: pick a class, edit/reorder its subjects, Save. Director/Admin/Headmaster.
    /// Persists to ClassSubjects and refreshes the SubjectCatalog cache.
    /// </summary>
    public class frmSubjects : Form
    {
        private readonly SubjectRepository _repo = new SubjectRepository(AppConfig.ConnectionString);
        private ListBox _classList, _subjectList;
        private TextBox _newSubject;
        private Button _addBtn, _removeBtn, _upBtn, _downBtn, _saveBtn, _cancelBtn;
        private Label _status;
        private string _currentClass;
        private bool _dirty;

        public frmSubjects()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmSubjects", this)) return;
            Load += async (s, e) => await InitAsync();
        }

        private void BuildUi()
        {
            Text = "Subjects";
            Size = new Size(720, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Subjects (per class)",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            _classList = new ListBox { Left = 16, Top = 60, Width = 200, Height = 400, Font = new Font("Segoe UI", 10F) };
            _classList.SelectedIndexChanged += async (s, e) => await OnClassChangedAsync();

            _subjectList = new ListBox { Left = 232, Top = 60, Width = 320, Height = 360, Font = new Font("Segoe UI", 10F) };

            _newSubject = new TextBox { Left = 232, Top = 428, Width = 200, Font = new Font("Segoe UI", 10F) };
            _addBtn = new Button { Left = 440, Top = 426, Width = 112, Height = 28, Text = "Add", FlatStyle = FlatStyle.Flat };
            _upBtn = new Button { Left = 564, Top = 60, Width = 120, Height = 30, Text = "Move Up", FlatStyle = FlatStyle.Flat };
            _downBtn = new Button { Left = 564, Top = 96, Width = 120, Height = 30, Text = "Move Down", FlatStyle = FlatStyle.Flat };
            _removeBtn = new Button { Left = 564, Top = 140, Width = 120, Height = 30, Text = "Remove", FlatStyle = FlatStyle.Flat };
            _saveBtn = new Button { Left = 232, Top = 470, Width = 130, Height = 36, Text = "Save Class", BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cancelBtn = new Button { Left = 372, Top = 470, Width = 100, Height = 36, Text = "Close", FlatStyle = FlatStyle.Flat };
            _status = new Label { Left = 16, Top = 470, Width = 200, Height = 36, ForeColor = AppConfig.Colors.MutedTextColor };

            _addBtn.Click += (s, e) => AddSubject();
            _removeBtn.Click += (s, e) => { if (_subjectList.SelectedIndex >= 0) { _subjectList.Items.RemoveAt(_subjectList.SelectedIndex); _dirty = true; } };
            _upBtn.Click += (s, e) => Move(-1);
            _downBtn.Click += (s, e) => Move(1);
            _saveBtn.Click += async (s, e) => await SaveAsync();
            _cancelBtn.Click += (s, e) => Close();

            Controls.Add(_classList); Controls.Add(_subjectList); Controls.Add(_newSubject);
            Controls.Add(_addBtn); Controls.Add(_upBtn); Controls.Add(_downBtn); Controls.Add(_removeBtn);
            Controls.Add(_saveBtn); Controls.Add(_cancelBtn); Controls.Add(_status);
            Controls.Add(title);
        }

        private async Task InitAsync()
        {
            try
            {
                await _repo.EnsureTableAsync();
                _classList.Items.Clear();
                foreach (var cls in AppConfig.ClassNames) _classList.Items.Add(cls);
                if (_classList.Items.Count > 0) _classList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load subjects: " + ex.Message, "Subjects");
            }
        }

        private async Task OnClassChangedAsync()
        {
            if (_classList.SelectedItem == null) return;
            string next = _classList.SelectedItem.ToString();
            if (_dirty && _currentClass != null && next != _currentClass)
            {
                if (UIHelper.ShowConfirmation($"Discard unsaved changes to {_currentClass}?", "Subjects") != DialogResult.Yes)
                {
                    _classList.SelectedItem = _currentClass; // revert selection
                    return;
                }
            }
            _currentClass = next;
            _dirty = false;
            try
            {
                var subs = await _repo.GetSubjectsForClassAsync(_currentClass);
                if (subs.Count == 0) subs = new List<string>(SubjectCatalog.LegacySubjects);
                _subjectList.Items.Clear();
                foreach (var s in subs) _subjectList.Items.Add(s);
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load class subjects: " + ex.Message, "Subjects");
            }
        }

        private void AddSubject()
        {
            string s = (_newSubject.Text ?? "").Trim();
            if (s.Length == 0) return;
            if (_subjectList.Items.Cast<object>().Any(x => string.Equals(x.ToString(), s, StringComparison.OrdinalIgnoreCase)))
            { UIHelper.ShowWarning("That subject is already in the list.", "Subjects"); return; }
            _subjectList.Items.Add(s);
            _newSubject.Text = "";
            _dirty = true;
        }

        private void Move(int delta)
        {
            int i = _subjectList.SelectedIndex;
            if (i < 0) return;
            int j = i + delta;
            if (j < 0 || j >= _subjectList.Items.Count) return;
            var item = _subjectList.Items[i];
            _subjectList.Items.RemoveAt(i);
            _subjectList.Items.Insert(j, item);
            _subjectList.SelectedIndex = j;
            _dirty = true;
        }

        private async Task SaveAsync()
        {
            if (_currentClass == null) return;
            var subs = _subjectList.Items.Cast<object>().Select(x => x.ToString().Trim())
                                   .Where(x => x.Length > 0).ToList();
            if (subs.Count == 0)
            { UIHelper.ShowWarning("Add at least one subject before saving.", "Subjects"); return; }

            _saveBtn.Enabled = false;
            try
            {
                await _repo.SaveSubjectsForClassAsync(_currentClass, subs);
                SubjectCatalog.Refresh();
                _dirty = false;
                _status.Text = "Saved " + DateTime.Now.ToString("HH:mm:ss") + ".";
                UIHelper.ShowSuccess($"Subjects saved for {_currentClass}.", "Subjects");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save: " + ex.Message, "Subjects");
            }
            finally { _saveBtn.Enabled = true; }
        }
    }
}
```

- [ ] **Step 3: Register in csproj**

After the self-closing `<Compile Include="frmGradingScheme.cs" />` add:

```xml
    <Compile Include="frmSubjects.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Render-verify (offline harness)**

Render `frmSubjects` (Director). Confirm: title, class list (left), subject list (middle), Add box +
Move Up/Down/Remove, Save Class/Close. (DB down → lists empty; layout still renders.)

- [ ] **Step 6: Commit**

```bash
git add frmSubjects.cs Services/AuthService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(subjects): frmSubjects per-class editor + RBAC"
```

---

### Task 5: Dashboard nav entry

**Files:**
- Modify: `frmDashboard.cs`

- [ ] **Step 1: Add the nav button**

In `frmDashboard.cs`, in the Director/Admin/Headmaster settings block, after the
`nav.Controls.Add(CreateNavButton("Grading Scheme", () => new frmGradingScheme().ShowDialog()));`
line add:

```csharp
                nav.Controls.Add(CreateNavButton("Subjects", () => new frmSubjects().ShowDialog()));
```

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(subjects): add Subjects to dashboard settings nav"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app):**
  1. Director/Admin/Headmaster → dashboard → **Subjects**: classes listed; selecting a class shows its 9 seeded subjects.
  2. For CRECHE, remove a subject + add one + reorder → **Save Class**.
  3. Open **Exams**, look up a CRECHE student → the grid shows CRECHE's edited subjects; a BASIC student still shows its own list.
  4. Enter + save scores → report card for that student shows those subjects.
  5. Re-running (reopen) doesn't duplicate or re-seed an edited class.

## Self-review notes

- **Spec coverage:** ClassSubjects table + per-class seed (Task 1) ✓; repository (Task 1) ✓; fail-safe
  cached accessor (Task 2) ✓; EXAMS builds grid per class (Task 3) ✓; frmSubjects editor + reorder +
  validation (Task 4) ✓; RBAC Director/Admin/Headmaster (Task 4) ✓; dashboard nav (Task 5) ✓; report
  card unchanged ✓.
- **Type/name consistency:** `SubjectRepository` (`EnsureTableAsync/EnsureTableAsyncSafe/
  GetSubjectsForClassAsync/GetAllAsync/SaveSubjectsForClassAsync/LegacySubjects`), `SubjectCatalog`
  (`SubjectsForClass/Refresh/LegacySubjects`), `EXAMS.RebuildSubjectGrid(IReadOnlyList<string>)`,
  `frmSubjects` — consistent across tasks.
- **No placeholders:** every step shows complete code; the one EXAMS conditional (completionLabel
  null-guard) is handled.
- **Scope:** report card + `ReportCardPdfService` untouched (the latter's draw path is dead, per the
  grading-scheme finding).
