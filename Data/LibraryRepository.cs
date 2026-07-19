using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Common;

namespace kingdom_Preparatory_School_Management_System.Data
{
    /// <summary>
    /// Books catalogue + lending. Issuing inserts a loan and decrements AvailableCopies;
    /// returning marks the loan returned and increments it. SQL Server via Microsoft.Data.SqlClient.
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
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string books = @"IF OBJECT_ID(N'Books', N'U') IS NULL
                    CREATE TABLE Books (
                        BookId INT IDENTITY(1,1) PRIMARY KEY,
                        Title NVARCHAR(200) NOT NULL, Author NVARCHAR(150), ISBN NVARCHAR(30),
                        Category NVARCHAR(80), TotalCopies INT NOT NULL DEFAULT (1),
                        AvailableCopies INT NOT NULL DEFAULT (1), AddedDate DATETIME);";
                using (var cmd = new SqlCommand(books, c)) await cmd.ExecuteNonQueryAsync();

                const string loans = @"IF OBJECT_ID(N'BookLoans', N'U') IS NULL
                    CREATE TABLE BookLoans (
                        LoanId INT IDENTITY(1,1) PRIMARY KEY,
                        BookId INT NOT NULL, BorrowerType NVARCHAR(20) NOT NULL,
                        BorrowerId NVARCHAR(50) NOT NULL, BorrowerName NVARCHAR(150),
                        IssueDate DATETIME NOT NULL, DueDate DATETIME NOT NULL,
                        ReturnDate DATETIME NULL, Status NVARCHAR(20) NOT NULL);";
                using (var cmd = new SqlCommand(loans, c)) await cmd.ExecuteNonQueryAsync();

