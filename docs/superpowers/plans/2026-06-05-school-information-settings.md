# School Information Settings — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Store the school's identity (name, address, P.O. Box, Ghana Post GPS address, phones, email, logo) and fee schedule (admission fee + per-class term fees) in the database, editable by Director/Administrator through a settings form, and make the existing hardcoded call sites read from it.

**Architecture:** Two SQL Server (LocalDB) tables via OleDb — `SchoolInformation` (single row) and `ClassFees` (one row per class). A repository (`SchoolInfoRepository`) creates + seeds them lazily. A static fail-safe cache (`SchoolProfile`) exposes the values synchronously; the hardcoded sites (`AdmissionFees.Amount`, `StudentService.GetFeeForClass`, report-card `SchoolInfo`) delegate to it with no signature changes. A WinForms form (`frmSchoolInfo`) edits everything and refreshes the cache on save.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb` (`Provider=MSOLEDBSQL`), `Properties.Settings`.

**Spec:** `docs/superpowers/specs/2026-06-05-school-information-settings-design.md`

---

## Conventions for this codebase (read first)

- **No unit-test framework exists** in this project (xUnit was tried and removed; test files are not compiled). The verification gates used throughout this plan are:
  1. **Build clean:** `dotnet build -clp:ErrorsOnly -nologo` from the repo root must report **0 errors**.
  2. **Runtime probe (DB/logic):** a throwaway `-STA` PowerShell script that loads `bin\Debug\<app>.exe` by reflection and calls the new methods. Requires LocalDB to be running; if LocalDB will not start in the working shell, mark the probe **deferred to the user** and rely on build-clean + code review for that task. Do NOT attempt risky LocalDB repair.
  3. **Render harness (forms):** the offline WinForms render harness (see user memory `offline-winforms-render-harness.md`) renders a form to PNG via reflection (off-screen `Show()` + `DrawToBitmap`). Used to confirm form layout without launching the app.
- **OleDb parameters are positional** (`?` placeholders, added in column order).
- **`datetime` columns:** MSOLEDBSQL rejects sub-second precision — truncate `DateTime` to whole seconds before binding (`TruncateSeconds`, copied from `Data/DraftAdmissionRepository.cs:128`).
- **Table creation pattern:** `IF OBJECT_ID(N'Name', N'U') IS NULL CREATE TABLE ...`, executed via OleDb, mirroring `Data/DraftAdmissionRepository.cs:24-42`.
- **Namespaces:** root is `kingdom_Preparatory_School_Management_System`; data classes in `.Data`, services in `.Services`, models in `.Models`, shared/static in `.Common`.
- **Money:** SQL `MONEY` columns ↔ C# `decimal` (read with `Convert.ToDecimal`).
- All commits use the repo's footer:
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`

## File structure (what gets created/modified)

- **Create** `Models/SchoolInformation.cs` — full identity + fee model.
- **Create** `Data/ISchoolInfoRepository.cs` — repository contract.
- **Create** `Data/SchoolInfoRepository.cs` — table create/seed + CRUD via OleDb.
- **Create** `Common/SchoolProfile.cs` — static fail-safe cached accessor.
- **Create** `frmSchoolInfo.cs` — settings form (Director/Administrator).
- **Modify** `Common/AdmissionFees.cs` — `Amount` delegates to `SchoolProfile`.
- **Modify** `Services/StudentService.cs:220-248` — `GetFeeForClass` delegates to `SchoolProfile`.
- **Modify** `Services/ReportCardDataService.cs:71` — populate `SchoolInfo` from `SchoolProfile`.
- **Modify** `Services/AuthService.cs:72` — add `frmSchoolInfo` RBAC entry.
- **Modify** `frmDashboard.cs` — add "School Information" nav for Director/Administrator.
- **Modify** `kingdom_Preparatory_School_Management_System.csproj` — register the 5 new `.cs` files (project uses explicit `<Compile Include>` items; new files MUST be added or they will not compile).

---

### Task 1: `SchoolInformation` model

