using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;
using System.Net;

namespace Sommerhus.Mvc.Controllers.Public;

[Authorize]
public sealed class BookingsController(
    UserApiClient userApi,
    SommerhusApi publicApi,
    IStringLocalizer<SharedResource> localizer) : Controller
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
            TempData["Error"] = localizer["Pages.Booking.NotFound"].Value;
            return RedirectToAction(nameof(Index));
        }
        return View("~/Views/Bookings/Details.cshtml", res.Data);
    }

    [HttpGet("/houses/{houseId:guid}/book")]
    public async Task<IActionResult> Create(Guid houseId, [FromQuery] DateOnly? checkIn, [FromQuery] DateOnly? checkOut, [FromQuery] int? guests, CancellationToken ct = default)
    {
        var houseRes = await publicApi.GetHouseAsync(houseId, ct);
        if (!houseRes.Ok || houseRes.Data is null)
        {
            return HouseUnavailable(houseRes.StatusCode);
        }

        // The house page links here with its selection; without one, start from the same stay it shows.
        var house = houseRes.Data;
        var arrival = checkIn ?? BookingDefaults.Arrival(BookingDefaults.Today);
        var departure = checkOut ?? BookingDefaults.Departure(arrival);
        return BookingForm(house, arrival, departure, BookingDefaults.ClampGuests(guests, house.MaxGuests), guestNote: null, error: null);
    }

    [HttpPost("/houses/{houseId:guid}/book")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(Guid houseId, [FromForm] DateOnly? checkIn, [FromForm] DateOnly? checkOut, [FromForm] int? guests, [FromForm] string? guestNote, CancellationToken ct)
    {
        // A field that is missing or unreadable is reported the way the API would report it.
        HttpStatusCode? failedStatus = HttpStatusCode.BadRequest;
        IReadOnlyDictionary<string, string[]> failedErrors;
        if (checkIn is null || checkOut is null || guests is null)
        {
            var field = guests is null && checkIn is not null && checkOut is not null ? PricingErrors.Guests : PricingErrors.Dates;
            failedErrors = new Dictionary<string, string[]> { [field] = [] };
        }
        else
        {
            var dto = new CreateBookingDto
            {
                HouseId = houseId,
                CheckIn = checkIn.Value,
                CheckOut = checkOut.Value,
                Guests = guests.Value,
                GuestNote = guestNote
            };

            var res = await userApi.CreateBookingAsync(dto, ct);
            if (res.Ok && res.Data is not null)
            {
                TempData["Success"] = localizer["Pages.Booking.Created"].Value;
                return RedirectToAction(nameof(Details), new { id = res.Data.Id });
            }

            failedStatus = res.StatusCode;
            failedErrors = res.Errors;
        }

        // Show the form again with what was entered; the house is only needed for that.
        var houseRes = await publicApi.GetHouseAsync(houseId, ct);
        if (!houseRes.Ok || houseRes.Data is null)
        {
            return HouseUnavailable(houseRes.StatusCode);
        }

        var house = houseRes.Data;
        var error = PricingErrorText.Describe(failedStatus, failedErrors, localizer, PricingErrorContext.Booking, house.MaxGuests);
        return BookingForm(house, checkIn, checkOut, guests, guestNote, error.Text);
    }

    [HttpPost("/bookings/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var res = await userApi.CancelBookingAsync(id, ct);
        TempData[res.Ok ? "Success" : "Error"] = res.Ok
            ? localizer["Pages.Booking.Cancelled"].Value
            : localizer["Pages.Booking.CancelFailed"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    private ViewResult BookingForm(PublicHouseDetailsDto house, DateOnly? checkIn, DateOnly? checkOut, int? guests, string? guestNote, string? error)
    {
        ViewBag.House = house;
        ViewBag.CheckIn = checkIn;
        ViewBag.CheckOut = checkOut;
        ViewBag.Guests = guests;
        ViewBag.GuestNote = guestNote;
        ViewBag.Error = error;
        return View("~/Views/Bookings/Create.cshtml");
    }

    // The API answers 404 for an unknown and for an unpublished house alike.
    private IActionResult HouseUnavailable(HttpStatusCode? status)
        => status is HttpStatusCode.NotFound || (status is { } code && (int)code < 300)
            ? NotFound()
            : StatusCode(StatusCodes.Status502BadGateway);
}
