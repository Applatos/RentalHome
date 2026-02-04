using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared; using Sommerhus.Core.Dtos.Admin; using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Admin.HouseGroups;

public interface IAdminHouseGroupService
{
    Task<IReadOnlyList<LookupItem>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<HouseGroupListItemDto>>> ListAsync(CancellationToken ct);
    Task<ServiceResult<HouseGroupDetailsDto>> GetAsync(Guid id, CancellationToken ct);
    Task<ServiceResult<LookupItem>> CreateAsync(HouseGroupDto dto, CancellationToken ct);
    Task<ServiceResult<HouseGroupDetailsDto>> UpdateAsync(Guid id, UpsertHouseGroupDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);

    Task<ServiceResult<SeasonSpanDto>> AddSeasonSpanAsync(Guid groupId, UpsertSeasonSpanDto dto, CancellationToken ct);
    Task<ServiceResult<SeasonSpanDto>> UpdateSeasonSpanAsync(Guid groupId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteSeasonSpanAsync(Guid groupId, Guid spanId, CancellationToken ct);

    // House season span management (when house has a group)
    Task<ServiceResult<SeasonSpanDto>> AddHouseSeasonSpanAsync(Guid houseId, UpsertSeasonSpanDto dto, CancellationToken ct);
    Task<ServiceResult<SeasonSpanDto>> UpdateHouseSeasonSpanAsync(Guid houseId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteHouseSeasonSpanAsync(Guid houseId, Guid spanId, CancellationToken ct);
}
