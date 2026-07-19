using KingdomPrep.Web.Data.Entities;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IEmployeeRepository
{
    Task<List<EmployeeEntity>> GetEmployeesAsync(string? searchTerm = null, string? deptFilter = null, Guid? schoolId = null);
    Task<List<string>> GetDepartmentsAsync(Guid? schoolId = null);
}

public class EmployeeRepository(IDbContextFactory<AppDbContext> factory) : IEmployeeRepository
{
    public async Task<List<EmployeeEntity>> GetEmployeesAsync(string? searchTerm = null, string? deptFilter = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Employees.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(deptFilter))
            query = query.Where(e => e.Department == deptFilter);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(e =>
                (e.FullName != null && e.FullName.ToLower().Contains(term)) ||
                (e.Position != null && e.Position.ToLower().Contains(term)));
        }

        return await query.OrderBy(e => e.FullName).ToListAsync();
    }

    public async Task<List<string>> GetDepartmentsAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Employees.AsNoTracking().Where(e => e.Department != null && e.Department != "");
        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);
        return await query.Select(e => e.Department!).Distinct().OrderBy(d => d).ToListAsync();
    }
}

public interface IWardRepository
{
    Task<StudentEntity?> GetWardAsync(int studentInternalId, Guid? schoolId = null);
    Task<List<StudentEntity>> GetWardsForParentAsync(string parentUsername, Guid? schoolId = null);
}

public class WardRepository(IDbContextFactory<AppDbContext> factory) : IWardRepository
{
    public async Task<StudentEntity?> GetWardAsync(int studentInternalId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Students.AsNoTracking().Where(s => s.StudentID == studentInternalId);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<StudentEntity>> GetWardsForParentAsync(string parentUsername, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Students.AsNoTracking().Where(s => s.ParentUsername == parentUsername);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        return await query.ToListAsync();
    }
}

public interface IWardBillingRepository
{
    Task<List<StudentFeeLedgerEntity>> GetFeeLedgerAsync(string studentId, Guid? schoolId = null);
    Task<List<WardFeeBreakdownItem>> GetFeeBreakdownAsync(string studentId, Guid? schoolId = null);
}

public class WardFeeBreakdownItem
{
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
    public int SortOrder { get; set; }
}

public class WardBillingRepository(IDbContextFactory<AppDbContext> factory) : IWardBillingRepository
{
    public async Task<List<StudentFeeLedgerEntity>> GetFeeLedgerAsync(string studentId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.StudentFeeLedgers.AsNoTracking().Where(l => l.StudentID == studentId);
        if (schoolId.HasValue) q = q.Where(l => l.SchoolId == schoolId.Value);

        var ledgers = await q.OrderByDescending(l => l.TermID).ToListAsync();
        var ledgerOutstanding = ledgers.Sum(l => Math.Max(0, l.TotalExpectedAmount - l.TotalPaidAmount));

        if (int.TryParse(studentId, out var numericStudentId))
        {
            var paymentQuery = db.PaymentRecords.AsNoTracking().Where(p => p.StudentID == numericStudentId);
            if (schoolId.HasValue) paymentQuery = paymentQuery.Where(p => p.SchoolId == schoolId.Value);

            var latestAccountBalance = await paymentQuery
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.ID)
                .Select(p => p.Balance)
                .FirstOrDefaultAsync();

            if (latestAccountBalance > ledgerOutstanding)
            {
                var extraBalance = latestAccountBalance - ledgerOutstanding;
                ledgers.Insert(0, new StudentFeeLedgerEntity
                {
                    StudentID = studentId,
                    TermID = int.MaxValue,
                    CurrentTermCharge = extraBalance,
                    TotalExpectedAmount = extraBalance,
                    TotalPaidAmount = 0,
                    SchoolId = schoolId
                });
            }
        }

        return ledgers;
    }

