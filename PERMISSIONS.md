# RBAC Permission Matrix

This document defines which user roles can access which screens / actions in the
Kingdom Preparatory School Management System.

**Legend**

- ✅ = Full access (open + edit)
- 👁 = Read-only (can open, cannot modify/save)
- ❌ = Blocked (form will not open; sidebar item hidden)

**Roles** (defined in `Services/AuthService.cs` → `UserRole` enum)

- **Director(s)** — school owner(s) / board. Top of the hierarchy. Full
  visibility across academics, HR, and finance. Sets policy; usually does not
  do day-to-day data entry.
- **Administrator** — IT/system administrator. Maintains the application
  itself (users, backups, SMTP) and acts as the office data-entry clerk.
- **Headmaster** — head of school. Day-to-day oversight across academics,
  HR, and discipline.
- **Teacher** — classroom-facing staff. Students, exams, attendance,
  report cards.
- **Accountant** — bursar / finance officer. Fees, payments, defaulters.
- **Parent** *(web app only — phase 2)* — guardian of one or more enrolled
  students. Read-only access via the upcoming parent portal; **no access to the
  desktop application**. See the "Parent Web App" section below for scope.

---

## Matrix (Desktop App)

> Edit the cells below to override the defaults. The enforcement code reads
> this file as the source of truth.

> **Parent** rows describe access in the **upcoming web app** — parents
> don't log into the desktop app at all (desktop login rejects
> `UserRole.Parent`). Every Parent ✅/👁 cell is automatically scoped to the
> parent's own ward(s) via the `ParentStudents` junction table. See the
> "Parent Web App" section below for full scope.

| Form / Module                                  | Director | Admin | Headmaster | Teacher | Accountant | Parent |
| ---------------------------------------------- | :------: | :---: | :--------: | :-----: | :--------: | :----: |
| **Dashboard & analytics**                      |          |       |            |         |            |        |
| `frmDashboard`                                 |    ✅     |   ✅   |     ✅ (without fees records)      |    ❌ — new `frmTeacherDashboard` to be built    |     ✅      |   ❌    |
| `frmDashboardCharts` (21 analytics charts)     |    ✅     |   ✅   |     ✅ (without fees charts)      |    ✅ (own class only)    |     ✅      |   ✅ (own ward(s) only)    |
| **Students**                                   |          |       |            |         |            |        |
| `frmAddStd` (add student)                      |    ✅     |   👁   |     ✅      |    ❌    |     ❌      |   ❌    |
| `frmStdView` / `frmStdDetails`                 |    👁     |   👁   |     ✅      |    ✅ (own class only)    |     👁      |   ❌    |
| `frmStudentPromotion`                          |    ✅     |   👁   |     ✅ (approves all promotions)      |    ✅ (own class only)    |     ❌      |   👁 (own ward(s) only)    |
| **Employees / HR**                             |          |       |            |         |            |        |
| `frmEmployee` (add employee)                   |    ✅     |   👁   |     ❌      |    ❌    |     ❌      |   ❌    |
| `frmEmpView` / `frmEmpDetails`                 |    ✅     |   👁   |     ✅      |    ❌    |     👁      |   ❌    |
| **Leave**                                      |          |       |            |         |            |        |
| `frmEmpLeave` (apply for leave)                |    ✅     |   ✅   |     ✅      |    ✅    |     ✅      |   ❌    |
| `frmLeaveApproval`                             |    ✅     |   👁   |     ✅      |    ❌    |     ❌      |   ❌    |
| `frmLeaveDetails` / `EmpleaveView`             |    ✅     |   👁   |     ✅      | 👁 (own) |  👁 (own)  |   ❌    |
| `frmLeaveBalanceReport`                        |    ✅     |   ✅   |     ✅      |    ✅    |     ✅      |   ❌    |
| **Academics**                                  |          |       |            |         |            |        |
| `EXAMS` (enter scores)                         |    👁     |   👁   |     ✅      |    ✅    |     ❌      |   ❌    |
| `EXAMSVIEW` / `examsviewdetails`               |    ✅     |   👁   |     ✅      |    ✅    |     ❌      |   ❌    |
| `frmAttendance`                                |    👁     |     👁 |     ✅      |    ✅    |     ❌      |   ❌    |
| `frmClassAdmin`                                |    ✅     |   👁   |     ✅      |    ❌    |     ❌      |   ❌    |
| `Forms/GenerateReportCardsForm`                |    ✅     |   👁 |     ✅      |    ✅    |     ❌      |   ❌    |
| **Finance**                                    |          |       |            |         |            |        |
| `frmFess` (fees setup)                         |    ✅     |   👁   |     👁      |    ❌    |     ✅ (Director approval)      |   ❌    |
| `frmFessPayment` (record payment)              |    ❌     |   ❌   |     ❌      |    ❌    |     ✅      |   ❌    |
| `frmOutstandingFees` (defaulters)              |    ✅     |   👁   |     ✅      |    👁 (own class only)    |     ✅      |   ✅ (own ward(s) only)    |
| **System / Admin**                             |          |       |            |         |            |        |
| `frmRegistration` (create user accounts)       |    ✅     |   👁   |     ❌      |    ❌    |     ❌      |   ❌    |
| `frmBackupManager` (backup / restore database) |    ✅     |   👁   |     ❌      |    ❌    |     ❌      |   ❌    |
| `frmEmailSettings` (SMTP configuration)        |    ✅     |   ✅   |     ❌      |    ❌    |     ❌      |   ❌    |

