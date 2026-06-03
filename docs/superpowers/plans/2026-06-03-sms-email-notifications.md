# SMS (custom sender name) & Email Notifications Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make SMS actually send through Arkesel with a per-duty custom sender name (KPSSTDADM / KPSEMPADM / KPSFEES), load it from configurable settings, add an SMS settings UI, and wire the three existing flows (student registration, employee registration, weekly fee reminder). Email is already implemented and stays as-is.

**Architecture:** A pluggable `ISmsProvider` (Arkesel + Log fallback) sits behind a static `SmsService` facade. Two pure helpers — `SmsSenderIds` (builds the alphanumeric sender from a configurable abbreviation) and `PhoneNumberGh` (normalizes Ghana numbers) — are unit-tested. Config lives in `AppConfig.Sms` backed by `Properties.Settings`. The three call sites already invoke `SmsService`; we repoint them at the new per-duty methods.

**Tech Stack:** C# / .NET Framework 4.7.2, WinForms, `System.Net.Http` (already referenced), xUnit (`[Fact]`, existing `Tests/` folder), Arkesel SMS v2 HTTP/JSON API.

---

## Verification note (read first)

This is a WinForms `.exe` project; there is no configured xUnit runner. Each task therefore verifies in two layers:

1. **Compile gate** — `dotnet build -clp:ErrorsOnly -nologo` must report `Build succeeded. 0 Error(s)`. Run from the project root:
   `cd "C:/Users/DELL/Downloads/New folder (2)/IPMC PROJECT BUABENG EMMANUEL AIKINS (1)/BUABENG EMMANUEL AIKINS - Copy"`
