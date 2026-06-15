using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a class-to-teacher assignment
    /// </summary>
    public class ClassAssignment
    {
        public string ClassName { get; set; }
        public int? ClassTeacherID { get; set; }
        public string ClassTeacherName { get; set; }
        public DateTime? AssignedDate { get; set; }
    }
}
