# Library & Book Lending — Design

**Date:** 2026-06-06
**Status:** Approved (pending spec review)
**Author:** Buabeng Emmanuel Aikins (with Claude)

## Problem

The school has no way to catalogue books or track who has borrowed them. This is a net-new
module (no existing library/asset code).

## Goal

Let Director/Administrator/Headmaster maintain a book catalogue and issue/return books to
students and staff with due-date and overdue tracking — a self-contained module that follows the
app's existing data/UI patterns.

## Scope

In scope (MVP):
- Book catalogue: add/edit/delete/search books with copy counts.
- Lending: issue a copy to a **student** or **staff** member, with an issue date and due date;
  return a copy; copy availability adjusts automatically.
- Overdue **display** (loans past their due date highlighted) — informational only.

Out of scope (explicitly, per decisions):
- **Fines** of any kind and any link to the fee/financial record (`payment_record`/`fees`).
- Per-physical-copy tracking (condition, barcode per copy) — copies are tracked as counts.
- Reservations/holds, ISBN lookup from an online service, reports/exports.

## Approach (chosen)

Two tables (`Books`, `BookLoans`) + one cohesive `LibraryRepository` + one tabbed `frmLibrary`
form. One repository keeps the two-step operations atomic-by-intent (issue = insert loan +
decrement available copies; return = mark returned + increment). Rejected: separate
catalog/issue forms (nav clutter for an MVP); per-copy rows (heavier than needed).

## Data model

```sql
IF OBJECT_ID(N'Books', N'U') IS NULL
CREATE TABLE Books (
    BookId          INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(200) NOT NULL,
    Author          NVARCHAR(150),
    ISBN            NVARCHAR(30),
    Category        NVARCHAR(80),
    TotalCopies     INT NOT NULL DEFAULT (1),
    AvailableCopies INT NOT NULL DEFAULT (1),
    AddedDate       DATETIME
);

IF OBJECT_ID(N'BookLoans', N'U') IS NULL
CREATE TABLE BookLoans (
    LoanId       INT IDENTITY(1,1) PRIMARY KEY,
    BookId       INT NOT NULL,
    BorrowerType NVARCHAR(20) NOT NULL,   -- 'Student' | 'Staff'
    BorrowerId   NVARCHAR(50) NOT NULL,   -- numeric student id or employee id (stored as text)
    BorrowerName NVARCHAR(150),           -- snapshot at issue time
    IssueDate    DATETIME NOT NULL,
    DueDate      DATETIME NOT NULL,
    ReturnDate   DATETIME NULL,
    Status       NVARCHAR(20) NOT NULL    -- 'Active' | 'Returned'
);
```

`DateTime` parameters are truncated to whole seconds before binding (MSOLEDBSQL `datetime`
caveat — same `TruncateSeconds` helper used elsewhere). Tables created idempotently in
`EnsureTablesAsync` (`IF OBJECT_ID … CREATE TABLE`). No DB-level FK (consistent with the rest of
the app's loose tables); referential integrity is enforced in code.

## Components

### `Models/Book.cs` (new)
`BookId (int), Title, Author, ISBN, Category, TotalCopies (int), AvailableCopies (int),
AddedDate (DateTime)`.

### `Models/BookLoan.cs` (new)
`LoanId (int), BookId (int), BookTitle (string, joined for display), BorrowerType, BorrowerId,
BorrowerName, IssueDate, DueDate, ReturnDate (DateTime?), Status`. A computed
`bool IsOverdue => Status == "Active" && ReturnDate == null && DueDate.Date < DateTime.Today`.

### `Data/ILibraryRepository.cs` + `Data/LibraryRepository.cs` (new)
- `Task EnsureTablesAsync()` — create both tables.
- Books: `Task<List<Book>> GetBooksAsync(string search)`, `Task<int> AddBookAsync(Book)`,
  `Task UpdateBookAsync(Book)`, `Task<bool> DeleteBookAsync(int bookId)` (rejects if the book
  has any Active loan).
- Loans: `Task<(bool Ok, string Message)> IssueAsync(int bookId, string borrowerType,
  string borrowerId, string borrowerName, DateTime dueDate)` — verifies `AvailableCopies > 0`,
  inserts the loan, decrements `AvailableCopies` (both statements on one open connection);
  `Task ReturnAsync(int loanId)` — sets `ReturnDate`/`Status='Returned'` and increments the
  book's `AvailableCopies`; `Task<List<BookLoan>> GetActiveLoansAsync()` (joined to Book title,
  ordered by DueDate). OleDb, `AppConfig.ConnectionString`, mirrors existing repositories.

### `frmLibrary.cs` (new — one form, `TabControl` with two tabs)
- **Catalog tab:** a search box + `DataGridView` of books (Title, Author, ISBN, Category,
  Total, Available); **Add** / **Edit** (a small input dialog for the book fields) / **Delete**
  (blocked with a message if the book has active loans) / **Refresh**.
- **Loans tab:** an **Issue** panel — borrower type (Student/Staff radio or combo), Borrower ID
  textbox (accepts `KPS` prefix via `StudentId.Parse`), a **Look up** that fills the borrower
  name from `StudentService.GetStudentAsync` (Student) or the employee table (Staff), a book
  picker (combo of titles with available copies > 0), a due-date picker defaulting to
  `Today + 14 days`, and an **Issue** button. Below it, a `DataGridView` of **active loans**
  (Book, Borrower, Issued, Due, status) with a **Return** button; overdue rows
  (`IsOverdue`) shown with a red fore/own-row style.
- Constructor: `BuildUi(); if (!AuthService.RequireAccess("frmLibrary", this)) return; Load += …`.
- Themed with `AppConfig.Colors`/`UiTheme`; verified offline via the render harness.

### RBAC + navigation
- `Services/AuthService.cs`: `["frmLibrary"] = { Director, Administrator, Headmaster }`.
- `frmDashboard.cs`: a "Library" nav entry in the Director/Admin/Headmaster settings group.

## Data flow

1. Catalogue: admin adds books (TotalCopies = AvailableCopies at creation). Editing TotalCopies
   adjusts AvailableCopies by the same delta (never below 0 or below issued count).
2. Issue: pick borrower + book + due date → repo verifies a copy is available → inserts an
   Active loan and decrements AvailableCopies.
3. Return: select an active loan → repo marks it Returned (ReturnDate = today) and increments
   AvailableCopies.
4. Overdue: active loans with `DueDate < today` are highlighted in the active-loans grid.

## Error handling / edge cases

- Issue with no available copy → blocked with a clear message.
- Delete a book that has Active loans → blocked.
- Borrower ID that doesn't resolve to a student/staff → warn and don't issue (name required).
- DB/load errors → caught, shown via `UIHelper`, logged via `LoggerHelper`; the rest of the app
  is unaffected (Library is isolated).
- Returning a loan that's already returned → no-op.

## Testing / verification

- `dotnet build -clp:ErrorsOnly -nologo` → 0 errors.
- Reflection probe: `EnsureTablesAsync`, add a book, issue (Available decremented), return
  (Available restored), overdue detection on a back-dated due date.
- Render harness: `frmLibrary` (both tabs render).
- Manual (user): add a book with 3 copies; issue 1 to a student (KPS id) and 1 to a staff member
  → Available shows 1; a back-dated due date highlights as overdue; Return restores the copy;
  deleting a book with an active loan is blocked.

## Success criteria

- Director/Administrator/Headmaster can catalogue books and issue/return them to students and
  staff, with availability and overdue status tracked.
- No fines and no changes to the fee/financial tables.
- The module is self-contained and never breaks the rest of the app if its tables are missing
  (created on first use).