2. **Logic gate (pure helpers)** — a PowerShell reflection snippet loads the freshly built exe and asserts the method output (the project's established offline-verification technique; see memory `offline-winforms-render-harness`). Template:

```powershell
$ErrorActionPreference='Stop'
$bin="C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\bin\Debug"
[AppDomain]::CurrentDomain.add_AssemblyResolve([ResolveEventHandler]{param($s,$e)
  $p=Join-Path $bin (($e.Name -split ',')[0]+'.dll'); if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$NS='kingdom_Preparatory_School_Management_System'
# ... call static methods via $asm.GetType("$NS.Services.Xxx").GetMethod('Yyy').Invoke($null,@(args)) and compare
```

The xUnit `[Fact]` tests are still written (they document intent and run if a runner is added later), but the **reflection snippet is the authoritative "run the test" step** in this environment.

---

## File Structure

- Create `Services/PhoneNumberGh.cs` — pure Ghana phone normalization (`NormalizeGh`).
- Create `Services/SmsSenderIds.cs` — builds per-duty alphanumeric sender IDs from `AppConfig.Sms.SchoolAbbreviation`.
- Create `Services/ISmsProvider.cs` — provider interface.
- Create `Services/Sms/LogSmsProvider.cs` — log-only provider (default/offline).
- Create `Services/Sms/ArkeselSmsProvider.cs` — real Arkesel v2 HTTP/JSON provider.
- Modify `Services/SmsService.cs` — facade: provider selection from config + per-duty methods + test method.
- Modify `Common/AppConfig.cs` — add `Sms.Enabled` and `Sms.SchoolAbbreviation`.
- Modify `Properties/Settings.Designer.cs` + `app.config` — add `SmsEnabled`, `SmsSchoolAbbreviation`.
- Modify `Services/StudentService.cs:66` — call `SendStudentAdmissionAsync`.
- Modify `Services/EmployeeService.cs:45` — call `SendEmployeeAdmissionAsync`.
- Modify `frmDashboard.cs:1185` — call `SendFeeReminderAsync` (signature unchanged enough; see task).
- Modify `frmEmailSettings.cs` — add SMS settings group + test button.
- Create `Tests/PhoneNumberGhTests.cs`, `Tests/SmsSenderIdsTests.cs` — xUnit tests.
- Add `csproj` `<Compile Include>` entries for every new `.cs`.
- `.csproj` already references `System.Net.Http`; add `System.Net.Http.Formatting`? No — use raw `HttpClient` + manual JSON string. No new references needed.

---

## Task 1: Add SMS config settings (Enabled, SchoolAbbreviation)

**Files:**
- Modify: `Properties/Settings.settings` (after the `SmsFromNumber` Setting, ~line 42)
- Modify: `Properties/Settings.Designer.cs` (after the `SmsFromNumber` property, ~line 154)
- Modify: `Common/AppConfig.cs` (inside `public static class Sms`, after `FromNumber`)

> NOTE: This project has **no `userSettings` section in `app.config`** — settings rely on `DefaultSettingValueAttribute` in `Settings.Designer.cs` and the `Settings.settings` source file (user-scoped values persist to `user.config` at runtime). Do NOT edit `app.config`.

- [ ] **Step 1: Add the two settings properties to `Settings.Designer.cs`**

Insert after the `SmsFromNumber` property block:

```csharp
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool SmsEnabled {
            get {
                return ((bool)(this["SmsEnabled"]));
            }
            set {
                this["SmsEnabled"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KPS")]
        public string SmsSchoolAbbreviation {
            get {
                return ((string)(this["SmsSchoolAbbreviation"]));
            }
            set {
                this["SmsSchoolAbbreviation"] = value;
            }
        }
```

- [ ] **Step 2: Add matching entries to `Properties/Settings.settings`**

Find the `<Setting Name="SmsFromNumber" ...>` element and add two siblings after it (match the existing element style):

```xml
    <Setting Name="SmsEnabled" Type="System.Boolean" Scope="User">
      <Value Profile="(Default)">False</Value>
    </Setting>
    <Setting Name="SmsSchoolAbbreviation" Type="System.String" Scope="User">
      <Value Profile="(Default)">KPS</Value>
    </Setting>
```

- [ ] **Step 3: Add accessors to `AppConfig.Sms`**

Insert after the `FromNumber` property inside `public static class Sms`:

```csharp
            public static bool Enabled
            {
                get
                {
                    try { return Properties.Settings.Default.SmsEnabled; }
                    catch { return false; }
                }
                set
                {
                    try { Properties.Settings.Default.SmsEnabled = value; Properties.Settings.Default.Save(); }
                    catch { }
                }
            }

            public static string SchoolAbbreviation
            {
                get
                {
                    try
                    {
                        string v = Properties.Settings.Default.SmsSchoolAbbreviation;
                        return string.IsNullOrWhiteSpace(v) ? "KPS" : v.Trim().ToUpperInvariant();
                    }
                    catch { return "KPS"; }
                }
                set
                {
                    try { Properties.Settings.Default.SmsSchoolAbbreviation = value; Properties.Settings.Default.Save(); }
                    catch { }
                }
            }
```

- [ ] **Step 4: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add Properties/Settings.settings Properties/Settings.Designer.cs Common/AppConfig.cs
git commit -m "feat(sms): add SmsEnabled and SmsSchoolAbbreviation settings"
```

---

## Task 2: `PhoneNumberGh.NormalizeGh` (pure helper, TDD)

**Files:**
- Create: `Services/PhoneNumberGh.cs`
- Create: `Tests/PhoneNumberGhTests.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj` (add `<Compile Include>` for both)

- [ ] **Step 1: Write the failing xUnit test**

Create `Tests/PhoneNumberGhTests.cs`:

```csharp
using Xunit;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Tests
{
    public class PhoneNumberGhTests
    {
        [Theory]
        [InlineData("0241234567", "233241234567")]
        [InlineData("+233241234567", "233241234567")]
        [InlineData("233241234567", "233241234567")]
        [InlineData("024 123 4567", "233241234567")]
        [InlineData("024-123-4567", "233241234567")]
        public void NormalizeGh_ValidNumbers_Returns233Format(string input, string expected)
        {
            Assert.Equal(expected, PhoneNumberGh.NormalizeGh(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("hello")]
        [InlineData("12345")]
        [InlineData(null)]
        public void NormalizeGh_Invalid_ReturnsNull(string input)
        {
            Assert.Null(PhoneNumberGh.NormalizeGh(input));
        }
    }
}
```

- [ ] **Step 2: Create the implementation**

Create `Services/PhoneNumberGh.cs`:

```csharp
using System.Linq;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Normalizes Ghana mobile numbers to international "233XXXXXXXXX" (12 digits)
    /// for SMS gateways. Returns null when the input is not a plausible GH mobile.
    /// </summary>
    public static class PhoneNumberGh
    {
        public static string NormalizeGh(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Keep digits only (drops +, spaces, dashes, parens).
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return null;

            // 0XXXXXXXXX (10) -> 233XXXXXXXXX
            if (digits.Length == 10 && digits[0] == '0')
                return "233" + digits.Substring(1);

            // 233XXXXXXXXX (12)
            if (digits.Length == 12 && digits.StartsWith("233"))
                return digits;

            // 9-digit local without leading zero (e.g. 24XXXXXXX) -> 233XXXXXXXXX
            if (digits.Length == 9)
                return "233" + digits;

            return null;
        }
    }
}
```

- [ ] **Step 3: Register both files in the csproj**

In `kingdom_Preparatory_School_Management_System.csproj`, inside an `<ItemGroup>` that contains `<Compile Include=...>` entries, add:

```xml
    <Compile Include="Services\PhoneNumberGh.cs" />
    <Compile Include="Tests\PhoneNumberGhTests.cs" />
```

(If `Tests\EmployeeRepositorySecurityTests.cs` is already listed, place the test next to it; otherwise add it in the same ItemGroup.)

- [ ] **Step 4: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 5: Logic gate (reflection assertion)**

Run this PowerShell (using the template at the top); it must print `PHONE OK`:

```powershell
powershell -STA -Command "& {
$bin='C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve([ResolveEventHandler]{param($s,$e) $p=Join-Path $bin (($e.Name -split ',')[0]+'.dll'); if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Services.PhoneNumberGh')
$m=$t.GetMethod('NormalizeGh')
if($m.Invoke($null,@('0241234567')) -ne '233241234567'){throw 'fail 0-format'}
if($m.Invoke($null,@('+233241234567')) -ne '233241234567'){throw 'fail +233'}
if($m.Invoke($null,@('hello')) -ne $null){throw 'fail junk'}
'PHONE OK'}"
```

Expected: `PHONE OK`

- [ ] **Step 6: Commit**

```bash
git add Services/PhoneNumberGh.cs Tests/PhoneNumberGhTests.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(sms): add PhoneNumberGh.NormalizeGh with tests"
```

---

## Task 3: `SmsSenderIds` (pure helper, TDD)

**Files:**
- Create: `Services/SmsSenderIds.cs`
- Create: `Tests/SmsSenderIdsTests.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Write the failing xUnit test**

