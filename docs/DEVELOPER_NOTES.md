# Kingdom Preparatory School Management System - Developer Notes

**Version:** 1.0  
**Last Updated:** May 25, 2026  
**Audience:** Developers, System Administrators, Code Reviewers

---

## Architecture Overview

### Three-Tier Architecture

```
┌─────────────────────────────────────┐
│     UI Layer (Windows Forms)        │
│  - Forms, Controls, Event Handlers  │
│  - FormValidationHelper             │
│  - ConfirmationHelper               │
├─────────────────────────────────────┤
│    Services Layer (Business Logic)  │
│  - ExamService                      │
│  - StudentService                   │
│  - ReportCardManager                │
│  - ReportCardDataService            │
│  - ReportCardPDFGenerator           │
│  - ReportCardPrinter                │
├─────────────────────────────────────┤
│  Data Layer (Repository Pattern)    │
│  - IExamRepository                  │
│  - IStudentRepository               │
│  - IFeeRepository                   │
│  - Repositories use parameterized   │
│    queries, async/await             │
└─────────────────────────────────────┘
```

### Design Principles

1. **Dependency Injection (DI):** Services injected via constructor
2. **Repository Pattern:** Abstraction from database
3. **Async/Await:** Non-blocking operations
4. **Parameterized Queries:** SQL injection prevention
5. **Centralized Logging:** All operations logged

---

## Event Handler Patterns

### Pattern 1: Form Load with Data Loading

```csharp
private async void FormName_Load(object sender, EventArgs e)
{
    try
    {
        // Initialize UI
        InitializeUI();
        
        // Load data asynchronously
        await LoadDataAsync();
        
        // Log success
        LoggerHelper.LogInfo("Form loaded successfully");
    }
    catch (Exception ex)
    {
        // Show user-friendly error
        UIHelper.ShowError("Error loading form: " + ex.Message, "Form Name");
        
        // Log full exception for debugging
        LoggerHelper.LogError("FormName_Load failed", ex);
    }
}

private async Task LoadDataAsync()
{
    statusLabel.Text = "Loading...";
    var data = await _service.GetDataAsync();
    dataGrid.DataSource = data;
    statusLabel.Text = "Ready";
}
```

**Key Points:**
- Always use `async void` for event handlers
- Use `try-catch` for exception handling
- Show user-friendly messages with `UIHelper`
- Log full exceptions with `LoggerHelper.LogError()`
- Update status label during operations

### Pattern 2: Button Click with Validation & Confirmation

```csharp
private async void btnSave_Click(object sender, EventArgs e)
{
    try
    {
        // Validate inputs
        if (!FormValidationHelper.ValidateRequired(txtField, "Field Name"))
            return;
        
        if (!FormValidationHelper.ValidateNumeric(txtScore, "Score", out decimal score))
            return;
        
        // Ask for confirmation (for destructive operations)
        if (!ConfirmationHelper.ConfirmSave("Save changes to database?"))
            return;
        
        // Show progress
        btnSave.Text = "Saving...";
        btnSave.Enabled = false;
        
        // Perform operation
        bool success = await _service.SaveAsync(score);
        
        if (success)
        {
            ConfirmationHelper.ShowInfo("Saved successfully");
            await ReloadDataAsync();
            LoggerHelper.LogInfo($"Record saved: {score}");
        }
        else
        {
            UIHelper.ShowError("Save failed", "Error");
            LoggerHelper.LogWarning("Save operation returned false");
        }
    }
    catch (Exception ex)
    {
        UIHelper.ShowError("Error: " + ex.Message, "Save Error");
        LoggerHelper.LogError("btnSave_Click failed", ex);
    }
    finally
    {
        // Always restore button state
        btnSave.Text = "Save";
        btnSave.Enabled = true;
    }
}
```

**Key Points:**
- Validate before processing
- Confirm destructive operations
- Show progress feedback
- Use `finally` block to restore UI state
- Log success and failures

### Pattern 3: ComboBox Selection Change

