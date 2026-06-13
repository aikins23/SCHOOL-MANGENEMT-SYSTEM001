# Student CSV Import/Export + Parent Credentials Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the student-view CSV export→fill→import round trip reliable, and have each imported student yield one PARENT account whose credentials are SMSed to the guardian's number.

**Architecture:** Fix the existing `CsvImportExportService` in place (ID parsing, dates, row-level failure reporting, counts). Pure credential helpers live in `Common/ImportCredentials.cs` (unit-tested). A typed `SmsService.SendParentCredentialsAsync` rides the durable SMS outbox and includes the new configurable `SchoolInformation.PortalUrl`. `frmStdView` gains pre-import confirmation and off-UI-thread execution.

**Tech Stack:** C#/.NET 4.7.2 WinForms, OleDb/MSOLEDBSQL, explicit-include csproj, Kingdom.Tests custom runner.

**Spec:** `docs/superpowers/specs/2026-06-12-student-import-export-credentials-design.md`

---

## Conventions

- Gates: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; `dotnet run --project "Tests/Kingdom.Tests/Kingdom.Tests.csproj"` → all pass. If `bin\Debug` exe is locked by a debug session, build with `-p:OutputPath=bin/Verify/`.
- Existing facts this plan relies on: `AuthService.RegisterAsync(u,p,p,"PARENT",null)` returns `(false, "Username already exists.")` for duplicates and `ParseRole("PARENT") == UserRole.Parent`; `Common.StudentId.Parse/Display`; `ValidationHelper.IsStrongPassword` = ≥8 chars + upper + lower + digit; `SmsSenderIds.StudentAdmission`; `SchoolProfile.DisplayName`; `frmStdView.LoadStudents()` refreshes the grid (already called after import).
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: `ImportCredentials` helper (+ unit tests)

**Files:** Create `Common/ImportCredentials.cs`; modify `kingdom_Preparatory_School_Management_System.csproj`, `Tests/Kingdom.Tests/Program.cs`.

- [ ] **Step 1: Create `Common/ImportCredentials.cs`**

```csharp
using System;
using System.Security.Cryptography;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Credentials for guardian (PARENT) accounts created by the student CSV import.
    /// Username derives from the ward's display ID (e.g. kps9016 — the abbreviation prefix
    /// guarantees the 3-char minimum). Passwords are crypto-random and satisfy
    /// ValidationHelper.IsStrongPassword (8+ chars, upper, lower, digit).
    /// </summary>
    public static class ImportCredentials
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static string UsernameFor(string studentId) =>
            StudentId.Display(studentId).ToLowerInvariant();

        public static string NewPassword()
        {
            // "Pw" + 4 digits + 2 lowercase letters => 8 chars with upper, lower, digit.
            int n  = Next(0, 10000);
            char a = (char)('a' + Next(0, 26));
            char b = (char)('a' + Next(0, 26));
            return "Pw" + n.ToString("0000") + a + b;
        }

        private static int Next(int minInclusive, int maxExclusive)
        {
            var buf = new byte[4];
            lock (Rng) Rng.GetBytes(buf);
            uint v = BitConverter.ToUInt32(buf, 0);
            return (int)(minInclusive + (v % (uint)(maxExclusive - minInclusive)));
        }
    }
}
```

- [ ] **Step 2: csproj** — next to `<Compile Include="Common\SmsOutboxKey.cs" />` add:

```xml
    <Compile Include="Common\ImportCredentials.cs" />
```

- [ ] **Step 3: Tests** — in `Tests/Kingdom.Tests/Program.cs`, register after the `SmsOutboxKey` TestCase line:

```csharp
                new TestCase("ImportCredentials derives usernames and strong passwords", ImportCredentials_DerivesUsernamesAndPasswords),
```

and add the method after `SmsOutboxKey_IsDeterministic`:

