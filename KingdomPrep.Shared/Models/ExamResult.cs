using System;

namespace KingdomPrep.Shared.Models
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
        public decimal RawCategoryTotal => Category1 + Category2 + Category3;
        public decimal CategoryTotal => Scale(RawCategoryTotal, 60m, 50m); // SBA scaled to 50%
        public decimal ExamScore { get; set; } // Exam score already scaled to 50%
        public decimal TotalScore { get; set; } // SBA(50) + Exam(50), capped at 100
        public string Grade { get; set; }
        public string Remark { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Calculates the final score and grade based on raw inputs
        /// </summary>
        public void Calculate()
        {
            TotalScore = Clamp(CategoryTotal + Clamp(ExamScore, 0m, 50m), 0m, 100m);
            Grade = GetGrade(TotalScore);
            Remark = GetRemark(Grade);
        }

        private static decimal Scale(decimal value, decimal fromMax, decimal toMax)
        {
            if (fromMax <= 0m) return 0m;
            return Math.Round(Clamp(value, 0m, fromMax) / fromMax * toMax, 2);
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            return value > max ? max : value;
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
