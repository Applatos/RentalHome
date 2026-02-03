using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.HouseGroups;

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
}
