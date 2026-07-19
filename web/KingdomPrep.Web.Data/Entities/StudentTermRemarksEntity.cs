using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("StudentTermRemarks")]
public class StudentTermRemarksEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ID")]
    public int ID { get; set; }

    [Column("StudentID")]
    public string StudentID { get; set; } = "";

    [Column("Term")]
    public string Term { get; set; } = "";

    [Column("Year")]
    public string Year { get; set; } = "";

    [Column("Attitude")]
    public string? Attitude { get; set; }

    [Column("Interest")]
    public string? Interest { get; set; }

    [Column("Conduct")]
    public string? Conduct { get; set; }

    [Column("ClassTeacherRemarks")]
    public string? ClassTeacherRemarks { get; set; }

    [Column("HeadTeacherRemarks")]
    public string? HeadTeacherRemarks { get; set; }

    [Column("CreatedDate")]
    public DateTime CreatedDate { get; set; }


    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