**Files:**
- Create: `Models/SchoolInformation.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create the model**

Create `Models/SchoolInformation.cs`:

```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// School identity + admission fee, persisted as a single row (Id = 1) in the
    /// SchoolInformation table. Source of truth for what was previously hardcoded.
    /// </summary>
    public class SchoolInformation
    {
        public string Name { get; set; } = "KINGDOM PREPARATORY SCHOOL";
        public string Address { get; set; } = "AKIM ODA- ABENASE";
        public string PoBox { get; set; } = "P. O. BOX 7 AKIM ODA";
        public string GpsAddress { get; set; } = "";          // Ghana Post GPS, e.g. AO-1234-5678
        public string Phone1 { get; set; } = "0548050141";
        public string Phone2 { get; set; } = "0246087609";
        public string Email { get; set; } = "noreply@kingdomprep.edu.gh";
        public byte[] Logo { get; set; }                        // null => fall back to Resources/school_logo.png
        public decimal AdmissionFee { get; set; } = 100m;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        /// <summary>"0548050141 / 0246087609" with blanks dropped.</summary>
        public string Phones
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Phone1)) return Phone2 ?? "";
                if (string.IsNullOrWhiteSpace(Phone2)) return Phone1 ?? "";
                return Phone1 + " / " + Phone2;
            }
        }
    }
}
```

- [ ] **Step 2: Register the file in the project**

In `kingdom_Preparatory_School_Management_System.csproj`, find an existing model compile entry (e.g. `<Compile Include="Models\SchoolInfo.cs" />`) and add directly after it:

```xml
    <Compile Include="Models\SchoolInformation.cs" />
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Models/SchoolInformation.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(school-info): SchoolInformation model (identity + admission fee + GP address)"
```

---

### Task 2: Repository — table create/seed + CRUD

**Files:**
- Create: `Data/ISchoolInfoRepository.cs`
- Create: `Data/SchoolInfoRepository.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

This task seeds `ClassFees` from the **current** `GetFeeForClass` values. Those values (from `Services/StudentService.cs:220-248`) are: CRECHE 2000, NURSERY 1 3450, NURSERY 2 3750, KINDERGARTEN 1 3654, KINDERGARTEN 2 / BASIC 1–9 2423, anything else 1200. The repository computes them with a private `LegacyFeeForClass` helper so the seed always matches today's behavior.

- [ ] **Step 1: Create the interface**