Create `Tests/SmsSenderIdsTests.cs`:

```csharp
using Xunit;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Tests
{
    public class SmsSenderIdsTests
    {
        [Fact]
        public void Build_UppercasesAndConcatenates()
        {
            Assert.Equal("KPSSTDADM", SmsSenderIds.Build("kps", SmsSenderIds.StudentSuffix));
            Assert.Equal("KPSEMPADM", SmsSenderIds.Build("KPS", SmsSenderIds.EmployeeSuffix));
            Assert.Equal("KPSFEES",   SmsSenderIds.Build("KPS", SmsSenderIds.FeeSuffix));
        }

        [Fact]
        public void Build_TrimsSpaces()
        {
            Assert.Equal("KPSFEES", SmsSenderIds.Build("  kps ", SmsSenderIds.FeeSuffix));
        }

        [Fact]
        public void ExceedsMaxLength_DetectsTooLong()
        {
            Assert.False(SmsSenderIds.ExceedsMaxLength("KPS"));      // KPSSTDADM = 9
            Assert.True(SmsSenderIds.ExceedsMaxLength("KINGDOM"));   // KINGDOMSTDADM = 13
        }
    }
}
```

- [ ] **Step 2: Create the implementation**

Create `Services/SmsSenderIds.cs`:

```csharp
using System;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Builds per-duty alphanumeric SMS Sender IDs from the configured school
    /// abbreviation. GSM caps alphanumeric sender IDs at 11 characters.
    /// </summary>
    public static class SmsSenderIds
    {
        public const string StudentSuffix  = "STDADM";
        public const string EmployeeSuffix = "EMPADM";
        public const string FeeSuffix      = "FEES";
        public const int MaxLength = 11;

        public static string Build(string abbreviation, string suffix)
        {
            string abbr = (abbreviation ?? "").Trim().ToUpperInvariant();
            return abbr + suffix;
        }

        /// <summary>True when {abbr}STDADM (the longest duty suffix) would exceed 11 chars.</summary>
        public static bool ExceedsMaxLength(string abbreviation)
        {
            return Build(abbreviation, StudentSuffix).Length > MaxLength;
        }

        // Live values from config:
        public static string StudentAdmission  => Build(AppConfig.Sms.SchoolAbbreviation, StudentSuffix);
        public static string EmployeeAdmission => Build(AppConfig.Sms.SchoolAbbreviation, EmployeeSuffix);
        public static string FeeReminder       => Build(AppConfig.Sms.SchoolAbbreviation, FeeSuffix);
    }
}
```

- [ ] **Step 3: Register both files in the csproj**

