using Microsoft.AspNetCore.Http;
using Sommerhus.Contracts.Dtos.Admin.Areas;

namespace Sommerhus.Application.Public.Areas;

public interface IAreaQueryService
{
    Task<IEnumerable<AreaListItemDto>> SearchAsync(string? query, CancellationToken ct);
    Task<AreaDetailsDto?> GetAsync(Guid id, HttpRequest request, CancellationToken ct);
}
