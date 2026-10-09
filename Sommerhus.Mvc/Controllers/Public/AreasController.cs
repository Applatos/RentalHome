using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Public.Areas;

namespace Sommerhus.Mvc.Controllers.Public;

public sealed class AreasController(SommerhusApi api, IStringLocalizer<SharedResource> localizer) : SommerhusControllerBase
{
    [HttpGet("/areas")]
    public async Task<IActionResult> Index([FromQuery] string? q, CancellationToken ct)
    {
        var result = await api.GetAreasAsync(q, ct);
        if (!result.Ok || result.Data is null)
        {
            SetError(result.Message ?? localizer["Areas.Messages.LoadListFailed"].Value);
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

            SetError(result.Message ?? localizer["Areas.Messages.LoadFailed"].Value);
            return RedirectToAction(nameof(Index));
        }

        // The area's own house list has no card data, so the cards come from the house search.
        var houses = await api.GetHousesAsync(new HouseSearchFilter { AreaId = id, PageSize = 50 }, ct);

        return View("~/Views/Areas/details.cshtml", new AreaPageVm
        {
            Area = result.Data,
            Houses = houses.Data?.Items ?? []
        });
    }
}
