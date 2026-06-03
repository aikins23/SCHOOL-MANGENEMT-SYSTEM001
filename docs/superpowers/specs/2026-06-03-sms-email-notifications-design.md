# SMS (custom sender name) & Email Notifications — Design

**Date:** 2026-06-03
**Status:** Approved (design)

## Goal

Notify recipients by **SMS** and **email** on three events:

1. **Student registration** — notify the student's guardian / emergency contact.
2. **Employee registration** — notify the employee.
3. **Weekly school-fees reminder** — notify guardians of students with an outstanding balance.

SMS must use a **customized alphanumeric sender name that varies per event ("duty")**.

## Current state (what already exists)

- **Email** is fully implemented over SMTP in `Services/NotificationService.cs` (templates for fee reminder, payment received, exam result, leave, announcements; config in `AppConfig.Email` backed by `Properties.Settings`).
- **Registration notifications are already wired**:
  - `Services/StudentService.cs` sends a registration email (guardian) + `SmsService.SendRegistrationSmsAsync` (emergency contact).
  - `Services/EmployeeService.cs` sends a registration email + `SmsService.SendRegistrationSmsAsync`.
- **Weekly fee reminder already exists**: `frmDashboard.CheckFeeRemindersAsync()` runs on a 60-minute `Timer`, only acts on **Mondays**, dedupes via `Properties.Settings.Default.LastFeeReminderWeek`, and sends email + SMS to each guardian with a balance.
- **SMS does NOT actually send**: `Services/SmsService.cs` is a stub with `LogOnly` / `Custom` providers — it only appends to `logs/sms.log`. Its static `Provider`/`ApiKey`/`FromNumber` are never loaded from config. `AppConfig.Sms` (Provider/ApiKey/FromNumber, persisted) exists but is unused.
- **No SMS settings UI** — `frmEmailSettings` configures email only.

**Conclusion:** the flows exist; the work is to make SMS *real*, give it *per-duty custom sender names*, load it from config, and add a settings UI. Email is left untouched (paths verified only).

## Decisions

- **Gateway:** Arkesel (Ghana). Implemented first behind a pluggable interface so other providers can be added without rework.
- **Sender-name scheme** (alphanumeric, GSM limit 11 chars):
  - Student admission: `{ABBR}STDADM` → `KPSSTDADM`
  - Employee admission: `{ABBR}EMPADM` → `KPSEMPADM`
  - Weekly fee reminder: `{ABBR}FEES` → `KPSFEES`
- **Abbreviation:** `{ABBR}` is a **configurable setting**, default `KPS`. A future school-info system / administrator can change it without code changes.
- **Settings UI:** extend the existing settings form (`frmEmailSettings`) with an SMS section (not a separate screen). Email config unchanged.

## Architecture

```
ISmsProvider                       // SendAsync(senderId, recipient, message) -> (bool Success, string Message)
 ├─ ArkeselSmsProvider             // Arkesel v2 HTTP/JSON, "api-key" header, per-message sender
 └─ LogSmsProvider                 // appends to logs/sms.log (offline/dev fallback, default)

SmsService (static facade)
 ├─ resolves the active ISmsProvider from AppConfig.Sms (Enabled + Provider)
 ├─ SendStudentAdmissionAsync(contact, fullName)        -> sender = SenderIds.StudentAdmission
 ├─ SendEmployeeAdmissionAsync(contact, fullName)       -> sender = SenderIds.EmployeeAdmission
 ├─ SendFeeReminderAsync(contact, studentName, balance) -> sender = SenderIds.FeeReminder
 └─ SendTestAsync(recipient)                            -> sender = SenderIds.StudentAdmission (for the Test button)

SmsSenderIds                       // builds "{ABBR}STDADM" / "{ABBR}EMPADM" / "{ABBR}FEES" from AppConfig.Sms.SchoolAbbreviation
PhoneNumber.NormalizeGh(raw)       // "0XXXXXXXXX"/"+233..."/"233..." -> "233XXXXXXXXX"; returns null if not a plausible GH mobile
```

### Config (`AppConfig.Sms`, backed by `Properties.Settings`)
- `Enabled` (bool, new `SmsEnabled`) — master on/off for real sending.
- `Provider` (existing `SmsProvider`) — `"Arkesel"` | `"LogOnly"`.
- `ApiKey` (existing `SmsApiKey`).
- `SchoolAbbreviation` (new `SmsSchoolAbbreviation`, default `"KPS"`).
- The unused `FromNumber`/`SmsFromNumber` is superseded by the per-duty sender IDs (kept or removed during implementation; not used for sending).

### Arkesel call (v2)
- `POST https://sms.arkesel.com/api/v2/sms/send`
- Header: `api-key: <ApiKey>`
- JSON body: `{ "sender": "<senderId>", "message": "<text>", "recipients": ["233XXXXXXXXX"] }`
- Success = HTTP 200 and `status == "success"` in the JSON; otherwise treat as failure and log the response.

## Data flow

1. **Student registration** (`StudentService`): after a successful insert, fire-and-forget
   - email → guardian (existing), and
   - `SmsService.SendStudentAdmissionAsync(emergencyContact, fullName)` (sender `KPSSTDADM`).
2. **Employee registration** (`EmployeeService`): after insert, fire-and-forget email (existing) + `SendEmployeeAdmissionAsync(contact, fullName)` (sender `KPSEMPADM`).
3. **Weekly reminder** (`frmDashboard.CheckFeeRemindersAsync`): per outstanding student, existing email + `SendFeeReminderAsync(emergencyContact, name, balance)` (sender `KPSFEES`). Existing Monday/once-per-week guard unchanged.

All SMS calls are best-effort and must never throw into the caller or block the UI.

## Error handling

- `Enabled == false` or `Provider == LogOnly` → log only (no network), return success-ish so callers don't error.
- Missing/blank/implausible phone → skip, log `SKIPPED`.
- Arkesel non-success (bad key, unregistered sender ID, HTTP error) → log full response to `logs/sms.log`; surfaced to the user **only** through the Test button.
- Network exceptions are caught; the facade returns `(false, message)` and logs; registration/reminder flows ignore the result.

## Settings UI (extend `frmEmailSettings`)

New "SMS Notifications" group:
- **Enable SMS** checkbox (`SmsEnabled`).
- **Provider** (read-only/combo: Arkesel).
- **API key** textbox (`SmsApiKey`).
- **School abbreviation** textbox (`SmsSchoolAbbreviation`, default `KPS`).
- **Sender IDs preview** label: live-updates to `KPSSTDADM · KPSEMPADM · KPSFEES` as the abbreviation changes, with an 11-char warning.
- **Send test SMS**: phone input + button → `SmsService.SendTestAsync`; shows the provider's success/error message.
- Save writes to `AppConfig.Sms`.

## Testing

- **Unit (pure):** `SmsSenderIds` builds correct IDs and enforces/flags the 11-char limit; `PhoneNumber.NormalizeGh` handles `0…`, `+233…`, `233…`, spaces, and rejects junk.
- **Provider (optional):** `ArkeselSmsProvider` with a mocked `HttpMessageHandler` — asserts URL, `api-key` header, and JSON body; parses success vs error responses.
- **Manual:** Test SMS button against a real Arkesel account with a registered sender ID.
- `LogSmsProvider` keeps the app fully functional with no account/network.

## Out of scope / notes

- Email templates and SMTP are unchanged (verified, not rebuilt).
- Registering the 3 Sender IDs in the Arkesel dashboard is a **manual admin step** (telco approval, usually free, can take a day).
- Future school-info system will supply `SchoolAbbreviation`; until then it defaults to `KPS` and is editable in settings.
