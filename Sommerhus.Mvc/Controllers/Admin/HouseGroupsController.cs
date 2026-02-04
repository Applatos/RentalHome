using Microsoft.AspNetCore.Mvc;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public class HouseGroupsController(AdminApiClient api) : Controller
{
    [HttpGet("/admin/house-groups")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "house-groups";
        
        var groupsRes = await api.GetHouseGroupListAsync(ct);
        if (!groupsRes.Ok || groupsRes.Data is null)
        {
            TempData["Err"] = groupsRes.Message ?? "Kunne ikke hente husgrupper.";
            return View("~/Views/Admin/HouseGroups/Index.cshtml", Array.Empty<HouseGroupListItemDto>());
        }

        return View("~/Views/Admin/HouseGroups/Index.cshtml", groupsRes.Data);
    }

    [HttpGet("/admin/house-groups/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "house-groups";
        
        var groupRes = await api.GetHouseGroupAsync(id, ct);
        if (!groupRes.Ok || groupRes.Data is null)
        {
            TempData["Err"] = groupRes.Message ?? "Husgruppe ikke fundet.";
            return RedirectToAction(nameof(Index));
        }

        var seasonCodesRes = await api.GetSeasonCodesAsync(ct);
        var seasonCodes = seasonCodesRes.Ok && seasonCodesRes.Data is not null 
            ? seasonCodesRes.Data 
            : Array.Empty<SeasonCodeDto>();

        var vm = new HouseGroupDetailsVm(groupRes.Data, seasonCodes);

        return View("~/Views/Admin/HouseGroups/Details.cshtml", vm);
    }

    [HttpGet("/admin/house-groups/new")]
    public IActionResult Create()
    {
        ViewData["AdminTab"] = "house-groups";
        return View("~/Views/Admin/HouseGroups/Create.cshtml", new UpsertHouseGroupDto());
    }

    [HttpPost("/admin/house-groups")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UpsertHouseGroupDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewData["AdminTab"] = "house-groups";
            return View("~/Views/Admin/HouseGroups/Create.cshtml", dto);
        }

        var res = await api.CreateHouseGroupAsync(new HouseGroupDto(dto.Name), ct);
        if (res.Ok && res.Data is not null)
        {
            TempData["Ok"] = "Husgruppe oprettet.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Err"] = res.Message ?? "Kunne ikke oprette husgruppe.";
        ViewData["AdminTab"] = "house-groups";
        return View("~/Views/Admin/HouseGroups/Create.cshtml", dto);
    }

    [HttpGet("/admin/house-groups/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "house-groups";
        
        var groupRes = await api.GetHouseGroupAsync(id, ct);
        if (!groupRes.Ok || groupRes.Data is null)
        {
            TempData["Err"] = groupRes.Message ?? "Husgruppe ikke fundet.";
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpsertHouseGroupDto { Name = groupRes.Data.Name };
        return View("~/Views/Admin/HouseGroups/Edit.cshtml", dto);
    }

    [HttpPost("/admin/house-groups/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpsertHouseGroupDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewData["AdminTab"] = "house-groups";
            return View("~/Views/Admin/HouseGroups/Edit.cshtml", dto);
        }

        var res = await api.UpdateHouseGroupAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Husgruppe opdateret.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Err"] = res.Message ?? "Kunne ikke opdatere husgruppe.";
        ViewData["AdminTab"] = "house-groups";
        return View("~/Views/Admin/HouseGroups/Edit.cshtml", dto);
    }

    [HttpPost("/admin/house-groups/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await api.DeleteHouseGroupAsync(id, ct);
        TempData[res.Ok ? "Ok" : "Err"] = res.Ok
            ? "Husgruppe slettet."
            : res.Message ?? "Kunne ikke slette husgruppe.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/house-groups/{groupId:guid}/calendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSeasonSpan(Guid groupId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige data for sæsonperiode.";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        var res = await api.AddSeasonSpanAsync(groupId, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Sæsonperiode tilføjet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke tilføje sæsonperiode.";
        }

        return RedirectToAction(nameof(Details), new { id = groupId });
    }

    [HttpPost("/admin/house-groups/{groupId:guid}/calendar/{spanId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSeasonSpan(Guid groupId, Guid spanId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige data for sæsonperiode.";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        var res = await api.UpdateSeasonSpanAsync(groupId, spanId, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Sæsonperiode opdateret.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke opdatere sæsonperiode.";
        }

        return RedirectToAction(nameof(Details), new { id = groupId });
    }

    [HttpPost("/admin/house-groups/{groupId:guid}/calendar/{spanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSeasonSpan(Guid groupId, Guid spanId, CancellationToken ct = default)
    {
        var res = await api.DeleteSeasonSpanAsync(groupId, spanId, ct);
        TempData[res.Ok ? "Ok" : "Err"] = res.Ok
            ? "Sæsonperiode slettet."
            : res.Message ?? "Kunne ikke slette sæsonperiode.";

        return RedirectToAction(nameof(Details), new { id = groupId });
    }
}

public record HouseGroupDetailsVm(
    HouseGroupDetailsDto Group,
    IReadOnlyList<SeasonCodeDto> SeasonCodes
);
