using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents an employee leave application
    /// </summary>
    public class LeaveRequest
    {
        public int LeaveID { get; set; }
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string LeaveOption { get; set; } // "With Pay", "Without Pay"
        public string Reason { get; set; } // "Sick", "Vacation", etc.
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } // "PENDING", "APPROVED", "REJECTED"
        public DateTime AppliedDate { get; set; } = DateTime.Now;

        public int DurationDays => (EndDate.Date - StartDate.Date).Days + 1;
    }
}