                // Quantity column (Class loans can be many copies) — added idempotently.
                const string addQty = @"IF COL_LENGTH('BookLoans','Quantity') IS NULL
                    ALTER TABLE BookLoans ADD Quantity INT NOT NULL CONSTRAINT DF_BookLoans_Qty DEFAULT (1);";
                using (var cmd = new SqlCommand(addQty, c)) await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<Book>> GetBooksAsync(string search)
        {
            var list = new List<Book>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                string sql = "SELECT * FROM Books";
                bool hasSearch = !string.IsNullOrWhiteSpace(search);
                if (hasSearch) sql += " WHERE Title LIKE ? OR Author LIKE ? OR ISBN LIKE ? OR Category LIKE ?";
                sql += " ORDER BY Title";
                using (var cmd = new SqlCommand(sql, c))
                {
                    if (hasSearch)
                    {
                        string like = "%" + search.Trim() + "%";
                        cmd.AddPositionalParameter(like);
                        cmd.AddPositionalParameter(like);
                        cmd.AddPositionalParameter(like);
                        cmd.AddPositionalParameter(like);
                    }
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync()) list.Add(MapBook(r));
                }
            }
            return list;
        }

        public async Task<int> AddBookAsync(Book b)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string sql = @"INSERT INTO Books (Title,Author,ISBN,Category,TotalCopies,AvailableCopies,AddedDate)
                    VALUES (?,?,?,?,?,?,?)";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(b.Title ?? "");
                    cmd.AddPositionalParameter(b.Author ?? "");
                    cmd.AddPositionalParameter(b.ISBN ?? "");
                    cmd.AddPositionalParameter(b.Category ?? "");
                    cmd.AddPositionalParameter(b.TotalCopies);
                    cmd.AddPositionalParameter(b.AvailableCopies);
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    await cmd.ExecuteNonQueryAsync();
                    using (var idc = new SqlCommand("SELECT @@IDENTITY", c))
                    {
                        var id = await idc.ExecuteScalarAsync();
                        var bookId = id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
                        await TryRecordSyncUpsertAsync("Books", "BookId", bookId, "Insert");
                        return bookId;
                    }
                }
            }
        }

        public async Task UpdateBookAsync(Book b)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string sql = @"UPDATE Books SET Title=?,Author=?,ISBN=?,Category=?,
                    TotalCopies=?,AvailableCopies=? WHERE BookId=?";
                using (var cmd = new SqlCommand(sql, c))
                {
                    cmd.AddPositionalParameter(b.Title ?? "");
                    cmd.AddPositionalParameter(b.Author ?? "");
                    cmd.AddPositionalParameter(b.ISBN ?? "");
                    cmd.AddPositionalParameter(b.Category ?? "");
                    cmd.AddPositionalParameter(b.TotalCopies);
                    cmd.AddPositionalParameter(b.AvailableCopies);
                    cmd.AddPositionalParameter(b.BookId);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows > 0) await TryRecordSyncUpsertAsync("Books", "BookId", b.BookId, "Update");
                }
            }
        }

        public async Task<bool> DeleteBookAsync(int bookId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                int active;
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM BookLoans WHERE BookId=? AND Status='Active'", c))
                {
                    cmd.AddPositionalParameter(bookId);
                    active = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                if (active > 0) return false;
                using (var cmd = new SqlCommand("DELETE FROM Books WHERE BookId=?", c))
                {
                    await TryRecordSyncDeleteAsync("Books", "BookId", bookId);
                    cmd.AddPositionalParameter(bookId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
        }

        public async Task<(bool Ok, string Message)> IssueAsync(int bookId, string borrowerType, string borrowerId, string borrowerName, DateTime dueDate, int quantity = 1)
        {
            if (quantity < 1) quantity = 1;
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                int avail;
                using (var cmd = new SqlCommand("SELECT AvailableCopies FROM Books WHERE BookId=?", c))
                {
                    cmd.AddPositionalParameter(bookId);
                    var v = await cmd.ExecuteScalarAsync();
                    avail = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                }
                if (avail < quantity)
                    return (false, $"Only {avail} cop{(avail == 1 ? "y" : "ies")} available for that book.");

                const string ins = @"INSERT INTO BookLoans (BookId,BorrowerType,BorrowerId,BorrowerName,IssueDate,DueDate,ReturnDate,Status,Quantity)
                    VALUES (?,?,?,?,?,?,?,?,?)";
                using (var cmd = new SqlCommand(ins, c))
                {
                    cmd.AddPositionalParameter(bookId);
                    cmd.AddPositionalParameter(borrowerType ?? "");
                    cmd.AddPositionalParameter(borrowerId ?? "");
                    cmd.AddPositionalParameter(borrowerName ?? "");
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    cmd.AddPositionalParameter(TruncateSeconds(dueDate));
                    cmd.AddPositionalParameter(DBNull.Value, SqlDbType.DateTime);
                    cmd.AddPositionalParameter("Active");
                    cmd.AddPositionalParameter(quantity);
                    await cmd.ExecuteNonQueryAsync();
                    var loanId = await GetLastIdentityAsync(c);
                    await TryRecordSyncUpsertAsync("BookLoans", "LoanId", loanId, "Insert");
                }
                using (var cmd = new SqlCommand("UPDATE Books SET AvailableCopies=AvailableCopies-? WHERE BookId=?", c))
                {
                    cmd.AddPositionalParameter(quantity);
                    cmd.AddPositionalParameter(bookId);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows > 0) await TryRecordSyncUpsertAsync("Books", "BookId", bookId, "Update");
                }
                return (true, "Book issued.");
            }
        }

        public async Task ReturnAsync(int loanId)
        {
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                int bookId, qty;
                using (var cmd = new SqlCommand("SELECT BookId, Quantity FROM BookLoans WHERE LoanId=? AND Status='Active'", c))
                {
                    cmd.AddPositionalParameter(loanId);
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        if (!await r.ReadAsync()) return; // not active / already returned
                        bookId = Convert.ToInt32(r["BookId"]);
                        qty = r["Quantity"] == DBNull.Value ? 1 : Convert.ToInt32(r["Quantity"]);
                    }
                }
                using (var cmd = new SqlCommand("UPDATE BookLoans SET ReturnDate=?, Status='Returned' WHERE LoanId=?", c))
                {
                    cmd.AddPositionalParameter(TruncateSeconds(DateTime.Now));
                    cmd.AddPositionalParameter(loanId);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows > 0) await TryRecordSyncUpsertAsync("BookLoans", "LoanId", loanId, "Update");
                }
                using (var cmd = new SqlCommand("UPDATE Books SET AvailableCopies=AvailableCopies+? WHERE BookId=?", c))
                {
                    cmd.AddPositionalParameter(qty);
                    cmd.AddPositionalParameter(bookId);
                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows > 0) await TryRecordSyncUpsertAsync("Books", "BookId", bookId, "Update");
                }
            }
        }

        public async Task<List<BookLoan>> GetActiveLoansAsync()
        {
            var list = new List<BookLoan>();
            using (var c = new SqlConnection(SqlCommandExtensions.StripProvider(_connectionString)))
            {
                await c.OpenAsync();
                const string sql = @"SELECT l.*, b.Title AS BookTitle FROM BookLoans l
                    INNER JOIN Books b ON l.BookId=b.BookId WHERE l.Status='Active' ORDER BY l.DueDate";
                using (var cmd = new SqlCommand(sql, c))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync()) list.Add(MapLoan(r));
            }
            return list;
        }

        private static DateTime TruncateSeconds(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second);

        private static string S(object o) => o == null || o == DBNull.Value ? "" : o.ToString();
        private static int I(object o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);

        private static async Task<object> GetLastIdentityAsync(SqlConnection connection)
        {
            using (var cmd = new SqlCommand("SELECT @@IDENTITY", connection))
            {
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? 0 : value;
            }
        }

        private async Task TryRecordSyncUpsertAsync(string tableName, string primaryKeyName, object primaryKeyValue, string operation)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordUpsertAsync(tableName, primaryKeyName, primaryKeyValue, operation);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Library sync capture skipped: " + ex.Message);
            }
        }

        private async Task TryRecordSyncDeleteAsync(string tableName, string primaryKeyName, object primaryKeyValue)
        {
            try
            {
                if (primaryKeyValue == null || string.IsNullOrWhiteSpace(Convert.ToString(primaryKeyValue))) return;
                await new SyncChangeRecorder(_connectionString).RecordDeleteAsync(tableName, primaryKeyName, primaryKeyValue);
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Library delete sync capture skipped: " + ex.Message);
            }
        }

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
            Status = S(r["Status"]),
            Quantity = HasColumn(r, "Quantity") && r["Quantity"] != DBNull.Value ? Convert.ToInt32(r["Quantity"]) : 1
        };

        private static bool HasColumn(IDataRecord r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
