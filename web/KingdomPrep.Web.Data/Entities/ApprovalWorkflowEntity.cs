using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ApprovalWorkflows")]
public class ApprovalWorkflowEntity
{
    [Key]
    [Column("WorkflowId")]
    public int WorkflowId { get; set; }

    [Column("EntityType")]
    public string EntityType { get; set; } = "";

    [Column("EntityId")]
    public int EntityId { get; set; }

    [Column("CurrentStatus")]
    public string CurrentStatus { get; set; } = "Draft";

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }

    [Column("SyncId")]
    public Guid? SyncId { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
