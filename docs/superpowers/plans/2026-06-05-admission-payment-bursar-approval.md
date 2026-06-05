# Admission Payment with Bursar Approval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hold a new admission (student + initial payment) as a draft until the bursar approves; on approval, create the real student + payment records, print two receipts, and send the admission SMS — nothing touches the live tables or notifies before approval.

**Architecture:** A new `DraftAdmissions` table + `DraftAdmissionRepository`/`DraftAdmissionService` capture the pending admission (incl. photo + amounts). The admin's `frmAddStd` submits a draft and enters payment via `frmFessPayment` in a new admission mode. The bursar approves from a Pending tab + dashboard card; approval promotes the draft (reusing `StudentService`/`FeeRepository`), fires the SMS, prints two receipts, and deletes the draft.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, SQL Server LocalDB via OleDb (`Provider=MSOLEDBSQL`), existing receipt rendering (`ReceiptPrintData`/`DrawReceipt`).

---

## Verification note

No xUnit runner in the main project. Each task verifies via:
1. **Compile gate:** `dotnet build -clp:ErrorsOnly -nologo` → `0 Error(s)` (run from project root).
2. **Logic/data gate:** a PowerShell reflection harness (the project's offline-verification technique — see memory `offline-winforms-render-harness`) that loads the built exe and exercises the new service/repository against the live LocalDB, or renders a form. Templates appear in the relevant tasks.

`AppConfig.ConnectionString` points at `(localdb)\MSSQLLocalDB / Neat_Academy`. Use only `git add <named files>`; never `git add -A` (the tree has unrelated WIP).

---

## File Structure

- Create `Models/DraftAdmission.cs` — the draft DTO (student fields + payment + metadata).
- Create `Data/DraftAdmissionRepository.cs` (+ `Data/IDraftAdmissionRepository.cs`) — table setup + CRUD (Add, GetPending, GetById, Delete).
- Create `Services/DraftAdmissionService.cs` — validation (≥50%) + `CreateDraftAsync` + `ApproveAsync` (promotion) + `RejectAsync`.
- Create `Common/AdmissionFees.cs` — `Amount = 100m` constant.
- Modify `Services/SmsService.cs` — admission message variant with payment lines.
- Modify `Services/StudentService.cs` — remove the immediate admission SMS from `AddStudentAsync` (approval owns notifications).
- Modify `frmAddStd.cs` — "Submit for Approval" → create draft, open prefilled payment.
- Modify `frmFessPayment.cs` — admission mode (prefill + GHS100 + ≥50% validation + Submit-for-Approval) and a Pending Approvals tab (Approve/Reject + two receipts).
- Modify `frmDashboard.cs` — "Pending Admission Payments (N)" card for the Accountant.
- Modify `kingdom_Preparatory_School_Management_System.csproj` — add the new `.cs` files.

---

## Task 1: `DraftAdmissions` table + setup

**Files:** Create `Data/IDraftAdmissionRepository.cs`, `Data/DraftAdmissionRepository.cs`; Modify csproj.

- [ ] **Step 1: Interface** `Data/IDraftAdmissionRepository.cs`:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface IDraftAdmissionRepository
    {
        Task EnsureTableAsync();
        Task<int> AddAsync(DraftAdmission draft);
        Task<IEnumerable<DraftAdmission>> GetPendingAsync();
        Task<DraftAdmission> GetByIdAsync(int draftId);
        Task<bool> DeleteAsync(int draftId);
    }
}
```

- [ ] **Step 2: Repository with table setup** `Data/DraftAdmissionRepository.cs`. `EnsureTableAsync` creates the table if missing (T-SQL, SQL Server):
```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public class DraftAdmissionRepository : IDraftAdmissionRepository
    {
        private readonly string _connectionString;
        public DraftAdmissionRepository(string connectionString) { _connectionString = connectionString; }

        public async Task EnsureTableAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                string sql = @"IF OBJECT_ID(N'DraftAdmissions', N'U') IS NULL
                    CREATE TABLE DraftAdmissions (
                        DraftID INT IDENTITY(1,1) PRIMARY KEY,
                        FirstName NVARCHAR(100), LastName NVARCHAR(100), DOB DATETIME,
                        Gender NVARCHAR(20), ClassID NVARCHAR(50), Email NVARCHAR(150),
                        HomeTown NVARCHAR(150), Residence NVARCHAR(150), Allegies NVARCHAR(255),
                        EmergencyConatct NVARCHAR(50), GuidanceName NVARCHAR(150),
                        GuidianceEmail NVARCHAR(150), Guidiance_Location NVARCHAR(150),
                        admission_date DATETIME, Std_pic VARBINARY(MAX),
                        AdmissionFee MONEY, SchoolFeePaid MONEY, TermTotal MONEY,
                        PaymentMode NVARCHAR(50), SubmittedBy NVARCHAR(100), SubmittedDate DATETIME);";
                using (var cmd = new OleDbCommand(sql, c)) await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> AddAsync(DraftAdmission d)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                string sql = @"INSERT INTO DraftAdmissions
                    (FirstName,LastName,DOB,Gender,ClassID,Email,HomeTown,Residence,Allegies,
                     EmergencyConatct,GuidanceName,GuidianceEmail,Guidiance_Location,admission_date,Std_pic,
                     AdmissionFee,SchoolFeePaid,TermTotal,PaymentMode,SubmittedBy,SubmittedDate)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", d.FirstName ?? "");
                    cmd.Parameters.AddWithValue("?", d.LastName ?? "");
                    cmd.Parameters.AddWithValue("?", d.DateOfBirth);
                    cmd.Parameters.AddWithValue("?", d.Gender ?? "");
                    cmd.Parameters.AddWithValue("?", d.ClassID ?? "");
                    cmd.Parameters.AddWithValue("?", d.Email ?? "");
                    cmd.Parameters.AddWithValue("?", d.HomeTown ?? "");
                    cmd.Parameters.AddWithValue("?", d.Residence ?? "");
                    cmd.Parameters.AddWithValue("?", d.Allergies ?? "");
                    cmd.Parameters.AddWithValue("?", d.EmergencyContact ?? "");
                    cmd.Parameters.AddWithValue("?", d.GuardianName ?? "");
                    cmd.Parameters.AddWithValue("?", d.GuardianEmail ?? "");
                    cmd.Parameters.AddWithValue("?", d.GuardianLocation ?? "");
                    cmd.Parameters.AddWithValue("?", d.AdmissionDate);
                    cmd.Parameters.Add("?", OleDbType.VarBinary).Value = (object)d.ProfilePhoto ?? new byte[0];
                    cmd.Parameters.AddWithValue("?", d.AdmissionFee);
                    cmd.Parameters.AddWithValue("?", d.SchoolFeePaid);
                    cmd.Parameters.AddWithValue("?", d.TermTotal);
                    cmd.Parameters.AddWithValue("?", d.PaymentMode ?? "Cash");
                    cmd.Parameters.AddWithValue("?", d.SubmittedBy ?? "");
                    cmd.Parameters.AddWithValue("?", d.SubmittedDate);
                    await cmd.ExecuteNonQueryAsync();
                    using (var idCmd = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idCmd.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task<IEnumerable<DraftAdmission>> GetPendingAsync()
        {
            var list = new List<DraftAdmission>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT * FROM DraftAdmissions ORDER BY SubmittedDate", c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(Map(r));
            }
            return list;
        }

        public async Task<DraftAdmission> GetByIdAsync(int draftId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("SELECT * FROM DraftAdmissions WHERE DraftID = ?", c))
                {
                    cmd.Parameters.AddWithValue("?", draftId);
                    using (var r = await cmd.ExecuteReaderAsync())
                        return await r.ReadAsync() ? Map(r) : null;
                }
            }
        }

        public async Task<bool> DeleteAsync(int draftId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                using (var cmd = new OleDbCommand("DELETE FROM DraftAdmissions WHERE DraftID = ?", c))
                {
                    cmd.Parameters.AddWithValue("?", draftId);
                    return await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        private static DraftAdmission Map(IDataRecord r) => new DraftAdmission
        {
            DraftID = Convert.ToInt32(r["DraftID"]),
            FirstName = r["FirstName"]?.ToString(),
            LastName = r["LastName"]?.ToString(),
            DateOfBirth = Convert.ToDateTime(r["DOB"]),
            Gender = r["Gender"]?.ToString(),
            ClassID = r["ClassID"]?.ToString(),
            Email = r["Email"]?.ToString(),
            HomeTown = r["HomeTown"]?.ToString(),
            Residence = r["Residence"]?.ToString(),
            Allergies = r["Allegies"]?.ToString(),
            EmergencyContact = r["EmergencyConatct"]?.ToString(),
            GuardianName = r["GuidanceName"]?.ToString(),
            GuardianEmail = r["GuidianceEmail"]?.ToString(),
            GuardianLocation = r["Guidiance_Location"]?.ToString(),
            AdmissionDate = Convert.ToDateTime(r["admission_date"]),
            ProfilePhoto = r["Std_pic"] as byte[],
            AdmissionFee = Convert.ToDecimal(r["AdmissionFee"]),
            SchoolFeePaid = Convert.ToDecimal(r["SchoolFeePaid"]),
            TermTotal = Convert.ToDecimal(r["TermTotal"]),
            PaymentMode = r["PaymentMode"]?.ToString(),
            SubmittedBy = r["SubmittedBy"]?.ToString(),
            SubmittedDate = Convert.ToDateTime(r["SubmittedDate"])
        };
    }
}
```

- [ ] **Step 3: csproj** — add `<Compile Include="Data\IDraftAdmissionRepository.cs" />` and `<Compile Include="Data\DraftAdmissionRepository.cs" />`. (References `Models.DraftAdmission` from Task 2 — build after Task 2.)

- [ ] **Step 4: Commit** (build deferred to Task 2):
```bash
git add Data/IDraftAdmissionRepository.cs Data/DraftAdmissionRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(admission): DraftAdmissions table + repository"
```

---

## Task 2: `DraftAdmission` model + `AdmissionFees` constant + build

**Files:** Create `Models/DraftAdmission.cs`, `Common/AdmissionFees.cs`; Modify csproj.

- [ ] **Step 1:** `Models/DraftAdmission.cs`:
```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A pending admission held until the bursar approves payment.</summary>
    public class DraftAdmission
    {
        public int DraftID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ClassID { get; set; }
        public string Email { get; set; }
        public string HomeTown { get; set; }
        public string Residence { get; set; }
        public string Allergies { get; set; }
        public string GuardianName { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianLocation { get; set; }
        public string EmergencyContact { get; set; }
        public DateTime AdmissionDate { get; set; }
        public byte[] ProfilePhoto { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal SchoolFeePaid { get; set; }
        public decimal TermTotal { get; set; }
        public string PaymentMode { get; set; }
        public string SubmittedBy { get; set; }
        public DateTime SubmittedDate { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        /// <summary>Maps this draft to a Student for promotion on approval.</summary>
        public Student ToStudent() => new Student
        {
            FirstName = FirstName, LastName = LastName, DateOfBirth = DateOfBirth,
            Gender = Gender, ClassID = ClassID, Email = Email, HomeTown = HomeTown,
            Residence = Residence, Allergies = Allergies, GuardianName = GuardianName,
            GuardianEmail = GuardianEmail, GuardianLocation = GuardianLocation,
            EmergencyContact = EmergencyContact, AdmissionDate = AdmissionDate,
            ProfilePhoto = ProfilePhoto
        };
    }
}
```

- [ ] **Step 2:** `Common/AdmissionFees.cs`:
```csharp
namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Hardcoded admission fee until the school-info settings feature exists.
    /// </summary>
    public static class AdmissionFees
    {
        public static readonly decimal Amount = 100m;
    }
}
```

- [ ] **Step 3: csproj** — add `<Compile Include="Models\DraftAdmission.cs" />` and `<Compile Include="Common\AdmissionFees.cs" />`.

- [ ] **Step 4: Build** — `dotnet build -clp:ErrorsOnly -nologo` → `0 Error(s)` (Tasks 1+2 now compile together).

- [ ] **Step 5: Data gate** — run a reflection harness that calls `EnsureTableAsync` then `AddAsync`/`GetPendingAsync`/`DeleteAsync` against LocalDB and prints the round-tripped draft. Confirm the row inserts and deletes. (Template: load exe, `new DraftAdmissionRepository(AppConfig.ConnectionString)`, await EnsureTableAsync, AddAsync a sample, GetPendingAsync count ≥1, DeleteAsync, print "DRAFT REPO OK".)

- [ ] **Step 6: Commit**
```bash
git add Models/DraftAdmission.cs Common/AdmissionFees.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(admission): DraftAdmission model + AdmissionFees constant"
```

---

## Task 3: `DraftAdmissionService` — validation + create + approve + reject

**Files:** Create `Services/DraftAdmissionService.cs`; Modify csproj.

Dependencies it orchestrates on approval: `StudentService.AddStudentAsync` (creates Student + initial school-fee balance), `FeeRepository.AddPaymentRecordAsync` (records payments).

- [ ] **Step 1: Service** `Services/DraftAdmissionService.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class DraftAdmissionService
    {
        private readonly IDraftAdmissionRepository _drafts;
        private readonly StudentService _students;
        private readonly IFeeRepository _fees;

        public DraftAdmissionService(IDraftAdmissionRepository drafts, StudentService students, IFeeRepository fees)
        {
            _drafts = drafts; _students = students; _fees = fees;
        }

        /// <summary>Minimum school fee payable at admission: half the term total.</summary>
        public static decimal MinSchoolFee(decimal termTotal) => Math.Round(termTotal / 2m, 2);

        public (bool Ok, string Message) Validate(DraftAdmission d)
        {
            if (d.AdmissionFee < Common.AdmissionFees.Amount)
                return (false, $"Admission fee of GHS {Common.AdmissionFees.Amount:N2} is required.");
            if (d.SchoolFeePaid < MinSchoolFee(d.TermTotal))
                return (false, $"School fee must be at least 50% of GHS {d.TermTotal:N2} (GHS {MinSchoolFee(d.TermTotal):N2}).");
            return (true, "");
        }

        public async Task<(bool Ok, string Message)> CreateDraftAsync(DraftAdmission d)
        {
            var v = Validate(d);
            if (!v.Ok) return v;
            await _drafts.EnsureTableAsync();
            d.SubmittedDate = DateTime.Now;
            int id = await _drafts.AddAsync(d);
            return id > 0 ? (true, "Submitted for bursar approval.") : (false, "Could not save the draft admission.");
        }

        public Task<IEnumerable<DraftAdmission>> GetPendingAsync() => _drafts.GetPendingAsync();

        public Task<bool> RejectAsync(int draftId) => _drafts.DeleteAsync(draftId);

        /// <summary>
        /// Promotes a draft: creates the Student (+ initial school-fee balance), records
        /// the admission fee and the school-fee payment, deletes the draft, and returns
        /// the created student so the caller can print receipts + send the SMS.
        /// </summary>
        public async Task<(bool Ok, string Message, Student Student)> ApproveAsync(int draftId, string bursarName)
        {
            var d = await _drafts.GetByIdAsync(draftId);
            if (d == null) return (false, "Draft not found (already processed?).", null);

            var student = d.ToStudent();
            var add = await _students.AddStudentAsync(student); // creates Students row + initial school-fee balance, assigns StudentID
            if (!add.Success) return (false, "Promotion failed: " + add.Message, null);

            decimal schoolBalanceAfter = Math.Max(0m, d.TermTotal - d.SchoolFeePaid);

            // Admission fee row first (does not change the school balance): carry the current school balance.
            await _fees.AddPaymentRecordAsync(student.StudentID, student.ClassID, student.FullName,
                d.AdmissionFee, d.TermTotal, "Admission Fee", bursarName, DateTime.Today);

            // School-fee payment row LAST so GetLatestBalanceAsync returns the school balance.
            await _fees.AddPaymentRecordAsync(student.StudentID, student.ClassID, student.FullName,
                d.SchoolFeePaid, schoolBalanceAfter, d.PaymentMode ?? "Cash", bursarName, DateTime.Today);

            await _drafts.DeleteAsync(draftId);
            return (true, "Approved.", student);
        }
    }
}
```

- [ ] **Step 2: csproj** — add `<Compile Include="Services\DraftAdmissionService.cs" />`.

- [ ] **Step 3: Build** → `0 Error(s)`.

- [ ] **Step 4: Logic gate (validation)** — reflection harness: call `DraftAdmissionService.MinSchoolFee(2423)` → expect `1211.5`; `Validate` on a draft with SchoolFeePaid below half → `Ok=false`; at/above half with AdmissionFee 100 → `Ok=true`. Print `VALIDATION OK`.

- [ ] **Step 5: Commit**
```bash
git add Services/DraftAdmissionService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(admission): DraftAdmissionService (validate/create/approve/reject)"
```

---

## Task 4: SMS message variant with payment lines

**Files:** Modify `Services/SmsService.cs`.

- [ ] **Step 1:** Add an overload that appends the payment lines. Insert after the existing `BuildStudentAdmissionMessage`:
```csharp
        /// <summary>
        /// Admission message including the bursar-approved payment lines.
        /// </summary>
        public static string BuildStudentAdmissionMessage(Models.Student s, decimal admissionFeePaid, decimal schoolFeePaid, decimal termTotal)
        {
            string guardian = string.IsNullOrWhiteSpace(s.GuardianName) ? "Guardian" : s.GuardianName.Trim();
            return
$@"Dear {guardian}, your ward {s.FirstName} has been admitted to Kingdom Preparatory School with the following details:
- Student ID: {s.StudentID}
- Name: {s.FullName}
- Class: {s.ClassID}
- Gender: {s.Gender}
- Date of Birth: {s.DateOfBirth:dd/MM/yyyy}
- Admission Date: {s.AdmissionDate:dd/MM/yyyy}
- Admission fee paid: GHS {admissionFeePaid:N2}
- School fee paid: GHS {schoolFeePaid:N2} out of GHS {termTotal:N2}

To rectify any details or information, kindly visit or contact the school administrator. Thank you.
- Administration";
        }

        public static Task<(bool Success, string Message)> SendStudentAdmissionAsync(
            string recipient, Models.Student student, decimal admissionFeePaid, decimal schoolFeePaid, decimal termTotal)
        {
            return SendSmsAsync(recipient,
                BuildStudentAdmissionMessage(student, admissionFeePaid, schoolFeePaid, termTotal),
                SmsSenderIds.StudentAdmission);
        }