    public async Task<List<WardFeeBreakdownItem>> GetFeeBreakdownAsync(string studentId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var ledgerQuery = db.StudentFeeLedgers.AsNoTracking().Where(l => l.StudentID == studentId);
        if (schoolId.HasValue) ledgerQuery = ledgerQuery.Where(l => l.SchoolId == schoolId.Value);

        var ledgers = await ledgerQuery.OrderBy(l => l.TermID).ToListAsync();
        var chargeLines = new List<FeeChargeLine>();
        var paidPool = 0m;

        foreach (var ledger in ledgers)
        {
            var previousBalance = Math.Max(0, ledger.PreviousBalance);
            var termCharge = Math.Max(0, ledger.CurrentTermCharge);
            if (termCharge == 0 && ledger.TotalExpectedAmount > previousBalance)
            {
                termCharge = ledger.TotalExpectedAmount - previousBalance;
            }

            if (previousBalance > 0)
            {
                chargeLines.Add(new FeeChargeLine(
                    "Previous Debit",
                    $"Brought forward before term {ledger.TermID}",
                    previousBalance,
                    1000 + ledger.TermID));
            }

            if (termCharge > 0)
            {
                chargeLines.Add(new FeeChargeLine(
                    "Term Fee",
                    $"Term {ledger.TermID} school fee",
                    termCharge,
                    2000 + ledger.TermID));
            }

            paidPool += Math.Max(0, ledger.TotalPaidAmount);
        }

        if (await HasAdditionalFeeTablesAsync(db))
        {
            var additionalQuery =
                from charge in db.AdditionalFeeStudentCharges.AsNoTracking()
                join fee in db.AdditionalFees.AsNoTracking()
                    on charge.AdditionalFeeId equals fee.AdditionalFeeId
                where charge.StudentID == studentId
                    && charge.Amount > 0
                    && charge.Status == "Active"
                    && fee.Status == "Active"
                select new
                {
                    fee.FeeName,
                    fee.TermName,
                    fee.AcademicYear,
                    charge.Amount,
                    ChargeSchoolId = charge.SchoolId,
                    FeeSchoolId = fee.SchoolId
                };

            if (schoolId.HasValue)
            {
                additionalQuery = additionalQuery.Where(x =>
                    (!x.ChargeSchoolId.HasValue || x.ChargeSchoolId == schoolId.Value) &&
                    (!x.FeeSchoolId.HasValue || x.FeeSchoolId == schoolId.Value));
            }

            var additionalCharges = await additionalQuery
                .OrderBy(x => x.AcademicYear)
                .ThenBy(x => x.TermName)
                .ThenBy(x => x.FeeName)
                .ToListAsync();

            var index = 0;
            foreach (var charge in additionalCharges)
            {
                var period = string.Join(" ", new[] { charge.TermName, charge.AcademicYear }.Where(x => !string.IsNullOrWhiteSpace(x)));
                var description = string.IsNullOrWhiteSpace(period) ? charge.FeeName : $"{charge.FeeName} - {period}";
                chargeLines.Add(new FeeChargeLine("Additional Fee", description, charge.Amount, 3000 + index));
                index++;
            }
        }

        var breakdown = AllocatePayments(chargeLines, paidPool);
        var knownOutstanding = breakdown.Sum(x => x.Outstanding);
        var latestAccountBalance = await GetLatestAccountBalanceAsync(db, studentId, schoolId);
        if (latestAccountBalance > knownOutstanding)
        {
            var previousDebit = latestAccountBalance - knownOutstanding;
            breakdown.Add(new WardFeeBreakdownItem
            {
                Category = "Previous Debit",
                Description = "Previous account balance",
                Amount = previousDebit,
                Paid = 0,
                Outstanding = previousDebit,
                SortOrder = 9000
            });
        }

        return breakdown
            .Where(x => x.Amount > 0 || x.Outstanding > 0)
            .OrderBy(x => x.SortOrder)
            .ToList();
    }

    private static List<WardFeeBreakdownItem> AllocatePayments(IEnumerable<FeeChargeLine> charges, decimal paidPool)
    {
        var remainingPaid = Math.Max(0, paidPool);
        var items = new List<WardFeeBreakdownItem>();
        foreach (var charge in charges.OrderBy(c => c.SortOrder))
        {
            var amount = Math.Max(0, charge.Amount);
            var paid = Math.Min(amount, remainingPaid);
            remainingPaid -= paid;
            items.Add(new WardFeeBreakdownItem
            {
                Category = charge.Category,
                Description = charge.Description,
                Amount = amount,
                Paid = paid,
                Outstanding = Math.Max(0, amount - paid),
                SortOrder = charge.SortOrder
            });
        }

        return items;
    }

    private static async Task<decimal> GetLatestAccountBalanceAsync(AppDbContext db, string studentId, Guid? schoolId)
    {
        if (!int.TryParse(studentId, out var numericStudentId)) return 0;

        var paymentQuery = db.PaymentRecords.AsNoTracking().Where(p => p.StudentID == numericStudentId);
        if (schoolId.HasValue) paymentQuery = paymentQuery.Where(p => p.SchoolId == schoolId.Value);

        return await paymentQuery
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ID)
            .Select(p => p.Balance)
            .FirstOrDefaultAsync();
    }

    private static async Task<bool> HasAdditionalFeeTablesAsync(AppDbContext db)
    {
        if (!db.Database.IsRelational()) return true;

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT CASE WHEN OBJECT_ID(N'AdditionalFees', N'U') IS NOT NULL
          AND OBJECT_ID(N'AdditionalFeeStudentCharges', N'U') IS NOT NULL
     THEN 1 ELSE 0 END;";
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private sealed record FeeChargeLine(string Category, string Description, decimal Amount, int SortOrder);
}

public interface IReportCardPortalRepository
{
    Task<List<ExamResultEntity>> GetResultsAsync(string studentId, string term, string year, Guid? schoolId = null);
    Task<List<ExamResultEntity>> GetClassResultsAsync(string classId, string term, string year, Guid? schoolId = null);
    Task<List<(string Term, string Year)>> GetAvailableTermsAsync(string studentId, Guid? schoolId = null);
    Task<StudentTermRemarksEntity?> GetRemarksAsync(string studentId, string term, string year, Guid? schoolId = null);
}

public class ReportCardPortalRepository(IDbContextFactory<AppDbContext> factory) : IReportCardPortalRepository
{
    public async Task<List<ExamResultEntity>> GetResultsAsync(string studentId, string term, string year, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        if (!int.TryParse(studentId, out int parsedId)) return new List<ExamResultEntity>();
        var q = db.Exams.AsNoTracking().Where(e => e.StudentId == parsedId && e.Term == term && e.Year == year);
        if (schoolId.HasValue) q = q.Where(e => e.SchoolId == schoolId.Value);
        q = OnlyPublishedToPortal(q, db, schoolId);
        return await q.OrderBy(e => e.Subject).ToListAsync();
    }

    public async Task<List<ExamResultEntity>> GetClassResultsAsync(string classId, string term, string year, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.Exams.AsNoTracking().Where(e => e.ClassId == classId && e.Term == term && e.Year == year);
        if (schoolId.HasValue) q = q.Where(e => e.SchoolId == schoolId.Value);
        q = OnlyPublishedToPortal(q, db, schoolId);
        return await q.OrderBy(e => e.Subject).ToListAsync();
    }

