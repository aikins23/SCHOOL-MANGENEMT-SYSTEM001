using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ExamSetups")]
public class ExamSetupEntity
{
    [Column("SetupID")]
    public int SetupID { get; set; }

    [Column("Term")]
    public string? Term { get; set; }

    [Column("Year")]
    public string? Year { get; set; }

    [Column("StartDate")]
    public DateTime? StartDate { get; set; }

    [Column("EndDate")]
    public DateTime? EndDate { get; set; }

    [Column("ExamTypeId")]
    public int? ExamTypeId { get; set; }

    [Column("AssessmentNumber")]
    public int? AssessmentNumber { get; set; }

    [Column("AssessmentLabel")]
    public string? AssessmentLabel { get; set; }

    [Column("ClassWeight")]
    public int ClassWeight { get; set; } = 50;

    [Column("ExamWeight")]
    public int ExamWeight { get; set; } = 50;

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("IsPublishedToPortal")]
    public bool IsPublishedToPortal { get; set; }

    [Column("PublishedAt")]
    public DateTime? PublishedAt { get; set; }

    [Column("PublishedBy")]
    public string? PublishedBy { get; set; }
}