```

- [ ] **Step 2: Build** → `0 Error(s)`.

- [ ] **Step 3: Render gate** — reflection harness builds the message with sample student + amounts; confirm it shows the two payment lines. Print the message.

- [ ] **Step 4: Commit**
```bash
git add Services/SmsService.cs
git commit -m "feat(admission): SMS admission message variant with payment lines"
```

---

## Task 5: Remove the immediate admission notifications from `StudentService.AddStudentAsync`

**Files:** Modify `Services/StudentService.cs`.

Admissions now go through drafts; the approval path sends the SMS (Task 8). The email is also deferred to approval. Remove the notification block from `AddStudentAsync` (the `GuardianEmail` and `EmergencyContact` notification ifs added previously), leaving the student + initial fee creation intact.

- [ ] **Step 1:** Delete the two notification `if` blocks in `AddStudentAsync` (the `SendEmailAsync` + `SendStudentAdmissionAsync` calls). Keep the fee-record creation and the `return (true, ...)`.

- [ ] **Step 2: Build** → `0 Error(s)`. (No callers of the old 2-arg `SendStudentAdmissionAsync` remain in StudentService.)

- [ ] **Step 3: Commit**
```bash
git add Services/StudentService.cs
git commit -m "refactor(admission): defer admission notifications to bursar approval"
```

---

## Task 6: `frmAddStd` — Submit for Approval → draft + open payment

**Files:** Modify `frmAddStd.cs`. READ `SaveStudent()` (~line 954) and `MapFormToStudent()` (~line 908) first.

- [ ] **Step 1:** In `SaveStudent()`, for a **new** admission (the `isNew` branch), replace the `AddStudentAsync` call path with: build a `DraftAdmission` from the form, then open `frmFessPayment` in admission mode passing the draft. Concretely, after `var student = MapFormToStudent();` for the new-student case:
```csharp
                // Admission no longer saves directly; collect payment, then submit a draft for the bursar.
                var draft = new Models.DraftAdmission
                {
                    FirstName = student.FirstName, LastName = student.LastName,
                    DateOfBirth = student.DateOfBirth, Gender = student.Gender, ClassID = student.ClassID,
                    Email = student.Email, HomeTown = student.HomeTown, Residence = student.Residence,
                    Allergies = student.Allergies, GuardianName = student.GuardianName,
                    GuardianEmail = student.GuardianEmail, GuardianLocation = student.GuardianLocation,
                    EmergencyContact = student.EmergencyContact, AdmissionDate = student.AdmissionDate,
                    ProfilePhoto = student.ProfilePhoto,
                    TermTotal = _studentService.GetFeeForClass(student.ClassID),
                    AdmissionFee = Common.AdmissionFees.Amount,
                    SubmittedBy = AuthService.CurrentUser.Username
                };
                using (var pay = new frmFessPayment(draft))
                {
                    pay.ShowDialog();
                }
                ClearStudentDetails();
                txtStdID.Text = "";
                return;
