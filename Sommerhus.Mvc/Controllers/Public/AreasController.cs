using System.Net;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class AreasController(SommerhusApi api) : SommerhusControllerBase
{
    [HttpGet("/areas")]
    public async Task<IActionResult> Index([FromQuery] string? q, CancellationToken ct)
    {
        var result = await api.GetAreasAsync(q, ct);
        if (!result.Ok || result.Data is null)
        {
            SetError(result.Message ?? "Could not load areas.");
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

            SetError(result.Message ?? "Could not load area.");
            return RedirectToAction(nameof(Index));
        }

        return View("~/Views/Areas/details.cshtml", result.Data);
    }
}
