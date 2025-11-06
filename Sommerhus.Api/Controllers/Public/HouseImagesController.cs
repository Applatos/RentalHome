using Microsoft.AspNetCore.Mvc;
using Sommerhus.Application.Public.Images;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/houses/{houseId:guid}/images")]
public class HouseImagesController(IHouseImageQueryService images) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<ImageDto>> List(Guid houseId, CancellationToken ct)
        => images.GetAsync(houseId, Request, ct);
}