```
Keep the existing **update** branch (editing an existing student) calling `UpdateStudentAsync` as-is.

- [ ] **Step 2: Build** — will fail until `frmFessPayment(DraftAdmission)` ctor exists (Task 7). Proceed to Task 7, then build.

- [ ] **Step 3: Commit** (after Task 7 builds):
```bash
git add frmAddStd.cs
git commit -m "feat(admission): frmAddStd submits a draft + opens payment for approval"
```

---

## Task 7: `frmFessPayment` admission mode (prefill + GHS100 + ≥50% + Submit)

**Files:** Modify `frmFessPayment.cs`. READ the constructor, `LookupStudent`, the amount/being fields, and `RecordPayment` first.

- [ ] **Step 1:** Add a draft field + an admission-mode constructor:
```csharp
        private Models.DraftAdmission _admissionDraft;

        public frmFessPayment(Models.DraftAdmission draft) : this()
        {
            _admissionDraft = draft;
            EnterAdmissionMode();
        }
```

- [ ] **Step 2:** Add `EnterAdmissionMode()` that prefills the wizard from the draft (student name, class, balance = TermTotal), shows the admission fee (GHS 100, read-only label), seeds the amount box with the ≥50% minimum, and changes the primary action to **"Submit for Approval"**:
```csharp
        private void EnterAdmissionMode()
        {
            if (studentNameBox != null) studentNameBox.Text = _admissionDraft.FullName;
            if (classBox != null)       classBox.Text = _admissionDraft.ClassID;
            if (balanceBox != null)     balanceBox.Text = _admissionDraft.TermTotal.ToString("0.00");
            if (beingBox != null)       beingBox.Text = "Admission - School Fees";
            if (amountBox != null)      amountBox.Text = Services.DraftAdmissionService.MinSchoolFee(_admissionDraft.TermTotal).ToString("0.00");
            if (_continueToPaymentBtn != null) _continueToPaymentBtn.Text = "Continue";
            if (_recordBtn != null)     _recordBtn.Text = "Submit for Approval";
        }