Create `Data/ISchoolInfoRepository.cs`:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ISchoolInfoRepository
    {
        Task EnsureTablesAsync();
        Task<SchoolInformation> GetAsync();
        Task<Dictionary<string, decimal>> GetClassFeesAsync();
        Task SaveAsync(SchoolInformation info);
        Task SaveClassFeesAsync(IDictionary<string, decimal> fees);
    }
}
```

- [ ] **Step 2: Create the repository**

Create `Data/SchoolInfoRepository.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Persists school identity (single row, Id = 1) and per-class term fees.
    /// Tables are created and seeded from the current hardcoded defaults on first use.
    /// SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class SchoolInfoRepository : ISchoolInfoRepository
    {
        private readonly string _connectionString;

        public SchoolInfoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();

                const string createInfo = @"IF OBJECT_ID(N'SchoolInformation', N'U') IS NULL
                    CREATE TABLE SchoolInformation (
                        Id INT NOT NULL PRIMARY KEY,
                        Name NVARCHAR(200), Address NVARCHAR(250), PoBox NVARCHAR(100),
                        GpsAddress NVARCHAR(50), Phone1 NVARCHAR(50), Phone2 NVARCHAR(50),
                        Email NVARCHAR(150), Logo VARBINARY(MAX), AdmissionFee MONEY,
                        UpdatedDate DATETIME);";
                using (var cmd = new OleDbCommand(createInfo, c)) await cmd.ExecuteNonQueryAsync();

                const string createFees = @"IF OBJECT_ID(N'ClassFees', N'U') IS NULL
                    CREATE TABLE ClassFees (
                        ClassName NVARCHAR(50) NOT NULL PRIMARY KEY,
                        TermFee MONEY);";
                using (var cmd = new OleDbCommand(createFees, c)) await cmd.ExecuteNonQueryAsync();

                // Seed identity row if absent.
                bool hasInfo;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM SchoolInformation WHERE Id = 1", c))
                    hasInfo = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                if (!hasInfo)
                {
                    var d = new SchoolInformation();
                    const string ins = @"INSERT INTO SchoolInformation
                        (Id,Name,Address,PoBox,GpsAddress,Phone1,Phone2,Email,Logo,AdmissionFee,UpdatedDate)
                        VALUES (1,?,?,?,?,?,?,?,?,?,?)";
                    using (var cmd = new OleDbCommand(ins, c))
                    {
                        cmd.Parameters.AddWithValue("?", d.Name);
                        cmd.Parameters.AddWithValue("?", d.Address);
                        cmd.Parameters.AddWithValue("?", d.PoBox);
                        cmd.Parameters.AddWithValue("?", d.GpsAddress);
                        cmd.Parameters.AddWithValue("?", d.Phone1);
                        cmd.Parameters.AddWithValue("?", d.Phone2);
                        cmd.Parameters.AddWithValue("?", d.Email);
                        cmd.Parameters.Add("?", OleDbType.VarBinary).Value = DBNull.Value;
                        cmd.Parameters.AddWithValue("?", d.AdmissionFee);
                        cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Seed per-class fees for any class not yet present.
                foreach (var className in AppConfig.ClassNames)
                {
                    bool exists;
                    using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM ClassFees WHERE ClassName = ?", c))
                    {
                        cmd.Parameters.AddWithValue("?", className);
                        exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    if (!exists)
                    {
                        using (var cmd = new OleDbCommand("INSERT INTO ClassFees (ClassName, TermFee) VALUES (?, ?)", c))
                        {
                            cmd.Parameters.AddWithValue("?", className);
                            cmd.Parameters.AddWithValue("?", LegacyFeeForClass(className));
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        public async Task<SchoolInformation> GetAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT * FROM SchoolInformation WHERE Id = 1", c))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync()) return new SchoolInformation(); // defaults
                    return new SchoolInformation
                    {
                        Name = AsString(r["Name"]),
                        Address = AsString(r["Address"]),
                        PoBox = AsString(r["PoBox"]),
                        GpsAddress = AsString(r["GpsAddress"]),
                        Phone1 = AsString(r["Phone1"]),
                        Phone2 = AsString(r["Phone2"]),
                        Email = AsString(r["Email"]),
                        Logo = r["Logo"] as byte[],
                        AdmissionFee = Convert.ToDecimal(r["AdmissionFee"]),
                        UpdatedDate = Convert.ToDateTime(r["UpdatedDate"])
                    };
                }
            }
        }

        public async Task<Dictionary<string, decimal>> GetClassFeesAsync()
        {
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT ClassName, TermFee FROM ClassFees", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        map[AsString(r["ClassName"])] = Convert.ToDecimal(r["TermFee"]);
            }
            return map;
        }

        public async Task SaveAsync(SchoolInformation info)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE SchoolInformation SET
                    Name=?, Address=?, PoBox=?, GpsAddress=?, Phone1=?, Phone2=?, Email=?,
                    Logo=?, AdmissionFee=?, UpdatedDate=? WHERE Id = 1";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", info.Name ?? "");
                    cmd.Parameters.AddWithValue("?", info.Address ?? "");
                    cmd.Parameters.AddWithValue("?", info.PoBox ?? "");
                    cmd.Parameters.AddWithValue("?", info.GpsAddress ?? "");
                    cmd.Parameters.AddWithValue("?", info.Phone1 ?? "");
                    cmd.Parameters.AddWithValue("?", info.Phone2 ?? "");
                    cmd.Parameters.AddWithValue("?", info.Email ?? "");
                    cmd.Parameters.Add("?", OleDbType.VarBinary).Value =
                        (object)info.Logo ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("?", info.AdmissionFee);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0) // row missing somehow — ensure then retry once
                    {
                        await EnsureTablesAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task SaveClassFeesAsync(IDictionary<string, decimal> fees)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                foreach (var kv in fees)
                {
                    using (var upd = new OleDbCommand("UPDATE ClassFees SET TermFee = ? WHERE ClassName = ?", c))
                    {
                        upd.Parameters.AddWithValue("?", kv.Value);
                        upd.Parameters.AddWithValue("?", kv.Key);
                        if (await upd.ExecuteNonQueryAsync() == 0)
                        {
                            using (var ins = new OleDbCommand("INSERT INTO ClassFees (ClassName, TermFee) VALUES (?, ?)", c))
                            {
                                ins.Parameters.AddWithValue("?", kv.Key);
                                ins.Parameters.AddWithValue("?", kv.Value);
                                await ins.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
        }

        // SQL 'datetime' rejects sub-second precision from MSOLEDBSQL; drop it.
        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

        private static string AsString(object o) => o == null || o == DBNull.Value ? "" : o.ToString();

        // Mirrors the legacy StudentService.GetFeeForClass switch so the seed matches today.
        public static decimal LegacyFeeForClass(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId)) return 1200m;
            switch (classId.Trim().ToUpperInvariant())
            {
                case "CRECHE": return 2000m;
                case "NURSERY 1": return 3450m;
                case "NURSERY 2": return 3750m;
                case "KINDERGARTEN 1": return 3654m;
                case "KINDERGARTEN 2":
                case "BASIC 1":
                case "BASIC 2":
                case "BASIC 3":
                case "BASIC 4":
                case "BASIC 5":
                case "BASIC 6":
                case "BASIC 7":
                case "BASIC 8":
                case "BASIC 9": return 2423m;
                default: return 1200m;
            }
        }
    }
}
```

- [ ] **Step 3: Register both files in the project**

In `kingdom_Preparatory_School_Management_System.csproj`, after an existing `<Compile Include="Data\...` entry (e.g. `Data\DraftAdmissionRepository.cs`) add:

```xml
    <Compile Include="Data\ISchoolInfoRepository.cs" />
    <Compile Include="Data\SchoolInfoRepository.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors.

- [ ] **Step 5: Runtime probe (DB) — verify seed values**

Create a throwaway script `tmp_probe_schoolinfo.ps1` at the repo root:

```powershell
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
$bin = Join-Path $PSScriptRoot 'bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($s,$e)
  $n = (New-Object Reflection.AssemblyName($e.Name)).Name
  $p = Join-Path $bin "$n.dll"; if (Test-Path $p) { [Reflection.Assembly]::LoadFrom($p) } else { $null } })
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$cs  = $asm.GetType('kingdom_Preparatory_School_Management_System.Common.AppConfig')::ConnectionString
$repoT = $asm.GetType('kingdom_Preparatory_School_Management_System.Data.SchoolInfoRepository')
$repo  = [Activator]::CreateInstance($repoT, @($cs))
$repoT.GetMethod('EnsureTablesAsync').Invoke($repo, $null).GetAwaiter().GetResult()
$fees  = $repoT.GetMethod('GetClassFeesAsync').Invoke($repo, $null).GetAwaiter().GetResult()
$info  = $repoT.GetMethod('GetAsync').Invoke($repo, $null).GetAwaiter().GetResult()
"CRECHE     = $($fees['CRECHE'])"
"BASIC 9    = $($fees['BASIC 9'])"
"NURSERY 1  = $($fees['NURSERY 1'])"
"AdmissionFee = $($info.AdmissionFee)"
"Name         = $($info.Name)"
```

Run: `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tmp_probe_schoolinfo.ps1`
Expected output includes:
```
CRECHE     = 2000
BASIC 9    = 2423
NURSERY 1  = 3450
AdmissionFee = 100
Name         = KINGDOM PREPARATORY SCHOOL
```
If LocalDB will not start in this shell, mark this probe **deferred to the user** and proceed (build-clean already passed). Delete the script after: `Remove-Item tmp_probe_schoolinfo.ps1`.

- [ ] **Step 6: Commit**

```bash
git add Data/ISchoolInfoRepository.cs Data/SchoolInfoRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(school-info): repository with table create + seed from current defaults"
```

---

### Task 3: `SchoolProfile` static cached accessor

**Files:**
- Create: `Common/SchoolProfile.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create the accessor**

