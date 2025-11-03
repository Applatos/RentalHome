using Microsoft.AspNetCore.Mvc;
using Sommerhus.Application.Public.Cities;
using Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class CitiesController(ICityQueryService cities) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<CityListItemDto>> Get(CancellationToken ct)
        => cities.GetAsync(ct);
}