```csharp
        private static void ImportCredentials_DerivesUsernamesAndPasswords()
        {
            string ab = StudentId.Abbrev.ToLowerInvariant();
            AssertEx.Equal(ab + "9016", ImportCredentials.UsernameFor("9016"));
            AssertEx.Equal(ab + "9016", ImportCredentials.UsernameFor(ab.ToUpperInvariant() + "9016")); // display form in
            AssertEx.Equal(ab + "1", ImportCredentials.UsernameFor("1"));                                // short IDs OK
            AssertEx.True(ImportCredentials.UsernameFor("1").Length >= 3, "prefix must satisfy the 3-char username minimum");

            string p1 = ImportCredentials.NewPassword();
            string p2 = ImportCredentials.NewPassword();
            AssertEx.True(ValidationHelper.IsStrongPassword(p1), "password must satisfy the strong-password rule: " + p1);
            AssertEx.Equal(8, p1.Length);
            AssertEx.NotEqual(p1, p2, "consecutive passwords must differ");
        }
```

- [ ] **Step 4: Build + run suite** — expect the new test to PASS, all green.
- [ ] **Step 5: Commit** — `git add Common/ImportCredentials.cs kingdom_Preparatory_School_Management_System.csproj Tests/Kingdom.Tests/Program.cs docs/superpowers/plans/2026-06-12-student-import-export-credentials.md` → `feat(import): ImportCredentials helper + tests`.

---

### Task 2: `PortalUrl` setting (model → repo → SchoolProfile → settings screen)

**Files:** Modify `Models/SchoolInformation.cs`, `Data/SchoolInfoRepository.cs`, `Common/SchoolProfile.cs`, `frmSchoolInfo.cs`.

- [ ] **Step 1: Model** — in `Models/SchoolInformation.cs` after the `Email` property add:

```csharp
        public string PortalUrl { get; set; } = "";           // parent portal link for credential SMS; blank = omit
```

- [ ] **Step 2: Repo ALTER** — in `Data/SchoolInfoRepository.cs`, append to the `colAlters` array (after the `SecondaryColor` entry, comma-separated):

```csharp
                    "IF COL_LENGTH('SchoolInformation','PortalUrl') IS NULL ALTER TABLE SchoolInformation ADD PortalUrl NVARCHAR(200) NOT NULL CONSTRAINT DF_SI_PortalUrl DEFAULT ('')"
```

- [ ] **Step 3: Repo read** — in `GetAsync`'s object initializer after `Email = AsString(r["Email"]),` add:

```csharp
                        PortalUrl = AsString(r["PortalUrl"]),
```

(Safe: `GetAsync` is only reached after `EnsureTablesAsync` — same guarantee the colour columns rely on.)

- [ ] **Step 4: Repo save** — in `SaveAsync` change the UPDATE statement's `Email=?,` to `Email=?, PortalUrl=?,` and insert the parameter immediately after the Email parameter line:

```csharp
                    cmd.Parameters.AddWithValue("?", info.PortalUrl ?? "");
```

(Order matters with OleDb positional params: it must be added right after the Email value, before the Logo parameter.)

- [ ] **Step 5: Accessor** — in `Common/SchoolProfile.cs` after `public static string Email => Info.Email;` add:

```csharp
        public static string PortalUrl => Info.PortalUrl;
```

- [ ] **Step 6: Settings screen** — in `frmSchoolInfo.cs`: add field `private TextBox _portalUrl;` next to `_email`; in `BuildUi` after `_email = addRow("Email");` add `_portalUrl = addRow("Parent Portal URL");`; in `LoadAsync` after `_email.Text = info.Email;` add `_portalUrl.Text = info.PortalUrl;`; in `SaveAsync`'s `new SchoolInformation { ... }` after `Email = _email.Text.Trim(),` add `PortalUrl = _portalUrl.Text.Trim(),`.

- [ ] **Step 7: Build** → 0 errors. **Commit**: `feat(import): configurable Parent Portal URL in School Information`.

---

### Task 3: `SmsService.SendParentCredentialsAsync`

**Files:** Modify `Services/SmsService.cs`.

- [ ] **Step 1:** After `SendTransportReminderAsync` add:

```csharp
        /// <summary>Guardian login credentials for the parent portal (sent on student import).
        /// Routed through the durable outbox; the portal link is included only when configured.</summary>
        public static Task<(bool Success, string Message)> SendParentCredentialsAsync(
            string recipient, string studentName, string username, string password)
        {
            string portal = SchoolProfile.PortalUrl;
            string linkLine = string.IsNullOrWhiteSpace(portal) ? "" : "\nPortal: " + portal.Trim();
            string message =
$@"Dear Guardian, here is your login to track {studentName} at {SchoolProfile.DisplayName}:
Username: {username}
Password: {password}{linkLine}
- Administration";
            return SendSmsAsync(recipient, message, SmsSenderIds.StudentAdmission);
        }
```

