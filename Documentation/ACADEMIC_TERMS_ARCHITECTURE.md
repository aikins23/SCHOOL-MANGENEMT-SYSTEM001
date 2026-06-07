# Academic Year & Term Management Architecture
# Kingdom Preparatory School Management System

**Date:** June 6, 2026
**Document Purpose:** To define the database schema, business logic, and UI flow for transitioning the application to a term-based academic structure with historical tracking and ledger-based fee accounting.

---

## 1. System Requirements & Goals

Based on stakeholder decisions, the system must support the following capabilities:
1. **Class History:** Maintain a historical record of which class a student belonged to in any given academic year.
2. **Term-Specific Fee Accounting (Ledger System):** Outstanding fee balances must be tracked on a per-term basis (e.g., Owes GHc 500 for Term 1, GHc 1500 for Term 2). Payments must be applied to specific term debts.
3. **Automated Closure Reporting:** Upon closing an academic term or year, the system must automatically generate a comprehensive, professional PDF summary report detailing enrollment, staff changes, and financial performance for that session.
4. **Automated Reopening Reminders (SMS):** The system must allow administrators to schedule automated SMS alerts to parents one week and one day prior to the start of a new academic year/term.
5. **Legacy Data Auditing:** All existing records must be preserved and assigned to a "Legacy Term" to ensure backward compatibility and auditability without breaking the new constraints.

---

## 2. Database Schema Design

The following tables will be introduced to support the new architecture.

### 2.1 Academic Structure Tables

**`AcademicYears`**
*   `Id` (INT, Primary Key, Identity)
*   `YearName` (VARCHAR, e.g., "2025/2026")
*   `StartDate` (DATE)
*   `EndDate` (DATE)
*   `IsActive` (BIT) - *Constraint: Only one row can be 1.*

**`AcademicTerms`**
*   `Id` (INT, Primary Key, Identity)
*   `AcademicYearId` (INT, Foreign Key)
*   `TermName` (VARCHAR, e.g., "First Term")
*   `StartDate` (DATE)
*   `EndDate` (DATE)
*   `IsActive` (BIT) - *Constraint: Only one row can be 1.*

### 2.2 Student Enrollment History
To support tracking class progression historically.

**`StudentEnrollments`**
*   `EnrollmentId` (INT, Primary Key, Identity)
*   `StudentId` (INT, Foreign Key)
*   `ClassId` (INT, Foreign Key)
*   `AcademicYearId` (INT, Foreign Key)
*   `EnrollmentDate` (DATETIME)
*   `Status` (VARCHAR, e.g., "Promoted", "Repeated", "Transferred", "Left")

### 2.3 Ledger-Based Fee Accounting
Replacing the single-balance approach with a term-by-term ledger.

**`StudentFeeLedger`**
*   `LedgerId` (INT, Primary Key, Identity)
*   `StudentId` (INT, Foreign Key)
*   `TermId` (INT, Foreign Key)
*   `TotalExpectedAmount` (DECIMAL)
*   `TotalPaidAmount` (DECIMAL)
*   `Balance` (Computed: TotalExpected - TotalPaid)

*(Note: The existing `FeePayments` table will be updated to include a `TermId` column to link individual receipts to the specific term debt they are clearing.)*

---

## 3. Data Migration Strategy (The Legacy Term)

To ensure the system remains stable and historical data is queryable:
1.  **Create Legacy Entities:** The migration script will insert an Academic Year named `"Legacy Data (Pre-2026)"` and an Academic Term named `"Legacy Term"`.
2.  **Update Existing Records:** An `UPDATE` script will attach the `"Legacy Term"` ID to all existing rows in the `FeePayments`, `Exams`, and `Attendance` tables.
3.  **Create Initial Ledger Entries:** For every student with an existing outstanding balance, a single `StudentFeeLedger` row will be created under the `"Legacy Term"` holding their current total debt.

---

## 4. Application Architecture (C#)

### 4.1 Service Layer (`AcademicService.cs`)
A new service will act as the "Source of Truth" for the active term.

