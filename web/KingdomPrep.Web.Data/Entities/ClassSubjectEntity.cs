using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ClassSubjects")]
public class ClassSubjectEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClassName")]
    public string? ClassName { get; set; }

    [Column("Subject")]
    public string? Subject { get; set; }

    [Column("SortOrder")]
    public int? SortOrder { get; set; }

    [Column("SyncId")]
    public Guid? SyncId { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
