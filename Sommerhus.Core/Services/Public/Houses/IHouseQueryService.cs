using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Houses;

public interface IHouseQueryService
{
    Task<PageResult<PublicHouseListItemDto>> SearchAsync(HouseSearchFilter filter, string baseUrl, CancellationToken ct);
    Task<PublicHouseDetailsDto?> GetAsync(Guid id, string baseUrl, CancellationToken ct);
}
