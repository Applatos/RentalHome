using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Contracts.Dtos.Public.Cities;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Cities;

public interface IAdminCityService
{
    Task<IReadOnlyList<CityListItemDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct);
}
