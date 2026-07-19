using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class StudentRepositoryTests
{
    // The repository wraps every call in `using var db = factory.CreateDbContext()`,
    // so the factory must hand back a FRESH context each time (not one shared
    // instance that gets disposed after the first call). All contexts share the
    // same in-memory database name, so they see the same data.
    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(new AppDbContext(options));
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task GetAllAsync_FiltersBySearchTermAndClass()
    {
        // Arrange
        var options = NewInMemoryOptions();
        var repo = new StudentRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Students.AddRange(
                new StudentEntity { FirstName = "Alice", LastName = "Smith", ClassID = "BASIC 1" },
                new StudentEntity { FirstName = "Bob", LastName = "Jones", ClassID = "BASIC 2" },
                new StudentEntity { FirstName = "Charlie", LastName = "Brown", ClassID = "BASIC 1" }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var result1 = await repo.GetAllAsync("Alice", "");
        var result2 = await repo.GetAllAsync("", "BASIC 1");
        var result3 = await repo.GetAllAsync("Charlie", "BASIC 2");

        // Assert
        Assert.Single(result1);
        Assert.Equal("Alice", result1[0].FirstName);

        Assert.Equal(2, result2.Count);

        Assert.Empty(result3);
    }

    [Fact]
    public async Task AddAsync_AddsStudent()
    {
        // Arrange
        var options = NewInMemoryOptions();
        var repo = new StudentRepository(new TestDbContextFactory(options));
        var student = new StudentEntity { FirstName = "New", LastName = "Kid", ClassID = "BASIC 3" };

        // Act
        await repo.AddAsync(student);

        // Assert
        var students = await repo.GetAllAsync("", "");
        Assert.Single(students);
        Assert.Equal("New Kid", students[0].FullName);
    }
}
