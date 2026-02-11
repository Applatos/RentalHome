using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Owner;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Owner;

public interface IOwnerHouseService
{
    Task<IReadOnlyList<OwnerHouseListItemDto>> ListAsync(string ownerId, CancellationToken ct);
    Task<ServiceResult<AdminHouseDetailsDto>> GetDetailsAsync(string ownerId, Guid houseId, string baseUrl, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(string ownerId, Guid houseId, OwnerUpdateHouseDto dto, CancellationToken ct);
}
