using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Public.Houses;
using System.Net;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class HousesController(SommerhusApi api, IStringLocalizer<SharedResource> localizer) : SommerhusControllerBase
{

    [HttpGet("/")]
    public IActionResult Index()
    {
        return RedirectToAction("Houses");
    }

    // HOUSES (master + pagination)
    [HttpGet("/houses")]
    public async Task<IActionResult> Houses(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] string? city,
        [FromQuery] Guid? area,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? minGuests,
        [FromQuery] bool? hasPool,
        [FromQuery] bool? petFriendly,
        [FromQuery] HouseSearchSort sort = HouseSearchSort.Relevance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var featureFilters = Request.Query
            .Where(kvp => kvp.Key.StartsWith("f_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                kvp => kvp.Key[2..],
                kvp => kvp.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

        var filter = new HouseSearchFilter
        {
            Query = query,
            City = city,
            AreaId = area,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinBedrooms = minBedrooms,
            MinGuests = minGuests,
            HasPool = hasPool,
            PetFriendly = petFriendly,
            FeatureFilters = featureFilters.Count == 0 ? null : featureFilters,
            Sort = sort,
            Page = page,
            PageSize = pageSize
        };

        var areasTask = api.GetAreasAsync(null, ct);
        var featuresTask = api.GetSearchableFeaturesAsync(ct);
        var housesTask = api.GetHousesAsync(filter, ct);

        await Task.WhenAll(areasTask, featuresTask, housesTask);

        var areasRes = areasTask.Result;
        var featuresRes = featuresTask.Result;
        var housesRes = housesTask.Result;

        if (!housesRes.Ok || housesRes.Data is null)
        {
            SetError(housesRes.Message ?? "Could not load houses");
        }

        var vm = new HouseListVm
        {
            Houses = housesRes.Data?.Items ?? [],
            Areas = areasRes.Ok ? areasRes.Data ?? [] : [],
            SearchableFeatures = featuresRes.Ok ? featuresRes.Data ?? [] : [],
            Filter = filter,
            Total = housesRes.Data?.Total ?? 0,
            Page = housesRes.Data?.Page ?? page,
            PageSize = housesRes.Data?.PageSize ?? pageSize
        };

        return View(vm);
    }



    [HttpGet("/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var res = await api.GetHouseAsync(id, ct);
        if (res.Ok && res.Data is not null)
        {
            return View("~/Views/Houses/details.cshtml", res.Data);
        }

        // The API answers 404 for an unknown and for an unpublished house alike.
        return res.StatusCode == HttpStatusCode.NotFound || res.Ok
            ? NotFound()
            : StatusCode(StatusCodes.Status502BadGateway);
    }

    /// <summary>
    /// Proxies a quote for the price widget. A failure is answered with <c>{ code, message }</c>,
    /// where the message is already localized, so the page never shows the API's own text.
    /// </summary>
    [HttpPost("/houses/{id:guid}/quote")]
    public async Task<IActionResult> Quote(Guid id, [FromBody] PriceQuoteRequestDto? payload, CancellationToken ct)
    {
        if (payload is null || !ModelState.IsValid)
        {
            var invalid = PricingErrorText.Describe(
                HttpStatusCode.BadRequest,
                new Dictionary<string, string[]> { [PricingErrors.Dates] = [] },
                localizer,
                PricingErrorContext.Quote);
            return BadRequest(new { code = invalid.Code, message = invalid.Text });
        }

        var request = payload with { HouseId = id };
        var res = await api.GetPriceQuoteAsync(request, ct);

        if (res.Ok)
        {
            if (res.Data is null)
            {
                return StatusCode((int)(res.StatusCode ?? HttpStatusCode.NoContent));
            }

            return Json(res.Data);
        }

        // Only the guest error needs the capacity, so the house is fetched just for that one.
        int? maxGuests = null;
        if (PricingErrorText.Classify(res.StatusCode, res.Errors) == PricingErrors.Guests)
        {
            var house = await api.GetHouseAsync(id, ct);
            maxGuests = house.Data?.MaxGuests;
        }

        var error = PricingErrorText.Describe(res, localizer, PricingErrorContext.Quote, maxGuests);
        var status = res.StatusCode is { } code && (int)code >= 400 ? (int)code : StatusCodes.Status502BadGateway;
        return StatusCode(status, new { code = error.Code, message = error.Text });
    }

    [HttpGet("/houses/{id:guid}/availability")]
    public async Task<IActionResult> Availability(
        Guid id,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        if (to <= from)
        {
            return BadRequest(new { message = "End date must be after start date." });
        }

        var res = await api.GetHouseAvailabilityAsync(id, from, to, ct);
        if (!res.Ok)
        {
            var status = (int)(res.StatusCode ?? HttpStatusCode.BadGateway);
            return StatusCode(status, new { message = res.Message ?? "Could not load availability." });
        }

        var blocks = res.Data ?? [];
        var hasConflict = blocks.Any(b => b.Status != Sommerhus.Domain.Models.AvailabilityStatus.Available);
        return Json(new { available = !hasConflict, blocks });
    }

}
