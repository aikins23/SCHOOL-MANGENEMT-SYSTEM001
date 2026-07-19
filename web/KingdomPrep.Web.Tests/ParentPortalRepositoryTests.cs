using KingdomPrep.Web.Data;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class ParentPortalRepositoryTests
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
    public async Task GetResultsAsync_ReturnsOnlyPublishedPortalResults()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new ReportCardPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Exams.AddRange(
                new ExamResultEntity { ExamID = 1, StudentId = 101, Subject = "English", Term = "First Term", Year = "2025/2026", ExamTypeId = 1, SchoolId = schoolId },
                new ExamResultEntity { ExamID = 2, StudentId = 101, Subject = "Math", Term = "Second Term", Year = "2025/2026", ExamTypeId = 1, SchoolId = schoolId });
            db.ExamSetups.AddRange(
                new ExamSetupEntity { SetupID = 1, Term = "First Term", Year = "2025/2026", ExamTypeId = 1, IsPublishedToPortal = true, SchoolId = schoolId },
                new ExamSetupEntity { SetupID = 2, Term = "Second Term", Year = "2025/2026", ExamTypeId = 1, IsPublishedToPortal = false, SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var published = await repo.GetResultsAsync("101", "First Term", "2025/2026", schoolId);
        var unpublished = await repo.GetResultsAsync("101", "Second Term", "2025/2026", schoolId);

        Assert.Single(published);
        Assert.Empty(unpublished);
    }

    [Fact]
    public async Task GetAvailableTermsAsync_ReturnsOnlyPublishedTerms()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new ReportCardPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Exams.AddRange(
                new ExamResultEntity { ExamID = 1, StudentId = 101, Subject = "English", Term = "First Term", Year = "2025/2026", ExamTypeId = 1, SchoolId = schoolId },
                new ExamResultEntity { ExamID = 2, StudentId = 101, Subject = "Math", Term = "Second Term", Year = "2025/2026", ExamTypeId = 1, SchoolId = schoolId });
            db.ExamSetups.AddRange(
                new ExamSetupEntity { SetupID = 1, Term = "First Term", Year = "2025/2026", ExamTypeId = 1, IsPublishedToPortal = true, SchoolId = schoolId },
                new ExamSetupEntity { SetupID = 2, Term = "Second Term", Year = "2025/2026", ExamTypeId = 1, IsPublishedToPortal = false, SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var terms = await repo.GetAvailableTermsAsync("101", schoolId);

        var term = Assert.Single(terms);
        Assert.Equal("First Term", term.Term);
    }

    [Fact]
    public async Task WardRepository_ReturnsOnlyCurrentParentsWardsForSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new WardRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Students.AddRange(
                new StudentEntity { StudentID = 101, FirstName = "Ama", LastName = "Mensah", ClassID = "BASIC 1", ParentUsername = "parent1", SchoolId = schoolId },
                new StudentEntity { StudentID = 102, FirstName = "Kofi", LastName = "Boateng", ClassID = "BASIC 2", ParentUsername = "parent1", SchoolId = otherSchoolId },
                new StudentEntity { StudentID = 103, FirstName = "Esi", LastName = "Owusu", ClassID = "BASIC 1", ParentUsername = "parent2", SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var wards = await repo.GetWardsForParentAsync("parent1", schoolId);

        Assert.Single(wards);
        Assert.Equal(101, wards[0].StudentID);
    }

    [Fact]
    public async Task WardBillingRepository_IncludesLegacyAccountBalanceAboveTermLedger()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new WardBillingRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.Add(new StudentFeeLedgerEntity
            {
                StudentID = "101",
                TermID = 1,
                TotalExpectedAmount = 1000m,
                TotalPaidAmount = 400m,
                SchoolId = schoolId
            });
            db.PaymentRecords.Add(new PaymentRecordEntity
            {
                StudentID = 101,
                StudentName = "Ama Mensah",
                ClassID = "BASIC 1",
                AmountPaid = 0m,
                Balance = 850m,
                PaymentDate = new DateTime(2026, 7, 11),
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        var ledger = await repo.GetFeeLedgerAsync("101", schoolId);

        Assert.Equal(2, ledger.Count);
        Assert.Equal(250m, ledger[0].TotalExpectedAmount);
        Assert.Equal(850m, ledger.Sum(l => l.TotalExpectedAmount - l.TotalPaidAmount));
    }

    [Fact]
    public async Task WardBillingRepository_DoesNotDuplicateWhenLedgerAlreadyCoversAccountBalance()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new WardBillingRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.Add(new StudentFeeLedgerEntity
            {
                StudentID = "101",
                TermID = 1,
                TotalExpectedAmount = 1000m,
                TotalPaidAmount = 100m,
                SchoolId = schoolId
            });
            db.PaymentRecords.Add(new PaymentRecordEntity
            {
                StudentID = 101,
                StudentName = "Ama Mensah",
                ClassID = "BASIC 1",
                AmountPaid = 0m,
                Balance = 850m,
                PaymentDate = new DateTime(2026, 7, 11),
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        var ledger = await repo.GetFeeLedgerAsync("101", schoolId);

        var row = Assert.Single(ledger);
        Assert.Equal(900m, row.TotalExpectedAmount - row.TotalPaidAmount);
    }

    [Fact]
    public async Task WardBillingRepository_ReturnsTermPreviousAndAdditionalFeeBreakdown()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new WardBillingRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.StudentFeeLedgers.Add(new StudentFeeLedgerEntity
            {
                StudentID = "101",
                TermID = 1,
                PreviousBalance = 200m,
                CurrentTermCharge = 1000m,
                TotalExpectedAmount = 1200m,
                TotalPaidAmount = 250m,
                SchoolId = schoolId
            });
            db.AdditionalFees.Add(new AdditionalFeeEntity
            {
                AdditionalFeeId = 1,
                FeeName = "Lab user fee",
                TermName = "First Term",
                AcademicYear = "2026/2027",
                Status = "Active",
                SchoolId = schoolId
            });
            db.AdditionalFeeStudentCharges.Add(new AdditionalFeeStudentChargeEntity
            {
                ChargeId = 1,
                AdditionalFeeId = 1,
                StudentID = "101",
                Amount = 50m,
                Status = "Active",
                SchoolId = schoolId
            });
            db.PaymentRecords.Add(new PaymentRecordEntity
            {
                StudentID = 101,
                StudentName = "Ama Mensah",
                Balance = 1000m,
                PaymentDate = new DateTime(2026, 7, 11),
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        var breakdown = await repo.GetFeeBreakdownAsync("101", schoolId);

        Assert.Equal(3, breakdown.Count);
        Assert.Contains(breakdown, x => x.Category == "Previous Debit" && x.Amount == 200m && x.Paid == 200m && x.Outstanding == 0m);
        Assert.Contains(breakdown, x => x.Category == "Term Fee" && x.Amount == 1000m && x.Paid == 50m && x.Outstanding == 950m);
        Assert.Contains(breakdown, x => x.Category == "Additional Fee" && x.Description.Contains("Lab user fee") && x.Outstanding == 50m);
        Assert.Equal(1000m, breakdown.Sum(x => x.Outstanding));
    }

    [Fact]
    public async Task WardBillingRepository_AddsPreviousDebitWhenAccountBalanceExceedsKnownCharges()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new WardBillingRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.AdditionalFees.Add(new AdditionalFeeEntity
            {
                AdditionalFeeId = 1,
                FeeName = "Sports fee",
                TermName = "First Term",
                AcademicYear = "2026/2027",
                Status = "Active",
                SchoolId = schoolId
            });
            db.AdditionalFeeStudentCharges.Add(new AdditionalFeeStudentChargeEntity
            {
                ChargeId = 1,
                AdditionalFeeId = 1,
                StudentID = "101",
                Amount = 50m,
                Status = "Active",
                SchoolId = schoolId
            });
            db.PaymentRecords.Add(new PaymentRecordEntity
            {
                StudentID = 101,
                StudentName = "Ama Mensah",
                Balance = 180m,
                PaymentDate = new DateTime(2026, 7, 11),
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        var breakdown = await repo.GetFeeBreakdownAsync("101", schoolId);

        Assert.Contains(breakdown, x => x.Category == "Additional Fee" && x.Outstanding == 50m);
        Assert.Contains(breakdown, x => x.Category == "Previous Debit" && x.Description == "Previous account balance" && x.Outstanding == 130m);
        Assert.Equal(180m, breakdown.Sum(x => x.Outstanding));
    }

    [Fact]
    public async Task SubmitRequestAsync_ValidatesWardOwnership()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Students.Add(new StudentEntity
            {
                StudentID = 101,
                FirstName = "Ama",
                LastName = "Mensah",
                ClassID = "BASIC 1",
                ParentUsername = "parent1",
                SchoolId = schoolId
            });
            await db.SaveChangesAsync();
        }

        await repo.SubmitRequestAsync(new ParentRequestEntity
        {
            ParentUsername = "parent1",
            StudentID = 101,
            RequestType = "TransferLetter",
            Details = "Moving to another town",
            SchoolId = schoolId
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SubmitRequestAsync(new ParentRequestEntity
        {
            ParentUsername = "parent2",
            StudentID = 101,
            RequestType = "TransferLetter",
            Details = "Should not be allowed",
            SchoolId = schoolId
        }));

        using var verify = new AppDbContext(options);
        var request = Assert.Single(verify.ParentRequests);
        Assert.Equal("Pending", request.Status);
        Assert.Equal("TransferLetter", request.RequestType);
    }

    [Fact]
    public async Task SubmitRequestAsync_RejectsMissingRequiredFields()
    {
        var options = NewInMemoryOptions();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.SubmitRequestAsync(new ParentRequestEntity
        {
            ParentUsername = "parent1",
            StudentID = 0,
            RequestType = "TransferLetter",
            Details = "Details"
        }));
    }

    [Fact]
    public async Task GetRequestsAsync_FiltersByParentAndSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ParentRequests.AddRange(
                new ParentRequestEntity { ParentUsername = "parent1", StudentID = 101, RequestType = "TransferLetter", Details = "A", CreatedDate = new DateTime(2026, 6, 1), SchoolId = schoolId },
                new ParentRequestEntity { ParentUsername = "parent1", StudentID = 102, RequestType = "InformationChange", Details = "B", CreatedDate = new DateTime(2026, 6, 2), SchoolId = otherSchoolId },
                new ParentRequestEntity { ParentUsername = "parent2", StudentID = 103, RequestType = "TransferLetter", Details = "C", CreatedDate = new DateTime(2026, 6, 3), SchoolId = schoolId });
            await db.SaveChangesAsync();
        }

        var requests = await repo.GetRequestsAsync("parent1", schoolId);

        Assert.Single(requests);
        Assert.Equal(101, requests[0].StudentID);
    }

    [Fact]
    public async Task GetAllRequestsAsync_FiltersByStatusSearchAndSchool()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ParentRequests.AddRange(
                new ParentRequestEntity { ParentUsername = "parent1", StudentID = 101, RequestType = "TransferLetter", Details = "Moving to Accra", Status = "Pending", CreatedDate = new DateTime(2026, 6, 1), SchoolId = schoolId },
                new ParentRequestEntity { ParentUsername = "parent2", StudentID = 102, RequestType = "InformationChange", Details = "Phone number update", Status = "Approved", CreatedDate = new DateTime(2026, 6, 2), SchoolId = schoolId },
                new ParentRequestEntity { ParentUsername = "parent3", StudentID = 103, RequestType = "TransferLetter", Details = "Moving to Kumasi", Status = "Pending", CreatedDate = new DateTime(2026, 6, 3), SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        var requests = await repo.GetAllRequestsAsync("pending", "accra", schoolId);

        var request = Assert.Single(requests);
        Assert.Equal(101, request.StudentID);
        Assert.Equal("Pending", request.Status);
    }

    [Fact]
    public async Task UpdateRequestStatusAsync_UpdatesOnlyCurrentSchoolAndNormalizesStatus()
    {
        var options = NewInMemoryOptions();
        var schoolId = Guid.NewGuid();
        var otherSchoolId = Guid.NewGuid();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.ParentRequests.AddRange(
                new ParentRequestEntity { RequestID = 1, ParentUsername = "parent1", StudentID = 101, RequestType = "TransferLetter", Details = "A", Status = "Pending", SchoolId = schoolId },
                new ParentRequestEntity { RequestID = 2, ParentUsername = "parent2", StudentID = 102, RequestType = "TransferLetter", Details = "B", Status = "Pending", SchoolId = otherSchoolId });
            await db.SaveChangesAsync();
        }

        await repo.UpdateRequestStatusAsync(1, "approved", schoolId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateRequestStatusAsync(2, "Approved", schoolId));

        using var verify = new AppDbContext(options);
        Assert.Equal("Approved", verify.ParentRequests.Single(r => r.RequestID == 1).Status);
        Assert.Equal("Pending", verify.ParentRequests.Single(r => r.RequestID == 2).Status);
    }

    [Fact]
    public async Task UpdateRequestStatusAsync_RejectsInvalidStatus()
    {
        var options = NewInMemoryOptions();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateRequestStatusAsync(1, "Done"));
    }

    [Fact]
    public async Task GetClassAssignmentsAsync_ReturnsClassAssignmentsNewestFirst()
    {
        var options = NewInMemoryOptions();
        var repo = new ParentPortalRepository(new TestDbContextFactory(options));

        using (var db = new AppDbContext(options))
        {
            db.Assignments.AddRange(
                new AssignmentEntity { AssignmentID = 1, ClassID = "BASIC 1", Subject = "English", Title = "Older", CreatedDate = new DateTime(2026, 6, 1), DueDate = new DateTime(2026, 6, 10) },
                new AssignmentEntity { AssignmentID = 2, ClassID = "BASIC 1", Subject = "Math", Title = "Newer", CreatedDate = new DateTime(2026, 6, 2), DueDate = new DateTime(2026, 6, 11) },
                new AssignmentEntity { AssignmentID = 3, ClassID = "BASIC 2", Subject = "Science", Title = "Other", CreatedDate = new DateTime(2026, 6, 3), DueDate = new DateTime(2026, 6, 12) });
            await db.SaveChangesAsync();
        }

        var assignments = await repo.GetClassAssignmentsAsync("BASIC 1");

        Assert.Equal(2, assignments.Count);
        Assert.Equal("Newer", assignments[0].Title);
        Assert.DoesNotContain(assignments, a => a.ClassID == "BASIC 2");
    }
}