```xml
    <Compile Include="Services\SmsSenderIds.cs" />
    <Compile Include="Tests\SmsSenderIdsTests.cs" />
```

- [ ] **Step 4: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 5: Logic gate (reflection assertion)**

```powershell
powershell -STA -Command "& {
$bin='C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve([ResolveEventHandler]{param($s,$e) $p=Join-Path $bin (($e.Name -split ',')[0]+'.dll'); if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Services.SmsSenderIds')
$b=$t.GetMethod('Build')
if($b.Invoke($null,@('kps','STDADM')) -ne 'KPSSTDADM'){throw 'fail build'}
$e=$t.GetMethod('ExceedsMaxLength')
if($e.Invoke($null,@('KPS')) -ne $false){throw 'fail len ok'}
if($e.Invoke($null,@('KINGDOM')) -ne $true){throw 'fail len long'}
'SENDERID OK'}"
```

Expected: `SENDERID OK`

- [ ] **Step 6: Commit**

```bash
git add Services/SmsSenderIds.cs Tests/SmsSenderIdsTests.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(sms): add SmsSenderIds builder with tests"
```

---

## Task 4: `ISmsProvider` + `LogSmsProvider`

**Files:**
- Create: `Services/ISmsProvider.cs`
- Create: `Services/Sms/LogSmsProvider.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create the interface**

Create `Services/ISmsProvider.cs`:

```csharp
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>One concrete SMS delivery channel (Arkesel, log, etc.).</summary>
    public interface ISmsProvider
    {
        string Name { get; }
        Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message);
    }
}
```

- [ ] **Step 2: Create the log provider**

Create `Services/Sms/LogSmsProvider.cs`:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>Writes the SMS to logs/sms.log instead of sending. Default/offline fallback.</summary>
    public sealed class LogSmsProvider : ISmsProvider
    {
        public string Name => "LogOnly";

        public Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                string snippet = message?.Substring(0, Math.Min(message?.Length ?? 0, 80));
                File.AppendAllText(Path.Combine(dir, "sms.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [LOG_ONLY] From: {senderId} To: {recipient233} | Msg: {snippet}...\n");
            }
            catch { }
            return Task.FromResult((true, $"SMS logged (sender {senderId}) for {recipient233}"));
        }
    }
}
```

- [ ] **Step 3: Register both files in the csproj**

```xml
    <Compile Include="Services\ISmsProvider.cs" />
    <Compile Include="Services\Sms\LogSmsProvider.cs" />
```

- [ ] **Step 4: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add Services/ISmsProvider.cs Services/Sms/LogSmsProvider.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(sms): add ISmsProvider interface and LogSmsProvider"
```

---

## Task 5: `ArkeselSmsProvider` (real HTTP)

**Files:**
- Create: `Services/Sms/ArkeselSmsProvider.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create the Arkesel provider**

Create `Services/Sms/ArkeselSmsProvider.cs`:

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Arkesel SMS v2. POST https://sms.arkesel.com/api/v2/sms/send
    /// Header: api-key: &lt;key&gt;. Body: {"sender","message","recipients":["233..."]}.
    /// Success = HTTP 2xx AND the response body contains "status":"success".
    /// </summary>
    public sealed class ArkeselSmsProvider : ISmsProvider
    {
        private const string Endpoint = "https://sms.arkesel.com/api/v2/sms/send";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly string _apiKey;

        public ArkeselSmsProvider(string apiKey) { _apiKey = apiKey ?? ""; }

        public string Name => "Arkesel";

        public async Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (false, "Arkesel API key is not configured.");

            string json = "{\"sender\":\"" + Escape(senderId) + "\"," +
                          "\"message\":\"" + Escape(message) + "\"," +
                          "\"recipients\":[\"" + Escape(recipient233) + "\"]}";

            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, Endpoint))
                {
                    req.Headers.TryAddWithoutValidation("api-key", _apiKey);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using (var resp = await Http.SendAsync(req))
                    {
                        string body = await resp.Content.ReadAsStringAsync();
                        bool ok = resp.IsSuccessStatusCode &&
                                  body.IndexOf("\"status\":\"success\"", StringComparison.OrdinalIgnoreCase) >= 0;
                        return ok
                            ? (true, $"SMS sent to {recipient233} (sender {senderId})")
                            : (false, $"Arkesel error ({(int)resp.StatusCode}): {Trim(body)}");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Arkesel request failed: {ex.Message}");
            }
        }

        private static string Escape(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", "\\n");

        private static string Trim(string s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > 200 ? s.Substring(0, 200) : s);
    }
}
```

- [ ] **Step 2: Register the file in the csproj**

```xml
    <Compile Include="Services\Sms\ArkeselSmsProvider.cs" />
