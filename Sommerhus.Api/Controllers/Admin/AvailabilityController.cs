using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Security;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Availability;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AdminRoles.Admin)]
[Route("api/admin/houses/{houseId:guid}/availability")]
public sealed class AvailabilityController(IAdminAvailabilityService availabilityService) : ControllerBase
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

        var result = await availabilityService.GetBlocksAsync(houseId, fromDate, toDate, ct);
        return this.FromResult(result);
    }

    [HttpGet("{blockId:guid}")]
    public async Task<ActionResult<AvailabilityBlockDto>> GetBlock(Guid houseId, Guid blockId, CancellationToken ct)
        => this.FromResult(await availabilityService.GetBlockAsync(blockId, ct));

    [HttpPost]
    public async Task<ActionResult<AvailabilityBlockDto>> CreateBlock(
        Guid houseId,
        [FromBody] UpsertAvailabilityBlockDto dto,
        CancellationToken ct)
    {
        var result = await availabilityService.CreateBlockAsync(houseId, dto, ct);
        return result.Status switch
        {
            ServiceResultStatus.Success => CreatedAtAction(nameof(GetBlock), new { houseId, blockId = result.Value!.Id }, result.Value),
            _ => this.FromResult(result)
        };
    }

    [HttpPut("{blockId:guid}")]
    public async Task<ActionResult<AvailabilityBlockDto>> UpdateBlock(
        Guid houseId,
        Guid blockId,
        [FromBody] UpsertAvailabilityBlockDto dto,
        CancellationToken ct)
        => this.FromResult(await availabilityService.UpdateBlockAsync(blockId, dto, ct));

    [HttpDelete("{blockId:guid}")]
    public async Task<ActionResult> DeleteBlock(Guid houseId, Guid blockId, CancellationToken ct)
        => this.FromResult(await availabilityService.DeleteBlockAsync(blockId, ct));

    [HttpGet("check")]
    public async Task<ActionResult<object>> CheckAvailability(
        Guid houseId,
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        CancellationToken ct)
    {
        var available = await availabilityService.IsAvailableAsync(houseId, checkIn, checkOut, ct);
        return Ok(new { houseId, checkIn, checkOut, available });
    }
}
