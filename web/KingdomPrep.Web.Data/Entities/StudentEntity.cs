using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Students")]
public class StudentEntity
{
    [Column("StudentID")] public string StudentID { get; set; } = "";
    [Column("ClassID")] public string? ClassID { get; set; }
}
