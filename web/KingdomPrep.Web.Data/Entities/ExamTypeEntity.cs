using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ExamTypes")]
public class ExamTypeEntity
{
    public int ExamTypeId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public decimal WeightPercentage { get; set; }
    public bool IsGradedExam { get; set; } = true;
    public bool IncludeInReportCard { get; set; } = true;
    public int DisplayOrder { get; set; }
    public Guid? SchoolId { get; set; }
    public bool IsSystemType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
