using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public sealed record PerformanceReportDetail(
    ClassPerformanceReportEntity Report,
    string Status,
    List<StudentPerformanceEntryEntity> Entries);

public interface IPerformanceReportRepository
{
    Task<List<PerformanceReportDetail>> GetTeacherReportsAsync(int teacherId, Guid? schoolId = null);
    Task<PerformanceReportDetail> SubmitTeacherReportAsync(
        int teacherId,
        ClassPerformanceReportEntity report,
        IEnumerable<StudentPerformanceEntryEntity> entries,
        Guid? schoolId = null);
    Task<List<PerformanceReportDetail>> GetPendingApprovalReportsAsync(Guid? schoolId = null);
    Task SetApprovalStatusAsync(int reportId, string status, Guid? schoolId = null);
    Task<List<PerformanceReportDetail>> GetApprovedReportsForStudentAsync(string parentUsername, int studentId, Guid? schoolId = null);
}

public sealed class PerformanceReportRepository(IDbContextFactory<AppDbContext> factory) : IPerformanceReportRepository
{
    private static readonly string[] ValidPeriods = ["Weekly", "Monthly", "Termly"];
    private static readonly string[] ValidTrends = ["Improving", "Stable", "Declining"];

    public async Task<List<PerformanceReportDetail>> GetTeacherReportsAsync(int teacherId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ClassPerformanceReports.AsNoTracking().Where(r => r.TeacherId == teacherId);
        if (schoolId.HasValue) query = query.Where(r => r.SchoolId == schoolId.Value);

        var reports = await query.OrderByDescending(r => r.ReportDate).Take(50).ToListAsync();
        return await HydrateAsync(db, reports);
    }

    public async Task<PerformanceReportDetail> SubmitTeacherReportAsync(
        int teacherId,
        ClassPerformanceReportEntity report,
        IEnumerable<StudentPerformanceEntryEntity> entries,
        Guid? schoolId = null)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (teacherId <= 0) throw new ArgumentException("Teacher ID is required.", nameof(teacherId));

        NormalizeReport(report);
        if (!ValidPeriods.Contains(report.ReportPeriod)) throw new ArgumentException("Report period must be Weekly, Monthly, or Termly.", nameof(report));
        if (string.IsNullOrWhiteSpace(report.ClassId)) throw new ArgumentException("Class is required.", nameof(report));
        if (string.IsNullOrWhiteSpace(report.AcademicYear)) throw new ArgumentException("Academic year is required.", nameof(report));
        if (string.IsNullOrWhiteSpace(report.Term)) throw new ArgumentException("Academic term is required.", nameof(report));
        if (string.IsNullOrWhiteSpace(report.ReportText)) throw new ArgumentException("Report summary is required.", nameof(report));

        using var db = await factory.CreateDbContextAsync();
        if (schoolId.HasValue && !await TeacherOwnsClassAsync(db, teacherId, report.ClassId, schoolId.Value))
        {
            throw new InvalidOperationException("Selected class does not belong to the signed-in teacher.");
        }

        var classStudentIds = await db.Students.AsNoTracking()
            .Where(s => s.ClassID == report.ClassId && (!schoolId.HasValue || s.SchoolId == schoolId.Value))
            .Select(s => s.StudentID.ToString())
            .ToListAsync();
        var validStudentIds = classStudentIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cleanEntries = new List<StudentPerformanceEntryEntity>();
        foreach (var entry in entries.Where(e => e != null))
        {
            if (!validStudentIds.Contains(entry.StudentId)) continue;

            var trend = (entry.PerformanceTrend ?? "").Trim();
            cleanEntries.Add(new StudentPerformanceEntryEntity
            {
                StudentId = entry.StudentId.Trim(),
                PerformanceTrend = ValidTrends.Contains(trend) ? trend : "Stable",
                ExerciseMarksObtained = NormalizeMark(entry.ExerciseMarksObtained, nameof(entry.ExerciseMarksObtained)),
                ExerciseMarksTotal = NormalizeMark(entry.ExerciseMarksTotal, nameof(entry.ExerciseMarksTotal)),
                HomeworkMarksObtained = NormalizeMark(entry.HomeworkMarksObtained, nameof(entry.HomeworkMarksObtained)),
                HomeworkMarksTotal = NormalizeMark(entry.HomeworkMarksTotal, nameof(entry.HomeworkMarksTotal)),
                TeacherNotes = string.IsNullOrWhiteSpace(entry.TeacherNotes) ? null : entry.TeacherNotes.Trim()
            });
        }

        foreach (var entry in cleanEntries)
        {
            ValidateMarkPair(entry.ExerciseMarksObtained, entry.ExerciseMarksTotal, "exercise");
            ValidateMarkPair(entry.HomeworkMarksObtained, entry.HomeworkMarksTotal, "homework");
        }

