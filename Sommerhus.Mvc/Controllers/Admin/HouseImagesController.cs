using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HouseImagesController(AdminApiClient api) : AdminControllerBase
{
    [HttpPost("/admin/houses/{id:guid}/images")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadHouseImages(Guid id, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        if (files is null || !files.Any())
        {
            SetError("Please select at least one image.");
            return RedirectToDetails(id, "images");
        }

        var res = await api.UploadHouseImagesAsync(id, files, ct);
        if (res.Ok)
        {
            var uploadedCount = res.Data?.Count ?? 0;
            SetSuccess(uploadedCount > 0 ? $"Uploaded {uploadedCount} image(s)." : "No images were uploaded.");
        }
        else
        {
            SetError(res.Message ?? "Upload failed.");
        }

        return RedirectToDetails(id, "images");
    }

    [HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/set-kind")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHouseImageKind(Guid id, Guid imageId, string kind, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError("Invalid house ID.");
            return RedirectToDetails(id, "images");
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            SetError("Invalid image type.");
            return RedirectToDetails(id, "images");
        }

        var res = await api.SetHouseImageKindAsync(id, imageId, kind, ct);
        if (res.Ok)
        {
            SetSuccess($"Set to {kind}.");
        }
        else
        {
            SetError(res.Message ?? "Could not update image.");
        }

        return RedirectToDetails(id, "images");
    }

    [HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHouseImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError("Invalid house ID.");
            return RedirectToDetails(id, "images");
        }

        var res = await api.DeleteHouseImageAsync(id, imageId, ct);
        if (res.Ok)
        {
            SetSuccess("Image deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete image.");
        }

        return RedirectToDetails(id, "images");
    }
}
