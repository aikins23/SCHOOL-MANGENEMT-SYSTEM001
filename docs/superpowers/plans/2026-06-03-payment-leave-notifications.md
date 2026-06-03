# Payment & Leave Notifications Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Send email + SMS on fee payment (guardian) and on employee leave submit/approve/reject (employee + a configurable HR address), reusing the existing SMS infrastructure (sender IDs KPSFEES for payment, KPSEMPADM for leave).

**Architecture:** New per-duty methods on `SmsService` and two new email methods on `NotificationService`. A new `AppConfig.Notify` (HR email/phone) backed by settings. `FeeRepository.AddPaymentRecordAsync` gains a guardian-phone lookup + fire-and-forget SMS (and its email becomes fire-and-forget). `LeaveService` gets an optional `EmployeeService` (constructor overload) and fires notifications from `ApplyForLeaveAsync`/`UpdateLeaveStatusAsync`; the 3 submit/approve/reject forms pass the employee service. HR fields added to the settings form.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, OleDb (MS Access-style `payment_record`/`Students` tables), existing `SmsService`/`NotificationService`/`AppConfig`.

---

## Verification note (read first)

No xunit runner exists; do not compile test files. Each task verifies via:
1. **Compile gate:** `dotnet build -clp:ErrorsOnly -nologo` → `Build succeeded.` / `0 Error(s)`. Project root:
   `cd "C:/Users/DELL/Downloads/New folder (2)/IPMC PROJECT BUABENG EMMANUEL AIKINS (1)/BUABENG EMMANUEL AIKINS - Copy"`
2. **Logic gate (reflection harness)** for new `SmsService` methods — loads the built exe and asserts the LogOnly result. Reusable script body (write to `verify_tmp.ps1`, run `powershell.exe -STA -ExecutionPolicy Bypass -File verify_tmp.ps1`, then delete it; do NOT commit it):
```
$ErrorActionPreference='Stop'
$bin='C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\bin\Debug'
[AppDomain]::CurrentDomain.add_AssemblyResolve([ResolveEventHandler]{param($s,$e)
  $n=($e.Name -split ',')[0]; if($n -like '*.resources'){return $null}
  $p=Join-Path $bin ($n+'.dll'); if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$t=$asm.GetType('kingdom_Preparatory_School_Management_System.Services.SmsService')
# <PER-TASK ASSERTIONS HERE>
```
Value tuples accessed via reflection use `.Item1`/`.Item2` (PowerShell), not `.Success`/`.Message`.

---

## File Structure

- Modify `Properties/Settings.settings` + `Properties/Settings.Designer.cs` — add `HrNotifyEmail`, `HrNotifyPhone`.
- Modify `Common/AppConfig.cs` — add `Notify` class (HrEmail/HrPhone).
- Modify `Services/SmsService.cs` — add 4 methods (payment + 3 leave).
- Modify `Services/NotificationService.cs` — add 2 email methods (leave submitted, HR alert).
- Modify `Data/FeeRepository.cs` — add `GetStudentGuardianPhoneAsync`; update `AddPaymentRecordAsync`.
- Modify `Services/LeaveService.cs` — optional `EmployeeService`, notify helpers, wire submit + decision.
- Modify `frmEmpLeave.cs`, `frmLeaveApproval.cs`, `frmLeaveDetails.cs` — pass `EmployeeService` to `LeaveService`.
- Modify `frmEmailSettings.cs` — HR Email + HR Phone fields.

No new files; no new csproj entries; no new references.

---

## Task 1: HR notify settings + `AppConfig.Notify`

**Files:** `Properties/Settings.settings`, `Properties/Settings.Designer.cs`, `Common/AppConfig.cs`

- [ ] **Step 1: `Settings.settings`** — after the `SmsSchoolAbbreviation` Setting, add:
```xml
    <Setting Name="HrNotifyEmail" Type="System.String" Scope="User">
      <Value Profile="(Default)"></Value>
    </Setting>
    <Setting Name="HrNotifyPhone" Type="System.String" Scope="User">
      <Value Profile="(Default)"></Value>
    </Setting>
```

