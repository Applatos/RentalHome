using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Houses;

public interface IHouseSearchService
{
    Task<PageResult<HouseSearchResultDto>> SearchAsync(HouseSearchFilter filter, string baseUrl, CancellationToken ct);
    Task RebuildIndexAsync(CancellationToken ct);
    Task UpdateDocumentAsync(Guid houseId, CancellationToken ct);
}
