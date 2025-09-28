using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;
using PubHouses = Sommerhus.Contracts.Dtos.Public.Houses;

namespace Sommerhus.Mvc.Controllers;

public sealed class HousesController(SommerhusApi api, AdminApiClient admin) : Controller
{
    [HttpGet("/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var dto = await api.GetHouseAsync(id, ct); // PubHouses.HouseDetailsDto?
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost("/admin/houses/{id:guid}/images/{imgId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imgId, CancellationToken ct)
    {
        await admin.DeleteHouseImageAsync(id, imgId, ct);
        return RedirectToAction("House", "Admin", new { id });
    }
}
