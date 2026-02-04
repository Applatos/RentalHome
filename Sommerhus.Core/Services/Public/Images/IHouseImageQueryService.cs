using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Dtos.Shared; using Sommerhus.Core.Dtos.Admin; using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Public.Images;

public interface IHouseImageQueryService
{
    Task<IEnumerable<ImageDto>> GetAsync(Guid houseId, HttpRequest request, CancellationToken ct);
}
