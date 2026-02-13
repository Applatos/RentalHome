using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Identity;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Public;

[Authorize(Roles = $"{AppRoles.HouseOwner},{AppRoles.Admin}")]
public sealed class OwnerBookingsController(UserApiClient userApi) : Controller
{
    [HttpPost("/owner/bookings/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(Guid id, CancellationToken ct)
    {
        var res = await userApi.ConfirmBookingAsync(id, ct);
        TempData[res.Ok ? "Success" : "Error"] = res.Ok ? "Booking confirmed." : (res.Message ?? "Could not confirm booking.");
        return RedirectToAction("OwnerDashboard", "Dashboard");
    }

    [HttpPost("/owner/bookings/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(Guid id, CancellationToken ct)
    {
        var res = await userApi.RejectBookingAsync(id, ct);
        TempData[res.Ok ? "Success" : "Error"] = res.Ok ? "Booking rejected." : (res.Message ?? "Could not reject booking.");
        return RedirectToAction("OwnerDashboard", "Dashboard");
    }
}
