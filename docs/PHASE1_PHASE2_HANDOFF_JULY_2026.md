# Nyansapo ERP Phase 1 and Phase 2 Handoff

Date: 2026-07-11
Scope: Desktop app, Blazor web app, SQL Server database, integration test stability

> Historical handoff snapshot. The `71/71` result below describes the test
> environment available on 2026-07-11. Use `OUTSTANDING_FEATURES_TRACKER.md`
> for the current fresh-clone gates and remaining SQL integration work.

## Current Status

Implementation is paused here so we can switch to another task and return safely later.

The most recent verified milestone is SQL Server access and integration testing. The desktop test master SQL login is working, database integration tests are no longer skipped, and the full desktop test suite passes.

Verified result:

```text
71/71 tests passed. 0 skipped.
```

## What Is Done

### SQL Server Access

- Repaired `Scripts/CreateNyansapoSqlLogins.sql`.
- `nyansapo_test` can now authenticate against SQL Server `master`.
- `nyansapo_test` is enabled, unlocked, and has `dbcreator`.
- `nyansapo_app` is enabled and unlocked, but does not have `dbcreator`.
- The setup script is now safer to rerun because it resets existing logins instead of leaving stale passwords in place.
- The setup script no longer fails when `Neat_Academy_LatestCandidate` does not exist.

### Database Direction

- Current direction remains SQL Server with `SqlClient`.
- Active production code tests confirm no Access/OleDb data access is used in production paths.
- The SQL Server test connection is now usable for real integration testing.

### Phase 1 Stability

- Login logic has passing automated coverage.
- Student identity chain tests pass.
- Trigger-safe insert behavior has been repaired for important database paths.
- Live desktop smoke runner was added and previously passed the main workflow checks.
- Database cleanup for smoke test data is in place.

### Phase 2 Critical Workflow Automation

The live smoke test runner covers:

- Login
- Add student
- Admission approval
- Admission SMS queue
- Fee payment
- Send notice
- Grading
- Report card PDF generation
- Promotion
- Timetable save/load

The latest full automated suite now passes all desktop tests with zero skips.

### Timetable Work Completed So Far

- Department grouping logic exists.
- JHS classes map together for department generation.
- Default timetable periods match the common school day test.
- Insufficient-slot validation has a test and produces a clearer warning.
- Department generation saves all classes without teacher conflicts in tests.

## What Is Left

### Immediate Next Step

Run the actual desktop UI smoke pass manually or with UI automation:

1. Login.
2. Add student.
3. Admission approval.
4. Fee payment.
5. SMS notice.
6. Grading.
7. Report card generation.
8. Promotion.
9. Timetable end-to-end.

The automated tests are clean, but the real WinForms UI still needs human-visible confirmation because some previous issues were UI-specific freezes, clipping, and modal behavior.

### Phase 1 Remaining

- Confirm live database population after restore/promotion is the expected current project data.
- Keep destructive database operations blocked unless explicitly approved.
- Add a pre-test backup/export habit before any workflow that mutates live data.
- Confirm all hard-coded school identity strings are removed from printouts, forms, and web pages.

### Phase 2 Remaining

- Perform the real desktop UI workflow pass listed above.
- Capture any remaining UI freezes or button failures.
- Confirm admission approval sends or queues SMS to the correct emergency/contact number.
- Confirm report card print/export works even when Windows has no default PDF app.
- Confirm grading save/load works with the live SQL schema.
- Confirm promotion updates class, fees, and student records correctly.

### Timetable Remaining

- Finish department-first timetable UX.
- Make department timetable generation produce the whole department, not only one selected class.
- Allow periods to be editable after assigning subjects.
- Support department export/download at once.
- Finalize teacher-based versus subject-based behavior:
  - Teacher-based: simpler generation, no cross-subject teacher conflict checking needed inside one-class-teacher mode.
  - Subject-based: generate at department level and enforce teacher conflict checks.
- Make generated timetable PDF match the school-style output from silence hour to closing.

### Web App Remaining

- Continue replacing placeholder module routes with real pages.
- Finish teacher and headteacher workflows for remarks, conduct, attitude, and performance approval.
- Keep parent portal visibility behind headteacher upload/approval.
- Continue PWA polish after deployment testing.

### Payment Work Remaining

Real Paystack and MTN Momo key testing is intentionally paused until real keys exist.

Still left:

- Keep school-private payment configuration.
- Hide school scope IDs from UI.
- Verify payments server-side before marking fees as paid.
- Validate amount, currency, student, school, and reference.

### CI and Maintainability Remaining

- Add/finish build and test pipeline.
- Remove or ignore generated build artifacts and scratch executables.
- Gradually refactor very large WinForms forms.
- Reduce empty catch blocks and fire-and-forget operations where errors matter.
- Introduce desktop DI gradually where it will reduce manual service wiring.

## Resume Point

When returning to this work, start here:

1. Run the real desktop UI smoke pass.
2. Record any failure with screenshot, error text, and workflow step.
3. Fix failures one by one.
4. Rerun `Tests\Kingdom.Tests` after each fix.
5. Return to timetable department export once the critical desktop flow is visibly stable.

## Useful Commands

Run full desktop test suite with the SQL test login:

```powershell
.\Run-DesktopTests.ps1 -Full -TrustServerCertificate
```

The local-only switch permits the self-signed development certificate. The runner securely prompts for the test password instead of storing it in shell history.

Run live desktop smoke test:

```powershell
dotnet run --project "Tests\Kingdom.Tests\Kingdom.Tests.csproj" --no-build -- --live-smoke
```

Run SQL login setup after SQL Server mixed authentication is enabled:

```powershell
sqlcmd -S . -E -i "Scripts\CreateNyansapoSqlLogins.sql" -v APP_PASSWORD="<app-password>" TEST_PASSWORD="<test-password>"
```

## Current Confidence

Automated backend/database confidence is high for the covered desktop workflows because all tests now run and pass against SQL Server.

UI confidence is medium until the actual WinForms workflow pass is completed, because earlier problems included freezes, clipping, and UI-only behavior that automated service tests cannot fully prove.