```csharp
private void comboClass_SelectedIndexChanged(object sender, EventArgs e)
{
    try
    {
        // Ignore programmatic changes
        if (comboClass.SelectedIndex < 0)
            return;
        
        // Load related data
        _ = LoadStudentsAsync();
    }
    catch (Exception ex)
    {
        LoggerHelper.LogError("ComboBox selection failed", ex);
    }
}

private async Task LoadStudentsAsync()
{
    try
    {
        string selectedClass = comboClass.SelectedItem.ToString();
        var students = await _service.GetStudentsByClassAsync(selectedClass);
        dataGrid.DataSource = students;
    }
    catch (Exception ex)
    {
        UIHelper.ShowError("Failed to load students: " + ex.Message, "Load Error");
        LoggerHelper.LogError("LoadStudentsAsync failed", ex);
    }
}
```

**Key Points:**
- Check SelectedIndex > 0 (avoid "Select..." item)
- Use `_ = async call` to suppress unawaited warnings
- Wrap async calls in try-catch

---

## Validation Framework

### FormValidationHelper Usage

**Pattern 1: Single Field Validation**
```csharp
if (!FormValidationHelper.ValidateRequired(txtName, "Student Name"))
    return;  // Stop processing, user already notified
```

**Pattern 2: Numeric Range Validation**
```csharp
if (!FormValidationHelper.ValidateRange(txtScore, "Score", 0, 100, out decimal score))
    return;  // Score variable available, 0-100 guaranteed
```

**Pattern 3: Email Validation (Optional)**
```csharp
if (!FormValidationHelper.ValidateEmail(txtEmail))
    return;  // Email optional, but if provided, must be valid
```

**Pattern 4: ComboBox Selection**
```csharp
if (!FormValidationHelper.ValidateComboBox(comboClass, "Class"))
    return;  // Selected index guaranteed > 0
```

**Pattern 5: Batch Form Validation**
```csharp
var fields = new Dictionary<Control, string>
{
    { txtName, "Name" },
    { txtEmail, "Email" },
    { comboClass, "Class" }
};

if (!FormValidationHelper.ValidateForm(fields))
    return;  // All fields validated, user notified of failures
```

### Validation Error Feedback

**Automatic:**
- Control background color changes to light red (RGB 255,200,200)
- Tooltip displays error message
- Error logged to LoggerHelper for audit trail
- User can immediately see which field is invalid

---

## Logging Patterns

### LoggerHelper Usage

**Info Level:** User actions, successful operations
```csharp
LoggerHelper.LogInfo("Report card generated for student: KPS001");
LoggerHelper.LogInfo("Payment recorded: 500 GHS");
LoggerHelper.LogInfo("Attendance saved for 2026-05-25");
```

**Warning Level:** Validation failures, non-critical issues
```csharp
LoggerHelper.LogWarning("Score validation failed: input was 'abc'");
LoggerHelper.LogWarning("Student not found: ID 'KPS999'");
LoggerHelper.LogWarning("Attempt to save with empty remarks");
```

**Error Level:** Exceptions, failures
```csharp
try
{
    // operation
}
catch (Exception ex)
{
    LoggerHelper.LogError("Database save failed", ex);  // Includes stack trace
}
```

### Log Output Location

- **File:** `C:\Program Files\Kingdom Prep\bin\Debug\NLog.log`
- **Console:** Application output window (debug mode)
- **Event Viewer:** Windows Event Log (if configured)

### Log Retention

- Daily rotation: Logs rotate at midnight
- Keep 7 days of historical logs
- Archive older logs to `C:\Logs\Archive\`

---

## Database Integration Patterns

### Repository Pattern Example

```csharp
public interface IExamRepository
{
    Task<DataTable> GetResultsByClassAsync(string className);
    Task<bool> SaveResultsAsync(List<ExamResult> results);
}

public class ExamRepository : IExamRepository
{
    private readonly string _connectionString;
    