- [ ] **Step 2: `Settings.Designer.cs`** — after the `SmsSchoolAbbreviation` property, add:
```csharp
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string HrNotifyEmail {
            get { return ((string)(this["HrNotifyEmail"])); }
            set { this["HrNotifyEmail"] = value; }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string HrNotifyPhone {
            get { return ((string)(this["HrNotifyPhone"])); }
            set { this["HrNotifyPhone"] = value; }
        }
```

- [ ] **Step 3: `Common/AppConfig.cs`** — add a new nested class after the `Sms` class (inside `AppConfig`):
```csharp
        // Notification recipients (HR leave alerts, etc.)
        public static class Notify
        {
            public static string HrEmail
            {
                get { try { return Properties.Settings.Default.HrNotifyEmail ?? ""; } catch { return ""; } }
                set { try { Properties.Settings.Default.HrNotifyEmail = value; Properties.Settings.Default.Save(); } catch { } }
            }

            public static string HrPhone
            {
                get { try { return Properties.Settings.Default.HrNotifyPhone ?? ""; } catch { return ""; } }
                set { try { Properties.Settings.Default.HrNotifyPhone = value; Properties.Settings.Default.Save(); } catch { } }
            }
        }
```

- [ ] **Step 4: Build** — `dotnet build -clp:ErrorsOnly -nologo` → `0 Error(s)`.

- [ ] **Step 5: Commit**
```bash
git add Properties/Settings.settings Properties/Settings.Designer.cs Common/AppConfig.cs
git commit -m "feat(notify): add HR notify email/phone settings"
```

---

## Task 2: `SmsService` payment + leave methods

**Files:** `Services/SmsService.cs`

- [ ] **Step 1:** Insert these four methods into the `SmsService` class, immediately BEFORE the `private static void Log(` method:
```csharp
        public static Task<(bool Success, string Message)> SendPaymentReceivedAsync(
            string recipient, string studentName, decimal amountPaid, decimal newBalance)
        {
            string balanceLine = newBalance > 0
                ? $"Outstanding balance: GHS {newBalance:N2}."
                : "Balance fully cleared.";
            string message =
                $"Dear Guardian, payment of GHS {amountPaid:N2} received for {studentName}. " +
                $"{balanceLine} Thank you. - Kingdom Preparatory School Accounts";
            return SendSmsAsync(recipient, message, SmsSenderIds.FeeReminder);
        }

        public static Task<(bool Success, string Message)> SendLeaveSubmittedAsync(
            string recipient, string employeeName)
        {
            string message =
                $"Dear {employeeName}, your leave request has been submitted and is pending approval. " +
                "- Kingdom Preparatory School HR";
            return SendSmsAsync(recipient, message, SmsSenderIds.EmployeeAdmission);
        }

        public static Task<(bool Success, string Message)> SendLeaveDecisionAsync(
            string recipient, string employeeName, string status, DateTime startDate, DateTime endDate)
        {
            string message =
                $"Dear {employeeName}, your leave ({startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}) " +
                $"has been {status.ToUpperInvariant()}. - Kingdom Preparatory School HR";
            return SendSmsAsync(recipient, message, SmsSenderIds.EmployeeAdmission);
        }

        public static Task<(bool Success, string Message)> SendLeaveHrAlertAsync(
            string hrPhone, string employeeName, DateTime startDate, DateTime endDate)
        {
            string message =
                $"Leave request from {employeeName} ({startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}) " +
                "is pending review. - Kingdom Preparatory School";
            return SendSmsAsync(hrPhone, message, SmsSenderIds.EmployeeAdmission);
        }
```
(`using System;` is already present in `SmsService.cs`, so `DateTime` resolves.)

- [ ] **Step 2: Build** → `0 Error(s)`.

