using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KingdomPrep.Web.Data;

public interface IStudentRepository
{
    Task<List<StudentEntity>> GetAllAsync(string? searchTerm = null, string? classFilter = null, Guid? schoolId = null);
      Task<(List<StudentEntity> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string sortColumn, bool sortAscending, string? searchTerm = null, string? classFilter = null, Guid? schoolId = null);
    Task<StudentEntity?> GetByIdAsync(string studentId, Guid? schoolId = null);
    Task UpdateAsync(StudentEntity student, Guid? schoolId = null);
    Task AddAsync(StudentEntity student, Guid? schoolId = null);
    Task DeleteAsync(int studentId, Guid? schoolId = null);
    Task<List<string>> GetClassesAsync(Guid? schoolId = null);
}

public class StudentRepository(IDbContextFactory<AppDbContext> factory) : IStudentRepository
{

    public async Task<(List<StudentEntity> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string sortColumn, bool sortAscending, string? searchTerm = null, string? classFilter = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Students.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);

        if (!string.IsNullOrWhiteSpace(classFilter))
        {
            query = query.Where(s => s.ClassID == classFilter);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (int.TryParse(searchTerm, out int searchId))
            {
                query = query.Where(s => s.StudentID == searchId || (s.FirstName != null && s.FirstName.Contains(searchTerm)) || (s.LastName != null && s.LastName.Contains(searchTerm)));
            }
            else
            {
                query = query.Where(s => (s.FirstName != null && s.FirstName.Contains(searchTerm)) || (s.LastName != null && s.LastName.Contains(searchTerm)));
            }
        }

        int totalCount = await query.CountAsync();

        query = sortColumn switch
        {
            "ID" => sortAscending ? query.OrderBy(s => s.StudentID) : query.OrderByDescending(s => s.StudentID),
            "Name" => sortAscending ? query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName) : query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName),
            "Class" => sortAscending ? query.OrderBy(s => s.ClassID) : query.OrderByDescending(s => s.ClassID),
            "Gender" => sortAscending ? query.OrderBy(s => s.Gender) : query.OrderByDescending(s => s.Gender),
            "DOB" => sortAscending ? query.OrderBy(s => s.DateOfBirth) : query.OrderByDescending(s => s.DateOfBirth),
            _ => sortAscending ? query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName) : query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<List<StudentEntity>> GetAllAsync(string? searchTerm = null, string? classFilter = null, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = BuildStudentLookupQuery(db, searchTerm, classFilter, schoolId, includeOnlyUnscopedSchoolRows: false);

        var students = await query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync();
        if (students.Count == 0 && schoolId.HasValue)
        {
            students = await BuildStudentLookupQuery(db, searchTerm, classFilter, schoolId, includeOnlyUnscopedSchoolRows: true)
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();
        }

        return students;
    }

    private static IQueryable<StudentEntity> BuildStudentLookupQuery(
        AppDbContext db,
        string? searchTerm,
        string? classFilter,
        Guid? schoolId,
        bool includeOnlyUnscopedSchoolRows)
    {
        var query = db.Students.AsNoTracking().AsQueryable();
        if (includeOnlyUnscopedSchoolRows)
        {
            query = query.Where(s => s.SchoolId == null);
        }
        else if (schoolId.HasValue)
        {
            query = query.Where(s => s.SchoolId == schoolId.Value);
        }

        if (!string.IsNullOrWhiteSpace(classFilter))
        {
            query = query.Where(s => s.ClassID == classFilter);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (int.TryParse(searchTerm, out int searchId))
            {
                query = query.Where(s =>
                    s.StudentID == searchId ||
                    (s.FirstName != null && s.FirstName.Contains(searchTerm)) ||
                    (s.LastName != null && s.LastName.Contains(searchTerm)));
            }
            else
            {
                query = query.Where(s =>
                    (s.FirstName != null && s.FirstName.Contains(searchTerm)) ||
                    (s.LastName != null && s.LastName.Contains(searchTerm)));
            }
        }

        return query;
    }

    public async Task<StudentEntity?> GetByIdAsync(string studentId, Guid? schoolId = null)
    {
        if (!int.TryParse(studentId, out int parsedId)) return null;
        using var db = await factory.CreateDbContextAsync();
        var query = db.Students.AsQueryable().Where(s => s.StudentID == parsedId);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(StudentEntity student, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        if (schoolId.HasValue)
        {
            student.SchoolId = schoolId.Value;
            var belongsToSchool = await db.Students.AnyAsync(s => s.StudentID == student.StudentID && s.SchoolId == schoolId.Value);
            if (!belongsToSchool) throw new InvalidOperationException("Student does not belong to the signed-in school.");
        }

        db.Students.Update(student);
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(StudentEntity student, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        if (schoolId.HasValue) student.SchoolId = schoolId.Value;
        db.Students.Add(student);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int studentId, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Students.Where(s => s.StudentID == studentId);
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        var student = await query.FirstOrDefaultAsync();
        if (student != null)
        {
            db.Students.Remove(student);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetClassesAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.ClassAssignments.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(c => c.SchoolId == schoolId.Value);
        return await query.Select(c => c.ClassName).Distinct().OrderBy(c => c).ToListAsync();
    }
}
