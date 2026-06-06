# Library & Book Lending Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A self-contained library module — catalogue books and issue/return them to students and staff with due-date and overdue tracking (no fines).

**Architecture:** Two tables (`Books`, `BookLoans`) + one `LibraryRepository` + one tabbed `frmLibrary` (Catalog / Loans). Mirrors the app's existing OleDb repository + WinForms patterns. RBAC + a dashboard nav entry. No changes to fee/financial tables.

**Tech Stack:** C#/.NET Framework 4.7.2, WinForms, `System.Data.OleDb`.

**Spec:** `docs/superpowers/specs/2026-06-06-library-lending-design.md`

---

## Conventions (read first)

- **No unit-test framework.** Gates: (1) `dotnet build -clp:ErrorsOnly -nologo` → 0 errors; (2) reflection probe for repo logic (needs LocalDB — defer if it won't start); (3) render harness for the form.
- **Explicit-include csproj:** every new `.cs` needs a `<Compile Include="..." />` entry. Code-only forms use a self-closing entry; anchor on `frmSubjects.cs`.
- **`datetime` caveat:** truncate `DateTime` params to whole seconds before binding (`TruncateSeconds`).
- Lookups: `new StudentService(new StudentRepository(cs), new FeeRepository(cs)).GetStudentAsync(id)` → `Student` (`.FullName`); `new EmployeeRepository(cs).GetByIdAsync(id)` → `Employee` (`.FullName`). `Common.StudentId.Parse` strips the `KPS` prefix.
- Commit footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File structure

- **Create** `Models/Book.cs`, `Models/BookLoan.cs`
- **Create** `Data/ILibraryRepository.cs`, `Data/LibraryRepository.cs`
- **Create** `frmLibrary.cs`
- **Modify** `Services/AuthService.cs`, `frmDashboard.cs`, `kingdom_Preparatory_School_Management_System.csproj`

---

### Task 1: Models

**Files:**
- Create: `Models/Book.cs`, `Models/BookLoan.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Models/Book.cs`**

```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
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
```

- [ ] **Step 2: Create `Models/BookLoan.cs`**

```csharp
using System;

namespace kingdom_Preparatory_School_Management_System.Models
{
    /// <summary>A single issue of a book copy to a student or staff member.</summary>
    public class BookLoan
    {
        public int LoanId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";   // joined for display
        public string BorrowerType { get; set; } = ""; // "Student" | "Staff"
        public string BorrowerId { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "Active";  // "Active" | "Returned"

        public bool IsOverdue =>
            Status == "Active" && ReturnDate == null && DueDate.Date < DateTime.Today;
    }
}
```

- [ ] **Step 3: Register in csproj**

After `<Compile Include="Models\GradeBand.cs" />` add:

```xml
    <Compile Include="Models\Book.cs" />
    <Compile Include="Models\BookLoan.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Models/Book.cs Models/BookLoan.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(library): Book and BookLoan models"
```

---

### Task 2: `LibraryRepository`

**Files:**
- Create: `Data/ILibraryRepository.cs`, `Data/LibraryRepository.cs`
- Modify: `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Create `Data/ILibraryRepository.cs`**

```csharp
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
```

- [ ] **Step 2: Create `Data/LibraryRepository.cs`**

```csharp
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
```

- [ ] **Step 3: Register in csproj**

After `<Compile Include="Data\SubjectRepository.cs" />` add:

```xml
    <Compile Include="Data\ILibraryRepository.cs" />
    <Compile Include="Data\LibraryRepository.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Data/ILibraryRepository.cs Data/LibraryRepository.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(library): LibraryRepository (catalogue + issue/return + active loans)"
```

---

### Task 3: RBAC + `frmLibrary`

**Files:**
- Create: `frmLibrary.cs`
- Modify: `Services/AuthService.cs`, `kingdom_Preparatory_School_Management_System.csproj`

- [ ] **Step 1: Add RBAC entry**

In `Services/AuthService.cs`, after `["frmSubjects"] = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },` add:

```csharp
            ["frmLibrary"]                = new[] { UserRole.Director, UserRole.Administrator, UserRole.Headmaster },