```

- [ ] **Step 3: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Services/Sms/ArkeselSmsProvider.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(sms): add ArkeselSmsProvider (v2 HTTP)"
```

---

## Task 6: Rewrite `SmsService` facade (provider selection + per-duty methods)

**Files:**
- Modify: `Services/SmsService.cs` (full replacement)

- [ ] **Step 1: Replace the file contents**

Replace the entire contents of `Services/SmsService.cs` with:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// SMS facade. Chooses a provider from AppConfig.Sms and exposes per-duty
    /// helpers that attach the correct custom sender ID. All sends are best-effort
    /// and never throw to the caller.
    /// </summary>
    public static class SmsService
    {
        private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        private static ISmsProvider ResolveProvider()
        {
            // Real sending only when enabled AND provider is Arkesel AND a key exists.
            if (AppConfig.Sms.Enabled &&
                string.Equals(AppConfig.Sms.Provider, "Arkesel", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(AppConfig.Sms.ApiKey))
            {
                return new ArkeselSmsProvider(AppConfig.Sms.ApiKey);
            }
            return new LogSmsProvider();
        }

        /// <summary>Low-level send with an explicit sender ID. Normalizes the GH number first.</summary>
        public static async Task<(bool Success, string Message)> SendSmsAsync(
            string recipient, string message, string senderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return (false, "Message body is required.");

                string normalized = PhoneNumberGh.NormalizeGh(recipient);
                if (normalized == null)
                {
                    Log("SKIPPED", recipient, senderId, "Invalid/blank phone number");
                    return (false, "Invalid recipient phone number.");
                }

                var provider = ResolveProvider();
                var result = await provider.SendAsync(senderId, normalized, message);
                Log(result.Success ? "SENT" : "ERROR", normalized, senderId,
                    $"{provider.Name}: {result.Message}");
                return result;
            }
            catch (Exception ex)
            {
                Log("ERROR", recipient, senderId, ex.Message);
                return (false, $"Failed to send SMS: {ex.Message}");
            }
        }

        public static Task<(bool Success, string Message)> SendStudentAdmissionAsync(string recipient, string fullName)
        {
            string message =
$@"Welcome to Kingdom Preparatory School, {fullName}!

You have been successfully registered as a student.

- Administration";
            return SendSmsAsync(recipient, message, SmsSenderIds.StudentAdmission);
        }

        public static Task<(bool Success, string Message)> SendEmployeeAdmissionAsync(string recipient, string fullName)
        {
            string message =
$@"Welcome to Kingdom Preparatory School, {fullName}!

You have been successfully registered as an employee.

- Administration";
            return SendSmsAsync(recipient, message, SmsSenderIds.EmployeeAdmission);
        }

        public static Task<(bool Success, string Message)> SendFeeReminderAsync(
            string recipient, string studentName, decimal balance)
        {
            string message =
$@"Dear Guardian,

This is a reminder that {studentName} has an outstanding balance of GHS {balance:N2}.

Please pay at your earliest convenience.

- Kingdom Preparatory School Accounts";
            return SendSmsAsync(recipient, message, SmsSenderIds.FeeReminder);
        }

        /// <summary>Used by the settings Test button. Uses the student-admission sender ID.</summary>
        public static Task<(bool Success, string Message)> SendTestAsync(string recipient)
        {
            return SendSmsAsync(recipient,
                "Test SMS from Kingdom Preparatory School. Your SMS settings are working.",
                SmsSenderIds.StudentAdmission);
        }

        private static void Log(string eventType, string recipient, string senderId, string details)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(Path.Combine(LogDir, "sms.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{eventType}] From: {senderId} To: {recipient} | {details}\n");
            }
            catch { }
        }
    }
}
```

> Note: this removes the old `SendRegistrationSmsAsync`, `SendFeeReminderSmsAsync`, `Provider/ApiKey/FromNumber` statics and the `SmsProvider` enum. Tasks 7–9 update the three callers; do those before re-building if you build incrementally, OR expect compile errors at the call sites until Task 9 is done. (Plan order: do 6→7→8→9 then build.)

- [ ] **Step 2: (defer build to Task 9)** — call sites still reference old methods; proceed to Task 7.

---

## Task 7: Wire student registration

**Files:**
- Modify: `Services/StudentService.cs:64-67`

- [ ] **Step 1: Replace the SMS call**

Replace:

```csharp
                if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                {
                    _ = SmsService.SendRegistrationSmsAsync(student.EmergencyContact, student.FullName, "Student");
                }
