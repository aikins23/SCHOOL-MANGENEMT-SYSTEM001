using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Users")]
public class UserEntity
{
    [Column("Username")] public string Username { get; set; } = "";
    [Column("Password")] public string Password { get; set; } = "";
    [Column("User_Type")] public string? UserType { get; set; }
    [Column("EmploymentID")] public int? EmploymentID { get; set; }
}