- [ ] **Step 3: Logic gate** — write `verify_tmp.ps1` using the harness body above with these assertions appended, run it, expect `LEAVE+PAY SMS OK`, then delete the file:
```
$pay=$t.GetMethod('SendPaymentReceivedAsync').Invoke($null,@('0241234567','Kwame',[decimal]500,[decimal]1423))
$pay.Wait()
$dec=$t.GetMethod('SendLeaveDecisionAsync').Invoke($null,@('0541234567','Ama','APPROVED',[DateTime]::Today,[DateTime]::Today.AddDays(2)))
$dec.Wait()
if($pay.Result.Item1 -and $dec.Result.Item1){'LEAVE+PAY SMS OK'}else{'FAIL'}
```
Expected: `LEAVE+PAY SMS OK`. (Both go through LogOnly since SMS is disabled by default; payment uses KPSFEES, leave uses KPSEMPADM — confirm by checking `logs/sms.log` shows `From: KPSFEES` and `From: KPSEMPADM`.)

- [ ] **Step 4: Commit**
```bash
git add Services/SmsService.cs
git commit -m "feat(notify): add payment + leave SMS methods (KPSFEES/KPSEMPADM)"
```

---

## Task 3: `NotificationService` leave emails

**Files:** `Services/NotificationService.cs`

- [ ] **Step 1:** Insert these two methods into `NotificationService`, immediately AFTER the existing `SendLeaveApprovalAsync` method:
```csharp
        /// <summary>Confirms to the employee that their leave request was received.</summary>
        public static async Task<(bool Success, string Message)> SendLeaveSubmittedAsync(
            string employeeName, string employeeEmail, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(employeeEmail))
                return (false, "Employee email is required");

            int duration = (endDate.Date - startDate.Date).Days + 1;
            string subject = "Leave Request Received - Kingdom Preparatory School";
            string body = $@"Dear {employeeName},

Your leave application has been received and is pending approval.

  Start Date: {startDate:MMMM dd, yyyy}
  End Date: {endDate:MMMM dd, yyyy}
  Duration: {duration} day(s)
  Status: PENDING

You will be notified once a decision has been made.

Best regards,
Human Resources Department
Kingdom Preparatory School";

            return await SendEmailAsync(employeeEmail, subject, body, NotificationType.LeaveApproval);
        }

        /// <summary>Alerts the HR address that a new leave request needs review.</summary>
        public static async Task<(bool Success, string Message)> SendLeaveRequestHrAlertAsync(
            string hrEmail, string employeeName, DateTime startDate, DateTime endDate, string reason)
        {
            if (string.IsNullOrWhiteSpace(hrEmail))
                return (false, "HR email is required");

            int duration = (endDate.Date - startDate.Date).Days + 1;
            string subject = $"Leave Request Pending Review - {employeeName}";
            string body = $@"A new leave request requires review:

  Employee: {employeeName}
  Start Date: {startDate:MMMM dd, yyyy}
  End Date: {endDate:MMMM dd, yyyy}
  Duration: {duration} day(s)
  Reason: {reason}

Please review it in the Leave Approval screen.

Kingdom Preparatory School";

            return await SendEmailAsync(hrEmail, subject, body, NotificationType.LeaveApproval);
        }
```

- [ ] **Step 2: Build** → `0 Error(s)`.

- [ ] **Step 3: Commit**
```bash
git add Services/NotificationService.cs
git commit -m "feat(notify): add leave-submitted and HR-alert emails"
```

---

## Task 4: Fee payment — guardian phone lookup + SMS + non-blocking email

**Files:** `Data/FeeRepository.cs`

- [ ] **Step 1:** Add this private method immediately AFTER `GetStudentGuardianEmailAsync`:
```csharp
        private async Task<string> GetStudentGuardianPhoneAsync(string studentId)
        {
            try
            {
                using (var connection = new OleDbConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Column EmergencyConatct is a legacy typo; same number used at registration.
                    var query = "SELECT EmergencyConatct FROM Students WHERE StudentID = ?";
                    using (var command = new OleDbCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", studentId);
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                return "";
            }
        }
```

