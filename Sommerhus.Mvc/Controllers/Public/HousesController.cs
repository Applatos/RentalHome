using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;

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
    public async Task<IActionResult> Houses([FromQuery] string? q, [FromQuery] int page = 0, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var res = await _api.GetHousesAsync(q, page, pageSize, ct);

        if (!res.Ok)
        {
            TempData["Err"] = res.Message ?? "could not find house list";
            return View();
        }
        return View(res.Data);
    }



    [HttpGet("/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var dto = await _api.GetHouseAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    //[HttpPost("/admin/houses/{id:guid}/images/{imgId:guid}/delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteImage(Guid id, Guid imgId, CancellationToken ct)
    //{
    //    await _api.DeleteHouseImageAsync(id, imgId, ct);
    //    return RedirectToAction("House", "Admin", new { id });
    //}
}
