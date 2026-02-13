using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Identity;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Public.Dashboard;

namespace Sommerhus.Mvc.Controllers.Public;

[Authorize]
public sealed class DashboardController(UserApiClient userApi) : Controller
{
    [HttpGet("/account/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";

        if (role == AppRoles.HouseOwner)
            return RedirectToAction(nameof(OwnerDashboard));

        var bookingsTask = userApi.ListBookingsAsync(ct);
        var favoritesTask = userApi.ListFavoritesAsync(ct);

        await Task.WhenAll(bookingsTask, favoritesTask);

        var allBookings = bookingsTask.Result.Data ?? [];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var vm = new UserDashboardVm
        {
            UserName = User.Identity?.Name ?? "",
            Role = role,
            UpcomingBookings = allBookings
                .Where(b => b.CheckOut >= today && b.Status is BookingStatus.Pending or BookingStatus.Confirmed)
                .OrderBy(b => b.CheckIn)
                .Take(5)
                .ToList(),
            PastBookings = allBookings
                .Where(b => b.CheckOut < today || b.Status is BookingStatus.Completed or BookingStatus.Cancelled)
                .OrderByDescending(b => b.CheckIn)
                .Take(10)
                .ToList(),
            Favorites = favoritesTask.Result.Data ?? []
        };

        return View("~/Views/Dashboard/Index.cshtml", vm);
    }

    [HttpGet("/owner/dashboard")]
    [Authorize(Roles = $"{AppRoles.HouseOwner},{AppRoles.Admin}")]
    public async Task<IActionResult> OwnerDashboard(CancellationToken ct)
    {
        var housesTask = userApi.ListOwnerHousesAsync(ct);
        var bookingsTask = userApi.ListOwnerBookingsAsync(ct);

        await Task.WhenAll(housesTask, bookingsTask);

        var allBookings = bookingsTask.Result.Data ?? [];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var vm = new OwnerDashboardVm
        {
            UserName = User.Identity?.Name ?? "",
            Houses = housesTask.Result.Data ?? [],
            UpcomingBookings = allBookings
                .Where(b => b.CheckOut >= today && b.Status is BookingStatus.Pending or BookingStatus.Confirmed)
                .OrderBy(b => b.CheckIn)
                .Take(10)
                .ToList()
        };

        return View("~/Views/Dashboard/Owner.cshtml", vm);
    }

    [HttpPost("/account/favorites/{houseId:guid}/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFavorite(Guid houseId, string? returnUrl, CancellationToken ct)
    {
        await userApi.AddFavoriteAsync(houseId, ct);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost("/account/favorites/{houseId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFavorite(Guid houseId, string? returnUrl, CancellationToken ct)
    {
        await userApi.RemoveFavoriteAsync(houseId, ct);
        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}
