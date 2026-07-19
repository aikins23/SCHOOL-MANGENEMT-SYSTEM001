using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IStudentRemarksRepository
{
    Task<List<StudentTermRemarksEntity>> GetClassRemarksAsync(string classId, string term, string year, Guid? schoolId = null);
    Task SaveHeadTeacherRemarkAsync(string classId, string studentId, string term, string year, string? remark, Guid? schoolId = null);
}

public sealed class StudentRemarksRepository(IDbContextFactory<AppDbContext> factory) : IStudentRemarksRepository
{
    public async Task<List<StudentTermRemarksEntity>> GetClassRemarksAsync(string classId, string term, string year, Guid? schoolId = null)
    {
        if (string.IsNullOrWhiteSpace(classId) || string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(year))
        {
            return new List<StudentTermRemarksEntity>();
        }

        classId = classId.Trim();
        term = term.Trim();
        year = year.Trim();

        await using var db = await factory.CreateDbContextAsync();
        var remarks = await GetClassRemarksCoreAsync(db, classId, term, year, schoolId, includeOnlyUnscopedSchoolRows: false);
        if (remarks.Count == 0 && schoolId.HasValue)
        {
            remarks = await GetClassRemarksCoreAsync(db, classId, term, year, schoolId, includeOnlyUnscopedSchoolRows: true);
        }

        return remarks;
    }

    public async Task SaveHeadTeacherRemarkAsync(string classId, string studentId, string term, string year, string? remark, Guid? schoolId = null)
    {
        if (string.IsNullOrWhiteSpace(classId)) throw new ArgumentException("Class is required.", nameof(classId));
        if (string.IsNullOrWhiteSpace(studentId)) throw new ArgumentException("Student is required.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(term)) throw new ArgumentException("Academic term is required.", nameof(term));
        if (string.IsNullOrWhiteSpace(year)) throw new ArgumentException("Academic year is required.", nameof(year));

        classId = classId.Trim();
        studentId = studentId.Trim();
        term = term.Trim();
        year = year.Trim();
        remark = remark?.Trim();

        await using var db = await factory.CreateDbContextAsync();
        var studentBelongsToClass = await db.Students.AsNoTracking().AnyAsync(s =>
            s.StudentID.ToString() == studentId &&
            s.ClassID == classId &&
            (!schoolId.HasValue || s.SchoolId == schoolId.Value));

        if (!studentBelongsToClass && schoolId.HasValue)
        {
            studentBelongsToClass = await db.Students.AsNoTracking().AnyAsync(s =>
                s.StudentID.ToString() == studentId &&
                s.ClassID == classId &&
                s.SchoolId == null);
        }

        if (!studentBelongsToClass)
        {
            throw new InvalidOperationException("Selected student does not belong to this class.");
        }

        var existing = await db.StudentTermRemarks.FirstOrDefaultAsync(r =>
            r.StudentID == studentId &&
            r.Term == term &&
            r.Year == year &&
            (!schoolId.HasValue || r.SchoolId == schoolId.Value));

        if (existing == null && schoolId.HasValue)
        {
            existing = await db.StudentTermRemarks.FirstOrDefaultAsync(r =>
                r.StudentID == studentId &&
                r.Term == term &&
                r.Year == year &&
                r.SchoolId == null);
        }

        if (existing == null)
        {
            existing = new StudentTermRemarksEntity
            {
                StudentID = studentId,
                Term = term,
                Year = year,
                CreatedDate = DateTime.Now,
                SchoolId = schoolId
            };
            db.StudentTermRemarks.Add(existing);
        }

        if (existing.SchoolId == null && schoolId.HasValue)
        {
            existing.SchoolId = schoolId.Value;
        }

        existing.HeadTeacherRemarks = remark;
        await db.SaveChangesAsync();
    }

    private static async Task<List<StudentTermRemarksEntity>> GetClassRemarksCoreAsync(
        AppDbContext db,
        string classId,
        string term,
        string year,
        Guid? schoolId,
        bool includeOnlyUnscopedSchoolRows)
    {
        var studentQuery = db.Students.AsNoTracking().Where(s => s.ClassID == classId);
        var remarksQuery = db.StudentTermRemarks.AsNoTracking().Where(r => r.Term == term && r.Year == year);

        if (includeOnlyUnscopedSchoolRows)
        {
            studentQuery = studentQuery.Where(s => s.SchoolId == null);
            remarksQuery = remarksQuery.Where(r => r.SchoolId == null);
        }
        else if (schoolId.HasValue)
        {
            studentQuery = studentQuery.Where(s => s.SchoolId == schoolId.Value);
            remarksQuery = remarksQuery.Where(r => r.SchoolId == schoolId.Value);
        }

        var studentIds = await studentQuery
            .Select(s => s.StudentID.ToString())
            .ToListAsync();

        return await remarksQuery
            .Where(r => studentIds.Contains(r.StudentID))
            .ToListAsync();
    }
}
