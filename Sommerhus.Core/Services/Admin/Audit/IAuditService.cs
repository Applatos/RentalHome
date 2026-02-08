using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Audit;

public interface IAuditService
{
    Task<PageResult<AuditEntryDto>> QueryAsync(
        string? entityType,
        string? entityId,
        string? changedBy,
        int page,
        int pageSize,
        CancellationToken ct);
}
