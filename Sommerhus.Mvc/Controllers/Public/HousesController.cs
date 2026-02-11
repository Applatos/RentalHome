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
    public async Task<IActionResult> Houses([FromQuery] string? q, [FromQuery] Guid? area, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var areasTask = api.GetAreasAsync(null, ct);
        var housesTask = api.GetHousesAsync(q, area, page, pageSize, ct);

        await Task.WhenAll(areasTask, housesTask);

        var areasRes = areasTask.Result;
        var housesRes = housesTask.Result;

        if (!housesRes.Ok || housesRes.Data is null)
        {
            SetError(housesRes.Message ?? "Could not load houses");
        }

        var vm = new HouseListVm
        {
            Houses = housesRes.Data?.Items ?? [],
            Areas = areasRes.Ok ? areasRes.Data ?? [] : [],
            Query = q ?? string.Empty,
            SelectedArea = area?.ToString() ?? string.Empty
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

}
