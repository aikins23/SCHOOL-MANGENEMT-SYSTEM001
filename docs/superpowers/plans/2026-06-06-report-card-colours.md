# Configurable Report Card Colours (+ Preview) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let Director/Administrator choose the report card's three brand colours in School Information, preview a printable sample with the chosen colours before saving, and have the report card use the saved colours.

**Architecture:** Three ARGB-int colour columns added (idempotently) to the existing `SchoolInformation` row; `SchoolProfile` exposes them as `System.Drawing.Color` plus a transient preview override; `ReportCardPDFGenerator`'s `Navy`/`Gold`/`LightBlue` become properties reading `SchoolProfile`; `frmSchoolInfo` gains a "Report Card Colours" section with three `ColorDialog` pickers, a Preview button, and Save.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, PdfSharp, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-report-card-colours-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) reflection probe for pure logic; (3) render harness / user run for the form.
- **No new files** → no `.csproj` edits. All four targets are existing, compiled files.
- Colours stored as **ARGB int** (`Color.ToArgb()` / `Color.FromArgb(int)`).
- The defaults are single-sourced in `Models/SchoolInformation` and reused by the repository's
  `ALTER … DEFAULT` and by `SchoolProfile`'s fail-safe.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure (all modified, none created)

- `Models/SchoolInformation.cs` — three colour properties.
- `Data/SchoolInfoRepository.cs` — idempotent ALTER + read/write the columns.
- `Common/SchoolProfile.cs` — colour getters + transient preview override.
- `Services/ReportCardPDFGenerator.cs` — read colours from `SchoolProfile`.
- `frmSchoolInfo.cs` — "Report Card Colours" UI + pickers + Preview + Save wiring.

---

### Task 1: Colour columns — model + repository

**Files:**
- Modify: `Models/SchoolInformation.cs`
- Modify: `Data/SchoolInfoRepository.cs`

- [ ] **Step 1: Add colour properties to the model**

In `Models/SchoolInformation.cs`, add `using System.Drawing;` at the top (after `using System;`):

```csharp
using System;
using System.Drawing;
```

Then add these three properties after `public decimal AdmissionFee { get; set; } = 100m;`:

```csharp
        // Report card brand colours (ARGB ints). Defaults = the legacy hardcoded colours.
        public int PrimaryColorArgb { get; set; } = Color.FromArgb(9, 35, 96).ToArgb();    // header / Navy
        public int AccentColorArgb { get; set; } = Color.FromArgb(210, 190, 36).ToArgb();  // accent / Gold
        public int SecondaryColorArgb { get; set; } = Color.FromArgb(189, 214, 238).ToArgb(); // row tint / Light Blue
```

- [ ] **Step 2: Add idempotent ALTERs in `EnsureTablesAsync`**

In `Data/SchoolInfoRepository.cs`, in `EnsureTablesAsync`, immediately AFTER the `createInfo`
command block (the `using (var cmd = new OleDbCommand(createInfo, c)) ...` line) and BEFORE the
`createFees` block, insert:

```csharp
                // Add report-card colour columns to existing installs (idempotent).
                var def = new SchoolInformation();
                string[] colAlters =
                {
                    $"IF COL_LENGTH('SchoolInformation','PrimaryColor') IS NULL ALTER TABLE SchoolInformation ADD PrimaryColor INT NOT NULL CONSTRAINT DF_SI_Primary DEFAULT ({def.PrimaryColorArgb})",
                    $"IF COL_LENGTH('SchoolInformation','AccentColor') IS NULL ALTER TABLE SchoolInformation ADD AccentColor INT NOT NULL CONSTRAINT DF_SI_Accent DEFAULT ({def.AccentColorArgb})",
                    $"IF COL_LENGTH('SchoolInformation','SecondaryColor') IS NULL ALTER TABLE SchoolInformation ADD SecondaryColor INT NOT NULL CONSTRAINT DF_SI_Secondary DEFAULT ({def.SecondaryColorArgb})"
                };
                foreach (var alter in colAlters)
                    using (var cmd = new OleDbCommand(alter, c)) await cmd.ExecuteNonQueryAsync();
```

