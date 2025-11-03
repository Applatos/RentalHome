using Microsoft.AspNetCore.Mvc;
using Sommerhus.Application.Public.Houses;
using Sommerhus.Contracts.Dtos.Public.Houses;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class HousesController(IHouseQueryService houses) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<HouseListItemDto>> Search(
        [FromQuery] string? city,
        [FromQuery] string? zip,
        [FromQuery(Name = "q")] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => houses.SearchAsync(city, zip, query, page, pageSize, Request, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var house = await houses.GetAsync(id, Request, ct);
        if (house is null)
        {
            return NotFound();
        }

        return house;
    }
}
