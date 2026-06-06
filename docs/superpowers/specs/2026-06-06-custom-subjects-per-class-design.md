# Custom Subjects Per Class — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The exam-entry subjects are a single hardcoded list in `EXAMS.cs`
(`private readonly string[] subjects = { "MATHEMATICS", "INT. SCIENCE", … }` — 9 subjects),
identical for every class. A school cannot give CRECHE a different subject set from BASIC 9,
or rename/add subjects, without editing and rebuilding code.

## Goal

Let Director/Administrator/Headmaster define each class's subject list in-app; have the exam
entry screen build its grid from the selected student's class subjects; with no behaviour
change until an admin edits a class.

## Scope

In scope:
- A `ClassSubjects` table (one row per class+subject, with display order) + seed from the
  current 9 subjects for every class in `AppConfig.ClassNames`.
- `SubjectRepository` + a fail-safe cached `Common/SubjectCatalog` accessor.
- A `frmSubjects` editor (pick a class, edit/reorder its subjects) + dashboard nav + RBAC.
- `EXAMS.cs` builds its subject grid from the looked-up student's class subjects.

Out of scope (noted, not built):
- Report card changes — it already renders each student's `examss` results, so it auto-adapts.
- A normalized master-subject catalogue; per-subject metadata (credits, teachers).
- Migrating/renaming existing `examss.subject` rows when a subject is renamed (historical rows
  keep their original text).

## Approach (chosen)

Settings table + cached accessor (same pattern as School Information / Grading Scheme). Subjects
are free-text per class with an explicit `SortOrder`. Rejected: a normalized
master-Subjects + join table (over-engineered for free-text names); a delimited string on
`ClassAssignments` (hard to edit/order).

## Data model

```sql
IF OBJECT_ID(N'ClassSubjects', N'U') IS NULL
CREATE TABLE ClassSubjects (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    ClassName NVARCHAR(50)  NOT NULL,
    Subject   NVARCHAR(80)  NOT NULL,
    SortOrder INT           NOT NULL DEFAULT (0)
);
```

**Seed (only for classes with no rows yet):** for each class in `AppConfig.ClassNames`, insert
the legacy 9 subjects with `SortOrder` 0..8:
`MATHEMATICS, INT. SCIENCE, ENGLISH LANGUAGE, SOCIAL STUDIES, COMPUTING, REL. & MORAL EDU.,
CARRER TECH., CREATIVE ART, GHANAIAN LANG.`
(seed is per-class and idempotent: a class that already has rows is left untouched, so an admin
who trims a class won't have it re-seeded).

## Components

### `Data/ISubjectRepository.cs` + `Data/SubjectRepository.cs` (new)
- `Task EnsureTableAsync()` — create + per-class seed.
- `Task<List<string>> GetSubjectsForClassAsync(string className)` — ordered by `SortOrder`.
- `Task<Dictionary<string,List<string>>> GetAllAsync()` — class → ordered subjects (for the editor).
- `Task SaveSubjectsForClassAsync(string className, IEnumerable<string> subjects)` — replace-all
  for that class (DELETE WHERE ClassName=? then INSERT with SortOrder = index).
- OleDb, `AppConfig.ConnectionString`, mirrors `GradingSchemeRepository`.
- `public static string[] LegacySubjects` — the 9, reused for seed + fail-safe.

### `Common/SubjectCatalog.cs` (new — static cached accessor)
- Lazy-load all class subjects once into a dictionary; cache; `Refresh()` clears it.
- `IReadOnlyList<string> SubjectsForClass(string className)` — the class's ordered subjects;
  **fail-safe**: empty/missing class or DB error → `LegacySubjects`.

### `frmSubjects.cs` (new — editor)
- Left: a `ListBox` of `AppConfig.ClassNames`. Right: a reorderable `ListBox`/grid of the selected
  class's subjects with **Add**, **Remove**, **Move Up**, **Move Down**; **Save** persists the
  current class and calls `SubjectCatalog.Refresh()`; selecting another class prompts to save if
  there are unsaved edits.
- Constructor: `BuildUi(); if (!AuthService.RequireAccess("frmSubjects", this)) return; Load += …`.
- Validation: no blank/duplicate subject names within a class; at least one subject to save.
- Themed with `AppConfig.Colors`; verified offline via the render harness.

### `EXAMS.cs` (modify)
- Extract the subject-grid population (header + rows + `subjectRows` dict + `RowStyles`) into
  `RebuildSubjectGrid(IReadOnlyList<string> subjects)` that clears and repopulates `subjectGrid`.
- `BuildSubjectEntryPanel` calls `RebuildSubjectGrid(SubjectCatalog.LegacySubjects)` (or empty) at
  construction; `LookupStudent` calls `RebuildSubjectGrid(SubjectCatalog.SubjectsForClass(student.ClassID))`
  after setting `classBox`. The hardcoded `subjects` array is replaced by the catalogue/fail-safe.
- The save path already iterates `subjectRows`, so it persists whatever subjects are shown.

### RBAC + navigation
- `Services/AuthService.cs`: `["frmSubjects"] = { Director, Administrator, Headmaster }`.
- `frmDashboard.cs`: a "Subjects" nav entry in the Director/Admin/Headmaster settings block
  (next to School Information / Grading Scheme).

## Data flow

1. Exam entry: look up a student → `EXAMS` reads `SubjectCatalog.SubjectsForClass(class)` (cached;
   fail-safe to the 9) → rebuilds the grid → bursar/teacher enters scores → saved to `examss` per
   subject (unchanged).
2. Admin opens `frmSubjects` → edits a class's subjects → Save → DB replace-all →
   `SubjectCatalog.Refresh()` → next exam entry for that class uses the new list.
3. Report cards: unchanged — built from each student's `examss` rows.

## Error handling

- DB/table errors in `SubjectCatalog` → `LegacySubjects`; exam entry never breaks.
- `frmSubjects` load/save errors → caught, shown via `UIHelper`, logged.
- Invalid edits (blank/duplicate) → blocked before any DB write.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection probe: seed + `SubjectsForClass("BASIC 5")` returns the 9 in order.
- Render harness: `frmSubjects` (class list + subjects editor) and `EXAMS` (grid still renders).
- Manual (user): edit CRECHE's subjects, save; open Exam entry for a CRECHE student → the grid
  shows the edited subjects; a BASIC student still shows its own list; existing exam reports
  unaffected; re-running `EnsureTableAsync` doesn't duplicate or re-seed an edited class.

## Success criteria

- Each class's subject list is editable in-app by Director/Administrator/Headmaster and persisted.
- The exam-entry grid is built from the selected student's class subjects.
- A missing/unreadable table never breaks exam entry (fail-safe to the legacy 9).
