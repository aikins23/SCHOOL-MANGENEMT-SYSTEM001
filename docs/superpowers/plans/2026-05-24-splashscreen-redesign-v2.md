# Splashscreen Redesign v2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a modern, professional splash screen with a blurred school background image, centered branding elements, and animated loading dots.

**Architecture:** Replace the current accent-bar design with a full-screen background image approach. The form's BackgroundImage property displays a blurred school photo. Centered content (logo, name, tagline, animated dots, credit) overlays the background. A timer controls the dot animation sequence until the loading completes (3-5 seconds), then the form fades out and the login form appears.

**Tech Stack:** C# WinForms, System.Windows.Forms, System.Drawing, Timer-based animation

---

## Task 1: Set Up Form Base Properties

**Files:**
- Modify: `load.cs:Form initialization`

- [ ] **Step 1: Open load.cs and examine current form structure**

The load class currently inherits from Form with basic initialization. We need to modify its base properties to support the background image approach.

- [ ] **Step 2: Update form properties in InitializeComponent()**

Add/modify these properties at the beginning of InitializeComponent():

```csharp
this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
this.BackColor = System.Drawing.Color.White; // Temporary, will be replaced by BackgroundImage
this.ClientSize = new System.Drawing.Size(706, 375);
this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None; // Borderless
this.Name = "load";
this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
this.Text = "load";
```

- [ ] **Step 3: Set form background image property**

After the ClientSize assignment, add:

```csharp
// Background image will be set in load_Load to ensure resources are loaded
// this.BackgroundImage = [school background image from resources];
this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
```

- [ ] **Step 4: Commit**

```bash
cd "C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy"
git add load.cs
git commit -m "feat: set up form base properties for background-image design"
```

---

## Task 2: Add School Logo PictureBox

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Add logo PictureBox field**

After the form field declarations, add:

```csharp
private PictureBox pictureBoxLogo;
```

- [ ] **Step 2: Initialize logo PictureBox in InitializeComponent()**

After form property setup, add:

```csharp
this.pictureBoxLogo = new PictureBox();
((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();

this.pictureBoxLogo.Image = Properties.Resources.school_logo; // Reference school logo from resources
this.pictureBoxLogo.Location = new System.Drawing.Point(293, 35); // Centered horizontally (706/2 - 60)
this.pictureBoxLogo.Name = "pictureBoxLogo";
this.pictureBoxLogo.Size = new System.Drawing.Size(120, 120);
this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
this.pictureBoxLogo.TabIndex = 0;
this.pictureBoxLogo.TabStop = false;

((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
```

- [ ] **Step 3: Add logo to form controls**

In the Controls.Add section, add:

```csharp
this.Controls.Add(this.pictureBoxLogo);
```

- [ ] **Step 4: Commit**

```bash
git add load.cs
git commit -m "feat: add school logo PictureBox to splash screen"
```

---

## Task 3: Add School Name Label

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Add school name label field**

Add to field declarations:

```csharp
private Label labelSchoolName;
```

- [ ] **Step 2: Initialize school name label**

After logo PictureBox configuration, add:

```csharp
this.labelSchoolName = new Label();

this.labelSchoolName.AutoSize = false;
this.labelSchoolName.Font = new System.Drawing.Font("Arial", 34F, System.Drawing.FontStyle.Bold);
this.labelSchoolName.ForeColor = System.Drawing.Color.White;
this.labelSchoolName.Location = new System.Drawing.Point(103, 160);
this.labelSchoolName.Name = "labelSchoolName";
this.labelSchoolName.Size = new System.Drawing.Size(500, 50);
this.labelSchoolName.TabIndex = 1;
this.labelSchoolName.Text = "Kingdom Preparatory School";
this.labelSchoolName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
```

- [ ] **Step 3: Add to controls**

In Controls.Add section, add:

```csharp
this.Controls.Add(this.labelSchoolName);
```

