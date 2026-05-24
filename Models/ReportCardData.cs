using System;
using System.Collections.Generic;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Data Transfer Object containing all information needed to generate a single report card
    /// </summary>
    public class ReportCardData
    {
        // Student Information
        public string StudentID { get; set; }
        public string StudentName { get; set; }
        public string ClassID { get; set; }
        public string Gender { get; set; }
        public byte[] ProfilePhoto { get; set; }
        public DateTime AdmissionDate { get; set; }

        // Academic Information
        public string Term { get; set; }
        public string Year { get; set; }

        // Attendance Summary
        public int PresentDays { get; set; }
        public int TotalSchoolDays { get; set; }
        public int AttendancePercentage
        {
            get
            {
                if (TotalSchoolDays == 0) return 0;
                return (PresentDays * 100) / TotalSchoolDays;
            }
        }

        // Subject Results with Rankings
        public List<SubjectResult> SubjectResults { get; set; } = new List<SubjectResult>();

        // Overall Rankings
        public int OverallPosition { get; set; }
        public int TotalStudentsInClass { get; set; }

        // Remarks
        public StudentTermRemarks Remarks { get; set; }

        // School Information
        public SchoolInfo SchoolInfo { get; set; }
    }
}
