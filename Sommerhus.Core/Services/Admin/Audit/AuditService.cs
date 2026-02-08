using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Audit;

public sealed class AuditService(AppDbContext db) : IAuditService
{
    public async Task<PageResult<AuditEntryDto>> QueryAsync(
        string? entityType,
        string? entityId,
        string? changedBy,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = db.AuditEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(e => e.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(changedBy))
        {
            query = query.Where(e => EF.Functions.Like(e.ChangedBy, $"%{changedBy}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.ChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEntryDto(
                e.Id,
                e.EntityType,
                e.EntityId,
                e.Action.ToString(),
                e.ChangedBy,
                e.ChangedAtUtc,
                e.Changes))
            .ToListAsync(ct);

        return new PageResult<AuditEntryDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }
}
