using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("StaffAttendance")]
public class StaffAttendanceEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Column("EmployeeId")]
    public int EmployeeId { get; set; }

    [Column("EmployeeName")]
    public string EmployeeName { get; set; } = "";

    [Column("Department")]
    public string Department { get; set; } = "";

    [Column("Date")]
    public DateTime Date { get; set; }

    [Column("ClockInTime")]
    public DateTime ClockInTime { get; set; }

    [Column("ClockInLatitude")]
    public double ClockInLatitude { get; set; }

    [Column("ClockInLongitude")]
    public double ClockInLongitude { get; set; }

    [Column("ClockInDistanceMeters")]
    public double ClockInDistanceMeters { get; set; }

    [Column("ClockOutTime")]
    public DateTime? ClockOutTime { get; set; }

    [Column("ClockOutLatitude")]
    public double? ClockOutLatitude { get; set; }

    [Column("ClockOutLongitude")]
    public double? ClockOutLongitude { get; set; }

    [Column("ClockOutDistanceMeters")]
    public double? ClockOutDistanceMeters { get; set; }

    [Column("Status")]
    public string Status { get; set; } = "Present (On Time)";

    [Column("VerificationStatus")]
    public string VerificationStatus { get; set; } = "Verified On-Premises";

    [Column("Remarks")]
    public string? Remarks { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
