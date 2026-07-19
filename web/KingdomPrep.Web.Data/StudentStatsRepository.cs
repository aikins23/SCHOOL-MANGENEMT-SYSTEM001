using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IStudentStats
{
    Task<int> CountAsync(Guid? schoolId = null);
}

public class StudentStatsRepository(IDbContextFactory<AppDbContext> factory) : IStudentStats
{
    public async Task<int> CountAsync(Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.Students.AsQueryable();
        if (schoolId.HasValue) query = query.Where(s => s.SchoolId == schoolId.Value);
        return await query.CountAsync();
    }
}
