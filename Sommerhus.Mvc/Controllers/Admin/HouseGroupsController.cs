using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin.HouseGroups;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseGroupsController(AdminApiClient api) : AdminControllerBase
{
    [HttpGet("/admin/house-groups")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetAdminTab("house-groups");
        
        var groupsRes = await api.GetHouseGroupListAsync(ct);
        if (!groupsRes.Ok || groupsRes.Data is null)
        {
            SetError(groupsRes.Message ?? "Could not load house groups.");
            return View("~/Views/Admin/HouseGroups/Index.cshtml", Array.Empty<HouseGroupDto>());
        }

        return View("~/Views/Admin/HouseGroups/Index.cshtml", groupsRes.Data);
    }

    [HttpGet("/admin/house-groups/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        SetAdminTab("house-groups");
        
        var groupRes = await api.GetHouseGroupAsync(id, ct);
        if (!groupRes.Ok || groupRes.Data is null)
        {
            SetError(groupRes.Message ?? "House group not found.");
            return RedirectToAction(nameof(Index));
        }

        var seasonCodesRes = await api.GetSeasonCodesAsync(ct);
        var seasonCodes = seasonCodesRes.Ok && seasonCodesRes.Data is not null 
            ? seasonCodesRes.Data 
            : Array.Empty<SeasonCodeDto>();

        var vm = new HouseGroupDetailsVm
        {
            Group = groupRes.Data,
            SeasonCodes = seasonCodes,
            Tab = tab
        };

        return View("~/Views/Admin/HouseGroups/Details.cshtml", vm);
    }

    [HttpGet("/admin/house-groups/new")]
    public IActionResult New()
    {
        SetAdminTab("house-groups");
        return View("~/Views/Admin/HouseGroups/Create.cshtml", new UpsertHouseGroupDto());
    }

    [HttpPost("/admin/house-groups")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UpsertHouseGroupDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetAdminTab("house-groups");
            return View("~/Views/Admin/HouseGroups/Create.cshtml", dto);
        }

        var res = await api.CreateHouseGroupAsync(new HouseGroupDto(Guid.NewGuid(), dto.Name), ct);
        if (res.Ok && res.Data is not null)
        {
            SetSuccess("House group created.");
            return RedirectToAction(nameof(Index));
        }

        SetError(res.Message ?? "Could not create house group.");
        SetAdminTab("house-groups");
        return View("~/Views/Admin/HouseGroups/Create.cshtml", dto);
    }

    [HttpGet("/admin/house-groups/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        SetAdminTab("house-groups");
        
        var groupRes = await api.GetHouseGroupAsync(id, ct);
        if (!groupRes.Ok || groupRes.Data is null)
        {
            SetError(groupRes.Message ?? "House group not found.");
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
            SetAdminTab("house-groups");
            return View("~/Views/Admin/HouseGroups/Edit.cshtml", dto);
        }

        var res = await api.UpdateHouseGroupAsync(id, dto, ct);
        if (res.Ok)
        {
            SetSuccess("House group updated.");
            return RedirectToAction(nameof(Details), new { id });
        }

        SetError(res.Message ?? "Could not update house group.");
        SetAdminTab("house-groups");
        return View("~/Views/Admin/HouseGroups/Edit.cshtml", dto);
    }




    [HttpPost("/admin/house-groups/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await api.DeleteHouseGroupAsync(id, ct);
        if (res.Ok)
        {
            SetSuccess("House group deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete house group.");
        }

        return RedirectToAction(nameof(Index));
    }














    [HttpPost("/admin/house-groups/{groupId:guid}/calendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSeasonSpan(Guid groupId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid season span data.");
            return RedirectToAction(nameof(Details), new { id = groupId, tab = "calendar" });
        }

        var res = await api.AddSeasonSpanAsync(groupId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Season span added.");
        }
        else
        {
            SetError(res.Message ?? "Could not add season span.");
        }

        return RedirectToAction(nameof(Details), new { id = groupId, tab = "calendar" });
    }

    [HttpPost("/admin/house-groups/{groupId:guid}/calendar/{spanId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSeasonSpan(Guid groupId, Guid spanId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid season span data.");
            return RedirectToAction(nameof(Details), new { id = groupId, tab = "calendar" });
        }

        var res = await api.UpdateSeasonSpanAsync(groupId, spanId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Season span updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not update season span.");
        }

        return RedirectToAction(nameof(Details), new { id = groupId, tab = "calendar" });
    }

    [HttpPost("/admin/house-groups/{groupId:guid}/calendar/{spanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSeasonSpan(Guid groupId, Guid spanId, CancellationToken ct = default)
    {
        var res = await api.DeleteSeasonSpanAsync(groupId, spanId, ct);
        if (res.Ok)
        {
            SetSuccess("Season span deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete season span.");
        }

        return RedirectToAction(nameof(Details), new { id = groupId, tab = "calendar" });
    }
}

