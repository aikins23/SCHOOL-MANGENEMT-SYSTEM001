using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface IUserRepository
{
    Task<UserEntity?> FindByUsernameAsync(string username);
}

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<UserEntity?> FindByUsernameAsync(string username) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
}
