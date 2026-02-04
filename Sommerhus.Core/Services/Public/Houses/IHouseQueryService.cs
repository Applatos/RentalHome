using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Dtos.Shared; using Sommerhus.Core.Dtos.Admin; using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Public.Houses;

public interface IHouseQueryService
{
    Task<IEnumerable<HouseListItemDto>> SearchAsync(string? city, string? zip, string? query, Guid? areaId, int page, int pageSize, HttpRequest request, CancellationToken ct);
    Task<HouseDetailsDto?> GetAsync(Guid id, HttpRequest request, CancellationToken ct);
}
