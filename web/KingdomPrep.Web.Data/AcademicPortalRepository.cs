using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public sealed record ClassSubjectSummary(string ClassName, int SubjectCount);
public sealed record AcademicResultsOverview(
    string ClassName,
    string Subject,
    int StudentCount,
    int RecordedCount,
    decimal AverageScore)
{
    public int MissingCount => Math.Max(0, StudentCount - RecordedCount);
    public decimal CompletionRate => StudentCount == 0 ? 0 : Math.Round((decimal)RecordedCount / StudentCount * 100m, 1);
}

public interface IAcademicPortalRepository
{
    Task<List<ClassSubjectEntity>> GetClassSubjectsAsync(string? classFilter = null, string? searchTerm = null, Guid? schoolId = null);
    Task<List<string>> GetClassesAsync(Guid? schoolId = null);
    Task<List<ClassSubjectSummary>> GetClassSubjectSummaryAsync(Guid? schoolId = null);
    Task<List<AcademicResultsOverview>> GetResultsOverviewAsync(string term, string year, string? classFilter = null, Guid? schoolId = null);
    Task<List<ExamTypeEntity>> GetExamTypesAsync(bool includeInactive = false, Guid? schoolId = null);
    Task<ExamTypeEntity> SaveExamTypeAsync(ExamTypeEntity examType, Guid? schoolId = null);
    Task DeactivateExamTypeAsync(int examTypeId);
    Task EnsureDefaultExamTypesAsync(Guid? schoolId = null);
    Task<ClassSubjectEntity> SaveClassSubjectAsync(ClassSubjectEntity subject, Guid? schoolId = null);
    Task DeleteClassSubjectAsync(int subjectId, Guid? schoolId = null);
}

public class AcademicPortalRepository(IDbContextFactory<AppDbContext> factory) : IAcademicPortalRepository
{
    private static readonly ExamTypeEntity[] DefaultExamTypes =
    [
        new() { Name = "End of Term Examination", Code = "EOT", Description = "Standard end of term examination.", WeightPercentage = 70, IncludeInReportCard = true, IsGradedExam = true, DisplayOrder = 10, IsSystemType = true },
        new() { Name = "Mid-Term Examination", Code = "MID", Description = "Mid-term assessment within the active term.", WeightPercentage = 20, IncludeInReportCard = true, IsGradedExam = true, DisplayOrder = 20, IsSystemType = true },
        new() { Name = "Class Test", Code = "CT", Description = "Regular classroom test or continuous assessment.", WeightPercentage = 10, IncludeInReportCard = true, IsGradedExam = true, DisplayOrder = 30, IsSystemType = true },
        new() { Name = "Mock Examination", Code = "MOCK", Description = "Mock examination, usually for candidate classes.", WeightPercentage = 0, IncludeInReportCard = false, IsGradedExam = true, DisplayOrder = 40, IsSystemType = true },
        new() { Name = "Quiz", Code = "QUIZ", Description = "Short quiz assessment.", WeightPercentage = 0, IncludeInReportCard = false, IsGradedExam = true, DisplayOrder = 50, IsSystemType = true },
        new() { Name = "Assignment", Code = "ASSIGN", Description = "Assignment-based assessment.", WeightPercentage = 0, IncludeInReportCard = false, IsGradedExam = true, DisplayOrder = 60, IsSystemType = true }
    ];

    public async Task<List<ClassSubjectEntity>> GetClassSubjectsAsync(string? classFilter = null, string? searchTerm = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ClassSubjects.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        if (!string.IsNullOrWhiteSpace(classFilter)) query = query.Where(s => s.ClassName == classFilter);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s =>
                (s.ClassName != null && s.ClassName.ToLower().Contains(term)) ||
                (s.Subject != null && s.Subject.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(s => s.ClassName)
            .ThenBy(s => s.SortOrder ?? 999)
            .ThenBy(s => s.Subject)
            .ToListAsync();
    }

    public async Task<List<string>> GetClassesAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var assignmentQuery = db.ClassAssignments.AsNoTracking().AsQueryable();
        var subjectQuery = db.ClassSubjects.AsNoTracking().AsQueryable();
        var studentQuery = db.Students.AsNoTracking()
            .Where(s => s.ClassID != null && s.ClassID != "" && s.ClassID != "GRADUATED");
        if (schoolId.HasValue)
        {
            assignmentQuery = assignmentQuery.Where(c => c.SchoolId == schoolId.Value);
            subjectQuery = subjectQuery.Where(c => c.SchoolId == schoolId.Value);
            studentQuery = studentQuery.Where(s => s.SchoolId == schoolId.Value);
        }

        var classes = await assignmentQuery.Select(c => c.ClassName)
            .Concat(subjectQuery.Where(c => c.ClassName != null).Select(c => c.ClassName!))
            .Concat(studentQuery.Select(s => s.ClassID!))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        if (classes.Count == 0 && schoolId.HasValue)
        {
            classes = await db.Students.AsNoTracking()
                .Where(s => s.SchoolId == null && s.ClassID != null && s.ClassID != "" && s.ClassID != "GRADUATED")
                .Select(s => s.ClassID!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        return classes;
    }

    public async Task<List<ClassSubjectSummary>> GetClassSubjectSummaryAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ClassSubjects.AsNoTracking().Where(s => s.ClassName != null && s.Subject != null);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);

        var rows = await query
            .GroupBy(s => s.ClassName!)
            .Select(g => new { ClassName = g.Key, SubjectCount = g.Count() })
            .OrderBy(g => g.ClassName)
            .ToListAsync();

        return rows.Select(r => new ClassSubjectSummary(r.ClassName, r.SubjectCount)).ToList();
    }