```

with:

```csharp
                if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                {
                    _ = SmsService.SendStudentAdmissionAsync(student.EmergencyContact, student.FullName);
                }
```

- [ ] **Step 2: Commit (build happens in Task 9)**

```bash
git add Services/StudentService.cs
git commit -m "feat(sms): student registration uses SendStudentAdmissionAsync"
```

---

## Task 8: Wire employee registration

**Files:**
- Modify: `Services/EmployeeService.cs:43-46`

- [ ] **Step 1: Replace the SMS call**

Replace:

```csharp
                    if (!string.IsNullOrWhiteSpace(employee.Contact))
                    {
                        _ = SmsService.SendRegistrationSmsAsync(employee.Contact, employee.FullName, "Employee");
                    }
```

with:

```csharp
                    if (!string.IsNullOrWhiteSpace(employee.Contact))
                    {
                        _ = SmsService.SendEmployeeAdmissionAsync(employee.Contact, employee.FullName);
                    }
```

- [ ] **Step 2: Commit**

```bash
git add Services/EmployeeService.cs
git commit -m "feat(sms): employee registration uses SendEmployeeAdmissionAsync"
```

---

## Task 9: Wire weekly fee reminder + full build

**Files:**
- Modify: `frmDashboard.cs:1182-1187`

- [ ] **Step 1: Replace the SMS call**

Replace:

```csharp
                        // Send SMS reminder
                        if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                        {
                            _ = SmsService.SendFeeReminderSmsAsync(
                                student.EmergencyContact, studentName, balance);
                        }
```

with:

```csharp
                        // Send SMS reminder (custom sender ID KPSFEES)
                        if (!string.IsNullOrWhiteSpace(student.EmergencyContact))
                        {
                            _ = SmsService.SendFeeReminderAsync(
                                student.EmergencyContact, studentName, balance);
                        }
```

- [ ] **Step 2: Compile gate (all call sites now updated)**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`
If errors mention `SendRegistrationSmsAsync`/`SendFeeReminderSmsAsync`/`SmsProvider`, a caller was missed — fix it.

- [ ] **Step 3: Logic gate (LogOnly path end-to-end via reflection)**

Confirms the facade resolves LogProvider and writes the log without throwing:

```powershell
powershell -STA -Command "& {
$bin='C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve([ResolveEventHandler]{param($s,$e) $p=Join-Path $bin (($e.Name -split ',')[0]+'.dll'); if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Services.SmsService')
$m=$t.GetMethod('SendStudentAdmissionAsync')
$task=$m.Invoke($null,@('0241234567','Ama Test'))
$task.Wait()
$r=$task.Result
'RESULT Success='+$r.Success+' Msg='+$r.Message}"
```

Expected: prints `RESULT Success=True Msg=SMS logged (sender KPSSTDADM) for 233241234567` (LogOnly, since SmsEnabled defaults False).

- [ ] **Step 4: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(sms): weekly fee reminder uses SendFeeReminderAsync (KPSFEES)"
```

---

## Task 10: SMS settings UI in `frmEmailSettings`

**Files:**
- Modify: `frmEmailSettings.cs` (add fields, build the group in `InitializeComponent`, load in `LoadSettings`, save in the Save handler, add Test handler)

- [ ] **Step 1: Add control fields**

After the existing field declarations (near `private Label lblStatus;`), add:

```csharp
        // SMS settings
        private GroupBox grpSmsSettings;
        private CheckBox chkSmsEnabled;
        private Label lblSmsApiKey;
        private TextBox txtSmsApiKey;
        private Label lblSmsAbbr;
        private TextBox txtSmsAbbr;
        private Label lblSmsSenderPreview;
        private Label lblSmsTestPhone;
        private TextBox txtSmsTestPhone;
        private Button btnTestSms;
```

- [ ] **Step 2: Enlarge the form and build the SMS group**

In `InitializeComponent`, change `this.Size` to give room:

```csharp
            this.Size = new System.Drawing.Size(500, 760);
