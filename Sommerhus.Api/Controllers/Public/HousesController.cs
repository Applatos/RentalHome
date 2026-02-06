using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class HousesController(IHouseQueryService houses) : ControllerBase
{
    [HttpGet]
    public Task<PageResult<PublicHouseListItemDto>> Search(
        [FromQuery] string? city,
        [FromQuery] string? zip,
        [FromQuery(Name = "q")] string? query,
        [FromQuery] Guid? area,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => houses.SearchAsync(city, zip, query, area, page, pageSize, Request, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicHouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var house = await houses.GetAsync(id, Request, ct);
        if (house is null)
        {
            return NotFound();
        }

        return house;
    }
}
