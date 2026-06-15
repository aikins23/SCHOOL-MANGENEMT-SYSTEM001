using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a school notice or announcement.
    /// </summary>
    public class Notice
    {
        public int NoticeID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Target { get; set; } // All, Parents, Employees, Class
        public string TargetClass { get; set; }
        public string Channel { get; set; } // SMS, Email, Both
        public string SentBy { get; set; }
        public DateTime SentDate { get; set; }
        public int RecipientCount { get; set; }
        public string Status { get; set; }
    }
}
