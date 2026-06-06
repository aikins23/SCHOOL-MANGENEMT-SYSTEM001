# Configurable Grading Scheme — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

Grade bands are hardcoded in the report-card code, in **two** places:
- `Services/ReportCardPDFGenerator.cs` — a `GradeLevels[]` legend (80+ Advance(A)=1, 75–79
  Proficiency(P)=2, 70–74 Approaching Proficiency(AP)=3, 65–69 Developing=4, ≤64 Beginning=5)
  plus `GetGradeForScore`/`GetRemarkForScore` with `>=80/75/70/65` thresholds.
- `Services/ReportCardPdfService.cs` — a duplicate copy of the same legend.

A school with a different academic policy (e.g. A=90+, or A–F letters) cannot change this
without editing and rebuilding code. For a sellable system, the grading scheme must be
configurable in-app.

## Goal

Store the grading scheme (an ordered set of grade bands) in the database, editable by
Director/Administrator through a settings screen, and have all grade computation and the
report-card legend read from it — with no behavior change until an admin edits it.

## Scope

In scope:
- A `GradingScheme` table of grade bands + seed from the current 5-band scheme.
- A `GradeBand` model and a `Common/GradingScheme` cached accessor.
- A `GradingSchemeRepository`.
- Retire the hardcodes in both report-card generators (grade code, remark, and legend).
- A `frmGradingScheme` settings form (Director/Administrator) + dashboard nav entry + RBAC.

Out of scope (noted, not built here):
- The dashboard "grade distribution" chart buckets (`GetGradeDistributionAsync`) — left as-is.
- Per-class-level schemes (one school-wide scheme only).
- Custom subjects per class (separate feature).

## Approach (chosen)

**Settings table + cached accessor**, identical in shape to the School Information feature
(`SchoolInfoRepository` + `SchoolProfile`). One school-wide scheme; fully custom bands
(add/edit/remove rows). Rejected: editing only fixed thresholds (less sellable);
per-class-level schemes (YAGNI now).

## Data model

```sql
IF OBJECT_ID(N'GradingScheme', N'U') IS NULL
CREATE TABLE GradingScheme (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    MinScore  INT          NOT NULL,   -- inclusive lower bound, 0..100
    Code      NVARCHAR(10),            -- printed in the subject "Grade" column, e.g. "1" or "A"
    Label     NVARCHAR(80)             -- printed as the remark + legend text, e.g. "Advance(A)"
);
```

**Seed (only when empty)** from the current scheme:
`(80,"1","Advance(A)")`, `(75,"2","Proficiency(P)")`, `(70,"3","Approaching Proficiency(AP)")`,
`(65,"4","Developing")`, `(0,"5","Beginning")`.

**Band selection:** order bands by `MinScore` descending; a score maps to the first band whose
`MinScore <= score`. The `MinScore = 0` band is the catch-all floor.

## Components

### `Models/GradeBand.cs` (new)
```
public class GradeBand { public int MinScore; public string Code; public string Label; }
```
(Auto-properties; defaults empty strings.)

### `Data/IGradingSchemeRepository.cs` + `Data/GradingSchemeRepository.cs` (new)
- `Task EnsureTableAsync()` — create + seed when empty.
- `Task<List<GradeBand>> GetBandsAsync()` — ordered by `MinScore` DESC.
- `Task SaveBandsAsync(IEnumerable<GradeBand> bands)` — replace-all (DELETE then INSERT inside
  the same connection), so edits/removes/reorders persist cleanly.
- OleDb, `AppConfig.ConnectionString`, mirrors `SchoolInfoRepository`.

### `Common/GradingScheme.cs` (new — static cached accessor)
- Lazy load once (synchronous `.GetAwaiter().GetResult()`), cache, `Refresh()` clears it.
- `IReadOnlyList<GradeBand> Bands` — ordered high→low.
- `string CodeForScore(decimal score)` → the matching band's Code (""/safe if none).
- `string LabelForScore(decimal score)` → the matching band's Label.
- **Fail-safe:** on any DB error / empty table, return the legacy 5 bands so report cards never
  break.

### Integration (retire the hardcodes)
- `Services/ReportCardPDFGenerator.cs`:
  - `GetGradeForScore(score)` → `return Common.GradingScheme.CodeForScore(score);`
  - `GetRemarkForScore(score)` → `return Common.GradingScheme.LabelForScore(score);`
  - The `GradeLevels[]` legend is built from `Common.GradingScheme.Bands`. The displayed range
    text is derived from consecutive `MinScore`s: the top band shows `"{MinScore}+"`, each middle
    band shows `"{MinScore}-{nextHigherMinScore-1}"`, the floor band shows `"0-{nextHigherMinScore-1}"`.
- `Services/ReportCardPdfService.cs`: its duplicate legend is built from
  `Common.GradingScheme.Bands` the same way (single source of truth).

### `frmGradingScheme.cs` (new — settings form)
- Constructor: `BuildUi(); if (!AuthService.RequireAccess("frmGradingScheme", this)) return; Load += LoadAsync;`
  (mirrors `frmSchoolInfo`).
- A `DataGridView` with editable columns **Min %**, **Code**, **Label**; Add Row / Remove Row
  buttons; Save / Cancel; status label.
- **Validation on Save:** at least one band; every Min % an integer 0–100; no duplicate Min %;
  exactly one band with Min % = 0 (the floor). On pass: `SaveBandsAsync` then
  `GradingScheme.Refresh()`.
- Themed with `AppConfig.Colors`/`UiTheme`; verified offline via the render harness.

### RBAC + navigation
- `Services/AuthService.cs`: `["frmGradingScheme"] = new[] { Director, Administrator }`.
- `frmDashboard.cs`: a "Grading Scheme" nav entry in the Director/Administrator settings block
  (next to "School Information").

## Data flow

1. App / report card needs a grade → `GradingScheme` (loads once from DB, caches; fail-safe to
   legacy bands).
2. Director/Admin opens `frmGradingScheme` → edits bands → Save → DB replace-all →
   `GradingScheme.Refresh()` → subsequent report cards use the new bands.

## Error handling

- DB/table errors in `GradingScheme` → legacy 5 bands; the app never breaks.
- Save errors → caught, shown in the form status label, logged via `LoggerHelper`.
- Invalid grid input → blocked with a clear validation message before any DB write.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection probe: seed + `CodeForScore`/`LabelForScore` (e.g. 82→"1"/"Advance(A)", 60→"5"/"Beginning").
- Offline render of `frmGradingScheme` (grid + Add/Remove + Save).
- Manual (user): edit a band threshold, save, generate a report card → grades/legend reflect the
  change; running `EnsureTableAsync` twice does not duplicate or overwrite edits.

## Success criteria

- Grade bands are editable in-app by Director/Administrator and persisted in the database.
- Both report-card generators compute grade code, remark, and legend from the configured bands
  with no signature changes at call sites.
- A missing/unreadable table never breaks report cards (fail-safe to the legacy scheme).