Create `Common/SchoolProfile.cs`:

```csharp
using System;
using System.Collections.Generic;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Single source of truth for school identity + fees, read synchronously by the
    /// (formerly hardcoded) call sites. Loads once from the database and caches; any DB
    /// error falls back to the model defaults so the app never breaks. Call Refresh()
    /// after saving settings.
    /// </summary>
    public static class SchoolProfile
    {
        private static readonly object _lock = new object();
        private static SchoolInformation _info;
        private static Dictionary<string, decimal> _fees;

        private static void EnsureLoaded()
        {
            if (_info != null && _fees != null) return;
            lock (_lock)
            {
                if (_info != null && _fees != null) return;
                try
                {
                    var repo = new SchoolInfoRepository(AppConfig.ConnectionString);
                    repo.EnsureTablesAsync().GetAwaiter().GetResult();
                    _info = repo.GetAsync().GetAwaiter().GetResult();
                    _fees = repo.GetClassFeesAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("SchoolProfile load failed; using defaults", ex);
                    _info = _info ?? new SchoolInformation();
                    _fees = _fees ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock) { _info = null; _fees = null; }
        }

        private static SchoolInformation Info { get { EnsureLoaded(); return _info; } }

        public static string Name => Info.Name;
        public static string Address => Info.Address;
        public static string PoBox => Info.PoBox;
        public static string GpsAddress => Info.GpsAddress;
        public static string Phone1 => Info.Phone1;
        public static string Phone2 => Info.Phone2;
        public static string Phones => Info.Phones;
        public static string Email => Info.Email;
        public static byte[] Logo => Info.Logo;
        public static decimal AdmissionFee => Info.AdmissionFee;

        public static decimal FeeForClass(string classId)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(classId) &&
                _fees.TryGetValue(classId.Trim(), out var fee))
                return fee;
            return SchoolInfoRepository.LegacyFeeForClass(classId); // fallback for unknown class
        }
    }
}
```

