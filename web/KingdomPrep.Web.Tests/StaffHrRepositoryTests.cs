using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class StaffHrRepositoryTests
{
    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AppDbContext(options));
    }

    private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task EmployeeRepository_FiltersByDepartmentSearchAndSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new EmployeeRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Employees.AddRange(
                new EmployeeEntity { EmployeeID = 1, FullName = "Ama Mensah", Department = "Teaching", Position = "Teacher", SchoolId = schoolId },
                new EmployeeEntity { EmployeeID = 2, FullName = "Kofi Boateng", Department = "Accounts", Position = "Accountant", SchoolId = schoolId },
                new EmployeeEntity { EmployeeID = 3, FullName = "Other Staff", Department = "Teaching", Position = "Teacher", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var employees = await repo.GetEmployeesAsync("ama", "Teaching", schoolId);
        var departments = await repo.GetDepartmentsAsync(schoolId);

        Assert.Single(employees);
        Assert.Equal("Ama Mensah", employees[0].FullName);
        Assert.Equal(new[] { "Accounts", "Teaching" }, departments);
    }

    [Fact]
    public async Task LeaveRepository_SubmitsValidatedPendingLeave()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new LeavePortalRepository(new TestDbContextFactory(options));

        await repo.SubmitLeaveAsync(new LeaveEntity
        {
            EmploymentID = 1,
            Name = "Ama Mensah",
            Department = "Teaching",
            Position = "Teacher",
            LeaveOption = "Sick Leave",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 3),
            Status = "",
            SchoolId = schoolId
        });

        using var verify = new AppDbContext(options);
        var leave = Assert.Single(verify.Leaves);
        Assert.Equal("PENDING", leave.Status);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.SubmitLeaveAsync(new LeaveEntity
        {
            EmploymentID = 1,
            LeaveOption = "Sick Leave",
            StartDate = new DateTime(2026, 7, 5),
            EndDate = new DateTime(2026, 7, 1),
            SchoolId = schoolId
        }));
    }

    [Fact]
    public async Task LeaveRepository_FiltersQueueByStatusSearchAndSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new LeavePortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Leaves.AddRange(
                new LeaveEntity { EmploymentID = 1, Name = "Ama Mensah", Department = "Teaching", Position = "Teacher", LeaveOption = "Sick Leave", StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 7, 2), Status = "PENDING", SchoolId = schoolId },
                new LeaveEntity { EmploymentID = 2, Name = "Kofi Boateng", Department = "Accounts", Position = "Accountant", LeaveOption = "Casual Leave", StartDate = new DateTime(2026, 7, 3), EndDate = new DateTime(2026, 7, 3), Status = "APPROVED", SchoolId = schoolId },
                new LeaveEntity { EmploymentID = 3, Name = "Other Staff", Department = "Teaching", Position = "Teacher", LeaveOption = "Sick Leave", StartDate = new DateTime(2026, 7, 4), EndDate = new DateTime(2026, 7, 4), Status = "PENDING", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var leaves = await repo.GetLeavesAsync("PENDING", "teaching", schoolId);

        Assert.Single(leaves);
        Assert.Equal("Ama Mensah", leaves[0].Name);
    }

    [Fact]
    public async Task LeaveRepository_UpdatesStatusOnlyForCurrentSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new LeavePortalRepository(new TestDbContextFactory(options));
        var start = new DateTime(2026, 7, 1);

        using (var db = new AppDbContext(options))
        {
            db.Leaves.AddRange(
                new LeaveEntity { EmploymentID = 1, Name = "Ama Mensah", LeaveOption = "Sick Leave", StartDate = start, EndDate = start, Status = "PENDING", SchoolId = schoolId },
                new LeaveEntity { EmploymentID = 2, Name = "Other Ama", LeaveOption = "Sick Leave", StartDate = start, EndDate = start, Status = "PENDING", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        await repo.UpdateLeaveStatusAsync(1, start, "approved", schoolId);

        using var verify = new AppDbContext(options);
        Assert.Equal("APPROVED", verify.Leaves.Single(l => l.SchoolId == schoolId).Status);
        Assert.Equal("PENDING", verify.Leaves.Single(l => l.SchoolId == otherSchoolId).Status);
    }
}
