using Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Application.Public.Cities;

public interface ICityQueryService
{
    Task<IEnumerable<CityListItemDto>> GetAsync(CancellationToken ct);
}
