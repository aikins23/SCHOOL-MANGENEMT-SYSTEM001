using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Thin façade used by the exam views to export a student's terminal report card to PDF.
    /// Maps the flat grid row into <see cref="ReportCardData"/> and delegates to
    /// <see cref="ReportCardPDFGenerator"/>, which renders the official layout using the configured
    /// school identity (name/address/logo) from <see cref="Common.SchoolProfile"/>. The school name,
    /// address, logo, etc. are NOT hardcoded here — they come from School Information settings.
    /// </summary>
    public static class ReportCardPdfService
    {
        public static void Export(Dictionary<string, string> data)
        {
            try
            {
                string name     = V(data, "NAME", "Student");
                string safeName = name.Replace(" ", "_").Replace("/", "-");
                string fileName = $"ReportCard_{safeName}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string path     = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var reportData = ToReportCardData(data);
                var generator  = new ReportCardPDFGenerator();
                var bytes      = generator.GeneratePDFAsync(reportData).GetAwaiter().GetResult();
                File.WriteAllBytes(path, bytes);

                MessageBox.Show(
                    $"Report card saved to Desktop:\n{fileName}",
                    "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                try { Process.Start(path); } catch { /* viewer not found */ }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF generation failed:\n" + ex.Message,
                    "PDF Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static ReportCardData ToReportCardData(Dictionary<string, string> data)
        {
            var reportData = new ReportCardData
            {
                StudentID = V(data, "STD_ID", V(data, "StudentID", "")),
                StudentName = V(data, "NAME", "Student"),
                ClassID = V(data, "CLASS", ""),
                Gender = V(data, "GENDER", ""),
                Term = V(data, "TERMS", V(data, "TERM", "TERM 3")),
                Year = V(data, "YEAR", "2024/2025"),
                PresentDays = ParseInt(V(data, "ATTENDANCE", "")),
                TotalSchoolDays = ParseInt(V(data, "TOTAL_SCHOOL_DAYS", "")),
                OverallPosition = ParseInt(V(data, "TOTAL_RANK", "")),
                TotalStudentsInClass = ParseInt(V(data, "NUMBER_ON_ROLL", "")),
                // Leave null so the generator falls back to the buyer's configured identity
                // (SchoolProfile.Name/Address/Phones) rather than the SchoolInfo model defaults.
                SchoolInfo = null,
                Remarks = new StudentTermRemarks
                {
                    Attitude = V(data, "ATTITUDE", ""),
                    Interest = V(data, "INTEREST", ""),
                    Conduct = V(data, "CONDUCT", ""),
                    ClassTeacherRemarks = V(data, "CLASS_TEACHER_REMARKS", ""),
                    HeadTeacherRemarks = V(data, "HEAD_TEACHER_REMARKS", "")
                }
            };

            AddSubject(reportData, data, "Literacy", "ENG");
            AddSubject(reportData, data, "Numeracy", "MATHS");
            AddSubject(reportData, data, "Pre-Writing", "SCI");
            AddSubject(reportData, data, "Pre-Reading", "SOCIAL");
            AddSubject(reportData, data, "Creative Arts", "CRE_ART");
            AddSubject(reportData, data, "OWOP", "COMP");
            AddSubject(reportData, data, "Career Technology", "CAREER");
            AddSubject(reportData, data, "R.M.E.", "RME");
            AddSubject(reportData, data, "Ghanaian Language", "GHA_LANG");

            return reportData;
        }

        private static void AddSubject(ReportCardData reportData, Dictionary<string, string> data, string label, string key)
        {
            decimal classScore = ParseDecimal(V(data, key + "_CAT", ""));
            decimal examScore = ParseDecimal(V(data, key + "_EXAM", ""));
            decimal totalScore = ParseDecimal(V(data, key, ""));

            if (classScore == 0 && examScore == 0 && totalScore == 0)
                return;

            reportData.SubjectResults.Add(new SubjectResult
            {
                Subject = label,
                ClassScore = classScore,
                ExamScore = examScore,
                TotalScore = totalScore,
                Grade = V(data, key + "_GRADE", ""),
                Remark = V(data, key + "_REMARK", ""),
                PositionInClass = ParseInt(V(data, key + "_POS", ""))
            });
        }

        private static decimal ParseDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0m;
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string digits = "";
            foreach (char c in value)
            {
                if (char.IsDigit(c))
                    digits += c;
                else if (digits.Length > 0)
                    break;
            }

            return int.TryParse(digits, out int result) ? result : 0;
        }

        private static string V(Dictionary<string, string> d, string key, string fallback = "")
        {
            if (d == null) return fallback;
            return d.TryGetValue(key, out string v) && !string.IsNullOrWhiteSpace(v)
                   ? v : fallback;
        }
    }
}