- [ ] **Step 2: Register the file in the project**

In `kingdom_Preparatory_School_Management_System.csproj`, after `<Compile Include="Common\AdmissionFees.cs" />` (or any `Common\` entry) add:

```xml
    <Compile Include="Common\SchoolProfile.cs" />
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Common/SchoolProfile.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(school-info): SchoolProfile fail-safe cached accessor"
```

---

### Task 4: Retire the hardcodes (integration)

**Files:**
- Modify: `Common/AdmissionFees.cs`
- Modify: `Services/StudentService.cs:220-248`
- Modify: `Services/ReportCardDataService.cs:71`

- [ ] **Step 1: Delegate the admission fee**

Replace the body of `Common/AdmissionFees.cs` so `Amount` reads from `SchoolProfile`:

```csharp
namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// One-time admission fee. Reads from School Information settings (SchoolProfile),
    /// falling back to the model default (100) when settings are unavailable.
    /// </summary>
    public static class AdmissionFees
    {
        public static decimal Amount => SchoolProfile.AdmissionFee;
    }
}
```

- [ ] **Step 2: Delegate per-class fees**

In `Services/StudentService.cs`, replace the entire `GetFeeForClass` method (currently lines 220-248) with:

```csharp
        public decimal GetFeeForClass(string classId)
        {
            return Common.SchoolProfile.FeeForClass(classId);
        }
```

- [ ] **Step 3: Populate report-card SchoolInfo from settings**

In `Services/ReportCardDataService.cs`, replace `SchoolInfo = new SchoolInfo()` (line 71) with:

```csharp
                    SchoolInfo = new SchoolInfo
                    {
                        Name = Common.SchoolProfile.Name,
                        Location = Common.SchoolProfile.Address,
                        PhoneNumbers = Common.SchoolProfile.Phones,
                        Logo = Common.SchoolProfile.Logo
                    }
```

(The report-card generator already falls back to `Resources/school_logo.png` when `Logo` is null, so a school that hasn't uploaded a logo still prints correctly.)

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Common/AdmissionFees.cs Services/StudentService.cs Services/ReportCardDataService.cs
git commit -m "feat(school-info): drive admission fee, class fees, report-card identity from settings"
```

---

### Task 5: RBAC entry for the settings form

**Files:**
- Modify: `Services/AuthService.cs:72`

- [ ] **Step 1: Add the access rule**

In `Services/AuthService.cs`, in the `_formAccess` dictionary, immediately after the
`["frmEmailSettings"] = new[] { UserRole.Director, UserRole.Administrator },` line (line 72) add:

