using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers;

public class AreasController(ISommerhusApi api) : Controller
{
    public record IndexVm(string? Query, IReadOnlyList<AreaListItem> Areas);

    [HttpGet("/areas")]
    public async Task<IActionResult> Index([FromQuery] string? q, CancellationToken ct)
    {
        var areas = await api.GetAreasAsync(q, ct);
        return View(new IndexVm(q, areas));
    }

    [HttpGet("/areas/{slug}")]
    [HttpGet("/area/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var area = await api.GetAreaAsync(slug, ct);
        return area is null ? NotFound() : View(area);
    }
}
