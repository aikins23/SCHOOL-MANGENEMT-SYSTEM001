using System;

namespace KingdomPrep.Shared.Models
{
    /// <summary>A pending admission held until the bursar approves payment.</summary>
    public class DraftAdmission
    {
        public int DraftID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ClassID { get; set; }
        public string Email { get; set; }
        public string HomeTown { get; set; }
        public string Residence { get; set; }
        public string Allergies { get; set; }
        public string GuardianName { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianLocation { get; set; }
        public string EmergencyContact { get; set; }
        public DateTime AdmissionDate { get; set; }
        public byte[] ProfilePhoto { get; set; }
        public decimal AdmissionFee { get; set; }
        public decimal SchoolFeePaid { get; set; }
        public decimal TermTotal { get; set; }
        public string PaymentMode { get; set; }
        public string SubmittedBy { get; set; }
        public DateTime SubmittedDate { get; set; }
        public int? BusRouteId { get; set; }   // chosen transport route, null = no bus

        public string FullName => $"{FirstName} {LastName}";

        /// <summary>Maps this draft to a Student for promotion on approval.</summary>
        public Student ToStudent() => new Student
        {
            FirstName = FirstName,
            LastName = LastName,
            DateOfBirth = DateOfBirth,
            Gender = Gender,
            ClassID = ClassID,
            Email = Email,
            HomeTown = HomeTown,
            Residence = Residence,
            Allergies = Allergies,
            GuardianName = GuardianName,
            GuardianEmail = GuardianEmail,
            GuardianLocation = GuardianLocation,
            EmergencyContact = EmergencyContact,
            AdmissionDate = AdmissionDate,
            ProfilePhoto = ProfilePhoto
        };
    }
}
