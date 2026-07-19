using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("SchoolInformation")]
public class SchoolInfoEntity
{
    public int Id { get; set; }
    public Guid SchoolId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PoBox { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public byte[]? Logo { get; set; }

    [Column("Latitude")]
    public double Latitude { get; set; } = 5.6037; // Default Accra latitude

    [Column("Longitude")]
    public double Longitude { get; set; } = -0.1870; // Default Accra longitude

    [Column("GeofenceRadiusMeters")]
    public double GeofenceRadiusMeters { get; set; } = 150.0; // Default 150 meters


    [NotMapped]
    public string Phones => string.Join(" | ", new[] { Phone1, Phone2 }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
