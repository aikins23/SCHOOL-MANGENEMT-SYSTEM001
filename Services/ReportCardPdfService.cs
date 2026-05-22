using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace kingdom_Preparatory_School_Management_System.Services
{
    public static class ReportCardPdfService
    {
        public static void Export(Dictionary<string, string> data)
        {
            try
            {
                string studentName = data.ContainsKey("NAME") ? data["NAME"] : "Student";
                string fileName = $"ReportCard_{studentName.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                PdfDocument document = new PdfDocument();
                document.Info.Title = $"Report Card - {studentName}";
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                double pageWidth = page.Width.Point;

                // Fonts
                XFont fontTitle = new XFont("Arial", 20, XFontStyleEx.Bold);
                XFont fontHeader = new XFont("Arial", 12, XFontStyleEx.Bold);
                XFont fontBody = new XFont("Arial", 10, XFontStyleEx.Regular);
                XFont fontBodyBold = new XFont("Arial", 10, XFontStyleEx.Bold);

                // Draw Header
                gfx.DrawString("KINGDOM PREPARATORY SCHOOL", fontTitle, XBrushes.Navy, new XRect(0, 40, pageWidth, 40), XStringFormats.Center);
                gfx.DrawString("STUDENT ACADEMIC REPORT CARD", fontHeader, XBrushes.DarkGray, new XRect(0, 70, pageWidth, 20), XStringFormats.Center);
                gfx.DrawLine(XPens.Navy, 40, 95, pageWidth - 40, 95);

                // Student Info Section
                double y = 120;
                DrawInfoLine(gfx, "Student Name:", GetVal(data, "NAME"), 40, y, fontBodyBold, fontBody);
                DrawInfoLine(gfx, "Term:", GetVal(data, "TERMS"), 300, y, fontBodyBold, fontBody);
                y += 20;
                DrawInfoLine(gfx, "Class:", GetVal(data, "CLASS"), 40, y, fontBodyBold, fontBody);
                DrawInfoLine(gfx, "Academic Year:", GetVal(data, "YEAR"), 300, y, fontBodyBold, fontBody);
                y += 20;
                DrawInfoLine(gfx, "Position in Class:", FormatRank(GetVal(data, "TOTAL_RANK")), 40, y, fontBodyBold, fontBody);
                
                y += 40;

                // Subjects Table Header
                XColor tableHeaderColor = XColors.Navy;
                gfx.DrawRectangle(new XSolidBrush(tableHeaderColor), 40, y, pageWidth - 80, 25);
                
                double[] cols = { 45, 220, 280, 360, 440 }; // Subject, Mark, Position, Grade, Remark
                string[] headers = { "SUBJECT", "MARK", "POS", "GD", "REMARK" };
                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawString(headers[i], fontBodyBold, XBrushes.White, cols[i], y + 17);
                }

                y += 25;

                // Subjects Data
                string[] subjects = { "ENG", "MATHS", "SCI", "SOCIAL", "COMP", "CAREER", "CRE_ART", "RME", "GHA_LANG" };
                string[] displayNames = { "English Language", "Mathematics", "Int. Science", "Social Studies", "Computing", "Career Tech.", "Creative Art", "Rel. & Moral Edu.", "Ghanaian Lang." };

                for (int i = 0; i < subjects.Length; i++)
                {
                    string sub = subjects[i];
                    gfx.DrawRectangle(XPens.LightGray, 40, y, pageWidth - 80, 22);
                    
                    gfx.DrawString(displayNames[i], fontBody, XBrushes.Black, cols[0], y + 15);
                    gfx.DrawString(GetVal(data, sub), fontBody, XBrushes.Black, cols[1], y + 15);
                    gfx.DrawString(FormatRank(GetVal(data, sub + "_POS")), fontBody, XBrushes.Black, cols[2], y + 15);
                    gfx.DrawString(GetVal(data, sub + "_GRADE"), fontBody, XBrushes.Black, cols[3], y + 15);
                    gfx.DrawString(GetVal(data, sub + "_REMARK"), fontBody, XBrushes.Black, cols[4], y + 15);
                    
                    y += 22;
                }

                // Footer
                y += 50;
                gfx.DrawLine(XPens.Black, 40, y, 200, y);
                gfx.DrawString("Class Teacher's Signature", fontBody, XBrushes.Black, 45, y + 15);

                gfx.DrawLine(XPens.Black, pageWidth - 200, y, pageWidth - 40, y);
                gfx.DrawString("Headmaster's Signature", fontBody, XBrushes.Black, pageWidth - 195, y + 15);

                document.Save(filePath);
                MessageBox.Show($"Report card exported successfully to Desktop:\n{fileName}", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating PDF: " + ex.Message, "PDF Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void DrawInfoLine(XGraphics gfx, string label, string value, double x, double y, XFont fontBold, XFont fontRegular)
        {
            gfx.DrawString(label, fontBold, XBrushes.Black, x, y);
            gfx.DrawString(value, fontRegular, XBrushes.Black, x + gfx.MeasureString(label, fontBold).Width + 5, y);
        }

        private static string GetVal(Dictionary<string, string> data, string key)
        {
            return data.ContainsKey(key) ? data[key] : "-";
        }

        private static string FormatRank(string value)
        {
            if (int.TryParse(value, out int rank) && rank > 0)
            {
                return rank + GetOrdinalSuffix(rank);
            }
            return value;
        }

        private static string GetOrdinalSuffix(int number)
        {
            if (number <= 0) return "";
            switch (number % 100)
            {
                case 11: case 12: case 13: return "th";
            }
            switch (number % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }
    }
}
