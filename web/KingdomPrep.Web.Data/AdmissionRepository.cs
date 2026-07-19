using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KingdomPrep.Web.Data;

public interface IAdmissionRepository
{
    Task<List<DraftAdmissionEntity>> GetPendingAdmissionsAsync(Guid? schoolId = null, string? searchTerm = null, string? classFilter = null);
    Task<DraftAdmissionEntity?> GetByIdAsync(int id, Guid? schoolId = null);
    Task ApproveAdmissionAsync(int draftId, Guid? schoolId = null, string? actorUsername = null);
    Task DeleteDraftAsync(int draftId, Guid? schoolId = null, string? actorUsername = null);
}

public class AdmissionRepository(IDbContextFactory<AppDbContext> factory, IAuditLogService? audit = null) : IAdmissionRepository
{
    public async Task<List<DraftAdmissionEntity>> GetPendingAdmissionsAsync(Guid? schoolId = null, string? searchTerm = null, string? classFilter = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.DraftAdmissions.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(d => d.SchoolId == schoolId.Value);
        if (!string.IsNullOrWhiteSpace(classFilter)) query = query.Where(d => d.ClassID == classFilter);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(d =>
                (d.FirstName != null && d.FirstName.ToLower().Contains(term)) ||
                (d.LastName != null && d.LastName.ToLower().Contains(term)) ||
                (d.GuidanceName != null && d.GuidanceName.ToLower().Contains(term)) ||
                (d.EmergencyContact != null && d.EmergencyContact.ToLower().Contains(term)) ||
                (d.Email != null && d.Email.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(d => d.SubmittedDate ?? d.AdmissionDate).ToListAsync();
    }

    public async Task<DraftAdmissionEntity?> GetByIdAsync(int id, Guid? schoolId = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.DraftAdmissions.AsNoTracking().Where(d => d.DraftID == id);
        if (schoolId.HasValue) query = query.Where(d => d.SchoolId == schoolId.Value);
        return await query.FirstOrDefaultAsync();
    }

    public async Task ApproveAdmissionAsync(int draftId, Guid? schoolId = null, string? actorUsername = null)
    {
        string studentName;
        Guid? logSchoolId;
        using var db = await factory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var draftQuery = db.DraftAdmissions.Where(d => d.DraftID == draftId);
            if (schoolId.HasValue) draftQuery = draftQuery.Where(d => d.SchoolId == schoolId.Value);
            var draft = await draftQuery.FirstOrDefaultAsync();
            if (draft == null) throw new InvalidOperationException("Admission draft was not found for the current school.");
            ValidateDraftForApproval(draft);
            studentName = $"{draft.FirstName} {draft.LastName}".Trim();
            logSchoolId = schoolId ?? draft.SchoolId;

            var student = new StudentEntity
            {
                FirstName = draft.FirstName!.Trim(),
                LastName = draft.LastName!.Trim(),
                DateOfBirth = draft.DOB,
                Gender = draft.Gender,
                ClassID = draft.ClassID!.Trim(),
                Email = draft.Email,
                HomeTown = draft.HomeTown,
                Residence = draft.Residence,
                Allergies = draft.Allergies,
                EmergencyContact = draft.EmergencyContact,
                GuardianName = draft.GuidanceName,
                GuardianEmail = draft.GuidanceEmail,
                GuardianLocation = draft.GuidanceLocation,
                AdmissionDate = draft.AdmissionDate,
                SchoolId = schoolId ?? draft.SchoolId
            };

            db.Students.Add(student);
            db.DraftAdmissions.Remove(draft);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                "AdmissionApproved",
                "DraftAdmission",
                draftId.ToString(),
                $"Approved admission for {studentName}.",
                logSchoolId));
        }
    }

    public async Task DeleteDraftAsync(int draftId, Guid? schoolId = null, string? actorUsername = null)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.DraftAdmissions.Where(d => d.DraftID == draftId);
        if (schoolId.HasValue) query = query.Where(d => d.SchoolId == schoolId.Value);
        var draft = await query.FirstOrDefaultAsync();
        if (draft == null) throw new InvalidOperationException("Admission draft was not found for the current school.");
        var studentName = $"{draft.FirstName} {draft.LastName}".Trim();
        var logSchoolId = schoolId ?? draft.SchoolId;

        db.DraftAdmissions.Remove(draft);
        await db.SaveChangesAsync();

        if (audit != null)
        {
            await audit.WriteAsync(new AuditLogEntry(
                actorUsername ?? "system",
                "AdmissionRejected",
                "DraftAdmission",
                draftId.ToString(),
                $"Rejected admission draft for {studentName}.",
                logSchoolId));
        }
    }

    private static void ValidateDraftForApproval(DraftAdmissionEntity draft)
    {
        if (string.IsNullOrWhiteSpace(draft.FirstName)) throw new InvalidOperationException("Admission draft is missing the student's first name.");
        if (string.IsNullOrWhiteSpace(draft.LastName)) throw new InvalidOperationException("Admission draft is missing the student's last name.");
        if (string.IsNullOrWhiteSpace(draft.ClassID)) throw new InvalidOperationException("Admission draft is missing the class.");
    }
}
