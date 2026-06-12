# Student Import/Export + Parent Credentials — Design

**Date:** 2026-06-12
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

Schools adopting the system have hundreds of existing ("old") students that are too slow to enter
one-by-one through the admission form. The desired flow: **export** the student sheet (CSV), fill
it offline (new rows and/or corrections), **import** it back, and have the system automatically
create a guardian login per student and **SMS the username + default password to the guardian's
phone number**.

A draft already exists (`Services/CsvImportExportService.cs` + Import/Export buttons on
`frmStdView`), but review found defects that make it unshippable:

1. **Fatal:** accounts are registered with role `"STUDENT"`, which `AuthService.ParseRole` does not
   know → maps to `UserRole.Unknown` → the SMSed credentials can never log in.
2. Username = raw numeric StudentID; usernames must be 3–20 chars, so 1–2 digit IDs silently fail
   to register (no account, no SMS).
3. `new Random()` constructed per row → rows in the same clock tick get identical passwords.
4. ID round-trip bug: a sheet containing the display form `KPS9016` doesn't match the stored
   numeric ID → imported as a NEW student with a corrupt string ID.
5. White-label leaks: hardcoded SMS sender `"NYANSAPO"`, hardcoded link `nyansapoerp.edu.gh`,
   password suffix `"kps"`.
6. Failed rows are only logged — the operator gets no row-level feedback.
7. `DateTime.TryParse` is locale-dependent for the DOB column.
8. The import's DB work runs on the UI thread (OleDb's async APIs block), freezing the grid.

## Goal

A reliable export→fill→import round trip on the student view that (a) adds or updates students in
bulk, (b) creates one **Parent** account per imported student with a derived username and a strong
random password, and (c) SMSes those credentials to the guardian's number — offline-safe via the
SMS outbox, white-label-clean, with row-level error reporting.

## Decisions (from brainstorming)

- **Account role = PARENT** (existing `UserRole.Parent`); the credentials are for the guardian and
  will authenticate against the future parent portal. No auth-system changes.
- **Fees on import = same as admission**: `StudentService.AddStudentAsync` already creates the
  standard opening fee + balance records for new students; imports keep that behaviour (onboard at
  term start; accountants adjust via payments if needed).
- **Portal URL = configurable setting** (`SchoolInformation.PortalUrl`); the credentials SMS
  includes the link only when the setting is non-blank.

## Scope

In scope: fixes to `CsvImportExportService`, a typed credentials SMS in `SmsService`, the
`PortalUrl` setting (model + repo column + `SchoolProfile` accessor + School Information screen
field), `frmStdView` import UX (confirmation, off-UI-thread work, row-level result summary), and
unit tests for the new pure helpers.

Out of scope: the parent portal itself; Excel `.xlsx` parsing (CSV opens in Excel); student photos
in the sheet; changing the export column set.

## Components

### CSV format (unchanged columns)

```
StudentID,FirstName,LastName,DateOfBirth,Gender,ClassID,Email,HomeTown,Residence,Allergies,GuardianName,GuardianEmail,GuardianLocation,EmergencyContact
```

- Export emits the **numeric** StudentID. Import accepts numeric (`9016`) **or** display
  (`KPS9016`) form via `Common.StudentId.Parse`.
- Blank `StudentID` ⇒ new student, ID auto-generated (existing behaviour).
- `EmergencyContact` is the guardian phone the credentials SMS goes to.

### `Common/ImportCredentials.cs` (new, pure → unit-testable)

- `static string UsernameFor(string studentId)` → `StudentId.Display(studentId).ToLowerInvariant()`
  (e.g. `kps9016`; the abbreviation prefix guarantees the 3-char minimum).
- `static string NewPassword()` → 8+ chars satisfying `ValidationHelper.IsStrongPassword`
  (uppercase + lowercase + digits), generated from a single `RNGCryptoServiceProvider`-backed
  source shared across calls (distinct per call, no brand suffix). Format: `Pw` + 4 random digits +
  2 random lowercase letters (e.g. `Pw4821xq`).

### `Services/CsvImportExportService.cs` (fix in place)

