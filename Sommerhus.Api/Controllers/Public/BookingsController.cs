using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Public.Bookings;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/bookings")]
[Authorize]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    private readonly IBookingService bookingService = bookingService;

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(
        [FromBody] CreateBookingDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await bookingService.CreateAsync(userId, dto, ct);
        return this.FromResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> List(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var items = await bookingService.ListByUserAsync(userId, ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await bookingService.GetAsync(userId, id, ct);
        return this.FromResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await bookingService.CancelAsync(userId, id, ct);
        return this.FromResult(result);
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
