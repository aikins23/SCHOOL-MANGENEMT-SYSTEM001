using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("AcademicTerms")]
public class AcademicTermEntity
{
    public int TermID { get; set; }
    public int AcademicYearID { get; set; }
    public string TermName { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReopeningDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosureReportPath { get; set; }
    public Guid SchoolId { get; set; }
}
