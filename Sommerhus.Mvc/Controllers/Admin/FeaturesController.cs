using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers.Admin;

[Authorize]
public sealed class FeaturesController : Controller
{
    private readonly AdminApiClient _api;

    public FeaturesController(AdminApiClient api) => _api = api;

    [HttpGet("/admin/features")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var res = await _api.GetFeaturesAsync(ct);
        if (!res.Ok)
        {
            TempData["Err"] = res.Message ?? "Could not load features.";
            return View();
        }
        ViewData["AdminTab"] = "features";
        return View(res.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            TempData["Err"] = "Name, Key and Type are required.";
            return RedirectToAction(nameof(Index));
        }
        var res = await _api.CreateFeatureAsync(dto, ct);
        if (res.Ok && res.Data is Guid id)
        {
            TempData["Ok"] = $"Feature created (#{id}).";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Could not create feature.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            TempData["Err"] = "Name, Key and Type are required.";
            return RedirectToAction(nameof(Index));
        }
        var res = await _api.UpdateFeatureAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Feature updated.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Could not update feature.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await _api.DeleteFeatureAsync(id, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Feature deleted.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Could not delete feature. It may be in use.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/features/{id:guid}/icon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "features";

        if (file is null || file.Length == 0)
        {
            TempData["Err"] = "Select a file.";
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();
        var res = await _api.UploadFeatureIconAsync(id, stream, file.FileName, file.ContentType, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Icon uploaded.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Could not upload icon.";
        }

        return RedirectToAction(nameof(Index));
    }
}
