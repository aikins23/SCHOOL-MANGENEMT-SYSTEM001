# Payment & Leave Notifications — Design

**Date:** 2026-06-03
**Status:** Approved (design)
**Builds on:** the SMS infrastructure from `2026-06-03-sms-email-notifications-design.md` (pluggable `ISmsProvider`/Arkesel, per-duty sender IDs via `SmsSenderIds`, `PhoneNumberGh`, `AppConfig.Sms`).

## Goal

Add email + SMS notifications for two more events:

1. **Fee payment recorded** — SMS receipt to the guardian (email already sends).
2. **Employee leave** — request submitted, approved, and rejected.

SMS reuses existing per-duty custom sender IDs (no new Arkesel registration): payment → `KPSFEES`, all leave messages → `KPSEMPADM`.

## Current state

- **Fee payment:** `FeeRepository.AddPaymentRecordAsync` already sends an email via `NotificationService.SendPaymentReceivedAsync` (it `await`s it inline — can stall the payment up to the 30s SMTP timeout). No SMS. Guardian email is fetched via `GetStudentGuardianEmailAsync` (column `Students.GuidianceEmail`, a legacy typo). There is no guardian-phone fetch yet.
- **Leave:** `LeaveService.ApplyForLeaveAsync` (submit, sets `PENDING`) and `LeaveService.UpdateLeaveStatusAsync` (approve/reject) send **nothing**. `NotificationService.SendLeaveApprovalAsync` (decision email) exists but is **dead code** (never called). `LeaveRequest` carries `EmployeeID`, `EmployeeName`, `Department`, `Position`, dates, `Status`, `Reason` — but **not** the employee's email/phone. `EmployeeService.GetEmployeeAsync(id)` returns an `Employee` with `.Email` and `.Contact` (phone). `LeaveService` is constructed in 5 places with only an `ILeaveRepository`. Decisions happen from two forms: `frmLeaveApproval` (APPROVED/REJECTED) and `frmLeaveDetails`; submit from `frmEmpLeave`.

## Decisions

- **Sender IDs:** payment → `KPSFEES` (`SmsSenderIds.FeeReminder`); leave (submit/decision/HR-alert) → `KPSEMPADM` (`SmsSenderIds.EmployeeAdmission`). No new sender IDs to register.
- **Leave request recipients:** notify the **employee** (confirmation) **and** a **configurable HR address** (email + phone) that a request needs review.
- **Leave employee contact resolution:** give `LeaveService` an **optional** `EmployeeService` dependency via a constructor overload (`LeaveService(repo)` kept; add `LeaveService(repo, employeeService)`). Notifications fire only when the employee service is available. The 3 submit/approve/reject forms pass it; the 2 read-only sites (`EmpleaveView`, `frmLeaveBalanceReport`) don't.
- **All sends fire-and-forget, best-effort, logged.** Never block or fail the payment/leave operation. The existing inline-`await`ed payment email is changed to fire-and-forget too.

## Architecture / components

### New config — `AppConfig.Notify`
```
AppConfig.Notify.HrEmail   (string, setting "HrNotifyEmail",  default "")
AppConfig.Notify.HrPhone   (string, setting "HrNotifyPhone",  default "")
```
Backed by `Properties.Settings` (added to `Settings.settings` + `Settings.Designer.cs`, same pattern as `SmsApiKey`). Editable in `frmEmailSettings`.

### `SmsService` — new per-duty methods
- `SendPaymentReceivedAsync(recipient, studentName, amountPaid, newBalance)` → `SmsSenderIds.FeeReminder` (KPSFEES).
- `SendLeaveSubmittedAsync(recipient, employeeName)` → `EmployeeAdmission` (KPSEMPADM). Employee confirmation.
- `SendLeaveDecisionAsync(recipient, employeeName, status, startDate, endDate)` → `EmployeeAdmission`. status = "APPROVED"/"REJECTED".
- `SendLeaveHrAlertAsync(hrPhone, employeeName, startDate, endDate)` → `EmployeeAdmission`. HR alert.
All build short messages and call the existing `SendSmsAsync(recipient, message, senderId)` (which normalizes the phone, picks the provider, logs, and is exception-safe).

### `NotificationService` — email methods
- Reuse existing `SendPaymentReceivedAsync` (payment) and `SendLeaveApprovalAsync` (decision).
- Add `SendLeaveSubmittedAsync(employeeName, employeeEmail, startDate, endDate)` — employee confirmation.
- Add `SendLeaveRequestHrAlertAsync(hrEmail, employeeName, startDate, endDate, reason)` — HR alert.

### `FeeRepository`
- Add `GetStudentGuardianPhoneAsync(studentId)` mirroring `GetStudentGuardianEmailAsync`: `SELECT EmergencyConatct FROM Students WHERE StudentID = ?` (column `EmergencyConatct` is a legacy typo; it is the same emergency-contact number registration/reminders use via `Student.EmergencyContact`).

### `LeaveService`
- Add `private readonly EmployeeService _employeeService;` and a constructor overload `LeaveService(ILeaveRepository repository, EmployeeService employeeService)`. Keep the existing single-arg constructor (sets `_employeeService = null`).
- Private helper `NotifyLeaveAsync(...)` resolves the employee via `_employeeService?.GetEmployeeAsync(request.EmployeeID)` and dispatches the right email+SMS. Guarded by `if (_employeeService == null) return;`.

## Data flow

**Payment** (`AddPaymentRecordAsync`, after insert succeeds):
- `_ =` email to guardian email (existing template) — now fire-and-forget.
- fetch guardian phone → `_ = SmsService.SendPaymentReceivedAsync(phone, name, amount, newBalance)`.

**Leave submit** (`ApplyForLeaveAsync`, after `AddLeaveRequestAsync` succeeds):
- look up employee → `_ =` email + SMS confirmation to the employee.
- if `AppConfig.Notify.HrEmail`/`HrPhone` set → `_ =` email + SMS HR alert.

**Leave decision** (`UpdateLeaveStatusAsync`, after `UpdateLeaveRequestAsync` succeeds, status APPROVED/REJECTED):
- look up employee → `_ =` `SendLeaveApprovalAsync` email + `SendLeaveDecisionAsync` SMS to the employee.

## Error handling

- Every send is `_ = ...Async(...)` fire-and-forget; the underlying `SmsService`/`NotificationService` methods already catch their own exceptions and return tuples, so nothing throws into the payment/leave flow.
- Missing/blank email or phone → that channel is skipped (the send methods return early).
- SMS still honors `AppConfig.Sms.Enabled`/LogOnly; with SMS disabled, everything logs to `logs/sms.log`.
- Employee lookup failure (null employee) → skip leave notifications silently (logged).

## Settings UI (`frmEmailSettings`)

Add to the existing notifications area: **HR Email** textbox + **HR Phone** textbox (bound to `AppConfig.Notify.HrEmail`/`HrPhone`), loaded in `LoadSettings` and saved in `BtnSave_Click` (alongside the SMS settings, before email validation so they persist independently).

## Testing

- **Build gate:** `dotnet build` → 0 errors.
- **Logic gate (reflection harness):** each new `SmsService` method on the LogOnly path returns success and logs the correct sender ID (KPSFEES for payment, KPSEMPADM for leave) and normalized number.
- **Manual:** emails verified via existing SMTP; a real leave approve/reject and a payment exercise the full path.

## Out of scope

- No new SMS sender IDs (reusing KPSFEES/KPSEMPADM).
- No change to leave approval logic, overlap detection, or balances.
- Email templates/SMTP unchanged except the two new leave email methods.
- HR address is a single global email+phone (not per-department routing).