    public ExamRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<DataTable> GetResultsByClassAsync(string className)
    {
        try
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                
                string query = @"
                    SELECT StudentID, FullName, SubjectID, Score, Term, Year
                    FROM ExamResults
                    WHERE ClassID = @ClassID
                    ORDER BY FullName";
                
                using (OleDbCommand cmd = new OleDbCommand(query, conn))
                {
                    // CRITICAL: Use parameterized queries
                    cmd.Parameters.AddWithValue("@ClassID", className);
                    
                    DataTable results = new DataTable();
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        await Task.Run(() => adapter.Fill(results));
                    }
                    
                    return results;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError("GetResultsByClassAsync failed", ex);
            throw;
        }
    }
    
    public async Task<bool> SaveResultsAsync(List<ExamResult> results)
    {
        try
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                await conn.OpenAsync();
                
                foreach (var result in results)
                {
                    string query = @"
                        INSERT INTO ExamResults (StudentID, SubjectID, Score, Term, Year)
                        VALUES (@StudentID, @SubjectID, @Score, @Term, @Year)";
                    
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StudentID", result.StudentID);
                        cmd.Parameters.AddWithValue("@SubjectID", result.SubjectID);
                        cmd.Parameters.AddWithValue("@Score", result.Score);
                        cmd.Parameters.AddWithValue("@Term", result.Term);
                        cmd.Parameters.AddWithValue("@Year", result.Year);
                        
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                
                return true;
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError("SaveResultsAsync failed", ex);
            return false;
        }
    }
}
```

**Key Points:**
- Always use `@ParameterName` syntax
- Never concatenate user input into queries
- Use `async/await` for non-blocking operations
- Log exceptions for debugging
- Return `Task<bool>` for operation results

---

## Common Gotchas & Solutions

### Gotcha 1: Async Method Called Without Await

**Problem:**
```csharp
private void btnLoad_Click(object sender, EventArgs e)
{
    LoadDataAsync();  // ⚠️ Fire and forget - warning CS4014
}
```

**Solution:**
```csharp
private void btnLoad_Click(object sender, EventArgs e)
{
    _ = LoadDataAsync();  // ✅ Explicitly suppress warning
}
```

### Gotcha 2: Validation Not Checked

**Problem:**
```csharp
private void btnSave_Click(object sender, EventArgs e)
{
    decimal score = decimal.Parse(txtScore.Text);  // Crashes on invalid input
    SaveScore(score);
}
```

**Solution:**
```csharp
private void btnSave_Click(object sender, EventArgs e)
{
    if (!FormValidationHelper.ValidateRange(txtScore, "Score", 0, 100, out decimal score))
        return;  // Validation failed, user already notified
    
    SaveScore(score);
}
```

### Gotcha 3: Exception Swallowed

**Problem:**
```csharp
try
{
    var result = await _service.GetDataAsync();
    dataGrid.DataSource = result;
}
catch { }  // ⚠️ Silent failure
```

**Solution:**
```csharp
try
{
    var result = await _service.GetDataAsync();
    dataGrid.DataSource = result;
}
catch (Exception ex)
{
    UIHelper.ShowError("Failed to load data: " + ex.Message, "Error");
    LoggerHelper.LogError("GetDataAsync failed", ex);
}
```

### Gotcha 4: ComboBox Index Not Checked

**Problem:**
```csharp
private void comboClass_SelectedIndexChanged(object sender, EventArgs e)
{
    string selectedClass = comboClass.SelectedItem.ToString();  // Crashes if index < 0
    LoadStudents(selectedClass);
}
```

**Solution:**
```csharp
private void comboClass_SelectedIndexChanged(object sender, EventArgs e)
{
    if (comboClass.SelectedIndex < 0)
        return;  // Ignore "Select..." item
    
    string selectedClass = comboClass.SelectedItem.ToString();
    _ = LoadStudentsAsync();
}
```

### Gotcha 5: Hardcoded Connection String

**Problem:**
```csharp
string connection = "Data Source=192.168.1.100;...";  // ⚠️ Hardcoded
```

**Solution:**
```csharp
string connection = AppConfig.ConnectionString;  // ✅ From App.config
```

### Gotcha 6: Forgetting Parameter Type in OleDb

**Problem:**
```csharp
cmd.Parameters.AddWithValue("@Score", "95");  // String, not decimal
// Compares "95" as string in database
```

**Solution:**
```csharp
decimal score = decimal.Parse("95");
cmd.Parameters.AddWithValue("@Score", score);  // Proper type
```

---

## Performance Considerations

### DataGrid with 1000+ Rows

**Problem:** Slow scrolling, high memory usage

**Solution: Virtual Mode**
```csharp
dataGrid.VirtualMode = true;
dataGrid.CellValueNeeded += (s, e) =>
{
    e.Value = dataSource[e.RowIndex, e.ColumnIndex];
};
```

### Batch Operations

**Problem:** Saving 100 records one-by-one is slow

**Solution: Batch Insert**
```csharp
// Instead of loop with individual inserts
foreach (var record in records)
    await InsertRecordAsync(record);  // 100 queries

