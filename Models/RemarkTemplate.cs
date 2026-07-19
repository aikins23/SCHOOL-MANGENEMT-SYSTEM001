using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Pre-defined remark templates that teachers can pick from when filling report cards.
    /// Schools can customize their own templates per category and sub-category.
    /// Categories: "Conduct", "Effort", "Attendance", "Academic", "General"
    /// </summary>
    public class RemarkTemplate
    {
        public int RemarkTemplateId { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string RemarkText { get; set; }
        public int? SchoolId { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public static class RemarkCategories
    {
        public const string Conduct = "Conduct";
        public const string Effort = "Effort";
        public const string Attendance = "Attendance";
        public const string Academic = "Academic";
        public const string General = "General";
    }

    public static class RemarkSubCategories
    {
        public const string Excellent = "Excellent";
        public const string Good = "Good";
        public const string Satisfactory = "Satisfactory";
        public const string NeedsImprovement = "Needs Improvement";
        public const string Poor = "Poor";
    }
}
