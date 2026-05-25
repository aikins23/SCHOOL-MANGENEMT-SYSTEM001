using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Generates professional PDF report cards matching Kingdom Preparatory School template.
    /// Layout includes:
    /// - Header with logo, school name, and student photo
    /// - Student information table (6 rows x 2 columns)
    /// - Subjects table with class score (50%), exam score (60%), total (100%), grade, position, remarks
    /// - Grading legend on right side of subjects table
    /// - Total scores row
    /// - Remarks section (attitude, interest, conduct, teacher comments)
    /// - Signature section
    /// </summary>
    public class ReportCardPDFGenerator
    {
        private const double PageWidth = 210;   // mm (A4)
        private const double PageHeight = 297;  // mm (A4)
        private const double Margin = 8;        // mm

        // Colors - Professional navy blue theme
        private static readonly XColor HeaderBlue = XColor.FromArgb(0x1A, 0x23, 0x32);      // Dark navy header
        private static readonly XColor TableHeaderBg = XColor.FromArgb(0x2C, 0x3E, 0x50);   // Table header
        private static readonly XColor TextDark = XColor.FromArgb(0x1F, 0x2E, 0x3D);        // Dark text
        private static readonly XColor TextLight = XColor.FromArgb(0xFF, 0xFF, 0xFF);       // White text
        private static readonly XColor BorderColor = XColor.FromArgb(0xBD, 0xBD, 0xBD);     // Light gray borders
        private static readonly XColor AlternateRowBg = XColor.FromArgb(0xF5, 0xF5, 0xF5);  // Alternate row color

        // Grading legend - numeric scale 1-5 per specification
        private static readonly GradeLevel[] GradingLevels = new[]
        {
            new GradeLevel { ScoreRange = "80+", Grade = "1", Remarks = "Advanced(A)" },
            new GradeLevel { ScoreRange = "75-79", Grade = "2", Remarks = "Proficiency(P)" },
            new GradeLevel { ScoreRange = "70-74", Grade = "3", Remarks = "Approaching Prof.(AP)" },
            new GradeLevel { ScoreRange = "65-69", Grade = "4", Remarks = "Developing(D)" },
            new GradeLevel { ScoreRange = "<65", Grade = "5", Remarks = "Beginning(B)" }
        };

        public async Task<byte[]> GeneratePDFAsync(ReportCardData data)
        {
            return await Task.Run(() =>
            {
                using (var document = new PdfDocument())
                {
                    var page = document.AddPage();
                    page.Width = XUnit.FromMillimeter(PageWidth);
                    page.Height = XUnit.FromMillimeter(PageHeight);

                    var gfx = XGraphics.FromPdfPage(page);

                    try
                    {
                        // PDFsharp uses bottom-left origin (y=0 at bottom)
                        // Start from top margin, but convert to PDF coordinates where y increases upward
                        double yPosition = PageHeight - Margin;

                        // Draw sections in order per template (moving downward in visual space)
                        yPosition = DrawHeader(gfx, page, data, yPosition);
                        yPosition = DrawStudentInfoTable(gfx, page, data, yPosition);
                        yPosition = DrawSubjectsAndLegend(gfx, page, data, yPosition);
                        yPosition = DrawTotalRow(gfx, page, data, yPosition);
                        yPosition = DrawRemarksSection(gfx, page, data, yPosition);
                        yPosition = DrawSignatureSection(gfx, page, yPosition);

                        using (var stream = new MemoryStream())
                        {
                            document.Save(stream, false);
                            return stream.ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError("Error generating report card PDF", ex);
                        throw new PDFGenerationException("Error generating report card PDF", ex);
                    }
                }
            });
        }

        /// <summary>
        /// Draws the header section with logo, school name, and student photo.
        /// Spec: 120mm height, Logo (left 100×100), School Name (center 60% width), Photo (right 100×120)
        /// </summary>
        private double DrawHeader(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double headerHeight = 120;  // mm - per specification
            double headerBoxX = Margin;
            double headerBoxWidth = PageWidth - (2 * Margin);
            double centerWidth = headerBoxWidth * 0.60;  // 60% for school name
            double sideWidth = (headerBoxWidth - centerWidth) / 2;  // 20% each for logo and photo

            // Draw header background (dark navy blue)
            // PDF Y coordinate system: y=0 at bottom, increases upward
            gfx.DrawRectangle(
                new XSolidBrush(HeaderBlue),
                headerBoxX, yStart - headerHeight, headerBoxWidth, headerHeight);

            // LEFT: Logo (100mm x 100mm per specification)
            double logoSize = 100;
            double logoX = headerBoxX + 5;
            double logoY = yStart + (headerHeight - logoSize) / 2;

            if (data.SchoolInfo?.Logo != null && data.SchoolInfo.Logo.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream(data.SchoolInfo.Logo))
                    {
                        var image = XImage.FromStream(stream);
                        gfx.DrawImage(image, logoX, logoY, logoSize, logoSize);
                    }
                }
                catch (Exception ex)
                {
                    // If logo fails, draw placeholder
                    LoggerHelper.LogError("Failed to load school logo image", ex);
                    DrawLogoPlaceholder(gfx, logoX, logoY, logoSize);
                }
            }
            else
            {
                DrawLogoPlaceholder(gfx, logoX, logoY, logoSize);
            }

            // CENTER: School name (large, bold, white) and contact info
            double centerX = headerBoxX + sideWidth;
            var schoolNameFont = new XFont("Segoe UI Bold", 28);
            var schoolName = data.SchoolInfo?.Name ?? "KINGDOM PREPARATORY SCHOOL";
            var schoolNameSize = gfx.MeasureString(schoolName, schoolNameFont);

            gfx.DrawString(
                schoolName,
                schoolNameFont,
                new XSolidBrush(TextLight),
                centerX + (centerWidth / 2) - (schoolNameSize.Width / 2),
                yStart - 30);

            // Location and contact info (smaller, white)
            var contactFont = new XFont("Segoe UI", 11);
            var location = data.SchoolInfo?.Location ?? "";
            var phone = data.SchoolInfo?.PhoneNumbers ?? "";
            var contactText = string.IsNullOrEmpty(location) ? phone : $"{location} | {phone}";

            if (!string.IsNullOrEmpty(contactText))
            {
                var contactSize = gfx.MeasureString(contactText, contactFont);
                gfx.DrawString(
                    contactText,
                    contactFont,
                    new XSolidBrush(TextLight),
                    centerX + (centerWidth / 2) - (contactSize.Width / 2),
                    yStart + 65);
            }

            // RIGHT: Student photo (100mm x 120mm per specification)
            double photoWidth = 100;
            double photoHeight = 120;
            double photoX = headerBoxX + headerBoxWidth - photoWidth - 5;
            double photoY = yStart + (headerHeight - photoHeight) / 2;

            if (data.ProfilePhoto != null && data.ProfilePhoto.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream(data.ProfilePhoto))
                    {
                        var image = XImage.FromStream(stream);
                        gfx.DrawImage(image, photoX, photoY, photoWidth, photoHeight);
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Failed to load student profile photo", ex);
                    DrawPhotoPlaceholder(gfx, photoX, photoY, photoWidth, photoHeight);
                }
            }
            else
            {
                DrawPhotoPlaceholder(gfx, photoX, photoY, photoWidth, photoHeight);
            }

            return yStart - (headerHeight + 3);
        }

        /// <summary>
        /// Draws student information in a 3-row x 2-column table layout (per specification).
        /// Row 1: Student Name | Resuming Date
        /// Row 2: Admission No. | Attendance
        /// Row 3: Class/Form | Number On Roll
        /// </summary>
        private double DrawStudentInfoTable(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = Margin;
            double tableWidth = PageWidth - (2 * Margin);
            double colWidth = tableWidth / 2;
            double rowHeight = 8;
            double yPos = yStart;

            var labelFont = new XFont("Segoe UI Bold", 9);
            var valueFont = new XFont("Segoe UI", 9);

            // Define student info rows per specification: (Label, Value, Label, Value)
            var rows = new[]
            {
                (new[] { "Student Name:", data.StudentName ?? "", "Resuming Date:", "To be announced" }),
                (new[] { "Admission No.:", data.StudentID ?? "", "Attendance:", $"{data.PresentDays}/{data.TotalSchoolDays} ({data.AttendancePercentage}%)" }),
                (new[] { "Class/Form:", data.ClassID ?? "", "Number On Roll:", data.TotalStudentsInClass.ToString() })
            };

            // Draw all rows
            foreach (var row in rows)
            {
                // Left column
                gfx.DrawRectangle(new XPen(BorderColor, 0.5), tableX, yPos, colWidth, rowHeight);
                gfx.DrawString(row[0], labelFont, new XSolidBrush(TextDark), tableX + 1, yPos + 1.5);
                gfx.DrawString(row[1], valueFont, new XSolidBrush(TextDark), tableX + 30, yPos + 1.5);

                // Right column
                gfx.DrawRectangle(new XPen(BorderColor, 0.5), tableX + colWidth, yPos, colWidth, rowHeight);
                gfx.DrawString(row[2], labelFont, new XSolidBrush(TextDark), tableX + colWidth + 1, yPos + 1.5);
                gfx.DrawString(row[3], valueFont, new XSolidBrush(TextDark), tableX + colWidth + 30, yPos + 1.5);

                yPos -= rowHeight;
            }

            return yPos - 2;
        }

        /// <summary>
        /// Draws the main subjects table and grading legend side-by-side.
        /// Main table: Subject | Class Score (50%) | Exam Score (60%) | Total (100%) | Grade | Position | Remarks
        /// Legend: Score Range | Grade | Remarks (5 rows)
        /// Spec: Row height 12mm, fonts 9pt per specification
        /// </summary>
        private double DrawSubjectsAndLegend(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            // Defensive check for null or empty SubjectResults
            if (data.SubjectResults == null || data.SubjectResults.Count == 0)
            {
                return yStart;
            }

            double tableX = Margin;
            double mainTableWidth = PageWidth - (2 * Margin) - 50;  // Leave space for legend
            double legendX = tableX + mainTableWidth + 2;
            double legendWidth = 48;
            double rowHeight = 12;  // mm - increased from 5.5 per specification
            double yPos = yStart;

            var headerFont = new XFont("Segoe UI Bold", 9);  // 9pt per specification
            var dataFont = new XFont("Segoe UI", 9);  // 9pt per specification
            // Legend uses smaller font (8pt vs 9pt table) to fit in sidebar without reducing readability
            var legendFont = new XFont("Segoe UI", 8);

            // Main table headers
            string[] headers = { "Subject", "Class\nScore\n(50%)", "Exam\nScore\n(60%)", "Total\nScore\n(100%)", "Grade", "Position", "Remarks" };
            double[] colWidths = { 22, 10, 10, 10, 8, 10, 13 };

            // Draw header row with background
            double cellX = tableX;
            double headerRowHeight = 12;  // Match subject row height for consistency
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(new XSolidBrush(TableHeaderBg), cellX, yPos, colWidths[i], headerRowHeight);
                gfx.DrawRectangle(new XPen(BorderColor, 0.5), cellX, yPos, colWidths[i], headerRowHeight);

                // Multi-line header text
                gfx.DrawString(headers[i], headerFont, new XSolidBrush(TextLight), cellX + 0.5, yPos + 0.5,
                    new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });

                cellX += colWidths[i];
            }

            yPos -= headerRowHeight;

            // Draw subject data rows
            for (int subjectIndex = 0; subjectIndex < data.SubjectResults.Count; subjectIndex++)
            {
                var subject = data.SubjectResults[subjectIndex];
                cellX = tableX;

                var values = new[]
                {
                    subject.Subject ?? "",
                    FormatScore(subject.ClassScore, 50),
                    FormatScore(subject.ExamScore, 60),
                    FormatScore(subject.TotalScore, 100),
                    subject.Grade ?? "-",
                    FormatPosition(subject.PositionInClass),
                    subject.Remark ?? ""
                };

                bool isAlternateRow = (subjectIndex % 2) == 1;
                XColor rowBg = isAlternateRow ? AlternateRowBg : XColor.FromArgb(255, 255, 255);

                for (int i = 0; i < values.Length; i++)
                {
                    gfx.DrawRectangle(new XSolidBrush(rowBg), cellX, yPos, colWidths[i], rowHeight);
                    gfx.DrawRectangle(new XPen(BorderColor, 0.5), cellX, yPos, colWidths[i], rowHeight);
                    gfx.DrawString(values[i], dataFont, new XSolidBrush(TextDark), cellX + 0.5, yPos + 0.8);

                    cellX += colWidths[i];
                }

                yPos += rowHeight;
            }

            // Draw legend on right side (starting from header row Y)
            double legendY = yStart;
            string[] legendHeaders = { "Score Range", "Grade", "Remarks" };
            double[] legendColWidths = { 16, 8, 24 };

            // Legend header
            double legendCellX = legendX;
            for (int i = 0; i < legendHeaders.Length; i++)
            {
                gfx.DrawRectangle(new XSolidBrush(TableHeaderBg), legendCellX, legendY, legendColWidths[i], headerRowHeight);
                gfx.DrawRectangle(new XPen(BorderColor, 0.5), legendCellX, legendY, legendColWidths[i], headerRowHeight);
                gfx.DrawString(legendHeaders[i], headerFont, new XSolidBrush(TextLight),
                    legendCellX + 0.5, legendY + 2, new XStringFormat { Alignment = XStringAlignment.Center });
                legendCellX += legendColWidths[i];
            }

            legendY -= headerRowHeight;

            // Legend data rows
            foreach (var gradeLevel in GradingLevels)
            {
                legendCellX = legendX;
                var legendValues = new[] { gradeLevel.ScoreRange, gradeLevel.Grade, gradeLevel.Remarks };

                for (int i = 0; i < legendValues.Length; i++)
                {
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(255, 255, 255)), legendCellX, legendY, legendColWidths[i], rowHeight);
                    gfx.DrawRectangle(new XPen(BorderColor, 0.5), legendCellX, legendY, legendColWidths[i], rowHeight);
                    gfx.DrawString(legendValues[i], legendFont, new XSolidBrush(TextDark),
                        legendCellX + 0.5, legendY + 0.8);
                    legendCellX += legendColWidths[i];
                }

                legendY += rowHeight;
            }

            return yPos - 2;
        }

        /// <summary>
        /// Draws a totals row showing aggregate scores across all subjects.
        /// </summary>
        private double DrawTotalRow(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = Margin;
            double mainTableWidth = PageWidth - (2 * Margin) - 50;
            double rowHeight = 12;  // mm - match subject row height per specification

            var totalFont = new XFont("Segoe UI Bold", 9);  // 9pt per specification
            string[] headers = { "Subject", "Class\nScore\n(50%)", "Exam\nScore\n(60%)", "Total\nScore\n(100%)", "Grade", "Position", "Remarks" };
            double[] colWidths = { 22, 10, 10, 10, 8, 10, 13 };

            decimal totalClassScore = 0;
            decimal totalExamScore = 0;
            decimal totalScore = 0;

            foreach (var subject in data.SubjectResults)
            {
                totalClassScore += subject.ClassScore;
                totalExamScore += subject.ExamScore;
                totalScore += subject.TotalScore;
            }

            int subjectCount = data.SubjectResults.Count;
            decimal avgClassScore = subjectCount > 0 ? totalClassScore / subjectCount : 0;
            decimal avgExamScore = subjectCount > 0 ? totalExamScore / subjectCount : 0;
            decimal avgTotalScore = subjectCount > 0 ? totalScore / subjectCount : 0;

            var values = new[]
            {
                "TOTAL/AVG",
                FormatScore(avgClassScore, 50),
                FormatScore(avgExamScore, 60),
                FormatScore(avgTotalScore, 100),
                GetGradeForScore(avgTotalScore),
                "-",
                ""
            };

            double cellX = tableX;
            for (int i = 0; i < values.Length; i++)
            {
                gfx.DrawRectangle(new XSolidBrush(AlternateRowBg), cellX, yStart, colWidths[i], rowHeight);
                gfx.DrawRectangle(new XPen(BorderColor, 1.0), cellX, yStart, colWidths[i], rowHeight);
                gfx.DrawString(values[i], totalFont, new XSolidBrush(TextDark), cellX + 0.5, yStart + 0.8);

                cellX += colWidths[i];
            }

            return yStart + rowHeight + 2;
        }

        /// <summary>
        /// Draws the remarks section with behavioral indicators and teacher comments.
        /// </summary>
        private double DrawRemarksSection(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = Margin;
            double tableWidth = PageWidth - (2 * Margin);
            double sectionHeight = 18;

            var labelFont = new XFont("Segoe UI Bold", 9);
            var valueFont = new XFont("Segoe UI", 9);

            // Draw section background
            gfx.DrawRectangle(new XPen(BorderColor, 0.5), tableX, yStart, tableWidth, sectionHeight);

            double yLine = yStart + 1;

            // Behavioral section
            gfx.DrawString("BEHAVIORAL INDICATORS", labelFont, new XSolidBrush(TextDark), tableX + 1, yLine);
            yLine += 3.5;

            var remarks = data.Remarks;
            gfx.DrawString("Attitude:", labelFont, new XSolidBrush(TextDark), tableX + 1, yLine);
            gfx.DrawString(remarks?.Attitude ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + 20, yLine);

            gfx.DrawString("Interest:", labelFont, new XSolidBrush(TextDark), tableX + 40, yLine);
            gfx.DrawString(remarks?.Interest ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + 55, yLine);

            gfx.DrawString("Conduct:", labelFont, new XSolidBrush(TextDark), tableX + 75, yLine);
            gfx.DrawString(remarks?.Conduct ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + 90, yLine);

            yLine += 4;

            // Teacher remarks
            gfx.DrawString("Class Teacher's Remarks:", labelFont, new XSolidBrush(TextDark), tableX + 1, yLine);
            gfx.DrawString(remarks?.ClassTeacherRemarks ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + 45, yLine);

            yLine += 4;

            gfx.DrawString("Head Teacher's Remarks:", labelFont, new XSolidBrush(TextDark), tableX + 1, yLine);
            gfx.DrawString(remarks?.HeadTeacherRemarks ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + 45, yLine);

            return yStart - (sectionHeight + 2);
        }

        /// <summary>
        /// Draws signature lines for authorized signatories.
        /// Per specification: School Director (left) | Head Teacher (right)
        /// </summary>
        private double DrawSignatureSection(XGraphics gfx, PdfPage page, double yStart)
        {
            double tableX = Margin;
            double tableWidth = PageWidth - (2 * Margin);
            double colWidth = tableWidth / 2;
            double sectionHeight = 12;
            double lineHeight = 3;

            var signatureFont = new XFont("Segoe UI", 8);
            var dateFont = new XFont("Segoe UI", 7);

            // Draw section background
            gfx.DrawRectangle(new XPen(BorderColor, 0.5), tableX, yStart, tableWidth, sectionHeight);

            double yLine = yStart + 1;

            // Left column: School Director (per specification, not Class Teacher)
            gfx.DrawLine(new XPen(TextDark, 1.0), tableX + 2, yLine + lineHeight, tableX + colWidth - 2, yLine + lineHeight);
            gfx.DrawString("School Director's Signature", signatureFont, new XSolidBrush(TextDark), tableX + 2, yLine + 4);
            gfx.DrawString("Date: __________________", dateFont, new XSolidBrush(TextDark), tableX + 2, yLine + 6.5);

            // Right column: Head Teacher
            gfx.DrawLine(new XPen(TextDark, 1.0), tableX + colWidth + 2, yLine + lineHeight, tableX + tableWidth - 2, yLine + lineHeight);
            gfx.DrawString("Head Teacher's Signature & Stamp", signatureFont, new XSolidBrush(TextDark), tableX + colWidth + 2, yLine + 4);
            gfx.DrawString("Date: __________________", dateFont, new XSolidBrush(TextDark), tableX + colWidth + 2, yLine + 6.5);

            return yStart - (sectionHeight + 1);
        }

        /// <summary>
        /// Helper to draw logo placeholder when logo image is unavailable.
        /// </summary>
        private void DrawLogoPlaceholder(XGraphics gfx, double x, double y, double size)
        {
            gfx.DrawRectangle(new XPen(TextLight, 1), x, y, size, size);
            gfx.DrawString("LOGO", new XFont("Segoe UI", 6), new XSolidBrush(TextLight),
                x + 1, y + size / 2 - 1.5);
        }

        /// <summary>
        /// Helper to draw photo placeholder when student photo is unavailable.
        /// </summary>
        private void DrawPhotoPlaceholder(XGraphics gfx, double x, double y, double width, double height)
        {
            gfx.DrawRectangle(new XPen(TextLight, 1), x, y, width, height);
            gfx.DrawString("PHOTO", new XFont("Segoe UI", 8), new XSolidBrush(TextLight),
                x + 2, y + height / 2 - 3);
        }

        /// <summary>
        /// Formats a score to one decimal place (e.g., 85.5, 92.0)
        /// </summary>
        private string FormatScore(decimal score, int maxScore)
        {
            if (score == 0 && maxScore == 0) return "-";
            return $"{score:F1}";
        }

        /// <summary>
        /// Formats position with ordinal suffix (1st, 2nd, 3rd, etc.).
        /// </summary>
        private string FormatPosition(int position)
        {
            if (position <= 0) return "-";

            string suffix = "th";
            if (position % 100 != 11 && position % 10 == 1) suffix = "st";
            else if (position % 100 != 12 && position % 10 == 2) suffix = "nd";
            else if (position % 100 != 13 && position % 10 == 3) suffix = "rd";

            return $"{position}{suffix}";
        }

        /// <summary>
        /// Determines numeric grade (1-5) based on score per specification.
        /// Grade 1: Score 80+ (Advanced)
        /// Grade 2: Score 75-79 (Proficiency)
        /// Grade 3: Score 70-74 (Approaching Proficiency)
        /// Grade 4: Score 65-69 (Developing)
        /// Grade 5: Score <65 (Beginning)
        /// </summary>
        private string GetGradeForScore(decimal score)
        {
            if (score >= 80) return "1";
            if (score >= 75) return "2";
            if (score >= 70) return "3";
            if (score >= 65) return "4";
            return "5";
        }

        private class GradeLevel
        {
            public string ScoreRange { get; set; }
            public string Grade { get; set; }
            public string Remarks { get; set; }
        }
    }

    public class PDFGenerationException : Exception
    {
        public PDFGenerationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
