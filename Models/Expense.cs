using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A school expenditure row (maps to the legacy Expenses table; Category = Purpose).</summary>
    public class Expense
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";      // -> Purpose column
        public string Description { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public decimal Amount { get; set; }
        public string Payee { get; set; } = "";
        public string Payer { get; set; } = "";
    }
}
