using System;
using System.Collections.Generic;

namespace kingdom_Preparatory_School_Management_System.Models
{
    public class WeeklyOutputReport
    {
        public int OutputReportId { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string AcademicYear { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }

        public decimal AverageCompletionRate { get; set; }
        public int StudentsOnTrack { get; set; }
        public int StudentsNeedingSupport { get; set; }

        public string Notes { get; set; } = string.Empty;

        public int WorkflowId { get; set; } // Link to the approval workflow

        public List<StudentWorkTracking> StudentTrackings { get; set; } = new List<StudentWorkTracking>();
    }

    public class StudentWorkTracking
    {
        public int TrackingId { get; set; }
        public int OutputReportId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public int ExercisesCompleted { get; set; }
        public int HomeworksCompleted { get; set; }
        public decimal CompletionPercentage { get; set; }

        /// <summary>
        /// "OnTrack", "NeedsSupport", "Excellent"
        /// </summary>
        public string Status { get; set; } = "OnTrack";
    }
}
