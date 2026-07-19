using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("examss")]
public class ExamResultEntity
{
    [Column("sn")] public int ExamID { get; set; }
    [Column("std_id")] public int? StudentId { get; set; }
    [Column("std_name")] public string? StudentName { get; set; }
    [Column("std_class")] public string? ClassId { get; set; }
    [Column("subject")] public string? Subject { get; set; }
    [Column("cat1")] public double? ClassScore1 { get; set; }
    [Column("cat2")] public double? ClassScore2 { get; set; }
    [Column("cat3")] public double? ClassScore3 { get; set; }
    [Column("tl_cat")] public double? CategoryTotal { get; set; }
    [Column("exam_score")] public double? ExamScore { get; set; }
    [Column("gt")] public double? TotalScore { get; set; }
    [Column("grade")] public string? Grade { get; set; }
    [Column("remark")] public string? Remark { get; set; }
    [Column("term")] public string? Term { get; set; }
    [Column("year")] public string? Year { get; set; }
    [Column("TermID")] public int? TermID { get; set; }
    [Column("ExamTypeId")] public int? ExamTypeId { get; set; }
    [Column("AssessmentNumber")] public int? AssessmentNumber { get; set; }
    [Column("AssessmentLabel")] public string? AssessmentLabel { get; set; }
    [Column("SchoolId")] public Guid? SchoolId { get; set; }
    [Column("UpdatedAt")] public DateTime? UpdatedAt { get; set; }
}
