using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents an employee in the school management system
    /// </summary>
    public class Employee
    {
        public string EmployeeID { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Contact { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string HomeTown { get; set; }
        public string Residence { get; set; }
        public DateTime EmploymentDate { get; set; }
        public string EmploymentMode { get; set; }
        public string EmploymentStatus { get; set; }
        public string EmergencyContactPerson { get; set; }
        public string EmergencyContact { get; set; }
        public string PerformanceReview { get; set; }
        public decimal Salary { get; set; }
        public byte[] ProfilePhoto { get; set; }

        /// <summary>
        /// Gets the age of the employee based on the date of birth.
        /// </summary>
        public int GetAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
