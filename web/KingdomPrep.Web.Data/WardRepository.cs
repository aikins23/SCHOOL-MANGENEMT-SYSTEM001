using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IWardRepository
{
    Task<StudentEntity?> GetWardAsync(int? studentInternalId);
}

public class WardRepository(AppDbContext db) : IWardRepository
{
    public async Task<StudentEntity?> GetWardAsync(int? studentInternalId)
    {
        if (studentInternalId == null) return null;
        
        string idStr = studentInternalId.Value.ToString();
        return await db.Students
            .FirstOrDefaultAsync(s => s.StudentID == idStr);
    }
}