```csharp
public interface IAcademicService
{
    Task<AcademicTerm> GetActiveTermAsync();
    Task<IEnumerable<AcademicTerm>> GetAllTermsAsync();
    Task<(bool success, string message)> SetActiveTermAsync(int termId);
    Task<(bool success, string message)> CloseTermAsync(int termId); // Triggers Report
    Task<(bool success, string message)> CreateAcademicYearAsync(AcademicYear year);
    Task<(bool success, string message)> CreateTermAsync(AcademicTerm term);
}
```
*   `GetActiveTermAsync()` will be called during the `Load` event of core forms (Fees, Exams, Attendance) to automatically bind transactions to the current point in time without user input.

### 4.2 Fee Payment Flow Redesign
*   **Current State:** Cashier enters a student ID and sees one global "Outstanding Balance".
*   **New State:** Cashier enters a student ID and sees a DataGrid of active debts broken down by term (e.g., Legacy Term: GHc 200, Term 1: GHc 1000). The cashier must select which term the parent is paying for before processing the transaction.

### 4.3 Administrator Workflow
*   **New UI (`frmAcademicSettings`):** A dedicated administrative form where the Principal can:
    *   Create a new Academic Year.
    *   Create Terms within that year.
    *   Click a "Set Active" button to roll the entire school over to a new term.
    *   **Close Term / Year:** Officially lock a session and trigger the automated reporting engine.
    *   **Schedule Reminders:** Configure automated SMS alerts for upcoming terms.

### 4.4 End-of-Session Closure & Automated PDF Reporting
When an Administrator initiates a "Close Term" or "Close Academic Year" action, the system will execute a background routine to aggregate session data and generate a formal, professional PDF summary report suitable for presentation to the School Board or Directors.

**The End-of-Session Report will include:**
*   **Demographics:** Total students enrolled, new admissions, and students who left/transferred during the session.
*   **Staffing:** Total active employees, new hires, and employees who resigned or were terminated.
*   **Financial Performance (Ledger Summary):** 
    *   Total Expected Fees
    *   Total Collected Fees (Revenue)
    *   Total Outstanding Fees (Debt carried forward)
*   **Formatting:** The output will be a branded, date-stamped, and neatly formatted PDF utilizing the school's logo and letterhead.

### 4.5 Scheduled Reopening Reminders (SMS Gateway Integration)
When a new term is created with a future `StartDate`, the system will allow the administrator to queue SMS reminders.
*   **Trigger 1:** One week before the `StartDate`, the system automatically sends a bulk SMS to all parents: *"Reminder: Kingdom Preparatory School resumes on [Date]. Please ensure all outstanding fees are settled."*
*   **Trigger 2:** One day before the `StartDate`, the system sends a final alert: *"School resumes tomorrow! We look forward to welcoming your ward back to campus."*
*   **Implementation:** This will require a background task (e.g., using `Quartz.NET` or a Windows Scheduled Task) that calls the SMS Gateway Service API daily to check for pending scheduled messages.

---

## 5. Implementation Roadmap

1.  **Phase 1: Database Migration**
    *   Write and execute the SQL schema update script.
    *   Execute the Legacy Data mapping script.
2.  **Phase 2: Service Implementation**
    *   Create Models (`AcademicYear`, `AcademicTerm`, `StudentEnrollment`).
    *   Build `AcademicRepository` and `AcademicService`.
3.  **Phase 3: Administrative UI, Reporting, & SMS**
    *   Build `frmAcademicSettings` for term management.
    *   Implement the PDF Generation Engine (using a library like iText7 or PdfSharp) for the End-of-Session Report.
    *   Implement SMS Gateway integration and background scheduling logic for Reopening Alerts.
4.  **Phase 4: Refactor Core Forms**
    *   Update `frmFessPayment` to read from `StudentFeeLedger` and accept Term selections.
    *   Update `frmStudentPromotion` to write to the new `StudentEnrollments` history table.