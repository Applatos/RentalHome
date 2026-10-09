using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin.Features;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class FeaturesController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{

    [HttpGet("/admin/features")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetAdminTab("features");
        var res = await api.GetFeaturesAsync(ct);
        if (!res.Ok)
        {
            SetError(res.Message ?? localizer["Admin.Features.Messages.LoadFailed"].Value);
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
            SetError(localizer["Admin.Features.Messages.Required"].Value);
            return RedirectToAction(nameof(Index));
        }
        var res = await api.CreateFeatureAsync(dto, ct);
        if (res.Ok && res.Data is Guid id)
        {
            SetSuccess(localizer["Admin.Features.Messages.Created", id].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Features.Messages.CreateFailed"].Value);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/features/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            SetError(localizer["Admin.Features.Messages.Required"].Value);
            return RedirectToAction(nameof(Index));
        }
        var res = await api.UpdateFeatureAsync(id, dto, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Features.Messages.Updated"].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Features.Messages.UpdateFailed"].Value);
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
            SetSuccess(localizer["Admin.Features.Messages.Deleted"].Value);
        }
        else
        {
            SetError(res.Message ?? localizer["Admin.Features.Messages.DeleteFailed"].Value);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/features/{id:guid}/icon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadIcon(Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            SetError(localizer["Admin.Features.Messages.SelectFile"].Value);
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();
        var res = await api.UploadFeatureIconAsync(id, stream, file.FileName, file.ContentType, ct);
        if (res.Ok)
        {
            SetSuccess(localizer["Admin.Features.Messages.IconUploaded"].Value);
        }
        else
        {
            var errors = res.Errors.Values.SelectMany(messages => messages).ToArray();
            SetError(errors.Length > 0
                ? string.Join(" ", errors)
                : res.Message ?? localizer["Admin.Features.Messages.IconUploadFailed"].Value);
        }

        return RedirectToAction(nameof(Index));
    }
}