- [ ] **Step 2:** In `AddPaymentRecordAsync`, replace the existing post-insert notification block:
```csharp
                        if (result > 0)
                        {
                            var guardianEmail = await GetStudentGuardianEmailAsync(studentId);
                            if (!string.IsNullOrWhiteSpace(guardianEmail))
                            {
                                await NotificationService.SendPaymentReceivedAsync(
                                    studentName, guardianEmail, amountPaid, newBalance, date, classId
                                );
                            }
                        }
```
with (email now fire-and-forget + new SMS):
```csharp
                        if (result > 0)
                        {
                            var guardianEmail = await GetStudentGuardianEmailAsync(studentId);
                            if (!string.IsNullOrWhiteSpace(guardianEmail))
                            {
                                _ = NotificationService.SendPaymentReceivedAsync(
                                    studentName, guardianEmail, amountPaid, newBalance, date, classId);
                            }

                            var guardianPhone = await GetStudentGuardianPhoneAsync(studentId);
                            if (!string.IsNullOrWhiteSpace(guardianPhone))
                            {
                                _ = SmsService.SendPaymentReceivedAsync(
                                    guardianPhone, studentName, amountPaid, newBalance);
                            }
                        }
```
(`FeeRepository` already references `NotificationService` from the `Services` namespace, so `SmsService` resolves with no new `using`.)

- [ ] **Step 3: Build** → `0 Error(s)`.

- [ ] **Step 4: Commit**
```bash
git add Data/FeeRepository.cs
git commit -m "feat(notify): SMS receipt on fee payment; non-blocking email"
```

---

## Task 5: `LeaveService` — optional EmployeeService + notifications

**Files:** `Services/LeaveService.cs`

- [ ] **Step 1:** Replace the existing field + constructor:
```csharp
        private readonly ILeaveRepository _repository;

        public LeaveService(ILeaveRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
```
with:
```csharp
        private readonly ILeaveRepository _repository;
        private readonly EmployeeService _employeeService;

        public LeaveService(ILeaveRepository repository)
            : this(repository, null) { }

        public LeaveService(ILeaveRepository repository, EmployeeService employeeService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _employeeService = employeeService;
        }
```

- [ ] **Step 2:** In `ApplyForLeaveAsync`, replace:
```csharp
                request.Status = "PENDING";
                bool success = await _repository.AddLeaveRequestAsync(request);
                return success
                    ? (true, "Leave application submitted successfully.")
                    : (false, "Failed to submit leave application.");
```
with:
```csharp
                request.Status = "PENDING";
                bool success = await _repository.AddLeaveRequestAsync(request);
                if (success)
                {
                    _ = NotifyLeaveSubmittedAsync(request);
                }
                return success
                    ? (true, "Leave application submitted successfully.")
                    : (false, "Failed to submit leave application.");
```

- [ ] **Step 3:** In `UpdateLeaveStatusAsync`, replace:
```csharp
                request.Status = newStatus;
                bool success = await _repository.UpdateLeaveRequestAsync(request);
                return success 
                    ? (true, $"Leave application {newStatus.ToLower()} successfully.") 
                    : (false, $"Failed to {newStatus.ToLower()} leave application.");
```
with:
```csharp
                request.Status = newStatus;
                bool success = await _repository.UpdateLeaveRequestAsync(request);
                if (success && (newStatus == "APPROVED" || newStatus == "REJECTED"))
                {
                    _ = NotifyLeaveDecisionAsync(request, newStatus);
                }
                return success 
                    ? (true, $"Leave application {newStatus.ToLower()} successfully.") 
                    : (false, $"Failed to {newStatus.ToLower()} leave application.");
```

