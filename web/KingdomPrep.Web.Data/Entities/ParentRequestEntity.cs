using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ParentRequests")]
public class ParentRequestEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("RequestID")]
    public int RequestID { get; set; }

    [Column("ParentUsername")]
    public string ParentUsername { get; set; } = "";

    [Column("StudentID")]
    public int StudentID { get; set; }

    [Column("RequestType")]
    public string RequestType { get; set; } = "";

    [Column("Details")]
    public string? Details { get; set; }

    [Column("Status")]
    public string Status { get; set; } = "Pending";

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
