using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>
    /// Represents a student in the school management system
    /// </summary>
    public class Student
    {
        public string StudentID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ClassID { get; set; }
        public string Email { get; set; }
        public string HomeTown { get; set; }
        public string Residence { get; set; }
        public string Allergies { get; set; }
        public byte[] ProfilePhoto { get; set; }
        public string GuardianName { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianLocation { get; set; }
        public string EmergencyContact { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public int GetAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
