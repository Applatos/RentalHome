using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;
using PubAreas = Sommerhus.Contracts.Dtos.Public.Areas;
using PubHouses = Sommerhus.Contracts.Dtos.Public.Houses;

namespace Sommerhus.Mvc.Controllers;

public sealed class HomeController(SommerhusApi api) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? area, CancellationToken ct)
    {
        var houses = await api.GetHousesAsync(citySlug: area, q: q, skip: 0, take: 20, ct);
        var areas = await api.GetAreasAsync(q, ct);

        ViewBag.Query = q ?? "";
        ViewBag.Area = area ?? "";
        ViewBag.Areas = areas; // IList<PubAreas.AreaListItemDto>
        return View(houses);   // IList<PubHouses.HouseListItemDto>
    }
}
