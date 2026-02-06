using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Houses;

public interface IHouseQueryService
{
    Task<PageResult<PublicHouseListItemDto>> SearchAsync(string? city, string? zip, string? query, Guid? areaId, int page, int pageSize, HttpRequest request, CancellationToken ct);
    Task<PublicHouseDetailsDto?> GetAsync(Guid id, HttpRequest request, CancellationToken ct);
}