```csharp
            ["frmSchoolInfo"]             = new[] { UserRole.Director, UserRole.Administrator },
```

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Services/AuthService.cs
git commit -m "feat(school-info): RBAC for frmSchoolInfo (Director + Administrator)"
```

---

### Task 6: `frmSchoolInfo` settings form

**Files:**
- Create: `frmSchoolInfo.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create the form**

Create `frmSchoolInfo.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
    /// School Information settings: identity (name, address, P.O. Box, GP address, phones,
    /// email, logo) and fees (admission fee + per-class term fees). Director/Administrator.
    /// Saving persists to the database and refreshes the SchoolProfile cache.
    /// </summary>
    public class frmSchoolInfo : Form
    {
        private readonly SchoolInfoRepository _repo = new SchoolInfoRepository(AppConfig.ConnectionString);

        private TextBox _name, _address, _poBox, _gps, _phone1, _phone2, _email, _admissionFee;
        private PictureBox _logo;
        private byte[] _logoBytes;
        private DataGridView _feeGrid;
        private Button _saveBtn, _cancelBtn, _uploadBtn;
        private Label _status;

        public frmSchoolInfo()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmSchoolInfo", this)) return;
            Load += async (s, e) => await LoadAsync();
        }

        private void BuildUi()
        {
            Text = "School Information";
            Size = new Size(640, 760);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowIcon = false;
            BackColor = AppConfig.Colors.PageBackColor;
            AutoScroll = true;

            var title = new Label
            {
                Text = "  School Information", Dock = DockStyle.Top, Height = 44,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };

            int y = 56, lblX = 18, boxX = 170, boxW = 420, rowH = 32, gap = 8;
            Func<string, TextBox> addRow = caption =>
            {
                var l = new Label { Text = caption, Left = lblX, Top = y + 4, Width = boxX - lblX - 6, Font = new Font("Segoe UI", 10F) };
                var t = new TextBox { Left = boxX, Top = y, Width = boxW, Font = new Font("Segoe UI", 10F) };
                Controls.Add(l); Controls.Add(t);
                y += rowH + gap;
                return t;
            };

            Controls.Add(title);
            var idHdr = new Label { Text = "Identity", Left = lblX, Top = y, Width = 300, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppConfig.Colors.PrimaryColor };
            Controls.Add(idHdr); y += 28;

            _name = addRow("School Name");
            _address = addRow("Address");
            _poBox = addRow("P. O. Box");
            _gps = addRow("GP Address (Ghana Post GPS)");
            _phone1 = addRow("Phone 1");
            _phone2 = addRow("Phone 2");
            _email = addRow("Email");

            // Logo
            var logoLbl = new Label { Text = "Logo", Left = lblX, Top = y + 4, Width = boxX - lblX - 6, Font = new Font("Segoe UI", 10F) };
            _logo = new PictureBox { Left = boxX, Top = y, Width = 96, Height = 96, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
            _uploadBtn = new Button { Text = "Upload Logo…", Left = boxX + 110, Top = y + 30, Width = 130, Height = 32, FlatStyle = FlatStyle.Flat };
            _uploadBtn.Click += (s, e) => UploadLogo();
            Controls.Add(logoLbl); Controls.Add(_logo); Controls.Add(_uploadBtn);
            y += 96 + gap;

            // Fees
            var feeHdr = new Label { Text = "Fees", Left = lblX, Top = y, Width = 300, Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppConfig.Colors.PrimaryColor };
            Controls.Add(feeHdr); y += 28;
            _admissionFee = addRow("Admission Fee (GHS)");

            var feeGridLbl = new Label { Text = "Per-Class Term Fees (GHS)", Left = lblX, Top = y, Width = 400, Font = new Font("Segoe UI", 10F) };
            Controls.Add(feeGridLbl); y += 26;
            _feeGrid = new DataGridView
            {
                Left = lblX, Top = y, Width = boxW + boxX - lblX, Height = 200,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
            };
            _feeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Class", HeaderText = "Class", ReadOnly = true });
            _feeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TermFee", HeaderText = "Term Fee" });
            Controls.Add(_feeGrid); y += _feeGrid.Height + gap;

            _status = new Label { Left = lblX, Top = y, Width = boxW + boxX, Height = 22, ForeColor = AppConfig.Colors.MutedTextColor };
            Controls.Add(_status); y += 28;

            _saveBtn = new Button { Text = "Save", Left = boxX, Top = y, Width = 130, Height = 38, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cancelBtn = new Button { Text = "Cancel", Left = boxX + 140, Top = y, Width = 110, Height = 38, FlatStyle = FlatStyle.Flat };
            _saveBtn.Click += async (s, e) => await SaveAsync();
            _cancelBtn.Click += (s, e) => Close();
            Controls.Add(_saveBtn); Controls.Add(_cancelBtn);
        }

        private async Task LoadAsync()
        {
            try
            {
                await _repo.EnsureTablesAsync();
                var info = await _repo.GetAsync();
                var fees = await _repo.GetClassFeesAsync();

                _name.Text = info.Name; _address.Text = info.Address; _poBox.Text = info.PoBox;
                _gps.Text = info.GpsAddress; _phone1.Text = info.Phone1; _phone2.Text = info.Phone2;
                _email.Text = info.Email; _admissionFee.Text = info.AdmissionFee.ToString("0.##");
                _logoBytes = info.Logo;
                SetLogoPreview(info.Logo);

                _feeGrid.Rows.Clear();
                foreach (var className in AppConfig.ClassNames)
                {
                    decimal fee = fees.TryGetValue(className, out var f) ? f : SchoolInfoRepository.LegacyFeeForClass(className);
                    _feeGrid.Rows.Add(className, fee.ToString("0.##"));
                }
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not load school information: " + ex.Message, "School Information");
            }
        }

        private void SetLogoPreview(byte[] bytes)
        {
            try
            {
                if (bytes != null && bytes.Length > 0)
                {
                    using (var ms = new MemoryStream(bytes)) _logo.Image = Image.FromStream(ms);
                    return;
                }
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "school_logo.png");
                if (File.Exists(path)) _logo.Image = Image.FromFile(path);
            }
            catch { /* preview is best-effort */ }
        }

        private void UploadLogo()
        {
            using (var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                if (!AppConfig.AllowedImageExtensions.Contains(ext))
                { UIHelper.ShowWarning("Unsupported image type.", "Logo"); return; }
                var bytes = File.ReadAllBytes(dlg.FileName);
                if (bytes.LongLength > AppConfig.MaxPhotoSizeBytes)
                { UIHelper.ShowWarning($"Logo must be under {AppConfig.MaxPhotoSizeMB} MB.", "Logo"); return; }
                _logoBytes = bytes;
                SetLogoPreview(bytes);
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            { UIHelper.ShowWarning("School name is required.", "School Information"); return; }
            if (!decimal.TryParse(_admissionFee.Text, out var admission) || admission < 0)
            { UIHelper.ShowWarning("Admission fee must be a number ≥ 0.", "School Information"); return; }

            var fees = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _feeGrid.Rows)
            {
                if (row.IsNewRow) continue;
                string cls = row.Cells["Class"].Value?.ToString();
                if (!decimal.TryParse(row.Cells["TermFee"].Value?.ToString(), out var fee) || fee < 0)
                { UIHelper.ShowWarning($"Term fee for {cls} must be a number ≥ 0.", "School Information"); return; }
                fees[cls] = fee;
            }

            _saveBtn.Enabled = false;
            try
            {
                var info = new SchoolInformation
                {
                    Name = _name.Text.Trim(), Address = _address.Text.Trim(), PoBox = _poBox.Text.Trim(),
                    GpsAddress = _gps.Text.Trim(), Phone1 = _phone1.Text.Trim(), Phone2 = _phone2.Text.Trim(),
                    Email = _email.Text.Trim(), Logo = _logoBytes, AdmissionFee = admission
                };
                await _repo.SaveAsync(info);
                await _repo.SaveClassFeesAsync(fees);
                SchoolProfile.Refresh();
                _status.Text = "Saved " + DateTime.Now.ToString("HH:mm:ss") + ".";
                UIHelper.ShowSuccess("School information saved.", "School Information");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError("Could not save: " + ex.Message, "School Information");
            }
            finally { _saveBtn.Enabled = true; }
        }
    }
}
```

