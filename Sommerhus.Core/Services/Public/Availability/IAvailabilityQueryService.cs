using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Availability;

public interface IAvailabilityQueryService
{
    Task<IReadOnlyList<AvailabilityBlockDto>?> GetBlocksAsync(Guid houseId, DateOnly from, DateOnly to, CancellationToken ct);
}
