using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("SchoolPaymentSettings")]
public class SchoolPaymentSettingsEntity
{
    [Key]
    public int Id { get; set; }

    public Guid SchoolId { get; set; }

    [MaxLength(200)]
    public string? PaystackPublicKey { get; set; }

    [MaxLength(500)]
    public string? PaystackSecretKey { get; set; }

    [MaxLength(500)]
    public string? MomoSubscriptionKey { get; set; }

    [MaxLength(200)]
    public string? MomoApiUser { get; set; }

    [MaxLength(500)]
    public string? MomoApiKey { get; set; }

    [MaxLength(50)]
    public string? MomoTargetEnvironment { get; set; }

    [MaxLength(300)]
    public string? MomoBaseUrl { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [MaxLength(150)]
    public string? UpdatedBy { get; set; }
}
