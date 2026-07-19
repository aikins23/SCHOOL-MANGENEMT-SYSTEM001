using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a type of examination configurable per school.
    /// Examples: End of Term, Mid-Term, Class Test, Mock Exam, BECE Mock, Quiz.
    /// Each type has its own weight in final grade calculations.
    /// </summary>
    public class ExamType
    {
        public int ExamTypeId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal WeightPercentage { get; set; }
        public bool IsGradedExam { get; set; }
        public bool IncludeInReportCard { get; set; }
        public int DisplayOrder { get; set; }
        public Guid? SchoolId { get; set; }
        public bool IsSystemType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public static class SystemExamTypeCodes
    {
        public const string EndOfTerm = "EOT";
        public const string MidTerm = "MID";
        public const string ClassTest = "CT";
        public const string Mock = "MOCK";
        public const string Quiz = "QUIZ";
        public const string Assignment = "ASSIGN";
    }
}
