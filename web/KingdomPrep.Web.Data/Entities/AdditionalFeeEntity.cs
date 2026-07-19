using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("AdditionalFees")]
public class AdditionalFeeEntity
{
    [Key]
    [Column("AdditionalFeeId")] public int AdditionalFeeId { get; set; }
    [Column("FeeName")] public string FeeName { get; set; } = "";
    [Column("Description")] public string? Description { get; set; }
    [Column("AcademicYear")] public string AcademicYear { get; set; } = "";
    [Column("TermName")] public string TermName { get; set; } = "";
    [Column("Status")] public string Status { get; set; } = "";
    [Column("SchoolId")] public Guid? SchoolId { get; set; }
}

[Table("AdditionalFeeStudentCharges")]
public class AdditionalFeeStudentChargeEntity
{
    [Key]
    [Column("ChargeId")] public int ChargeId { get; set; }
    [Column("AdditionalFeeId")] public int AdditionalFeeId { get; set; }
    [Column("StudentID")] public string StudentID { get; set; } = "";
    [Column("StudentName")] public string? StudentName { get; set; }
    [Column("ClassID")] public string? ClassID { get; set; }
    [Column("Amount")] public decimal Amount { get; set; }
    [Column("Status")] public string Status { get; set; } = "";
    [Column("SchoolId")] public Guid? SchoolId { get; set; }
}
