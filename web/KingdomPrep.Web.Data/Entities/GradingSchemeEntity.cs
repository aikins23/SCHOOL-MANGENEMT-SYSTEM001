using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("GradingScheme")]
public class GradingSchemeEntity
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("MinScore")]
    public int MinScore { get; set; }

    [Column("Code")]
    public string? Code { get; set; }

    [Column("Label")]
    public string? Label { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
