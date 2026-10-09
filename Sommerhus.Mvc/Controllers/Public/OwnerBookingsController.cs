using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Identity;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Public;

[Authorize(Roles = $"{AppRoles.HouseOwner},{AppRoles.Admin}")]
public sealed class OwnerBookingsController(UserApiClient userApi, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpPost("/owner/bookings/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(Guid id, CancellationToken ct)
    {
        var res = await userApi.ConfirmBookingAsync(id, ct);
        TempData[res.Ok ? "Success" : "Error"] = res.Ok ? localizer["Dashboard.Owner.Messages.Confirmed"].Value : (res.Message ?? localizer["Dashboard.Owner.Messages.ConfirmFailed"].Value);
        return RedirectToAction("OwnerDashboard", "Dashboard");
    }

    [HttpPost("/owner/bookings/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(Guid id, CancellationToken ct)
    {
        var res = await userApi.RejectBookingAsync(id, ct);
        TempData[res.Ok ? "Success" : "Error"] = res.Ok ? localizer["Dashboard.Owner.Messages.Rejected"].Value : (res.Message ?? localizer["Dashboard.Owner.Messages.RejectFailed"].Value);
        return RedirectToAction("OwnerDashboard", "Dashboard");
    }
}
