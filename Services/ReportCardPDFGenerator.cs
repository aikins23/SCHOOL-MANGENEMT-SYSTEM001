using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Generates the official terminal report card layout (school identity from SchoolProfile).
    /// The dimensions are point-based for A4 output and are tuned to match card.png.
    /// </summary>
    public class ReportCardPDFGenerator
    {
        private const double PageWidth = 595;
        private const double PageHeight = 842;
        private const double ReportX = 55;
        private const double ReportY = 58;
        private const double ReportWidth = 485;

        private const double HeaderHeight = 105;
        private const double InfoRowHeight = 23;
        private const double SubjectHeaderHeight = 42;
        private const double SubjectRowHeight = 23;
        private const double RemarksRowHeight = 23;
        private const double PromotedRowHeight = 22;
        private const double SignatureHeight = 48;
        private const double BottomLegendHeight = 45;
        private const double InfoLabelFontSize = 7.8;
        private const double InfoValueFontSize = 8.0;
        private const double InfoSmallValueFontSize = 7.0;

        // Brand colours come from the configurable School Information settings (SchoolProfile),
        // honouring the transient preview override. Black/white stay fixed.
        private static XColor Primary   => ToX(Common.SchoolProfile.ReportPrimaryColor);
        private static XColor Secondary => ToX(Common.SchoolProfile.ReportSecondaryColor);
        private static XColor Accent    => ToX(Common.SchoolProfile.ReportAccentColor);
        private static readonly XColor Black = XColors.Black;
        private static readonly XColor White = XColors.White;

        private static XColor ToX(System.Drawing.Color c) => XColor.FromArgb(c.A, c.R, c.G, c.B);

        private static XPen Border(double width = 0.7) => new XPen(Black, width);
        private static XSolidBrush Brush(XColor color) => new XSolidBrush(color);
        private static XFont Font(double size, bool bold = false) =>
            new XFont("Arial", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);

        // Built from the configurable grading scheme. The displayed score range is derived
        // from consecutive MinScores: top band "{min}+", middle "{min}-{nextHigherMin-1}",
        // floor band "0-{nextHigherMin-1}".
        private static GradeLevel[] GradeLevels => BuildGradeLevels();

        private static GradeLevel[] BuildGradeLevels()
        {
            var bands = Common.GradingScheme.Bands; // high -> low
            var levels = new GradeLevel[bands.Count];
            for (int i = 0; i < bands.Count; i++)
            {
                int min = bands[i].MinScore;
                string range;
                if (i == 0) range = min + "+";
                else range = min + "-" + (bands[i - 1].MinScore - 1);
                levels[i] = new GradeLevel(range, bands[i].Code, bands[i].Label);
            }
            return levels;
        }

        public async Task<byte[]> GeneratePDFAsync(ReportCardData data)
        {
            return await Task.Run(() =>
            {
                using (var document = new PdfDocument())
                {
                    document.Info.Title = $"Report Card - {data?.StudentName ?? "Student"}";

                    var page = document.AddPage();
                    page.Width = XUnit.FromPoint(PageWidth);
                    page.Height = XUnit.FromPoint(PageHeight);

                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        try
                        {
                            double y = ReportY;
                            y = DrawHeader(gfx, y, data);
                            y = DrawStudentInfo(gfx, y, data);
                            y = DrawSubjectsAndGrading(gfx, y, data);
                            y = DrawRemarks(gfx, y, data);
                            y = DrawSignatures(gfx, y);
                            y = DrawBottomGradingScale(gfx, y);

                            gfx.DrawRectangle(Border(1.2), ReportX, ReportY, ReportWidth, y - ReportY);
                            Common.PrintBranding.DrawPdfFooter(gfx, PageWidth, PageHeight);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.LogError("Error generating report card PDF", ex);
                            throw new PDFGenerationException("Error generating report card PDF", ex);
                        }
                    }

                    if (data?.Billing != null)
                    {
                        var billPage = document.AddPage();
                        billPage.Width = XUnit.FromPoint(PageWidth);
                        billPage.Height = XUnit.FromPoint(PageHeight);
                        using (var billGfx = XGraphics.FromPdfPage(billPage))
                        {
                            DrawBillingSheet(billGfx, data);
                            Common.PrintBranding.DrawPdfFooter(billGfx, PageWidth, PageHeight);
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        document.Save(stream, false);
                        return stream.ToArray();
                    }
                }
            });
        }

        private void DrawBillingSheet(XGraphics gfx, ReportCardData data)
        {
            var bill = data.Billing;
            var navy = Primary;
            var gold = Accent;
            var muted = ToX(UiTheme.Muted);
            var text = ToX(UiTheme.Text);

            gfx.DrawRectangle(Brush(navy), 0, 0, PageWidth, 96);
            gfx.DrawString(Common.SchoolProfile.DisplayName, Font(20, true), Brush(White), new XRect(48, 24, 500, 28), XStringFormats.TopLeft);
            gfx.DrawString("Student Fee Bill Sheet", Font(10, true), Brush(gold), new XRect(48, 58, 500, 20), XStringFormats.TopLeft);

            double y = 132;
            gfx.DrawString(data.StudentName ?? "", Font(16, true), Brush(text), 48, y);
            y += 24;
            gfx.DrawString($"Student ID: {data.StudentID}    Class: {data.ClassID}    Term: {bill.TermName}", Font(10), Brush(muted), 48, y);
            y += 42;

            DrawBillRow(gfx, y, "Previous balance brought forward", bill.PreviousBalance, false); y += 42;
            DrawBillRow(gfx, y, "Current term school fees", bill.CurrentTermFee, false); y += 42;
            DrawBillRow(gfx, y, "Total amount expected", bill.TotalExpected, true); y += 42;
            DrawBillRow(gfx, y, "Payments received", bill.AmountPaid, false); y += 42;
            DrawBillRow(gfx, y, "Outstanding balance", bill.Balance, true); y += 56;

            gfx.DrawString(
                "This bill sheet is attached for parent/guardian clarity. Payments are recorded against the student account; previous debt is automatically carried forward when a term is closed.",
                Font(9),
                Brush(muted),
                new XRect(48, y, 500, 54),
                XStringFormats.TopLeft);

            gfx.DrawLine(new XPen(navy, 1), 48, 760, 547, 760);
            gfx.DrawString($"Generated: {DateTime.Now:dd MMM yyyy, h:mm tt}", Font(9), Brush(muted), new XRect(48, 774, 500, 18), XStringFormats.TopLeft);
        }

        private void DrawBillRow(XGraphics gfx, double y, string label, decimal amount, bool emphasis)
        {
            var fill = emphasis ? UiTheme.GoldSoft : UiTheme.SurfaceAlt;
            var text = ToX(UiTheme.Text);
            var muted = ToX(UiTheme.Muted);
            gfx.DrawRectangle(new XSolidBrush(ToX(fill)), 48, y - 20, 499, 32);
            gfx.DrawString(label, Font(emphasis ? 10 : 9.5, emphasis), Brush(emphasis ? text : muted), new XRect(64, y - 13, 310, 18), XStringFormats.TopLeft);
            gfx.DrawString($"GHS {amount:N2}", Font(10.5, true), Brush(text), new XRect(378, y - 13, 150, 18), XStringFormats.TopRight);
        }

        private double DrawHeader(XGraphics gfx, double y, ReportCardData data)
        {
            DrawFilledCell(gfx, ReportX, y, ReportWidth, HeaderHeight, Primary, 1.2);

            double logoSize = 76;
            double logoX = ReportX + 25;
            double logoY = y + 10;
            DrawImageOrLogoPlaceholder(gfx, data?.SchoolInfo?.Logo, logoX, logoY, logoSize, logoSize);

            double photoW = 86;
            double photoH = 78;
            double photoX = ReportX + ReportWidth - photoW - 16;
            double photoY = y + 8;
            DrawImageOrPhotoPlaceholder(gfx, data?.ProfilePhoto, photoX, photoY, photoW, photoH);

            double textX = logoX + logoSize + 18;
            double textW = photoX - textX - 10;
            // Fall back to the configured School Information (SchoolProfile) rather than a hardcoded
            // school identity, so another school never shows the wrong details.
            string schoolName = Coalesce(data?.SchoolInfo?.Name, Common.SchoolProfile.Name);
            string location   = Coalesce(data?.SchoolInfo?.Location, Common.SchoolProfile.Address);
            string phone      = Coalesce(data?.SchoolInfo?.PhoneNumbers, Common.SchoolProfile.Phones);

            CenterText(gfx, schoolName, textX, y + 30, textW, Font(13, true), White);
            CenterText(gfx, location, textX, y + 48, textW, Font(11, true), White);
            CenterText(gfx, phone, textX, y + 64, textW, Font(10, true), White);
            CenterText(gfx, "STUDENT TERMINAL REPORT", textX, y + 82, textW, Font(12, true), White);

            return y + HeaderHeight;
        }

        private double DrawStudentInfo(XGraphics gfx, double y, ReportCardData data)
        {
            double leftPairW = ReportWidth * 0.59;
            double rightPairW = ReportWidth - leftPairW;
            double leftLabelW = 82;
            double rightLabelW = 78;
            double rightValueW = rightPairW - rightLabelW;
            var rows = BuildInfoRows(data).ToList();
            var labelFont = Font(InfoLabelFontSize, true);
            var valueFont = Font(InfoValueFontSize);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                double rowY = y + (i * InfoRowHeight);

                DrawCell(gfx, ReportX, rowY, leftLabelW, InfoRowHeight);
                DrawLeftText(gfx, row.LeftLabel, ReportX, rowY, leftLabelW, InfoRowHeight, labelFont);

                DrawCell(gfx, ReportX + leftLabelW, rowY, leftPairW - leftLabelW, InfoRowHeight);
                CenterTextInCell(gfx, row.LeftValue, ReportX + leftLabelW, rowY, leftPairW - leftLabelW, InfoRowHeight, valueFont);

                DrawCell(gfx, ReportX + leftPairW, rowY, rightLabelW, InfoRowHeight);
                DrawLeftText(gfx, row.RightLabel, ReportX + leftPairW, rowY, rightLabelW, InfoRowHeight, labelFont);

                DrawCell(gfx, ReportX + leftPairW + rightLabelW, rowY, rightValueW, InfoRowHeight);
                DrawInfoRightValue(gfx, row, ReportX + leftPairW + rightLabelW, rowY, rightValueW);
            }

            return y + rows.Count * InfoRowHeight;
        }

        private IEnumerable<InfoRow> BuildInfoRows(ReportCardData data)
        {
            string academicYear = string.IsNullOrWhiteSpace(data?.Year) ? CurrentAcademicYearLabel() : data.Year;
            string term = string.IsNullOrWhiteSpace(data?.Term) ? "TERM 3" : data.Term.ToUpperInvariant();

            yield return new InfoRow("Student Name:", data?.StudentName ?? "", "Resuming Date:", FormatReportDate(data?.TermReopeningDate));
            yield return new InfoRow("Admission No.:", Common.StudentId.Display(data?.StudentID), "Attendance:", "",
                data?.PresentDays > 0 ? data.PresentDays.ToString() : "",
                data?.TotalSchoolDays > 0 ? data.TotalSchoolDays.ToString() : "");
            yield return new InfoRow("Class/Form:", data?.ClassID ?? "", "Number On Roll:", data?.TotalStudentsInClass > 0 ? data.TotalStudentsInClass.ToString() : "");
            yield return new InfoRow("Gender:", data?.Gender?.ToUpperInvariant() ?? "", "Position in Class:", FormatPosition(data?.OverallPosition ?? 0));
            yield return new InfoRow("Term:", term, "Average Score:", FormatScore(GetAverageTotal(data)));
            yield return new InfoRow("Closing Date:", FormatReportDate(data?.TermClosingDate), "Academic Year", academicYear);
        }

        private static string CurrentAcademicYearLabel()
        {
            int year = DateTime.Today.Year;
            return $"{year}/{year + 1}";
        }

        private static string FormatReportDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dddd, dd MMMM yyyy").ToUpperInvariant() : "Not set";
        }

        private void DrawInfoRightValue(XGraphics gfx, InfoRow row, double x, double y, double width)
        {
            if (row.RightLabel.StartsWith("Attendance", StringComparison.OrdinalIgnoreCase))
            {
                CenterTextInCell(gfx, row.AttendancePresent, x, y, width * 0.38, InfoRowHeight, Font(InfoValueFontSize, true));
                DrawCell(gfx, x + width * 0.38, y, width * 0.28, InfoRowHeight);
                CenterTextInCell(gfx, "Out of", x + width * 0.38, y, width * 0.28, InfoRowHeight, Font(InfoSmallValueFontSize, true));
                DrawCell(gfx, x + width * 0.66, y, width * 0.34, InfoRowHeight);
                CenterTextInCell(gfx, row.AttendanceTotal, x + width * 0.66, y, width * 0.34, InfoRowHeight, Font(InfoValueFontSize, true));
                return;
            }

            var font = row.RightLabel == "Academic Year" || row.RightLabel.StartsWith("Number", StringComparison.OrdinalIgnoreCase)
                ? Font(InfoValueFontSize, true)
                : Font(InfoValueFontSize);
            CenterTextInCell(gfx, row.RightValue, x, y, width, InfoRowHeight, font);
        }

        private double DrawSubjectsAndGrading(XGraphics gfx, double y, ReportCardData data)
        {
            var subjects = GetSubjectRows(data).ToList();
            double leftTableW = ReportWidth * 0.72;
            double gradingW = ReportWidth - leftTableW;

            double[] widths =
            {
                leftTableW * 0.270,   // Subjects
                leftTableW * 0.110,   // Class Score
                leftTableW * 0.110,   // Exam Score
                leftTableW * 0.110,   // Total Score
                leftTableW * 0.085,   // Grade
                leftTableW * 0.130,   // Position Per Subject
                leftTableW * 0.185    // Remarks (was 0.110 — widened to fit "Outstanding")
            };

            string[] headers =
            {
                "Subjects",
                "Class\nScore\n(50%)",
                "Exam\nScore\n(50%)",
                "Total\nScore\n(100%)",
                "Grade",
                "Position\nPer\nSubject",
                "Remarks"
            };

            double x = ReportX;
            for (int i = 0; i < headers.Length; i++)
            {
                DrawFilledCell(gfx, x, y, widths[i], SubjectHeaderHeight, Secondary);
                DrawMultilineCenter(gfx, headers[i], x, y, widths[i], SubjectHeaderHeight, Font(7, true), Black);
                x += widths[i];
            }

            double gradingX = ReportX + leftTableW;
            DrawFilledCell(gfx, gradingX, y, gradingW, SubjectHeaderHeight, Primary);
            CenterTextInCell(gfx, "Grading System", gradingX, y, gradingW, SubjectHeaderHeight, Font(8, true), Accent);

            double rowY = y + SubjectHeaderHeight;
            for (int i = 0; i < subjects.Count; i++)
            {
                DrawSubjectRow(gfx, subjects[i], ReportX, rowY, widths, false);
                DrawGradingSideRow(gfx, gradingX, rowY, gradingW, i);
                rowY += SubjectRowHeight;
            }

            var total = BuildTotalRow(subjects);
            DrawSubjectRow(gfx, total, ReportX, rowY, widths, true);
            DrawFilledCell(gfx, gradingX, rowY, gradingW, SubjectRowHeight, Secondary);

            return rowY + SubjectRowHeight;
        }

        private void DrawSubjectRow(XGraphics gfx, SubjectDisplayRow row, double x, double y, double[] widths, bool total)
        {
            string[] values =
            {
                row.Subject,
                row.ClassScore,
                row.ExamScore,
                row.TotalScore,
                row.Grade,
                row.Position,
                row.Remark
            };

            var font = Font(7.6, total || row.Subject.Length > 0);
            XColor fill = total ? Secondary : White;
            double cellX = x;
            for (int i = 0; i < values.Length; i++)
            {
                DrawFilledCell(gfx, cellX, y, widths[i], SubjectRowHeight, fill);
                if (i == 0)
                    DrawWrappedLeftText(gfx, values[i], cellX, y, widths[i], SubjectRowHeight, font);
                else if (i == 6)
                    DrawWrappedCenterText(gfx, values[i], cellX, y, widths[i], SubjectRowHeight, font);
                else
                    CenterTextInCell(gfx, values[i], cellX, y, widths[i], SubjectRowHeight, font);
                cellX += widths[i];
            }
        }

        private void DrawGradingSideRow(XGraphics gfx, double x, double y, double width, int rowIndex)
        {
            double[] widths = { width * 0.30, width * 0.18, width * 0.52 };

            if (rowIndex == 0)
            {
                string[] headers = { "Score", "Grade", "Remarks" };
                double cellX = x;
                for (int i = 0; i < headers.Length; i++)
                {
                    DrawFilledCell(gfx, cellX, y, widths[i], SubjectRowHeight, Secondary);
                    CenterTextInCell(gfx, headers[i], cellX, y, widths[i], SubjectRowHeight, Font(6.2, true));
                    cellX += widths[i];
                }
                return;
            }

            int gradeIndex = rowIndex - 1;
            if (gradeIndex >= GradeLevels.Length)
            {
                DrawCell(gfx, x, y, width, SubjectRowHeight);
                return;
            }

            var grade = GradeLevels[gradeIndex];
            string[] values = { grade.ScoreRange, grade.Grade, grade.Remarks };
            double nextX = x;
            for (int i = 0; i < values.Length; i++)
            {
                DrawCell(gfx, nextX, y, widths[i], SubjectRowHeight);
                var text = i == 2 ? WrapGradingLabel(values[i]) : values[i];
                var font = i == 2 ? Font(5.2) : Font(5.8);
                DrawMultilineCenter(gfx, text, nextX, y, widths[i], SubjectRowHeight, font, Black);
                nextX += widths[i];
            }
        }

        private static string WrapGradingLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            return value
                .Replace("Approaching Proficiency", "Approaching\nProficiency")
                .Replace("Advance(A)", "Advance (A)")
                .Replace("Proficiency(P)", "Proficiency (P)")
                .Replace("Developing(D)", "Developing (D)")
                .Replace("Beginning(B)", "Beginning (B)");
        }

        private IEnumerable<SubjectDisplayRow> GetSubjectRows(ReportCardData data)
        {
            var source = data?.SubjectResults ?? new List<SubjectResult>();
            foreach (var subject in source)
            {
                yield return new SubjectDisplayRow
                {
                    Subject = subject.Subject ?? "",
                    ClassScore = FormatScore(subject.ClassScore),
                    ExamScore = FormatScore(subject.ExamScore),
                    TotalScore = FormatScore(subject.TotalScore),
                    Grade = string.IsNullOrWhiteSpace(subject.Grade) ? GetGradeForScore(subject.TotalScore) : subject.Grade,
                    Position = FormatPosition(subject.PositionInClass),
                    Remark = string.IsNullOrWhiteSpace(subject.Remark) ? GetRemarkForScore(subject.TotalScore) : subject.Remark
                };
            }
        }

        private SubjectDisplayRow BuildTotalRow(IReadOnlyCollection<SubjectDisplayRow> subjects)
        {
            decimal classTotal = subjects.Sum(s => ParseDecimal(s.ClassScore));
            decimal examTotal = subjects.Sum(s => ParseDecimal(s.ExamScore));
            decimal total = subjects.Sum(s => ParseDecimal(s.TotalScore));
            int gradeTotal = subjects.Sum(s => int.TryParse(s.Grade, out int grade) ? grade : 0);

            return new SubjectDisplayRow
            {
                Subject = "Total",
                ClassScore = FormatScore(classTotal),
                ExamScore = FormatScore(examTotal),
                TotalScore = FormatScore(total),
                Grade = gradeTotal > 0 ? gradeTotal.ToString() : "",
                Position = "",
                Remark = ""
            };
        }

        private double DrawRemarks(XGraphics gfx, double y, ReportCardData data)
        {
            var remarks = data?.Remarks;
            var rows = new[]
            {
                Tuple.Create("Attitude:", remarks?.Attitude ?? ""),
                Tuple.Create("Interest:", remarks?.Interest ?? ""),
                Tuple.Create("Conduct:", remarks?.Conduct ?? ""),
                Tuple.Create("Class Teacher's Remarks:", remarks?.ClassTeacherRemarks ?? ""),
                Tuple.Create("Head Teacher's Remarks:", remarks?.HeadTeacherRemarks ?? "")
            };

            double labelW = ReportWidth * 0.34;
            for (int i = 0; i < rows.Length; i++)
            {
                double rowY = y + i * RemarksRowHeight;
                DrawCell(gfx, ReportX, rowY, labelW, RemarksRowHeight);
                DrawLeftText(gfx, rows[i].Item1, ReportX, rowY, labelW, RemarksRowHeight, Font(7.8, true));
                DrawCell(gfx, ReportX + labelW, rowY, ReportWidth - labelW, RemarksRowHeight);
                DrawLeftText(gfx, rows[i].Item2, ReportX + labelW, rowY, ReportWidth - labelW, RemarksRowHeight, Font(7.8));
            }

            double promotedY = y + rows.Length * RemarksRowHeight;
            DrawCell(gfx, ReportX, promotedY, ReportWidth, PromotedRowHeight);
            CenterTextInCell(gfx, "Promoted to:", ReportX, promotedY, ReportWidth, PromotedRowHeight, Font(8.3, true));

            return promotedY + PromotedRowHeight;
        }

        private double DrawSignatures(XGraphics gfx, double y)
        {
            double halfW = ReportWidth / 2;
            DrawCell(gfx, ReportX, y, halfW, SignatureHeight);
            DrawCell(gfx, ReportX + halfW, y, halfW, SignatureHeight);

            DrawSignatureStroke(gfx, ReportX + 92, y + 9);
            DrawSignatureStroke(gfx, ReportX + halfW + 92, y + 9);

            CenterTextInCell(gfx, "School Director's Signature", ReportX, y + SignatureHeight - 16, halfW, 16, Font(8, true));
            CenterTextInCell(gfx, "Head Teacher's Signature & Stamp", ReportX + halfW, y + SignatureHeight - 16, halfW, 16, Font(8, true));

            return y + SignatureHeight;
        }

        private double DrawBottomGradingScale(XGraphics gfx, double y)
        {
            double labelW = 84;
            double gridW = ReportWidth - labelW;
            DrawFilledCell(gfx, ReportX, y, labelW, BottomLegendHeight, Primary);
            CenterTextInCell(gfx, "Grading System", ReportX, y, labelW, BottomLegendHeight, Font(7, true), Accent);

            double labelColW = 36;
            double levelW = (gridW - labelColW) / GradeLevels.Length;
            double rowH = BottomLegendHeight / 3;
            string[] labels = { "Score", "Grade", "Remarks" };

            for (int r = 0; r < labels.Length; r++)
            {
                double rowY = y + r * rowH;
                DrawFilledCell(gfx, ReportX + labelW, rowY, labelColW, rowH, Secondary);
                CenterTextInCell(gfx, labels[r], ReportX + labelW, rowY, labelColW, rowH, Font(5.5, true));

                for (int i = 0; i < GradeLevels.Length; i++)
                {
                    string value = r == 0 ? GradeLevels[i].ScoreRange
                        : r == 1 ? GradeLevels[i].Grade
                        : GradeLevels[i].Remarks.Replace("\n", " ");
                    double cellX = ReportX + labelW + labelColW + i * levelW;
                    DrawCell(gfx, cellX, rowY, levelW, rowH);
                    CenterTextInCell(gfx, value, cellX, rowY, levelW, rowH, Font(5.2));
                }
            }

            return y + BottomLegendHeight;
        }

        private static string Coalesce(params string[] values)
        {
            if (values != null)
                foreach (var v in values)
                    if (!string.IsNullOrWhiteSpace(v))
                        return v.Trim();
            return "";
        }

        private void DrawImageOrLogoPlaceholder(XGraphics gfx, byte[] imageBytes, double x, double y, double width, double height)
        {
            if (TryDrawImage(gfx, imageBytes, x, y, width, height))
                return;

            string logoPath = FindResource("school_logo.png") ?? FindResource("school logo.png");
            if (!string.IsNullOrEmpty(logoPath) && TryDrawImage(gfx, File.ReadAllBytes(logoPath), x, y, width, height))
                return;

            // Logo-absent placeholder: show only the configurable abbreviation. The header beside it
            // already prints the full school name, location and phone from School Information, so we
            // avoid any hardcoded school motto/town that would be wrong for another school.
            DrawCell(gfx, x, y, width, height, Accent, 1.2);
            CenterTextInCell(gfx, Common.StudentId.Abbrev, x, y + (height - 18) / 2, width, 18, Font(16, true), Accent);
        }

        private void DrawImageOrPhotoPlaceholder(XGraphics gfx, byte[] imageBytes, double x, double y, double width, double height)
        {
            if (TryDrawImage(gfx, imageBytes, x, y, width, height))
                return;

            DrawCell(gfx, x, y, width, height, XColor.FromArgb(210, 210, 210), 1.0);
            CenterTextInCell(gfx, "PHOTO", x, y, width, height, Font(7), XColor.FromArgb(120, 120, 120));
        }

        private bool TryDrawImage(XGraphics gfx, byte[] imageBytes, double x, double y, double width, double height)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return false;

            try
            {
                // PdfSharp's XImage.FromStream calls GetBuffer() on the stream, which
                // throws for a MemoryStream created from a byte[] (non-publicly-visible
                // buffer). Pass publiclyVisible: true so the logo/photo actually decode
                // instead of silently falling back to the placeholder.
                using (var stream = new MemoryStream(imageBytes, 0, imageBytes.Length, writable: false, publiclyVisible: true))
                using (var image = XImage.FromStream(stream))
                {
                    gfx.DrawImage(image, x, y, width, height);
                    gfx.DrawRectangle(Border(0.9), x, y, width, height);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning($"Could not draw report image: {ex.Message}");
                return false;
            }
        }

        private string FindResource(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "Resources", fileName),
                Path.Combine(baseDir, "..", "..", "Resources", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName)
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private void DrawSignatureStroke(XGraphics gfx, double x, double y)
        {
            var pen = new XPen(Black, 1.0);
            gfx.DrawBezier(pen, x - 28, y + 24, x - 5, y + 6, x + 6, y + 36, x + 28, y + 16);
            gfx.DrawBezier(pen, x - 34, y + 30, x - 8, y + 14, x + 8, y + 28, x + 34, y + 20);
            gfx.DrawLine(pen, x - 18, y + 33, x + 7, y - 2);
            gfx.DrawLine(pen, x - 5, y + 33, x + 17, y - 5);
        }

        private void DrawFilledCell(XGraphics gfx, double x, double y, double width, double height, XColor fill, double borderWidth = 0.7)
        {
            gfx.DrawRectangle(Brush(fill), x, y, width, height);
            gfx.DrawRectangle(Border(borderWidth), x, y, width, height);
        }

        private void DrawCell(XGraphics gfx, double x, double y, double width, double height, XColor? borderColor = null, double borderWidth = 0.7)
        {
            gfx.DrawRectangle(new XPen(borderColor ?? Black, borderWidth), x, y, width, height);
        }

        private void CenterText(XGraphics gfx, string text, double x, double y, double width, XFont font, XColor? color = null)
        {
            gfx.DrawString(text ?? "", font, Brush(color ?? Black),
                new XRect(x, y - font.Size, width, font.Size + 2),
                new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });
        }

        private void CenterTextInCell(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
        {
            gfx.DrawString(text ?? "", font, Brush(color ?? Black), new XRect(x + 1, y, width - 2, height),
                new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });
        }

        private void DrawLeftText(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
        {
            gfx.DrawString(text ?? "", font, Brush(color ?? Black), new XRect(x + 3, y, width - 5, height),
                new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
        }

        private void DrawWrappedLeftText(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
        {
            DrawWrappedText(gfx, text, x + 3, y, width - 6, height, font, XStringAlignment.Near, color);
        }

        private void DrawWrappedCenterText(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
        {
            DrawWrappedText(gfx, text, x + 2, y, width - 4, height, font, XStringAlignment.Center, color);
        }

        private void DrawWrappedText(XGraphics gfx, string text, double x, double y, double width, double height,
            XFont font, XStringAlignment alignment, XColor? color = null)
        {
            var lines = WrapText(gfx, text ?? "", font, Math.Max(8, width), 2);
            double lineHeight = font.Size + 1.2;
            double startY = y + (height - (lines.Count * lineHeight)) / 2;

            for (int i = 0; i < lines.Count; i++)
            {
                gfx.DrawString(lines[i], font, Brush(color ?? Black),
                    new XRect(x, startY + i * lineHeight, width, lineHeight),
                    new XStringFormat { Alignment = alignment, LineAlignment = XLineAlignment.Center });
            }
        }

        private static List<string> WrapText(XGraphics gfx, string text, XFont font, double width, int maxLines)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                result.Add("");
                return result;
            }

            string normalized = string.Join(" ", text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            foreach (var word in normalized.Split(' '))
            {
                if (result.Count == 0)
                {
                    AppendFittingWord(gfx, result, word, font, width);
                    continue;
                }

                string current = result[result.Count - 1];
                string candidate = string.IsNullOrWhiteSpace(current) ? word : current + " " + word;
                if (gfx.MeasureString(candidate, font).Width <= width)
                {
                    result[result.Count - 1] = candidate;
                }
                else
                {
                    AppendFittingWord(gfx, result, word, font, width);
                }

                if (result.Count > maxLines)
                    break;
            }

            if (result.Count > maxLines)
            {
                result = result.Take(maxLines).ToList();
            }

            if (result.Count == maxLines && gfx.MeasureString(result[maxLines - 1], font).Width > width)
            {
                result[maxLines - 1] = TrimToWidth(gfx, result[maxLines - 1], font, width);
            }

            if (result.Count == maxLines && normalized.Length > string.Join(" ", result).Length)
            {
                result[maxLines - 1] = TrimToWidth(gfx, result[maxLines - 1] + "...", font, width);
            }

            return result;
        }

        private static void AppendFittingWord(XGraphics gfx, List<string> result, string word, XFont font, double width)
        {
            if (gfx.MeasureString(word, font).Width <= width)
            {
                result.Add(word);
                return;
            }

            string remaining = word;
            while (remaining.Length > 0)
            {
                string part = TrimToWidth(gfx, remaining, font, width);
                if (string.IsNullOrEmpty(part))
                    break;

                result.Add(part);
                remaining = remaining.Substring(part.Length);
            }
        }

        private static string TrimToWidth(XGraphics gfx, string value, XFont font, double width)
        {
            string text = value ?? "";
            while (text.Length > 0 && gfx.MeasureString(text, font).Width > width)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text;
        }

        private void DrawMultilineCenter(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
        {
            string[] lines = (text ?? "").Split(new[] { '\n' }, StringSplitOptions.None);
            double lineHeight = font.Size + 1.5;
            double startY = y + (height - (lines.Length * lineHeight)) / 2;

            for (int i = 0; i < lines.Length; i++)
            {
                gfx.DrawString(lines[i], font, Brush(color ?? Black),
                    new XRect(x + 1, startY + i * lineHeight, width - 2, lineHeight),
                    new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });
            }
        }

        private decimal GetAverageTotal(ReportCardData data)
        {
            if (data?.SubjectResults == null || data.SubjectResults.Count == 0)
                return 0m;
            return Math.Round(data.SubjectResults.Average(s => s.TotalScore), 2, MidpointRounding.AwayFromZero);
        }

        private string FormatScore(decimal score)
        {
            return Math.Round(score, 2, MidpointRounding.AwayFromZero).ToString("0.00");
        }

        private decimal ParseDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0m;
        }

        private string FormatPosition(int position)
        {
            if (position <= 0)
                return "";

            string suffix = "th";
            if (position % 100 != 11 && position % 10 == 1) suffix = "st";
            else if (position % 100 != 12 && position % 10 == 2) suffix = "nd";
            else if (position % 100 != 13 && position % 10 == 3) suffix = "rd";

            return $"{position}{suffix}";
        }

        private string GetGradeForScore(decimal score)
        {
            return Common.GradingScheme.CodeForScore(score);
        }

        private string GetRemarkForScore(decimal score)
        {
            return Common.GradingScheme.LabelForScore(score);
        }

        private sealed class InfoRow
        {
            public InfoRow(string leftLabel, string leftValue, string rightLabel, string rightValue,
                string attendancePresent = "", string attendanceTotal = "")
            {
                LeftLabel = leftLabel;
                LeftValue = leftValue;
                RightLabel = rightLabel;
                RightValue = rightValue;
                AttendancePresent = attendancePresent;
                AttendanceTotal = attendanceTotal;
            }

            public string LeftLabel { get; }
            public string LeftValue { get; }
            public string RightLabel { get; }
            public string RightValue { get; }
            public string AttendancePresent { get; }
            public string AttendanceTotal { get; }
        }

        private sealed class SubjectDisplayRow
        {
            public string Subject { get; set; }
            public string ClassScore { get; set; }
            public string ExamScore { get; set; }
            public string TotalScore { get; set; }
            public string Grade { get; set; }
            public string Position { get; set; }
            public string Remark { get; set; }
        }

        private sealed class GradeLevel
        {
            public GradeLevel(string scoreRange, string grade, string remarks)
            {
                ScoreRange = scoreRange;
                Grade = grade;
                Remarks = remarks;
            }

            public string ScoreRange { get; }
            public string Grade { get; }
            public string Remarks { get; }
        }
    }

    public class PDFGenerationException : Exception
    {
        public PDFGenerationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
