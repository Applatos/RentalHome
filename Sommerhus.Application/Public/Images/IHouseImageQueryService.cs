using Microsoft.AspNetCore.Http;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Public.Images;

public interface IHouseImageQueryService
{
    Task<IEnumerable<ImageDto>> GetAsync(Guid houseId, HttpRequest request, CancellationToken ct);
}