- [ ] **Step 2: Register the file in the project**

In `kingdom_Preparatory_School_Management_System.csproj`, after the self-closing
`<Compile Include="frmPendingApprovals.cs" />` entry (a code-only form, the right pattern to
follow — do **not** add a `<SubType>` child; this form has no `.Designer.cs`/`.resx`) add:

```xml
    <Compile Include="frmSchoolInfo.cs" />
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors. (If `UIHelper.ShowSuccess/ShowWarning/ShowError` signatures differ, match the existing calls used in `frmPendingApprovals.cs`.)

- [ ] **Step 4: Render harness — verify layout**

Render `frmSchoolInfo` to PNG using the offline harness (per user memory `offline-winforms-render-harness.md`): set `AuthService.CurrentUser` backing field to a `UserSession { Role = Director }`, `Show()` the form off-screen, `DrawToBitmap` to `tmp_schoolinfo.png`. Open the PNG and confirm: title, Identity group (7 textboxes incl. GP Address), logo preview + Upload button, Admission Fee box, per-class fee grid populated with all 14 classes, Save/Cancel. If LocalDB is down (the `Load` handler needs it), the form still renders the static layout; the grid may be empty — that is acceptable for the layout check. Delete the PNG and any temp script afterward.

- [ ] **Step 5: Commit**

```bash
git add frmSchoolInfo.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(school-info): frmSchoolInfo settings form (identity, logo upload, per-class fees)"
```

---

### Task 7: Dashboard navigation entry

**Files:**
- Modify: `frmDashboard.cs`

The dashboard already has a "Settings" area/button for Director/Administrator that opens `frmEmailSettings` (added earlier this session). Add a sibling "School Information" entry next to it using the **same** nav-button helper and role guard already present in this file.

- [ ] **Step 1: Locate the existing Settings nav code**

Run: `grep -n "frmEmailSettings\|CreateNavButton\|Settings" frmDashboard.cs`
Identify the line where the Settings/`frmEmailSettings` nav button is created and the role check that guards it (Director/Administrator).

- [ ] **Step 2: Add the nav entry**

Immediately after the existing `frmEmailSettings` nav-button line, add an analogous entry. Use the exact helper signature found in Step 1. It will look like one of these (match whichever the file uses):

```csharp
            nav.Controls.Add(CreateNavButton("School Information", () => OpenForm(new frmSchoolInfo())));
