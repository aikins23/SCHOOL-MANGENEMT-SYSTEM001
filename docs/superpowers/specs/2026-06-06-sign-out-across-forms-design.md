# Consistent Sign Out Across Forms — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

Sign-out today exists only on the two dashboards (`frmDashboard` nav button; `frmTeacherDashboard`
has its own). The working mechanism — `Common.FormManager.SignOut()` (confirm → `AuthService.Logout()`
→ fresh `frmlogin` → close every other form) — is solid, but there is **no shared base form**, so
every other window (~25 forms derive directly from `Form`) has no sign-out affordance. Worse, some
forms open with the dashboard **hidden** (e.g. Add Student via `OpenForm(new frmAddStd(), true)`),
leaving the user with no way to sign out until they close the form. For a product being sold, "I can
sign out from any window" is expected polish.

## Goal

Add a consistent, discoverable **Sign Out** control to every top-level working form, reusing the
existing `FormManager.SignOut()` flow — without touching that flow, RBAC, or any form's logic.

## Scope

In scope:
- A new reusable helper `Common/SessionUi.cs` exposing `AttachSignOut(Form form)` that drops one
  self-contained "Sign Out" chip into a form's client top-right.
- One call — `Common.SessionUi.AttachSignOut(this);` — added to each target form's constructor.

Target forms (19): `frmAddStd`, `frmStdView`, `frmStdDetails`, `frmEmployee`, `frmEmpView`,
`frmEmpDetails`, `EXAMS`, `EXAMSVIEW`, `frmFessPayment`, `frmPaymentHistory`, `frmPendingApprovals`,
`frmEmpLeave`, `frmLeaveDetails`, `frmSubjects`, `frmLibrary`, `frmTransport`, `frmSchoolInfo`,
`frmGradingScheme`, `frmEmailSettings`.

Out of scope / excluded:
- `frmDashboard` (already has Sign Out in nav) and `frmTeacherDashboard` (already has its own button).
- `frmlogin`, `load` (splash) — pre-auth / borderless.
- Transient modal pop-ups created inline (Add/Edit Book, Add/Edit Bus/Route, Change Bus Route,
  report-card preview, login success dialog) — short-lived child dialogs, not landing windows.
- No change to `FormManager.SignOut()`, `AuthService`, RBAC, or any form's existing behaviour.

## Approach (chosen)

Reusable helper + one call per form. Rejected: (a) a shared base form — too invasive to retrofit
onto ~25 heterogeneous designer/code forms; (b) centralizing in `frmDashboard.OpenForm` — misses
drill-down forms opened elsewhere (e.g. `frmStdDetails` from `frmStdView`) and the `.ShowDialog()`
settings forms; (c) a global hotkey — not discoverable.

**Why a self-contained chip:** every target form uses a standard OS title bar, so the OS window
controls (incl. the real ✕) sit in the non-client area and the **client-area top-right is free**.
The chip paints its own background (it does not rely on WinForms transparency, which would show the
form's back colour rather than an overlapping header panel), so it looks intentional on a plain form
or on a coloured header band alike.

## Components

### `Common/SessionUi.cs` (new)

```
public static class SessionUi
{
    public static void AttachSignOut(Form form);
}
```

`AttachSignOut(form)`:
- No-ops if `form` is null, if no user is authenticated (`AuthService.CurrentUser.IsAuthenticated`
  is false — so it never appears pre-login), or if the form already contains the chip (idempotent;
  the chip is named `"_sharedSignOut"`).
- Builds a flat `Button`: text `"⎋  Sign Out"`, `Size ≈ 104×30`, `FlatStyle.Flat`,
  `BackColor = White`, `ForeColor = AccentRed (190,18,60)`, 1px light-red border,
  hover fill `(255,235,238)`, `Cursor = Hand`, `TabStop = false`,
  `Anchor = Top | Right`, located at `(ClientSize.Width − Width − 8, 8)`.
- `Click → FormManager.SignOut()`.
- Adds it to `form.Controls` and `BringToFront()` so it sits above any docked header panel.
- Whole body wrapped in try/catch → `LoggerHelper.LogWarning` (UI sugar must never break a form).

### Per-form wiring
The single line `Common.SessionUi.AttachSignOut(this);` is added as the **last statement of each
target form's constructor** (after `InitializeComponent()` for designer forms; at the end of the
UI-building constructor for code-built forms). `Anchor = Top|Right` keeps it pinned through later
resizes/maximize.

### Project file
Add `<Compile Include="Common\SessionUi.cs" />` to the csproj (explicit-include project).

## Data flow

Form constructed → `AttachSignOut(this)` adds the chip (only when authenticated) → user clicks it →
`FormManager.SignOut()` confirms, logs out, shows a fresh login, and closes all other forms.

## Error handling / edge cases

- **Not authenticated** (shouldn't happen for these forms, but defensive): chip not added.
- **Idempotent**: re-attaching (e.g. form reused) won't duplicate the chip.
- **Header overlap**: chip has its own background, so it reads cleanly over coloured header bands;
  OS title bar holds the real ✕, so no overlap with window controls.
- **In-client top-right control** on a specific form (rare): caught at render verification; the chip
  can be nudged (e.g. shifted down/left) per-form if it overlaps real content.
- **Sign out from a modal settings dialog**: `FormManager.SignOut()` already closes every open form
  (snapshotting `Application.OpenForms`); the modal closes and login shows — no special handling.
- Any exception while attaching is logged and swallowed; the form still works.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Offline render harness on representative forms — a code-built one (`frmLibrary`), a legacy designer
  one (`frmAddStd`), `frmFessPayment`, and `frmPaymentHistory` — confirming a "Sign Out" control is
  present, anchored top-right, and not overlapping existing top-right content. (Forms whose
  constructor needs the DB may not render offline; for those, rely on the deterministic placement +
  build.)
- Manual (user): sign in, open several forms (incl. Add Student with the dashboard hidden), click
  Sign Out → confirmation → returns to login with all windows closed.

## Success criteria

- Every target form shows a consistent top-right Sign Out chip when signed in.
- Clicking it runs the existing confirm→logout→login flow from anywhere.
- No regression to dashboards, RBAC, the sign-out mechanism, or any form's logic; no chip pre-login.
