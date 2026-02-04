using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Services.Public.Cities;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class CitiesController(ICityQueryService cities) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<CityListItemDto>> Get(CancellationToken ct)
        => cities.GetAsync(ct);
}