    public async Task<List<(string Term, string Year)>> GetAvailableTermsAsync(string studentId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        if (!int.TryParse(studentId, out int parsedId)) return new List<(string, string)>();
        var q = db.Exams.AsNoTracking().Where(e => e.StudentId == parsedId);
        if (schoolId.HasValue) q = q.Where(e => e.SchoolId == schoolId.Value);
        q = OnlyPublishedToPortal(q, db, schoolId);

        var raw = await q.Select(e => new { e.Term, e.Year }).Distinct().ToListAsync();
        return raw
            .Where(r => r.Term != null && r.Year != null)
            .Select(r => (r.Term!, r.Year!))
            .OrderByDescending(r => r.Item2)
            .ThenByDescending(r => r.Item1)
            .ToList();
    }

    public async Task<StudentTermRemarksEntity?> GetRemarksAsync(string studentId, string term, string year, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        if (!int.TryParse(studentId, out int parsedId)) return null;

        var hasPublishedResults = await OnlyPublishedToPortal(
                db.Exams.AsNoTracking().Where(e => e.StudentId == parsedId && e.Term == term && e.Year == year),
                db,
                schoolId)
            .AnyAsync();
        if (!hasPublishedResults) return null;

        var q = db.Set<StudentTermRemarksEntity>().AsNoTracking().Where(r => r.StudentID == studentId && r.Term == term && r.Year == year);
        if (schoolId.HasValue) q = q.Where(r => r.SchoolId == schoolId.Value);
        return await q.FirstOrDefaultAsync();
    }

    private static IQueryable<ExamResultEntity> OnlyPublishedToPortal(
        IQueryable<ExamResultEntity> query,
        AppDbContext db,
        Guid? schoolId)
    {
        return query.Where(e => db.ExamSetups.Any(s =>
            s.IsPublishedToPortal &&
            s.Term == e.Term &&
            s.Year == e.Year &&
            (!schoolId.HasValue || s.SchoolId == schoolId.Value) &&
            (
                e.ExamTypeId == null ||
                (s.ExamTypeId == e.ExamTypeId &&
                 (e.AssessmentNumber == null || s.AssessmentNumber == e.AssessmentNumber))
            )));
    }
}

public interface IAttendancePortalRepository
{
    Task<List<AttendanceEntity>> GetAttendanceRecordsAsync(string referenceId, Guid? schoolId = null, int limit = 30);
    Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(string referenceId, Guid? schoolId = null);
    Task SaveAttendanceAsync(IEnumerable<AttendanceEntity> records);
    Task<List<AttendanceEntity>> GetClassAttendanceAsync(string classId, DateTime startDate, DateTime endDate, Guid? schoolId = null);
}

public class AttendanceSummaryDto
{
    public int TotalDays { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public decimal AttendancePercentage { get; set; }
}

public class AttendancePortalRepository(IDbContextFactory<AppDbContext> factory) : IAttendancePortalRepository
{
    public async Task<List<AttendanceEntity>> GetAttendanceRecordsAsync(string referenceId, Guid? schoolId = null, int limit = 30)
    {
        using var db = await factory.CreateDbContextAsync();
        if (!int.TryParse(referenceId, out int parsedId)) return new List<AttendanceEntity>();
        var q = db.Attendances.AsNoTracking().Where(a => a.ReferenceID == parsedId && a.ReferenceType == "Student");
        if (schoolId.HasValue) q = q.Where(a => a.SchoolId == schoolId.Value);
        return await q.OrderByDescending(a => a.Date).Take(limit).ToListAsync();
    }

    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(string referenceId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        if (!int.TryParse(referenceId, out int parsedId)) return new AttendanceSummaryDto();
        var q = db.Attendances.AsNoTracking().Where(a => a.ReferenceID == parsedId && a.ReferenceType == "Student");
        if (schoolId.HasValue) q = q.Where(a => a.SchoolId == schoolId.Value);

        var list = await q.Select(a => a.Status).ToListAsync();
        int total = list.Count;
        int present = list.Count(s => string.Equals(s, "Present", StringComparison.OrdinalIgnoreCase));
        int absent = list.Count(s => string.Equals(s, "Absent", StringComparison.OrdinalIgnoreCase));
        int late = list.Count(s => string.Equals(s, "Late", StringComparison.OrdinalIgnoreCase));

        return new AttendanceSummaryDto { TotalDays = total, PresentCount = present, AbsentCount = absent, LateCount = late, AttendancePercentage = total == 0 ? 100 : Math.Round((decimal)present / total * 100, 2) };
    }

