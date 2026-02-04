using System.Net;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Public.Areas;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class AreasController(SommerhusApi api) : Controller
{
    [HttpGet("/areas")]
    public async Task<IActionResult> Index([FromQuery] string? q, CancellationToken ct)
    {
        var result = await api.GetAreasAsync(q, ct);
        if (!result.Ok || result.Data is null)
        {
            TempData["Err"] = result.Message ?? "Kunne ikke hente områderne.";
            return View(Array.Empty<AreaListItemDto>());
        }

        ViewData["Query"] = q ?? string.Empty;
        return View(result.Data);
    }

    [HttpGet("/areas/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await api.GetAreaAsync(id, ct);
        if (!result.Ok || result.Data is null)
        {
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            TempData["Err"] = result.Message ?? "Kunne ikke hente området.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }
}