- [ ] **Step 4:** Add these two private helpers inside the class (e.g. after `UpdateLeaveStatusAsync`):
```csharp
        // Fire-and-forget; resolves the employee's contact and sends email + SMS.
        private async Task NotifyLeaveSubmittedAsync(Models.LeaveRequest request)
        {
            if (_employeeService == null) return;
            try
            {
                var emp = await _employeeService.GetEmployeeAsync(request.EmployeeID);
                if (emp != null)
                {
                    if (!string.IsNullOrWhiteSpace(emp.Email))
                        _ = NotificationService.SendLeaveSubmittedAsync(
                            request.EmployeeName, emp.Email, request.StartDate, request.EndDate);
                    if (!string.IsNullOrWhiteSpace(emp.Contact))
                        _ = SmsService.SendLeaveSubmittedAsync(emp.Contact, request.EmployeeName);
                }

                string hrEmail = AppConfig.Notify.HrEmail;
                if (!string.IsNullOrWhiteSpace(hrEmail))
                    _ = NotificationService.SendLeaveRequestHrAlertAsync(
                        hrEmail, request.EmployeeName, request.StartDate, request.EndDate, request.Reason);

                string hrPhone = AppConfig.Notify.HrPhone;
                if (!string.IsNullOrWhiteSpace(hrPhone))
                    _ = SmsService.SendLeaveHrAlertAsync(
                        hrPhone, request.EmployeeName, request.StartDate, request.EndDate);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Leave-submitted notification failed", ex);
            }
        }

        private async Task NotifyLeaveDecisionAsync(Models.LeaveRequest request, string status)
        {
            if (_employeeService == null) return;
            try
            {
                var emp = await _employeeService.GetEmployeeAsync(request.EmployeeID);
                if (emp == null) return;
                if (!string.IsNullOrWhiteSpace(emp.Email))
                    _ = NotificationService.SendLeaveApprovalAsync(
                        request.EmployeeName, emp.Email, status, request.StartDate, request.EndDate, request.Reason);
                if (!string.IsNullOrWhiteSpace(emp.Contact))
                    _ = SmsService.SendLeaveDecisionAsync(
                        emp.Contact, request.EmployeeName, status, request.StartDate, request.EndDate);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Leave-decision notification failed", ex);
            }
        }
```
(`LeaveService` is in the `Services` namespace, so `EmployeeService`, `SmsService`, `NotificationService`, and `LoggerHelper` resolve without new usings. `using System;` and `kingdom_Preparatory_School_Management_System.Common` are already imported, so `Exception` and `AppConfig` resolve.)

- [ ] **Step 5: Build** → `0 Error(s)`. (The 5 existing `new LeaveService(repo)` call sites still compile via the kept single-arg constructor.)

- [ ] **Step 6: Commit**
```bash
git add Services/LeaveService.cs
git commit -m "feat(notify): wire leave submit/decision email+SMS via optional EmployeeService"
```

---

## Task 6: Pass `EmployeeService` into `LeaveService` at the 3 action forms

**Files:** `frmEmpLeave.cs`, `frmLeaveApproval.cs`, `frmLeaveDetails.cs`

- [ ] **Step 1: `frmEmpLeave.cs`** — replace the existing init block (≈ lines 31-35):
```csharp
            var leaveRepo = new LeaveRepository(AppConfig.ConnectionString);
            _leaveService = new LeaveService(leaveRepo);
            
            var employeeRepo = new EmployeeRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(employeeRepo);
```
with (build employee service first, then inject it):
```csharp
            var leaveRepo = new LeaveRepository(AppConfig.ConnectionString);
            var employeeRepo = new EmployeeRepository(AppConfig.ConnectionString);
            _employeeService = new EmployeeService(employeeRepo);
            _leaveService = new LeaveService(leaveRepo, _employeeService);
```

