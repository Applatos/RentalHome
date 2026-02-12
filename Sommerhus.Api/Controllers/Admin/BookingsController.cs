using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Identity;
using Sommerhus.Core.Services.Admin.Bookings;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class BookingsController(IAdminBookingService adminBookingService) : ControllerBase
{
    private readonly IAdminBookingService adminBookingService = adminBookingService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> List(
        [FromQuery] Guid? houseId,
        [FromQuery] string? userId,
        [FromQuery] BookingStatus? status,
        CancellationToken ct)
    {
        var filter = new BookingFilterDto
        {
            HouseId = houseId,
            UserId = userId,
            Status = status
        };

        var items = await adminBookingService.ListAsync(filter, ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await adminBookingService.GetAsync(id, ct);
        return this.FromResult(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateBookingStatusDto dto, CancellationToken ct)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var result = await adminBookingService.UpdateStatusAsync(id, dto, adminUserId, ct);
        return this.FromResult(result);
    }
}
