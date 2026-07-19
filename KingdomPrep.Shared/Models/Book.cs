using System;

namespace KingdomPrep.Shared.Models
{
    /// <summary>A catalogued book title; copies are tracked as counts.</summary>
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string ISBN { get; set; } = "";
        public string Category { get; set; } = "";
        public int TotalCopies { get; set; } = 1;
        public int AvailableCopies { get; set; } = 1;
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public override string ToString() => Title; // for ComboBox display
    }
}
