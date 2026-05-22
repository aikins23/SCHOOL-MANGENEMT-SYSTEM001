using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a single subject exam result
    /// </summary>
    public class ExamResult
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string ClassId { get; set; }
        public string Subject { get; set; }
        public string Term { get; set; }
        public string Year { get; set; }
        public decimal Category1 { get; set; } // Test (40)
        public decimal Category2 { get; set; } // Group (10)
        public decimal Category3 { get; set; } // Project (10)
        public decimal CategoryTotal => Category1 + Category2 + Category3;
        public decimal ExamScore { get; set; } // Exam (100)
        public decimal TotalScore { get; set; } // (CategoryTotal/60 * 50) + (ExamScore/100 * 50)
        public string Grade { get; set; }
        public string Remark { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Calculates the final score and grade based on raw inputs
        /// </summary>
        public void Calculate()
        {
            // Calculation logic from EXAMS.cs:
            // decimal classScore = test + group + project;
            // decimal total = ((classScore / 60m) * 50m) + ((exam / 100m) * 50m);
            
            TotalScore = ((CategoryTotal / 60m) * 50m) + ((ExamScore / 100m) * 50m);
            Grade = GetGrade(TotalScore);
            Remark = GetRemark(Grade);
        }

        private string GetGrade(decimal score)
        {
            if (score >= 80m) return "1";
            if (score >= 75m) return "2";
            if (score >= 70m) return "3";
            if (score >= 65m) return "4";
            return "5";
        }

        private string GetRemark(string grade)
        {
            switch (grade)
            {
                case "1": return "Advance";
                case "2": return "Proficiency";
                case "3": return "Approaching Proficiency";
                case "4": return "Developing";
                case "5": return "Beginning";
                default: return "-";
            }
        }
    }
}
