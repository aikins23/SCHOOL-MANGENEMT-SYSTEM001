# Splashscreen Redesign v2 - Background-Image Based

**Date:** 2026-05-24  
**Project:** Kingdom Preparatory School Management System  
**Component:** Load Form (Splashscreen)  
**Version:** 2.0 (Background-image design)

## Overview

Redesign the application splashscreen with a modern, professional background-image approach. Replace the simple progress bar with animated loading dots overlaid on a blurred school background, creating an atmospheric and engaging loading experience.

## Design Goals

1. **Professional First Impression** — Modern, clean splash screen matching contemporary standards
2. **Atmospheric Design** — Blurred school background creates context and visual interest
3. **Clear Branding** — School logo, name, and motto prominently featured
4. **Smooth Loading UX** — Animated dots provide visual feedback without distraction
5. **Readable Design** — White and light gray text ensures readability over any background

## Visual Design

### Layout & Composition

The splash screen uses a centered vertical stack on a full-screen blurred background:

```
┌──────────────────────────────────────────┐
│                                          │
│      [Blurred School Background]         │
│                                          │
│           [School Logo Image]            │
│                                          │
│    Kingdom Preparatory School            │
│       KNOWLEDGE IS POWER                 │
│                                          │
│         ● ● ● ●  (animated)              │
│                                          │
│    DEVELOPED BY: DARKTECH HUB            │
│                                          │
└──────────────────────────────────────────┘
```

### Form Properties

- **Size:** 706x375 pixels
- **BorderStyle:** None (borderless, full coverage)
- **StartPosition:** CenterScreen
- **BackgroundImage:** Blurred school building or campus photo
- **BackgroundImageLayout:** Stretch (fills entire form)

### Elements & Positioning

#### Background Image
- **Source:** School building, campus, or professional educational facility photo
- **Effect:** Gaussian blur (~15-20px radius)
- **Opacity:** Slightly darkened or with dark overlay for text readability
- **Coverage:** Full form (706x375)

#### School Logo
- **Source:** school logo.png from resources
- **Size:** 120-150px width, auto height (maintain aspect ratio)
- **Position:** Centered, top section of form (y~30-40px from top)
- **Color:** Preserves original logo colors

