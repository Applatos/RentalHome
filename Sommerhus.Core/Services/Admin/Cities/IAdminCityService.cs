using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Dtos.Public.Cities;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Cities;

public interface IAdminCityService
{
    Task<IReadOnlyList<CityListItemDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct);
}
