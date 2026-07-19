using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Students")]
public class StudentEntity
{
    [Key]
    [Column("StudentID")]
    public int StudentID { get; set; }

    [Column("FirstName")]
    public string? FirstName { get; set; }

    [Column("LastName")]
    public string? LastName { get; set; }

    [Column("ClassID")]
    public string? ClassID { get; set; }

    [Column("DOB")]
    public DateTime? DateOfBirth { get; set; }

    [Column("Gender")]
    public string? Gender { get; set; }

    [Column("Email")]
    public string? Email { get; set; }

    [Column("HomeTown")]
    public string? HomeTown { get; set; }

    [Column("Residence")]
    public string? Residence { get; set; }

    [Column("Allegies")]
    public string? Allergies { get; set; }

    [Column("EmergencyConatct")]
    public string? EmergencyContact { get; set; }

    [Column("GuidanceName")]
    public string? GuardianName { get; set; }

    [Column("GuidianceEmail")]
    public string? GuardianEmail { get; set; }

    [Column("Guidiance_Location")]
    public string? GuardianLocation { get; set; }

    [Column("admission_date")]
    public DateTime? AdmissionDate { get; set; }

    [Column("Std_pic")]
    public byte[]? ProfilePhoto { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }

    [Column("ParentUsername")]
    public string? ParentUsername { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
