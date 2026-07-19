using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public sealed record AuditLogEntry(
    string ActorUsername,
    string Action,
    string EntityType,
    string? EntityId = null,
    string? Summary = null,
    Guid? SchoolId = null);

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry);
    Task<List<AuditLogEntity>> GetLogsAsync(string? searchTerm = null, string? actionFilter = null, DateTime? from = null, DateTime? to = null, Guid? schoolId = null, int take = 200);
}

public class AuditLogService(IDbContextFactory<AppDbContext> factory) : IAuditLogService
{
    public async Task WriteAsync(AuditLogEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Action)) throw new ArgumentException("Audit action is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.EntityType)) throw new ArgumentException("Audit entity type is required.", nameof(entry));

        using var db = await factory.CreateDbContextAsync();
        db.AuditLogs.Add(new AuditLogEntity
        {
            CreatedAt = DateTime.UtcNow,
            ActorUsername = string.IsNullOrWhiteSpace(entry.ActorUsername) ? "system" : entry.ActorUsername.Trim(),
            Action = entry.Action.Trim(),
            EntityType = entry.EntityType.Trim(),
            EntityId = entry.EntityId?.Trim(),
            Summary = entry.Summary?.Trim(),
            SchoolId = entry.SchoolId
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<AuditLogEntity>> GetLogsAsync(string? searchTerm = null, string? actionFilter = null, DateTime? from = null, DateTime? to = null, Guid? schoolId = null, int take = 200)
    {
        using var db = await factory.CreateDbContextAsync();
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (schoolId.HasValue) query = query.Where(l => l.SchoolId == schoolId.Value);
        if (!string.IsNullOrWhiteSpace(actionFilter)) query = query.Where(l => l.Action == actionFilter);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(l => l.CreatedAt < to.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(l =>
                l.ActorUsername.ToLower().Contains(term) ||
                l.Action.ToLower().Contains(term) ||
                l.EntityType.ToLower().Contains(term) ||
                (l.EntityId != null && l.EntityId.ToLower().Contains(term)) ||
                (l.Summary != null && l.Summary.ToLower().Contains(term)));
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.AuditLogID)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync();
    }
}
