using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Mvc.Extensions;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin;

namespace Sommerhus.Mvc.Controllers.Admin;

[Authorize]
public sealed class AreasController : Controller
{
    private readonly AdminApiClient _api;

    public AreasController(AdminApiClient api) => _api = api;

    [HttpGet("/admin/areas")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var res = await _api.GetAreasAsync(ct);
        ViewData["AdminTab"] = "areas";

        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] ??= res.Message ?? "Could not load areas.";
            return View(Array.Empty<AreaListItemDto>());
        }

        return View(res.Data);
    }

    [HttpGet("/admin/areas/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "areas";
        var res = await _api.GetAreaAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] = res.Message ?? "Area not found.";
            return RedirectToAction(nameof(Index));
        }

        var imagesRes = await _api.GetAreaImagesAsync(id, ct);
        IReadOnlyList<ImageDto> galleryImages;
        if (imagesRes.Ok && imagesRes.Data is not null)
        {
            galleryImages = imagesRes.Data;
        }
        else
        {
            galleryImages = res.Data.Images?.Select(i => new ImageDto(i.Id, i.Url, null, "gallery")).ToList() ?? new List<ImageDto>();
            if (!imagesRes.Ok && !string.IsNullOrWhiteSpace(imagesRes.Message))
            {
                TempData["Err"] ??= imagesRes.Message;
            }
        }

        ViewBag.AreaImages = galleryImages;
        ViewBag.Tab = tab;
        return View(res.Data);
    }

    [HttpGet("/admin/areas/new")]
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        var vm = await BuildAreaEditVmAsync(null, Array.Empty<Guid>(), null, null, ct);
        ViewData["AdminTab"] = "areas";
        return View("Edit", vm);
    }

    [HttpPost("/admin/areas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] AreaEditVm vm, CancellationToken ct = default)
    {
        var dto = new UpsertAreaDto(vm.Name, vm.CityIds, vm.Description);
        var res = await _api.CreateAreaAsync(dto, ct);
        if (res.Ok && res.Data is not null)
        {
            TempData["Ok"] = "Area created.";
            return RedirectToAction(nameof(Details), new { id = res.Data.Id });
        }
        TempData["Err"] = res.Message ?? "Could not create area.";
        var rebuiltVm = await BuildAreaEditVmAsync(null, vm.CityIds, vm.Name, vm.Description, ct);
        ViewData["AdminTab"] = "areas";
        return View("Edit", rebuiltVm);
    }

    [HttpGet("/admin/areas/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var res = await _api.GetAreaAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] = res.Message ?? "Area not found.";
            return RedirectToAction(nameof(Index));
        }

        var dto = res.Data;
        var vm = await BuildAreaEditVmAsync(res.Data, dto.CityIds, dto.Name, dto.Description, ct);
        ViewData["AdminTab"] = "areas";
        return View("Edit", vm);
    }

    [HttpPost("/admin/areas/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromForm] AreaEditVm vm, CancellationToken ct = default)
    {
        var dto = new UpsertAreaDto(vm.Name, vm.CityIds, vm.Description);
        var res = await _api.UpdateAreaAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Area updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["Err"] = res.Message ?? "Could not update area.";
        var rebuiltVm = await BuildAreaEditVmAsync(null, vm.CityIds, vm.Name, vm.Description, ct);
        ViewData["AdminTab"] = "areas";
        return View("Edit", rebuiltVm);
    }

    [HttpPost("/admin/areas/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await _api.DeleteAreaAsync(id, ct);
        TempData[res.Ok ? "Ok" : "Err"] = res.Ok
            ? "Area deleted."
            : res.Message ?? "Could not delete area.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile? file, string? redirectTo, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "Invalid area.";
            return RedirectAfterImageChange(id, redirectTo);
        }

        if (file is null || file.Length == 0)
        {
            TempData["Err"] = "Select an image.";
            return RedirectAfterImageChange(id, redirectTo);
        }

        var res = await _api.UploadAreaImageAsync(id, file, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Image uploaded.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Could not upload image.";
        }

        return RedirectAfterImageChange(id, redirectTo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, string? redirectTo, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "Invalid area.";
            return RedirectAfterImageChange(id, redirectTo);
        }

        var res = await _api.DeleteAreaImageAsync(id, imageId, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Image deleted.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Could not delete image.";
        }

        return RedirectAfterImageChange(id, redirectTo);
    }

    private async Task<AreaEditVm> BuildAreaEditVmAsync(
        AreaDetailsDto? area,
        IReadOnlyCollection<Guid>? selectedCityIds,
        string? name,
        string? description,
        CancellationToken ct)
    {
        var cityIds = selectedCityIds ?? area?.CityIds ?? Array.Empty<Guid>();
        var cities = await LoadCityOptionsAsync(cityIds, ct);

        var images = new List<ImageDto>();
        if (area?.Id is { } areaId && areaId != Guid.Empty)
        {
            var imagesRes = await _api.GetAreaImagesAsync(areaId, ct);
            if (imagesRes.Ok && imagesRes.Data is not null)
            {
                images = imagesRes.Data.ToList();
            }
            else
            {
                images = area.Images?.Select(i => new ImageDto(i.Id, i.Url, null, "gallery")).ToList() ?? new List<ImageDto>();
                if (!imagesRes.Ok && !string.IsNullOrWhiteSpace(imagesRes.Message))
                {
                    TempData["Err"] ??= imagesRes.Message;
                }
            }
        }

        return new AreaEditVm
        {
            Id = area?.Id is { } idValue && idValue != Guid.Empty ? idValue : null,
            Name = name ?? area?.Name ?? string.Empty,
            CityIds = cityIds.ToList(),
            Description = description ?? area?.Description,
            Images = images,
            Cities = cities
        };
    }

    private IActionResult RedirectAfterImageChange(Guid id, string? redirectTo)
    {
        if (string.Equals(redirectTo, "edit", StringComparison.OrdinalIgnoreCase))
        {
            return id == Guid.Empty
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Edit), new { id });
        }

        return id == Guid.Empty
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { id, tab = "images" });
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadCityOptionsAsync(IReadOnlyCollection<Guid>? selectedCityIds, CancellationToken ct)
    {
        var citiesRes = await _api.GetCitiesAsync(ct);
        if (citiesRes.Ok && citiesRes.Data is not null)
        {
            return citiesRes.Data.ToSelectList(selectedCityIds).ToList();
        }

        TempData["Err"] ??= citiesRes.Message ?? "Could not load cities.";
        return new List<SelectListItem>();
    }
}