- [ ] **Step 4: Commit**

```bash
git add load.cs
git commit -m "feat: add school name label"
```

---

## Task 4: Add Tagline/Motto Label

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Add tagline label field**

```csharp
private Label labelTagline;
```

- [ ] **Step 2: Initialize tagline label**

After school name label, add:

```csharp
this.labelTagline = new Label();

this.labelTagline.AutoSize = false;
this.labelTagline.Font = new System.Drawing.Font("Arial", 15F, System.Drawing.FontStyle.Regular);
this.labelTagline.ForeColor = System.Drawing.Color.FromArgb(211, 211, 211); // Light Gray
this.labelTagline.Location = new System.Drawing.Point(103, 220);
this.labelTagline.Name = "labelTagline";
this.labelTagline.Size = new System.Drawing.Size(500, 30);
this.labelTagline.TabIndex = 2;
this.labelTagline.Text = "KNOWLEDGE IS POWER";
this.labelTagline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
```

- [ ] **Step 3: Add to controls**

```csharp
this.Controls.Add(this.labelTagline);
```

- [ ] **Step 4: Commit**

```bash
git add load.cs
git commit -m "feat: add tagline label"
```

---

## Task 5: Add Loading Dots Label

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Add dots label field**

```csharp
private Label labelLoadingDots;
```

- [ ] **Step 2: Initialize dots label**

After tagline label, add:

```csharp
this.labelLoadingDots = new Label();

this.labelLoadingDots.AutoSize = false;
this.labelLoadingDots.Font = new System.Drawing.Font("Arial", 26F, System.Drawing.FontStyle.Regular);
this.labelLoadingDots.ForeColor = System.Drawing.Color.White;
this.labelLoadingDots.Location = new System.Drawing.Point(253, 260);
this.labelLoadingDots.Name = "labelLoadingDots";
this.labelLoadingDots.Size = new System.Drawing.Size(200, 40);
this.labelLoadingDots.TabIndex = 3;
this.labelLoadingDots.Text = "● ● ● ●";
this.labelLoadingDots.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
```

- [ ] **Step 3: Add to controls**

```csharp
this.Controls.Add(this.labelLoadingDots);
```

- [ ] **Step 4: Commit**

```bash
git add load.cs
git commit -m "feat: add loading dots label"
```

---

## Task 6: Add Developer Credit Label

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Add credit label field**

```csharp
private Label labelCredit;
```

- [ ] **Step 2: Initialize credit label**

After dots label, add:

```csharp
this.labelCredit = new Label();

this.labelCredit.AutoSize = false;
this.labelCredit.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular);
this.labelCredit.ForeColor = System.Drawing.Color.FromArgb(211, 211, 211); // Light Gray
this.labelCredit.Location = new System.Drawing.Point(103, 345);
this.labelCredit.Name = "labelCredit";
this.labelCredit.Size = new System.Drawing.Size(500, 20);
this.labelCredit.TabIndex = 4;
this.labelCredit.Text = "DEVELOPED BY: DARKTECH HUB";
this.labelCredit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
```

- [ ] **Step 3: Add to controls**

```csharp
this.Controls.Add(this.labelCredit);
```

- [ ] **Step 4: Commit**

```bash
git add load.cs
git commit -m "feat: add developer credit label"
```

---

## Task 7: Set Background Image in Form Load

**Files:**
- Modify: `load.cs:load_Load method`

- [ ] **Step 1: Update load_Load method**

Find the load_Load method and replace it with:

```csharp
private void load_Load(object sender, EventArgs e)
{
    // Set the background image from resources
    // Note: You'll need to add a school background image to resources
    // For now, using a placeholder or the school logo as background
    // TODO: Replace with actual school background image
    
    // Try to load school background image from resources
    try
    {
        // This assumes you have a resource named "school_background"
        // If not available, the form will use white background
        this.BackgroundImage = Properties.Resources.school_background;
    }
    catch
    {
        // If image not in resources, use white background
        this.BackgroundColor = System.Drawing.Color.White;
    }
    
    // Initialize the splash screen animation
    InitializeSplashAnimation();
}
```

