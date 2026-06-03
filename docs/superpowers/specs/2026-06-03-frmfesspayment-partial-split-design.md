# frmFessPayment Partial-Class Split — Design

**Date:** 2026-06-03
**Status:** Approved (design)
**Type:** Refactor (no behavior change). Increment 1 of taming the god-form.

## Goal

Reduce `frmFessPayment.cs` (~2,878 lines) from one giant file into ~5 focused `partial class frmFessPayment` files split by responsibility, and delete confirmed dead code. **No behavior change** — pure code organization so future edits are lower-risk and the file is small enough to reason about.

## Why partial classes (not a UserControl yet)

The immediate pain is file size. A partial-class split is a mechanical, reversible move with zero behavior change, verifiable by build + the offline render harness. True decoupling (extracting a `ReceiptView` control + `ReceiptData` DTO) is the better end-state but rewires field access and changes the behavior surface — that is **Increment 2**, specced separately later. This spec is only the safe split.

## Current structure (by method, from the single file)

- Fields (controls, `_rp*` receipt labels, wizard panels, state).
- Constructor + `BuildModernPaymentView`, `BuildNotePanel`, `CreateBulletPoint`, `InitializeFormControls`, `SetPlaceholder`, `BuildHeader`, `BuildWizardPanel`, `BuildWizardTitleRow`, `BuildHistoryFooter`.
- Progress indicator: `BuildProgressIndicator`, `ProgressPanel_Paint`, `RefreshProgressIndicator`, `ShowStep`.
- Step 1: `BuildStep1Panel`, `UpdateContinueButton`, `SetStudentInfoCardVisible`, `SetStudentNotFoundVisible`, `GoToStep2`.
- Step 2: `BuildStep2Panel`, `UpdatePreviewButton`, `GoToStep3`, `ConfirmOverpaymentIfNeeded`, `BuildQuickAmountButtons`.
- Step 3 / receipt: `BuildStep3Panel`, `BuildPreRecordActions`, `BuildPostRecordActions`, `BuildReceiptPreviewControl`, `CreateReceiptTile`, `RefreshReceiptPreview`, nested `SmoothScrollPanel`.
- History: `FilterHistory`.
- Actions/data: `RecordPayment`, `LookupStudent`, `ClearPaymentForm`, `NumberToWords`, receipt-number helpers, `UpdateReceiptAmountWords`, `UpdateStep2Balance`.
- **Dead:** `BuildPaymentPanel` (never called — verified: no call sites).

## Target file layout (all `partial class frmFessPayment`, same namespace)

| File | Responsibility | Methods moved |
|---|---|---|
| `frmFessPayment.cs` | Class shell, constructor, **all field declarations except `_rp*`**, top-level scaffolding | constructor, `BuildModernPaymentView`, `BuildNotePanel`, `CreateBulletPoint`, `InitializeFormControls`, `SetPlaceholder`, `BuildHeader`, `BuildWizardPanel`, `BuildWizardTitleRow`, `BuildHistoryFooter` |
| `frmFessPayment.Steps.cs` | Wizard flow + step panels | `BuildProgressIndicator`, `ProgressPanel_Paint`, `RefreshProgressIndicator`, `ShowStep`, `BuildStep1Panel`, `UpdateContinueButton`, `SetStudentInfoCardVisible`, `SetStudentNotFoundVisible`, `GoToStep2`, `BuildStep2Panel`, `UpdatePreviewButton`, `GoToStep3`, `ConfirmOverpaymentIfNeeded`, `BuildQuickAmountButtons`, `BuildStep3Panel`, `BuildPreRecordActions`, `BuildPostRecordActions` |
| `frmFessPayment.Receipt.cs` | Receipt preview rendering | the `_rp*` label field declarations + `_receiptPreviewShell`, `BuildReceiptPreviewControl`, `CreateReceiptTile`, `RefreshReceiptPreview`, nested `SmoothScrollPanel` class |
| `frmFessPayment.Actions.cs` | Data operations + history | `LookupStudent`, `RecordPayment`, `ClearPaymentForm`, `FilterHistory`, `UpdateReceiptAmountWords`, `UpdateStep2Balance`, `NumberToWords`, receipt-number helper(s) |

(If a method's home is ambiguous, prefer the file whose responsibility it most serves; the exact assignment is finalized in the plan. The 4-file split is the target; a method landing in a slightly different partial is acceptable as long as each method lives in exactly one file.)

## Constraints / rules

- **Pure move:** cut a method/field from the source and paste verbatim into its target partial. Do **not** rename, change signatures, or alter bodies. The only deletion is dead `BuildPaymentPanel`.
- Every method and field lives in **exactly one** file (no duplicates, no orphans).
- All new files start with the same `using`s the symbols need and `namespace kingdom_Preparatory_School_Management_System { public partial class frmFessPayment : Form { ... } }`.
- The class is **already `partial`** (`frmFessPayment.cs:16` declares `public partial class frmFessPayment : Form`, and a `frmFessPayment.Designer.cs` already exists). New partial files just repeat that declaration — **no keyword change** to the main file. Do not move anything out of `frmFessPayment.Designer.cs`.
- Add each new `.cs` to the `.csproj` `<Compile>` group. No new packages/references.
- Do not touch files outside `frmFessPayment.*` and the `.csproj`.

## Verification (every increment)

1. `dotnet build -clp:ErrorsOnly -nologo` → `0 Error(s)`.
2. Offline render harness (auth bypass → Accountant, off-screen `Show`, `DrawToBitmap`): render Step 2 and Step 3 (the receipt card) and confirm they are unchanged from before the split. Step 1 renders without error.
3. Commit each file-move separately so any regression is isolated to one small commit.

## Out of scope

- No `ReceiptView`/DTO extraction (that is Increment 2).
- No logic, layout, query, or behavior changes.
- No changes to other god-forms (`frmEmployee`, `frmDashboard`, `frmAddStd`) — separate increments.
