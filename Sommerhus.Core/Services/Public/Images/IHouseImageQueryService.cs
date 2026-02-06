using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Images;

public interface IHouseImageQueryService
{
    Task<IEnumerable<ImageDto>> GetAsync(Guid houseId, string baseUrl, CancellationToken ct);
}
