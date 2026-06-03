using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

/// <summary>
/// Read-only EF Core context over the EXISTING Neat_Academy schema.
/// Do not run EF migrations against this — the desktop app owns the schema.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<StudentEntity> Students => Set<StudentEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserEntity>().HasKey(u => u.Username);
        b.Entity<StudentEntity>().HasKey(s => s.StudentID);
    }
}
