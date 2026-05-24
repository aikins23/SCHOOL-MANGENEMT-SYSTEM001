using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents teacher remarks and behavioral observations for a student in a specific term
    /// </summary>
    public class StudentTermRemarks
    {
        public int ID { get; set; }
        public string StudentID { get; set; }
        public string Term { get; set; }                 // "TERM 1", "TERM 2", "TERM 3"
        public string Year { get; set; }                 // "2024/2025"
        public string ClassTeacherRemarks { get; set; }
        public string HeadTeacherRemarks { get; set; }
        public string Attitude { get; set; }
        public string Interest { get; set; }
        public string Conduct { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