    public async Task SaveAttendanceAsync(IEnumerable<AttendanceEntity> records)
    {
        using var db = await factory.CreateDbContextAsync();
        foreach (var r in records)
        {
            // If we are overwriting attendance for a student on a specific date, we should check if it exists first.
            var existing = await db.Attendances.FirstOrDefaultAsync(a =>
                a.ReferenceID == r.ReferenceID &&
                a.ReferenceType == r.ReferenceType &&
                a.Date.Date == r.Date.Date &&
                a.SchoolId == r.SchoolId);

            if (existing != null)
            {
                existing.Status = r.Status;
                existing.Remarks = r.Remarks;
                db.Attendances.Update(existing);
            }
            else
            {
                db.Attendances.Add(r);
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<AttendanceEntity>> GetClassAttendanceAsync(string classId, DateTime startDate, DateTime endDate, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();

        var query = db.Attendances.AsNoTracking()
            .Join(db.Students.AsNoTracking(),
                  a => a.ReferenceID,
                  s => s.StudentID,
                  (a, s) => new { Attendance = a, Student = s })
            .Where(x => x.Student.ClassID == classId
                     && x.Attendance.Date >= startDate
                     && x.Attendance.Date <= endDate);

        if (schoolId.HasValue)
        {
            query = query.Where(x => x.Student.SchoolId == schoolId.Value &&
                                     x.Attendance.SchoolId == schoolId.Value);
        }

        return await query.Select(x => x.Attendance).ToListAsync();
    }
}

public interface IStudentPortalRepository
{
    Task<List<StudentEntity>> GetStudentsAsync(string? searchTerm = null, string? classFilter = null, Guid? schoolId = null);
}

public class StudentPortalRepository(IDbContextFactory<AppDbContext> factory) : IStudentPortalRepository
{
    public async Task<List<StudentEntity>> GetStudentsAsync(string? searchTerm = null, string? classFilter = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.Students.AsNoTracking().Where(s => s.ClassID != null && s.ClassID != "GRADUATED");
        if (schoolId.HasValue) q = q.Where(s => s.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(classFilter)) q = q.Where(s => s.ClassID == classFilter);

        // EF Core translates toString differently across versions, so if search term is an ID, parse it.
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (int.TryParse(searchTerm, out int searchId))
            {
                q = q.Where(s => s.StudentID == searchId || (s.FirstName != null && s.FirstName.Contains(searchTerm)) || (s.LastName != null && s.LastName.Contains(searchTerm)));
            }
            else
            {
                q = q.Where(s => (s.FirstName != null && s.FirstName.Contains(searchTerm)) || (s.LastName != null && s.LastName.Contains(searchTerm)));
            }
        }

        return await q.OrderBy(s => s.ClassID).ThenBy(s => s.FirstName).ToListAsync();
    }
}

public interface ITeacherPortalRepository
{
    Task<List<ClassAssignmentEntity>> GetAssignedClassesAsync(int employeeId, Guid? schoolId = null);
    Task<int> GetStudentCountForClassesAsync(List<string> classNames, Guid? schoolId = null);
    Task<List<AssignmentEntity>> GetRecentAssignmentsAsync(int employeeId, int count = 5, Guid? schoolId = null);
    Task<List<AssignmentEntity>> GetAllAssignmentsAsync(int employeeId, Guid? schoolId = null);
    Task SaveAssignmentAsync(AssignmentEntity assignment, Guid? schoolId = null);
    Task DeleteAssignmentAsync(int assignmentId, int employeeId, Guid? schoolId = null);
    Task<List<StudentTermRemarksEntity>> GetClassRemarksAsync(int employeeId, string classId, string term, string year, Guid? schoolId = null);
    Task SaveStudentRemarksAsync(int employeeId, string classId, StudentTermRemarksEntity remarks, Guid? schoolId = null);
}

public class TeacherPortalRepository(IDbContextFactory<AppDbContext> factory) : ITeacherPortalRepository
{
    public async Task<List<ClassAssignmentEntity>> GetAssignedClassesAsync(int employeeId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.ClassAssignments.AsNoTracking().Where(c => c.ClassTeacherID == employeeId);
        if (schoolId.HasValue) q = q.Where(c => c.SchoolId == schoolId.Value);
        return await q.OrderBy(c => c.ClassName).ToListAsync();
    }

    public async Task<int> GetStudentCountForClassesAsync(List<string> classNames, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.Students.AsNoTracking().Where(s => s.ClassID != null && classNames.Contains(s.ClassID));
        if (schoolId.HasValue) q = q.Where(s => s.SchoolId == schoolId.Value);
        return await q.CountAsync();
    }

    public async Task<List<AssignmentEntity>> GetRecentAssignmentsAsync(int employeeId, int count = 5, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var empStr = employeeId.ToString();
        var assignedClasses = await GetAssignedClassNamesAsync(db, employeeId, schoolId);
        return await db.Assignments.AsNoTracking()
            .Where(a => a.TeacherID == empStr && assignedClasses.Contains(a.ClassID))
            .OrderByDescending(a => a.CreatedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<AssignmentEntity>> GetAllAssignmentsAsync(int employeeId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var empStr = employeeId.ToString();
        var assignedClasses = await GetAssignedClassNamesAsync(db, employeeId, schoolId);
        return await db.Assignments.AsNoTracking()
            .Where(a => a.TeacherID == empStr && assignedClasses.Contains(a.ClassID))
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
    }

    public async Task SaveAssignmentAsync(AssignmentEntity assignment, Guid? schoolId = null)
    {
        if (assignment == null) throw new ArgumentNullException(nameof(assignment));
        if (!int.TryParse(assignment.TeacherID, out var employeeId)) throw new ArgumentException("Teacher ID is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.ClassID)) throw new ArgumentException("Assignment class is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.Title)) throw new ArgumentException("Assignment title is required.", nameof(assignment));
        if (string.IsNullOrWhiteSpace(assignment.Subject)) throw new ArgumentException("Assignment subject is required.", nameof(assignment));

        using var db = await factory.CreateDbContextAsync();
        if (schoolId.HasValue && !await TeacherOwnsClassAsync(db, assignment.TeacherID, assignment.ClassID, schoolId.Value))
        {
            throw new InvalidOperationException("Assignment class does not belong to the signed-in teacher's school.");
        }

        if (assignment.AssignmentID == 0)
        {
            assignment.CreatedDate = DateTime.Now;
            db.Assignments.Add(assignment);
        }
        else
        {
            var existing = await db.Assignments.FirstOrDefaultAsync(a => a.AssignmentID == assignment.AssignmentID);
            if (existing == null) throw new InvalidOperationException("Assignment was not found.");
            if (existing.TeacherID != employeeId.ToString())
            {
                throw new InvalidOperationException("Assignment does not belong to the signed-in teacher.");
            }

            if (schoolId.HasValue && !await TeacherOwnsClassAsync(db, existing.TeacherID, existing.ClassID, schoolId.Value))
            {
                throw new InvalidOperationException("Assignment does not belong to the signed-in teacher's school.");
            }

            existing.Title = assignment.Title;
            existing.Description = assignment.Description;
            existing.ClassID = assignment.ClassID;
            existing.Subject = assignment.Subject;
            existing.DueDate = assignment.DueDate;
            db.Assignments.Update(existing);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAssignmentAsync(int assignmentId, int employeeId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var assignment = await db.Assignments.FindAsync(assignmentId);
        if (assignment != null &&
            assignment.TeacherID == employeeId.ToString() &&
            (!schoolId.HasValue || await TeacherOwnsClassAsync(db, assignment.TeacherID, assignment.ClassID, schoolId.Value)))
        {
            db.Assignments.Remove(assignment);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<StudentTermRemarksEntity>> GetClassRemarksAsync(int employeeId, string classId, string term, string year, Guid? schoolId = null)
    {
        if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(year))
            return new List<StudentTermRemarksEntity>();

        using var db = await factory.CreateDbContextAsync();
        if (schoolId.HasValue && !await TeacherOwnsClassAsync(db, employeeId.ToString(), classId, schoolId.Value))
        {
            throw new InvalidOperationException("Selected class does not belong to the signed-in teacher.");
        }

        var studentIds = await db.Students.AsNoTracking()
            .Where(s => s.ClassID == classId && (!schoolId.HasValue || s.SchoolId == schoolId.Value))
            .Select(s => s.StudentID.ToString())
            .ToListAsync();

        return await db.StudentTermRemarks.AsNoTracking()
            .Where(r => studentIds.Contains(r.StudentID) &&
                        r.Term == term &&
                        r.Year == year &&
                        (!schoolId.HasValue || r.SchoolId == schoolId.Value))
            .ToListAsync();
    }

    public async Task SaveStudentRemarksAsync(int employeeId, string classId, StudentTermRemarksEntity remarks, Guid? schoolId = null)
    {
        if (remarks == null) throw new ArgumentNullException(nameof(remarks));
        if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentException("Class is required.", nameof(classId));
        if (string.IsNullOrWhiteSpace(remarks.StudentID)) throw new ArgumentException("Student is required.", nameof(remarks));
        if (string.IsNullOrWhiteSpace(remarks.Term)) throw new ArgumentException("Academic term is required.", nameof(remarks));
        if (string.IsNullOrWhiteSpace(remarks.Year)) throw new ArgumentException("Academic year is required.", nameof(remarks));

        remarks.StudentID = remarks.StudentID.Trim();
        remarks.Term = remarks.Term.Trim();
        remarks.Year = remarks.Year.Trim();
        remarks.Conduct = remarks.Conduct?.Trim();
        remarks.Interest = remarks.Interest?.Trim();
        remarks.Attitude = remarks.Attitude?.Trim();
        remarks.ClassTeacherRemarks = remarks.ClassTeacherRemarks?.Trim();
        remarks.HeadTeacherRemarks = remarks.HeadTeacherRemarks?.Trim();

        using var db = await factory.CreateDbContextAsync();
        if (schoolId.HasValue && !await TeacherOwnsClassAsync(db, employeeId.ToString(), classId, schoolId.Value))
        {
            throw new InvalidOperationException("Selected class does not belong to the signed-in teacher.");
        }

        var studentBelongsToClass = await db.Students.AsNoTracking().AnyAsync(s =>
            s.StudentID.ToString() == remarks.StudentID &&
            s.ClassID == classId &&
            (!schoolId.HasValue || s.SchoolId == schoolId.Value));
        if (!studentBelongsToClass) throw new InvalidOperationException("Selected student does not belong to this class.");

        var existing = await db.StudentTermRemarks.FirstOrDefaultAsync(r =>
            r.StudentID == remarks.StudentID &&
            r.Term == remarks.Term &&
            r.Year == remarks.Year &&
            (!schoolId.HasValue || r.SchoolId == schoolId.Value));

        if (existing == null)
        {
            remarks.SchoolId = schoolId;
            remarks.CreatedDate = DateTime.Now;
            db.StudentTermRemarks.Add(remarks);
        }
        else
        {
            existing.Conduct = remarks.Conduct;
            existing.Interest = remarks.Interest;
            existing.Attitude = remarks.Attitude;
            existing.ClassTeacherRemarks = remarks.ClassTeacherRemarks;
            existing.HeadTeacherRemarks = remarks.HeadTeacherRemarks;
            db.StudentTermRemarks.Update(existing);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<List<string>> GetAssignedClassNamesAsync(AppDbContext db, int employeeId, Guid? schoolId)
    {
        var q = db.ClassAssignments.AsNoTracking().Where(c => c.ClassTeacherID == employeeId);
        if (schoolId.HasValue) q = q.Where(c => c.SchoolId == schoolId.Value);
        return await q.Select(c => c.ClassName).ToListAsync();
    }

    private static async Task<bool> TeacherOwnsClassAsync(AppDbContext db, string? teacherId, string? classId, Guid schoolId)
    {
        if (!int.TryParse(teacherId, out var employeeId) || string.IsNullOrWhiteSpace(classId)) return false;
        return await db.ClassAssignments.AsNoTracking()
            .AnyAsync(c => c.ClassTeacherID == employeeId && c.ClassName == classId && c.SchoolId == schoolId);
    }
}

public interface IGradingPortalRepository
{
    Task<List<string>> GetSubjectsAsync(string? classId = null, Guid? schoolId = null);
    Task<ExamSetupEntity?> GetLatestExamSetupAsync(Guid? schoolId = null);
    Task<ExamSetupEntity?> GetOpenExamSetupAsync(Guid? schoolId = null, DateTime? asOf = null);
    Task<bool> IsGradingOpenAsync(string term, string year, Guid? schoolId = null, DateTime? asOf = null, int? examTypeId = null, int? assessmentNumber = null);
    Task<List<ExamResultEntity>> GetClassGradesAsync(string classId, string subject, string term, string year, Guid? schoolId = null, int? examTypeId = null, int? assessmentNumber = null);
    Task<List<ExamResultEntity>> GetClassGradesAsync(string classId, string term, string year, Guid? schoolId = null, int? examTypeId = null, int? assessmentNumber = null);
    Task SaveGradesAsync(IEnumerable<ExamResultEntity> grades);
}

public class GradingPortalRepository(IDbContextFactory<AppDbContext> factory) : IGradingPortalRepository
{
    public async Task<List<string>> GetSubjectsAsync(string? classId = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ClassSubjects.AsNoTracking().Where(s => !string.IsNullOrEmpty(s.Subject));
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        if (!string.IsNullOrWhiteSpace(classId)) query = query.Where(s => s.ClassName == classId);

        var subjects = await query
            .OrderBy(s => s.SortOrder ?? 999)
            .ThenBy(s => s.Subject)
            .Select(s => s.Subject!)
            .Distinct()
            .ToListAsync();

        if (!subjects.Any() && string.IsNullOrWhiteSpace(classId))
        {
            // Fallback if not synced yet
            return new List<string>
            {
                "English Language", "Mathematics", "Integrated Science", "Social Studies",
                "RME", "Computing", "French", "Ghanaian Language", "Creative Arts", "History", "Our World Our People"
            };
        }
        return subjects;
    }

    public async Task<ExamSetupEntity?> GetLatestExamSetupAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ExamSetups.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);
        return await query
            .OrderByDescending(e => e.EndDate)
            .ThenByDescending(e => e.SetupID)
            .FirstOrDefaultAsync();
    }

    public async Task<ExamSetupEntity?> GetOpenExamSetupAsync(Guid? schoolId = null, DateTime? asOf = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var today = (asOf ?? DateTime.Today).Date;
        var query = db.ExamSetups.AsNoTracking()
            .Where(e => (!e.StartDate.HasValue || e.StartDate.Value.Date <= today)
                     && (!e.EndDate.HasValue || e.EndDate.Value.Date >= today));

        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);

        return await query
            .OrderByDescending(e => e.EndDate)
            .ThenByDescending(e => e.SetupID)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsGradingOpenAsync(string term, string year, Guid? schoolId = null, DateTime? asOf = null, int? examTypeId = null, int? assessmentNumber = null)
    {
        if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(year)) return false;

        using var db = await factory.CreateDbContextAsync();
        var today = (asOf ?? DateTime.Today).Date;
        var query = db.ExamSetups.AsNoTracking()
            .Where(e => e.Term == term
                     && e.Year == year
                     && (!e.StartDate.HasValue || e.StartDate.Value.Date <= today)
                     && (!e.EndDate.HasValue || e.EndDate.Value.Date >= today));

        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);
        if (examTypeId.HasValue) query = query.Where(e => e.ExamTypeId == examTypeId.Value);
        if (assessmentNumber.HasValue) query = query.Where(e => e.AssessmentNumber == assessmentNumber.Value);
        return await query.AnyAsync();
    }

    public async Task<List<ExamResultEntity>> GetClassGradesAsync(string classId, string subject, string term, string year, Guid? schoolId = null, int? examTypeId = null, int? assessmentNumber = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.Exams.AsNoTracking().Where(e => e.ClassId == classId && e.Subject == subject && e.Term == term && e.Year == year);
        if (schoolId.HasValue) q = q.Where(e => e.SchoolId == schoolId.Value);
        q = FilterAssessment(q, examTypeId, assessmentNumber);
        return await q.OrderBy(e => e.StudentName).ToListAsync();
    }

    public async Task<List<ExamResultEntity>> GetClassGradesAsync(string classId, string term, string year, Guid? schoolId = null, int? examTypeId = null, int? assessmentNumber = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.Exams.AsNoTracking().Where(e => e.ClassId == classId && e.Term == term && e.Year == year);
        if (schoolId.HasValue) q = q.Where(e => e.SchoolId == schoolId.Value);
        q = FilterAssessment(q, examTypeId, assessmentNumber);
        return await q.OrderBy(e => e.StudentName).ThenBy(e => e.Subject).ToListAsync();
    }

    public async Task SaveGradesAsync(IEnumerable<ExamResultEntity> grades)
    {
        using var db = await factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        foreach (var g in grades)
        {
            var existing = await db.Exams.FirstOrDefaultAsync(e =>
                e.StudentId == g.StudentId &&
                e.ClassId == g.ClassId &&
                e.Subject == g.Subject &&
                e.Term == g.Term &&
                e.Year == g.Year &&
                e.ExamTypeId == g.ExamTypeId &&
                e.AssessmentNumber == g.AssessmentNumber &&
                e.SchoolId == g.SchoolId);

            if (existing != null)
            {
                existing.ClassScore1 = g.ClassScore1;
                existing.ClassScore2 = g.ClassScore2;
                existing.ClassScore3 = g.ClassScore3;
                existing.CategoryTotal = g.CategoryTotal;
                existing.ExamScore = g.ExamScore;
                existing.TotalScore = g.TotalScore;
                existing.Grade = g.Grade;
                existing.Remark = g.Remark;
                existing.AssessmentLabel = g.AssessmentLabel;
                existing.UpdatedAt = now;
                db.Exams.Update(existing);
            }
            else
            {
                g.UpdatedAt = now;
                db.Exams.Add(g);
            }
        }
        await db.SaveChangesAsync();
    }

    private static IQueryable<ExamResultEntity> FilterAssessment(
        IQueryable<ExamResultEntity> query,
        int? examTypeId,
        int? assessmentNumber)
    {
        if (examTypeId.HasValue) query = query.Where(e => e.ExamTypeId == examTypeId.Value);
        if (assessmentNumber.HasValue) query = query.Where(e => e.AssessmentNumber == assessmentNumber.Value);
        return query;
    }
}



public class DefaulterDto
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string ClassID { get; set; } = "";
    public decimal Arrears { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal TotalPaid { get; set; }
}

public interface IDefaulterPortalRepository
{
    Task<List<DefaulterDto>> GetClassDefaultersAsync(string classId, Guid? schoolId = null);
}

public class DefaulterPortalRepository(IDbContextFactory<AppDbContext> factory) : IDefaulterPortalRepository
{
    public async Task<List<DefaulterDto>> GetClassDefaultersAsync(string classId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();

        var query = db.Students.AsNoTracking().Where(s => s.ClassID == classId);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        var students = await query.ToListAsync();

        var studentIds = students.Select(s => s.StudentID.ToString()).ToList();

        var ledgers = await db.StudentFeeLedgers.AsNoTracking()
            .Where(l => studentIds.Contains(l.StudentID) && (!schoolId.HasValue || l.SchoolId == schoolId.Value))
            .GroupBy(l => l.StudentID)
            .Select(g => new {
                StudentID = g.Key,
                TotalExpected = g.Sum(x => x.TotalExpectedAmount),
                TotalPaid = g.Sum(x => x.TotalPaidAmount)
            }).ToListAsync();

        var defaulters = new List<DefaulterDto>();
        foreach (var s in students)
        {
            var l = ledgers.FirstOrDefault(x => x.StudentID == s.StudentID.ToString());
            decimal expected = l?.TotalExpected ?? 0;
            decimal paid = l?.TotalPaid ?? 0;
            decimal arrears = expected - paid;

            if (arrears > 0)
            {
                defaulters.Add(new DefaulterDto
                {
                    StudentId = s.StudentID,
                    FullName = s.FullName ?? "Unknown",
                    ClassID = s.ClassID ?? "",
                    TotalExpected = expected,
                    TotalPaid = paid,
                    Arrears = arrears
                });
            }
        }

        return defaulters.OrderByDescending(d => d.Arrears).ToList();
    }
}

public interface ILeavePortalRepository
{
    Task<List<LeaveEntity>> GetMyLeavesAsync(int employmentId, Guid? schoolId = null);
    Task<List<LeaveEntity>> GetLeavesAsync(string? status = null, string? searchTerm = null, Guid? schoolId = null);
    Task SubmitLeaveAsync(LeaveEntity leave);
    Task UpdateLeaveStatusAsync(int employmentId, DateTime startDate, string status, Guid? schoolId = null, string? actorUsername = null);
}

public class LeavePortalRepository(IDbContextFactory<AppDbContext> factory, IAuditLogService? audit = null) : ILeavePortalRepository
{
    public async Task<List<LeaveEntity>> GetMyLeavesAsync(int employmentId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.Leaves.AsNoTracking()
            .Where(l => l.EmploymentID == employmentId);
        if (schoolId.HasValue) q = q.Where(l => l.SchoolId == schoolId.Value);
        return await q
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();
    }

    public async Task<List<LeaveEntity>> GetLeavesAsync(string? status = null, string? searchTerm = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Leaves.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(l => l.SchoolId == schoolId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(term) ||
                (l.Department != null && l.Department.ToLower().Contains(term)) ||
                (l.Position != null && l.Position.ToLower().Contains(term)) ||
                l.LeaveOption.ToLower().Contains(term));
        }

        return await query
            .OrderBy(l => l.Status == "PENDING" ? 0 : 1)
            .ThenByDescending(l => l.StartDate)
            .ToListAsync();
    }

    public async Task SubmitLeaveAsync(LeaveEntity leave)
    {
        if (leave == null) throw new ArgumentNullException(nameof(leave));
        if (leave.EmploymentID <= 0) throw new ArgumentException("Employment ID is required.", nameof(leave));
        if (string.IsNullOrWhiteSpace(leave.LeaveOption)) throw new ArgumentException("Leave type is required.", nameof(leave));
        if (leave.StartDate.Date > leave.EndDate.Date) throw new ArgumentException("Leave start date cannot be after end date.", nameof(leave));
        leave.Status = string.IsNullOrWhiteSpace(leave.Status) ? "PENDING" : leave.Status.Trim().ToUpperInvariant();

        using var db = await factory.CreateDbContextAsync();

        var existing = await db.Leaves.FirstOrDefaultAsync(l => l.EmploymentID == leave.EmploymentID && l.StartDate == leave.StartDate && l.SchoolId == leave.SchoolId);
        if (existing == null)
        {
            db.Leaves.Add(leave);
        }
        else
        {
            existing.EndDate = leave.EndDate;
            existing.Reasons = leave.Reasons;
            existing.LeaveOption = leave.LeaveOption;
            existing.Status = leave.Status;
            db.Leaves.Update(existing);
        }
        await db.SaveChangesAsync();
    }

    public async Task UpdateLeaveStatusAsync(int employmentId, DateTime startDate, string status, Guid? schoolId = null, string? actorUsername = null)
    {
        if (employmentId <= 0) throw new ArgumentException("Employment ID is required.", nameof(employmentId));
        status = (status ?? "").Trim().ToUpperInvariant();
        if (status != "APPROVED" && status != "REJECTED" && status != "PENDING")
        {
            throw new ArgumentException("Leave status must be APPROVED, REJECTED, or PENDING.", nameof(status));
        }

        using var db = await factory.CreateDbContextAsync();
        var query = db.Leaves.Where(l => l.EmploymentID == employmentId && l.StartDate == startDate);
        if (schoolId.HasValue) query = query.Where(l => l.SchoolId == schoolId.Value);
        var leave = await query.FirstOrDefaultAsync();
        if (leave == null) throw new InvalidOperationException("Leave request was not found for the current school.");

        leave.Status = status;
        db.Leaves.Update(leave);
        await db.SaveChangesAsync();

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                $"Leave{status}",
                "Leave",
                $"{employmentId}:{startDate:yyyy-MM-dd}",
                $"Marked leave request for {leave.Name} as {status}.",
                schoolId ?? leave.SchoolId));
        }
    }
}

public interface IParentPortalRepository
{
    Task<List<ParentRequestEntity>> GetRequestsAsync(string parentUsername, Guid? schoolId = null);
    Task<List<ParentRequestEntity>> GetAllRequestsAsync(string? status = null, string? searchTerm = null, Guid? schoolId = null);
    Task SubmitRequestAsync(ParentRequestEntity request);
    Task UpdateRequestStatusAsync(int requestId, string status, Guid? schoolId = null, string? actorUsername = null);
    Task<List<AssignmentEntity>> GetClassAssignmentsAsync(string classId, Guid? schoolId = null);
}

public class ParentPortalRepository(IDbContextFactory<AppDbContext> factory, IAuditLogService? audit = null) : IParentPortalRepository
{
    public async Task<List<ParentRequestEntity>> GetRequestsAsync(string parentUsername, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var q = db.ParentRequests.AsNoTracking().Where(r => r.ParentUsername == parentUsername);
        if (schoolId.HasValue) q = q.Where(r => r.SchoolId == schoolId.Value);
        return await q.OrderByDescending(r => r.CreatedDate).ToListAsync();
    }