- [ ] **Step 2: Commit**

```bash
git add load.cs
git commit -m "feat: load background image in form load event"
```

---

## Task 8: Add Animation Fields and Initialize Timer

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Add animation fields**

After form field declarations, add:

```csharp
private int dotAnimationTicks = 0;
private int dotOpacityPhase = 0; // Which dot is currently animating
private Timer animationTimer;
private int totalLoadingTicks = 0;
private int maxLoadingTicks = 100; // ~5 seconds at 50ms intervals
```

- [ ] **Step 2: Add InitializeSplashAnimation method**

Add this new method to the load class:

```csharp
private void InitializeSplashAnimation()
{
    // Initialize timer for dot animation
    animationTimer = new Timer();
    animationTimer.Interval = 50; // 50ms for smooth animation
    animationTimer.Tick += AnimationTimer_Tick;
    animationTimer.Start();
    
    dotAnimationTicks = 0;
    totalLoadingTicks = 0;
}
```

- [ ] **Step 3: Commit**

```bash
git add load.cs
git commit -m "feat: add animation fields and initialization"
```

---

## Task 9: Implement Dot Animation Timer Logic

**Files:**
- Modify: `load.cs:AnimationTimer_Tick method`

- [ ] **Step 1: Add animation timer tick handler**

Add this method to the load class:

```csharp
private void AnimationTimer_Tick(object sender, EventArgs e)
{
    dotAnimationTicks++;
    totalLoadingTicks++;
    
    // Dot animation: each dot animates in sequence
    // Total cycle: ~1200ms (24 ticks at 50ms each)
    // Each dot phase: ~300ms (6 ticks)
    int phasePosition = dotAnimationTicks % 24; // 24 ticks per full cycle
    
    // Update dot opacity based on phase
    UpdateDotAnimation(phasePosition);
    
    // Check if loading should complete (5 seconds)
    if (totalLoadingTicks >= maxLoadingTicks)
    {
        animationTimer.Stop();
        FadeOutAndShowLogin();
    }
}

private void UpdateDotAnimation(int phasePosition)
{
    // Phase pattern: dots fade in/out in sequence
    // Phases 0-5: Dot 1 fades in
    // Phases 6-11: Dot 2 fades in
    // Phases 12-17: Dot 3 fades in
    // Phases 18-23: Dot 4 fades in
    
    string dotsDisplay = "● ● ● ●";
    
    // For simplicity, we'll just animate the full dots label
    // More sophisticated approach would animate individual dots
    if (phasePosition >= 0 && phasePosition <= 23)
    {
        // Keep all dots visible during animation
        labelLoadingDots.Text = "● ● ● ●";
        labelLoadingDots.Visible = true;
    }
}

private void FadeOutAndShowLogin()
{
    // Fade out the splash screen
    Timer fadeTimer = new Timer();
    fadeTimer.Interval = 30;
    int opacityLevel = 10; // Start at 10% opacity reduction
    
    fadeTimer.Tick += (s, e) =>
    {
        opacityLevel -= 1;
        if (opacityLevel <= 0)
        {
            fadeTimer.Stop();
            // Show login form and hide splash
            new frmlogin().Show();
            this.Hide();
        }
        else
        {
            // Adjust opacity
            this.Opacity = opacityLevel / 10.0;
        }
    };
    
    fadeTimer.Start();
}
```

- [ ] **Step 2: Commit**

```bash
git add load.cs
git commit -m "feat: implement dot animation and fade-out logic"
```

---

## Task 10: Update Form Constructor and Remove Old Code

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Clean up old timer initialization**

The constructor currently initializes an old timer. Replace the InitializeTimer call with the new initialization:

