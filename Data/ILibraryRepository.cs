using System.Collections.Generic;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    public interface ILibraryRepository
    {
        Task EnsureTablesAsync();
        Task<List<Book>> GetBooksAsync(string search);
        Task<int> AddBookAsync(Book book);
        Task UpdateBookAsync(Book book);
        Task<bool> DeleteBookAsync(int bookId);
        Task<(bool Ok, string Message)> IssueAsync(int bookId, string borrowerType, string borrowerId, string borrowerName, System.DateTime dueDate);
        Task ReturnAsync(int loanId);
        Task<List<BookLoan>> GetActiveLoansAsync();
    }
}