- [ ] **Step 2: `frmLeaveApproval.cs`** — replace (≈ lines 43-44):
```csharp
            var repository = new LeaveRepository(AppConfig.ConnectionString);
            _leaveService = new LeaveService(repository);
```
with:
```csharp
            var repository = new LeaveRepository(AppConfig.ConnectionString);
            var employeeService = new EmployeeService(new EmployeeRepository(AppConfig.ConnectionString));
            _leaveService = new LeaveService(repository, employeeService);
```
If the build reports `EmployeeRepository`/`EmployeeService` not found, add `using kingdom_Preparatory_School_Management_System.Data;` and `using kingdom_Preparatory_School_Management_System.Services;` to the top of the file.

- [ ] **Step 3: `frmLeaveDetails.cs`** — find the line `_leaveService = new LeaveService(repository);` (≈ line 21) and replace with:
```csharp
            var employeeService = new EmployeeService(new EmployeeRepository(AppConfig.ConnectionString));
            _leaveService = new LeaveService(repository, employeeService);
```
(Use the same local variable name the file already uses for the `LeaveRepository` — confirm it is `repository`; if it differs, keep that name.) Add the `Data`/`Services` usings if the build reports the types are missing.

- [ ] **Step 4: Build** → `0 Error(s)`.

- [ ] **Step 5: Commit**
```bash
git add frmEmpLeave.cs frmLeaveApproval.cs frmLeaveDetails.cs
git commit -m "feat(notify): inject EmployeeService into LeaveService at leave forms"
```

---

## Task 7: Settings UI — HR Email + HR Phone

**Files:** `frmEmailSettings.cs`

READ the file first; the SMS group (`grpSmsSettings`) and its layout variables (`my`, `labelWidth`, `controlWidth`, `controlHeight`) were added previously. Add two rows to that group, after the School Abbrev. row and before (or after) the Test row — keep them inside `grpSmsSettings` and increase the group height if needed.

- [ ] **Step 1:** Add fields near the other SMS fields:
```csharp
        private Label lblHrEmail;
        private TextBox txtHrEmail;
        private Label lblHrPhone;
        private TextBox txtHrPhone;
```

- [ ] **Step 2:** In `InitializeComponent`, inside the SMS group construction (after the sender-preview label is added, before the Test phone row), insert:
```csharp
            lblHrEmail = new Label();
            lblHrEmail.Text = "HR Email:";
            lblHrEmail.Location = new System.Drawing.Point(15, my);
            lblHrEmail.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblHrEmail);

            txtHrEmail = new TextBox();
            txtHrEmail.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtHrEmail.Size = new System.Drawing.Size(controlWidth, controlHeight);
            grpSmsSettings.Controls.Add(txtHrEmail);
            my += controlHeight + 10;

            lblHrPhone = new Label();
            lblHrPhone.Text = "HR Phone:";
            lblHrPhone.Location = new System.Drawing.Point(15, my);
            lblHrPhone.Size = new System.Drawing.Size(labelWidth, controlHeight);
            grpSmsSettings.Controls.Add(lblHrPhone);

            txtHrPhone = new TextBox();
            txtHrPhone.Location = new System.Drawing.Point(15 + labelWidth + 10, my);
            txtHrPhone.Size = new System.Drawing.Size(140, controlHeight);
            grpSmsSettings.Controls.Add(txtHrPhone);
            my += controlHeight + 12;
```
Then increase `grpSmsSettings.Size` height by ~80px (e.g. from `250` to `330`) and the form's `this.Size` height by ~80px (e.g. `760` → `840`) so the Test row and action buttons still fit below. Verify against the actual current values when you read the file.

- [ ] **Step 3:** In `LoadSettings()`, after the SMS loads, add:
```csharp
            txtHrEmail.Text = AppConfig.Notify.HrEmail;
            txtHrPhone.Text = AppConfig.Notify.HrPhone;
```

- [ ] **Step 4:** In `BtnSave_Click`, where the SMS settings are persisted (near the top of the `try`), add:
```csharp
                AppConfig.Notify.HrEmail = txtHrEmail.Text.Trim();
                AppConfig.Notify.HrPhone = txtHrPhone.Text.Trim();
```

