using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseAvailabilityController(AdminApiClient api) : AdminControllerBase
{
    [HttpPost("/admin/houses/{houseId:guid}/availability")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAvailabilityBlock(Guid houseId, [FromForm] UpsertAvailabilityBlockDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid availability data.");
            return RedirectToDetails(houseId, "availability");
        }

        var res = await api.CreateHouseAvailabilityBlockAsync(houseId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Availability block added.");
        }
        else if (res.HasValidationErrors)
        {
            SetError(string.Join(" ", res.Errors.SelectMany(e => e.Value)));
        }
        else
        {
            SetError(res.Message ?? "Could not add availability block.");
        }

        return RedirectToDetails(houseId, "availability");
    }

    [HttpPost("/admin/houses/{houseId:guid}/availability/{blockId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvailabilityBlock(Guid houseId, Guid blockId, [FromForm] UpsertAvailabilityBlockDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid availability data.");
            return RedirectToDetails(houseId, "availability");
        }

        var res = await api.UpdateHouseAvailabilityBlockAsync(houseId, blockId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Availability block updated.");
        }
        else if (res.HasValidationErrors)
        {
            SetError(string.Join(" ", res.Errors.SelectMany(e => e.Value)));
        }
        else
        {
            SetError(res.Message ?? "Could not update availability block.");
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
            SetSuccess("Availability block deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete availability block.");
        }

        return RedirectToDetails(houseId, "availability");
    }
}
