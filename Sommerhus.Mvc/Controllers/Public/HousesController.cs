using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Public.Houses;
using Sommerhus.Mvc.Services;
using System.Net;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class HousesController(SommerhusApi _api) : Controller
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
        var areasTask = _api.GetAreasAsync(null, ct);
        var housesTask = _api.GetHousesAsync(q, area, page, pageSize, ct);

        await Task.WhenAll(areasTask, housesTask);

        var areasRes = areasTask.Result;
        var housesRes = housesTask.Result;

        ViewBag.Query = q ?? "";
        ViewBag.Area = area?.ToString() ?? "";
        ViewBag.Areas = areasRes.Ok && areasRes.Data is not null ? areasRes.Data : Array.Empty<AreaListItemDto>();

        if (!housesRes.Ok)
        {
            TempData["Err"] = housesRes.Message ?? "Could not load houses";
            return View(Array.Empty<HouseListItemDto>());
        }
        return View(housesRes.Data);
    }



    [HttpGet("/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var res = await _api.GetHouseAsync(id, ct);
        if (!res.Ok)
        {
            TempData["Err"] = res.Message ?? "could not find house list";
            return View();
        }
        return View(res.Data);
    }

    [HttpPost("/houses/{id:guid}/quote")]
    public async Task<IActionResult> Quote(Guid id, [FromBody] PriceQuoteRequestDto payload, CancellationToken ct)
    {
        var request = payload with { HouseId = id };
        var res = await _api.GetPriceQuoteAsync(request, ct);

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
        return StatusCode(status, new { message = res.Message ?? "Kunne ikke hente pris" });
    }

    //[HttpPost("/admin/houses/{id:guid}/images/{imgId:guid}/delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteImage(Guid id, Guid imgId, CancellationToken ct)
    //{
    //    await _api.DeleteHouseImageAsync(id, imgId, ct);
    //    return RedirectToAction("House", "Admin", new { id });
    //}
}