```

Place it inside the same `if (role == UserRole.Director || role == UserRole.Administrator)` block that guards the existing Settings button so only those roles see it.

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: Build succeeds, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(school-info): add School Information to dashboard settings nav"
```

---

## Final verification (whole feature)

- [ ] **Build clean:** `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app, needs LocalDB):**
  1. Log in as Director/Administrator → open School Information from the dashboard.
  2. Confirm fields are pre-filled and the fee grid shows all 14 classes with the legacy values.
  3. Change a class's term fee + the admission fee, upload a logo, Save.
  4. Start a new admission/fee payment → confirm the changed admission fee and class fee appear.
  5. Generate a report card → confirm the configured name/address/phones and uploaded logo print.
  6. Reopen School Information → confirm the saved values (and logo) persist.

## Self-review notes

- **Spec coverage:** identity table (Task 2) ✓, GP address field (Tasks 1,2,6) ✓, ClassFees table + seed (Task 2) ✓, SchoolProfile cache + fail-safe (Task 3) ✓, retire AdmissionFees/GetFeeForClass/report-card hardcodes (Task 4) ✓, RBAC Director+Admin (Task 5) ✓, form with logo upload + fee grid (Task 6) ✓, dashboard nav (Task 7) ✓, abbreviation left in SMS settings (not touched — by design) ✓.
- **Type consistency:** `SchoolInformation` properties, `SchoolInfoRepository` method names (`EnsureTablesAsync/GetAsync/GetClassFeesAsync/SaveAsync/SaveClassFeesAsync`), `SchoolProfile` members (`Name/Address/PoBox/GpsAddress/Phones/Logo/AdmissionFee/FeeForClass/Refresh`), and `LegacyFeeForClass` (public static, reused by repo seed, SchoolProfile fallback, and the form) are consistent across tasks.
- **Project file:** every new `.cs` is added to the `.csproj` (this project uses explicit `<Compile Include>` — a new file omitted here silently won't build).
```
