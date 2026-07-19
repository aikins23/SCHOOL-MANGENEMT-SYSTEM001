using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("OnlinePaymentIntents")]
public class OnlinePaymentIntentEntity
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Reference { get; set; } = "";

    public Guid? SchoolId { get; set; }

    public int StudentID { get; set; }

    [MaxLength(100)]
    public string? ClassID { get; set; }

    [MaxLength(250)]
    public string? StudentName { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceBeforePayment { get; set; }

    [MaxLength(30)]
    public string Gateway { get; set; } = "";

    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(150)]
    public string? CreatedBy { get; set; }
}