(The brand-new seed `INSERT` can stay unchanged — it omits the colour columns, so they take the
DEFAULTs. Both fresh and existing databases converge.)

- [ ] **Step 3: Read the columns in `GetAsync`**

In `GetAsync`, add the three reads to the returned object initializer (after
`AdmissionFee = Convert.ToDecimal(r["AdmissionFee"]),`):

```csharp
                        PrimaryColorArgb = AsInt(r, "PrimaryColor", new SchoolInformation().PrimaryColorArgb),
                        AccentColorArgb = AsInt(r, "AccentColor", new SchoolInformation().AccentColorArgb),
                        SecondaryColorArgb = AsInt(r, "SecondaryColor", new SchoolInformation().SecondaryColorArgb),
```

Then add this helper next to `AsString` (near the bottom of the class):

```csharp
        private static int AsInt(System.Data.IDataRecord r, string col, int fallback)
        {
            try { var v = r[col]; return v == null || v == DBNull.Value ? fallback : Convert.ToInt32(v); }
            catch { return fallback; }
        }
```

- [ ] **Step 4: Write the columns in `SaveAsync`**

In `SaveAsync`, change the UPDATE SQL to include the three columns:

```csharp
                const string sql = @"UPDATE SchoolInformation SET
                    Name=?, Address=?, PoBox=?, GpsAddress=?, Phone1=?, Phone2=?, Email=?,
                    Logo=?, AdmissionFee=?, PrimaryColor=?, AccentColor=?, SecondaryColor=?, UpdatedDate=? WHERE Id = 1";
```

Then add the three parameters in order — immediately AFTER the
`cmd.Parameters.AddWithValue("?", info.AdmissionFee);` line and BEFORE the
`cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));` line:

```csharp
                    cmd.Parameters.AddWithValue("?", info.PrimaryColorArgb);
                    cmd.Parameters.AddWithValue("?", info.AccentColorArgb);
                    cmd.Parameters.AddWithValue("?", info.SecondaryColorArgb);
```

(Parameter order must match the `?` order: …AdmissionFee, PrimaryColor, AccentColor, SecondaryColor, UpdatedDate.)

- [ ] **Step 5: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Models/SchoolInformation.cs Data/SchoolInfoRepository.cs
git commit -m "feat(report-colours): store report card colours on SchoolInformation (idempotent columns)"
```

---

### Task 2: `SchoolProfile` colour getters + preview override

**Files:**
- Modify: `Common/SchoolProfile.cs`

- [ ] **Step 1: Add the System.Drawing using**

At the top of `Common/SchoolProfile.cs`, add after `using System.Collections.Generic;`:

```csharp
using System.Drawing;
```

- [ ] **Step 2: Add the override field + colour getters**

After the `public static decimal AdmissionFee => Info.AdmissionFee;` line, add:

```csharp
        /// <summary>
        /// Transient, in-memory colour override used by the settings Preview button so unsaved
        /// colours render without being persisted. Set it, generate, then clear it in a finally.
        /// </summary>
        public static (Color Primary, Color Accent, Color Secondary)? ReportColorOverride;

        public static Color ReportPrimaryColor =>
            ReportColorOverride?.Primary ?? Color.FromArgb(Info.PrimaryColorArgb);
        public static Color ReportAccentColor =>
            ReportColorOverride?.Accent ?? Color.FromArgb(Info.AccentColorArgb);
        public static Color ReportSecondaryColor =>
            ReportColorOverride?.Secondary ?? Color.FromArgb(Info.SecondaryColorArgb);
