using System;

namespace KingdomPrep.Shared.Models
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
