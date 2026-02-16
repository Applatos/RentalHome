using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Houses;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/search")]
public sealed class SearchController(IHouseSearchService houseSearchService) : ControllerBase
{
    [HttpPost("rebuild")]
    public async Task<IActionResult> Rebuild(CancellationToken ct)
    {
        await houseSearchService.RebuildIndexAsync(ct);
        return NoContent();
    }
}
