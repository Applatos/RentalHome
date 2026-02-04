using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Cities;

public interface ICityQueryService
{
    Task<IEnumerable<CityListItemDto>> GetAsync(CancellationToken ct);
}
