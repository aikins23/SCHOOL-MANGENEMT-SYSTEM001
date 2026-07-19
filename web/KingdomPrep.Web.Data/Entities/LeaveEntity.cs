using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data.Entities;

[Table("emp_leave")]
[PrimaryKey(nameof(EmploymentID), nameof(StartDate))]
public class LeaveEntity
{
    [Column("employmentID")]
    public int EmploymentID { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("department")]
    public string? Department { get; set; }

    [Column("position")]
    public string? Position { get; set; }

    [Column("Leave_op")]
    public string LeaveOption { get; set; } = "";

    [Column("Reasons")]
    public string? Reasons { get; set; }

    [Column("Start_Date")]
    public DateTime StartDate { get; set; }

    [Column("End_Date")]
    public DateTime EndDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
