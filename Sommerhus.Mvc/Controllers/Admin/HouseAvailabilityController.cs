using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseAvailabilityController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{
    [HttpPost("/admin/houses/{houseId:guid}/availability")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAvailabilityBlock(Guid houseId, [FromForm] UpsertAvailabilityBlockDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError(localizer["Admin.Availability.Messages.InvalidData"].Value);
            return RedirectToDetails(houseId, "availability");
        }

        var res = await api.CreateHouseAvailabilityBlockAsync(houseId, dto, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Availability.Messages.Added"].Value);
        }
        else if (res.HasValidationErrors)
        {
            SetError(string.Join(" ", res.Errors.SelectMany(e => e.Value)));
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Availability.Messages.AddFailed"].Value);
        }

        return RedirectToDetails(houseId, "availability");
    }

    [HttpPost("/admin/houses/{houseId:guid}/availability/{blockId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvailabilityBlock(Guid houseId, Guid blockId, [FromForm] UpsertAvailabilityBlockDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError(localizer["Admin.Availability.Messages.InvalidData"].Value);
            return RedirectToDetails(houseId, "availability");
        }

        var res = await api.UpdateHouseAvailabilityBlockAsync(houseId, blockId, dto, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Availability.Messages.Updated"].Value);
        }
        else if (res.HasValidationErrors)
        {
            SetError(string.Join(" ", res.Errors.SelectMany(e => e.Value)));
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Availability.Messages.UpdateFailed"].Value);
        }

        return RedirectToDetails(houseId, "availability");
    }

    [HttpPost("/admin/houses/{houseId:guid}/availability/{blockId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvailabilityBlock(Guid houseId, Guid blockId, CancellationToken ct = default)
    {
        var res = await api.DeleteHouseAvailabilityBlockAsync(houseId, blockId, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Availability.Messages.Deleted"].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Availability.Messages.DeleteFailed"].Value);
        }

        return RedirectToDetails(houseId, "availability");
    }
}
