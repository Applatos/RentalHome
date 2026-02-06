using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Admin.Houses;

public interface IAdminHouseService
{
    Task<PageResult<AdminHouseListItemDto>> SearchAsync(string? query, int page, int pageSize, CancellationToken ct);
    Task<ServiceResult<AdminHouseDetailsDto>> GetDetailsAsync(Guid id, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<Guid>> CreateAsync(UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
}
