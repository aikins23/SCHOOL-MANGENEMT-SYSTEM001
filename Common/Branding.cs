using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Resolves brand assets (logo + window icon) for the UI. Prefers the buyer's configured
    /// School Information logo (SchoolProfile.Logo); otherwise falls back to a bundled product
    /// logo file under Resources. Path lookup is robust to the build layout (AnyCPU bin\Debug
    /// and x86 bin\x86\Debug) and to running from the project tree. Everything is best-effort:
    /// a missing asset returns null so callers can fall back gracefully.
    /// </summary>
    public static class Branding
    {
        private static Image _appLogo;
        private static bool _appLogoTried;
        private static Image _appLogoOnBlue;
        private static bool _appLogoOnBlueTried;
        private static Icon _appIcon;
        private static bool _appIconTried;
        private static Icon _appIconOnBlue;
        private static bool _appIconOnBlueTried;

        /// <summary>Bundled product logo for light backgrounds.</summary>
        public static Image AppLogo
        {
            get
            {
                if (!_appLogoTried)
                {
                    _appLogoTried = true;
                    _appLogo = LoadImage("app_logo.png", "logo.png", "school_logo.png");
                }
                return _appLogo;
            }
        }

        /// <summary>Bundled product logo for dark/blue backgrounds.</summary>
        public static Image AppLogoOnBlue
        {
            get
            {
                if (!_appLogoOnBlueTried)
                {
                    _appLogoOnBlueTried = true;
                    _appLogoOnBlue = LoadImage("app_logo_bg.png", "logo_bg.png");
                    if (_appLogoOnBlue == null) _appLogoOnBlue = AppLogo;
                }
                return _appLogoOnBlue;
            }
        }

        /// <summary>
        /// The logo to show in app chrome: the buyer's configured logo if set, else the bundled
        /// product logo. 
        /// </summary>
        public static Image Logo => GetLogo(false);

        /// <summary>
        /// The logo to show in app chrome: the buyer's configured logo if set, else the bundled
        /// product logo. 
        /// </summary>
        /// <param name="onBlue">True to prefer the on-blue version if using the bundled logo.</param>
        public static Image GetLogo(bool onBlue)
        {
            try
            {
                byte[] bytes = SchoolProfile.Logo;
                if (bytes != null && bytes.Length > 0)
                    using (var ms = new MemoryStream(bytes))
                        return new Bitmap(ms);
            }
            catch { /* fall through to bundled */ }
            return onBlue ? AppLogoOnBlue : AppLogo;
        }

        /// <summary>Window/taskbar icon from Resources\app_icon.png, then icon.png. Null if absent.</summary>
        public static Icon AppIcon
        {
            get
            {
                if (!_appIconTried)
                {
                    _appIconTried = true;
                    _appIcon = LoadIcon("app_icon.png", "icon.png");
                }
                return _appIcon;
            }
        }

        /// <summary>Window/taskbar icon for blue backgrounds.</summary>
        public static Icon AppIconOnBlue
        {
            get
            {
                if (!_appIconOnBlueTried)
                {
                    _appIconOnBlueTried = true;
                    _appIconOnBlue = LoadIcon("app_icon_bg.png", "icon_bg.png");
                    if (_appIconOnBlue == null) _appIconOnBlue = AppIcon;
                }
                return _appIconOnBlue;
            }
        }

        private static IEnumerable<string> CandidateDirs()
        {
            // The assembly's own folder is the most reliable (correct under odd hosting too);
            // AppDomain.BaseDirectory is the usual runtime value. Include project-tree fallbacks.
            string asmDir = null;
            try { asmDir = Path.GetDirectoryName(typeof(Branding).Assembly.Location); } catch { }

            foreach (var root in new[] { asmDir, AppDomain.CurrentDomain.BaseDirectory })
            {
                if (string.IsNullOrEmpty(root)) continue;
                yield return Path.Combine(root, "Resources");                             // copied to output
                yield return SafeFull(Path.Combine(root, "..", "..", "Resources"));       // AnyCPU bin\Debug
                yield return SafeFull(Path.Combine(root, "..", "..", "..", "Resources")); // x86 bin\x86\Debug
            }
        }

        private static string SafeFull(string p)
        {
            try { return Path.GetFullPath(p); } catch { return p; }
        }

        private static string Resolve(params string[] fileNames)
        {
            foreach (var dir in CandidateDirs())
            {
                foreach (var name in fileNames)
                {
                    try
                    {
                        string path = Path.Combine(dir, name);
                        if (File.Exists(path)) return path;
                    }
                    catch { /* ignore bad path */ }
                }
            }
            return null;
        }

        private static Image LoadImage(params string[] fileNames)
        {
            try
            {
                string path = Resolve(fileNames);
                if (path == null) return null;
                using (var tmp = Image.FromFile(path))   // load then copy so the file isn't locked
                    return new Bitmap(tmp);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Branding.LoadImage: " + ex.Message);
                return null;
            }
        }

        private static Icon LoadIcon(params string[] fileNames)
        {
            try
            {
                string path = Resolve(fileNames);
                if (path == null) return null;
                using (var bmp = new Bitmap(path))
                    return Icon.FromHandle(bmp.GetHicon());
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Branding.LoadIcon: " + ex.Message);
                return null;
            }
        }
    }
}