    public async Task<List<AcademicResultsOverview>> GetResultsOverviewAsync(string term, string year, string? classFilter = null, Guid? schoolId = null)
    {
        if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(year)) return new List<AcademicResultsOverview>();

        using var db = await factory.CreateDbContextAsync();

        var subjectQuery = db.ClassSubjects.AsNoTracking().Where(s => s.ClassName != null && s.Subject != null);
        var studentQuery = db.Students.AsNoTracking().Where(s => s.ClassID != null && s.ClassID != "GRADUATED");
        var gradeQuery = db.Exams.AsNoTracking().Where(e => e.Term == term && e.Year == year && e.ClassId != null && e.Subject != null);

        if (schoolId.HasValue)
        {
            subjectQuery = subjectQuery.Where(s => s.SchoolId == schoolId.Value);
            studentQuery = studentQuery.Where(s => s.SchoolId == schoolId.Value);
            gradeQuery = gradeQuery.Where(e => e.SchoolId == schoolId.Value);
        }

        if (!string.IsNullOrWhiteSpace(classFilter))
        {
            subjectQuery = subjectQuery.Where(s => s.ClassName == classFilter);
            studentQuery = studentQuery.Where(s => s.ClassID == classFilter);
            gradeQuery = gradeQuery.Where(e => e.ClassId == classFilter);
        }

        var subjects = await subjectQuery
            .Select(s => new { ClassName = s.ClassName!, Subject = s.Subject!, SortOrder = s.SortOrder ?? 999 })
            .Distinct()
            .OrderBy(s => s.ClassName)
            .ThenBy(s => s.SortOrder)
            .ThenBy(s => s.Subject)
            .ToListAsync();

        var students = await studentQuery
            .Select(s => new { s.StudentID, ClassName = s.ClassID! })
            .ToListAsync();

        var grades = await gradeQuery
            .Where(e => e.StudentId.HasValue)
            .Select(e => new { ClassName = e.ClassId!, Subject = e.Subject!, StudentId = e.StudentId!.Value, e.TotalScore })
            .ToListAsync();

        var result = new List<AcademicResultsOverview>();
        foreach (var subject in subjects)
        {
            var classStudents = students.Where(s => s.ClassName == subject.ClassName).Select(s => s.StudentID).Distinct().ToList();
            var gradeRows = grades
                .Where(g => g.ClassName == subject.ClassName && g.Subject == subject.Subject && classStudents.Contains(g.StudentId))
                .GroupBy(g => g.StudentId)
                .Select(g => g.OrderByDescending(x => x.TotalScore.HasValue).First())
                .ToList();

            var scoredRows = gradeRows.Where(g => g.TotalScore.HasValue).Select(g => (decimal)g.TotalScore!.Value).ToList();
            var average = scoredRows.Count == 0 ? 0 : Math.Round(scoredRows.Average(), 1);

            result.Add(new AcademicResultsOverview(subject.ClassName, subject.Subject, classStudents.Count, gradeRows.Count, average));
        }

