using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    public class ScholarshipCategory
    {
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public string DiscountType { get; set; } // 'PERCENTAGE', 'FIXED'
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; }
    }

    public class StudentScholarship
    {
        public int AssignmentID { get; set; }
        public string StudentID { get; set; }
        public int CategoryID { get; set; }
        public DateTime AssignedDate { get; set; }
        public string ApprovalStatus { get; set; }

        // Join properties
        public string StudentName { get; set; }
        public string CategoryName { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
    }
}
