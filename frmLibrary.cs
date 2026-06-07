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
        private Label _borrowerName, _status, _qtyLabel;
        private ComboBox _borrowerType, _bookCombo, _classCombo;
        private DateTimePicker _dueDate;
        private NumericUpDown _quantity;
        private Button _lookupBtn;

        public frmLibrary()
        {
            BuildUi();
            Common.SessionUi.AttachSignOut(this);
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
            _borrowerType.Items.AddRange(new object[] { "Student", "Staff", "Class" }); _borrowerType.SelectedIndex = 0;
            _borrowerType.SelectedIndexChanged += (s, e) => UpdateBorrowerMode();
            _borrowerId = new TextBox { Left = 124, Top = 11, Width = 130, Font = new Font("Segoe UI", 10F) };
            _lookupBtn = new Button { Left = 260, Top = 9, Width = 80, Height = 26, Text = "Look up", FlatStyle = FlatStyle.Flat };
            _borrowerName = new Label { Left = 348, Top = 13, Width = 220, Text = "", ForeColor = AppConfig.Colors.TextColor };
            _lookupBtn.Click += async (s, e) => await LookupBorrowerAsync();

            // Class borrowing: class picker + quantity (shown only when type == "Class").
            _classCombo = new ComboBox { Left = 124, Top = 11, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
            _classCombo.Items.AddRange(AppConfig.ClassNames.Cast<object>().ToArray());
            if (_classCombo.Items.Count > 0) _classCombo.SelectedIndex = 0;
            _qtyLabel = new Label { Left = 314, Top = 14, Width = 30, Text = "Qty:", Visible = false };
            _quantity = new NumericUpDown { Left = 346, Top = 10, Width = 70, Minimum = 1, Maximum = 1, Value = 1, Visible = false };

            var bookLbl = new Label { Left = 8, Top = 50, Width = 44, Text = "Book:", TextAlign = ContentAlignment.MiddleLeft };
            _bookCombo = new ComboBox { Left = 56, Top = 47, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            _bookCombo.SelectedIndexChanged += (s, e) => UpdateQuantityMax();
            _dueDate = new DateTimePicker { Left = 364, Top = 47, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(14) };
            var issueBtn = new Button { Left = 502, Top = 45, Width = 100, Height = 28, Text = "Issue", BackColor = AppConfig.Colors.SuccessColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            issueBtn.Click += async (s, e) => await IssueAsync();

            issue.Controls.AddRange(new Control[] { _borrowerType, _borrowerId, _lookupBtn, _borrowerName, _classCombo, _qtyLabel, _quantity, bookLbl, _bookCombo, _dueDate, issueBtn });

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

        private void UpdateBorrowerMode()
        {
            bool isClass = _borrowerType.SelectedItem?.ToString() == "Class";
            _borrowerId.Visible = !isClass;
            _lookupBtn.Visible = !isClass;
            _borrowerName.Visible = !isClass;
            _classCombo.Visible = isClass;
            _qtyLabel.Visible = isClass;
            _quantity.Visible = isClass;
            if (isClass) UpdateQuantityMax();
        }

        private void UpdateQuantityMax()
        {
            int max = (_bookCombo.SelectedItem is Book b) ? Math.Max(1, b.AvailableCopies) : 1;
            _quantity.Maximum = max;
            if (_quantity.Value > max) _quantity.Value = max;
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
                Qty = l.Quantity,
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
            if (!(_bookCombo.SelectedItem is Book book) || book == null)
            { UIHelper.ShowWarning("Pick a book with available copies.", "Library"); return; }

            string type = _borrowerType.SelectedItem?.ToString() ?? "Student";
            string id, name;
            int qty = 1;
            if (type == "Class")
            {
                if (_classCombo.SelectedItem == null)
                { UIHelper.ShowWarning("Pick a class.", "Library"); return; }
                name = _classCombo.SelectedItem.ToString();
                id = name;
                qty = (int)_quantity.Value;
            }
            else
            {
                id = StudentId.Parse(_borrowerId.Text);
                name = _borrowerName.Text;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || name.StartsWith("("))
                { UIHelper.ShowWarning("Look up a valid borrower first.", "Library"); return; }
            }

            var res = await _repo.IssueAsync(book.BookId, type, id, name, _dueDate.Value.Date, qty);
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
