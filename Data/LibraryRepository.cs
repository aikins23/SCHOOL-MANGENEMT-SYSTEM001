using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Books catalogue + lending. Issuing inserts a loan and decrements AvailableCopies;
    /// returning marks the loan returned and increments it. SQL Server (LocalDB) via OleDb.
    /// </summary>
    public class LibraryRepository : ILibraryRepository
    {
        private readonly string _connectionString;

        public LibraryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnsureTablesAsync()
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string books = @"IF OBJECT_ID(N'Books', N'U') IS NULL
                    CREATE TABLE Books (
                        BookId INT IDENTITY(1,1) PRIMARY KEY,
                        Title NVARCHAR(200) NOT NULL, Author NVARCHAR(150), ISBN NVARCHAR(30),
                        Category NVARCHAR(80), TotalCopies INT NOT NULL DEFAULT (1),
                        AvailableCopies INT NOT NULL DEFAULT (1), AddedDate DATETIME);";
                using (var cmd = new OleDbCommand(books, c)) await cmd.ExecuteNonQueryAsync();

                const string loans = @"IF OBJECT_ID(N'BookLoans', N'U') IS NULL
                    CREATE TABLE BookLoans (
                        LoanId INT IDENTITY(1,1) PRIMARY KEY,
                        BookId INT NOT NULL, BorrowerType NVARCHAR(20) NOT NULL,
                        BorrowerId NVARCHAR(50) NOT NULL, BorrowerName NVARCHAR(150),
                        IssueDate DATETIME NOT NULL, DueDate DATETIME NOT NULL,
                        ReturnDate DATETIME NULL, Status NVARCHAR(20) NOT NULL);";
                using (var cmd = new OleDbCommand(loans, c)) await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<Book>> GetBooksAsync(string search)
        {
            var list = new List<Book>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                string sql = "SELECT * FROM Books";
                bool hasSearch = !string.IsNullOrWhiteSpace(search);
                if (hasSearch) sql += " WHERE Title LIKE ? OR Author LIKE ? OR ISBN LIKE ? OR Category LIKE ?";
                sql += " ORDER BY Title";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    if (hasSearch)
                    {
                        string like = "%" + search.Trim() + "%";
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                        cmd.Parameters.AddWithValue("?", like);
                    }
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(MapBook(r));
                }
            }
            return list;
        }

        public async Task<int> AddBookAsync(Book b)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO Books (Title,Author,ISBN,Category,TotalCopies,AvailableCopies,AddedDate)
                    VALUES (?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", b.Title ?? "");
                    cmd.Parameters.AddWithValue("?", b.Author ?? "");
                    cmd.Parameters.AddWithValue("?", b.ISBN ?? "");
                    cmd.Parameters.AddWithValue("?", b.Category ?? "");
                    cmd.Parameters.AddWithValue("?", b.TotalCopies);
                    cmd.Parameters.AddWithValue("?", b.AvailableCopies);
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new OleDbCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                    }
                }
            }
        }

        public async Task UpdateBookAsync(Book b)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE Books SET Title=?,Author=?,ISBN=?,Category=?,
                    TotalCopies=?,AvailableCopies=? WHERE BookId=?";
                using (var cmd = new OleDbCommand(sql, c))
                {
                    cmd.Parameters.AddWithValue("?", b.Title ?? "");
                    cmd.Parameters.AddWithValue("?", b.Author ?? "");
                    cmd.Parameters.AddWithValue("?", b.ISBN ?? "");
                    cmd.Parameters.AddWithValue("?", b.Category ?? "");
                    cmd.Parameters.AddWithValue("?", b.TotalCopies);
                    cmd.Parameters.AddWithValue("?", b.AvailableCopies);
                    cmd.Parameters.AddWithValue("?", b.BookId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> DeleteBookAsync(int bookId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                int active;
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM BookLoans WHERE BookId=? AND Status='Active'", c))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    active = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                if (active > 0) return false;
                using (var cmd = new OleDbCommand("DELETE FROM Books WHERE BookId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task<(bool Ok, string Message)> IssueAsync(int bookId, string borrowerType, string borrowerId, string borrowerName, DateTime dueDate)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                int avail;
                using (var cmd = new OleDbCommand("SELECT AvailableCopies FROM Books WHERE BookId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    var v = await cmd.ExecuteScalarAsync();
                    avail = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                }
                if (avail < 1) return (false, "No copies available for that book.");

                const string ins = @"INSERT INTO BookLoans (BookId,BorrowerType,BorrowerId,BorrowerName,IssueDate,DueDate,ReturnDate,Status)
                    VALUES (?,?,?,?,?,?,?,?)";
                using (var cmd = new OleDbCommand(ins, c))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    cmd.Parameters.AddWithValue("?", borrowerType ?? "");
                    cmd.Parameters.AddWithValue("?", borrowerId ?? "");
                    cmd.Parameters.AddWithValue("?", borrowerName ?? "");
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(dueDate));
                    cmd.Parameters.Add("?", OleDbType.DBTimeStamp).Value = DBNull.Value;
                    cmd.Parameters.AddWithValue("?", "Active");
                    await cmd.ExecuteNonQueryAsync();
                }
                using (var cmd = new OleDbCommand("UPDATE Books SET AvailableCopies=AvailableCopies-1 WHERE BookId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return (true, "Book issued.");
            }
        }

        public async Task ReturnAsync(int loanId)
        {
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                int bookId;
                using (var cmd = new OleDbCommand("SELECT BookId FROM BookLoans WHERE LoanId=? AND Status='Active'", c))
                {
                    cmd.Parameters.AddWithValue("?", loanId);
                    var v = await cmd.ExecuteScalarAsync();
                    if (v == null || v == DBNull.Value) return; // not active / already returned
                    bookId = Convert.ToInt32(v);
                }
                using (var cmd = new OleDbCommand("UPDATE BookLoans SET ReturnDate=?, Status='Returned' WHERE LoanId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", TruncateSeconds(DateTime.Now));
                    cmd.Parameters.AddWithValue("?", loanId);
                    await cmd.ExecuteNonQueryAsync();
                }
                using (var cmd = new OleDbCommand("UPDATE Books SET AvailableCopies=AvailableCopies+1 WHERE BookId=?", c))
                {
                    cmd.Parameters.AddWithValue("?", bookId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<BookLoan>> GetActiveLoansAsync()
        {
            var list = new List<BookLoan>();
            using (var c = new OleDbConnection(_connectionString))
            {
                await c.OpenAsync();
                const string sql = @"SELECT l.*, b.Title AS BookTitle FROM BookLoans l
                    INNER JOIN Books b ON l.BookId=b.BookId WHERE l.Status='Active' ORDER BY l.DueDate";
                using (var cmd = new OleDbCommand(sql, c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(MapLoan(r));
            }
            return list;
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
        private static int I(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);

        private static Book MapBook(IDataRecord r) => new Book
        {
            BookId = I(r["BookId"]), Title = S(r["Title"]), Author = S(r["Author"]),
            ISBN = S(r["ISBN"]), Category = S(r["Category"]),
            TotalCopies = I(r["TotalCopies"]), AvailableCopies = I(r["AvailableCopies"]),
            AddedDate = r["AddedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["AddedDate"])
        };

        private static BookLoan MapLoan(IDataRecord r) => new BookLoan
        {
            LoanId = I(r["LoanId"]), BookId = I(r["BookId"]), BookTitle = S(r["BookTitle"]),
            BorrowerType = S(r["BorrowerType"]), BorrowerId = S(r["BorrowerId"]), BorrowerName = S(r["BorrowerName"]),
            IssueDate = Convert.ToDateTime(r["IssueDate"]), DueDate = Convert.ToDateTime(r["DueDate"]),
            ReturnDate = r["ReturnDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ReturnDate"]),
            Status = S(r["Status"])
        };
    }
}
