using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Services.Public.Images;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/houses/{houseId:guid}/images")]
public class HouseImagesController(IHouseImageQueryService images) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<ImageDto>> List(Guid houseId, CancellationToken ct)
        => images.GetAsync(houseId, Request, ct);
}
