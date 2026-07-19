# Font Files for KingdomPrep ERP

This directory should contain the following WOFF2 font files for the design system:

## Required Fonts

### Segoe UI (UI Font)
- `segoeui.woff2` - Regular (400)
- `segoeui-bold.woff2` - Bold (700)

### Georgia (Brand Font)
- `georgia.woff2` - Regular (400)
- `georgia-bold.woff2` - Bold (700)

## How to Obtain

### Option 1: Convert from System Fonts (Windows)
If you have Windows, the fonts are installed at:
- `C:\Windows\Fonts\segoeui.ttf` / `segoeuib.ttf`
- `C:\Windows\Fonts\georgia.ttf` / `georgiab.ttf`

Convert to WOFF2 using:
```bash
# Using woff2_compress (from Google's woff2 tools)
woff2_compress segoeui.ttf
woff2_compress segoeuib.ttf
woff2_compress georgia.ttf
woff2_compress georgiab.ttf
```

Or use online converters like:
- https://transfonter.org/
- https://cloudconvert.com/ttf-to-woff2

### Option 2: Use Web-Safe Alternatives (No Font Files Needed)
If you cannot obtain the licensed fonts, the CSS in `design-system/tokens/colors.css` already includes fallback stacks:

```css
font-family: 'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif;
font-family: 'Georgia', serif;
```

The `@font-face` declarations in `design-system/typography/fonts.css` will gracefully fail if the WOFF2 files are missing, and the browser will use the system fallbacks.

## Licensing Notes

- **Segoe UI**: Microsoft font, licensed for Windows use. For web use, you may need a separate license or use the system font stack fallback.
- **Georgia**: Core web font, widely available on Windows/macOS. Safe to use with fallback stack.

## Recommended Approach for Production

1. **Development**: Use system font stack (no files needed)
2. **Production**:
   - Option A: Use `@font-face` with `local()` to use system-installed fonts
   - Option B: License web font versions from Microsoft/Monotype
   - Option C: Use similar open-source alternatives:
     - Instead of Segoe UI: `Inter` (https://fonts.google.com/specimen/Inter)
     - Instead of Georgia: `Merriweather` (https://fonts.google.com/specimen/Merriweather)

### Open Source Alternative Setup

If using Google Fonts alternatives, update `fonts.css`:

```css
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap');

:root {
  --font-family-ui: 'Inter', 'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif;
  --font-family-brand: 'Merriweather', 'Georgia', serif;
}
```