```
(Adjust control names to the real ones found when reading the file; `_recordBtn` = the Record-Payment button.)

- [ ] **Step 3:** In the record/submit handler, when `_admissionDraft != null`, branch to a `SubmitAdmissionDraftAsync()` instead of the normal `RecordPayment()`:
```csharp
        private async Task SubmitAdmissionDraftAsync()
        {
            if (!decimal.TryParse(amountBox.Text, out decimal schoolPaid)) { UIHelper.ShowError("Enter a valid school-fee amount.", "Admission"); return; }
            _admissionDraft.SchoolFeePaid = schoolPaid;
            _admissionDraft.AdmissionFee = Common.AdmissionFees.Amount;
            _admissionDraft.PaymentMode = string.IsNullOrWhiteSpace(paymentModeBox?.Text) ? "Cash" : paymentModeBox.Text;

            var svc = new Services.DraftAdmissionService(
                new Data.DraftAdmissionRepository(Common.AppConfig.ConnectionString),
                new Services.StudentService(new Data.StudentRepository(Common.AppConfig.ConnectionString), _feeRepository),
                _feeRepository);

            var result = await svc.CreateDraftAsync(_admissionDraft);
            if (!result.Ok) { UIHelper.ShowError(result.Message, "Admission"); return; }
            UIHelper.ShowSuccess("Submitted for bursar approval. The SMS and receipts will be sent after approval.", "Admission");
            Close();
        }