- [ ] **Step 2: Build** → 0 errors. **Commit**: `feat(import): typed parent-credentials SMS (outbox + portal link)`.

---

### Task 4: Import fixes in `CsvImportExportService`

**Files:** Modify `Services/CsvImportExportService.cs`.

- [ ] **Step 1: ID parsing + counts + failures.** Replace the body of the `for` loop's classification block — from `var cols = ParseCsvLine(lines[i]);` through the `isNew` determination — with:

```csharp
                    var cols = ParseCsvLine(lines[i]);
                    if (cols.Count < 14)
                    {
                        failures.Add($"Line {i + 1}: expected 14 columns, found {cols.Count}.");
                        continue;
                    }

                    // Accept both numeric (9016) and display (KPS9016) ID forms.
                    string studentId = Common.StudentId.Parse(cols[0]);
                    bool isNew = false;
                    bool explicitNewId = false;

                    if (string.IsNullOrEmpty(studentId))
                    {
                        studentId = await _studentRepository.GenerateNextStudentIdAsync();
                        isNew = true;
                    }
                    else if (!existingMap.ContainsKey(studentId))
                    {
                        isNew = true;
                        explicitNewId = true;
                    }
```

and declare at the top of the method (with the existing counters):

```csharp
            int newAutoCount = 0, newExplicitCount = 0, updatedCount = 0;
            var failures = new List<string>();
```

- [ ] **Step 2: DOB parsing.** Replace `student.DateOfBirth = DateTime.TryParse(...)` with:

```csharp
                    student.DateOfBirth = ParseDate(cols[3].Trim());
```

and add the private helper:

```csharp
        private static DateTime ParseDate(string value)
        {
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(value, formats, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var exact))
                return exact;
            return DateTime.TryParse(value, out var loose) ? loose : DateTime.Today.AddYears(-10);
        }
```

- [ ] **Step 3: Success/failure accounting.** After the add/update result, replace `if (!success) LoggerHelper.LogError(...)` blocks so failures are reported, and bump the per-kind counters:

```csharp
                    if (!success)
                    {
                        failures.Add($"Line {i + 1} ({Common.StudentId.Display(studentId)}): {res.Message}");
                        continue;
                    }
                    processedCount++;
                    if (isNew && explicitNewId) newExplicitCount++;
                    else if (isNew) newAutoCount++;
                    else updatedCount++;
```

(`res` is the `(Success, Message)` result of `AddStudentAsync`/`UpdateStudentAsync` — hoist it to a single variable: `var res = isNew ? await _studentService.AddStudentAsync(student) : await _studentService.UpdateStudentAsync(student); bool success = res.Success;`.)

- [ ] **Step 4: Credentials block.** Replace the whole "Auto-generate credentials and send SMS" block with:

```csharp
                    // One PARENT account per student; SMS the credentials to the guardian.
                    if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                    {
                        string username = Common.ImportCredentials.UsernameFor(student.StudentID);
                        string password = Common.ImportCredentials.NewPassword();

                        var authRes = await AuthService.RegisterAsync(username, password, password, "PARENT", null);
                        if (authRes.Success)
                        {
                            var smsRes = await SmsService.SendParentCredentialsAsync(
                                student.EmergencyContact, student.FullName, username, password);
                            if (smsRes.Success) smsCount++;
                            else failures.Add($"Line {i + 1} ({username}): SMS failed - {smsRes.Message}");
                        }
                        else if (authRes.Message != "Username already exists.")
                        {
                            // "already exists" = re-import; silently keep the original credentials.
                            failures.Add($"Line {i + 1} ({username}): account not created - {authRes.Message}");
                        }
                    }
```

- [ ] **Step 5: Result message.** Replace the final `return (true, ...)` with:

