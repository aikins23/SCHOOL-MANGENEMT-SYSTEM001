using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Expenses")]
public class ExpenseEntity
{
    [Key]
    [Column("ID")]
    public int ID { get; set; }

    [Column("Expenses_name")]
    public string ExpenseName { get; set; } = "";

    [Column("Purpose")]
    public string Purpose { get; set; } = "";

    [Column("Description")]
    public string? Description { get; set; }

    [Column("Date_Time")]
    public DateTime Date { get; set; } = DateTime.Now;

    [Column("Amount")]
    public string Amount { get; set; } = "0";

    [Column("Payee")]
    public string? Payee { get; set; }

    [Column("payer")]
    public string? Payer { get; set; }

    [Column("SchoolId")]
    public Guid? SchoolId { get; set; }
}
