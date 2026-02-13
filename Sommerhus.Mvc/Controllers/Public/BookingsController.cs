using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Public;

[Authorize]
public sealed class BookingsController(UserApiClient userApi, SommerhusApi publicApi) : Controller
{
    [HttpGet("/bookings")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var res = await userApi.ListBookingsAsync(ct);
        var bookings = res.Data ?? [];
        return View("~/Views/Bookings/Index.cshtml", bookings);
    }

    [HttpGet("/bookings/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var res = await userApi.GetBookingAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Booking not found.";
            return RedirectToAction(nameof(Index));
        }
        return View("~/Views/Bookings/Details.cshtml", res.Data);
    }

    [HttpGet("/houses/{houseId:guid}/book")]
    public async Task<IActionResult> Create(Guid houseId, [FromQuery] DateOnly? checkIn, [FromQuery] DateOnly? checkOut, [FromQuery] int guests = 2, CancellationToken ct = default)
    {
        var houseRes = await publicApi.GetHouseAsync(houseId, ct);
        if (!houseRes.Ok || houseRes.Data is null)
        {
            TempData["Error"] = "House not found.";
            return RedirectToAction("Houses", "Houses");
        }

        ViewBag.House = houseRes.Data;
        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        ViewBag.Guests = guests;

        return View("~/Views/Bookings/Create.cshtml");
    }

    [HttpPost("/houses/{houseId:guid}/book")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(Guid houseId, [FromForm] DateOnly checkIn, [FromForm] DateOnly checkOut, [FromForm] int guests, [FromForm] string? guestNote, CancellationToken ct)
    {
        var dto = new CreateBookingDto
        {
            HouseId = houseId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Guests = guests,
            GuestNote = guestNote
        };

        var res = await userApi.CreateBookingAsync(dto, ct);
        if (res.Ok && res.Data is not null)
        {
            TempData["Success"] = "Booking created successfully!";
            return RedirectToAction(nameof(Details), new { id = res.Data.Id });
        }

        if (res.HasValidationErrors)
        {
            foreach (var (field, messages) in res.Errors)
                foreach (var msg in messages)
                    ModelState.AddModelError(field, msg);
        }
        else
        {
            ModelState.AddModelError(string.Empty, res.Message ?? "Could not create booking.");
        }

        var houseRes = await publicApi.GetHouseAsync(houseId, ct);
        ViewBag.House = houseRes.Data;
        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        ViewBag.Guests = guests;

        return View("~/Views/Bookings/Create.cshtml");
    }

    [HttpPost("/bookings/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var res = await userApi.CancelBookingAsync(id, ct);
        TempData[res.Ok ? "Success" : "Error"] = res.Ok ? "Booking cancelled." : (res.Message ?? "Could not cancel booking.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