// Use batch operation
await InsertBatchAsync(records);  // 1 query with multiple values
```

### Report Generation

**Problem:** Generating 50 PDFs takes 5+ minutes, UI freezes

**Solution: Background Task**
```csharp
Task.Run(() =>
{
    foreach (var student in students)
    {
        GenerateReportCard(student);
        // Update progress on UI thread
        this.Invoke(() => statusLabel.Text = $"Generated {++count}/{total}");
    }
});
```

---

## Security Best Practices

### 1. SQL Injection Prevention

**❌ WRONG:**
```csharp
string query = $"SELECT * FROM Students WHERE Name = '{name}'";
```

**✅ RIGHT:**
```csharp
string query = "SELECT * FROM Students WHERE Name = @Name";
cmd.Parameters.AddWithValue("@Name", name);
```

### 2. Password Hashing (Future Feature)

**When implementing authentication:**
```csharp
using System.Security.Cryptography;

public static string HashPassword(string password)
{
    using (var sha256 = SHA256.Create())
    {
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
```

### 3. Input Validation

**Always validate:**
- User input length (prevent buffer overflows)
- Data types (numbers, dates, emails)
- Range checks (scores 0-100)
- Special characters (prevent script injection)

---

## Testing Patterns

### Unit Test Example

```csharp
[TestClass]
public class ExamServiceTests
{
    private ExamService _service;
    private Mock<IExamRepository> _mockRepository;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IExamRepository>();
        _service = new ExamService(_mockRepository.Object);
    }
    
    [TestMethod]
    public async Task GetResults_WithValidClass_ReturnsData()
    {
        // Arrange
        var mockData = new DataTable();
        _mockRepository.Setup(r => r.GetResultsByClassAsync("BASIC 3"))
            .ReturnsAsync(mockData);
        
        // Act
        var result = await _service.GetResultsByClassAsync("BASIC 3");
        
        // Assert
        Assert.IsNotNull(result);
        _mockRepository.Verify(r => r.GetResultsByClassAsync("BASIC 3"), Times.Once);
    }
}
```

---

## Build & Deployment

### Building from Command Line

```bash
dotnet clean
dotnet restore
dotnet build
```

### Publishing Release Build

```bash
dotnet publish -c Release -o C:\Publish\KingdomPrep\
```

### Deployment Checklist

- [ ] All unit tests passing
- [ ] No compiler warnings
- [ ] No security issues (SQL injection, XSS, etc.)
- [ ] Logging configured and tested
- [ ] Database migrations applied
- [ ] Backup created before deployment
- [ ] Rollback plan in place

---

## Code Review Checklist

Before merging pull requests:

- [ ] Code follows naming conventions (PascalCase for classes, camelCase for variables)
- [ ] All exceptions are caught and logged
- [ ] No hardcoded values (use constants/config)
- [ ] Parameterized SQL queries used (no concatenation)
- [ ] Async/await properly used (no fire-and-forget)
- [ ] User-facing errors are friendly (no stack traces)
- [ ] Validation implemented (FormValidationHelper)
- [ ] Confirmations for destructive operations (ConfirmationHelper)
- [ ] No magic numbers (extract to named constants)
- [ ] Comments explain "why", not "what"

---

## Future Improvements (Week 2+)

- [ ] Unit testing framework implementation
- [ ] Dependency injection container (Autofac/Unity)
- [ ] Role-based access control (RBAC)
- [ ] Audit logging (who changed what, when)
- [ ] Email notifications for fee reminders
- [ ] SMS integration for announcements
- [ ] Web API for mobile/web access
- [ ] Entity Framework Core migration

---

## Resources & References

- **Build Documentation:** `docs/DEPLOYMENT_GUIDE.md`
- **User Documentation:** `docs/USER_MANUAL.md`
- **Architecture Plan:** `docs/MERGED_8WEEK_ROADMAP.md`
- **NLog Configuration:** `App.config`
- **Database Schema:** See `docs/DEPLOYMENT_GUIDE.md` - Section 5

---

**Developer Notes Status: ✅ COMPLETE**

All patterns, frameworks, gotchas, and security best practices documented for current and future developers.
