using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Areas;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class AreasController(IAreaQueryService areas) : ControllerBase
{
    [HttpGet]
    public Task<IEnumerable<AreaListItemDto>> Search([FromQuery(Name = "q")] string? query, CancellationToken ct)
        => areas.SearchAsync(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AreaDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var details = await areas.GetAsync(id, Request, ct);
        if (details is null)
        {
            return NotFound();
        }

        return details;
    }
}
