using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Areas;

public interface IAdminAreaService
{
    Task<IReadOnlyList<AreaListItemDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct);
    Task<ServiceResult<AreaDetailsDto>> GetDetailsAsync(Guid id, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<AreaDetailsDto>> CreateAsync(UpsertAreaDto dto, HttpRequest request, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertAreaDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
}