        cleanEntries = cleanEntries
            .Where(e => !string.IsNullOrWhiteSpace(e.TeacherNotes) ||
                        e.PerformanceTrend != "Stable" ||
                        HasMarks(e))
            .ToList();

        if (!cleanEntries.Any()) throw new ArgumentException("At least one student performance entry is required.", nameof(entries));

        var now = DateTime.UtcNow;
        ApprovalWorkflowEntity workflow;
        if (report.ReportId == 0)
        {
            workflow = new ApprovalWorkflowEntity
            {
                EntityType = "ClassPerformanceReport",
                EntityId = 0,
                CurrentStatus = "Submitted",
                SchoolId = schoolId,
                SyncId = Guid.NewGuid(),
                UpdatedAt = now
            };
            db.ApprovalWorkflows.Add(workflow);
            await db.SaveChangesAsync();

            report.TeacherId = teacherId;
            report.ReportDate = now;
            report.SchoolId = schoolId;
            report.WorkflowId = workflow.WorkflowId;
            report.SyncId ??= Guid.NewGuid();
            report.UpdatedAt = now;
            db.ClassPerformanceReports.Add(report);
            await db.SaveChangesAsync();

            workflow.EntityId = report.ReportId;
            workflow.UpdatedAt = now;
        }
        else
        {
            var existing = await db.ClassPerformanceReports.FirstOrDefaultAsync(r =>
                r.ReportId == report.ReportId &&
                r.TeacherId == teacherId &&
                (!schoolId.HasValue || r.SchoolId == schoolId.Value));
            if (existing == null) throw new InvalidOperationException("Performance report was not found for this teacher.");

            workflow = await db.ApprovalWorkflows.FirstAsync(w => w.WorkflowId == existing.WorkflowId);
            if (workflow.CurrentStatus == "Approved") throw new InvalidOperationException("Approved reports cannot be edited.");

            existing.ReportPeriod = report.ReportPeriod;
            existing.AcademicYear = report.AcademicYear;
            existing.Term = report.Term;
            existing.WeekNumber = report.WeekNumber;
            existing.MonthNumber = report.MonthNumber;
            existing.ReportText = report.ReportText;
            existing.AnalyticsData = report.AnalyticsData;
            existing.ReportDate = now;
            existing.SyncId ??= Guid.NewGuid();
            existing.UpdatedAt = now;
            workflow.CurrentStatus = "Submitted";
            workflow.SchoolId ??= schoolId;
            workflow.SyncId ??= Guid.NewGuid();
            workflow.UpdatedAt = now;

            var oldEntries = await db.StudentPerformanceEntries.Where(e => e.ReportId == existing.ReportId).ToListAsync();
            db.StudentPerformanceEntries.RemoveRange(oldEntries);
            report = existing;
        }

        foreach (var entry in cleanEntries)
        {
            entry.ReportId = report.ReportId;
            entry.SchoolId = schoolId;
            entry.SyncId = Guid.NewGuid();
            entry.UpdatedAt = now;
            db.StudentPerformanceEntries.Add(entry);
        }

