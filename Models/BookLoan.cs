using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A single issue of a book copy to a student or staff member.</summary>
    public class BookLoan
    {
        public int LoanId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";   // joined for display
        public string BorrowerType { get; set; } = ""; // "Student" | "Staff" | "Class"
        public string BorrowerId { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public int Quantity { get; set; } = 1;          // copies on this loan (Class loans can be many)
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "Active";  // "Active" | "Returned"

        public bool IsOverdue =>
            Status == "Active" && ReturnDate == null && DueDate.Date < DateTime.Today;
    }
}
