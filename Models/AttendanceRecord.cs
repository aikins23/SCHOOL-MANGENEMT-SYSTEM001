using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents an attendance record for a student or staff member
    /// </summary>
    public class AttendanceRecord
    {
        public int AttendanceID { get; set; }
        public string ReferenceID { get; set; }
        public string ReferenceType { get; set; } // "STUDENT" or "STAFF"
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } // "PRESENT", "ABSENT", "LATE"
        public string Remarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