        await db.SaveChangesAsync();
        return new PerformanceReportDetail(report, workflow.CurrentStatus, cleanEntries);
    }

    public async Task<List<PerformanceReportDetail>> GetPendingApprovalReportsAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query =
            from report in db.ClassPerformanceReports.AsNoTracking()
            join workflow in db.ApprovalWorkflows.AsNoTracking() on report.WorkflowId equals workflow.WorkflowId
            where workflow.CurrentStatus == "Submitted"
            select report;

        if (schoolId.HasValue) query = query.Where(r => r.SchoolId == schoolId.Value);

        var reports = await query.OrderByDescending(r => r.ReportDate).ToListAsync();
        return await HydrateAsync(db, reports);
    }

    public async Task SetApprovalStatusAsync(int reportId, string status, Guid? schoolId = null)
    {
        status = NormalizeStatus(status);

        using var db = await factory.CreateDbContextAsync();
        var report = await db.ClassPerformanceReports.FirstOrDefaultAsync(r =>
            r.ReportId == reportId &&
            (!schoolId.HasValue || r.SchoolId == schoolId.Value));
        if (report == null) throw new InvalidOperationException("Performance report was not found.");

        var workflow = await db.ApprovalWorkflows.FirstOrDefaultAsync(w => w.WorkflowId == report.WorkflowId);
        if (workflow == null) throw new InvalidOperationException("Approval workflow was not found.");

        workflow.CurrentStatus = status;
        workflow.SchoolId ??= report.SchoolId ?? schoolId;
        workflow.SyncId ??= Guid.NewGuid();
        workflow.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<PerformanceReportDetail>> GetApprovedReportsForStudentAsync(string parentUsername, int studentId, Guid? schoolId = null)
    {
        if (string.IsNullOrWhiteSpace(parentUsername)) return new List<PerformanceReportDetail>();

        using var db = await factory.CreateDbContextAsync();
        var ownsWard = await db.Students.AsNoTracking().AnyAsync(s =>
            s.StudentID == studentId &&
            s.ParentUsername == parentUsername &&
            (!schoolId.HasValue || s.SchoolId == schoolId.Value));
        if (!ownsWard) return new List<PerformanceReportDetail>();

        var studentIdText = studentId.ToString();
        var query =
            from report in db.ClassPerformanceReports.AsNoTracking()
            join workflow in db.ApprovalWorkflows.AsNoTracking() on report.WorkflowId equals workflow.WorkflowId
            join entry in db.StudentPerformanceEntries.AsNoTracking() on report.ReportId equals entry.ReportId
            where workflow.CurrentStatus == "Approved" && entry.StudentId == studentIdText
            select report;

        if (schoolId.HasValue) query = query.Where(r => r.SchoolId == schoolId.Value);

        var reports = await query.Distinct().OrderByDescending(r => r.ReportDate).Take(20).ToListAsync();
        return await HydrateAsync(db, reports, studentIdText);
    }

    private static async Task<List<PerformanceReportDetail>> HydrateAsync(AppDbContext db, List<ClassPerformanceReportEntity> reports, string? studentId = null)
    {
        var reportIds = reports.Select(r => r.ReportId).ToList();
        var workflowIds = reports.Select(r => r.WorkflowId).ToList();

        var workflows = await db.ApprovalWorkflows.AsNoTracking()
            .Where(w => workflowIds.Contains(w.WorkflowId))
            .ToDictionaryAsync(w => w.WorkflowId);

        var entriesQuery = db.StudentPerformanceEntries.AsNoTracking().Where(e => reportIds.Contains(e.ReportId));
        if (!string.IsNullOrWhiteSpace(studentId)) entriesQuery = entriesQuery.Where(e => e.StudentId == studentId);
        var entries = await entriesQuery.OrderBy(e => e.StudentId).ToListAsync();

        return reports.Select(r => new PerformanceReportDetail(
                r,
                workflows.TryGetValue(r.WorkflowId, out var workflow) ? workflow.CurrentStatus : "Draft",
                entries.Where(e => e.ReportId == r.ReportId).ToList()))
            .ToList();
    }

    private static async Task<bool> TeacherOwnsClassAsync(AppDbContext db, int teacherId, string classId, Guid schoolId) =>
        await db.ClassAssignments.AsNoTracking()
            .AnyAsync(c => c.ClassTeacherID == teacherId && c.ClassName == classId && c.SchoolId == schoolId);

    private static void NormalizeReport(ClassPerformanceReportEntity report)
    {
        report.ClassId = report.ClassId?.Trim() ?? "";
        report.ReportPeriod = NormalizePeriod(report.ReportPeriod);
        report.AcademicYear = report.AcademicYear?.Trim() ?? "";
        report.Term = report.Term?.Trim() ?? "";
        report.ReportText = report.ReportText?.Trim();
        report.AnalyticsData = report.AnalyticsData?.Trim();
    }

    private static decimal? NormalizeMark(decimal? value, string fieldName)
    {
        if (!value.HasValue) return null;
        if (value.Value < 0) throw new ArgumentException("Marks cannot be negative.", fieldName);
        return decimal.Round(value.Value, 2);
    }

    private static void ValidateMarkPair(decimal? obtained, decimal? total, string label)
    {
        if (obtained.HasValue && total.HasValue && obtained.Value > total.Value)
        {
            throw new ArgumentException($"The {label} marks obtained cannot be greater than the total {label} marks.");
        }
    }

    private static bool HasMarks(StudentPerformanceEntryEntity entry) =>
        entry.ExerciseMarksObtained.HasValue ||
        entry.ExerciseMarksTotal.HasValue ||
        entry.HomeworkMarksObtained.HasValue ||
        entry.HomeworkMarksTotal.HasValue;

    private static string NormalizePeriod(string? period)
    {
        var normalized = (period ?? "").Trim();
        return ValidPeriods.FirstOrDefault(p => string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase)) ?? normalized;
    }

    private static string NormalizeStatus(string status)
    {
        var normalized = (status ?? "").Trim();
        if (string.Equals(normalized, "Approved", StringComparison.OrdinalIgnoreCase)) return "Approved";
        if (string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase)) return "Rejected";
        throw new ArgumentException("Approval status must be Approved or Rejected.", nameof(status));
    }
}