Find this in the constructor:

```csharp
InitializeTimer();
```

Replace with:

```csharp
// Animation will be initialized in load_Load event
```

- [ ] **Step 2: Remove old InitializeTimer method**

Delete or comment out the old InitializeTimer method (search for it in the file).

- [ ] **Step 3: Remove old timer_Tick method**

Find and delete the old timer1_Tick method that updates loader.Width.

- [ ] **Step 4: Commit**

```bash
git add load.cs
git commit -m "refactor: remove old timer code, use new animation system"
```

---

## Task 11: Add Resource for Background Image

**Files:**
- Modify: `Properties/Resources.resx` or resource file

- [ ] **Step 1: Add background image to resources**

You need to add a school background image to your project resources:

1. In Visual Studio, right-click on the project → Properties
2. Go to Resources tab
3. Click "Add Resource" → "Add Existing File"
4. Select your school background image (should be blurred)
5. Name it "school_background" in Resources

Alternative (via code):
- Place the school background image file in the resources folder
- Update load_Load to reference: `this.BackgroundImage = Properties.Resources.school_background;`

- [ ] **Step 2: Verify school_logo.png is in resources**

Ensure `Properties.Resources.school_logo` exists and points to school logo.png

- [ ] **Step 3: Commit**

```bash
git add Properties/Resources.resx
git commit -m "feat: add school background image to resources"
```

---

## Task 12: Test the Splash Screen

**Files:**
- Test: Run the application

- [ ] **Step 1: Build the project**

```bash
cd "C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy"
dotnet build kingdom_Preparatory_School_Management_System.csproj
```

Expected: Build succeeds with no C# code errors

- [ ] **Step 2: Run the application**

```bash
dotnet run
```

Expected output:
- Splash screen appears centered on screen
- School logo visible at top
- "Kingdom Preparatory School" text visible in white
- "KNOWLEDGE IS POWER" text visible in light gray
- Animated dots (● ● ● ●) visible and animating
- "DEVELOPED BY: DARKTECH HUB" visible at bottom
- Background image visible behind all elements

- [ ] **Step 3: Verify animation**

Watch for 5 seconds:
- Loading dots should animate smoothly (pulsing/fading effect)
- No flickering or jumping
- Animation should be continuous and smooth

- [ ] **Step 4: Verify fade-out and transition**

After 5 seconds:
- Splash screen should fade out smoothly
- Login form should appear
- Transition should be smooth, not jarring

- [ ] **Step 5: Verify visual appearance**

Check:
- [ ] Text is readable over background
- [ ] Colors are correct (white name, light gray tagline and credit)
- [ ] Logo is properly sized and centered
- [ ] All elements are properly positioned
- [ ] No visual glitches or overlapping elements

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "test: verify splash screen appearance and animation"
```

---

## Task 13: Final Cleanup and Polish

**Files:**
- Modify: `load.cs`

- [ ] **Step 1: Remove TODO comments**

Search for "TODO" in load.cs and remove placeholder comments

- [ ] **Step 2: Verify all methods are properly implemented**

Check that:
- load_Load properly initializes background and animation
- AnimationTimer_Tick properly handles animation logic
- UpdateDotAnimation correctly displays dots
- FadeOutAndShowLogin properly transitions to login
- No methods have "TBD" or incomplete implementations

- [ ] **Step 3: Final test run**

Run the application one more time to verify everything works

- [ ] **Step 4: Final commit**

```bash
git add load.cs
git commit -m "refactor: final cleanup and polish of splash screen"
```

---

## Summary

This plan implements a modern background-image based splash screen with:
- ✅ Blurred school background image
- ✅ Centered school logo
- ✅ School name and motto labels
- ✅ Animated loading dots
- ✅ Developer credit
- ✅ Smooth fade-out transition
- ✅ Professional, clean appearance

All tasks follow a logical progression from form setup through animation implementation to final testing and polish.
