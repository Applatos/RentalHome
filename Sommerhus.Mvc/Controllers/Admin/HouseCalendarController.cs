using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseCalendarController(AdminApiClient api) : AdminControllerBase
{
    [HttpPost("/admin/houses/{houseId:guid}/calendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHouseSeasonSpan(Guid houseId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid season span data.");
            return RedirectToDetails(houseId, "calendar");
        }

        var res = await api.AddHouseSeasonSpanAsync(houseId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Season span added.");
        }
        else
        {
            SetError(res.Message ?? "Could not add season span.");
        }

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHouseSeasonSpan(Guid houseId, Guid spanId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid season span data.");
            return RedirectToDetails(houseId, "calendar");
        }

        var res = await api.UpdateHouseSeasonSpanAsync(houseId, spanId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Season span updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not update season span.");
        }

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHouseSeasonSpan(Guid houseId, Guid spanId, CancellationToken ct = default)
    {
        var res = await api.DeleteHouseSeasonSpanAsync(houseId, spanId, ct);
        if (res.Ok)
        {
            SetSuccess("Season span deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete season span.");
        }

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar-override")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCalendarOverride(Guid houseId, [FromForm] Guid calendarId, CancellationToken ct = default)
    {
        var res = await api.SetHouseCalendarOverrideAsync(houseId, calendarId, ct);
        if (res.Ok)
            SetSuccess("Calendar override applied.");
        else
            SetError(res.Message ?? "Could not set calendar override.");

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar-override/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCalendarOverride(Guid houseId, CancellationToken ct = default)
    {
        var res = await api.RemoveHouseCalendarOverrideAsync(houseId, ct);
        if (res.Ok)
            SetSuccess("Calendar override removed. House now uses group default.");
        else
            SetError(res.Message ?? "Could not remove calendar override.");

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar-override/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCalendarOverride(Guid houseId, [FromForm] string name, CancellationToken ct = default)
    {
        var res = await api.CreateHouseCalendarOverrideAsync(houseId, name, ct);
        if (res.Ok)
            SetSuccess("Custom calendar created and applied as override.");
        else
            SetError(res.Message ?? "Could not create custom calendar.");

        return RedirectToDetails(houseId, "calendar");
    }
}
