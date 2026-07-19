using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("payment_record")]
public class PaymentRecordEntity
{
    [Key]
    [Column("ID")] public int ID { get; set; }
    [Column("StudentID")] public int StudentID { get; set; }
    [Column("classID")] public string? ClassID { get; set; }
    [Column("student_name")] public string? StudentName { get; set; }
    [Column("Amount_paid")] public decimal AmountPaid { get; set; }
    [Column("Balance")] public decimal Balance { get; set; }
    [Column("Date")] public DateTime? PaymentDate { get; set; }
    [Column("SchoolId")] public Guid? SchoolId { get; set; }
    [Column("PaymentReference")] public string? PaymentReference { get; set; }
}