```

- [ ] **Step 2: Create `frmLibrary.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Library: catalogue books and issue/return them to students and staff (no fines).
    /// Director / Administrator / Headmaster.
    /// </summary>
    public class frmLibrary : Form
    {
        private readonly LibraryRepository _repo = new LibraryRepository(AppConfig.ConnectionString);
        private readonly StudentService _students =
            new StudentService(new StudentRepository(AppConfig.ConnectionString), new FeeRepository(AppConfig.ConnectionString));
        private readonly EmployeeRepository _employees = new EmployeeRepository(AppConfig.ConnectionString);

        private DataGridView _booksGrid, _loansGrid;
        private TextBox _bookSearch, _borrowerId;
        private Label _borrowerName, _status;
        private ComboBox _borrowerType, _bookCombo;
        private DateTimePicker _dueDate;

        public frmLibrary()
        {
            BuildUi();
            if (!AuthService.RequireAccess("frmLibrary", this)) return;
            Load += async (s, e) => await InitAsync();
        }

        private void BuildUi()
        {
            Text = "Library";
            Size = new Size(940, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppConfig.Colors.PageBackColor;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 44, Text = "  Library",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = AppConfig.Colors.PrimaryColor, TextAlign = ContentAlignment.MiddleLeft
            };
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildCatalogTab());
            tabs.TabPages.Add(BuildLoansTab());
            _status = new Label { Dock = DockStyle.Bottom, Height = 24, ForeColor = AppConfig.Colors.MutedTextColor, Padding = new Padding(12, 0, 0, 0) };

            Controls.Add(tabs);
            Controls.Add(_status);
            Controls.Add(title);
        }

        // ── Catalog tab ──────────────────────────────────────────────────────────
        private TabPage BuildCatalogTab()
        {
            var tab = new TabPage("Catalog") { BackColor = Color.White };
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
            _bookSearch = new TextBox { Dock = DockStyle.Left, Width = 280, Font = new Font("Segoe UI", 10F) };
            _bookSearch.TextChanged += async (s, e) => await LoadBooksAsync();
            var addBtn = new Button { Dock = DockStyle.Right, Width = 90, Text = "Add", FlatStyle = FlatStyle.Flat };
            var editBtn = new Button { Dock = DockStyle.Right, Width = 90, Text = "Edit", FlatStyle = FlatStyle.Flat };
            var delBtn = new Button { Dock = DockStyle.Right, Width = 90, Text = "Delete", FlatStyle = FlatStyle.Flat };
            addBtn.Click += async (s, e) => { var b = EditBookDialog(null); if (b != null) { await _repo.AddBookAsync(b); await LoadBooksAsync(); await LoadBookComboAsync(); } };
            editBtn.Click += async (s, e) => await EditSelectedBookAsync();
            delBtn.Click += async (s, e) => await DeleteSelectedBookAsync();
            bar.Controls.Add(_bookSearch); bar.Controls.Add(delBtn); bar.Controls.Add(editBtn); bar.Controls.Add(addBtn);

            _booksGrid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
            };
            tab.Controls.Add(_booksGrid);
            tab.Controls.Add(bar);
            return tab;
        }

        // ── Loans tab ────────────────────────────────────────────────────────────
        private TabPage BuildLoansTab()
        {
            var tab = new TabPage("Loans") { BackColor = Color.White };
            var issue = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(8), BackColor = Color.White };
            _borrowerType = new ComboBox { Left = 8, Top = 10, Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _borrowerType.Items.AddRange(new object[] { "Student", "Staff" }); _borrowerType.SelectedIndex = 0;
            _borrowerId = new TextBox { Left = 124, Top = 11, Width = 130, Font = new Font("Segoe UI", 10F) };
            var lookupBtn = new Button { Left = 260, Top = 9, Width = 80, Height = 26, Text = "Look up", FlatStyle = FlatStyle.Flat };
            _borrowerName = new Label { Left = 348, Top = 13, Width = 220, Text = "", ForeColor = AppConfig.Colors.TextColor };
            lookupBtn.Click += async (s, e) => await LookupBorrowerAsync();

            var bookLbl = new Label { Left = 8, Top = 50, Width = 40, Text = "Book:", Top2() };
            _bookCombo = new ComboBox { Left = 56, Top = 47, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            _dueDate = new DateTimePicker { Left = 364, Top = 47, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(14) };
            var issueBtn = new Button { Left = 502, Top = 45, Width = 100, Height = 28, Text = "Issue", BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            issueBtn.Click += async (s, e) => await IssueAsync();

            issue.Controls.AddRange(new Control[] { _borrowerType, _borrowerId, lookupBtn, _borrowerName, bookLbl, _bookCombo, _dueDate, issueBtn });

            var returnBtn = new Button { Dock = DockStyle.Bottom, Height = 34, Text = "Return Selected", FlatStyle = FlatStyle.Flat };
            returnBtn.Click += async (s, e) => await ReturnSelectedAsync();

            _loansGrid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White
            };

            tab.Controls.Add(_loansGrid);
            tab.Controls.Add(returnBtn);
            tab.Controls.Add(issue);
            return tab;
        }

        private async Task InitAsync()
        {
            try
            {
                await _repo.EnsureTablesAsync();
                await LoadBooksAsync();
                await LoadBookComboAsync();
                await LoadLoansAsync();
            }
            catch (Exception ex) { UIHelper.ShowError("Could not load library: " + ex.Message, "Library"); }
        }

        private async Task LoadBooksAsync()
        {
            var books = await _repo.GetBooksAsync(_bookSearch.Text);
            _booksGrid.DataSource = books.Select(b => new { b.BookId, b.Title, b.Author, b.ISBN, b.Category, Total = b.TotalCopies, Available = b.AvailableCopies }).ToList();
            if (_booksGrid.Columns.Contains("BookId")) _booksGrid.Columns["BookId"].Visible = false;
            _status.Text = books.Count + " book(s).";
        }

        private async Task LoadBookComboAsync()
        {
            var books = await _repo.GetBooksAsync(null);
            _bookCombo.DisplayMember = "Title"; _bookCombo.ValueMember = "BookId";
            _bookCombo.DataSource = books.Where(b => b.AvailableCopies > 0).ToList();
        }

        private async Task LoadLoansAsync()
        {
            var loans = await _repo.GetActiveLoansAsync();
            _loansGrid.DataSource = loans.Select(l => new {
                l.LoanId, Book = l.BookTitle, Borrower = l.BorrowerName + " (" + l.BorrowerType + ")",
                Issued = l.IssueDate.ToString("dd/MM/yyyy"), Due = l.DueDate.ToString("dd/MM/yyyy"),
                Overdue = l.IsOverdue ? "OVERDUE" : ""
            }).ToList();
            if (_loansGrid.Columns.Contains("LoanId")) _loansGrid.Columns["LoanId"].Visible = false;
            // red rows for overdue
            foreach (DataGridViewRow row in _loansGrid.Rows)
                if ((row.Cells["Overdue"].Value?.ToString() ?? "") == "OVERDUE")
                    row.DefaultCellStyle.ForeColor = AppConfig.Colors.DangerColor;
        }

        private async Task LookupBorrowerAsync()
        {
            string id = StudentId.Parse(_borrowerId.Text);
            if (string.IsNullOrWhiteSpace(id)) { _borrowerName.Text = ""; return; }
            try
            {
                if (_borrowerType.SelectedItem?.ToString() == "Student")
                {
                    var st = await _students.GetStudentAsync(id);
                    _borrowerName.Text = st != null ? st.FullName : "(not found)";
                }
                else
                {
                    var emp = await _employees.GetByIdAsync(id);
                    _borrowerName.Text = emp != null ? emp.FullName : "(not found)";
                }
            }
            catch (Exception ex) { UIHelper.ShowError("Lookup failed: " + ex.Message, "Library"); }
        }

        private async Task IssueAsync()
        {
            if (_bookCombo.SelectedItem is Book book == false || book == null)
            { UIHelper.ShowWarning("Pick a book with available copies.", "Library"); return; }
            string id = StudentId.Parse(_borrowerId.Text);
            string name = _borrowerName.Text;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || name.StartsWith("("))
            { UIHelper.ShowWarning("Look up a valid borrower first.", "Library"); return; }
            var res = await _repo.IssueAsync(book.BookId, _borrowerType.SelectedItem.ToString(), id, name, _dueDate.Value.Date);
            if (!res.Ok) { UIHelper.ShowWarning(res.Message, "Library"); return; }
            _borrowerId.Text = ""; _borrowerName.Text = "";
            await LoadBooksAsync(); await LoadBookComboAsync(); await LoadLoansAsync();
            UIHelper.ShowSuccess("Book issued.", "Library");
        }

        private async Task ReturnSelectedAsync()
        {
            if (_loansGrid.CurrentRow?.Cells["LoanId"].Value == null) { UIHelper.ShowWarning("Select a loan to return.", "Library"); return; }
            int loanId = Convert.ToInt32(_loansGrid.CurrentRow.Cells["LoanId"].Value);
            await _repo.ReturnAsync(loanId);
            await LoadBooksAsync(); await LoadBookComboAsync(); await LoadLoansAsync();
            UIHelper.ShowSuccess("Book returned.", "Library");
        }

        private async Task EditSelectedBookAsync()
        {
            var existing = SelectedBook();
            if (existing == null) { UIHelper.ShowWarning("Select a book to edit.", "Library"); return; }
            var edited = EditBookDialog(existing);
            if (edited == null) return;
            int issued = existing.TotalCopies - existing.AvailableCopies;
            edited.BookId = existing.BookId;
            edited.AvailableCopies = Math.Max(0, edited.TotalCopies - issued);
            await _repo.UpdateBookAsync(edited);
            await LoadBooksAsync(); await LoadBookComboAsync();
        }

        private async Task DeleteSelectedBookAsync()
        {
            var b = SelectedBook();
            if (b == null) { UIHelper.ShowWarning("Select a book to delete.", "Library"); return; }
            if (UIHelper.ShowConfirmation($"Delete \"{b.Title}\"?", "Library") != DialogResult.Yes) return;
            bool ok = await _repo.DeleteBookAsync(b.BookId);
            if (!ok) { UIHelper.ShowWarning("Cannot delete: the book has active loans.", "Library"); return; }
            await LoadBooksAsync(); await LoadBookComboAsync();
        }

        private Book SelectedBook()
        {
            if (_booksGrid.CurrentRow?.Cells["BookId"].Value == null) return null;
            int id = Convert.ToInt32(_booksGrid.CurrentRow.Cells["BookId"].Value);
            return new Book
            {
                BookId = id,
                Title = _booksGrid.CurrentRow.Cells["Title"].Value?.ToString() ?? "",
                Author = _booksGrid.CurrentRow.Cells["Author"].Value?.ToString() ?? "",
                ISBN = _booksGrid.CurrentRow.Cells["ISBN"].Value?.ToString() ?? "",
                Category = _booksGrid.CurrentRow.Cells["Category"].Value?.ToString() ?? "",
                TotalCopies = Convert.ToInt32(_booksGrid.CurrentRow.Cells["Total"].Value ?? 0),
                AvailableCopies = Convert.ToInt32(_booksGrid.CurrentRow.Cells["Available"].Value ?? 0)
            };
        }

        // Small modal dialog for add/edit; returns null on cancel.
        private Book EditBookDialog(Book existing)
        {
            var dlg = new Form { Text = existing == null ? "Add Book" : "Edit Book", Size = new Size(380, 320), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            TextBox Mk(string label, int y, string val) { var l = new Label { Left = 14, Top = y + 3, Width = 90, Text = label }; var t = new TextBox { Left = 110, Top = y, Width = 230, Text = val ?? "" }; dlg.Controls.Add(l); dlg.Controls.Add(t); return t; }
            var t1 = Mk("Title", 16, existing?.Title);
            var t2 = Mk("Author", 52, existing?.Author);
            var t3 = Mk("ISBN", 88, existing?.ISBN);
            var t4 = Mk("Category", 124, existing?.Category);
            var lc = new Label { Left = 14, Top = 163, Width = 90, Text = "Total copies" };
            var nc = new NumericUpDown { Left = 110, Top = 160, Width = 80, Minimum = 1, Maximum = 1000, Value = existing == null ? 1 : Math.Max(1, existing.TotalCopies) };
            dlg.Controls.Add(lc); dlg.Controls.Add(nc);
            var ok = new Button { Left = 110, Top = 210, Width = 100, Height = 32, Text = "Save", DialogResult = DialogResult.OK, BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Left = 222, Top = 210, Width = 100, Height = 32, Text = "Cancel", DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
            dlg.Controls.Add(ok); dlg.Controls.Add(cancel); dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return null;
            if (string.IsNullOrWhiteSpace(t1.Text)) { UIHelper.ShowWarning("Title is required.", "Library"); return null; }
            int total = (int)nc.Value;
            return new Book { Title = t1.Text.Trim(), Author = t2.Text.Trim(), ISBN = t3.Text.Trim(), Category = t4.Text.Trim(), TotalCopies = total, AvailableCopies = total };
        }
    }
}
```

> Note: the `bookLbl` line uses a placeholder helper `Top2()` that does not exist — replace
> `var bookLbl = new Label { Left = 8, Top = 50, Width = 40, Text = "Book:", Top2() };`
> with `var bookLbl = new Label { Left = 8, Top = 50, Width = 44, Text = "Book:", TextAlign = ContentAlignment.MiddleLeft };`
> (This is the single intentional fix to apply while typing the file — do not copy the `Top2()` token.)

- [ ] **Step 3: Register in csproj**

After the self-closing `<Compile Include="frmSubjects.cs" />` add:

```xml
    <Compile Include="frmLibrary.cs" />
