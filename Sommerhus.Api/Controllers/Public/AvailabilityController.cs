using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Availability;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/houses/{houseId:guid}/availability")]
public sealed class AvailabilityController(IAvailabilityQueryService availabilityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AvailabilityBlockDto>>> GetBlocks(
        Guid houseId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = to ?? fromDate.AddMonths(12);

        var blocks = await availabilityService.GetBlocksAsync(houseId, fromDate, toDate, ct);
        if (blocks is null)
            return NotFound();

        return Ok(blocks);
    }
}
