# Grouped (Collapsible) Dashboard Navigation — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The dashboard sidebar (`frmDashboard`) has grown to ~20 flat nav buttons (Students, Staff,
Fees, Exams, Leave, Payment History, Settings, School Information, Grading Scheme, Subjects,
Library, Transport, Backup, Logs, …). It's long, unscannable, and mixes unrelated actions.

## Goal

Group related nav buttons under collapsible section headers so the sidebar is compact and
organised, while preserving role-based access (a section appears only if the role can use at
least one item in it).

## Scope

In scope:
- A collapsible **accordion** nav: clickable group headers (with a ▸/▾ chevron) that show/hide
  their child buttons in the existing scrollable sidebar.
- All groups **collapsed by default**; multiple may be open at once (independent toggles).
- **Role-aware**: build each group's children only for items the role can access; **omit** a
  group with no accessible items.
- Confined to `frmDashboard`'s nav-building code.

Out of scope / explicit changes:
- Inaccessible items become **hidden** rather than greyed-out: the current `EnableNavButton`
  disable behaviour for the grouped buttons (Add Student, Add Employee, Fees Payment, Exams) is
  removed in favour of hide-if-no-access. (Metric-card visibility flags, if any, are unaffected.)
- No flyout/popup menus; no persistence of expanded/collapsed state; no changes to the forms the
  buttons open, RBAC rules, or any data.

## Approach (chosen)

In-sidebar accordion via a `CreateNavGroup(header, children)` helper. Rejected: flyout submenus
(fiddly in WinForms, more state); single-open accordion (more surprising — multi-open chosen);
keeping the flat list (the problem being solved).

## Grouping

Top-level (no group): **Dashboard**, **Analytics**. Pinned footer (unchanged): **Exit**,
**Sign Out**.

Groups and their items (each item still gated by the same roles as today; a group renders only if
≥1 item is accessible):

| Group | Items | Item access (unchanged) |
|-------|-------|--------------------------|
| **Students** | Add Student, View Students | Add: Admin/Headmaster; View: all staff |
| **Staff** | Add Employee, View Employees | Add: Admin/Headmaster; View: Director/Admin/Headmaster/Accountant |
| **Leave** | Apply for Leave, Leave Requests | all staff / approvers |
| **Academics** | Exams, Exam Reports, Subjects | Exams: Admin/Teacher/Headmaster; Subjects: Director/Admin/Headmaster |
| **Finance** | Fees Payment, Payment History, Admission Approvals | per current rules (Accountant/Admin/Headmaster; Approvals: Accountant) |
| **Operations** | Library, Transport | Director/Admin/Headmaster |
| **Administration** | Settings, School Information, Grading Scheme, Database Backup, System Logs | Settings/SchoolInfo/Grading: Director/Admin/Headmaster; Backup/Logs: Admin/Headmaster |

(Item→role mapping mirrors the existing `_formAccess` / current gating; this design does not
change who can open anything — only how the buttons are arranged and that disallowed items are
omitted.)

## Components (all in `frmDashboard.cs`)

### `CreateNavGroup(string header, List<Button> children)` (new helper)
- Returns a `Panel` (added to the existing `nav` FlowLayoutPanel) containing:
  - a **header button** styled like a nav button, text = `"▸  " + header`, full sidebar width;
  - a **child container** `FlowLayoutPanel` (TopDown, AutoSize) holding `children`, with a small
    left indent; `Visible = false` initially.
- Header `Click` toggles `childContainer.Visible` and swaps the chevron (`▸`/`▾`).
- If `children` is empty, the caller does not create the group (the helper may also no-op/return
  null defensively).

### `BuildModernDashboard` nav section (rewrite)
Replace the flat `nav.Controls.Add(CreateNavButton(...))` sequence + the role-gated blocks with:
1. Add **Dashboard** (selected) directly.
2. For each group, assemble a `List<Button>` of only the accessible items (using the existing
   role checks / `AuthService.CanAccess`/`_formAccess` semantics already used), then
   `nav.Controls.Add(CreateNavGroup("Students", studentButtons))` etc., skipping empty lists.
3. Add **Analytics** directly (gated as today).
4. The bottom-docked **Exit/Sign Out/footer** assembly is unchanged.

### Cleanup
- Remove the now-unused `EnableNavButton` calls/method for the grouped buttons and the
  add-all-then-disable pattern (replaced by build-only-accessible). Keep `_approvalsNavBtn` only
  if still needed for the approvals notification badge; otherwise fold the Approvals button into
  the Finance group's list (badge logic re-pointed to that button).

## Data flow

Form load → `BuildModernDashboard` → builds Dashboard + groups (each with role-filtered children)
+ Analytics + footer. User clicks a group header → its child panel toggles visible; `navScroll`
scrolls if the expanded content exceeds the height.

## Error handling / edge cases

- A role with no items in a group → that group is not created (no empty headers).
- A role with access to a single standalone (e.g. only Dashboard + Analytics) → just those show.
- Accordion expansion beyond the visible area → handled by the existing `navScroll` AutoScroll
  (the prior overlap bug is already fixed).
- The currently-selected "Dashboard" highlight behaviour is preserved.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection render of `frmDashboard` for **Director**, **Accountant**, and **Teacher**:
  - headers present for groups with accessible items; collapsed (children hidden) initially;
  - clicking a header (invoke the header's click via reflection or just verify child panel exists
    and starts hidden) reveals children;
  - groups with no accessible items are absent (e.g. Teacher has no Administration group).
- Manual (user): sign in as each role; confirm sensible grouped sidebar; expand/collapse works;
  every button still opens its form; Exit/Sign Out still pinned.

## Success criteria

- The sidebar shows compact, collapsible groups; related actions are together.
- Each role sees only the groups/items it can use (empty groups hidden).
- Every existing destination still opens; no RBAC, data, or other-form changes.