```

- [ ] **Step 4: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors. (If `EmployeeRepository`/`StudentRepository`/`StudentService`/`FeeRepository`
constructors differ, match their actual signatures — all are `(connectionString)` except
`StudentService(IStudentRepository, IFeeRepository)`, confirmed in the repo.)

- [ ] **Step 5: Render-verify (offline harness)**

Render `frmLibrary` (Director). Confirm both tabs: Catalog (search + grid + Add/Edit/Delete),
Loans (borrower type/id/lookup, book combo, due date, Issue; active-loans grid + Return).

- [ ] **Step 6: Commit**

```bash
git add frmLibrary.cs Services/AuthService.cs kingdom_Preparatory_School_Management_System.csproj
git commit -m "feat(library): frmLibrary (catalog + issue/return) + RBAC"
```

---

### Task 4: Dashboard nav entry

**Files:**
- Modify: `frmDashboard.cs`

- [ ] **Step 1: Add the nav button**

In `frmDashboard.cs`, after `nav.Controls.Add(CreateNavButton("Subjects", () => new frmSubjects().ShowDialog()));` add:

```csharp
                nav.Controls.Add(CreateNavButton("Library", () => new frmLibrary().ShowDialog()));
```

- [ ] **Step 2: Build clean**

Run: `dotnet build -clp:ErrorsOnly -nologo`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add frmDashboard.cs
git commit -m "feat(library): add Library to dashboard settings nav"
```

