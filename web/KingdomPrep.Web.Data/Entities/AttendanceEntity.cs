using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Attendance")]
public class AttendanceEntity
{
    [Key]
    [Column("AttendanceID")]
    public int AttendanceID { get; set; }

    [Column("ReferenceID")]
    public int ReferenceID { get; set; }

    [Column("ReferenceType")]
    public string ReferenceType { get; set; } = "Student";

    [Column("FullName")]
    public string FullName { get; set; } = "";

    [Column("Date")]
    public DateTime Date { get; set; }

    [Column("Status")]
    public string Status { get; set; } = "Present";

    [Column("Remarks")]
    public string? Remarks { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
