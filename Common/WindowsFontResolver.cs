using System;
using System.IO;
using System.Linq;
using PdfSharp.Fonts;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// A simple font resolver for PdfSharp that loads fonts from the Windows Fonts directory.
    /// This is required in newer versions of PdfSharp (.NET Core / 6.0+) which do not have 
    /// access to the Windows API by default.
    /// </summary>
    public class WindowsFontResolver : IFontResolver
    {
        public string DefaultFontName => "Segoe UI";

        public byte[] GetFont(string faceName)
        {
            // The faceName parameter is what we returned from ResolveTypeface
            var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), faceName);
            if (File.Exists(fontPath))
            {
                return File.ReadAllBytes(fontPath);
            }
            
            // Fallback if the specific font file isn't found
            return null;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Map common font families to their actual Windows .ttf file names
            string fontFileName = "";
            string familyLower = familyName.ToLowerInvariant();

            if (familyLower == "segoe ui")
            {
                if (isBold && isItalic) fontFileName = "segoez.ttf";
                else if (isBold) fontFileName = "segoeuib.ttf";
                else if (isItalic) fontFileName = "segoeuii.ttf";
                else fontFileName = "segoeui.ttf";
            }
            else if (familyLower == "arial")
            {
                if (isBold && isItalic) fontFileName = "arialbi.ttf";
                else if (isBold) fontFileName = "arialbd.ttf";
                else if (isItalic) fontFileName = "ariali.ttf";
                else fontFileName = "arial.ttf";
            }
            else
            {
                // Default fallback
                fontFileName = "segoeui.ttf";
            }

            return new FontResolverInfo(fontFileName);
        }
    }
}
