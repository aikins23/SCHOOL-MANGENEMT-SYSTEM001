# Universal "KPS+ID" Student ID Display — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the student ID as `{abbrev}{number}` (e.g. `KPS9016`) everywhere, using the configurable school abbreviation, while keeping the database and all readbacks numeric; lookups accept `KPS9016` or `9016`.

**Architecture:** A central `Common/StudentId` helper (`Display`, `Parse`, `AttachGridFormatting`). Grids are formatted display-only (underlying value stays numeric → existing `row["ID"]` readbacks and numeric sort/filter unaffected). Labels/receipts/report-cards/SMS wrap with `Display`. Lookups parse with `Parse`. The two hardcoded SQL `'KPS'` aliases are removed so the prefix is single-sourced.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-student-id-kps-prefix-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) a PowerShell reflection probe for the pure helper; (3) render harness / user run for visual confirmation.
- **Explicit-include csproj:** the new `.cs` file MUST get a `<Compile Include="..." />` entry.
- **Display-only for grids:** never mutate the bound `DataTable`; use `CellFormatting` so `.Value` stays numeric.
- `AppConfig.Sms.SchoolAbbreviation` returns the configurable abbreviation (default "KPS", upper-cased).
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Create** `Common/StudentId.cs` — the helper.
- **Modify** `kingdom_Preparatory_School_Management_System.csproj` — register it.
- **Modify** `Data/StudentRepository.cs` — drop the two hardcoded `'KPS'` aliases.
- **Modify** grids: `frmStdView.cs`, `frmPaymentHistory.cs`, `frmOutstandingFees.cs`, `frmAttendance.cs`, `frmDashboard.cs`.
- **Modify** text/inputs: `frmStdDetails.cs`, `frmFessPayment.cs`.
- **Modify** report card + SMS: `Services/ReportCardPDFGenerator.cs`, `Services/SmsService.cs`.

---

### Task 1: `Common/StudentId` helper

**Files:**
- Create: `Common/StudentId.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Common/StudentId.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Formats the student ID for display as "{abbrev}{number}" (e.g. KPS9016) and parses
    /// that form back to the numeric string used for database queries. Display-only — the
    /// stored ID stays numeric everywhere. The abbreviation is the configurable school
    /// abbreviation (AppConfig.Sms.SchoolAbbreviation).
    /// </summary>
    public static class StudentId
    {
        public static string Abbrev => AppConfig.Sms.SchoolAbbreviation;

        /// <summary>"9016" -> "KPS9016". Null/blank -> "". Idempotent (won't double-prefix).</summary>
        public static string Display(object id)
        {
            if (id == null) return "";
            string s = id.ToString().Trim();
            if (s.Length == 0) return "";
            string ab = Abbrev;
            if (!string.IsNullOrEmpty(ab) && s.StartsWith(ab, StringComparison.OrdinalIgnoreCase))
                return s;
            return ab + s;
        }

        /// <summary>"KPS9016"/" kps9016 "/"9016" -> "9016". Strips a leading abbreviation.</summary>
        public static string Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string s = input.Trim();
            string ab = Abbrev;
            if (!string.IsNullOrEmpty(ab) && s.StartsWith(ab, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(ab.Length);
            return s.Trim();
        }

        /// <summary>
        /// Prefixes the named grid columns' DISPLAYED text via CellFormatting. The underlying
        /// cell value is untouched, so readbacks (row["ID"]) and numeric sort/filter still work.
        /// </summary>
        public static void AttachGridFormatting(DataGridView grid, params string[] idColumns)
        {
            if (grid == null || idColumns == null || idColumns.Length == 0) return;
            var cols = new HashSet<string>(idColumns, StringComparer.OrdinalIgnoreCase);
            grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.Value == null || e.Value == DBNull.Value) return;
                var col = grid.Columns[e.ColumnIndex];
                if (cols.Contains(col.Name) || cols.Contains(col.HeaderText))
                {
                    e.Value = Display(e.Value);
                    e.FormattingApplied = true;
                }
            };
        }
    }
}
```

