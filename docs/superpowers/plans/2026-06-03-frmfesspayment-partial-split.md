# frmFessPayment Partial-Class Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the ~2,878-line `frmFessPayment.cs` into four cohesive `partial class frmFessPayment` files and delete confirmed dead code, with **zero behavior change**.

**Architecture:** Pure mechanical cut/paste of whole methods (and the `_rp*` fields) between files of the **same partial class**. Because every file is the same class, any split that compiles is behavior-identical; the only real risks are (a) dropping/duplicating a method or (b) a missing `using` — both caught by `dotnet build`. A final offline render of Steps 2 & 3 confirms the UI is unchanged.

**Tech Stack:** C# / .NET Framework 4.7.2, WinForms. No new packages/references.

---

## Ground rules (apply to every task)

- **Verbatim moves only.** Cut a method from `frmFessPayment.cs` and paste it unchanged into the target file. Never rename, re-signature, or edit a method body. The sole deletion is the dead `BuildPaymentPanel`.
- Each method/field ends up in **exactly one** file.
- `frmFessPayment` is **already `partial`** (it has `frmFessPayment.Designer.cs`). New files repeat `public partial class frmFessPayment : Form`. **Do not touch `frmFessPayment.Designer.cs`.**
- Each new file uses this exact header (adjust `using`s only if the build reports a missing type):
```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class frmFessPayment : Form
    {
        // moved members go here
    }
}
```
- Add each new `.cs` to an existing `<Compile>` `<ItemGroup>` in `kingdom_Preparatory_School_Management_System.csproj`.
- Compile gate after each task: `dotnet build -clp:ErrorsOnly -nologo` must end `Build succeeded.` / `0 Error(s)`. If it reports a missing method, you dropped one — find it in git and move it. If it reports a missing type/name, add the needed `using` to the new file.
- Use ONLY the explicit `git add <named files>`. NEVER `git add -A`/`.`. Touch only `frmFessPayment*.cs` and the `.csproj`.
- `frmFessPayment.cs` is committed/clean at plan start. Do not modify any other source files.

---

## Task 1: Delete dead `BuildPaymentPanel`

**Files:** Modify `frmFessPayment.cs`

- [ ] **Step 1: Confirm it is dead**

Run: `grep -rn "BuildPaymentPanel" --include=*.cs . | grep -v ".worktrees"`
Expected: exactly ONE line — the definition (`private Control BuildPaymentPanel()`), no call sites.
If any call site exists, STOP and report (do not delete).

- [ ] **Step 2: Delete the whole method**

Remove the entire `private Control BuildPaymentPanel()` method body (from its signature through its closing `}` — currently ~lines 1672–1762). Delete nothing else.

- [ ] **Step 3: Compile gate**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 4: Commit**
```bash
git add frmFessPayment.cs
git commit -m "refactor(fees): remove dead BuildPaymentPanel"
```

---

## Task 2: Extract `frmFessPayment.Receipt.cs` (preview + printing)

**Files:** Create `frmFessPayment.Receipt.cs`; Modify `frmFessPayment.cs`, `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create the file** with the standard header (see Ground rules).

- [ ] **Step 2: Move these FIELD declarations** from `frmFessPayment.cs` into the new class body (cut from the field region near the top, paste verbatim):
  - `_rpStudentIdLbl, _rpClassLbl, _rpNameLbl, _rpAmountWordsLbl, _rpBeingLbl, _rpModeLbl, _rpAmountLbl, _rpBalanceLbl, _rpCreditLbl, _rpBursarLbl, _rpReceiptNumLbl, _rpDateLbl` (the `private Label _rp*;` lines)
  - `_receiptPreviewShell` (`private Panel _receiptPreviewShell;`)
  - `_rpLogoPictureBox` if present (`private PictureBox _rpLogoPictureBox;`)

- [ ] **Step 3: Move these METHODS** (cut from `frmFessPayment.cs`, paste verbatim into the new file), including the nested `SmoothScrollPanel` class and the `ReceiptPrintData` type if it is nested in this file:
  - Preview: `BuildReceiptPreviewControl`, `CreateReceiptTile`, `RefreshReceiptPreview`, and the nested `private sealed class SmoothScrollPanel : Panel { ... }`
  - Receipt building helpers: `GetSchoolLogoPath`, `BuildReceiptLogo`, `BuildSchoolHeader`, `CreateReceiptField`, `StyleReceiptInput`, `CreateReceiptNumber`
  - Printing: `PrintReceiptPreview`, `CleanReceiptNumber`, `DrawReceipt`, `DrawReceiptLogo`, `DrawLineField`, `DrawValueOnLine`, `DrawDottedLine`, `DrawCenteredText`

  Note: if a `ReceiptPrintData` / `ReceiptData` class/struct is declared inside `frmFessPayment.cs`, move it into this file too (it is receipt-printing data). If it is a separate top-level file already, leave it.

- [ ] **Step 4: Register in csproj** — add inside a `<Compile>` `<ItemGroup>`:
```xml
    <Compile Include="frmFessPayment.Receipt.cs" />