```

(`Info` already falls back to `new SchoolInformation()` on DB error, whose colour defaults are the
legacy colours — so these are fail-safe.)

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 4: Probe (reflection; fail-safe means LocalDB not required)**

Create `tmp_probe_colours.ps1`:

```powershell
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$bin = Join-Path $PSScriptRoot 'bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($s,$e)
  $n=(New-Object Reflection.AssemblyName($e.Name)).Name
  $p=Join-Path $bin "$n.dll"; if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null} })
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Common.SchoolProfile')
$p=$t.GetProperty('ReportPrimaryColor').GetValue($null)
"Primary = R$($p.R) G$($p.G) B$($p.B)"
```

Run: `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tmp_probe_colours.ps1`
Expected (default navy): `Primary = R9 G35 B96`
Delete after: `Remove-Item tmp_probe_colours.ps1`.

- [ ] **Step 5: Commit**

```bash
git add Common/SchoolProfile.cs
git commit -m "feat(report-colours): SchoolProfile colour getters + transient preview override"
```

---

### Task 3: Drive `ReportCardPDFGenerator` from the colours

**Files:**
- Modify: `Services/ReportCardPDFGenerator.cs` (lines ~33-37)

- [ ] **Step 1: Replace the static colour fields with scheme-backed properties**

Replace (lines ~33-37):

```csharp
        private static readonly XColor Navy = XColor.FromArgb(9, 35, 96);
        private static readonly XColor LightBlue = XColor.FromArgb(189, 214, 238);
        private static readonly XColor Gold = XColor.FromArgb(210, 190, 36);
        private static readonly XColor Black = XColors.Black;
        private static readonly XColor White = XColors.White;
```

with:

```csharp
        // Brand colours come from the configurable School Information settings (SchoolProfile),
        // honouring the transient preview override. Black/white stay fixed.
        private static XColor Navy      => ToX(Common.SchoolProfile.ReportPrimaryColor);
        private static XColor LightBlue => ToX(Common.SchoolProfile.ReportSecondaryColor);
        private static XColor Gold      => ToX(Common.SchoolProfile.ReportAccentColor);
        private static readonly XColor Black = XColors.Black;
        private static readonly XColor White = XColors.White;

        private static XColor ToX(System.Drawing.Color c) => XColor.FromArgb(c.A, c.R, c.G, c.B);
```

(All existing uses of `Navy`/`Gold`/`LightBlue` keep compiling — they are now properties.)

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If the compiler complains `Navy`/`Gold`/`LightBlue` are used in a `static`
field initializer that now can't see an instance — there are none in this file; they're used only
inside instance draw methods — so this should be clean.)

- [ ] **Step 3: Commit**

```bash
git add Services/ReportCardPDFGenerator.cs
git commit -m "feat(report-colours): report card reads brand colours from settings"
```

---

### Task 4: `frmSchoolInfo` — colour pickers + Preview

**Files:**
- Modify: `frmSchoolInfo.cs`

- [ ] **Step 1: Add fields**

In `frmSchoolInfo.cs`, change the field declarations block to add the swatch panels + preview button:

Replace:

```csharp
        private DataGridView _feeGrid;
        private Button _saveBtn, _cancelBtn, _uploadBtn;
        private Label _status;
```

with:

```csharp
        private DataGridView _feeGrid;
        private Button _saveBtn, _cancelBtn, _uploadBtn, _previewBtn;
        private Panel _primarySwatch, _accentSwatch, _secondarySwatch;
        private Label _status;