```

Then, immediately **before** the buttons (`btnSave`/`btnCancel`/`btnTestEmail`) are positioned, add a new group below the SMTP group. Insert after `grpSmtpSettings` is added to the form (`this.Controls.Add(grpSmtpSettings);`) and before the action buttons:

```csharp
            // ─── SMS Settings Group ───────────────────────────
            int sy = grpSmtpSettings.Bottom + 12;
            grpSmsSettings = new GroupBox();
            grpSmsSettings.Text = "SMS Notifications (Arkesel)";
            grpSmsSettings.Location = new System.Drawing.Point(padding, sy);
            grpSmsSettings.Size = new System.Drawing.Size(this.ClientSize.Width - (padding * 2), 250);
            grpSmsSettings.Padding = new Padding(15);

            int my = 22;
            chkSmsEnabled = new CheckBox();
            chkSmsEnabled.Text = "Enable SMS sending (off = log only)";
            chkSmsEnabled.Location = new System.Drawing.Point(15, my);
            chkSmsEnabled.Size = new System.Drawing.Size(controlWidth + labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(chkSmsEnabled);
            my += controlHeight + 12;

            lblSmsApiKey = new Label();
            lblSmsApiKey.Text = "Arkesel API Key:";
            lblSmsApiKey.Location = new System.Drawing.Point(15, my);
            lblSmsApiKey.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsApiKey);

            txtSmsApiKey = new TextBox();
            txtSmsApiKey.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtSmsApiKey.Size = new System.Drawing.Size(controlWidth, controlHeight);
            txtSmsApiKey.UseSystemPasswordChar = true;
            grpSmsSettings.Controls.Add(txtSmsApiKey);
            my += controlHeight + 12;

            lblSmsAbbr = new Label();
            lblSmsAbbr.Text = "School Abbrev.:";
            lblSmsAbbr.Location = new System.Drawing.Point(15, my);
            lblSmsAbbr.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsAbbr);

            txtSmsAbbr = new TextBox();
            txtSmsAbbr.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtSmsAbbr.Size = new System.Drawing.Size(120, controlHeight);
            txtSmsAbbr.CharacterCasing = CharacterCasing.Upper;
            txtSmsAbbr.MaxLength = 5;
            txtSmsAbbr.TextChanged += (s, e) => UpdateSenderPreview();
            grpSmsSettings.Controls.Add(txtSmsAbbr);
            my += controlHeight + 8;

            lblSmsSenderPreview = new Label();
            lblSmsSenderPreview.Location = new System.Drawing.Point(15, my);
            lblSmsSenderPreview.Size = new System.Drawing.Size(controlWidth + labelWidth, controlHeight + 6);
            lblSmsSenderPreview.ForeColor = System.Drawing.Color.DimGray;
            grpSmsSettings.Controls.Add(lblSmsSenderPreview);
            my += controlHeight + 14;

            lblSmsTestPhone = new Label();
            lblSmsTestPhone.Text = "Test phone:";
            lblSmsTestPhone.Location = new System.Drawing.Point(15, my);
            lblSmsTestPhone.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblSmsTestPhone);

            txtSmsTestPhone = new TextBox();
            txtSmsTestPhone.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtSmsTestPhone.Size = new System.Drawing.Size(140, controlHeight);
            grpSmsSettings.Controls.Add(txtSmsTestPhone);

            btnTestSms = new Button();
            btnTestSms.Text = "Send Test SMS";
            btnTestSms.Location = new System.Drawing.Point(15 + labelWidth + 10 + 150, my - 1);
            btnTestSms.Size = new System.Drawing.Size(130, controlHeight + 2);
            btnTestSms.Click += btnTestSms_Click;
            grpSmsSettings.Controls.Add(btnTestSms);

            this.Controls.Add(grpSmsSettings);
```

> If the action buttons (`btnSave` etc.) are positioned with absolute Y based on the SMTP group, move their Y to `grpSmsSettings.Bottom + 15` so they sit below the new group. Locate the lines that set `btnSave.Location`/`btnCancel.Location`/`btnTestEmail.Location`/`lblStatus.Location` and base their `y` on `grpSmsSettings.Bottom + 15` instead of the previous value.

- [ ] **Step 3: Add the preview helper + load + save + test handler**

Add these methods to the class:

```csharp
        private void UpdateSenderPreview()
        {
            string abbr = (txtSmsAbbr.Text ?? "").Trim().ToUpperInvariant();
            string student  = abbr + "STDADM";
            string employee = abbr + "EMPADM";
            string fees     = abbr + "FEES";
            string warn = student.Length > 11 ? "  ⚠ exceeds 11 chars" : "";
            lblSmsSenderPreview.Text = $"Sender IDs: {student} · {employee} · {fees}{warn}";
        }

        private async void btnTestSms_Click(object sender, EventArgs e)
        {
            // Persist current SMS fields first so the test uses what the user typed.
            AppConfig.Sms.Enabled = chkSmsEnabled.Checked;
            AppConfig.Sms.Provider = "Arkesel";
            AppConfig.Sms.ApiKey = txtSmsApiKey.Text.Trim();
            AppConfig.Sms.SchoolAbbreviation = txtSmsAbbr.Text.Trim();

            btnTestSms.Enabled = false;
            try
            {
                var result = await SmsService.SendTestAsync(txtSmsTestPhone.Text.Trim());
                if (lblStatus != null)
                {
                    lblStatus.Text = result.Message;
                    lblStatus.ForeColor = result.Success ? System.Drawing.Color.Green : System.Drawing.Color.Red;
                }
            }
            finally { btnTestSms.Enabled = true; }
        }
