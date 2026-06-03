using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IStudentStats
{
    Task<int> CountAsync();
}

public class StudentStatsRepository(AppDbContext db) : IStudentStats
{
    public Task<int> CountAsync() => db.Students.CountAsync();
}
