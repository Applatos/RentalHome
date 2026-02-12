using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Owner;

namespace Sommerhus.Api.Controllers.Owner;

[ApiController]
[Route("api/owner/bookings")]
[Authorize(Roles = $"{AppRoles.HouseOwner},{AppRoles.Admin}")]
public sealed class OwnerBookingsController(IOwnerBookingService ownerBookingService) : ControllerBase
{
    private readonly IOwnerBookingService ownerBookingService = ownerBookingService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> List(CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var items = await ownerBookingService.ListByOwnerAsync(ownerId, ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var result = await ownerBookingService.GetAsync(ownerId, id, ct);
        return this.FromResult(result);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] OwnerBookingActionDto? dto, CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var result = await ownerBookingService.ConfirmAsync(ownerId, id, dto?.Note, ct);
        return this.FromResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] OwnerBookingActionDto? dto, CancellationToken ct)
    {
        var ownerId = GetUserId();
        if (ownerId is null) return Unauthorized();

        var result = await ownerBookingService.RejectAsync(ownerId, id, dto?.Note, ct);
        return this.FromResult(result);
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}

public sealed class OwnerBookingActionDto
{
    public string? Note { get; set; }
}
