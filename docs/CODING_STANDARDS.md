# Nyansapo ERP - Coding Standards & Guidelines

This document outlines the coding standards, architectural patterns, and best practices to be followed when developing or maintaining the Nyansapo ERP School Management System. Adhering to these standards ensures code maintainability, reduces technical debt, and allows the project to scale from a single developer to a full engineering team.

---

## 1. Naming Conventions

### 1.1 C# & .NET Core
- **Classes and Interfaces**: Use `PascalCase`. Interfaces MUST start with an `I` (e.g., `IAuthService`, `DashboardRepository`).
- **Methods**: Use `PascalCase` (e.g., `GetStudentMetricsAsync()`).
- **Method Arguments and Local Variables**: Use `camelCase` (e.g., `studentId`, `isAuthorized`).
- **Private Fields**: Use `camelCase` prefixed with an underscore (e.g., `_dashboardService`, `_connectionString`).
- **Constants and Readonly**: Use `PascalCase` (e.g., `MaxLoginAttempts`).
- **Async Methods**: MUST end with the `Async` suffix (e.g., `ProcessPaymentAsync()`).

### 1.2 Windows Forms (WinForms)
- **Forms**: Prefix with `frm` (e.g., `frmDashboard`, `frmFessPayment`) or use `PascalCase` `Form` suffix (e.g., `DashboardForm`). *Note: Legacy code uses `frm` and `EXAMSVIEW`. Moving forward, standardizing on `frm[Name]` is preferred for consistency.*
- **UI Controls**: Prefix with the control type abbreviation:
  - Buttons: `btnSubmit`
  - TextBoxes: `txtUsername`
  - Labels: `lblStatus`
  - DataGridViews: `dgvStudents`
  - Panels: `pnlMainContainer`

### 1.3 Database (SQL Server)
- **Tables**: `PascalCase`, plural or singular consistent (e.g., `Students`, `Employee`).
- **Columns**: `PascalCase` (e.g., `StudentId`, `FirstName`). Avoid spaces in column names.
- **Stored Procedures**: Prefix with `sp_` followed by the action (e.g., `sp_GetOutstandingFees`).

---

## 2. Architectural Guidelines

### 2.1 Separation of Concerns
The system follows a layered architecture. Do not mix UI logic, business rules, and data access.
- **Data Access Layer (Repositories)**: All direct database interactions (`SqlCommand`, EF Core `DbContext`) must live in the `Data/` folder. Use Repository classes (e.g., `StudentRepository.cs`).
- **Business Layer (Services)**: Business rules (e.g., calculating fee balances, checking leave permissions) must live in the `Services/` folder (e.g., `HeadmasterService.cs`, `AuthService.cs`).
- **Presentation Layer (Forms/Blazor)**: UI files (`frmXxx.cs` or `.razor`) should ONLY contain UI rendering logic, event handlers, and data binding. They should call Services to get data. **Do not write `SqlCommand` queries inside Form event handlers.**

### 2.2 God Classes & WinForms Refactoring
- WinForms like `frmDashboard.cs` can grow to thousands of lines.
- **Rule**: If a Form file exceeds 1,000 lines, you must extract helper classes:
  - Move chart building logic to a separate `ChartHelper` or `DashboardChartManager` class.
  - Move grid binding logic to a `DataGridViewHelper`.
  - Use `UserControl`s for complex sections (e.g., extract the Recent Payments panel into `RecentPaymentsControl.cs`).

---

## 3. Database & Data Access

### 3.1 Dapper / ADO.NET (Desktop)
- Always use **Parameterized Queries** to prevent SQL Injection. Never concatenate strings for queries (e.g., `$"SELECT * FROM Users WHERE Name = '{name}'"` is STRICTLY FORBIDDEN).
- Always wrap database connections in a `using` statement to prevent connection leaks.
```csharp
using (var connection = new SqlConnection(_connectionString))
{
    await connection.OpenAsync();
    // Execute query
}
```

### 3.2 Entity Framework Core (Web)
- Use Asynchronous LINQ methods (`ToListAsync()`, `FirstOrDefaultAsync()`).
- Use `AsNoTracking()` for read-only queries to improve performance.

### 3.3 Multi-Tenancy (SchoolId)
- Almost all tables must have a `SchoolId` column.
- Always include `SchoolId` in WHERE clauses to prevent cross-tenant data leakage. Utilize the `TenantContext.FilterClauseSql()` helper in the desktop app.

---

## 4. Error Handling & Logging

- **Try-Catch Blocks**: Wrap critical operations (database calls, file I/O, API calls) in `try-catch` blocks.
- **Logging**: Use `NLog` via `LoggerHelper`. Do not silently swallow exceptions.
```csharp
try
{
    await _service.ProcessAsync();
}
catch (Exception ex)
{
    LoggerHelper.LogError("Failed to process transaction", ex);
    UIHelper.ShowError("An error occurred while processing the transaction.");
}
```
- **User Messages**: Show clean, non-technical error messages to the user via `UIHelper.ShowError()`. Never expose raw SQL errors or stack traces to the end user.

---

## 5. Security & Authentication

- **Passwords**: Must ALWAYS be hashed. Never store or compare plaintext passwords. The system uses a custom `P3` prefix hash. Use `PasswordHasher.Verify()`.
- **Role-Based Access Control (RBAC)**: Protect sensitive forms and web pages using `AuthService.CurrentUser.Role`.
  - Explicitly define form permissions in `AuthService._formAccess`.
  - Validate permissions *before* opening a form or executing an action.
- **Financial Data**: Financial data (raw amounts) must be strictly gated. Roles like `Headmaster` or `Teacher` should see percentages or aggregated statistics, not raw transactional data.

---

## 6. Offline Sync Engine Guidelines

- **Transactions**: Local database changes that need to sync to the web MUST be wrapped in a transaction that also writes to the `SyncOutboxRepository`.
- **Conflict Resolution**: The Web Platform is the ultimate source of truth for schema. In the event of a sync conflict, the server's timestamp/version dictates the resolution.
- **Background Processes**: Sync operations must happen asynchronously on background threads to prevent freezing the UI. Use `System.Threading.Tasks.Task`.

---

## 7. Testing Standards

- **Unit Tests**: Aim to cover all core Business Logic (Services) and Data Access logic (Repositories).
- **Naming Convention for Tests**: `[MethodName]_[StateUnderTest]_[ExpectedBehavior]` (e.g., `AuthenticateAsync_InvalidPassword_ReturnsNull`).
- **Dependencies**: Use Dependency Injection to mock database contexts or external APIs (like Paystack/SMS providers) when testing services.
