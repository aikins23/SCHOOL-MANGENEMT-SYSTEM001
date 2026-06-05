# Admission Payment with Bursar Approval (Draft-based) — Design

**Date:** 2026-06-05
**Status:** Approved (design)

## Goal

Separate **admission** (done by an Administrator) from **fee collection** (done by the Bursar/Accountant). When an administrator admits a student and records the initial payment, **nothing is written to the live tables and no SMS/receipt goes out** until the **bursar approves**. The admission is held as a **draft** until then.

## Workflow

1. **Admin** fills the admission form (`frmAddStd`). On submit, the modified **`frmFessPayment` (prefilled with the new student)** captures the initial payment:
   - **Admission fee: GHS 100** (fixed, shown read-only).
   - **School fee paid now** — must be **≥ 50%** of the term total (`StudentService.GetFeeForClass(class)`).
2. **Validation to submit:** admission fee (GHS 100) **and** school fee ≥ 50% of the term total. On **"Submit for Approval"**, the full student record (incl. photo) + the payment amounts are written to a new **`DraftAdmissions`** table. **No `Students` row, no `payment_record` row, no SMS, no receipt.**
3. **Bursar (Accountant)** sees pending drafts in **both**:
   - a **"Pending Approvals" tab** inside `frmFessPayment`, and
   - a **"Pending Admission Payments (N)"** card/alert on the bursar's dashboard (`frmDashboard`, which the Accountant uses).
4. **On Approve:**
   - Promote the draft → create the real **`Students`** record (assigns the StudentID) + the initial school-fee balance record.
   - Record the **admission fee** and the **school-fee payment** as real `payment_record` rows.
   - Print **two receipts**: one **Admission Fee** receipt, one **School Fees** receipt.
   - Fire the **admission SMS** to the guardian (now with the assigned StudentID and the payment lines).
   - **Delete the draft** (data now lives in the real tables).
5. **On Reject/Discard:** **delete the draft.** No student is ever created.

`DraftAdmissions` is purely a **pending queue** — a draft exists only while pending; approval or rejection removes it. No status column is needed.

## Decisions (locked)

- Admission fee = **GHS 100**, a hardcoded constant `AdmissionFees.Amount` for now (later driven by school-info settings).
- School fee at admission must be **≥ 50%** of the class term total.
- **Admin cannot finalize fees** — only submit a draft for approval. **Only the Accountant approves.**
- **SMS + receipts are gated on approval.** Today's immediate admission SMS is removed.
- Approved drafts are **discarded** after promotion; pending drafts are **kept** until acted on.

## Data model

### New table `DraftAdmissions`
Created via an idempotent setup routine (`CREATE TABLE` if not exists), mirroring the desktop's existing setup pattern.

| Column | Type | Notes |
|---|---|---|
| DraftID | AUTOINCREMENT PK | |
| FirstName, LastName | nvarchar | |
| DOB | datetime | |
| Gender, ClassID, Email, HomeTown, Residence, Allegies | nvarchar | (legacy spelling `Allegies` to match Students) |
| EmergencyConatct, GuidanceName, GuidianceEmail, Guidiance_Location | nvarchar | legacy spellings to match `Students` |
| admission_date | datetime | |
| Std_pic | varbinary(max) / image | the student photo |
| AdmissionFee | money | = 100 |
| SchoolFeePaid | money | amount paid now (≥ 50% of TermTotal) |
| TermTotal | money | `GetFeeForClass(ClassID)` snapshot |
| PaymentMode | nvarchar | Cash/Cheque (entered at submission) |
| SubmittedBy | nvarchar | admin username |
| SubmittedDate | datetime | |

### Existing tables
- `Students` and `payment_record` are written **only on approval** — their shape is unchanged. The StudentID is assigned at approval (existing `@@IDENTITY` flow).
- On approval the school-fee balance is set the same way admission does today (`AddInitialFeeRecordAsync` + `AddInitialPaymentRecordAsync` for the term total) and then the school-fee payment is recorded via `AddPaymentRecordAsync` (Balance = TermTotal − SchoolFeePaid). The admission fee is recorded as a separate `payment_record` row tagged so it does not affect the school-fee balance (Balance carried = current school balance; `payment_mode` annotated `Admission Fee`). The school-fee row is written **last** so `GetLatestBalanceAsync` returns the correct school balance.

## Components

- **`frmAddStd`** — "Save" becomes **"Submit for Approval"**: builds the student object (no DB write) and opens the prefilled payment step.
- **`frmFessPayment`** — new **admission mode**: prefilled student (read-only), GHS 100 admission field, school-fee input with ≥50% validation, **"Submit for Approval"** (writes the draft). Plus a **"Pending Approvals" tab**: lists pending drafts with **Approve** / **Reject**.
- **Bursar dashboard card** — "Pending Admission Payments (N)" on the Accountant's `frmDashboard`, opens the approval list.
- **`DraftAdmissionService` + `DraftAdmissionRepository`** — create/list/get/delete drafts; an `ApproveAsync(draftId, bursarName)` that performs the promotion atomically (student + fee + payments) and returns the data needed for receipts + SMS.
- **`StudentService`** — its admission path no longer sends the SMS directly; the approval path owns the SMS.
- **SMS builder** — a new variant of the admission message including the two payment lines:
  > … details … • **Admission fee paid – GHS 100.00** • **School fee paid – GHS X out of GHS {TermTotal}** …
- **Two-receipt generation** — reuse the existing receipt rendering, invoked once per fee type on approval.
- **`AdmissionFees`** constant class (`Amount = 100m`).

## Error handling

- Submission validation (admission fee present, school fee ≥ 50%) blocks "Submit for Approval" with a clear message.
- Approval is transactional: if promotion fails partway, nothing is committed and the draft stays pending (bursar can retry).
- SMS + receipts are best-effort **after** the DB promotion commits — a failed SMS never rolls back an approved admission (it's logged, consistent with the rest of the app).
- Photos are size-validated at submission (existing `AppConfig.MaxPhotoSizeBytes`).

## Testing / verification

- Build gate (`dotnet build`, 0 errors).
- Submit a draft → confirm `DraftAdmissions` has the row and `Students`/`payment_record` do **not**.
- Approve → confirm `Students` + two `payment_record` rows exist, the school balance = TermTotal − SchoolFeePaid, the draft is deleted, two receipts render, and the SMS message (offline render) shows the payment lines + assigned StudentID.
- Reject → draft deleted, no `Students` row.
- Validation: school fee < 50% is blocked.

## Out of scope (for now)

- School-info-driven fee configuration (admission fee + per-class fees from settings) — admission fee stays the hardcoded GHS 100; school fees stay `GetFeeForClass`.
- Editing a draft after submission (bursar approves or rejects; admin re-submits if rejected).
- Any change to employee registration.
