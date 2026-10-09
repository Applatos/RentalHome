using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseCalendarController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{
    [HttpPost("/admin/houses/{houseId:guid}/calendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHouseSeasonSpan(Guid houseId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError(localizer["Admin.Calendar.Messages.InvalidSpan"].Value);
            return RedirectToDetails(houseId, "calendar");
        }

        var res = await api.AddHouseSeasonSpanAsync(houseId, dto, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Calendar.Messages.SpanAdded"].Value);
        }
        else
        {
            SetError(ApiErrorText.Describe(res, localizer["Admin.Calendar.Messages.SpanAddFailed"].Value));
        }

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHouseSeasonSpan(Guid houseId, Guid spanId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError(localizer["Admin.Calendar.Messages.InvalidSpan"].Value);
            return RedirectToDetails(houseId, "calendar");
        }

        var res = await api.UpdateHouseSeasonSpanAsync(houseId, spanId, dto, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Calendar.Messages.SpanUpdated"].Value);
        }
        else
        {
            SetError(ApiErrorText.Describe(res, localizer["Admin.Calendar.Messages.SpanUpdateFailed"].Value));
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
            SetSuccess(localizer["Admin.Calendar.Messages.SpanDeleted"].Value);
        }
        else
        {
            SetError(ApiErrorText.Describe(res, localizer["Admin.Calendar.Messages.SpanDeleteFailed"].Value));
        }

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar-override")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCalendarOverride(Guid houseId, [FromForm] Guid calendarId, CancellationToken ct = default)
    {
        var res = await api.SetHouseCalendarOverrideAsync(houseId, calendarId, ct);
        if (res.Ok)
            SetSuccess(localizer["Admin.Calendar.Messages.OverrideApplied"].Value);
        else
            SetError(ApiErrorText.Describe(res, localizer["Admin.Calendar.Messages.OverrideApplyFailed"].Value));

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar-override/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCalendarOverride(Guid houseId, CancellationToken ct = default)
    {
        var res = await api.RemoveHouseCalendarOverrideAsync(houseId, ct);
        if (res.Ok)
            SetSuccess(localizer["Admin.Calendar.Messages.OverrideRemoved"].Value);
        else
            SetError(ApiErrorText.Describe(res, localizer["Admin.Calendar.Messages.OverrideRemoveFailed"].Value));

        return RedirectToDetails(houseId, "calendar");
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar-override/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCalendarOverride(Guid houseId, [FromForm] string? name, CancellationToken ct = default)
    {
        var res = await api.CreateHouseCalendarOverrideAsync(houseId, name, ct);
        if (res.Ok)
            SetSuccess(localizer["Admin.Calendar.Messages.CustomCreated"].Value);
        else
            SetError(ApiErrorText.Describe(res, localizer["Admin.Calendar.Messages.CustomCreateFailed"].Value));

        return RedirectToDetails(houseId, "calendar");
    }
}
