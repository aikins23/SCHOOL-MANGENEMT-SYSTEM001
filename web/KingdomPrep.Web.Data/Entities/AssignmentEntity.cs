using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Assignments")]
public class AssignmentEntity
{
    [Key]
    [Column("AssignmentID")]
    public int AssignmentID { get; set; }

    [Column("Title")]
    public string Title { get; set; } = "";

    [Column("Description")]
    public string? Description { get; set; }

    [Column("ClassID")]
    public string ClassID { get; set; } = "";

    [Column("Subject")]
    public string Subject { get; set; } = "";

    [Column("DueDate")]
    public DateTime DueDate { get; set; }

    [Column("TeacherID")]
    public string? TeacherID { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