```
Wire the record button: `if (_admissionDraft != null) { _ = SubmitAdmissionDraftAsync(); return; }` at the top of the existing record handler. (`_feeRepository` is the form's existing fee repo field; confirm its name when reading.)

- [ ] **Step 4: Build** (with Task 6) → `0 Error(s)`.

- [ ] **Step 5: Visual gate** — offline-render `frmFessPayment` constructed with a sample draft; confirm name/class/balance prefilled, "Submit for Approval" button text, amount seeded to half the term total.

- [ ] **Step 6: Commit**
```bash
git add frmFessPayment.cs frmAddStd.cs
git commit -m "feat(admission): frmFessPayment admission mode (prefill + 50% + submit draft)"
```

---

## Task 8: Pending Approvals tab + approve (promote, 2 receipts, SMS)

**Files:** Modify `frmFessPayment.cs`. Uses `BuildReceiptPrintData`/`DrawReceipt`/`PrintReceiptPreview` and `ReceiptPrintData`.

- [ ] **Step 1:** Add a **"Pending Approvals"** view (a secondary panel or a simple modal list `frmPendingApprovals` — choose a `DataGridView` of pending drafts: name, class, admission fee, school fee paid, term total, submitted by/date, with **Approve** and **Reject** buttons). Bind from `DraftAdmissionService.GetPendingAsync()`.

- [ ] **Step 2: Approve handler** — on Approve of the selected draft:
```csharp
        private async Task ApproveDraftAsync(int draftId)
        {
            string bursar = AuthService.CurrentUser.Username;
            var svc = new Services.DraftAdmissionService(
                new Data.DraftAdmissionRepository(Common.AppConfig.ConnectionString),
                new Services.StudentService(new Data.StudentRepository(Common.AppConfig.ConnectionString), _feeRepository),
                _feeRepository);
            var draft = await svc.GetPendingAsync(); // or a GetById; keep the DraftAdmission for receipt/SMS amounts
            // capture the specific draft's amounts before approval:
            var d = System.Linq.Enumerable.FirstOrDefault(draft, x => x.DraftID == draftId);
            if (d == null) { UIHelper.ShowError("Draft not found.", "Approval"); return; }

            var res = await svc.ApproveAsync(draftId, bursar);
            if (!res.Ok) { UIHelper.ShowError(res.Message, "Approval"); return; }

            // Two receipts (reuse the receipt renderer).
            PrintAdmissionReceipts(res.Student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal, bursar);

            // SMS (fire-and-forget) with payment lines.
            if (!string.IsNullOrWhiteSpace(res.Student.EmergencyContact))
                _ = Services.SmsService.SendStudentAdmissionAsync(res.Student.EmergencyContact, res.Student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal);
            if (!string.IsNullOrWhiteSpace(res.Student.GuardianEmail))
                _ = Services.NotificationService.SendEmailAsync(res.Student.GuardianEmail,
                    "Admission Confirmation - Kingdom Preparatory School",
                    Services.SmsService.BuildStudentAdmissionMessage(res.Student, d.AdmissionFee, d.SchoolFeePaid, d.TermTotal),
                    Services.NotificationService.NotificationType.GeneralAnnouncement);

            UIHelper.ShowSuccess("Approved. Receipts printed and SMS sent.", "Approval");
            // refresh the pending list
        }
