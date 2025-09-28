using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;
using AdmHouses = Sommerhus.Contracts.Dtos.Admin.Houses;

namespace Sommerhus.Mvc.Controllers;

public sealed class AdminController(AdminApiClient admin) : Controller
{
    [HttpGet("/admin")]
    public IActionResult Index() => RedirectToAction(nameof(Houses));

    [HttpGet("/admin/houses")]
    public async Task<IActionResult> Houses([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        AdmHouses.HousesPageDto res = await admin.SearchHousesAsync(q, page, pageSize, ct);
        return View(res);
    }

    [HttpGet("/admin/houses/{id:guid}")]
    public async Task<IActionResult> House(Guid id, CancellationToken ct)
    {
        var dto = await admin.GetHouseAsync(id, ct); // AdmHouses.HouseAdminDetailsDto?
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet("/admin/areas")]
    public async Task<IActionResult> Areas(CancellationToken ct)
        => View(await admin.GetAreasAsync(ct));

    [HttpGet("/admin/areas/{id:guid}")]
    public async Task<IActionResult> Area(Guid id, CancellationToken ct)
    {
        var dto = await admin.GetAreaAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet("/admin/features")]
    public async Task<IActionResult> Features(CancellationToken ct)
        => View(await admin.GetFeaturesAsync(ct));
}
