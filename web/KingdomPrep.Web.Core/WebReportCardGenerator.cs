using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace KingdomPrep.Web.Core;

public class ReportCardStudentDto
{
    public string StudentId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string HeadTeacherRemark { get; set; } = "";
    public string TeacherRemark { get; set; } = "";
    public string Attitude { get; set; } = "";
    public string Interest { get; set; } = "";
    public string Conduct { get; set; } = "";
    public int PresentDays { get; set; } = 0;
    public int TotalSchoolDays { get; set; } = 0;
    public DateTime? TermClosingDate { get; set; }
    public DateTime? TermReopeningDate { get; set; }
    public int OverallPosition { get; set; } = 0;
    public int TotalStudentsInClass { get; set; } = 0;
    public string Gender { get; set; } = "";
    public List<ReportCardGradeDto> Grades { get; set; } = new();
}

public class ReportCardGradeDto
{
    public string Subject { get; set; } = "";
    public decimal CategoryTotal { get; set; }
    public decimal ExamScore { get; set; }
    public decimal TotalScore { get; set; }
    public string Grade { get; set; } = "";
    public string Remark { get; set; } = "";
    public int PositionInClass { get; set; } = 0;
}

public class WebSchoolProfileDto
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string PhoneNumbers { get; set; } = "";
    public byte[]? Logo { get; set; }
}