```csharp
                string summary =
                    $"Import completed. {newAutoCount} new (auto-ID), {newExplicitCount} new (explicit ID), " +
                    $"{updatedCount} updated, {failures.Count} failed. Sent {smsCount} SMS credentials.";
                if (failures.Count > 0)
                    summary += Environment.NewLine + Environment.NewLine + "Failures:" + Environment.NewLine +
                               string.Join(Environment.NewLine, failures.Take(10)) +
                               (failures.Count > 10 ? Environment.NewLine + $"... and {failures.Count - 10} more (see log)." : "");
                return (true, summary, processedCount, smsCount);
```

(Also log every failure line via `LoggerHelper.LogWarning` so >10 overflows are recoverable.)

- [ ] **Step 6: Build** → 0 errors. **Commit**: `fix(import): PARENT role, ID parsing, crypto passwords, row-level failure reporting`.

---

### Task 5: `frmStdView` import UX

**Files:** Modify `frmStdView.cs` (`ImportCsvAsync` ~line 171, `ExportCsvAsync` ~line 147).

- [ ] **Step 1: Pre-import confirmation + off-UI-thread run.** In `ImportCsvAsync`, after the file is picked and before calling the service, insert:

```csharp
                    int dataRows = System.IO.File.ReadAllLines(ofd.FileName)
                        .Skip(1).Count(l => !string.IsNullOrWhiteSpace(l));
                    if (dataRows == 0)
                    { MessageBox.Show("The file contains no data rows.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (MessageBox.Show($"{dataRows} data row(s) found. Import now?\n\nNew students get opening fee records and their guardians receive credential SMS.",
                            "Confirm Import", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
```

and wrap the service call so OleDb work leaves the UI thread, with a wait cursor:

```csharp
                    UseWaitCursor = true;
                    try
                    {
                        var result = await System.Threading.Tasks.Task.Run(
                            () => csvSvc.ImportStudentsFromCsvAsync(ofd.FileName));
                        ...existing result handling...
                    }
                    finally { UseWaitCursor = false; }
```

(`using System.Linq;` is already imported in `frmStdView.cs`; verify, else add.)

- [ ] **Step 2: Export off the UI thread.** Same `Task.Run` wrap for `csvSvc.ExportStudentsToCsvAsync(sfd.FileName)` in `ExportCsvAsync`.

- [ ] **Step 3: Build** → 0 errors. **Commit**: `feat(import): pre-import confirmation + off-UI-thread CSV work in student view`.

---

### Task 6: Verify

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors (use `-p:OutputPath=bin/Verify/` if the exe is locked).
- [ ] `dotnet run --project "Tests/Kingdom.Tests/Kingdom.Tests.csproj"` → all pass incl. the new ImportCredentials test.
- [ ] **User smoke test:** Export students → add one row with blank ID + a real phone, and edit one existing row using its `KPS####` ID → import → confirmation shows row count; result shows `1 new (auto-ID), 0 new (explicit ID), 1 updated`; new student has opening fees; guardian gets ONE SMS with `kps####` username; re-import the same file → counts show updates only, **no second SMS**; Settings → School Information shows the Parent Portal URL field, and filling it makes the link appear in subsequent credential SMS.

## Self-review notes

- **Spec coverage:** ImportCredentials helper + tests (T1) ✓; PortalUrl end-to-end (T2) ✓; typed SMS via outbox + conditional link (T3) ✓; ID parse / dates / PARENT role / crypto passwords / re-import skip / row failures / counts breakdown (T4) ✓; confirmation + Task.Run + result summary in UI (T5) ✓; fees-as-admission = unchanged AddStudentAsync path (no task needed) ✓.
- **Placeholders:** "...existing result handling..." in T5 marks code that stays as-is (MessageBox on result.Success), not unwritten code; all new code is verbatim.
- **Type consistency:** `ImportCredentials.UsernameFor(string)/NewPassword()`, `SendParentCredentialsAsync(string,string,string,string)`, `SchoolProfile.PortalUrl`, duplicate-detection string matches `RegisterAsync`'s literal `"Username already exists."`.
- **Risk:** OleDb positional params in `SchoolInfoRepository.SaveAsync` — the PortalUrl parameter MUST be inserted between Email and Logo to match the new SQL order (called out in T2 Step 4).
