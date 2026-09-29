using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin.Features;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class FeaturesController(AdminApiClient api) : AdminControllerBase
{

    [HttpGet("/admin/features")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetAdminTab("features");
        var res = await api.GetFeaturesAsync(ct);
        if (!res.Ok)
        {
            SetError(res.Message ?? "Could not load features.");
            return View("~/Views/Admin/Features/Index.cshtml", new FeatureListVm { Features = [] });
        }
        return View("~/Views/Admin/Features/Index.cshtml", new FeatureListVm { Features = res.Data ?? [] });
    }

    [HttpPost("/admin/features")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            SetError("Name, Key and Type are required.");
            return RedirectToAction(nameof(Index));
        }
        var res = await api.CreateFeatureAsync(dto, ct);
        if (res.Ok && res.Data is Guid id)
        {
            SetSuccess($"Feature created (#{id}).");
        }
        else
        {
            SetError(res.Message ?? "Could not create feature.");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/features/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            SetError("Name, Key and Type are required.");
            return RedirectToAction(nameof(Index));
        }
        var res = await api.UpdateFeatureAsync(id, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Feature updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not update feature.");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/features/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await api.DeleteFeatureAsync(id, ct);
        if (res.Ok)
        {
            SetSuccess("Feature deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete feature. It may be in use.");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/features/{id:guid}/icon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            SetError("Select a file.");
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();
        var res = await api.UploadFeatureIconAsync(id, stream, file.FileName, file.ContentType, ct);
        if (res.Ok)
        {
            SetSuccess("Icon uploaded.");
        }
        else
        {
            var errors = res.Errors.Values.SelectMany(messages => messages).ToArray();
            SetError(errors.Length > 0
                ? string.Join(" ", errors)
                : res.Message ?? "Could not upload icon.");
        }

        return RedirectToAction(nameof(Index));
    }
}