- [ ] **Step 2: Register in csproj**

After `<Compile Include="Common\SchoolProfile.cs" />` add:

```xml
    <Compile Include="Common\StudentId.cs" />
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 4: Probe the helper (reflection)**

Create `tmp_probe_studentid.ps1`:

```powershell
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$bin = Join-Path $PSScriptRoot 'bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($s,$e)
  $n=(New-Object Reflection.AssemblyName($e.Name)).Name
  $p=Join-Path $bin "$n.dll"; if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null} })
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Common.StudentId')
$d=$t.GetMethod('Display'); $p=$t.GetMethod('Parse')
"Display(9016)   = $($d.Invoke($null,@([object]'9016')))"
"Display(KPS9016)= $($d.Invoke($null,@([object]'KPS9016')))"
"Parse(KPS9016)  = $($p.Invoke($null,@(' kps9016 ')))"
"Parse(9016)     = $($p.Invoke($null,@('9016')))"
```

Run: `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tmp_probe_studentid.ps1`
Expected:
```
Display(9016)   = KPS9016
Display(KPS9016)= KPS9016
Parse(KPS9016)  = 9016
Parse(9016)     = 9016
```
(`AppConfig.Sms.SchoolAbbreviation` reads user settings; in a fresh probe it returns the default "KPS". This probe does not need LocalDB. Delete the script after: `Remove-Item tmp_probe_studentid.ps1`.)

- [ ] **Step 5: Commit**

```bash
git add Common/StudentId.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(student-id): StudentId helper (Display/Parse/AttachGridFormatting)"
```

---

### Task 2: Remove the hardcoded SQL `'KPS'` aliases

**Files:**
- Modify: `Data/StudentRepository.cs` (two occurrences, ~lines 280 and 422)

- [ ] **Step 1: Replace both aliases**

There are two identical lines:

```csharp
                            'KPS' + CAST(StudentID AS VARCHAR) AS [STUDENT ID],
```

Replace BOTH with:

```csharp
                            StudentID AS [STUDENT ID],
```

Use Edit with `replace_all: true` on that exact line. The `[STUDENT ID]` column is now numeric; `frmStdView` will format it (Task 3).

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Data/StudentRepository.cs
git commit -m "refactor(student-id): drop hardcoded SQL 'KPS' prefix (single-sourced in helper)"
```

---

### Task 3: Grids — display-only prefix

**Files:**
- Modify: `frmStdView.cs` (~line 291), `frmPaymentHistory.cs` (~line 96), `frmOutstandingFees.cs` (~line 424 + CSV export ~499), `frmAttendance.cs` (~lines 448, 545), `frmDashboard.cs` (~line 1088)

For each grid, call `AttachGridFormatting` ONCE right after the grid is created (so the handler is attached before/at binding). Attaching at creation is safest; attaching after `DataSource =` also works since `CellFormatting` fires on paint.

- [ ] **Step 1: `frmStdView` — format `STUDENT ID`**

After `studentsGrid.DataSource = table;` and the `ConfigureGridColumns();` call (around line 291-292), add:

```csharp
                Common.StudentId.AttachGridFormatting(studentsGrid, "STUDENT ID");
```

(Attaching once per bind is fine — `CellFormatting` is idempotent for display. If the grid rebinds on filter, the single handler persists; do NOT add it inside a loop.)

To avoid stacking handlers on repeated loads, attach it once where the grid is constructed instead. Find `studentsGrid = new DataGridView` (around line 210) and immediately after the initializer block add:

```csharp
            Common.StudentId.AttachGridFormatting(studentsGrid, "STUDENT ID");
```

Use the construction-site attachment (remove the post-bind option above if you added it).

- [ ] **Step 2: `frmPaymentHistory` — format `STUDENT ID`**

In `BuildUi`, right after `_grid.CellFormatting += Grid_CellFormatting;` add:

```csharp
            Common.StudentId.AttachGridFormatting(_grid, "STUDENT ID");
```

- [ ] **Step 3: `frmOutstandingFees` — format `ID` + CSV export uses Display**

Find where `feesGrid` is constructed and attach after the initializer:

```csharp
            Common.StudentId.AttachGridFormatting(feesGrid, "ID");
```

Then in the CSV export (around line 499) replace:

```csharp
                        sw.WriteLine($"{row["ID"]},{row["Student Name"]},{row["Class"]},{row["Balance Owed"]},{row["Last Payment"]}");
```

with:

```csharp
                        sw.WriteLine($"{Common.StudentId.Display(row["ID"])},{row["Student Name"]},{row["Class"]},{row["Balance Owed"]},{row["Last Payment"]}");
```

- [ ] **Step 4: `frmAttendance` — format `ID`**

Find where `dataGrid` is constructed (the attendance grid) and attach after the initializer:

```csharp
            Common.StudentId.AttachGridFormatting(dataGrid, "ID");
```

(The attendance readback `ReferenceID = row.Cells["ID"].Value.ToString()` stays numeric because formatting is display-only — do NOT change it.)

- [ ] **Step 5: `frmDashboard` — Recent Payments `ID`**

Find where `recentPaymentsGrid = CreateAnalyticsGrid();` is (around line 516 region) and immediately after it add:

```csharp
            Common.StudentId.AttachGridFormatting(recentPaymentsGrid, "ID");
```

(The fee-reminder readback `row["ID"]` around line 1203 stays numeric — do NOT change it.)

- [ ] **Step 6: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add frmStdView.cs frmPaymentHistory.cs frmOutstandingFees.cs frmAttendance.cs frmDashboard.cs
git commit -m "feat(student-id): prefix student-ID columns in all grids (display-only)"
```

---

### Task 4: `frmStdDetails` — display + parse

**Files:**
- Modify: `frmStdDetails.cs` (lines ~344, ~391, ~460)

`txtStdID` is both a display field and the source of DB operations, so it must show the prefixed value but feed numeric values to the data layer.

- [ ] **Step 1: Show prefixed on load**

Replace (line ~344):

```csharp
            txtStdID.Text = row["ID"].ToString();
```

with:

```csharp
            txtStdID.Text = Common.StudentId.Display(row["ID"]);
```

- [ ] **Step 2: Parse when updating**

Replace (line ~391):

```csharp
                StudentID = txtStdID.Text,
```

with:

```csharp
                StudentID = Common.StudentId.Parse(txtStdID.Text),
```

- [ ] **Step 3: Parse when rolling out**

Replace (line ~460):

```csharp
                var (success, message) = await _studentService.RollOutStudentAsync(txtStdID.Text);
```

with:

```csharp
                var (success, message) = await _studentService.RollOutStudentAsync(Common.StudentId.Parse(txtStdID.Text));
```

(The confirmation/log strings that interpolate `txtStdID.Text` — e.g. `ID: {txtStdID.Text}` — are display only; leave them, they now correctly show `KPS9016`.)

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add frmStdDetails.cs
git commit -m "feat(student-id): student details shows KPS+ID, parses on save/roll-out"
```

---

### Task 5: `frmFessPayment` — lookup parse, payment parse, receipt display

**Files:**
- Modify: `frmFessPayment.cs` (lines ~2449, ~2567, ~1548, ~2560)

- [ ] **Step 1: Parse the lookup input**

In `LookupStudent` (line ~2449) replace:

```csharp
                string studentId = studentIdBox.Text.Trim();
```

with:

```csharp
                string studentId = Common.StudentId.Parse(studentIdBox.Text);
```

(So typing `KPS9016` or `9016` both resolve; `GetStudentAsync(studentId)` on the next line gets the numeric string.)

- [ ] **Step 2: Parse the payment request student ID**

In `RecordPayment` (line ~2567) replace:

```csharp
                    StudentId = studentIdBox.Text,
```

with:

```csharp
                    StudentId = Common.StudentId.Parse(studentIdBox.Text),
```

- [ ] **Step 3: Receipt tile shows prefixed ID**

In the receipt-refresh method (line ~1548) replace:

```csharp
            _rpStudentIdLbl.Text   = studentIdBox?.Text.Trim() ?? "";
```

