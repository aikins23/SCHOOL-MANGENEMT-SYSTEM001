using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("StudentFeeLedger")]
public class StudentFeeLedgerEntity
{
    [Key]
    [Column("StudentID")] public string StudentID { get; set; } = "";
    [Column("TermID")] public int TermID { get; set; }
    [Column("PreviousBalance")] public decimal PreviousBalance { get; set; }
    [Column("TotalExpectedAmount")] public decimal TotalExpectedAmount { get; set; }
    [Column("TotalPaidAmount")] public decimal TotalPaidAmount { get; set; }
    [Column("CurrentTermCharge")] public decimal CurrentTermCharge { get; set; }
    [Column("SchoolId")] public Guid? SchoolId { get; set; }
}
