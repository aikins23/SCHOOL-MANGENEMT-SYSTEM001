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
        // Color definitions - Professional navy blue theme
        private static readonly XColor HeaderBlue = XColor.FromArgb(0x1A, 0x23, 0x32);      // Dark navy header
        private static readonly XColor TableHeaderBg = XColor.FromArgb(0x2C, 0x3E, 0x50);   // Table header
        private static readonly XColor TextDark = XColor.FromArgb(0x1F, 0x2E, 0x3D);        // Dark text
        private static readonly XColor TextLight = XColor.FromArgb(0xFF, 0xFF, 0xFF);       // White text
        private static readonly XColor BorderColor = XColor.FromArgb(0xBD, 0xBD, 0xBD);     // Light gray borders
        private static readonly XColor AlternateRowBg = XColor.FromArgb(0xF5, 0xF5, 0xF5);  // Alternate row color
        private static readonly XColor WhiteBackground = XColor.FromArgb(0xFF, 0xFF, 0xFF); // White background

        /// <summary>
        /// Contains all magic number constants used in PDF layout and formatting.
        /// Organized by category: Page Measurements, Font Sizes, Spacing & Dimensions, and Pen Widths.
        /// All measurements in millimeters unless otherwise noted.
        /// </summary>
        private static class PDFConstants
        {
            // ==================== PAGE MEASUREMENTS (in millimeters) ====================
            /// <summary>
            /// Width of A4 page in millimeters (standard paper width)
            /// </summary>
            public const double PageWidth = 210;

            /// <summary>
            /// Height of A4 page in millimeters (standard paper height)
            /// </summary>
            public const double PageHeight = 297;

            /// <summary>
            /// Top, bottom, left, and right margin from page edges in millimeters
            /// </summary>
            public const double Margin = 8;

            /// <summary>
            /// Height of the header section containing logo, school name, and student photo (120mm)
            /// </summary>
            public const double HeaderHeight = 120;

            /// <summary>
            /// Width of school logo in header section (100mm)
            /// </summary>
            public const double LogoWidth = 100;

            /// <summary>
            /// Height of school logo in header section (100mm)
            /// </summary>
            public const double LogoHeight = 100;

            /// <summary>
            /// Horizontal offset of logo from left edge of header (5mm padding)
            /// </summary>
            public const double LogoLeftPadding = 5;

            /// <summary>
            /// Width of student photo in header section (100mm)
            /// </summary>
            public const double PhotoWidth = 100;

            /// <summary>
            /// Height of student photo in header section (120mm)
            /// </summary>
            public const double PhotoHeight = 120;

            /// <summary>
            /// Horizontal offset of photo from right edge of header (5mm padding)
            /// </summary>
            public const double PhotoRightPadding = 5;

            /// <summary>
            /// Percentage of header width allocated to center section (school name) = 60%
            /// </summary>
            public const double CenterSectionPercentage = 0.60;

            /// <summary>
            /// Height of each row in student information table (8mm per row)
            /// </summary>
            public const double StudentInfoRowHeight = 8;

            /// <summary>
            /// Height of each row in subjects and legend tables (12mm per row)
            /// </summary>
            public const double SubjectsTableRowHeight = 12;

            /// <summary>
            /// Height of behavioral remarks section (18mm)
            /// </summary>
            public const double RemarksTableHeight = 18;

            /// <summary>
            /// Height of signature section (12mm)
            /// </summary>
            public const double SignatureTableHeight = 12;

            /// <summary>
            /// Width of grading legend sidebar on right of main subjects table (48mm)
            /// </summary>
            public const double LegendWidth = 48;

            /// <summary>
            /// Horizontal gap between main table and legend (2mm)
            /// </summary>
            public const double TableLegendGap = 2;

            /// <summary>
            /// Vertical spacing below header section before next content (3mm)
            /// </summary>
            public const double HeaderBottomGap = 3;

            /// <summary>
            /// Vertical spacing below student info table before next content (2mm)
            /// </summary>
            public const double StudentInfoTableBottomGap = 2;

            /// <summary>
            /// Vertical spacing below subjects table before next content (2mm)
            /// </summary>
            public const double SubjectsTableBottomGap = 2;

            /// <summary>
            /// Vertical spacing below signature section before page end (1mm)
            /// </summary>
            public const double SignatureTableBottomGap = 1;

            /// <summary>
            /// Height of header row in subjects and legend tables (12mm)
            /// </summary>
            public const double TableHeaderHeight = 12;

            // ==================== FONT SIZES (in points) ====================
            /// <summary>
            /// Font size for school name in header (28pt)
            /// </summary>
            public const int SchoolNameFontSize = 28;

            /// <summary>
            /// Font size for contact information in header (11pt)
            /// </summary>
            public const int HeaderContactFontSize = 11;

            /// <summary>
            /// Font size for student info table labels and values (9pt)
            /// </summary>
            public const int StudentInfoFontSize = 9;

            /// <summary>
            /// Font size for subjects table headers and data (9pt)
            /// </summary>
            public const int SubjectsTableFontSize = 9;

            /// <summary>
            /// Font size for grading legend text (8pt - smaller than main table)
            /// </summary>
            public const int LegendFontSize = 8;

            /// <summary>
            /// Font size for remarks section text (9pt)
            /// </summary>
            public const int RemarksFontSize = 9;

            /// <summary>
            /// Font size for signature section labels (8pt)
            /// </summary>
            public const int SignatureLabelFontSize = 8;

            /// <summary>
            /// Font size for date field in signature section (7pt)
            /// </summary>
            public const int SignatureDateFontSize = 7;

            /// <summary>
            /// Font size for logo/photo placeholder text (6pt for logo, 8pt for photo)
            /// </summary>
            public const int LogoPlaceholderFontSize = 6;

            /// <summary>
            /// Font size for photo placeholder text (8pt)
            /// </summary>
            public const int PhotoPlaceholderFontSize = 8;

            // ==================== SPACING & TEXT POSITIONING (in millimeters) ====================
            /// <summary>
            /// Left padding for text within cells (1mm)
            /// </summary>
            public const double CellTextLeftPadding = 1;

            /// <summary>
            /// Top padding for text within cells (1.5mm for small rows, 0.8mm for tall rows)
            /// </summary>
            public const double CellTextTopPaddingSmall = 1.5;

            /// <summary>
            /// Top padding for text within cells in subjects table (0.8mm)
            /// </summary>
            public const double CellTextTopPaddingLarge = 0.8;

            /// <summary>
            /// Offset for left column values in student info table (30mm from column start)
            /// </summary>
            public const double StudentInfoValueColumnOffset = 30;

            /// <summary>
            /// X-offset for header school name (moved right by 30mm from center)
            /// </summary>
            public const double HeaderSchoolNameYOffset = 30;

            /// <summary>
            /// Y-offset for contact info text in header, relative to center (65mm down from top)
            /// </summary>
            public const double HeaderContactInfoYOffset = 65;

            /// <summary>
            /// Vertical spacing for behavioral section title to first item (3.5mm)
            /// </summary>
            public const double RemarksLabelSpacing = 3.5;

            /// <summary>
            /// Horizontal offset for "Interest" label in remarks section (40mm from left)
            /// </summary>
            public const double RemarksInterestLabelX = 40;

            /// <summary>
            /// Horizontal offset for "Interest" value in remarks section (55mm from left)
            /// </summary>
            public const double RemarksInterestValueX = 55;

            /// <summary>
            /// Horizontal offset for "Conduct" label in remarks section (75mm from left)
            /// </summary>
            public const double RemarksConductLabelX = 75;

            /// <summary>
            /// Horizontal offset for "Conduct" value in remarks section (90mm from left)
            /// </summary>
            public const double RemarksConductValueX = 90;

            /// <summary>
            /// Horizontal offset for teacher remarks value in remarks section (45mm from left)
            /// </summary>
            public const double RemarksValueX = 45;

            /// <summary>
            /// Vertical spacing for behavioral items in remarks section (4mm between rows)
            /// </summary>
            public const double RemarksRowSpacing = 4;

            /// <summary>
            /// Height of signature line in signature section (3mm from top)
            /// </summary>
            public const double SignatureLineHeight = 3;

            /// <summary>
            /// Vertical offset for signature line label below line (4mm)
            /// </summary>
            public const double SignatureLineLabelOffset = 4;

            /// <summary>
            /// Vertical offset for date field below signature line (6.5mm)
            /// </summary>
            public const double SignatureDateOffset = 6.5;

            /// <summary>
            /// Horizontal padding for signature section content (2mm from edges)
            /// </summary>
            public const double SignatureSectionPadding = 2;

            /// <summary>
            /// Vertical padding for remarks section content (1mm from top)
            /// </summary>
            public const double RemarksSectionTopPadding = 1;

            /// <summary>
            /// Vertical offset for "Attitude" label in remarks (1mm from section top)
            /// </summary>
            public const double RemarksAttitudeYOffset = 0; // Will be added to base Y

            /// <summary>
            /// Horizontal offset for "Attitude" value in remarks section (20mm from left)
            /// </summary>
            public const double RemarksAttitudeValueX = 20;

            /// <summary>
            /// Vertical offset for "Class Teacher's Remarks" label (4mm down from attitude line)
            /// </summary>
            public const double RemarksClassTeacherYOffset = 4;

            /// <summary>
            /// Logo placeholder text offset from top left (1mm, size/2 - 1.5mm)
            /// </summary>
            public const double LogoPlaceholderTextOffsetX = 1;

            /// <summary>
            /// Photo placeholder text offset from top left (2mm horizontally)
            /// </summary>
            public const double PhotoPlaceholderTextOffsetX = 2;

            /// <summary>
            /// Photo placeholder text vertical centering offset (height/2 - 3mm)
            /// </summary>
            public const double PhotoPlaceholderTextOffsetY = 3;

            /// <summary>
            /// Vertical offset to center placeholder text within logo area (1.5mm)
            /// </summary>
            public const double LogoPlaceholderTextCenteringOffset = 1.5;

            // ==================== PEN WIDTHS & BORDER STYLES (in points) ====================
            /// <summary>
            /// Standard border width for table cells (0.5 points)
            /// </summary>
            public const double StandardBorderWidth = 0.5;

            /// <summary>
            /// Bold border width for total row (1.0 points)
            /// </summary>
            public const double BoldBorderWidth = 1.0;

            /// <summary>
            /// Signature line width (1.0 points)
            /// </summary>
            public const double SignatureLineWidth = 1.0;

            /// <summary>
            /// Placeholder border width (1 point)
            /// </summary>
            public const double PlaceholderBorderWidth = 1;

            // ==================== LEGEND COLUMN WIDTHS (in millimeters) ====================
            /// <summary>
            /// Width of "Score Range" column in grading legend (16mm)
            /// </summary>
            public const double LegendScoreRangeColumnWidth = 16;

            /// <summary>
            /// Width of "Grade" column in grading legend (8mm)
            /// </summary>
            public const double LegendGradeColumnWidth = 8;

            /// <summary>
            /// Width of "Remarks" column in grading legend (24mm)
            /// </summary>
            public const double LegendRemarksColumnWidth = 24;

            // ==================== SUBJECTS TABLE COLUMN WIDTHS (in millimeters) ====================
            /// <summary>
            /// Width of "Subject" column in subjects table (22mm)
            /// </summary>
            public const double SubjectsTableSubjectColumnWidth = 22;

            /// <summary>
            /// Width of "Class Score (50%)" column in subjects table (10mm)
            /// </summary>
            public const double SubjectsTableClassScoreColumnWidth = 10;

            /// <summary>
            /// Width of "Exam Score (60%)" column in subjects table (10mm)
            /// </summary>
            public const double SubjectsTableExamScoreColumnWidth = 10;

            /// <summary>
            /// Width of "Total Score (100%)" column in subjects table (10mm)
            /// </summary>
            public const double SubjectsTableTotalScoreColumnWidth = 10;

            /// <summary>
            /// Width of "Grade" column in subjects table (8mm)
            /// </summary>
            public const double SubjectsTableGradeColumnWidth = 8;

            /// <summary>
            /// Width of "Position" column in subjects table (10mm)
            /// </summary>
            public const double SubjectsTablePositionColumnWidth = 10;

            /// <summary>
            /// Width of "Remarks" column in subjects table (13mm)
            /// </summary>
            public const double SubjectsTableRemarksColumnWidth = 13;

            // ==================== STRING FORMATTING CONSTANTS ====================
            /// <summary>
            /// Default font family for all text elements
            /// </summary>
            public const string DefaultFontFamily = "Segoe UI";

            /// <summary>
            /// Font family for bold text elements
            /// </summary>
            public const string BoldFontFamily = "Segoe UI Bold";
        }

        // Logging and computed layout values
        private static class LayoutCalculations
        {
            /// <summary>
            /// Calculate main table width (total width minus margins, minus legend space)
            /// </summary>
            public static double MainTableWidth => PDFConstants.PageWidth - (2 * PDFConstants.Margin) - PDFConstants.LegendWidth;
        }

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
                    page.Width = XUnit.FromMillimeter(PDFConstants.PageWidth);
                    page.Height = XUnit.FromMillimeter(PDFConstants.PageHeight);

                    var gfx = XGraphics.FromPdfPage(page);

                    try
                    {
                        // PDFsharp uses bottom-left origin (y=0 at bottom)
                        // Start from top margin, but convert to PDF coordinates where y increases upward
                        double yPosition = PDFConstants.PageHeight - PDFConstants.Margin;

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
            double headerBoxX = PDFConstants.Margin;
            double headerBoxWidth = PDFConstants.PageWidth - (2 * PDFConstants.Margin);
            double centerWidth = headerBoxWidth * PDFConstants.CenterSectionPercentage;
            double sideWidth = (headerBoxWidth - centerWidth) / 2;

            // Draw header background (dark navy blue)
            // PDF Y coordinate system: y=0 at bottom, increases upward
            gfx.DrawRectangle(
                new XSolidBrush(HeaderBlue),
                headerBoxX, yStart - PDFConstants.HeaderHeight, headerBoxWidth, PDFConstants.HeaderHeight);

            // LEFT: Logo (100mm x 100mm per specification)
            double logoX = headerBoxX + PDFConstants.LogoLeftPadding;
            double logoY = yStart + (PDFConstants.HeaderHeight - PDFConstants.LogoHeight) / 2;

            if (data.SchoolInfo?.Logo != null && data.SchoolInfo.Logo.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream(data.SchoolInfo.Logo))
                    {
                        var image = XImage.FromStream(stream);
                        gfx.DrawImage(image, logoX, logoY, PDFConstants.LogoWidth, PDFConstants.LogoHeight);
                    }
                }
                catch (Exception ex)
                {
                    // If logo fails, draw placeholder
                    LoggerHelper.LogError("Failed to load school logo image", ex);
                    DrawLogoPlaceholder(gfx, logoX, logoY, PDFConstants.LogoWidth);
                }
            }
            else
            {
                DrawLogoPlaceholder(gfx, logoX, logoY, PDFConstants.LogoWidth);
            }

            // CENTER: School name (large, bold, white) and contact info
            double centerX = headerBoxX + sideWidth;
            var schoolNameFont = new XFont(PDFConstants.BoldFontFamily, PDFConstants.SchoolNameFontSize);
            var schoolName = data.SchoolInfo?.Name ?? "KINGDOM PREPARATORY SCHOOL";
            var schoolNameSize = gfx.MeasureString(schoolName, schoolNameFont);

            gfx.DrawString(
                schoolName,
                schoolNameFont,
                new XSolidBrush(TextLight),
                centerX + (centerWidth / 2) - (schoolNameSize.Width / 2),
                yStart - PDFConstants.HeaderSchoolNameYOffset);

            // Location and contact info (smaller, white)
            var contactFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.HeaderContactFontSize);
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
                    yStart + PDFConstants.HeaderContactInfoYOffset);
            }

            // RIGHT: Student photo (100mm x 120mm per specification)
            double photoX = headerBoxX + headerBoxWidth - PDFConstants.PhotoWidth - PDFConstants.PhotoRightPadding;
            double photoY = yStart + (PDFConstants.HeaderHeight - PDFConstants.PhotoHeight) / 2;

            if (data.ProfilePhoto != null && data.ProfilePhoto.Length > 0)
            {
                try
                {
                    using (var stream = new MemoryStream(data.ProfilePhoto))
                    {
                        var image = XImage.FromStream(stream);
                        gfx.DrawImage(image, photoX, photoY, PDFConstants.PhotoWidth, PDFConstants.PhotoHeight);
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Failed to load student profile photo", ex);
                    DrawPhotoPlaceholder(gfx, photoX, photoY, PDFConstants.PhotoWidth, PDFConstants.PhotoHeight);
                }
            }
            else
            {
                DrawPhotoPlaceholder(gfx, photoX, photoY, PDFConstants.PhotoWidth, PDFConstants.PhotoHeight);
            }

            return yStart - (PDFConstants.HeaderHeight + PDFConstants.HeaderBottomGap);
        }

        /// <summary>
        /// Draws student information in a 3-row x 2-column table layout (per specification).
        /// Row 1: Student Name | Resuming Date
        /// Row 2: Admission No. | Attendance
        /// Row 3: Class/Form | Number On Roll
        /// </summary>
        private double DrawStudentInfoTable(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = PDFConstants.Margin;
            double tableWidth = PDFConstants.PageWidth - (2 * PDFConstants.Margin);
            double colWidth = tableWidth / 2;
            double yPos = yStart;

            var labelFont = new XFont(PDFConstants.BoldFontFamily, PDFConstants.StudentInfoFontSize);
            var valueFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.StudentInfoFontSize);

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
                gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), tableX, yPos, colWidth, PDFConstants.StudentInfoRowHeight);
                gfx.DrawString(row[0], labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.CellTextLeftPadding, yPos + PDFConstants.CellTextTopPaddingSmall);
                gfx.DrawString(row[1], valueFont, new XSolidBrush(TextDark), tableX + PDFConstants.StudentInfoValueColumnOffset, yPos + PDFConstants.CellTextTopPaddingSmall);

                // Right column
                gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), tableX + colWidth, yPos, colWidth, PDFConstants.StudentInfoRowHeight);
                gfx.DrawString(row[2], labelFont, new XSolidBrush(TextDark), tableX + colWidth + PDFConstants.CellTextLeftPadding, yPos + PDFConstants.CellTextTopPaddingSmall);
                gfx.DrawString(row[3], valueFont, new XSolidBrush(TextDark), tableX + colWidth + PDFConstants.StudentInfoValueColumnOffset, yPos + PDFConstants.CellTextTopPaddingSmall);

                yPos -= PDFConstants.StudentInfoRowHeight;
            }

            return yPos - PDFConstants.StudentInfoTableBottomGap;
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

            double tableX = PDFConstants.Margin;
            double mainTableWidth = LayoutCalculations.MainTableWidth;
            double legendX = tableX + mainTableWidth + PDFConstants.TableLegendGap;
            double yPos = yStart;

            var headerFont = new XFont(PDFConstants.BoldFontFamily, PDFConstants.SubjectsTableFontSize);
            var dataFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.SubjectsTableFontSize);
            var legendFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.LegendFontSize);

            // Main table headers
            string[] headers = { "Subject", "Class\nScore\n(50%)", "Exam\nScore\n(60%)", "Total\nScore\n(100%)", "Grade", "Position", "Remarks" };
            double[] colWidths = {
                PDFConstants.SubjectsTableSubjectColumnWidth,
                PDFConstants.SubjectsTableClassScoreColumnWidth,
                PDFConstants.SubjectsTableExamScoreColumnWidth,
                PDFConstants.SubjectsTableTotalScoreColumnWidth,
                PDFConstants.SubjectsTableGradeColumnWidth,
                PDFConstants.SubjectsTablePositionColumnWidth,
                PDFConstants.SubjectsTableRemarksColumnWidth
            };

            // Draw header row with background
            double cellX = tableX;
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(new XSolidBrush(TableHeaderBg), cellX, yPos, colWidths[i], PDFConstants.TableHeaderHeight);
                gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), cellX, yPos, colWidths[i], PDFConstants.TableHeaderHeight);

                // Multi-line header text
                gfx.DrawString(headers[i], headerFont, new XSolidBrush(TextLight), cellX + PDFConstants.CellTextLeftPadding, yPos + PDFConstants.CellTextLeftPadding,
                    new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });

                cellX += colWidths[i];
            }

            yPos -= PDFConstants.TableHeaderHeight;

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
                XColor rowBg = isAlternateRow ? AlternateRowBg : WhiteBackground;

                for (int i = 0; i < values.Length; i++)
                {
                    gfx.DrawRectangle(new XSolidBrush(rowBg), cellX, yPos, colWidths[i], PDFConstants.SubjectsTableRowHeight);
                    gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), cellX, yPos, colWidths[i], PDFConstants.SubjectsTableRowHeight);
                    gfx.DrawString(values[i], dataFont, new XSolidBrush(TextDark), cellX + PDFConstants.CellTextLeftPadding, yPos + PDFConstants.CellTextTopPaddingLarge);

                    cellX += colWidths[i];
                }

                yPos += PDFConstants.SubjectsTableRowHeight;
            }

            // Draw legend on right side (starting from header row Y)
            double legendY = yStart;
            string[] legendHeaders = { "Score Range", "Grade", "Remarks" };
            double[] legendColWidths = {
                PDFConstants.LegendScoreRangeColumnWidth,
                PDFConstants.LegendGradeColumnWidth,
                PDFConstants.LegendRemarksColumnWidth
            };

            // Legend header
            double legendCellX = legendX;
            for (int i = 0; i < legendHeaders.Length; i++)
            {
                gfx.DrawRectangle(new XSolidBrush(TableHeaderBg), legendCellX, legendY, legendColWidths[i], PDFConstants.TableHeaderHeight);
                gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), legendCellX, legendY, legendColWidths[i], PDFConstants.TableHeaderHeight);
                gfx.DrawString(legendHeaders[i], headerFont, new XSolidBrush(TextLight),
                    legendCellX + PDFConstants.CellTextLeftPadding, legendY + 2, new XStringFormat { Alignment = XStringAlignment.Center });
                legendCellX += legendColWidths[i];
            }

            legendY -= PDFConstants.TableHeaderHeight;

            // Legend data rows
            foreach (var gradeLevel in GradingLevels)
            {
                legendCellX = legendX;
                var legendValues = new[] { gradeLevel.ScoreRange, gradeLevel.Grade, gradeLevel.Remarks };

                for (int i = 0; i < legendValues.Length; i++)
                {
                    gfx.DrawRectangle(new XSolidBrush(WhiteBackground), legendCellX, legendY, legendColWidths[i], PDFConstants.SubjectsTableRowHeight);
                    gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), legendCellX, legendY, legendColWidths[i], PDFConstants.SubjectsTableRowHeight);
                    gfx.DrawString(legendValues[i], legendFont, new XSolidBrush(TextDark),
                        legendCellX + PDFConstants.CellTextLeftPadding, legendY + PDFConstants.CellTextTopPaddingLarge);
                    legendCellX += legendColWidths[i];
                }

                legendY += PDFConstants.SubjectsTableRowHeight;
            }

            return yPos - PDFConstants.SubjectsTableBottomGap;
        }

        /// <summary>
        /// Draws a totals row showing aggregate scores across all subjects.
        /// </summary>
        private double DrawTotalRow(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = PDFConstants.Margin;
            var totalFont = new XFont(PDFConstants.BoldFontFamily, PDFConstants.SubjectsTableFontSize);
            string[] headers = { "Subject", "Class\nScore\n(50%)", "Exam\nScore\n(60%)", "Total\nScore\n(100%)", "Grade", "Position", "Remarks" };
            double[] colWidths = {
                PDFConstants.SubjectsTableSubjectColumnWidth,
                PDFConstants.SubjectsTableClassScoreColumnWidth,
                PDFConstants.SubjectsTableExamScoreColumnWidth,
                PDFConstants.SubjectsTableTotalScoreColumnWidth,
                PDFConstants.SubjectsTableGradeColumnWidth,
                PDFConstants.SubjectsTablePositionColumnWidth,
                PDFConstants.SubjectsTableRemarksColumnWidth
            };

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
                gfx.DrawRectangle(new XSolidBrush(AlternateRowBg), cellX, yStart, colWidths[i], PDFConstants.SubjectsTableRowHeight);
                gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.BoldBorderWidth), cellX, yStart, colWidths[i], PDFConstants.SubjectsTableRowHeight);
                gfx.DrawString(values[i], totalFont, new XSolidBrush(TextDark), cellX + PDFConstants.CellTextLeftPadding, yStart + PDFConstants.CellTextTopPaddingLarge);

                cellX += colWidths[i];
            }

            return yStart + PDFConstants.SubjectsTableRowHeight + PDFConstants.SubjectsTableBottomGap;
        }

        /// <summary>
        /// Draws the remarks section with behavioral indicators and teacher comments.
        /// </summary>
        private double DrawRemarksSection(XGraphics gfx, PdfPage page, ReportCardData data, double yStart)
        {
            double tableX = PDFConstants.Margin;
            double tableWidth = PDFConstants.PageWidth - (2 * PDFConstants.Margin);

            var labelFont = new XFont(PDFConstants.BoldFontFamily, PDFConstants.RemarksFontSize);
            var valueFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.RemarksFontSize);

            // Draw section background
            gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), tableX, yStart, tableWidth, PDFConstants.RemarksTableHeight);

            double yLine = yStart + PDFConstants.RemarksSectionTopPadding;

            // Behavioral section
            gfx.DrawString("BEHAVIORAL INDICATORS", labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.CellTextLeftPadding, yLine);
            yLine += PDFConstants.RemarksLabelSpacing;

            var remarks = data.Remarks;
            gfx.DrawString("Attitude:", labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.CellTextLeftPadding, yLine);
            gfx.DrawString(remarks?.Attitude ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksAttitudeValueX, yLine);

            gfx.DrawString("Interest:", labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksInterestLabelX, yLine);
            gfx.DrawString(remarks?.Interest ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksInterestValueX, yLine);

            gfx.DrawString("Conduct:", labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksConductLabelX, yLine);
            gfx.DrawString(remarks?.Conduct ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksConductValueX, yLine);

            yLine += PDFConstants.RemarksRowSpacing;

            // Teacher remarks
            gfx.DrawString("Class Teacher's Remarks:", labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.CellTextLeftPadding, yLine);
            gfx.DrawString(remarks?.ClassTeacherRemarks ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksValueX, yLine);

            yLine += PDFConstants.RemarksRowSpacing;

            gfx.DrawString("Head Teacher's Remarks:", labelFont, new XSolidBrush(TextDark), tableX + PDFConstants.CellTextLeftPadding, yLine);
            gfx.DrawString(remarks?.HeadTeacherRemarks ?? "Not recorded", valueFont, new XSolidBrush(TextDark), tableX + PDFConstants.RemarksValueX, yLine);

            return yStart - (PDFConstants.RemarksTableHeight + PDFConstants.SubjectsTableBottomGap);
        }

        /// <summary>
        /// Draws signature lines for authorized signatories.
        /// Per specification: School Director (left) | Head Teacher (right)
        /// </summary>
        private double DrawSignatureSection(XGraphics gfx, PdfPage page, double yStart)
        {
            double tableX = PDFConstants.Margin;
            double tableWidth = PDFConstants.PageWidth - (2 * PDFConstants.Margin);
            double colWidth = tableWidth / 2;

            var signatureFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.SignatureLabelFontSize);
            var dateFont = new XFont(PDFConstants.DefaultFontFamily, PDFConstants.SignatureDateFontSize);

            // Draw section background
            gfx.DrawRectangle(new XPen(BorderColor, PDFConstants.StandardBorderWidth), tableX, yStart, tableWidth, PDFConstants.SignatureTableHeight);

            double yLine = yStart + PDFConstants.RemarksSectionTopPadding;

            // Left column: School Director (per specification, not Class Teacher)
            gfx.DrawLine(new XPen(TextDark, PDFConstants.SignatureLineWidth), tableX + PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureLineHeight, tableX + colWidth - PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureLineHeight);
            gfx.DrawString("School Director's Signature", signatureFont, new XSolidBrush(TextDark), tableX + PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureLineLabelOffset);
            gfx.DrawString("Date: __________________", dateFont, new XSolidBrush(TextDark), tableX + PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureDateOffset);

            // Right column: Head Teacher
            gfx.DrawLine(new XPen(TextDark, PDFConstants.SignatureLineWidth), tableX + colWidth + PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureLineHeight, tableX + tableWidth - PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureLineHeight);
            gfx.DrawString("Head Teacher's Signature & Stamp", signatureFont, new XSolidBrush(TextDark), tableX + colWidth + PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureLineLabelOffset);
            gfx.DrawString("Date: __________________", dateFont, new XSolidBrush(TextDark), tableX + colWidth + PDFConstants.SignatureSectionPadding, yLine + PDFConstants.SignatureDateOffset);

            return yStart - (PDFConstants.SignatureTableHeight + PDFConstants.SignatureTableBottomGap);
        }

        /// <summary>
        /// Helper to draw logo placeholder when logo image is unavailable.
        /// </summary>
        private void DrawLogoPlaceholder(XGraphics gfx, double x, double y, double size)
        {
            gfx.DrawRectangle(new XPen(TextLight, PDFConstants.PlaceholderBorderWidth), x, y, size, size);
            gfx.DrawString("LOGO", new XFont(PDFConstants.DefaultFontFamily, PDFConstants.LogoPlaceholderFontSize), new XSolidBrush(TextLight),
                x + PDFConstants.LogoPlaceholderTextOffsetX, y + size / 2 - PDFConstants.LogoPlaceholderTextCenteringOffset);
        }

        /// <summary>
        /// Helper to draw photo placeholder when student photo is unavailable.
        /// </summary>
        private void DrawPhotoPlaceholder(XGraphics gfx, double x, double y, double width, double height)
        {
            gfx.DrawRectangle(new XPen(TextLight, PDFConstants.PlaceholderBorderWidth), x, y, width, height);
            gfx.DrawString("PHOTO", new XFont(PDFConstants.DefaultFontFamily, PDFConstants.PhotoPlaceholderFontSize), new XSolidBrush(TextLight),
                x + PDFConstants.PhotoPlaceholderTextOffsetX, y + height / 2 - PDFConstants.PhotoPlaceholderTextOffsetY);
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
