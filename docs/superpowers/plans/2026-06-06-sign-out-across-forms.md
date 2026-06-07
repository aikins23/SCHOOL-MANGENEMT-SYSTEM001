# Consistent Sign Out Across Forms Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a consistent top-right "Sign Out" chip to every top-level working form, reusing the existing `FormManager.SignOut()` flow.

**Architecture:** A new static helper `Common/SessionUi.AttachSignOut(Form)` builds one self-contained Sign Out button (anchored top-right, own background, idempotent, auth-gated) and wires its click to `FormManager.SignOut()`. Each target form calls it once at the end of its constructor.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms. New file `Common/SessionUi.cs` + one-line edits to 19 form constructors + 1 csproj `<Compile>` entry.

**Spec:** `docs/superpowers/specs/2026-06-06-sign-out-across-forms-design.md`

---

## Conventions

- **No unit-test framework.** Gates: `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; offline render of representative forms.
- Explicit-include csproj: every new `.cs` needs a `<Compile Include="..." />` entry.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- Existing, reused as-is: `Common.FormManager.SignOut()`, `Services.AuthService.CurrentUser.IsAuthenticated`, `Services.LoggerHelper.LogWarning(string)`.

---

### Task 1: `SessionUi` helper

**Files:** Create `Common/SessionUi.cs`; modify `kingdom_Preparatory_School_Management_System.csproj`.

- [ ] **Step 1: Create the helper**

Create `Common/SessionUi.cs` with exactly:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Drops a consistent, self-contained "Sign Out" chip into the top-right of any top-level
    /// form, wired to the existing FormManager.SignOut() flow. Auth-gated and idempotent.
    /// </summary>
    public static class SessionUi
    {
        private const string SignOutName = "_sharedSignOut";

        public static void AttachSignOut(Form form)
        {
            if (form == null) return;
            try
            {
                // Only for a signed-in session — never on login/splash.
                if (!AuthService.CurrentUser.IsAuthenticated) return;
                // Idempotent: don't add a second chip.
                if (form.Controls.Find(SignOutName, true).Length > 0) return;

                var btn = new Button
                {
                    Name      = SignOutName,
                    Text      = "⎋  Sign Out",
                    Size      = new Size(104, 30),
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(190, 18, 60), // AccentRed
                    Cursor    = Cursors.Hand,
                    TabStop   = false,
                    Anchor    = AnchorStyles.Top | AnchorStyles.Right
                };
                btn.FlatAppearance.BorderColor        = Color.FromArgb(244, 199, 207);
                btn.FlatAppearance.BorderSize         = 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 235, 238);

                const int margin = 8;
                btn.Location = new Point(Math.Max(margin, form.ClientSize.Width - btn.Width - margin), margin);
                btn.Click += (s, e) => FormManager.SignOut();

                form.Controls.Add(btn);
                btn.BringToFront();
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning($"SessionUi.AttachSignOut({form.Name}): {ex.Message}");
            }
        }
    }
}
```

- [ ] **Step 2: Register in the csproj**

In `kingdom_Preparatory_School_Management_System.csproj`, next to the other `Common\*.cs`
`<Compile Include="...">` entries (e.g. `Common\FormManager.cs`), add:

```xml
    <Compile Include="Common\SessionUi.cs" />
```

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If `AuthService`/`FormManager`/`LoggerHelper` are unresolved, check the
`using` and that the csproj entry was added.)

- [ ] **Step 4: Commit**

```bash
git add Common/SessionUi.cs kingdom_Preparatory_School_Management_System.csproj docs/superpowers/plans/2026-06-06-sign-out-across-forms.md docs/superpowers/specs/2026-06-06-sign-out-across-forms-design.md
git commit -m "feat(signout): add SessionUi.AttachSignOut helper (shared Sign Out chip)"
```

---

### Task 2: Wire the chip into every target form

**Files (modify):** the 19 form `.cs` files listed below.

For **each** form, add this as the **last statement of the primary constructor** — after
`InitializeComponent();` on designer forms, or at the very end of the UI-building constructor on
code-built forms:

```csharp
            Common.SessionUi.AttachSignOut(this);
```

- [ ] **Step 1: Designer forms** (add after `InitializeComponent();`)
  - `frmAddStd.cs`
  - `frmStdView.cs`
  - `frmStdDetails.cs`
  - `frmEmployee.cs`
  - `frmEmpView.cs`
  - `frmEmpDetails.cs`
  - `EXAMS.cs`
  - `EXAMSVIEW.cs`
  - `frmFessPayment.cs`
  - `frmEmpLeave.cs`
  - `frmLeaveDetails.cs`

- [ ] **Step 2: Code-built forms** (add as the last line of the constructor that builds the UI)
  - `frmPaymentHistory.cs`
  - `frmPendingApprovals.cs`
  - `frmSubjects.cs`
  - `frmLibrary.cs`
  - `frmTransport.cs`
  - `frmSchoolInfo.cs`
  - `frmGradingScheme.cs`
  - `frmEmailSettings.cs`

(If a form has multiple constructors, add the call to the one(s) actually used to open it — the
public parameterless/primary constructor; the helper is idempotent so a duplicate call is harmless.)

- [ ] **Step 3: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 4: Render-verify representative forms**

Using the offline render harness, render and inspect:
- `frmLibrary` (code-built), `frmAddStd` (designer), `frmFessPayment`, `frmPaymentHistory`.
Confirm: a control named `_sharedSignOut` exists, sits at top-right (X ≈ ClientWidth−112, Y ≈ 8),
and does not overlap an existing top-right control. (Forms whose constructor requires the DB may
not render offline — for those, rely on build + the deterministic placement.)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(signout): show Sign Out on all top-level forms"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] Render shows the chip top-right on the representative forms with no overlap.
- [ ] **User smoke test:** sign in; open Add Student (dashboard hidden), a settings dialog, and a
  drill-down (a student's details) — each shows a top-right "Sign Out"; clicking it asks to confirm,
  then returns to the login screen with all windows closed. No chip appears on the login screen.

## Self-review notes

- **Spec coverage:** helper `AttachSignOut` (Task 1) ✓; auth-gate + idempotent + own-background chip
  + top-right anchor (Task 1 Step 1) ✓; one call per target form, 19 forms (Task 2) ✓; csproj entry
  (Task 1 Step 2) ✓; excludes dashboards/login/splash/transient modals (not in the list) ✓; no change
  to `FormManager.SignOut`/RBAC ✓.
- **Placeholder scan:** the inserted line is identical and shown verbatim; helper is complete. The
  per-form location ("after `InitializeComponent();`" / "end of UI constructor") is a precise anchor,
  not a placeholder.
- **Type consistency:** `SessionUi.AttachSignOut(Form)`, `FormManager.SignOut()`,
  `AuthService.CurrentUser.IsAuthenticated`, `LoggerHelper.LogWarning(string)` — all match existing
  signatures.
- **Risk:** a form with real content already at client top-right could overlap — caught at render
  (Task 2 Step 4); mitigation is a per-form nudge of the chip's location.