```

- [ ] **Step 3: Two receipts** — add `PrintAdmissionReceipts(...)` that builds two `ReceiptPrintData` (one "Admission Fee" GHS 100, one "School Fees" with paid/balance) and prints each via the existing `PrintDocument`/`DrawReceipt` path (factor the existing single-receipt print into a `PrintReceipt(ReceiptPrintData)` helper and call it twice).

- [ ] **Step 4: Reject handler** — `await svc.RejectAsync(draftId);` then refresh the list.

- [ ] **Step 5: Build + manual** — `0 Error(s)`. Manual: submit a draft (Task 7), open Pending Approvals as Accountant, Approve → Students row + two payment_record rows + draft deleted + SMS logged (`sms.log`) + two receipts.

- [ ] **Step 6: Commit**
```bash
git add frmFessPayment.cs
git commit -m "feat(admission): Pending Approvals tab — approve (promote + 2 receipts + SMS) / reject"
```

---

## Task 9: Bursar dashboard card "Pending Admission Payments (N)"

**Files:** Modify `frmDashboard.cs`. READ how insight cards/nav are built (`CreateInsightTile`, `pendingLeaveLabel` pattern ~line 510-549, and `LoadDashboardStatisticsAsync`).

- [ ] **Step 1:** Add a card/label `pendingAdmissionLabel` (mirroring `pendingLeaveLabel`) shown only for the **Accountant** role, with text "Pending Admission Payments". Make the card clickable → opens the Pending Approvals view from Task 8 (e.g. `OpenForm(new frmFessPayment())` then switch to its pending tab, or a dedicated `frmPendingApprovals`).

- [ ] **Step 2:** In `LoadDashboardStatisticsAsync` (or a dedicated loader), set the count:
```csharp
            var pendingDrafts = await new Services.DraftAdmissionService(
                new Data.DraftAdmissionRepository(AppConfig.ConnectionString),
                new Services.StudentService(new Data.StudentRepository(AppConfig.ConnectionString), _feeRepository),
                _feeRepository).GetPendingAsync();
            if (pendingAdmissionLabel != null) pendingAdmissionLabel.Text = System.Linq.Enumerable.Count(pendingDrafts).ToString();
