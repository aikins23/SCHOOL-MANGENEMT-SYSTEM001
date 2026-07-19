namespace KingdomPrep.DesignSystem;

/// <summary>
/// Centralized design tokens for the KingdomPrep ERP design system.
/// These values should be kept in sync with design-tokens.json and the CSS custom properties.
/// </summary>
public static class DesignTokens
{
    // Color Tokens
    public static class Colors
    {
        // Primary - Nyansapo Navy
        public const string Primary = "#11146a";
        public const string PrimaryDark = "#111827";
        public const string PrimaryHover = "#1e1b7a";
        public const string PrimaryLight = "#2d2f86";
        public const string PrimarySoft = "rgba(17, 20, 106, 0.04)";

        // Accent - Nyansapo Gold
        public const string Accent = "#d4af37";
        public const string AccentLight = "#fff4c2";
        public const string AccentSoft = "rgba(212, 175, 55, 0.12)";

        // Neutral
        public const string Background = "#f6f8fb";
        public const string Surface = "#ffffff";
        public const string SurfaceAlt = "#f8fafc";
        public const string TextPrimary = "#111827";
        public const string TextSecondary = "#64748b";
        public const string Border = "#e2e8f0";

        // Status
        public const string Success = "#10b981";
        public const string SuccessSoft = "#ecfdf5";
        public const string SuccessText = "#065f46";
        public const string Danger = "#e11d48";
        public const string DangerSoft = "#fff1f2";
        public const string Warning = "#f59e0b";
        public const string WarningSoft = "#fffbeb";
        public const string Info = "#3b82f6";
        public const string InfoSoft = "#eff6ff";

        // Disabled
        public const string DisabledBackground = "#e2e8f0";
        public const string DisabledText = "#64748b";
    }

    // Typography Tokens
    public static class Typography
    {
        public const string FontFamilyUI = "'Segoe UI', 'Helvetica Neue', Helvetica, Arial, sans-serif";
        public const string FontFamilyBrand = "'Georgia', serif";
        public const string FontFamilyMono = "'Consolas', 'Monaco', 'Courier New', monospace";

        public const string FontSizeXs = "0.75rem";      // 12px
        public const string FontSizeSm = "0.875rem";     // 14px
        public const string FontSizeBase = "1rem";       // 16px
        public const string FontSizeLg = "1.125rem";     // 18px
        public const string FontSizeXl = "1.25rem";      // 20px
        public const string FontSize2Xl = "1.5rem";      // 24px
        public const string FontSize3Xl = "1.875rem";    // 30px
        public const string FontSize4Xl = "2.25rem";     // 36px

        public const string FontWeightNormal = "400";
        public const string FontWeightMedium = "500";
        public const string FontWeightSemibold = "600";
        public const string FontWeightBold = "700";

        public const string LineHeightTight = "1.2";
        public const string LineHeightSnug = "1.375";
        public const string LineHeightNormal = "1.5";
        public const string LineHeightRelaxed = "1.625";

        public const string LetterSpacingTight = "-0.025em";
        public const string LetterSpacingNormal = "0";
        public const string LetterSpacingWide = "0.05em";
        public const string LetterSpacingWider = "0.08em";
    }

    // Spacing Tokens (8px base unit)
    public static class Spacing
    {
        public const string Space0 = "0";
        public const string Space1 = "0.25rem";  // 4px
        public const string Space2 = "0.5rem";   // 8px
        public const string Space3 = "0.75rem";  // 12px
        public const string Space4 = "1rem";     // 16px
        public const string Space5 = "1.5rem";   // 24px
        public const string Space6 = "2rem";     // 32px
        public const string Space8 = "3rem";     // 48px
        public const string Space10 = "4rem";    // 64px
        public const string Space12 = "5rem";    // 80px
        public const string Space16 = "6rem";    // 96px
    }

    // Border Radius Tokens
    public static class BorderRadius
    {
        public const string None = "0";
        public const string Sm = "0.25rem";    // 4px
        public const string Md = "0.375rem";   // 6px
        public const string Lg = "0.5rem";     // 8px
        public const string Xl = "0.75rem";    // 12px
        public const string Xxl = "1rem";      // 16px
        public const string Full = "9999px";
    }

    // Shadow Tokens
    public static class Shadows
    {
        public const string Sm = "0 1px 2px rgba(17, 20, 106, 0.05)";
        public const string Md = "0 4px 6px rgba(17, 20, 106, 0.07)";
        public const string Lg = "0 10px 15px rgba(17, 20, 106, 0.1)";
        public const string Xl = "0 20px 25px rgba(17, 20, 106, 0.12)";
        public const string Card = "0 16px 32px rgba(17, 20, 106, 0.08)";
        public const string CardHover = "0 20px 40px rgba(17, 20, 106, 0.12)";
        public const string Modal = "0 25px 50px rgba(17, 20, 106, 0.2)";
        public const string Dropdown = "0 10px 25px rgba(17, 20, 106, 0.15)";
        public const string Focus = "0 0 0 0.25rem rgba(212, 175, 55, 0.45)";
    }

    // Transition Tokens
    public static class Transitions
    {
        public const string Fast = "0.1s ease";
        public const string Normal = "0.15s ease-in-out";
        public const string Slow = "0.2s ease-in-out";
        public const string Slower = "0.3s ease-out";
    }

    // Breakpoint Tokens
    public static class Breakpoints
    {
        public const int Mobile = 640;
        public const int Tablet = 768;
        public const int Desktop = 1024;
        public const int Wide = 1280;
        public const int UltraWide = 1536;

        public const string MobileMax = "(max-width: 640.98px)";
        public const string TabletMin = "(min-width: 641px)";
        public const string DesktopMin = "(min-width: 1024px)";
        public const string WideMin = "(min-width: 1280px)";
    }

    // Z-Index Tokens
    public static class ZIndex
    {
        public const int Dropdown = 1000;
        public const int Sticky = 1020;
        public const int Modal = 1050;
        public const int Popover = 1060;
        public const int Tooltip = 1070;
        public const int Toast = 9999;
    }

    // Touch Target Tokens
    public static class TouchTargets
    {
        public const int Minimum = 44; // 44px minimum touch target
        public const string MinimumRem = "2.75rem"; // 44px at 16px base
    }
}