```

- [ ] **Step 5: Compile gate** → `0 Error(s)`. (A "does not exist" error naming one of the moved methods means it was left referenced but you also need its definition here — verify the cut landed in the new file exactly once.)

- [ ] **Step 6: Commit**
```bash
git add frmFessPayment.cs frmFessPayment.Receipt.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "refactor(fees): move receipt preview + printing to frmFessPayment.Receipt.cs"
```

---

## Task 3: Extract `frmFessPayment.Steps.cs` (wizard + step panels)

**Files:** Create `frmFessPayment.Steps.cs`; Modify `frmFessPayment.cs`, csproj

- [ ] **Step 1: Create the file** with the standard header.

- [ ] **Step 2: Move these METHODS** verbatim:
  - `BuildProgressIndicator`, `ProgressPanel_Paint`, `RefreshProgressIndicator`, `ShowStep`
  - `BuildStep1Panel`, `UpdateContinueButton`, `SetStudentInfoCardVisible`, `SetStudentNotFoundVisible`, `GoToStep2`
  - `BuildStep2Panel`, `UpdatePreviewButton`, `GoToStep3`, `ConfirmOverpaymentIfNeeded`, `BuildQuickAmountButtons`
  - `BuildStep3Panel`, `BuildPreRecordActions`, `BuildPostRecordActions`
  - `UpdateReceiptAmountWords`, `UpdateStep2Balance`, `NumberToWords` (live step-2/amount-in-words helpers)

- [ ] **Step 3: Register in csproj**:
```xml
    <Compile Include="frmFessPayment.Steps.cs" />
```

- [ ] **Step 4: Compile gate** → `0 Error(s)`.

- [ ] **Step 5: Commit**
```bash
git add frmFessPayment.cs frmFessPayment.Steps.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "refactor(fees): move wizard/step panels to frmFessPayment.Steps.cs"
```

---

## Task 4: Extract `frmFessPayment.Actions.cs` (data ops + history)

**Files:** Create `frmFessPayment.Actions.cs`; Modify `frmFessPayment.cs`, csproj

- [ ] **Step 1: Create the file** with the standard header.

- [ ] **Step 2: Move these METHODS** verbatim:
  - Data: `LookupStudent`, `RecordPayment`, `ClearPaymentForm`, `frmFessPayment_Load`
  - History: `FilterHistory`, `BuildHistoryPanel`, `ApplyPaymentHistoryGridLayout`, `SetHistoryColumn`, `PaymentGrid_CellFormatting`, and `LoadPaymentHistory` (the async history loader, if defined in `frmFessPayment.cs`)

  Leave the small legacy event-handler stubs (`txtStdID_TextChanged`, `pay_Click`, `btn_Re_Click`, `gunaPictureBox*_Click`, the `*ToolStripMenuItem*_Click` one-liners, etc.) in `frmFessPayment.cs` — they are tied to the designer and trivial.

- [ ] **Step 3: Register in csproj**:
```xml
    <Compile Include="frmFessPayment.Actions.cs" />