with:

```csharp
            _rpStudentIdLbl.Text   = Common.StudentId.Display(Common.StudentId.Parse(studentIdBox?.Text ?? ""));
```

(Parse-then-Display normalizes whether the user typed `9016` or `KPS9016` → always `KPS9016`.)

- [ ] **Step 4: Confirmation text shows prefixed ID**

In `RecordPayment` (line ~2560) replace:

```csharp
                string changeDescription = $"Record payment of GHS {amountPaid:N2} for {studentNameBox.Text} (ID: {studentIdBox.Text.Trim()})?";
```

with:

```csharp
                string changeDescription = $"Record payment of GHS {amountPaid:N2} for {studentNameBox.Text} (ID: {Common.StudentId.Display(Common.StudentId.Parse(studentIdBox.Text))})?";
```

- [ ] **Step 5: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add frmFessPayment.cs
git commit -m "feat(student-id): fee payment accepts KPS/numeric, receipt shows KPS+ID"
```

---

### Task 6: Report card + SMS display

**Files:**
- Modify: `Services/ReportCardPDFGenerator.cs` (line ~160), `Services/SmsService.cs` (lines ~76, ~97)

- [ ] **Step 1: Report card admission number**

In `ReportCardPDFGenerator.cs` (line ~160) replace:

```csharp
            yield return new InfoRow("Admission No.:", data?.StudentID ?? "", "Attendance:", "",
```

with:

```csharp
            yield return new InfoRow("Admission No.:", Common.StudentId.Display(data?.StudentID), "Attendance:", "",
```

(`Common.StudentId.Display` returns "" for null, so this also covers the `?? ""` case.)

- [ ] **Step 2: SMS bodies show prefixed ID**

In `Services/SmsService.cs` there are two identical lines (~76 and ~97):

```csharp
- Student ID: {s.StudentID}
```

Replace BOTH (Edit with `replace_all: true`) with:

```csharp
- Student ID: {Common.StudentId.Display(s.StudentID)}
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Services/ReportCardPDFGenerator.cs Services/SmsService.cs
git commit -m "feat(student-id): report card + SMS show KPS+ID"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] Grep check: `grep -rn "'KPS' + CAST" --include=*.cs .` returns nothing (hardcoded prefix gone).
- [ ] **User smoke test (running app):**
  1. View Students, Payment History, Outstanding Fees, Attendance, dashboard Recent Payments → student ID shows `KPS####`.
  2. Open a student's details → ID shows `KPS####`; Save and Roll-Out still work (operate on the numeric ID).
  3. Fees Payment → type `KPS9016` OR `9016` → both look up the same student; the receipt's STUDENT ID tile shows `KPS9016`.
  4. Generate a report card → "Admission No." shows `KPS####`.
  5. A registration/payment/admission SMS shows `Student ID: KPS####`.
  6. Sorting/selecting a grid row still behaves (underlying value numeric).

## Self-review notes

- **Spec coverage:** helper (Task 1) ✓; configurable abbrev via `AppConfig.Sms.SchoolAbbreviation` ✓; remove hardcoded SQL prefix (Task 2) ✓; grids incl. dashboard Recent Payments + View Students (Task 3) ✓; student details (Task 4) ✓; fee lookup/receipt + accept both forms (Task 5) ✓; report card + SMS (Task 6) ✓; display-only/readback-safe ✓.
- **Known limitation (acceptable):** grid in-place search boxes (e.g. Payment History) still match the *numeric* underlying value, so searching by the bare number works; the explicit "accept KPS or 9016" requirement is implemented for the fee-payment lookup (the field used to fetch a student). Not expanding grid search parsing — out of scope, avoids stripping "KPS" from name/bursar searches.
- **Type consistency:** all sites call `Common.StudentId.Display(...)` / `Common.StudentId.Parse(...)` / `Common.StudentId.AttachGridFormatting(grid, "<col>")` with the column names that match the DataTable aliases (`ID`, `STUDENT ID`).
- **No placeholders:** every step has exact old→new code; the one `replace_all` step (SMS) is explicitly flagged.
