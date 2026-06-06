# Separate Payment History from Fees Payment — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The Fees Payment screen (`frmFessPayment`) hosts two unrelated things in one window: the
3-step payment **wizard** (Look Up → Payment → Receipt) and a full **payment-history**
browser (search box, grid, record count, refresh). The combined layout is cramped
("compacted") and the wizard has no room to breathe.

## Goal

Move the payment-history browser into its own dedicated screen (`frmPaymentHistory`),
remove it from `frmFessPayment`, and provide two ways to open it: a dashboard nav entry and
a button on the Fees Payment form. No data-layer changes.

## Scope

In scope:
- New standalone `frmPaymentHistory` form (grid + search + record count + refresh).
- Remove the embedded history section and its supporting fields/methods from `frmFessPayment`.
- RBAC entry + dashboard nav + a "View Payment History" button on the fee form.

Out of scope:
- Any change to how payments are recorded or balances computed.
- Any change to `FeeRepository` (the history query already exists).
- The Outstanding Fees screen (`frmOutstandingFees`) — unchanged.

## Approach (chosen)

**Standalone form reusing the existing repository + theme.** Lift the history UI into a new
form that calls `FeeRepository.GetPaymentHistoryTableAsync()` and styles the grid with the
shared `UiTheme` statics (the same ones `frmFessPayment` uses). Strip the history section out
of `frmFessPayment`.

Rejected: extracting a reusable `UserControl` — only one consumer, so it adds abstraction
for no benefit (YAGNI).

## Components

### `frmPaymentHistory.cs` (new)
- **UI:** a top bar with a search `TextBox` (placeholder "Search history (Name, ID, Class,
  Bursar)…") on the left and a record-count badge + Refresh button on the right; a
  full-dock `DataGridView` below.
- **Data:** `LoadAsync()` calls `FeeRepository.GetPaymentHistoryTableAsync()` and binds the
  returned `DataTable` to the grid, then applies the column layout.
- **Search:** `TextChanged` sets `DataTable.DefaultView.RowFilter` across Name/ID/Class/Bursar
  (the existing `FilterHistory` logic, single-quotes escaped); the count badge reflects
  `DefaultView.Count`.
- **Styling:** grid styled via `UiTheme.StyleDataGrid` + the navy header / surface / accent
  colors, and currency cell-formatting for the AMOUNT PAID and BALANCE columns — the exact
  styling currently in `frmFessPayment` (`ApplyPaymentHistoryGridLayout`, `SetHistoryColumn`,
  `PaymentGrid_CellFormatting`), moved verbatim into this form.
- **Access:** constructor calls `AuthService.RequireAccess("frmPaymentHistory", this)`; loads
  on the `Load` event. Opened non-modally with `Show()`.
- **Registration:** add a self-closing `<Compile Include="frmPaymentHistory.cs" />` to the
  csproj (explicit-include project).

### `frmFessPayment.cs` (cleanup)
Remove the history-owned pieces:
- Controls/fields: the "Payment History ↓" header panel, `historySearchBox`, `paymentGrid`,
  `_historyCountLbl`, and the two history "Refresh" buttons.
- Methods: `LoadPaymentHistory`, `FilterHistory`, `ApplyPaymentHistoryGridLayout`,
  `SetHistoryColumn`, `PaymentGrid_CellFormatting`, `btn_Re_Click` (and the `btn_Re` wiring),
  and any builder method that assembled the history panel.
- Calls: drop the post-record `await LoadPaymentHistory()` (the separate screen loads fresh
  when opened).
- Add: a **"View Payment History"** button in the post-record action area that opens
  `new frmPaymentHistory().Show()`.
- The wizard layout reclaims the vacated space.

### Wiring
- `Services/AuthService.cs`: add
  `["frmPaymentHistory"] = new[] { Accountant, Director, Administrator, Headmaster }` to
  `_formAccess` (mirrors `frmOutstandingFees` access).
- `frmDashboard.cs`: add a "Payment History" nav entry (same finance roles) that opens
  `frmPaymentHistory`.

## Data flow

1. User opens Payment History (dashboard nav or fee-form button) → `RequireAccess` →
   `LoadAsync()` → `GetPaymentHistoryTableAsync()` → bind + layout.
2. Typing in search → `RowFilter` updates the visible rows and the count badge.
3. Refresh → re-runs `LoadAsync()`.

## Error handling

- Load/refresh failures are caught and shown via `UIHelper.ShowError` with a clear message
  (same pattern as the current `LoadPaymentHistory`).
- Access denied → `RequireAccess` closes the form (existing behavior).

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Offline render (reflection harness) of **both** forms: `frmPaymentHistory` shows the grid +
  search + count + refresh; `frmFessPayment` shows the wizard with the history section gone
  and a "View Payment History" button present.
- Manual (user, running app): open Payment History from the dashboard and from the fee form;
  record a payment, open history, confirm the new row appears after Refresh; confirm search
  filters by name/ID/class/bursar; confirm non-finance roles can't open it.

## Success criteria

- Payment history lives in its own screen; `frmFessPayment` is the wizard only and is no
  longer cramped.
- The history screen opens from both the dashboard and the fee form, for Accountant /
  Director / Administrator / Headmaster.
- No change to payment recording, balances, or the repository.
