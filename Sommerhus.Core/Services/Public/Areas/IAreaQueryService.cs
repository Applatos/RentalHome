using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Dtos.Admin.Areas;

namespace Sommerhus.Core.Services.Public.Areas;

public interface IAreaQueryService
{
    Task<IEnumerable<AreaListItemDto>> SearchAsync(string? query, CancellationToken ct);
    Task<AreaDetailsDto?> GetAsync(Guid id, HttpRequest request, CancellationToken ct);
}
