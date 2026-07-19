using System;

namespace KingdomPrep.Shared.Models
{
    public class AcademicYear
    {
        public int AcademicYearID { get; set; }
        public string YearName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(YearName) ? $"{StartDate:yyyy} - {EndDate:yyyy}" : YearName;
    }

    public class AcademicTerm
    {
        public int TermID { get; set; }
        public int AcademicYearID { get; set; }
        public string AcademicYearName { get; set; }
        public string TermName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ReopeningDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string ClosureReportPath { get; set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AcademicYearName)) return TermName ?? "";
                if (string.IsNullOrWhiteSpace(TermName)) return AcademicYearName;
                return $"{AcademicYearName} - {TermName}";
            }
        }
    }

    public class StudentBillingBreakdown
    {
        public string StudentID { get; set; }
        public int TermID { get; set; }
        public string TermName { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal CurrentTermFee { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance => TotalExpected - AmountPaid;
    }

    public class TermClosureSummary
    {
        public AcademicTerm Term { get; set; }
        public int TotalStudents { get; set; }
        public int NewAdmissions { get; set; }
        public int TotalEmployees { get; set; }
        public int NewEmployees { get; set; }
        public decimal TotalExpectedFees { get; set; }
        public decimal TotalCollectedFees { get; set; }
        public decimal TotalOutstandingFees { get; set; }
    }

    public class SchoolEvent
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } // Holiday, Event, Exam
    }

    public class TimePeriod
    {
        public int PeriodID { get; set; }
        public string PeriodName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBreak { get; set; }
        public int SortOrder { get; set; }

        public string DisplayTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }

    public class SubjectAllocation
    {
        public int AllocationID { get; set; }
        public string ClassID { get; set; }
        public string SubjectName { get; set; }
        public int? TeacherID { get; set; }
        public int PeriodsPerWeek { get; set; }

        // Joined data
        public string TeacherName { get; set; }
    }

    public class TimetableEntry
    {
        public int EntryID { get; set; }
        public string ClassID { get; set; }
        public int PeriodID { get; set; }
        public int DayOfWeek { get; set; } // 1=Mon...5=Fri
        public string SubjectName { get; set; }
        public int? TeacherID { get; set; }

        // Joined data
        public string TeacherName { get; set; }
        public string PeriodName { get; set; }
    }
}