    public async Task<List<ParentRequestEntity>> GetAllRequestsAsync(string? status = null, string? searchTerm = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ParentRequests.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(r => r.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeParentRequestStatus(status);
            query = query.Where(r => r.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.ParentUsername.ToLower().Contains(term) ||
                r.RequestType.ToLower().Contains(term) ||
                (r.Details != null && r.Details.ToLower().Contains(term)) ||
                r.StudentID.ToString().Contains(term));
        }

        return await query.OrderByDescending(r => r.CreatedDate).ToListAsync();
    }

    public async Task SubmitRequestAsync(ParentRequestEntity request)
    {
        using var db = await factory.CreateDbContextAsync();
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.ParentUsername)) throw new ArgumentException("Parent username is required.", nameof(request));
        if (request.StudentID <= 0) throw new ArgumentException("A ward must be selected.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.RequestType)) throw new ArgumentException("Request type is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Details)) throw new ArgumentException("Request details are required.", nameof(request));

        var wardQuery = db.Students.AsNoTracking()
            .Where(s => s.StudentID == request.StudentID && s.ParentUsername == request.ParentUsername);
        if (request.SchoolId.HasValue) wardQuery = wardQuery.Where(s => s.SchoolId == request.SchoolId.Value);
        var ownsWard = await wardQuery.AnyAsync();
        if (!ownsWard) throw new InvalidOperationException("Selected ward does not belong to this parent account.");

        request.RequestType = request.RequestType.Trim();
        request.Details = request.Details.Trim();
        request.Status = NormalizeParentRequestStatus(string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status);
        request.CreatedDate = DateTime.Now;
        db.ParentRequests.Add(request);
        await db.SaveChangesAsync();
    }

    public async Task UpdateRequestStatusAsync(int requestId, string status, Guid? schoolId = null, string? actorUsername = null)
    {
        if (requestId <= 0) throw new ArgumentException("Request ID is required.", nameof(requestId));
        var normalizedStatus = NormalizeParentRequestStatus(status);

        using var db = await factory.CreateDbContextAsync();
        var query = db.ParentRequests.Where(r => r.RequestID == requestId);
        if (schoolId.HasValue) query = query.Where(r => r.SchoolId == schoolId.Value);

        var request = await query.FirstOrDefaultAsync();
        if (request == null) throw new InvalidOperationException("Parent request was not found for the current school.");

        request.Status = normalizedStatus;
        db.ParentRequests.Update(request);
        await db.SaveChangesAsync();

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                $"ParentRequest{normalizedStatus}",
                "ParentRequest",
                requestId.ToString(),
                $"Marked {request.RequestType} request for student {request.StudentID} as {normalizedStatus}.",
                schoolId ?? request.SchoolId));
        }
    }

    public async Task<List<AssignmentEntity>> GetClassAssignmentsAsync(string classId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Assignments.AsNoTracking().Where(a => a.ClassID == classId);
        // Assuming AssignmentEntity doesn't have SchoolId based on the schema seen earlier,
        // but if we need filtering, we'll keep it simple for now as it's class-based.
        return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
    }

    private static string NormalizeParentRequestStatus(string status)
    {
        var normalized = (status ?? "").Trim().ToUpperInvariant();
        return normalized switch
        {
            "APPROVED" => "Approved",
            "REJECTED" => "Rejected",
            "PENDING" => "Pending",
            _ => throw new ArgumentException("Parent request status must be Approved, Rejected, or Pending.", nameof(status))
        };
    }
}
