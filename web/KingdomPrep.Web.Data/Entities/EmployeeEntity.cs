using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Employee")]
public class EmployeeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("employmentID")]
    public int EmployeeID { get; set; }

    [Column("fullName")]
    public string? FullName { get; set; }

    [Column("gender")]
    public string? Gender { get; set; }

    [Column("dOB")]
    public DateTime? DateOfBirth { get; set; }

    [Column("conatct")]
    public string? Contact { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("department")]
    public string? Department { get; set; }

    [Column("position")]
    public string? Position { get; set; }

    [Column("homeTown")]
    public string? HomeTown { get; set; }

    [Column("residence")]
    public string? Residence { get; set; }

    [Column("date_of_Emplyment")]
    public DateTime? EmploymentDate { get; set; }

    [Column("employment_Mode")]
    public string? EmploymentMode { get; set; }

    [Column("employment_Status")]
    public string? EmploymentStatus { get; set; }

    [Column("emergency_Contact_Person")]
    public string? EmergencyContactPerson { get; set; }

    [Column("emergency_contact")]
    public string? EmergencyContact { get; set; }

    [Column("Employees_Reviews")]
    public string? PerformanceReview { get; set; }

    [Column("salary")]
    public decimal Salary { get; set; }

    [Column("pic")]
    public byte[]? ProfilePhoto { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