```

- [ ] **Step 2: Build the "Report Card Colours" section**

In `BuildUi`, immediately AFTER the Fees section (after the line
`Controls.Add(_feeGrid); y += _feeGrid.Height + gap;`) and BEFORE the `_status` label, insert:

```csharp
            // Report Card Colours
            var colHdr = new Label { Text = "Report Card Colours", Left = lblX, Top = y, Width = 300, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppConfig.Colors.PrimaryColor };
            Controls.Add(colHdr); y += 28;

            _primarySwatch = AddColourRow("Primary (header)", ref y, lblX, boxX, rowH, gap);
            _accentSwatch = AddColourRow("Accent (logo / labels)", ref y, lblX, boxX, rowH, gap);
            _secondarySwatch = AddColourRow("Secondary (row tint)", ref y, lblX, boxX, rowH, gap);

            _previewBtn = new Button { Text = "Preview Report Card", Left = boxX, Top = y, Width = 200, Height = 32, FlatStyle = FlatStyle.Flat };
            _previewBtn.Click += async (s, e) => await PreviewAsync();
            Controls.Add(_previewBtn); y += rowH + gap + 6;
```

- [ ] **Step 3: Add the `AddColourRow` + `PickColour` helpers**

Add these methods to the class (e.g. just after `BuildUi`):

```csharp
        private Panel AddColourRow(string caption, ref int y, int lblX, int boxX, int rowH, int gap)
        {
            var l = new Label { Text = caption, Left = lblX, Top = y + 4, Width = boxX - lblX - 6, Font = new Font("Segoe UI", 10F) };
            var swatch = new Panel { Left = boxX, Top = y, Width = 64, Height = 26, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            var btn = new Button { Text = "Change…", Left = boxX + 74, Top = y - 2, Width = 90, Height = 30, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => PickColour(swatch);
            Controls.Add(l); Controls.Add(swatch); Controls.Add(btn);
            y += rowH + gap;
            return swatch;
        }

        private void PickColour(Panel swatch)
        {
            using (var dlg = new ColorDialog { FullOpen = true, Color = swatch.BackColor })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) swatch.BackColor = dlg.Color;
            }
        }
```

- [ ] **Step 4: Load the saved colours into the swatches**

In `LoadAsync`, after `SetLogoPreview(info.Logo);`, add:

```csharp
                _primarySwatch.BackColor = Color.FromArgb(info.PrimaryColorArgb);
                _accentSwatch.BackColor = Color.FromArgb(info.AccentColorArgb);
                _secondarySwatch.BackColor = Color.FromArgb(info.SecondaryColorArgb);
```

- [ ] **Step 5: Persist the colours on Save**

In `SaveAsync`, extend the `new SchoolInformation { ... }` initializer to include the three colours.
Change:

```csharp
                    Email = _email.Text.Trim(), Logo = _logoBytes, AdmissionFee = admission
                };
```

to:

```csharp
                    Email = _email.Text.Trim(), Logo = _logoBytes, AdmissionFee = admission,
                    PrimaryColorArgb = _primarySwatch.BackColor.ToArgb(),
                    AccentColorArgb = _accentSwatch.BackColor.ToArgb(),
                    SecondaryColorArgb = _secondarySwatch.BackColor.ToArgb()
                };
