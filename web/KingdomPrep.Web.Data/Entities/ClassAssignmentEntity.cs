using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("ClassAssignments")]
public class ClassAssignmentEntity
{
    [Key]
    [Column("ClassName")]
    public string ClassName { get; set; } = "";

    [Column("ClassTeacherID")]
    public int? ClassTeacherID { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
