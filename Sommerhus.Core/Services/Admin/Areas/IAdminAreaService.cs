using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Admin.Areas;

public interface IAdminAreaService
{
    Task<IReadOnlyList<AreaListItemDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct);
    Task<ServiceResult<AreaDetailsDto>> GetDetailsAsync(Guid id, string baseUrl, CancellationToken ct);
    Task<ServiceResult<AreaDetailsDto>> CreateAsync(UpsertAreaDto dto, string baseUrl, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertAreaDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
}
