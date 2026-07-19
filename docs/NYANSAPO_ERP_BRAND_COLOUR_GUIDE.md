# Nyansapo ERP Brand Colour Guide

## Purpose

Nyansapo ERP should feel premium, academic, trustworthy, and clean. The product is being sold to different schools, so the colour system must look professional by default while still allowing each school to keep its own logo and report-card accents.

The recommended identity is:

**Premium Navy + Heritage Gold + Clean White**

Navy gives the system authority and trust. Gold connects to the logo and gives the product a strong premium school-management identity. White and soft grey keep the screens modern, readable, and calm for daily office work.

## Core Palette

| Role | Colour | Hex | RGB | Use |
| --- | --- | --- | --- | --- |
| Brand Navy | Deep Navy | `#11146A` | `17, 20, 106` | Main sidebar, app headers, primary buttons, active tabs |
| Navy Hover | Royal Navy | `#1E1B7A` | `30, 27, 122` | Hover states, selected navigation backgrounds, secondary headers |
| Dark Ink | Ink Navy | `#111827` | `17, 24, 39` | Page titles, strong text, important values |
| Heritage Gold | Gold | `#D4AF37` | `212, 175, 55` | Brand accents, active indicators, progress bars, highlights |
| Soft Gold | Light Gold | `#FFF4C2` | `255, 244, 194` | Selected rows, friendly warnings, highlighted table rows |
| App Background | Soft Grey | `#F6F8FB` | `246, 248, 251` | Main page/form background |
| Card Surface | White | `#FFFFFF` | `255, 255, 255` | Cards, panels, forms, printable surfaces |
| Border | Cool Border | `#E2E8F0` | `226, 232, 240` | Input borders, grid lines, card borders |
| Muted Text | Slate Grey | `#64748B` | `100, 116, 139` | Labels, helper text, subtitles |
| Success | Emerald | `#10B981` | `16, 185, 129` | Save, paid, active, completed states |
| Danger | Rose Red | `#E11D48` | `225, 29, 72` | Delete, failed, critical debt alerts |

## Recommended Product Formula

Use this visual rhythm across the desktop app, web app, receipts, report cards, and admin screens:

| Area | Recommended Treatment |
| --- | --- |
| Sidebar | Deep navy background, white text, gold active indicator |
| Top section headers | Navy background with gold bottom line or small accent |
| Page background | Soft grey, never pure white for the whole page |
| Cards and forms | White surface, light border, subtle shadow if available |
| Primary actions | Navy button with white text |
| Save/confirm actions | Emerald button with white text |
| Destructive actions | Rose red button with white text |
| Selected table rows | Soft gold background with dark text |
| Data-grid headers | Navy background with white text |
| Important money totals | Ink navy text, gold accent if highlighted |
| Outstanding/debt values | Ink navy normally; rose red only when urgent |

## UI Rules

1. Navy should be the authority colour, not the entire interface.
2. Gold should be an accent, not a page background.
3. White cards on soft grey backgrounds should carry most forms and dashboards.
4. Red should be rare. Use it for delete, error, failed sync, or serious debt alerts only.
5. Green should mean success, saved, paid, active, or completed.
6. Do not use many unrelated colours in one form. Use navy, gold, white, grey, and one status colour.
7. Table rows should remain readable first. Alternate row grey and selected row soft gold work best.
8. Avoid heavy purple/blue gradients. The product should feel like school operations software, not a marketing page.

## Desktop WinForms Mapping

The shared `UiTheme.cs` should be the source of truth for most screens.

Recommended mapping:

```csharp
Page       = Color.FromArgb(246, 248, 251); // #F6F8FB
Surface    = Color.White;                   // #FFFFFF
SurfaceAlt = Color.FromArgb(248, 250, 252); // #F8FAFC
Border     = Color.FromArgb(226, 232, 240); // #E2E8F0
Text       = Color.FromArgb(17, 24, 39);    // #111827
Muted      = Color.FromArgb(100, 116, 139); // #64748B

Navy       = Color.FromArgb(17, 20, 106);   // #11146A
NavyHover  = Color.FromArgb(30, 27, 122);   // #1E1B7A
Gold       = Color.FromArgb(212, 175, 55);  // #D4AF37
GoldSoft   = Color.FromArgb(255, 244, 194); // #FFF4C2
Success    = Color.FromArgb(16, 185, 129);  // #10B981
Danger     = Color.FromArgb(225, 29, 72);   // #E11D48
```

Where the current project already uses `UiTheme.Navy`, `UiTheme.Gold`, `UiTheme.SurfaceAlt`, and `UiTheme.Border`, future screens should continue using those shared values instead of hard-coded colours.

## Web App Mapping

For the future ASP.NET web app, use CSS variables so the brand can stay consistent:

```css
:root {
  --nyansapo-navy: #11146a;
  --nyansapo-navy-hover: #1e1b7a;
  --nyansapo-gold: #d4af37;
  --nyansapo-gold-soft: #fff4c2;
  --nyansapo-bg: #f6f8fb;
  --nyansapo-surface: #ffffff;
  --nyansapo-text: #111827;
  --nyansapo-muted: #64748b;
  --nyansapo-border: #e2e8f0;
  --nyansapo-success: #10b981;
  --nyansapo-danger: #e11d48;
}
```

Use the same roles as the desktop app:

- `--nyansapo-navy`: navigation, page headers, primary buttons
- `--nyansapo-gold`: active accents, highlights, progress
- `--nyansapo-bg`: page background
- `--nyansapo-surface`: cards/forms
- `--nyansapo-danger`: destructive actions and errors
- `--nyansapo-success`: save/paid/completed states

## Reports, Receipts, and Printouts

Printouts should use less colour than the screen UI.

Recommended print usage:

| Print Area | Colour |
| --- | --- |
| Main school header | Navy |
| Accent line or label | Gold |
| Body text | Ink navy or black |
| Table header | Navy with white text |
| Highlight row | Very soft gold |
| Borders | Light grey |

Avoid large dark backgrounds on receipts because they waste ink. Use navy headers and gold lines instead.

## Accessibility Notes

- White text on navy is acceptable and should be used for headers and primary buttons.
- Gold text on white can be weak if the font is small. Use gold mostly for accents, not long body text.
- Muted grey text should only be used for labels and helper text, not important numbers.
- Debt balances and warning states should not rely on colour only; include labels like `Owing`, `Failed`, or `Overdue`.

## School Customisation Rule

Nyansapo ERP should keep the main product shell consistent. Schools may customise:

- Logo
- School name
- Report-card accent colours
- Receipt/logo image
- Printed document header details

Schools should not change:

- Core navigation colour system
- Button meaning colours
- Error/success colours
- Data-grid readability rules

This protects the sellable product identity while still letting each school feel represented.

## Implementation Priority

1. Update `UiTheme.cs` to match the approved palette.
2. Replace screen-level hard-coded navy/gold/grey values with `UiTheme` values.
3. Update web CSS variables to match this guide.
4. Update printable receipts/statements/report cards to use navy, gold, white, and light borders.
5. Keep school-specific report colours configurable, but keep the application shell branded as Nyansapo ERP.