```
(Use the dashboard's existing `_feeRepository`/repos if present; otherwise construct as above.)

- [ ] **Step 3: Build** → `0 Error(s)`.

- [ ] **Step 4: Visual gate** — render `frmDashboard` as Accountant; confirm the "Pending Admission Payments" card shows the count.

- [ ] **Step 5: Commit**
```bash
git add frmDashboard.cs
git commit -m "feat(admission): bursar dashboard card for pending admission payments"
```

---

## Task 10: End-to-end verification

- [ ] **Step 1: Full build** → `0 Error(s)`.
- [ ] **Step 2: Walk the flow (manual, LocalDB running):**
  1. As **Admin**: admit a student → payment screen prefilled (GHS 100 + ≥50% seeded) → try school fee < 50% → blocked → set ≥50% → **Submit for Approval**.
  2. Confirm `DraftAdmissions` has the row; `Students` and `payment_record` do **not**; no SMS.
  3. As **Accountant**: dashboard shows "Pending Admission Payments (1)" → open Pending Approvals → **Approve**.
  4. Confirm `Students` row created (StudentID assigned), two `payment_record` rows (admission + school), school balance = TermTotal − paid, draft deleted, **two receipts** printed, **SMS** in `logs/sms.log` with the payment lines + Student ID.
  5. Submit another draft → **Reject** → draft deleted, no Student.
- [ ] **Step 3:** Report results.

---

## Self-Review

- **Spec coverage:** draft table + repo (T1), model + AdmissionFees (T2), validate/create/approve/reject incl. ≥50% + admission-fee-separate + school-row-last (T3), SMS variant with payment lines (T4), deferred notifications (T5), frmAddStd submit-draft (T6), admission-mode payment + ≥50% (T7), pending tab + approve(promote/2 receipts/SMS)/reject (T8), dashboard card (T9), E2E (T10). ✓ All spec sections mapped.
- **Placeholder scan:** UI tasks (T7–T9) reference real control names to confirm on read (the forms are large/pre-existing); the data/service core (T1–T5) is full verbatim code. No "add error handling" hand-waves. ✓
- **Type consistency:** `DraftAdmission` fields/`ToStudent()` used by repo (T1) and service (T3); `DraftAdmissionService(IDraftAdmissionRepository, StudentService, IFeeRepository)` ctor consistent across T7/T8/T9 call sites; `ApproveAsync` returns `(Ok, Message, Student)` consumed in T8; SMS `SendStudentAdmissionAsync(recipient, student, admissionFeePaid, schoolFeePaid, termTotal)` defined T4, called T8. ✓
- **Note:** confirm exact `frmFessPayment` control/field names (amountBox, balanceBox, beingBox, paymentModeBox, _feeRepository, the record button) and `frmDashboard` card pattern when reading those files; adapt the snippets to the real names.