```

In `LoadSettings()`, after the email fields load, add:

```csharp
            chkSmsEnabled.Checked = AppConfig.Sms.Enabled;
            txtSmsApiKey.Text = AppConfig.Sms.ApiKey;
            txtSmsAbbr.Text = AppConfig.Sms.SchoolAbbreviation;
            UpdateSenderPreview();
```

In the Save button handler (locate `btnSave.Click` / `SaveSettings`), after the email settings are saved, add:

```csharp
            AppConfig.Sms.Enabled = chkSmsEnabled.Checked;
            AppConfig.Sms.Provider = "Arkesel";
            AppConfig.Sms.ApiKey = txtSmsApiKey.Text.Trim();
            AppConfig.Sms.SchoolAbbreviation = txtSmsAbbr.Text.Trim();
```

- [ ] **Step 4: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 5: Visual gate (offline render harness)**

Render `frmEmailSettings` to PNG using the technique in memory `offline-winforms-render-harness` (auth: set `AuthService.CurrentUser` to `Administrator`; off-screen `Show()`; `DrawToBitmap`). Confirm the SMS group shows the Enable checkbox, API key, abbreviation, the live `KPSSTDADM · KPSEMPADM · KPSFEES` preview, and the Test row, with the Save/Cancel buttons below it (not overlapping).

- [ ] **Step 6: Commit**

```bash
git add frmEmailSettings.cs
git commit -m "feat(sms): add SMS settings + test button to frmEmailSettings"
```

---

## Task 11: Final verification & docs

- [ ] **Step 1: Full solution build**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 2: Manual smoke (operator)**

Document in the PR description for the user to run on a machine with an Arkesel account:
1. Open Settings → SMS, enable, paste API key, set abbreviation `KPS`, Save.
2. Register the three sender IDs (`KPSSTDADM`, `KPSEMPADM`, `KPSFEES`) in the Arkesel dashboard.
3. Send Test SMS to your own number → expect delivery from `KPSSTDADM`.
4. Register a test student/employee with a phone → expect the welcome SMS.
5. `logs/sms.log` shows `SENT`/`ERROR` lines.

- [ ] **Step 3: Update memory index (optional)**

Note in project memory that SMS now sends via Arkesel with per-duty sender IDs and the abbreviation is configurable in Settings.

- [ ] **Step 4: Commit any doc changes**

```bash
git add -A
git commit -m "docs(sms): notes for Arkesel setup and sender ID registration"
```

---

## Self-Review

- **Spec coverage:** Arkesel provider (T5), pluggable interface + Log fallback (T4), facade + per-duty senders + test (T6), config Enabled/Abbreviation (T1), sender-ID scheme KPSSTDADM/EMPADM/FEES (T3/T6), phone normalization (T2), three call sites (T7/T8/T9), settings UI with preview + test (T10), error handling/logging (T6/Log providers), email untouched (no task — by design). ✓ All spec sections map to tasks.
- **Placeholder scan:** No TBD/TODO; every code step shows full code. ✓
- **Type consistency:** `ISmsProvider.SendAsync(senderId, recipient233, message)` used identically by `LogSmsProvider`, `ArkeselSmsProvider`, and `SmsService.SendSmsAsync`. `SmsSenderIds.StudentAdmission/EmployeeAdmission/FeeReminder` consumed by the facade. `AppConfig.Sms.Enabled/Provider/ApiKey/SchoolAbbreviation` consumed by `ResolveProvider` and the UI. ✓
- **Build-order caveat** explicitly called out (T6 leaves callers broken until T9) so an out-of-order executor knows to expect transient compile errors. ✓