```

- [ ] **Step 4: Compile gate** → `0 Error(s)`.

- [ ] **Step 5: Commit**
```bash
git add frmFessPayment.cs frmFessPayment.Actions.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "refactor(fees): move data ops + history to frmFessPayment.Actions.cs"
```

---

## Task 5: Final verification (build + render Steps 2 & 3)

**Files:** none (verification only)

- [ ] **Step 1: Full build**

Run: `dotnet build -clp:ErrorsOnly -nologo 2>&1 | grep -iE "Build (succeeded|FAILED)|Error\(s\)"`
Expected: `Build succeeded.` / `0 Error(s)`

- [ ] **Step 2: Confirm the file actually shrank**

Run: `wc -l frmFessPayment.cs frmFessPayment.Steps.cs frmFessPayment.Receipt.cs frmFessPayment.Actions.cs`
Expected: `frmFessPayment.cs` well under ~1,000 lines; the other three each a few hundred; total ≈ original minus the deleted dead method.

- [ ] **Step 3: Offline render Step 3 (receipt) and Step 2**

Create `verify_split.ps1` in the project root:
```powershell
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms; Add-Type -AssemblyName System.Drawing
$root="C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy"
$bin=Join-Path $root "bin\Debug"
[AppDomain]::CurrentDomain.add_AssemblyResolve([ResolveEventHandler]{param($s,$e)
  $n=($e.Name -split ',')[0]; if($n -like '*.resources'){return $null}
  $p=Join-Path $bin ($n+'.dll'); if(Test-Path $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $bin 'kingdom_Preparatory_School_Management_System.exe'))
$NS='kingdom_Preparatory_School_Management_System'
$authT=$asm.GetType("$NS.Services.AuthService"); $sessT=$asm.GetType("$NS.Services.AuthService+UserSession"); $roleT=$asm.GetType("$NS.Services.AuthService+UserRole")
$sess=[Activator]::CreateInstance($sessT); $sessT.GetProperty("Role").SetValue($sess,[Enum]::Parse($roleT,"Accountant"))
$authT.GetField("<CurrentUser>k__BackingField",[Reflection.BindingFlags]'Static,NonPublic').SetValue($null,$sess)
$BF=[Reflection.BindingFlags]'Instance,NonPublic,Public'
$f=[Activator]::CreateInstance($asm.GetType("$NS.frmFessPayment"))
$f.StartPosition='Manual'; $f.Location=New-Object System.Drawing.Point(-3200,-3200); $f.ShowInTaskbar=$false
$f.Size=New-Object System.Drawing.Size(1180,920); $f.Show()
[System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 300; [System.Windows.Forms.Application]::DoEvents()
function Box($n){ $f.GetType().GetField($n,$BF).GetValue($f) }
(Box 'studentIdBox').Text="9005"; (Box 'studentNameBox').Text="KWAME OSEI"; (Box 'classBox').Text="BASIC 3"
(Box 'balanceBox').Text="1923.00"; (Box 'amountBox').Text="500"; (Box 'beingBox').Text="School Fees"; (Box 'bursarBox').Text="BURSAR JANE"
$f.GetType().GetMethod('ShowStep',$BF).Invoke($f,@([int]3)); $f.GetType().GetMethod('RefreshReceiptPreview',$BF).Invoke($f,@())
[System.Windows.Forms.Application]::DoEvents(); Start-Sleep -Milliseconds 200; [System.Windows.Forms.Application]::DoEvents()
$shell=$f.GetType().GetField('_receiptPreviewShell',$BF).GetValue($f); $card=$shell.Controls[0]
$bmp=New-Object System.Drawing.Bitmap($card.Width,$card.Height)
$card.DrawToBitmap($bmp,(New-Object System.Drawing.Rectangle(0,0,$card.Width,$card.Height)))
$bmp.Save((Join-Path $root "split_receipt.png"),[System.Drawing.Imaging.ImageFormat]::Png); "SAVED receipt"
$f.Dispose()
```
Run: `powershell.exe -STA -ExecutionPolicy Bypass -File verify_split.ps1`
Expected: prints `SAVED receipt`. Open `split_receipt.png` and confirm the receipt looks exactly as before the split (school header, STUDENT ID/CLASS/RECEIVED FROM, THE SUM OF, BEING=School Fees, PAYMENT MODE=Cash, amount, balance, BURSAR JANE — nothing missing or clipped).

- [ ] **Step 4: Clean up the temp script**

Run: `rm -f verify_split.ps1 split_receipt.png`

- [ ] **Step 5: Done** — no commit needed (verification only). Report the new line counts and that the render matched.

---

## Self-Review

- **Spec coverage:** 4-file target met — `frmFessPayment.cs` (scaffolding + factory helpers + legacy stubs, retained), `.Steps.cs` (Task 3), `.Receipt.cs` (Task 2, incl. printing per spec "printing folds into Receipt"), `.Actions.cs` (Task 4, incl. history). Dead `BuildPaymentPanel` deleted (Task 1). Verification = build + render (Task 5). ✓
- **Placeholder scan:** No TBD/TODO. Moves are by explicit method name (verbatim cut/paste is the correct instruction for a refactor — reproducing 380-line bodies would invite accidental edits). ✓
- **Type consistency:** Method names match the current inventory of `frmFessPayment.cs`. `SmoothScrollPanel` and `ReceiptPrintData` nested types called out explicitly so they aren't orphaned. The factory helpers (`CreateField`, `CreateTextBox`, `Create*Button`, `CreateModernField`) and `InitializeFormControls`/`BuildModernPaymentView`/`BuildHeader`/etc. are intentionally **not** listed in any move task → they remain in `frmFessPayment.cs`. ✓
