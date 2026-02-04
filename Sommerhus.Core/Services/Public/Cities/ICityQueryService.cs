using Sommerhus.Core.Dtos.Public.Cities;

namespace Sommerhus.Core.Services.Public.Cities;

public interface ICityQueryService
{
    Task<IEnumerable<CityListItemDto>> GetAsync(CancellationToken ct);
}
