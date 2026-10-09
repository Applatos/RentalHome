using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseImagesController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{
    [HttpPost("/admin/houses/{id:guid}/images")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadHouseImages(Guid id, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        if (files is null || !files.Any())
        {
            SetError(localizer["Admin.Images.Messages.SelectOne"].Value);
            return RedirectToDetails(id, "images");
        }

        var res = await api.UploadHouseImagesAsync(id, files, ct);
        if (res.Ok)
        {
            var uploadedCount = res.Data?.Count ?? 0;
            SetSuccess(uploadedCount > 0 ? localizer["Admin.Images.Messages.Uploaded", uploadedCount].Value : localizer["Admin.Images.Messages.NoneUploaded"].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Images.Messages.UploadFailed"].Value);
        }

        return RedirectToDetails(id, "images");
    }

    [HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/set-kind")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHouseImageKind(Guid id, Guid imageId, string kind, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError(localizer["Admin.Houses.Messages.InvalidId"].Value);
            return RedirectToDetails(id, "images");
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            SetError(localizer["Admin.Images.Messages.InvalidKind"].Value);
            return RedirectToDetails(id, "images");
        }

        var res = await api.SetHouseImageKindAsync(id, imageId, kind, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Images.Messages.KindSet", localizer.LabelOrText<ImageKind>(kind).ToLower()].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Images.Messages.UpdateFailed"].Value);
        }

        return RedirectToDetails(id, "images");
    }

    [HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHouseImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError(localizer["Admin.Houses.Messages.InvalidId"].Value);
            return RedirectToDetails(id, "images");
        }

        var res = await api.DeleteHouseImageAsync(id, imageId, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Images.Messages.Deleted"].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Images.Messages.DeleteFailed"].Value);
        }

        return RedirectToDetails(id, "images");
    }
}
