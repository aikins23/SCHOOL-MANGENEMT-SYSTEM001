# Grouped Dashboard Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the flat ~20-button sidebar with collapsible, role-aware nav groups.

**Architecture:** A `CreateNavGroup(header, children)` accordion helper in `frmDashboard`; the nav block is rebuilt to add Dashboard + role-filtered groups + Analytics, omitting empty groups. The old add-all-then-`EnableNavButton`-disable model is removed (inaccessible items are simply not built).

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms (one file: `frmDashboard.cs`).

**Spec:** `docs/superpowers/specs/2026-06-06-grouped-dashboard-nav-design.md`

---

## Conventions

- **No unit-test framework.** Gates: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; offline render of `frmDashboard` per role.
- Single file: `frmDashboard.cs`. Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- Local functions are fine (C# 7.3). `AttachApprovalBadge`, `OpenForm`, `RunBackup`, `ViewLogs`, `OpenAnalyticsDashboard`, `CreateNavButton`, `AccentGold`, `SidebarBackColor` all already exist.

---

### Task 1: Collapsible role-aware nav groups

**Files:** Modify `frmDashboard.cs`.

- [ ] **Step 1: Ensure the List<> using**

If `frmDashboard.cs` lacks `using System.Collections.Generic;` at the top, add it (it currently does not).

- [ ] **Step 2: Add the `CreateNavGroup` helper**

Add this method right after `CreateNavButton` (after its closing brace, ~line 868):

```csharp
        /// <summary>
        /// A collapsible sidebar group: a header row with a chevron that toggles the
        /// visibility of its child nav buttons. Children are pre-filtered by role by the caller.
        /// </summary>
        private Control CreateNavGroup(string header, System.Collections.Generic.List<Button> children)
        {
            var container = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty, BackColor = SidebarBackColor
            };

            var headerBtn = CreateNavButton("▸  " + header, null);   // ▸ collapsed
            headerBtn.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            var childPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 4), Padding = new Padding(8, 0, 0, 0),
                BackColor = SidebarBackColor, Visible = false
            };
            foreach (var c in children) childPanel.Controls.Add(c);

            headerBtn.Click += (s, e) =>
            {
                childPanel.Visible = !childPanel.Visible;
                headerBtn.Text = (childPanel.Visible ? "▾  " : "▸  ") + header; // ▾ / ▸
            };

            container.Controls.Add(headerBtn);
            container.Controls.Add(childPanel);
            return container;
        }
```

- [ ] **Step 3: Rebuild the nav block**

Replace the entire flat block + role-gated blocks — from
`nav.Controls.Add(CreateNavButton("Dashboard", ...));` (line ~363) through the closing brace of
the `if (role == ... Administrator || ... Headmaster) { Database Backup; System Logs }` block
(line ~404, immediately before `navScroll.Controls.Add(nav);`) — with:

```csharp
            nav.Controls.Add(CreateNavButton("Dashboard", null, true));

            var role = AuthService.CurrentUser.Role;
            bool known   = AuthService.CurrentUser.IsAuthenticated;
            bool isAdmin = role == AuthService.UserRole.Administrator;
            bool isHead  = role == AuthService.UserRole.Headmaster;
            bool isDir   = role == AuthService.UserRole.Director;
            bool isAcct  = role == AuthService.UserRole.Accountant;
            bool isTeach = role == AuthService.UserRole.Teacher;

            void Add(System.Collections.Generic.List<Button> g, bool ok, string text, Action act)
            { if (ok) g.Add(CreateNavButton(text, act)); }
            void AddGroup(string title, System.Collections.Generic.List<Button> items)
            { if (items.Count > 0) nav.Controls.Add(CreateNavGroup(title, items)); }

            var students = new System.Collections.Generic.List<Button>();
            Add(students, isAdmin || isHead, "Add Student", () => OpenForm(new frmAddStd(), true));
            Add(students, known, "View Students", () => OpenForm(new frmStdView()));
            AddGroup("Students", students);

            var staff = new System.Collections.Generic.List<Button>();
            Add(staff, isAdmin || isHead, "Add Employee", () => OpenForm(new frmEmployee()));
            Add(staff, isDir || isAdmin || isHead || isAcct, "View Employees", () => OpenForm(new frmEmpView()));
            AddGroup("Staff", staff);

            var leave = new System.Collections.Generic.List<Button>();
            Add(leave, known, "Apply for Leave", () => OpenForm(new frmEmpLeave()));
            Add(leave, known, "Leave Requests", () => OpenForm(new frmLeaveDetails()));
            AddGroup("Leave", leave);

            var academics = new System.Collections.Generic.List<Button>();
            Add(academics, isAdmin || isTeach || isHead, "Exams", () => OpenForm(new EXAMS()));
            Add(academics, known, "Exam Reports", () => OpenForm(new EXAMSVIEW()));
            Add(academics, isDir || isAdmin || isHead, "Subjects", () => new frmSubjects().ShowDialog());
            AddGroup("Academics", academics);

            var finance = new System.Collections.Generic.List<Button>();
            Add(finance, isAdmin || isAcct || isHead, "Fees Payment", () => OpenForm(new frmFessPayment()));
            Add(finance, isAcct || isDir || isAdmin || isHead, "Payment History", () => OpenForm(new frmPaymentHistory()));
            if (isAcct)
            {
                _approvalsNavBtn = CreateNavButton("Admission Approvals", () => OpenForm(new frmPendingApprovals()));
                AttachApprovalBadge(_approvalsNavBtn);
                finance.Add(_approvalsNavBtn);
            }
            AddGroup("Finance", finance);

            var ops = new System.Collections.Generic.List<Button>();
            Add(ops, isDir || isAdmin || isHead, "Library", () => new frmLibrary().ShowDialog());
            Add(ops, isDir || isAdmin || isHead, "Transport", () => new frmTransport().ShowDialog());
            AddGroup("Operations", ops);

            var admin = new System.Collections.Generic.List<Button>();
            Add(admin, isDir || isAdmin || isHead, "Settings", () => new frmEmailSettings().ShowDialog());
            Add(admin, isDir || isAdmin || isHead, "School Information", () => new frmSchoolInfo().ShowDialog());
            Add(admin, isDir || isAdmin || isHead, "Grading Scheme", () => new frmGradingScheme().ShowDialog());
            Add(admin, isAdmin || isHead, "Database Backup", RunBackup);
            Add(admin, isAdmin || isHead, "System Logs", ViewLogs);
            AddGroup("Administration", admin);

            if (known) nav.Controls.Add(CreateNavButton("Analytics", OpenAnalyticsDashboard));
```

(Keeps Dashboard selected/top and Analytics standalone; `_approvalsNavBtn` + badge preserved inside Finance. The bottom-docked Exit/Sign Out/footer code that follows is unchanged.)

- [ ] **Step 4: Simplify `ApplyRolePermissions`**

Access is now decided at build time, so the disable pass is obsolete. Replace the whole
`ApplyRolePermissions` method body (lines ~160-186) with:

```csharp
        private void ApplyRolePermissions()
        {
            var role = AuthService.CurrentUser.Role;
            statusLabel.Text = $"Signed in as {AuthService.CurrentUser.Username} ({role})";
        }
```

- [ ] **Step 5: Remove the now-unused `EnableNavButton` method**

Delete the entire `private void EnableNavButton(string text, bool enabled) { ... }` method
(lines ~188-217, through its closing brace). Nothing references it after Step 4.

- [ ] **Step 6: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If the compiler flags an unresolved `EnableNavButton` call, a Step-4
remnant remains — remove it. If `List<>` is unresolved, Step 1 wasn't applied.)

- [ ] **Step 7: Render-verify per role (offline harness)**

Render `frmDashboard` for **Director**, **Accountant**, and **Teacher** (set the `CurrentUser`
backing field's `Role`); confirm via the nav-button/secondary enumeration:
- Group **header** rows exist (e.g. "▸  Students", "▸  Finance") and child buttons start hidden.
- A role with no items in a group has no header (Teacher → no "Administration", no "Finance"
  beyond what it can access; Accountant → "Finance" present with Admission Approvals).
- Dashboard + Analytics present as standalone; Exit/Sign Out still pinned.
(If LocalDB is down the dashboard still constructs its nav synchronously — enumerate buttons as in
the earlier nav probe.)

- [ ] **Step 8: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(nav): collapsible role-aware sidebar groups (replace flat nav + disable model)"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test:** sign in as Director — sidebar shows Dashboard, then collapsed groups
  (Students, Staff, Leave, Academics, Finance, Operations, Administration), Analytics, and pinned
  Exit/Sign Out. Click a header → it expands; click again → collapses. Every child button opens
  its form. Sign in as Teacher → only groups/items a teacher can use appear (no Administration).
  Accountant → Finance shows Fees Payment, Payment History, Admission Approvals (with its badge).

## Self-review notes

- **Spec coverage:** accordion helper (Step 2) ✓; Dashboard/Analytics standalone + groups
  (Step 3) ✓; role-filtered children + omit-empty-group (Step 3 `Add`/`AddGroup`) ✓; collapsed
  default + chevron toggle + multi-open (Step 2) ✓; hide-instead-of-disable, `EnableNavButton`
  removed (Steps 4-5) ✓; approvals badge preserved (Step 3 Finance) ✓; footer unchanged ✓.
- **Role mapping** mirrors the prior gating: Add Student/Employee = Admin/Headmaster; View
  Students/Exam Reports/Analytics = any authenticated; View Employees = Dir/Admin/Head/Acct;
  Fees Payment = Admin/Acct/Head; Payment History = Acct/Dir/Admin/Head; Subjects/Library/
  Transport/Settings/SchoolInfo/Grading = Dir/Admin/Head; Backup/Logs = Admin/Head; Approvals =
  Acct. Exams = Admin/Teacher/Head.
- **No placeholders:** every step has complete code; line numbers are guides (match by text).
- **Risk:** the Step-3 replacement spans the old flat block + 4 role `if` blocks — match from the
  `Dashboard` add line through the end of the Backup/Logs block, leaving `navScroll.Controls.Add(nav);`
  intact.
```
