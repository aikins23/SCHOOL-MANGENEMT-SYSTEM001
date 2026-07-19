using System;
using System.Collections.Generic;

namespace kingdom_Preparatory_School_Management_System.Models
{
    public class ClassPerformanceReport
    {
        public int ReportId { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public DateTime ReportDate { get; set; }
        public string ReportPeriod { get; set; } = "Monthly"; // "Weekly", "Monthly", "Termly"
        public string AcademicYear { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int? WeekNumber { get; set; }
        public int? MonthNumber { get; set; }

        public string ReportText { get; set; } = string.Empty;
        public string AnalyticsData { get; set; } = string.Empty; // JSON analytics data

        public int WorkflowId { get; set; } // Link to the approval workflow
        public Guid? SchoolId { get; set; }
        public Guid? SyncId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<StudentPerformanceEntry> StudentEntries { get; set; } = new List<StudentPerformanceEntry>();
    }

    public class StudentPerformanceEntry
    {
        public int EntryId { get; set; }
        public int ReportId { get; set; }
        public string StudentId { get; set; } = string.Empty;

        /// <summary>
        /// "Improving", "Stable", "Declining"
        /// </summary>
        public string PerformanceTrend { get; set; } = "Stable";

        public decimal? ExerciseMarksObtained { get; set; }
        public decimal? ExerciseMarksTotal { get; set; }
        public decimal? HomeworkMarksObtained { get; set; }
        public decimal? HomeworkMarksTotal { get; set; }

        public string TeacherNotes { get; set; } = string.Empty;
        public Guid? SchoolId { get; set; }
        public Guid? SyncId { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
