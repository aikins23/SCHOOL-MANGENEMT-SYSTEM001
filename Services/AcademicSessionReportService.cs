using System;
using System.IO;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Common;
using KingdomPrep.Shared.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public class AcademicSessionReportService
    {
        public Task<string> GenerateClosureReportAsync(TermClosureSummary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (summary.Term == null) throw new ArgumentException("Term summary is required.", nameof(summary));

            return Task.Run(() =>
            {
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports", "session-closures");
                Directory.CreateDirectory(folder);

                var safeName = MakeSafeFileName(summary.Term.DisplayName);
                var path = Path.Combine(folder, $"{safeName}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

                using (var document = new PdfDocument())
                {
                    document.Info.Title = $"Session Closure - {summary.Term.DisplayName}";
                    var page = document.AddPage();
                    page.Width = XUnit.FromPoint(595);
                    page.Height = XUnit.FromPoint(842);

                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        DrawReport(gfx, summary);
                    }

                    document.Save(path);
                }

                return path;
            });
        }

        private static void DrawReport(XGraphics gfx, TermClosureSummary summary)
        {
            var navy = ToX(UiTheme.Navy);
            var gold = ToX(UiTheme.Gold);
            var muted = ToX(UiTheme.Muted);
            var text = ToX(UiTheme.Text);

            var titleFont = new XFont("Arial", 22, XFontStyleEx.Bold);
            var headingFont = new XFont("Arial", 13, XFontStyleEx.Bold);
            var labelFont = new XFont("Arial", 9, XFontStyleEx.Bold);
            var bodyFont = new XFont("Arial", 10, XFontStyleEx.Regular);
            var valueFont = new XFont("Arial", 16, XFontStyleEx.Bold);

            gfx.DrawRectangle(new XSolidBrush(navy), 0, 0, 595, 94);
            gfx.DrawString(SchoolProfile.DisplayName, titleFont, XBrushes.White, new XRect(44, 24, 507, 28), XStringFormats.TopLeft);
            gfx.DrawString("Academic Session Closure Report", bodyFont, new XSolidBrush(gold), new XRect(44, 57, 507, 22), XStringFormats.TopLeft);

            double y = 126;
            gfx.DrawString(summary.Term.DisplayName, headingFont, new XSolidBrush(text), 44, y);
            y += 22;
            gfx.DrawString($"{summary.Term.StartDate:dd MMM yyyy} - {summary.Term.EndDate:dd MMM yyyy}", bodyFont, new XSolidBrush(muted), 44, y);
            y += 36;

            DrawMetric(gfx, 44, y, "Total Students", summary.TotalStudents.ToString("N0"), labelFont, valueFont, text, muted);
            DrawMetric(gfx, 220, y, "New Admissions", summary.NewAdmissions.ToString("N0"), labelFont, valueFont, text, muted);
            DrawMetric(gfx, 396, y, "Total Staff", summary.TotalEmployees.ToString("N0"), labelFont, valueFont, text, muted);
            y += 104;

            gfx.DrawString("Financial Performance", headingFont, new XSolidBrush(text), 44, y);
            y += 18;
            gfx.DrawLine(new XPen(gold, 2), 44, y, 551, y);
            y += 24;

            DrawFinancialRow(gfx, y, "Expected Fees", summary.TotalExpectedFees); y += 34;
            DrawFinancialRow(gfx, y, "Collected Fees", summary.TotalCollectedFees); y += 34;
            DrawFinancialRow(gfx, y, "Outstanding Fees Carried Forward", summary.TotalOutstandingFees); y += 48;

            gfx.DrawString("Operational Notes", headingFont, new XSolidBrush(text), 44, y);
            y += 24;
            gfx.DrawString("This report was generated automatically when the term was closed. Closed terms should be treated as locked audit periods.", bodyFont, new XSolidBrush(muted), new XRect(44, y, 507, 44), XStringFormats.TopLeft);

            gfx.DrawString($"Generated: {DateTime.Now:dd MMM yyyy, h:mm tt}", bodyFont, new XSolidBrush(muted), new XRect(44, 780, 507, 18), XStringFormats.TopLeft);
            PrintBranding.DrawPdfFooter(gfx, 595, 842);
        }

        private static void DrawMetric(XGraphics gfx, double x, double y, string label, string value, XFont labelFont, XFont valueFont, XColor text, XColor muted)
        {
            gfx.DrawRectangle(new XPen(XColor.FromArgb(223, 230, 240)), x, y, 150, 74);
            gfx.DrawString(label.ToUpperInvariant(), labelFont, new XSolidBrush(muted), new XRect(x + 14, y + 14, 122, 16), XStringFormats.TopLeft);
            gfx.DrawString(value, valueFont, new XSolidBrush(text), new XRect(x + 14, y + 36, 122, 24), XStringFormats.TopLeft);
        }

        private static void DrawFinancialRow(XGraphics gfx, double y, string label, decimal amount)
        {
            var text = ToX(UiTheme.Text);
            var muted = ToX(UiTheme.Muted);
            var bodyFont = new XFont("Arial", 10, XFontStyleEx.Regular);
            var valueFont = new XFont("Arial", 11, XFontStyleEx.Bold);

            gfx.DrawString(label, bodyFont, new XSolidBrush(muted), new XRect(64, y, 280, 20), XStringFormats.TopLeft);
            gfx.DrawString($"GHS {amount:N2}", valueFont, new XSolidBrush(text), new XRect(344, y, 160, 20), XStringFormats.TopRight);
            gfx.DrawLine(new XPen(XColor.FromArgb(223, 230, 240)), 64, y + 24, 531, y + 24);
        }

        private static string MakeSafeFileName(string value)
        {
            var safe = string.IsNullOrWhiteSpace(value) ? "term" : value;
            foreach (var ch in Path.GetInvalidFileNameChars())
                safe = safe.Replace(ch, '-');
            return safe.Replace(" ", "-");
        }

        private static XColor ToX(System.Drawing.Color color) =>
            XColor.FromArgb(color.A, color.R, color.G, color.B);
    }
}