---

## Final verification

- [ ] `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- [ ] **User smoke test (running app):**
  1. Director/Admin/Headmaster → dashboard → **Library**.
  2. Catalog → **Add** a book (3 copies) → it appears, Available = 3.
  3. Loans → Student, enter a KPS id → **Look up** fills the name → pick the book, Issue → Available = 2, loan listed.
  4. Issue another to a Staff id; back-date the due date → the loan row shows **OVERDUE** in red.
  5. **Return Selected** → Available goes back up; the loan leaves the active list.
  6. Try to **Delete** a book with an active loan → blocked with a message.

## Self-review notes

- **Spec coverage:** Books + BookLoans tables (Task 2) ✓; models (Task 1) ✓; repository catalogue
  CRUD + issue/return + active loans + delete-guard (Task 2) ✓; frmLibrary catalog + loans tabs,
  borrower lookup (student/staff, KPS-aware), due date default +14, overdue red highlight (Task 3) ✓;
  RBAC Director/Admin/Headmaster (Task 3) ✓; dashboard nav (Task 4) ✓; no fines / no fee-table
  changes ✓.
- **Type/name consistency:** `LibraryRepository` methods (`EnsureTablesAsync/GetBooksAsync/
  AddBookAsync/UpdateBookAsync/DeleteBookAsync/IssueAsync/ReturnAsync/GetActiveLoansAsync`) match
  the interface and the form's calls; `Book`/`BookLoan` property names match the mappers and grids;
  lookup uses confirmed `GetStudentAsync`/`GetByIdAsync`.
- **Placeholder note:** Task 3 Step 2 contains ONE deliberate bad token (`Top2()`) with an explicit
  inline correction immediately below it — the executor must apply that fix. Everything else is
  complete code.
