using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Students")]
public class StudentEntity
{
    [Column("StudentID")] public string StudentID { get; set; } = "";
    [Column("FirstName")] public string? FirstName { get; set; }
    [Column("LastName")] public string? LastName { get; set; }
    [Column("ClassID")] public string? ClassID { get; set; }
    [Column("DOB")] public DateTime? DateOfBirth { get; set; }
    [Column("Gender")] public string? Gender { get; set; }
    [Column("GuidanceName")] public string? GuardianName { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