        return result;
    }

    public async Task<List<ExamTypeEntity>> GetExamTypesAsync(bool includeInactive = false, Guid? schoolId = null)
    {
        await EnsureDefaultExamTypesAsync(schoolId);

        using var db = await factory.CreateDbContextAsync();
        var query = db.ExamTypes.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(e => e.IsActive);
        if (schoolId.HasValue) query = query.Where(e => e.SchoolId == schoolId.Value);
        else query = query.Where(e => e.SchoolId == null);

        return await query
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<ExamTypeEntity> SaveExamTypeAsync(ExamTypeEntity examType, Guid? schoolId = null)
    {
        if (examType == null) throw new ArgumentNullException(nameof(examType));

        examType.Name = examType.Name?.Trim() ?? "";
        examType.Code = (examType.Code ?? "").Trim().ToUpperInvariant();
        examType.Description = string.IsNullOrWhiteSpace(examType.Description) ? null : examType.Description.Trim();
        if (schoolId.HasValue) examType.SchoolId = schoolId.Value;

        if (string.IsNullOrWhiteSpace(examType.Name)) throw new ArgumentException("Exam type name is required.", nameof(examType));
        if (string.IsNullOrWhiteSpace(examType.Code)) throw new ArgumentException("Exam type code is required.", nameof(examType));
        if (examType.Code.Length > 20) throw new ArgumentException("Exam type code cannot be longer than 20 characters.", nameof(examType));
        if (examType.WeightPercentage < 0 || examType.WeightPercentage > 100) throw new ArgumentException("Weight percentage must be between 0 and 100.", nameof(examType));

        using var db = await factory.CreateDbContextAsync();
        var duplicate = await db.ExamTypes.AsNoTracking().AnyAsync(e =>
            e.ExamTypeId != examType.ExamTypeId &&
            e.Code == examType.Code &&
            e.SchoolId == examType.SchoolId);
        if (duplicate) throw new InvalidOperationException("An exam type with this code already exists.");

        if (examType.ExamTypeId == 0)
        {
            examType.IsActive = true;
            examType.CreatedDate = DateTime.UtcNow;
            db.ExamTypes.Add(examType);
        }
        else
        {
            var existing = await db.ExamTypes.FirstOrDefaultAsync(e => e.ExamTypeId == examType.ExamTypeId);
            if (existing == null) throw new InvalidOperationException("Exam type was not found.");

            existing.Name = examType.Name;
            existing.Description = examType.Description;
            existing.WeightPercentage = examType.WeightPercentage;
            existing.IsGradedExam = examType.IsGradedExam;
            existing.IncludeInReportCard = examType.IncludeInReportCard;
            existing.DisplayOrder = examType.DisplayOrder;
            existing.IsActive = examType.IsActive;
            existing.SchoolId = examType.SchoolId;

            if (!existing.IsSystemType)
            {
                existing.Code = examType.Code;
            }

            examType = existing;
        }

        await db.SaveChangesAsync();
        await TouchExamTypeAsync(db, examType.ExamTypeId);
        return examType;
    }

    public async Task DeactivateExamTypeAsync(int examTypeId)
    {
        using var db = await factory.CreateDbContextAsync();
        var examType = await db.ExamTypes.FirstOrDefaultAsync(e => e.ExamTypeId == examTypeId);
        if (examType == null) return;
        if (examType.IsSystemType) throw new InvalidOperationException("System exam types cannot be deactivated.");

        examType.IsActive = false;
        await db.SaveChangesAsync();
        await TouchExamTypeAsync(db, examType.ExamTypeId);
    }

    public async Task EnsureDefaultExamTypesAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var existingCodes = await db.ExamTypes
            .Where(e => e.SchoolId == schoolId)
            .Select(e => e.Code)
            .ToListAsync();

        foreach (var defaultType in DefaultExamTypes)
        {
            if (existingCodes.Any(c => c == defaultType.Code)) continue;

            db.ExamTypes.Add(new ExamTypeEntity
            {
                Name = defaultType.Name,
                Code = defaultType.Code,
                Description = defaultType.Description,
                WeightPercentage = defaultType.WeightPercentage,
                IsGradedExam = defaultType.IsGradedExam,
                IncludeInReportCard = defaultType.IncludeInReportCard,
                DisplayOrder = defaultType.DisplayOrder,
                SchoolId = schoolId,
                IsSystemType = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task TouchExamTypeAsync(AppDbContext db, int examTypeId)
    {
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ExamTypes SET UpdatedAt = SYSUTCDATETIME() WHERE ExamTypeId = {examTypeId}");
        }
        catch
        {
            // Older in-memory/unit-test providers or legacy schemas may not expose UpdatedAt.
        }
    }

    public async Task<ClassSubjectEntity> SaveClassSubjectAsync(ClassSubjectEntity subject, Guid? schoolId = null)
    {
        if (subject == null) throw new ArgumentNullException(nameof(subject));
        subject.ClassName = subject.ClassName?.Trim();
        subject.Subject = subject.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(subject.ClassName)) throw new ArgumentException("Class is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(subject.Subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        if (schoolId.HasValue) subject.SchoolId = schoolId.Value;

        using var db = await factory.CreateDbContextAsync();
        var duplicate = await db.ClassSubjects.AsNoTracking().AnyAsync(s =>
            s.Id != subject.Id &&
            s.ClassName == subject.ClassName &&
            s.Subject == subject.Subject &&
            s.SchoolId == subject.SchoolId);
        if (duplicate) throw new InvalidOperationException("This subject already exists for the selected class.");

        if (subject.Id == 0)
        {
            subject.SyncId ??= Guid.NewGuid();
            db.ClassSubjects.Add(subject);
        }
        else
        {
            var existing = await db.ClassSubjects.FirstOrDefaultAsync(s => s.Id == subject.Id && (!schoolId.HasValue || s.SchoolId == schoolId.Value));
            if (existing == null) throw new InvalidOperationException("Subject was not found for the current school.");
            existing.ClassName = subject.ClassName;
            existing.Subject = subject.Subject;
            existing.SortOrder = subject.SortOrder;
            if (existing.SyncId == null) existing.SyncId = Guid.NewGuid();
            subject = existing;
        }

        await db.SaveChangesAsync();
        return subject;
    }

    public async Task DeleteClassSubjectAsync(int subjectId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ClassSubjects.Where(s => s.Id == subjectId);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        var subject = await query.FirstOrDefaultAsync();
        if (subject == null) return;

        db.ClassSubjects.Remove(subject);
        await db.SaveChangesAsync();
    }
}