public class WebReportCardGenerator
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

    private static readonly XColor Primary = XColor.FromArgb(255, 10, 42, 92); // Dark Navy
    private static readonly XColor Secondary = XColor.FromArgb(255, 235, 240, 245); // Soft Blue Gray
    private static readonly XColor Accent = XColor.FromArgb(255, 218, 165, 32); // Goldenrod
    private static readonly XColor Black = XColors.Black;
    private static readonly XColor White = XColors.White;
    private const string BrandFooterText = "Nyansapo School ERP | darktechhub2@gmail.com | +233 54 836 9261 / +233 20 493 9571 | Accra, Ghana";

    private static XPen Border(double width = 0.7) => new XPen(Black, width);
    private static XSolidBrush Brush(XColor color) => new XSolidBrush(color);
    private static XFont Font(double size, bool bold = false) =>
        new XFont("Arial", size, bold ? XFontStyle.Bold : XFontStyle.Regular);

    private sealed class GradeLevel
    {
        public string ScoreRange { get; }
        public string Grade { get; }
        public string Remarks { get; }
        public GradeLevel(string range, string grade, string remarks) { ScoreRange = range; Grade = grade; Remarks = remarks; }
    }

    private static readonly GradeLevel[] GradeLevels = new[]
    {
        new GradeLevel("80-100", "1", "Highest"),
        new GradeLevel("70-79", "2", "Higher"),
        new GradeLevel("60-69", "3", "High"),
        new GradeLevel("50-59", "4", "High Average"),
        new GradeLevel("40-49", "5", "Average"),
        new GradeLevel("0-39", "6", "Low")
    };

    public byte[] GenerateBatchReportCards(string classId, string term, string yearStr, List<ReportCardStudentDto> students, WebSchoolProfileDto? schoolProfile = null)
    {
        schoolProfile ??= new WebSchoolProfileDto();
        using var document = new PdfDocument();
        document.Info.Title = $"Report Cards - {classId}";

        foreach (var data in students)
        {
            if (data.Grades.Count == 0) continue;

            var page = document.AddPage();
            page.Width = XUnit.FromPoint(PageWidth);
            page.Height = XUnit.FromPoint(PageHeight);

            using var gfx = XGraphics.FromPdfPage(page);

            double y = ReportY;
            y = DrawHeader(gfx, y, classId, term, yearStr, data, schoolProfile);
            y = DrawStudentInfo(gfx, y, classId, term, yearStr, data);
            y = DrawSubjectsAndGrading(gfx, y, data);
            y = DrawRemarks(gfx, y, data);
            y = DrawSignatures(gfx, y);
            y = DrawBottomGradingScale(gfx, y);

            gfx.DrawRectangle(Border(1.2), ReportX, ReportY, ReportWidth, y - ReportY);
            DrawBrandFooter(gfx);
        }

        if (document.PageCount == 0)
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawString($"No grades found for {classId} in {term} {yearStr}.", new XFont("Arial", 14), XBrushes.Gray, new XRect(0, 0, page.Width, page.Height), XStringFormats.Center);
            DrawBrandFooter(gfx);
        }

        using var ms = new MemoryStream();
        document.Save(ms, false);
        return ms.ToArray();
    }

    private double DrawHeader(XGraphics gfx, double y, string classId, string term, string yearStr, ReportCardStudentDto data, WebSchoolProfileDto schoolProfile)
    {
        DrawFilledCell(gfx, ReportX, y, ReportWidth, HeaderHeight, Primary, 1.2);

        double logoSize = 76;
        double logoX = ReportX + 25;
        double logoY = y + 10;
        DrawImageOrLogoPlaceholder(gfx, schoolProfile.Logo, schoolProfile.Name, logoX, logoY, logoSize, logoSize);

        double photoW = 86;
        double photoH = 78;
        double photoX = ReportX + ReportWidth - photoW - 16;
        double photoY = y + 8;
        DrawCell(gfx, photoX, photoY, photoW, photoH, XColor.FromArgb(255, 210, 210, 210), 1.0);
        CenterTextInCell(gfx, "PHOTO", photoX, photoY, photoW, photoH, Font(7), XColor.FromArgb(255, 120, 120, 120));

        double textX = logoX + logoSize + 18;
        double textW = photoX - textX - 10;

        var schoolName = Coalesce(schoolProfile.Name, "School Name");
        var address = Coalesce(schoolProfile.Address);
        var phones = Coalesce(schoolProfile.PhoneNumbers);

        CenterText(gfx, schoolName, textX, y + 30, textW, Font(13, true), White);
        CenterText(gfx, address, textX, y + 48, textW, Font(11, true), White);
        CenterText(gfx, phones, textX, y + 64, textW, Font(10, true), White);
        CenterText(gfx, "STUDENT TERMINAL REPORT", textX, y + 82, textW, Font(12, true), White);

        return y + HeaderHeight;
    }

    private static string Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "";
    }

    private void DrawImageOrLogoPlaceholder(XGraphics gfx, byte[]? imageBytes, string schoolName, double x, double y, double width, double height)
    {
        if (TryDrawImage(gfx, imageBytes, x, y, width, height))
        {
            return;
        }

        DrawCell(gfx, x, y, width, height, Accent, 1.2);
        CenterTextInCell(gfx, BuildInitials(schoolName), x, y, width, height, Font(16, true), Accent);
    }

    private static string BuildInitials(string value)
    {
        var words = (value ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetterOrDigit(w[0]))
            .Take(3)
            .Select(w => char.ToUpperInvariant(w[0]).ToString());

        var initials = string.Concat(words);
        return string.IsNullOrWhiteSpace(initials) ? "SCH" : initials;
    }

    private bool TryDrawImage(XGraphics gfx, byte[]? imageBytes, double x, double y, double width, double height)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return false;
        }

        try
        {
            using var stream = new MemoryStream(imageBytes, 0, imageBytes.Length, writable: false, publiclyVisible: true);
            using var image = XImage.FromStream(() => stream);
            gfx.DrawImage(image, x, y, width, height);
            gfx.DrawRectangle(Border(0.9), x, y, width, height);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private double DrawStudentInfo(XGraphics gfx, double y, string classId, string term, string yearStr, ReportCardStudentDto data)
    {
        double leftPairW = ReportWidth * 0.59;
        double rightPairW = ReportWidth - leftPairW;
        double leftLabelW = 82;
        double rightLabelW = 78;
        double rightValueW = rightPairW - rightLabelW;
        var labelFont = Font(InfoLabelFontSize, true);
        var valueFont = Font(InfoValueFontSize);

        var rows = new[]
        {
            new { LL = "Student Name:", LV = data.FullName, RL = "Resuming Date:", RV = FormatReportDate(data.TermReopeningDate) },
            new { LL = "Admission No.:", LV = data.StudentId, RL = "Attendance:", RV = $"{data.PresentDays} Out of {data.TotalSchoolDays}" },
            new { LL = "Class/Form:", LV = classId, RL = "Number On Roll:", RV = data.TotalStudentsInClass > 0 ? data.TotalStudentsInClass.ToString() : "" },
            new { LL = "Gender:", LV = data.Gender, RL = "Position in Class:", RV = FormatPosition(data.OverallPosition) },
            new { LL = "Term:", LV = term.ToUpper(), RL = "Average Score:", RV = FormatScore(data.Grades.Count > 0 ? data.Grades.Average(g => g.TotalScore) : 0) },
            new { LL = "Closing Date:", LV = FormatReportDate(data.TermClosingDate), RL = "Academic Year", RV = yearStr }
        };

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            double rowY = y + (i * InfoRowHeight);

            DrawCell(gfx, ReportX, rowY, leftLabelW, InfoRowHeight);
            DrawLeftText(gfx, row.LL, ReportX, rowY, leftLabelW, InfoRowHeight, labelFont);

            DrawCell(gfx, ReportX + leftLabelW, rowY, leftPairW - leftLabelW, InfoRowHeight);
            CenterTextInCell(gfx, row.LV, ReportX + leftLabelW, rowY, leftPairW - leftLabelW, InfoRowHeight, valueFont);

            DrawCell(gfx, ReportX + leftPairW, rowY, rightLabelW, InfoRowHeight);
            DrawLeftText(gfx, row.RL, ReportX + leftPairW, rowY, rightLabelW, InfoRowHeight, labelFont);

            DrawCell(gfx, ReportX + leftPairW + rightLabelW, rowY, rightValueW, InfoRowHeight);
            var font = row.RL == "Academic Year" || row.RL.StartsWith("Number", StringComparison.OrdinalIgnoreCase)
                ? Font(InfoValueFontSize, true)
                : valueFont;
            CenterTextInCell(gfx, row.RV, ReportX + leftPairW + rightLabelW, rowY, rightValueW, InfoRowHeight, font);
        }

        return y + rows.Length * InfoRowHeight;
    }

    private static string FormatReportDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("dddd, dd MMMM yyyy").ToUpperInvariant() : "Not set";
    }

    private double DrawSubjectsAndGrading(XGraphics gfx, double y, ReportCardStudentDto data)
    {
        double leftTableW = ReportWidth * 0.76;
        double gradingW = ReportWidth - leftTableW;

        double[] widths = { leftTableW * 0.270, leftTableW * 0.110, leftTableW * 0.110, leftTableW * 0.110, leftTableW * 0.085, leftTableW * 0.130, leftTableW * 0.185 };
        string[] headers = { "Subjects", "Class\nScore\n(50%)", "Exam\nScore\n(50%)", "Total\nScore\n(100%)", "Grade", "Position\nPer\nSubject", "Remarks" };

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
        for (int i = 0; i < data.Grades.Count; i++)
        {
            var r = data.Grades[i];
            string[] values = { r.Subject, FormatScore(r.CategoryTotal), FormatScore(r.ExamScore), FormatScore(r.TotalScore), GetComputedGrade(r.TotalScore), FormatPosition(r.PositionInClass), GetComputedRemark(r.TotalScore) };
            double cellX = ReportX;
            for (int j = 0; j < values.Length; j++)
            {
                DrawFilledCell(gfx, cellX, rowY, widths[j], SubjectRowHeight, White);
                if (j == 0) DrawLeftText(gfx, values[j], cellX, rowY, widths[j], SubjectRowHeight, Font(7.6));
                else CenterTextInCell(gfx, values[j], cellX, rowY, widths[j], SubjectRowHeight, Font(7.6));
                cellX += widths[j];
            }
            DrawGradingSideRow(gfx, gradingX, rowY, gradingW, i);
            rowY += SubjectRowHeight;
        }

        // Total Row
        decimal classTotal = data.Grades.Sum(s => s.CategoryTotal);
        decimal examTotal = data.Grades.Sum(s => s.ExamScore);
        decimal totalTotal = data.Grades.Sum(s => s.TotalScore);

        string[] tValues = { "Total", FormatScore(classTotal), FormatScore(examTotal), FormatScore(totalTotal), "", "", "" };
        double tx = ReportX;
        for (int j = 0; j < tValues.Length; j++)
        {
            DrawFilledCell(gfx, tx, rowY, widths[j], SubjectRowHeight, Secondary);
            if (j == 0) DrawLeftText(gfx, tValues[j], tx, rowY, widths[j], SubjectRowHeight, Font(7.6, true));
            else CenterTextInCell(gfx, tValues[j], tx, rowY, widths[j], SubjectRowHeight, Font(7.6, true));
            tx += widths[j];
        }
        DrawFilledCell(gfx, gradingX, rowY, gradingW, SubjectRowHeight, Secondary);

        return rowY + SubjectRowHeight;
    }

    private void DrawGradingSideRow(XGraphics gfx, double x, double y, double width, int rowIndex)
    {
        double[] widths = { width * 0.34, width * 0.25, width * 0.41 };
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
            DrawMultilineCenter(gfx, values[i], nextX, y, widths[i], SubjectRowHeight, Font(5.8), Black);
            nextX += widths[i];
        }
    }

    private double DrawRemarks(XGraphics gfx, double y, ReportCardStudentDto data)
    {
        var rows = new[]
        {
            Tuple.Create("Attitude:", data.Attitude),
            Tuple.Create("Interest:", data.Interest),
            Tuple.Create("Conduct:", data.Conduct),
            Tuple.Create("Class Teacher's Remarks:", data.TeacherRemark),
            Tuple.Create("Head Teacher's Remarks:", data.HeadTeacherRemark)
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

    private void DrawFilledCell(XGraphics gfx, double x, double y, double width, double height, XColor fill, double borderWidth = 0.7)
    {
        gfx.DrawRectangle(Brush(fill), x, y, width, height);
        gfx.DrawRectangle(Border(borderWidth), x, y, width, height);
    }

    private static void DrawBrandFooter(XGraphics gfx)
    {
        var font = new XFont("Arial", 6.8, XFontStyle.Regular);
        var brush = new XSolidBrush(XColor.FromArgb(125, 100, 116, 139));
        var pen = new XPen(XColor.FromArgb(70, 148, 163, 184), 0.4);
        double margin = 36;
        double lineY = PageHeight - 24;

        gfx.DrawLine(pen, margin, lineY, PageWidth - margin, lineY);
        gfx.DrawString(
            BrandFooterText,
            font,
            brush,
            new XRect(margin, lineY + 4, PageWidth - (margin * 2), 12),
            XStringFormats.TopCenter);
    }

    private void DrawCell(XGraphics gfx, double x, double y, double width, double height, XColor? borderColor = null, double borderWidth = 0.7)
    {
        gfx.DrawRectangle(new XPen(borderColor ?? Black, borderWidth), x, y, width, height);
    }

    private void CenterText(XGraphics gfx, string text, double x, double y, double width, XFont font, XColor? color = null)
    {
        gfx.DrawString(text ?? "", font, Brush(color ?? Black), new XRect(x, y - font.Size, width, font.Size + 2), new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });
    }

    private void CenterTextInCell(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
    {
        gfx.DrawString(text ?? "", font, Brush(color ?? Black), new XRect(x + 1, y, width - 2, height), new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });
    }

    private void DrawLeftText(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
    {
        gfx.DrawString(text ?? "", font, Brush(color ?? Black), new XRect(x + 3, y, width - 5, height), new XStringFormat { Alignment = XStringAlignment.Near, LineAlignment = XLineAlignment.Center });
    }

    private void DrawMultilineCenter(XGraphics gfx, string text, double x, double y, double width, double height, XFont font, XColor? color = null)
    {
        string[] lines = (text ?? "").Split(new[] { '\n' }, StringSplitOptions.None);
        double lineHeight = font.Size + 1.5;
        double startY = y + (height - (lines.Length * lineHeight)) / 2;
        for (int i = 0; i < lines.Length; i++)
        {
            gfx.DrawString(lines[i], font, Brush(color ?? Black), new XRect(x + 1, startY + i * lineHeight, width - 2, lineHeight), new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center });
        }
    }

    private string FormatScore(decimal score)
    {
        return score.ToString("0.00");
    }

    private string FormatPosition(int position)
    {
        if (position <= 0) return "";
        string suffix = "th";
        if (position % 100 != 11 && position % 10 == 1) suffix = "st";
        else if (position % 100 != 12 && position % 10 == 2) suffix = "nd";
        else if (position % 100 != 13 && position % 10 == 3) suffix = "rd";
        return $"{position}{suffix}";
    }

    private string GetComputedGrade(decimal totalScore)
    {
        if (totalScore >= 80) return "1";
        if (totalScore >= 70) return "2";
        if (totalScore >= 60) return "3";
        if (totalScore >= 50) return "4";
        if (totalScore >= 40) return "5";
        if (totalScore > 0) return "6";
        return "";
    }

    private string GetComputedRemark(decimal totalScore)
    {
        if (totalScore >= 80) return "Highest";
        if (totalScore >= 70) return "Higher";
        if (totalScore >= 60) return "High";
        if (totalScore >= 50) return "High Average";
        if (totalScore >= 40) return "Average";
        if (totalScore > 0) return "Low";
        return "";
    }
}
