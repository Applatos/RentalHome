using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Availability;

public interface IAdminAvailabilityService
{
    Task<ServiceResult<IReadOnlyList<AvailabilityBlockDto>>> GetBlocksAsync(Guid houseId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<ServiceResult<AvailabilityBlockDto>> GetBlockAsync(Guid blockId, CancellationToken ct);
    Task<ServiceResult<AvailabilityBlockDto>> CreateBlockAsync(Guid houseId, UpsertAvailabilityBlockDto dto, CancellationToken ct);
    Task<ServiceResult<AvailabilityBlockDto>> UpdateBlockAsync(Guid blockId, UpsertAvailabilityBlockDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteBlockAsync(Guid blockId, CancellationToken ct);
    Task<bool> IsAvailableAsync(Guid houseId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct);
}
