using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.HouseGroups;

public interface IAdminHouseGroupService
{
    Task<IReadOnlyList<LookupItem>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<LookupItem>> CreateAsync(HouseGroupDto dto, CancellationToken ct);
}
