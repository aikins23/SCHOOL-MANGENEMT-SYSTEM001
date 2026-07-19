namespace KingdomPrep.Web.Api;

public static class SyncTablePolicy
{
    public static readonly string[] AllowedTables =
    [
        "Students", "Employee", "Users",
        "fees", "payment_record", "examss", "Attendance", "emp_leave",
        "DraftAdmissions", "Rolled_Out_Students", "Rolled_Out_Employees",
        "Classes", "ClassAssignments", "ClassFees", "ClassSubjects",
        "GradingScheme", "StudentTermRemarks",
        "Expenses", "ExpenseCategories",
        "Buses", "BusRoutes", "StudentTransport", "TransportPayment", "TransportReminderLog",
        "Books", "BookLoans",
        "SmsOutbox", "Notices",
        "ExamTypes", "ExamSetups",
        "AcademicYears", "AcademicTerms", "StudentEnrollments", "StudentFeeLedger", "TermReminderSchedule",
        "AcademicCalendar", "SchoolHolidays", "TimePeriods", "SubjectAllocations", "TimetableEntries",
        "ScholarshipCategories", "StudentScholarships",
        "ApprovalWorkflows", "ClassPerformanceReports", "StudentPerformanceEntries",
        "SchoolInformation"
    ];

    private static readonly HashSet<string> Allowed = new(AllowedTables, StringComparer.OrdinalIgnoreCase);

    public static bool IsAllowed(string tableName) =>
        !string.IsNullOrWhiteSpace(tableName) && Allowed.Contains(tableName.Trim());
}