---

## Parent Web App (Phase 2)

The parent role exists **only in the upcoming web app**, not the desktop
application. Parents authenticate against the same `Users` table (same
`AuthService.HashPassword` flow), but they never load WinForms screens.

### What a parent can see

All views are **read-only** and **scoped to their own child(ren)**.

| Web view             | Scope                                                              |
| -------------------- | ------------------------------------------------------------------ |
| Child profile        | Their child's `Students` row only (name, class, photo, allergies)  |
| Attendance           | Their child's `Attendance` rows; monthly summary + last 30 days    |
| Exam results         | Their child's `examss` rows; per term, per subject + grade         |
| Report card          | PDF download — same generator as desktop, filtered to their child  |
| Fee statement        | Their child's `payment_record` rows; balance, history, receipts    |
| School announcements | Public broadcast messages (filtered to "all parents" audience)     |
| Profile settings     | Change their own password, update contact email/phone              |

### What a parent **cannot** see (and the system must enforce)

- Any other student's records (siblings excepted — see multi-child note below)
- Any staff/employee data
- Class lists, exam-entry forms, attendance entry
- Internal fees configuration (`frmFess` schedule); they only see what *their*
  child owes
- Anything from the Leave/HR module
- Any dashboard or chart that aggregates across students

### Data-model prerequisites for the web app

These are net-new and must exist before the web app is meaningful:

1. **Link parents to students.** Currently `Students` has `GuidanceName`,
   `GuidianceEmail`, `Guidiance_Location` — name/email free text only. No
   foreign key. Recommend a junction table:

   ```
   ParentStudents (
       ParentUsername   varchar  FK → Users.Username
       StudentID        int      FK → Students.StudentID
       Relationship     varchar  (Mother/Father/Guardian/Other)
   )
   ```

   Supports siblings (one parent ↔ many students) and shared custody
   (one student ↔ many parents).

2. **Parent self-registration flow** — invite-only initially: the office
   creates a Parent user, system emails an activation link with a one-time
   token, parent sets their own password. Prevents random sign-ups.

3. **Web-app-friendly auth.** `AuthService` currently uses a process-static
   `CurrentUser` (fine for WinForms, broken for web). The web app needs
   per-request session/JWT — likely a separate `AuthController` reusing
   `HashPassword`/`VerifyPassword` from `AuthService` but maintaining its own
   session store.

4. **Read-only API endpoints** — one per row in the "What a parent can see"
   table. All endpoints must filter by `ParentStudents.ParentUsername =
   currentUser` server-side; never trust a client-sent student ID.

### Out of scope (for now)

- Online fee payment (Paystack / Hubtel) — flagged for Phase 3.
- Parent-teacher messaging — separate feature.
- Push/SMS notifications — separate feature (email already exists).

---

## Rationale for non-obvious calls

These are the choices most likely to be wrong for your school's structure —
review them and flip if needed.

- **Director ≥ Headmaster ≥ Admin** in school authority, but **Admin ≥
  Director** in technical scope. The Director owns the school; the Admin
  configures the software. So Director is locked out of `frmEmailSettings`
  (raw SMTP config), but gets `frmBackupManager` (they own the data) and
  `frmRegistration` (they approve hires).

- **Director blocked from recording payments** (`frmFessPayment`). Same
  segregation-of-duties guard as Headmaster — owners don't punch in cash
  receipts at the bursar's terminal. They can still see fees, defaulters, and
  totals on dashboards.

- **Director gets read-only on score entry and attendance.** They review and
  audit, they don't fill in marks or daily registers.

- **Director can approve leave.** Final-instance approval, over the
  Headmaster's head if needed.

- **Headmaster ≠ Administrator.** Headmaster oversees the school; Admin
  maintains the system. So Headmaster doesn't get backup/restore, user
  registration, or SMTP settings — those are IT concerns.