- [ ] **Step 5: Build** → `0 Error(s)`.

- [ ] **Step 6: Visual gate** — render `frmEmailSettings` offline (memory `offline-winforms-render-harness`: auth `Administrator`, off-screen `Show`, `DrawToBitmap`) and confirm the HR Email / HR Phone rows appear inside the SMS group with no overlap and the Save/Cancel buttons still sit below.

- [ ] **Step 7: Commit**
```bash
git add frmEmailSettings.cs
git commit -m "feat(notify): add HR Email/Phone fields to settings"
```

---

## Task 8: Final verification

- [ ] **Step 1: Full build** → `0 Error(s)`.

- [ ] **Step 2: Logic gate (all new SMS senders)** — write `verify_tmp.ps1` (harness body + below), run, expect `ALL NOTIFY SMS OK`, delete:
```
$pay=$t.GetMethod('SendPaymentReceivedAsync').Invoke($null,@('0241234567','Kwame',[decimal]500,[decimal]0)); $pay.Wait()
$sub=$t.GetMethod('SendLeaveSubmittedAsync').Invoke($null,@('0241234567','Ama')); $sub.Wait()
$dec=$t.GetMethod('SendLeaveDecisionAsync').Invoke($null,@('0541234567','Ama','REJECTED',[DateTime]::Today,[DateTime]::Today.AddDays(1))); $dec.Wait()
$hr=$t.GetMethod('SendLeaveHrAlertAsync').Invoke($null,@('0201234567','Ama',[DateTime]::Today,[DateTime]::Today.AddDays(1))); $hr.Wait()
if($pay.Result.Item1 -and $sub.Result.Item1 -and $dec.Result.Item1 -and $hr.Result.Item1){'ALL NOTIFY SMS OK'}else{'FAIL'}
```

- [ ] **Step 3: Manual smoke (operator, document in PR):**
  1. Settings → set HR Email + HR Phone, Save.
  2. Submit a leave request → employee gets confirmation email/SMS; HR address gets an alert.
  3. Approve/reject it → employee gets the decision email/SMS.
  4. Record a fee payment → guardian gets email + SMS receipt.
  5. `logs/sms.log` shows `From: KPSFEES` (payment) and `From: KPSEMPADM` (leave).

- [ ] **Step 4: Commit any docs.**

---

## Self-Review

- **Spec coverage:** HR config (T1), payment SMS (T2/T4), leave SMS submit+decision+HR (T2/T5), leave emails submit+HR (T3), existing decision email wired (T5 via `SendLeaveApprovalAsync`), guardian phone lookup + fire-and-forget payment email (T4), optional EmployeeService + form wiring (T5/T6), settings UI (T7), fire-and-forget everywhere (T4/T5). ✓
- **Placeholder scan:** All code shown; the only "verify against actual values" notes are for layout numbers/variable names the engineer confirms by reading the file (T6/T7) — not logic placeholders. ✓
- **Type consistency:** `SmsService.SendPaymentReceivedAsync(recipient, studentName, amountPaid, newBalance)`, `SendLeaveSubmittedAsync(recipient, employeeName)`, `SendLeaveDecisionAsync(recipient, employeeName, status, startDate, endDate)`, `SendLeaveHrAlertAsync(hrPhone, employeeName, startDate, endDate)` — used identically in T4/T5. `NotificationService.SendLeaveSubmittedAsync(employeeName, employeeEmail, startDate, endDate)` and `SendLeaveRequestHrAlertAsync(hrEmail, employeeName, startDate, endDate, reason)` — used in T5. Existing `SendLeaveApprovalAsync(name,email,status,start,end,leaveType)` and `SendPaymentReceivedAsync(name,email,amount,newBalance,date,class)` matched. `AppConfig.Notify.HrEmail/HrPhone` consistent across T1/T5/T7. ✓
