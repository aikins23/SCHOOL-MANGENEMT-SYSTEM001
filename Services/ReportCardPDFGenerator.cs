using System;
using System.IO;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Service for generating professional PDF report cards using PDFsharp
    /// Matches Kingdom Preparatory School terminal report card layout exactly
    /// </summary>
    public class ReportCardPDFGenerator
    {
        private const double PageWidth = 210;   // mm
        private const double PageHeight = 297;  // mm
        private const double Margin = 10;       // mm

        // Colors
        private static readonly XColor HeaderBlue = XColor.FromArgb(0x1A, 0x2B, 0x47);
        private static readonly XColor TextDark = XColor.FromArgb(0x19, 0x24, 0x31);
        private static readonly XColor TextLight = XColor.FromArgb(0xFF, 0xFF, 0xFF);
        private static readonly XColor BorderGray = XColor.FromArgb(0xE0, 0xE0, 0xE0);
        private static readonly XColor HeaderGray = XColor.FromArgb(0xF0, 0xF0, 0xF0);

        /// <summary>
        /// Generates a complete report card PDF as a byte array
        /// </summary>
        public async Task<byte[]> GeneratePDFAsync(ReportCardData data)
        {
            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(PageWidth);
                page.Height = XUnit.FromMillimeter(PageHeight);

                var gfx = XGraphics.FromPdfPage(page);

                try
                {
                    double yPosition = Margin;

                    // Draw sections
                    yPosition = await DrawHeaderAsync(gfx, page, data, yPosition);
                    yPosition = DrawStudentInfo(gfx, page, data, yPosition);
                    yPosition = DrawSubjectsTable(gfx, page, data, yPosition);
                    yPosition = DrawRemarksSection(gfx, page, data, yPosition);
                    yPosition = DrawSignatureSection(gfx, page, yPosition);

                    // Convert to byte array
                    using (var stream = new MemoryStream())
                    {
                        document.Save(stream, false);
                        return stream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    throw new PDFGenerationException("Error generating report card PDF", ex);
                }
            }
        }

        private async Task<double> DrawHeaderAsync(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double headerHeight = 40;  // mm

            // Draw header background (dark blue)
            gfx.DrawRectangle(
                new XSolidBrush(HeaderBlue),
                Margin, yStart, PageWidth - (2 * Margin), headerHeight);

            // School Name (centered)
            var font = new XFont("Segoe UI", 20, XFontStyle.Bold);
            var schoolNameSize = gfx.MeasureString(data.SchoolInfo.Name, font);
            gfx.DrawString(
                data.SchoolInfo.Name,
                font,
                new XSolidBrush(TextLight),
                (PageWidth / 2) - (schoolNameSize.Width / 2),
                yStart + 5);

            // Location and contact
            var contactFont = new XFont("Segoe UI", 8);
            var locationText = $"{data.SchoolInfo.Location} | {data.SchoolInfo.PhoneNumbers}";
            var contactSize = gfx.MeasureString(locationText, contactFont);
            gfx.DrawString(
                locationText,
                contactFont,
                new XSolidBrush(TextLight),
                (PageWidth / 2) - (contactSize.Width / 2),
                yStart + 20);

            // Draw student photo placeholder (right side)
            var photoX = PageWidth - Margin - 30;
            gfx.DrawRectangle(
                new XSolidBrush(XColor.FromArgb(200, 200, 200)),
                photoX, yStart + 2, 28, 36);

            gfx.DrawString(
                "PHOTO",
                new XFont("Segoe UI", 7),
                new XSolidBrush(TextDark),
                photoX + 2, yStart + 16);

            return yStart + headerHeight + 3;
        }

        private double DrawStudentInfo(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double sectionHeight = 18;  // mm
            double tableX = Margin;
            double tableWidth = PageWidth - (2 * Margin);

            // Header background
            gfx.DrawRectangle(
                new XSolidBrush(HeaderGray),
                tableX, yStart, tableWidth, sectionHeight);

            var boldFont = new XFont("Segoe UI", 9, XFontStyle.Bold);
            var regularFont = new XFont("Segoe UI", 8);

            // Draw student info fields
            double col1X = tableX + 2;
            double col2X = tableX + (tableWidth / 2);
            double yText = yStart + 2;

            // Row 1
            gfx.DrawString("Student Name:", boldFont, new XSolidBrush(TextDark), col1X, yText);
            gfx.DrawString(data.StudentName, regularFont, new XSolidBrush(TextDark), col1X + 40, yText);

            gfx.DrawString("Resuming Date:", boldFont, new XSolidBrush(TextDark), col2X, yText);
            gfx.DrawString("MON, 1ST SEPTEMBER, " + data.Year.Split('/')[0], regularFont, new XSolidBrush(TextDark), col2X + 42, yText);

            // Row 2
            yText += 5;
            gfx.DrawString("Admission No.:", boldFont, new XSolidBrush(TextDark), col1X, yText);
            gfx.DrawString(data.StudentID, regularFont, new XSolidBrush(TextDark), col1X + 40, yText);

            gfx.DrawString("Attendance:", boldFont, new XSolidBrush(TextDark), col2X, yText);
            gfx.DrawString($"{data.PresentDays} Out of {data.TotalSchoolDays}", regularFont, new XSolidBrush(TextDark), col2X + 42, yText);

            // Row 3
            yText += 5;
            gfx.DrawString("Class/Form:", boldFont, new XSolidBrush(TextDark), col1X, yText);
            gfx.DrawString(data.ClassID, regularFont, new XSolidBrush(TextDark), col1X + 40, yText);

            gfx.DrawString("Number On Roll:", boldFont, new XSolidBrush(TextDark), col2X, yText);
            gfx.DrawString(data.TotalStudentsInClass.ToString(), regularFont, new XSolidBrush(TextDark), col2X + 52, yText);

            return yStart + sectionHeight + 3;
        }

        private double DrawSubjectsTable(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = Margin;
            double tableWidth = PageWidth - (2 * Margin);
            double rowHeight = 5;
            double yPos = yStart;

            // Table header
            var headerFont = new XFont("Segoe UI", 7, XFontStyle.Bold);
            var dataFont = new XFont("Segoe UI", 7);

            string[] headers = { "Subject", "Class Score", "Exams Score", "Total Score", "Grade", "Position", "Remarks" };
            double[] columnWidths = { 25, 20, 20, 20, 10, 15, 30 };

            // Draw header row
            double cellX = tableX;
            foreach (var i in System.Linq.Enumerable.Range(0, headers.Length))
            {
                gfx.DrawRectangle(
                    new XSolidBrush(HeaderGray),
                    cellX, yPos, columnWidths[i], rowHeight);

                gfx.DrawString(
                    headers[i],
                    headerFont,
                    new XSolidBrush(TextDark),
                    cellX + 1, yPos + 1.5);

                cellX += columnWidths[i];
            }

            yPos += rowHeight;

            // Draw data rows
            foreach (var subject in data.SubjectResults)
            {
                cellX = tableX;

                var values = new[] {
                    subject.Subject,
                    subject.ClassScore.ToString("F1"),
                    subject.ExamScore.ToString("F0"),
                    subject.TotalScore.ToString("F1"),
                    subject.Grade,
                    subject.PositionInClass.ToString() + (IsOrdinalOne(subject.PositionInClass) ? "st" : IsOrdinalTwo(subject.PositionInClass) ? "nd" : IsOrdinalThree(subject.PositionInClass) ? "rd" : "th"),
                    subject.Remark
                };

                foreach (var i in System.Linq.Enumerable.Range(0, values.Length))
                {
                    gfx.DrawRectangle(
                        new XSolidBrush(XColor.White),
                        cellX, yPos, columnWidths[i], rowHeight);
                    gfx.DrawRectangle(
                        new XPen(BorderGray),
                        cellX, yPos, columnWidths[i], rowHeight);

                    gfx.DrawString(
                        values[i],
                        dataFont,
                        new XSolidBrush(TextDark),
                        cellX + 1, yPos + 1.5);

                    cellX += columnWidths[i];
                }

                yPos += rowHeight;
            }

            return yPos + 3;
        }

        private double DrawRemarksSection(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double sectionHeight = 15;
            double tableX = Margin;
            double tableWidth = PageWidth - (2 * Margin);

            var boldFont = new XFont("Segoe UI", 8, XFontStyle.Bold);
            var regularFont = new XFont("Segoe UI", 7);

            // Draw remarks background
            gfx.DrawRectangle(
                new XSolidBrush(HeaderGray),
                tableX, yStart, tableWidth, sectionHeight);

            gfx.DrawString("Attitude:", boldFont, new XSolidBrush(TextDark), tableX + 2, yStart + 1);
            gfx.DrawString(data.Remarks?.Attitude ?? "", regularFont, new XSolidBrush(TextDark), tableX + 25, yStart + 1);

            gfx.DrawString("Interest:", boldFont, new XSolidBrush(TextDark), tableX + 2, yStart + 4);
            gfx.DrawString(data.Remarks?.Interest ?? "", regularFont, new XSolidBrush(TextDark), tableX + 25, yStart + 4);

            gfx.DrawString("Conduct:", boldFont, new XSolidBrush(TextDark), tableX + 2, yStart + 7);
            gfx.DrawString(data.Remarks?.Conduct ?? "", regularFont, new XSolidBrush(TextDark), tableX + 25, yStart + 7);

            gfx.DrawString("Class Teacher's Remarks:", boldFont, new XSolidBrush(TextDark), tableX + 2, yStart + 10);
            gfx.DrawString(data.Remarks?.ClassTeacherRemarks ?? "", regularFont, new XSolidBrush(TextDark), tableX + 55, yStart + 10);

            gfx.DrawString("Head Teacher's Remarks:", boldFont, new XSolidBrush(TextDark), tableX + 2, yStart + 13);
            gfx.DrawString(data.Remarks?.HeadTeacherRemarks ?? "", regularFont, new XSolidBrush(TextDark), tableX + 55, yStart + 13);

            return yStart + sectionHeight + 3;
        }

        private double DrawSignatureSection(XGraphics gfx, PdfPage page, double yStart)
        {
            double sectionHeight = 18;
            double tableX = Margin;
            double col1X = tableX;
            double col2X = tableX + ((PageWidth - (2 * Margin)) / 2);
            double tableWidth = PageWidth - (2 * Margin);

            var regularFont = new XFont("Segoe UI", 8);

            // Draw background
            gfx.DrawRectangle(
                new XSolidBrush(HeaderGray),
                tableX, yStart, tableWidth, sectionHeight);

            // Signature lines
            double lineY = yStart + 8;
            gfx.DrawLine(new XPen(TextDark), col1X + 2, lineY, col1X + 25, lineY);
            gfx.DrawLine(new XPen(TextDark), col2X + 2, lineY, col2X + 25, lineY);

            gfx.DrawString("School Director's Signature", regularFont, new XSolidBrush(TextDark), col1X + 2, yStart + 10);
            gfx.DrawString("Head Teacher's Signature & Stamp", regularFont, new XSolidBrush(TextDark), col2X + 2, yStart + 10);

            return yStart + sectionHeight + 2;
        }

        private bool IsOrdinalOne(int n) => n % 100 == 11 ? false : n % 10 == 1;
        private bool IsOrdinalTwo(int n) => n % 100 == 12 ? false : n % 10 == 2;
        private bool IsOrdinalThree(int n) => n % 100 == 13 ? false : n % 10 == 3;
    }

    public class PDFGenerationException : Exception
    {
        public PDFGenerationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