- **Headmaster blocked from recording payments** (`frmFessPayment`). Can see
  fee balances and outstanding lists, but cash entry stays at the bursar.

- **Accountant gets read-only on students/employees.** Needs name/class
  lookup when reconciling payments, but shouldn't be editing records.

- **Teacher blocked from class admin** (`frmClassAdmin`). Teachers work
  *within* classes but shouldn't be the ones creating/renaming them or
  reassigning class teachers.

- **Everyone (including Director/Admin/Accountant) can apply for leave.**
  They're employees too — `frmEmpLeave` is a self-service form.

- **Teacher leave records are own-only.** A teacher shouldn't see another
  teacher's leave history. The `(own)` qualifier means the view is filtered to
  `Attendance.ReferenceID = CurrentUser.EmploymentID`.

- **Parent has zero desktop access.** Even read-only viewing of their child's
  records is reserved for the web app. Mixing parent traffic with staff
  desktops opens too many side channels (USB exports, shared sessions).
  The Parent column in the matrix describes what the **web app** will expose
  per form's underlying data — not desktop access.

---

## Open questions for you to decide

Edit your answers under each question — these are the points I was least
confident about.

1. **Should the Director enter scores / attendance themselves, or only review?**
   Current default: **Read-only (👁)** — they review, don't enter.
   _Your decision:_

2. **Should the Headmaster create user accounts (`frmRegistration`)?**
   Some schools put user provisioning on the head; others keep it IT-only.
   Current default: **No** (Director + Admin only).
   _Your decision:_

3. **Should the Accountant see exam results (`EXAMSVIEW`)?**
   Useful context for "withhold results from defaulters" workflows.
   Current default: **No** (blocked).
   _Your decision:_

4. **Should Teachers have read-only access to *other* teachers' leave?**
   Current default: **No** — own records only.
   _Your decision:_

5. **Should the Headmaster be able to register new employees (`frmEmployee`)?**
   Or is that strictly an HR/Director task?
   Current default: **No** — Director + Admin only (you set this).
   _Your decision:_

6. **Should the Accountant be able to view student details
   (`frmStdView` / `frmStdDetails`) read-only, or be fully blocked?**
   Current default: **Read-only** (👁).
   _Your decision:_

7. **Should the Director be able to configure SMTP (`frmEmailSettings`)?**
   Current default: **No** (Admin only — it's a technical setting).
   _Your decision:_

8. **Parent webapp: can siblings see each other's records via a shared parent
   login?** I assumed yes — one parent login lists all their children. The
   alternative is one parent account per child (less convenient but less leakage).
   Current default: **Yes** — all linked children visible.
   _Your decision:_

9. **Parent webapp: should parents see fee receipts (PDF) immediately, or
   only after the Accountant marks the payment "confirmed"?**
   Current default: **Immediately** on `payment_record` insert.
   _Your decision:_

---

## How enforcement will work (proposed implementation)

Once you've finalized the matrix, the code changes will be:

1. **`Services/AuthService.cs`** — add two static helpers:

   ```csharp
   public static bool CanAccess(string formKey);
   public static bool RequireAccess(string formKey, Form form);
   ```

   `RequireAccess` shows a "Permission denied" dialog and closes the form if
   the current user's role isn't allowed.

2. **Permission table** lives in code as a `Dictionary<string, UserRole[]>` —
   one entry per row in this document.

3. **Each form's `Load` handler** calls
   `AuthService.RequireAccess("frmFessPayment", this)` as its first line.

4. **`Common/NavigationSidebar.cs`** filters its button list by
   `AuthService.CanAccess(...)` so users only see what they can open.

5. **Read-only (👁) enforcement** is form-specific: typically disables Save
   buttons and sets `DataGridView.ReadOnly = true`.

6. **`frmRegistration`** dropdown for User Type must offer "Director" and
   "Parent" as options (currently only Admin/Teacher/Accountant/Headmaster).
   *Parent will normally be created via the web app's invite flow, but the
   desktop registration form should still support it for office staff.*

7. **Desktop login blocks Parent role.** When `ParseRole` resolves to
   `UserRole.Parent`, the desktop login screen rejects the attempt with
   "Please use the parent web portal" — even before the dashboard loads.

No new database tables are needed for the **desktop RBAC** rollout — the role
is already stored in `Users.User_Type` and parsed by `AuthService.ParseRole`.
The enum already recognizes `"DIRECTOR"`/`"DIRECTORS"` and
`"PARENT"`/`"GUARDIAN"`. The `ParentStudents` junction table is only needed
when work on the **web app** begins.