Import changes per row:
- `StudentId.Parse` the ID column before the existing-student lookup.
- DOB parsing: `TryParseExact` `yyyy-MM-dd` then `dd/MM/yyyy`, then `TryParse` fallback (today's
  default-age fallback retained).
- On successful add/update with a non-blank `EmergencyContact`:
  - `username = ImportCredentials.UsernameFor(id)`; `password = ImportCredentials.NewPassword()`;
  - `AuthService.RegisterAsync(username, password, password, "PARENT", null)`;
  - on success → `SmsService.SendParentCredentialsAsync(phone, studentName, username, password)`;
  - on "username exists" → skip silently (re-import safety: no duplicate SMS, no password reset).
- **Row-level errors**: collect `(lineNumber, reason)` for malformed/failed rows; the result tuple
  gains a `Failed` list/summary string surfaced to the UI (first ~10 itemised, plus a count).

### `Services/SmsService.cs`

- New `SendParentCredentialsAsync(string recipient, string studentName, string username,
  string password)`:
  - sender ID `SmsSenderIds.StudentAdmission`; school identity via `SchoolProfile.DisplayName`;
  - appends `Portal: <url>` only when `SchoolProfile.PortalUrl` is non-blank;
  - routes through the durable `SendSmsAsync` (outbox) — offline imports queue the SMS for the
    flusher.

### Portal URL setting

- `Models/SchoolInformation.PortalUrl` (default `""`), idempotent
  `ALTER TABLE SchoolInformation ADD PortalUrl NVARCHAR(200)` in `SchoolInfoRepository`
  (same pattern as the colour columns), read/write in `GetAsync`/`SaveAsync`,
  `SchoolProfile.PortalUrl` accessor, and a "Parent Portal URL" textbox on `frmSchoolInfo`.

### `frmStdView.cs`

- Pre-import confirmation: parse the file first, show "N data rows found — proceed?".
- Wrap `ImportStudentsFromCsvAsync` / `ExportStudentsToCsvAsync` calls in `Task.Run` (OleDb blocks
  the UI thread) with a wait cursor and disabled buttons during the run.
- Result dialog shows processed / failed / SMS-sent counts plus the row-level failure summary;
  grid refreshes after import.

## Data flow

Export → CSV (numeric IDs) → school fills/edits rows → Import: parse → per row add/update student
(opening fees as per admission for new) → register PARENT account (`kps####`) → SMS credentials to
`EmergencyContact` via outbox → result summary with per-row failures → grid refresh. Re-import of
the same sheet updates students but sends no duplicate SMS (usernames already exist).

## Error handling / edge cases

- Malformed row (< 14 columns) → recorded in the failure list with its line number (no silent skip).
- Blank guardian phone → student imported, no account/SMS (counted, reported).
- Invalid phone → `SendSmsAsync` skips (existing normalisation); recorded.
- Existing username → no re-registration, no SMS (prevents password churn on re-import).
- Offline import → accounts created; SMSes sit Pending in `SmsOutbox` and flush on reconnect.
- Import exception mid-file → counts + failures up to that point returned; already-imported rows
  persist (idempotent to re-run thanks to the username/update guards).
- `PortalUrl` blank → SMS simply omits the link line.

## Testing / verification

- `dotnet build` → 0 errors; suite green.
- **Unit tests (Kingdom.Tests):** `ImportCredentials.UsernameFor` (numeric + display + short IDs);
  `NewPassword` satisfies `ValidationHelper.IsStrongPassword` and differs across consecutive calls.
- **Manual (user):** export current students; add a row with blank ID + a real phone; import →
  student appears with opening fees, guardian receives one SMS with working-format credentials;
  re-import the same file → no second SMS; a row with `KPS<existing id>` updates rather than
  duplicates.

## Success criteria

- Export→fill→import round trip works for both new and existing students, including display-form IDs.
- Every imported student with a guardian phone yields exactly one PARENT account (`kps####`) and
  exactly one credentials SMS, white-label-clean, offline-safe.
- The operator sees exactly which rows failed and why; the UI never freezes during import.
