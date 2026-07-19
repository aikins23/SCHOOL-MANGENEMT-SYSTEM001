using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("StudentPerformanceEntries")]
public class StudentPerformanceEntryEntity
{
    [Key]
    [Column("EntryId")]
    public int EntryId { get; set; }

    [Column("ReportId")]
    public int ReportId { get; set; }

    [Column("StudentId")]
    public string StudentId { get; set; } = "";

    [Column("PerformanceTrend")]
    public string PerformanceTrend { get; set; } = "Stable";

    [Column("ExerciseMarksObtained", TypeName = "decimal(6,2)")]
    public decimal? ExerciseMarksObtained { get; set; }

    [Column("ExerciseMarksTotal", TypeName = "decimal(6,2)")]
    public decimal? ExerciseMarksTotal { get; set; }

    [Column("HomeworkMarksObtained", TypeName = "decimal(6,2)")]
    public decimal? HomeworkMarksObtained { get; set; }

    [Column("HomeworkMarksTotal", TypeName = "decimal(6,2)")]
    public decimal? HomeworkMarksTotal { get; set; }

    [Column("TeacherNotes")]
    public string? TeacherNotes { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }

    [Column("SyncId")]
    public Guid? SyncId { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
