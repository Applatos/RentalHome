using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class HousesController(SommerhusApi _api) : Controller
{
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
