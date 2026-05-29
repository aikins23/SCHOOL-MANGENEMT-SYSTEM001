# Fees Payment UI Redesign — Design Spec
**Date:** 2026-05-29  
**Status:** Approved  
**Form:** `frmFessPayment.cs`

---

## Goal

Modernise the Fees Payment form from a dense single-panel paper-receipt layout into a clear 3-step wizard, while preserving (and improving) the printed receipt output. The cashier experience should be fast, error-resistant, and self-explanatory.

---

## Overall Layout

The form retains its existing 3-section vertical structure:

1. **Page header** — title ("Fees Payment") + subtitle + action buttons (Dashboard, Refresh)
2. **Wizard panel** — fixed-height panel containing the 3-step wizard
3. **Payment history grid** — fills remaining vertical space below the wizard

The wizard panel replaces the current monolithic receipt form. The history grid is unchanged in structure but always visible below, collapsed to a header row when not in focus.

---

## Progress Indicator

A horizontal dot-and-line indicator sits at the top of the wizard panel:

```
●————○————○
LOOK UP   PAYMENT   RECEIPT
```

- **Pending step:** grey circle, grey label
- **Active step:** navy (#1a2952) filled circle, navy bold label
- **Completed step:** green (#4caf50) filled circle with ✓, green label
- The connecting line between two steps fills navy once the earlier step is completed

---

## Step 1 — Student Lookup

**Purpose:** Identify the student and select the fee type before entering any amounts.

**Fields:**
| Field | Type | Behaviour |
|---|---|---|
| Student ID | TextBox | On change, auto-lookup student (same as current) |
| Look Up | Button | Secondary trigger for lookup |
| Fee Type | Editable ComboBox (`DropDown` style) | Preset options + free-type |

**Preset fee type options:**
- School Fees
- Examination Fees
- PTA Levy
- Uniform / Clothing
- Sports / Activity Fees
- Registration Fees

The ComboBox uses `DropDownStyle = DropDown` (editable) so the cashier can type any custom value not in the list.

**After successful lookup:**  
A student info card appears below the ID field showing:
- Full name (bold, navy)
- Class and ID
- Outstanding balance (bold, red `#c0392b`)

**Validation:**  
"Continue to Payment" button is disabled until both a valid student is found AND a fee type is entered/selected.

**Navigation:** "Continue to Payment →" advances to Step 2.

---

## Step 2 — Payment Details

**Purpose:** Enter the payment amount and supporting details.

**Student summary bar** (read-only, always visible at top of step):  
`KWESI BOAKYE · BASIC 7` — `Balance: GHc 1,500.00`

**Fields:**
| Field | Type | Behaviour |
|---|---|---|
| Amount (GHc) | TextBox (numeric) | Right-aligned, large font; triggers auto-update of Amount in Words |
| Payment Mode | ComboBox | Cash, Mobile Money, Bank Transfer, Cheque |
| Cheque / Ref No. | TextBox | Optional; visible for all modes, required only for Cheque |
| Amount in Words | TextBox (read-only) | Auto-generated from Amount |
| Being (Reason) | TextBox | Pre-filled from fee type selected in Step 1; editable |
| Bursar / Cashier | TextBox | Manual entry |
| Date | DateTimePicker | Defaults to today |

**Navigation:**  
- "← Back" returns to Step 1 (student info and fee type preserved)  
- "Preview Receipt →" advances to Step 3

---

## Step 3 — Receipt Preview & Confirm

**Purpose:** Show a rendered receipt for the cashier to verify before committing the payment.

### Receipt layout (top to bottom):

1. **School header** — logo (left, 52×52 px) + school name / address / phone (centred), separated by a 2px navy bottom border
2. **Receipt title + number + date row** — "Official Receipt" box (navy border) left; receipt number + date right
3. **Student row** — Student ID · Class · Received From (3 columns, light blue background tiles)
4. **The sum of** — amount in words, italic, navy left-border accent strip
5. **Being + Payment Mode** — 2-column row
6. **Amount box + Balance After** — side by side:
   - Left (2/3): `GHc [amount] .00` in large bold navy, 2px navy border
   - Right (1/3): `BALANCE AFTER` label + projected amount in green (#2e7d32), green background tile. In the preview state (before recording) this is a live calculation: `current balance − entered amount`. After recording it reflects the actual saved balance returned from the database.
7. **Bursar / Cashier + Signature line** — 2-column row

### Actions (before recording):
- `← Back` — returns to Step 2, all values preserved
- `Record Payment` (primary, navy, full-width flex) — commits the payment
- `Clear` — resets entire wizard to Step 1

### Success state (after recording):
- Green success banner appears above the receipt:  
  `✓ Payment Recorded Successfully — GHc 250.00 · KWESI BOAKYE · New balance: GHc 1,250.00`
- Receipt panel gets a green border; fields become read-only
- Action buttons swap to:
  - `🖨 Print Receipt` (primary, navy)
  - `+ New Payment` (green outline)
- History grid header shows "↑ just updated" indicator
- Clicking `+ New Payment` resets the entire wizard to Step 1

---

## Print Behaviour

The printed receipt is identical to the Step 3 receipt layout. No changes to existing `PrintDocument` / `PrintPage` logic — the `ReceiptPrintData` struct is populated from the same field values used to render Step 3.

---

## Error Handling

| Scenario | Behaviour |
|---|---|
| Student ID not found | Red inline message below ID field: "No student found with this ID" |
| Amount is zero or blank | "Preview Receipt →" disabled; inline hint below Amount field |
| Database error on Record | Status label shows error message in red; wizard stays on Step 3 |
| Logo file missing | Header renders without image (graceful fallback, no crash) |

---

## Fee Type → Being Auto-fill

When the cashier selects or types a fee type in Step 1, it is carried forward as the initial value of the "Being" field in Step 2. The cashier can still edit "Being" directly. If they go back to Step 1 and change the fee type, "Being" is only updated if it still matches the previous fee type value (i.e., if the cashier already edited it manually, their edit is preserved).

---

## Colour & Typography Reference

| Token | Value | Usage |
|---|---|---|
| Navy | `#1a2952` | Primary buttons, headers, borders |
| Green | `#4caf50` / `#2e7d32` | Success states, balance |
| Red | `#c0392b` | Outstanding balance, errors |
| Surface | `UiTheme.Surface` | Card backgrounds |
| Page | `UiTheme.Page` | Form background |
| Muted | `UiTheme.Muted` | Labels, secondary text |

Fonts follow existing `UiTheme` conventions (Segoe UI 10.5F body, Segoe UI Semibold for amounts, Arial Narrow for GHc label).

---

## Out of Scope

- Changes to the payment history grid columns or data source
- Changes to the `PrintDocument` rendering logic beyond populating `ReceiptPrintData`
- Adding search/filter to the history grid
- Auto-filling the bursar name from the logged-in user (future enhancement)
