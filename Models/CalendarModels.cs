using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    public class AcademicTerm
    {
        public int TermID { get; set; }
        public string TermName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
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
