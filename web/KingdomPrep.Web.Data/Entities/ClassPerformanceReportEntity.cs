using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ClassPerformanceReports")]
public class ClassPerformanceReportEntity
{
    [Key]
    [Column("ReportId")]
    public int ReportId { get; set; }

    [Column("ClassId")]
    public string ClassId { get; set; } = "";

    [Column("TeacherId")]
    public int TeacherId { get; set; }

    [Column("ReportDate")]
    public DateTime ReportDate { get; set; } = DateTime.Now;

    [Column("ReportPeriod")]
    public string ReportPeriod { get; set; } = "Monthly";

    [Column("AcademicYear")]
    public string AcademicYear { get; set; } = "";

    [Column("Term")]
    public string Term { get; set; } = "";

    [Column("WeekNumber")]
    public int? WeekNumber { get; set; }

    [Column("MonthNumber")]
    public int? MonthNumber { get; set; }

    [Column("ReportText")]
    public string? ReportText { get; set; }

    [Column("AnalyticsData")]
    public string? AnalyticsData { get; set; }

    [Column("WorkflowId")]
    public int WorkflowId { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }

    [Column("SyncId")]
    public Guid? SyncId { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
