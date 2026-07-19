using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("AuditLogs")]
public class AuditLogEntity
{
    [Key]
    public long AuditLogID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ActorUsername { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Summary { get; set; }
    public Guid? SchoolId { get; set; }
}
