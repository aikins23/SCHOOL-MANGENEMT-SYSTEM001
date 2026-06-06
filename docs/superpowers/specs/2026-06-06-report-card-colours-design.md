# Configurable Report Card Colours (+ Preview) — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The report card's brand colours are hardcoded in `Services/ReportCardPDFGenerator.cs`:
- `Navy = XColor.FromArgb(9, 35, 96)` — header band + section header fills (the dominant colour).
- `Gold = XColor.FromArgb(210, 190, 36)` — accent text (logo cell, "Grading System" labels).
- `LightBlue = XColor.FromArgb(189, 214, 238)` — total/alternating row tint.

A school cannot match the report card to its own brand without editing and rebuilding code.

## Goal

Let Director/Administrator choose the report card's three brand colours in the School
Information settings, preview a sample report card with the chosen colours (printable) before
committing, and have the report card generator use the saved colours — with no behaviour
change until an admin edits them.

## Scope

In scope:
- Three configurable colours: **Primary** (header/Navy), **Accent** (Gold), **Secondary**
  (row tint/Light Blue). Black/white text stays fixed.
- Stored on the existing `SchoolInformation` row (this is school branding, which already lives
  there with the logo/name).
- `SchoolProfile` exposes them as `System.Drawing.Color`, plus a transient preview override.
- A "Report Card Colours" section in `frmSchoolInfo` with three colour pickers, a **Preview
  Report Card** button, and Save.
- `ReportCardPDFGenerator` reads the colours from `SchoolProfile`.

Out of scope:
- Theming any other screen/report; the dead `ReportCardPdfService` draw path (already
  unreachable) stays untouched.
- Per-class-level or per-term colour variation.

## Approach (chosen)

Extend the School Information settings (table + `SchoolProfile` accessor + `frmSchoolInfo`),
matching the established pattern. Colours stored as **ARGB ints** (clean `Color.ToArgb()` /
`Color.FromArgb(int)` round-trip). The generator reads `SchoolProfile` and converts
`System.Drawing.Color` → `XColor`. Preview uses a transient in-memory override so unsaved
selections render without being persisted.

## Data model

Add three columns to the existing `SchoolInformation` table (idempotent — the table already
exists from the School Information feature):

```sql
-- inside SchoolInfoRepository.EnsureTablesAsync, after the CREATE TABLE block:
IF COL_LENGTH('SchoolInformation','PrimaryColor')   IS NULL
    ALTER TABLE SchoolInformation ADD PrimaryColor   INT NOT NULL CONSTRAINT DF_SI_Primary   DEFAULT (<navyArgb>);
IF COL_LENGTH('SchoolInformation','AccentColor')    IS NULL
    ALTER TABLE SchoolInformation ADD AccentColor    INT NOT NULL CONSTRAINT DF_SI_Accent    DEFAULT (<goldArgb>);
IF COL_LENGTH('SchoolInformation','SecondaryColor') IS NULL
    ALTER TABLE SchoolInformation ADD SecondaryColor INT NOT NULL CONSTRAINT DF_SI_Secondary DEFAULT (<lightBlueArgb>);
```

`<navyArgb>` = `Color.FromArgb(9,35,96).ToArgb()`, `<goldArgb>` = `Color.FromArgb(210,190,36).ToArgb()`,
`<lightBlueArgb>` = `Color.FromArgb(189,214,238).ToArgb()` (all opaque, alpha 255). The defaults
mean existing installs keep the current look until changed. The seed `INSERT` for a brand-new
row also sets these three columns.

## Components

### `Models/SchoolInformation.cs` (modify)
Add `int PrimaryColorArgb`, `int AccentColorArgb`, `int SecondaryColorArgb` with defaults equal
to the three ARGB constants above.

### `Data/SchoolInfoRepository.cs` (modify)
- `EnsureTablesAsync`: add the three idempotent `ALTER TABLE` statements; include the three
  columns in the seed `INSERT`.
- `GetAsync`: read the three columns (default to the constants if `DBNull`).
- `SaveAsync`: write the three columns.

### `Common/SchoolProfile.cs` (modify)
- Expose `System.Drawing.Color ReportPrimaryColor`, `ReportAccentColor`, `ReportSecondaryColor`
  (from the stored ARGB ints; fail-safe to the defaults).
- Add a transient, nullable override:
  `public static (Color Primary, Color Accent, Color Secondary)? ReportColorOverride;`
  Each `Report*Color` getter returns the override component when the override is set, otherwise
  the saved value. (Not persisted; cleared by the caller.)

### `Services/ReportCardPDFGenerator.cs` (modify)
Replace the three `private static readonly XColor` fields with properties that read
`SchoolProfile` and convert:
```
private static XColor Navy      => ToX(Common.SchoolProfile.ReportPrimaryColor);
private static XColor Gold      => ToX(Common.SchoolProfile.ReportAccentColor);
private static XColor LightBlue => ToX(Common.SchoolProfile.ReportSecondaryColor);
private static XColor ToX(System.Drawing.Color c) => XColor.FromArgb(c.A, c.R, c.G, c.B);
```
All existing uses of `Navy`/`Gold`/`LightBlue` keep working unchanged. `Black`/`White` stay
fixed.

### `frmSchoolInfo.cs` (modify) — "Report Card Colours" section
- Three rows, each: a label, a colour **swatch** `Panel` (filled with the current colour), and a
  "Change…" button opening the standard `ColorDialog`; picking a colour updates the swatch and
  an in-memory `Color` field.
- A **Preview Report Card** button:
  1. set `SchoolProfile.ReportColorOverride = (primary, accent, secondary)` from the swatches;
  2. build a sample `ReportCardData` (a dummy student + a few subjects + term/year);
  3. `new ReportCardPDFGenerator().GeneratePDFAsync(sample)` → bytes → write to a temp PDF →
     `Process.Start` to open it (the admin views/prints from the viewer);
  4. clear `SchoolProfile.ReportColorOverride` in a `finally`.
- **Save** persists the three ARGB ints (with the rest of the School Information row) and calls
  `SchoolProfile.Refresh()`.
- Load fills the swatches from `SchoolProfile`'s saved colours.

## Data flow

1. Report card generation → `ReportCardPDFGenerator` reads `SchoolProfile.Report*Color`
   (override if a preview is in progress, else the saved DB value; fail-safe to defaults).
2. Admin opens School Information → picks colours → **Preview** (renders a sample with the
   on-screen colours via the transient override, opens a printable PDF) → **Save** persists →
   `Refresh()` → real report cards use the new colours.

## Error handling

- DB/missing-column errors → fail-safe to the default colours; report cards never break.
- Preview failure (generation/IO) → caught, shown via `UIHelper.ShowError`; the override is
  always cleared in `finally`.
- `ColorDialog` cancel → no change.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection probe: `SchoolProfile.ReportPrimaryColor` returns the default navy when unset; the
  ARGB round-trip is correct.
- Render harness: `frmSchoolInfo` shows the three swatches + Change…/Preview buttons.
- Manual (user): pick a new primary colour → **Preview** opens a sample report card in that
  colour → Save → generate a real report card → it uses the new colour; reopen settings → the
  colours persist; previewing does not change the saved colours until Save.

## Success criteria

- The three report card brand colours are editable in-app (Director/Administrator), previewable
  as a printable sample before saving, and persisted in the database.
- `ReportCardPDFGenerator` uses the configured colours with no call-site changes.
- A missing/unreadable column never breaks report cards (fail-safe to the current defaults).
