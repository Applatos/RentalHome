using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Public.Houses;
using System.Net;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class HousesController(SommerhusApi api) : SommerhusControllerBase
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
        if (!res.Ok)
        {
            SetError(res.Message ?? "Could not find house.");
            return View("~/Views/Houses/Houses.cshtml");
        }
        return View("~/Views/Houses/details.cshtml", res.Data);
    }

    [HttpPost("/houses/{id:guid}/quote")]
    public async Task<IActionResult> Quote(Guid id, [FromBody] PriceQuoteRequestDto payload, CancellationToken ct)
    {
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

        if (res.HasValidationErrors)
        {
            return BadRequest(new { errors = res.Errors });
        }

        var status = (int)(res.StatusCode ?? HttpStatusCode.BadGateway);
        return StatusCode(status, new { message = res.Message ?? "Could not get price quote" });
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