```

- [ ] **Step 6: Add the Preview method**

Add this method to the class (e.g. after `SaveAsync`):

```csharp
        private async Task PreviewAsync()
        {
            _previewBtn.Enabled = false;
            SchoolProfile.ReportColorOverride =
                (_primarySwatch.BackColor, _accentSwatch.BackColor, _secondarySwatch.BackColor);
            try
            {
                var sample = BuildSampleReportCard();
                var bytes = await new ReportCardPDFGenerator().GeneratePDFAsync(sample);
                string path = Path.Combine(Path.GetTempPath(),
                    "ReportCardPreview_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf");
                File.WriteAllBytes(path, bytes);
                try { System.Diagnostics.Process.Start(path); }
                catch { UIHelper.ShowWarning("Preview saved to:\n" + path + "\n(No PDF viewer found to open it.)", "Preview"); }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not generate preview: " + ex.Message, "Preview");
            }
            finally
            {
                SchoolProfile.ReportColorOverride = null;
                _previewBtn.Enabled = true;
            }
        }

        private ReportCardData BuildSampleReportCard()
        {
            return new ReportCardData
            {
                StudentID = "0",
                StudentName = "Sample Student",
                ClassID = "BASIC 5",
                Gender = "Male",
                Term = "TERM 3",
                Year = "2024/2025",
                PresentDays = 58,
                TotalSchoolDays = 60,
                OverallPosition = 3,
                TotalStudentsInClass = 30,
                SubjectResults = new List<SubjectResult>
                {
                    new SubjectResult { Subject = "Mathematics", ClassScore = 52, ExamScore = 88, TotalScore = 84, PositionInClass = 2 },
                    new SubjectResult { Subject = "English Language", ClassScore = 48, ExamScore = 74, TotalScore = 72, PositionInClass = 5 },
                    new SubjectResult { Subject = "Integrated Science", ClassScore = 40, ExamScore = 66, TotalScore = 66, PositionInClass = 8 }
                },
                Remarks = new StudentTermRemarks { StudentID = "0", Term = "TERM 3", Year = "2024/2025" },
                SchoolInfo = new SchoolInfo
                {
                    Name = SchoolProfile.Name,
                    Location = SchoolProfile.Address,
                    PhoneNumbers = SchoolProfile.Phones,
                    Logo = SchoolProfile.Logo
                }
            };
        }
```

(`System.Diagnostics` is referenced fully-qualified, so no new `using` is needed. `List<>`,
`ReportCardData`, `SubjectResult`, `StudentTermRemarks`, `SchoolInfo` are already in scope via the
file's existing `using`s for `System.Collections.Generic` and the `Models` namespace.)

- [ ] **Step 7: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If `StudentTermRemarks` requires different members, open `Models/StudentTermRemarks.cs`
and set only the properties it actually has — at minimum construct `new StudentTermRemarks()`.)

- [ ] **Step 8: Render-verify (offline harness)**

Render `frmSchoolInfo` (Director). Confirm the new "Report Card Colours" header, three colour rows
(label + swatch + Change…), and the "Preview Report Card" button appear below the Fees grid. (DB
down → swatches default white until LoadAsync runs; layout still renders.)

- [ ] **Step 9: Commit**

```bash
git add frmSchoolInfo.cs
git commit -m "feat(report-colours): colour pickers + printable Preview in School Information"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app):**
  1. Director/Admin → School Information → the three swatches show the current colours (navy/gold/light-blue).
  2. Change the **Primary** colour → click **Preview Report Card** → a sample report card opens in
     the new header colour (printable from the viewer); the saved colours are NOT yet changed.
  3. Click **Save** → generate a real report card (Exam Reports) → it uses the new colour.
  4. Reopen School Information → the colours persist; Preview without saving does not alter saved colours.

## Self-review notes

- **Spec coverage:** 3 colour columns idempotently added (Task 1) ✓; model props with legacy
  defaults (Task 1) ✓; Get/Save (Task 1) ✓; SchoolProfile getters + transient override (Task 2) ✓;
  generator reads colours (Task 3) ✓; frmSchoolInfo pickers + Preview + Save (Task 4) ✓; black/white
  fixed ✓; fail-safe to defaults ✓.
- **Type/name consistency:** `PrimaryColorArgb/AccentColorArgb/SecondaryColorArgb` (model),
  `ReportPrimaryColor/ReportAccentColor/ReportSecondaryColor` + `ReportColorOverride` (SchoolProfile),
  `ToX` (generator), `AddColourRow/PickColour/PreviewAsync/BuildSampleReportCard` (form) — consistent
  across tasks; param order in `SaveAsync` matches the SQL `?` order.
- **No placeholders:** every step has complete code; the probe step has exact expected output; the one
  conditional (`StudentTermRemarks` shape, Task 4 Step 7) gives an explicit fallback.
- **Scope:** single screen + data layer; no new files, no csproj/RBAC/nav changes. `ReportCardPdfService`
  dead legend untouched (consistent with the grading-scheme plan's finding).
