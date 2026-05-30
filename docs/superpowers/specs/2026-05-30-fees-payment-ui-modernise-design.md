# Fees Payment — UI/UX modernise + layout fixes

Date: 2026-05-30
Scope: `frmFessPayment.cs` only. No edits to `UiTheme.cs` or other forms.

## Problem
The Fees Payment wizard looks messy on a 1080px-tall screen:
1. Receipt preview is clipped with an internal scrollbar (steps 3 & 4) because the
   form is declared taller than the screen.
2. Step 1 has a large empty vertical gap between FEE TYPE and "Continue to Payment".
3. After Record Payment the on-screen receipt appears blank.
4. Controls look dated (flat square buttons / textboxes).

## Decisions
- Style: rounded **only where it counts** — primary action buttons + focus-glow textboxes.
- Reach: this form first (prove the look before app-wide rollout).

## Changes

### A. Fit window to screen (fixes receipt cut-off)
- `frmFessPayment.cs:142-143` — reduce `ClientSize` height ~1100→~1000 and
  `MinimumSize` height ~1000→~900 so the window fits a 1080px screen.
- `frmFessPayment.cs:1152-1158` — compress receipt row heights (~565px total) by
  ~80px so the full receipt renders with no internal scrollbar.

### B. Remove Step-1 empty gap
- `frmFessPayment.cs:608` — change FEE TYPE row from `Percent 100` to a fixed
  height; add an explicit filler row to absorb slack so field + button sit naturally.

### C. Keep recorded receipt intact
- `frmFessPayment.cs:2319` — stop clearing `amountBox` (and other inputs) at record
  time. The print snapshot is already captured in `lastPrintedReceipt`; defer the
  input reset to "+ New Payment" (`ClearPaymentForm`) so the visible receipt stays
  populated for review/print.

### D. Modernise — rounded where it counts
- Add a local custom-painted rounded primary button (GraphicsPath + Region,
  navy fill, gold hover, AntiAlias). Apply ONLY to: Look Up, Continue to Payment,
  Preview Receipt, Record Payment, Print Receipt.
- Secondary/chip buttons (Back, Clear, GHc presets, Dashboard, Refresh, New Payment)
  stay flat.
- Modern textboxes: host panel painting a rounded 1px border — gold on focus, grey
  otherwise — for the active inputs (Student ID, Payment Amount, Cheque/Ref, Bursar).
  Read-only display boxes keep current flat style.

## Guardrails
- All changes confined to `frmFessPayment.cs`.
- Bugfixes (A/B/C) are independent of the restyle (D).
- Verification: project builds; run the app and confirm receipt renders fully, no
  Step-1 gap, recorded receipt stays populated, primary buttons rounded.
