# Additional Fees Implementation Notes

Date: 2026-07-11

> Historical implementation note. Test totals below record the suite at the time
> this slice was introduced; use `OUTSTANDING_FEATURES_TRACKER.md` for the
> current release gate and verification evidence.

## Implemented Backend Slice

The Additional Fees module now has a tested backend foundation for the desktop application.

Implemented files:

- `Models/AdditionalFee.cs`
- `Data/IAdditionalFeeRepository.cs`
- `Data/AdditionalFeeRepository.cs`
- `Services/AdditionalFeeService.cs`

Project wiring:

- `kingdom_Preparatory_School_Management_System.csproj`
- `Tests/Kingdom.Tests/Program.cs`

## Supported Workflow

The service now supports:

1. Create additional fee draft.
2. Assign fee as:
   - Flat amount for all students.
   - Different amount by class.
   - Different amount by department.
3. Preview affected students and expected total.
4. Submit fee for approval.
5. Block creator from approving their own fee.
6. Approve fee and post charges to student accounts.
7. Reject fee with a reason.
8. Prevent duplicate open fees for the same fee name, academic year, and term.

## Tables Created

The repository creates these tables if missing:

- `AdditionalFees`
- `AdditionalFeeAmounts`
- `AdditionalFeeStudentCharges`
- `AdditionalFeeAuditLog`

## Posting Behavior

Approval posts charges into:

- `AdditionalFeeStudentCharges` for the module-specific audit trail.
- `fees` so the charge appears as a student fee record.
- `payment_record` as a zero-payment balance row so existing outstanding-balance logic sees the new debt.

This is necessary because the current outstanding fee screens use the latest `payment_record.Balance`, not only the sum of `fees`.

## Department Mapping

Department targeting uses the existing school grouping from `Common/TimetableDepartments.cs`:

- Preschool
- Kindergarten
- Lower Primary
- Upper Primary
- Junior High School

## Verified Tests

Added and passed:

- `AdditionalFeeService previews and posts approved flat fees`
- `AdditionalFeeService targets class and department amounts`
- `AdditionalFeeService blocks duplicate open fees`

Full result after implementation:

```text
74/74 tests passed. 0 skipped.
```

## Next UI Work

Build a desktop form, likely `frmAdditionalFees`, under Finance.

Recommended UI sections:

1. Fee details:
   - Fee name
   - Description
   - Academic year
   - Term
   - Due date
   - Compulsory/optional
   - Part-payment allowed

2. Assignment mode:
   - Flat for all students
   - By class
   - By department

3. Amount grid:
   - Show class rows for class mode.
   - Show department rows for department mode.
   - Show one amount for flat mode.

4. Preview panel:
   - Affected students count
   - Expected total
   - Target classes/departments

5. Approval actions:
   - Save draft
   - Submit for approval
   - Approve
   - Reject/return

6. History grid:
   - Fee name
   - Term/year
   - Amount summary
   - Status
   - Created by
   - Approved by

Permission expectations:

- Accountant: create, edit draft, submit.
- Headmaster/Director/Administrator: approve, reject, return.
- Creator should not approve their own fee.

## Later Enhancements

- Add student exemptions with approval.
- Add overdue notifications.
- Add report/PDF exports for additional fee status.
- Add web UI after desktop workflow is stable.
