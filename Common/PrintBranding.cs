using System.Drawing;
using PdfSharp.Drawing;

namespace kingdom_Preparatory_School_Management_System.Common
{
    public static class PrintBranding
    {
        public const string BrandName = "Nyansapo School ERP";
        public const string BrandContact = "darktechhub2@gmail.com | +233 54 836 9261 / +233 20 493 9571 | Accra, Ghana";
        public static string FooterText => BrandName + " | " + BrandContact;

        public static void DrawPdfFooter(XGraphics gfx, double pageWidth, double pageHeight, double margin = 36)
        {
            if (gfx == null) return;

            var font = new XFont("Arial", 6.8, XFontStyleEx.Regular);
            var brush = new XSolidBrush(XColor.FromArgb(125, 100, 116, 139));
            var pen = new XPen(XColor.FromArgb(70, 148, 163, 184), 0.4);
            double lineY = pageHeight - 24;

            gfx.DrawLine(pen, margin, lineY, pageWidth - margin, lineY);
            gfx.DrawString(
                FooterText,
                font,
                brush,
                new XRect(margin, lineY + 4, pageWidth - (margin * 2), 12),
                XStringFormats.TopCenter);
        }

        public static void DrawGraphicsFooter(Graphics graphics, Rectangle bounds)
        {
            if (graphics == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            using (var font = new Font("Segoe UI", 7.2F, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(115, 100, 116, 139)))
            using (var pen = new Pen(Color.FromArgb(80, 148, 163, 184), 1F))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                int lineY = bounds.Bottom - 20;
                graphics.DrawLine(pen, bounds.Left, lineY, bounds.Right, lineY);
                graphics.DrawString(FooterText, font, brush, new RectangleF(bounds.Left, lineY + 2, bounds.Width, 16), format);
            }
        }
    }
}
