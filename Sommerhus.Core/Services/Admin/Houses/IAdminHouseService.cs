using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Houses;

public interface IAdminHouseService
{
    Task<PageResult<AdminHouseListItemDto>> SearchAsync(string? query, EntityStatus? status, int page, int pageSize, CancellationToken ct);
    Task<ServiceResult<AdminHouseDetailsDto>> GetDetailsAsync(Guid id, string baseUrl, CancellationToken ct);
    Task<ServiceResult<Guid>> CreateAsync(UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
}
