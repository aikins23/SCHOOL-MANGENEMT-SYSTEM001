using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("DraftAdmissions")]
public class DraftAdmissionEntity
{
    [Column("DraftID")] public int DraftID { get; set; }
    [Column("FirstName")] public string? FirstName { get; set; }
    [Column("LastName")] public string? LastName { get; set; }
    [Column("DOB")] public DateTime? DOB { get; set; }
    [Column("Gender")] public string? Gender { get; set; }
    [Column("ClassID")] public string? ClassID { get; set; }
    [Column("Email")] public string? Email { get; set; }
    [Column("HomeTown")] public string? HomeTown { get; set; }
    [Column("Residence")] public string? Residence { get; set; }
    [Column("Allegies")] public string? Allergies { get; set; } // Note: original schema has typo 'Allegies'
    [Column("EmergencyConatct")] public string? EmergencyContact { get; set; }
    [Column("GuidanceName")] public string? GuidanceName { get; set; }
    [Column("GuidianceEmail")] public string? GuidanceEmail { get; set; }
    [Column("Guidiance_Location")] public string? GuidanceLocation { get; set; }
    [Column("admission_date")] public DateTime? AdmissionDate { get; set; }
    [Column("AdmissionFee")] public decimal? AdmissionFee { get; set; }
    [Column("SchoolFeePaid")] public decimal? SchoolFeePaid { get; set; }
    [Column("TermTotal")] public decimal? TermTotal { get; set; }
    [Column("PaymentMode")] public string? PaymentMode { get; set; }
    [Column("SubmittedBy")] public string? SubmittedBy { get; set; }
    [Column("SubmittedDate")] public DateTime? SubmittedDate { get; set; }
    [Column("SchoolId")] public Guid? SchoolId { get; set; }
}