#### School Name
- **Text:** "Kingdom Preparatory School"
- **Font:** Arial or Roboto, 32-36pt, Bold
- **Color:** White (#FFFFFF)
- **Alignment:** Centered horizontally
- **Position:** Below logo (~30-40px spacing)
- **Effect:** Crisp, readable text

#### Tagline/Motto
- **Text:** "KNOWLEDGE IS POWER"
- **Font:** Arial or Roboto, 14-16pt, Regular
- **Color:** Light Gray (#D3D3D3 or #E0E0E0)
- **Alignment:** Centered horizontally
- **Position:** Below school name (~10-15px spacing)
- **Effect:** Supporting text, secondary prominence

#### Loading Dots
- **Visual:** Four dots (● ● ● ●) separated by ~8-10px spacing
- **Font:** Arial, 24-28pt
- **Color:** White (#FFFFFF) or accent color (Teal #20B2AA)
- **Alignment:** Centered horizontally
- **Position:** Below tagline (~40-50px spacing)
- **Animation:** Fade in/out sequence (see Animation section)

#### Developer Credit
- **Text:** "DEVELOPED BY: DARKTECH HUB"
- **Font:** Arial or Roboto, 9-10pt, Regular
- **Color:** Light Gray (#D3D3D3)
- **Alignment:** Centered horizontally
- **Position:** Bottom of form (~15-20px from bottom)
- **Effect:** Subtle attribution

### Color Scheme

| Element | Color | Hex | Usage |
|---------|-------|-----|-------|
| Background | Blurred Photo | N/A | Full-screen backdrop |
| School Name | White | #FFFFFF | Primary heading |
| Tagline | Light Gray | #D3D3D3 | Secondary heading |
| Loading Dots | White or Teal | #FFFFFF or #20B2AA | Animated indicator |
| Credit Text | Light Gray | #D3D3D3 | Footer attribution |
| Dark Overlay (optional) | Black | #000000 (20% opacity) | Text readability enhancement |

## Animation & Interaction

### Loading Dots Animation

**Animation Style:** Sequential fade in/out

**Sequence:**
1. Dot 1: Fade in (opacity 0 → 1)
2. Dot 2: Fade in (overlap timing)
3. Dot 3: Fade in (overlap timing)
4. Dot 4: Fade in (overlap timing)
5. Dot 1: Fade out (opacity 1 → 0)
6. Repeat cycle

**Timing:**
- Fade in duration: ~300ms per dot
- Hold at full opacity: ~100ms
- Fade out duration: ~300ms
- Total cycle time: ~1.2-1.5 seconds
- Repeats continuously until loading completes

**Effect:** Creates a flowing, continuous loading indicator without text updates

### Form Transitions

**On Load:**
- Form appears centered on screen
- Background image visible immediately
- All text elements visible
- Loading dots begin animation

**On Completion (3-5 seconds):**
- Loading animation stops (dots visible at full opacity)
- Form fades out smoothly (500-800ms fade duration)
- Login form appears

**Visual Smoothness:**
- No jarring transitions
- Opacity fade creates professional appearance
- Form disappears completely before login shows

## Technical Implementation

### Framework & Libraries

- **UI Framework:** WinForms (System.Windows.Forms)
- **Form Type:** Borderless Form
- **Controls:** Standard Label controls (not Guna.UI)
- **Animation:** Timer-based opacity and text changes

### Component Structure

| Component | Type | Purpose |
|-----------|------|---------|
| Form Background | BackgroundImage property | Full-screen blurred photo |
| School Logo | PictureBox | Display school logo from resources |
| School Name | Label | Main heading text |
| Tagline | Label | Supporting text |
| Loading Dots | Label | Animated indicator |
| Credit Text | Label | Developer attribution |
| Timer | System.Windows.Forms.Timer | Control animation timing |

### Control Properties

**Form (load class):**
- BackgroundImage: school background photo (blurred)
- BackgroundImageLayout: Stretch
- FormBorderStyle: None
- StartPosition: CenterScreen
- Size: 706x375

**Logo PictureBox:**
- Image: school logo.png (from resources)
- Size: 120x120 (or proportional)
- Location: Centered, ~35px from top
- SizeMode: Zoom

**School Name Label:**
- Text: "Kingdom Preparatory School"
- Font: Arial 34pt Bold
- ForeColor: White
- Location: Centered, ~160px from top
- Size: 500x50
- TextAlign: MiddleCenter

**Tagline Label:**
- Text: "KNOWLEDGE IS POWER"
- Font: Arial 15pt Regular
- ForeColor: Light Gray
- Location: Centered, ~220px from top
- Size: 500x30
- TextAlign: MiddleCenter

**Loading Dots Label:**
- Text: "● ● ● ●"
- Font: Arial 26pt Regular
- ForeColor: White
- Location: Centered, ~260px from top
- Size: 200x40
- TextAlign: MiddleCenter

**Credit Label:**
- Text: "DEVELOPED BY: DARKTECH HUB"
- Font: Arial 9pt Regular
- ForeColor: Light Gray
- Location: Centered, ~345px from top
- Size: 500x20
- TextAlign: MiddleCenter

### Animation Logic

**Timer Settings:**
- Interval: 50ms (provides smooth animation)
- Tick event: Updates dot opacity every 50ms

**Dot Opacity Animation:**
- Track elapsed time since animation start
- Calculate which dot should be fading in/out based on elapsed time
- Update label text opacity using alpha channel
- Reset cycle when complete (~1200ms)

**Opacity Values:**
- Full opacity: 1.0
- Half opacity: 0.5
- No opacity: 0.0
- Gradual fade: Increment/decrement by ~0.05 per tick

## Loading Timeline

| Time | Event |
|------|-------|
| 0.0s | Form displays, dots animation starts |
| 0.3s | Dot 1 at full opacity, Dot 2 fading in |
| 0.6s | Dots 1-2 fading out, Dot 3 at full opacity |
| 0.9s | Dots fading, Dot 4 at full opacity |
| 1.2s | All dots fading out, cycle repeats |
| 3-5s | Loading completes, form fades out |
| 5-6s | Login form appears |

## User Experience Flow

1. **Application Start:** Splash screen appears centered on screen
2. **Loading Phase:** Background image visible, all text visible, dots animate smoothly
3. **Loading Feedback:** Animated dots indicate system is responding (no frozen appearance)
4. **Completion:** Dots stop animating, splash screen fades smoothly away
5. **Transition:** Login form appears seamlessly

## Success Criteria

- [x] Background image fills entire form (blurred, atmospheric)
- [x] School logo displays correctly from resources
- [x] Text is white (school name) and light gray (tagline, credit)
- [x] Text is readable over background (may require dark overlay)
- [x] Loading dots animate smoothly (fade in/out sequence)
- [x] Animation is continuous and non-intrusive
- [x] Form transitions smoothly (fade out, not instant)
- [x] No visual glitches or jarring movements
- [x] Professional, modern appearance matching DevExpress example
- [x] Form appears centered, properly sized (706x375)

## Dependencies & Constraints

- **Dependency:** School background image must be provided (blurred before use or blurred in code)
- **Dependency:** school logo.png resource must be available
- **Constraint:** Background image should be professional/school-related (not distracting)
- **Constraint:** Text must remain readable over any background (may need dark overlay)
- **Constraint:** Animation must be smooth (50ms timer interval recommended)
- **Constraint:** Load time should not exceed 5 seconds for smooth progress

## Notes

- If background image is too bright, add a semi-transparent dark overlay (20-30% black opacity) to ensure text readability
- Loading animation can be paused/resumed based on actual application load progress if needed
- Form can be made responsive by calculating positions based on ClientSize instead of hardcoded values
- Consider extracting animation timing values to constants for easy adjustment
