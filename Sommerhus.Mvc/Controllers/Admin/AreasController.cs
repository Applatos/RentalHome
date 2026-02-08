using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Extensions;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin.Areas;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class AreasController(AdminApiClient api) : AdminControllerBase
{

    [HttpGet("/admin/areas")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetAdminTab("areas");
        var res = await api.GetAreasAsync(ct);

        if (!res.Ok || res.Data is null)
        {
            SetError(res.Message ?? "Could not load areas.");
            return View("~/Views/Admin/Areas/Index.cshtml", new AreaListVm { Areas = [] });
        }

        return View("~/Views/Admin/Areas/Index.cshtml", new AreaListVm { Areas = res.Data });
    }

    [HttpGet("/admin/areas/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        SetAdminTab("areas");
        var res = await api.GetAreaAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            SetError(res.Message ?? "Area not found.");
            return RedirectToAction(nameof(Index));
        }

        var cities = await LoadCityOptionsAsync(res.Data.CityIds, ct);

        return View("~/Views/Admin/Areas/Details.cshtml", new AreaDetailsVm
        {
            Area = res.Data,
            Cities = cities,
            ActiveTab = tab
        });
    }

    [HttpGet("/admin/areas/new")]
    public async Task<IActionResult> New(CancellationToken ct = default)
    {
        SetAdminTab("areas");

        var vm = new AreaCreateVm
        {
            Area = new UpsertAreaDto(),
            Cities = await LoadCityOptionsAsync(null, ct)
        };

        return View("~/Views/Admin/Areas/Create.cshtml", vm);
    }

    [HttpPost("/admin/areas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] AreaCreateVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            vm.Cities = await LoadCityOptionsAsync(vm.Area.CityIds, ct);
            SetError("Invalid fields.");
            return View("~/Views/Admin/Areas/Create.cshtml", vm);
        }

        var res = await api.CreateAreaAsync(vm.Area, ct);

        if (!res.Ok)
        {
            SetError(res.Message ?? "Could not create area.");
            vm.Cities = await LoadCityOptionsAsync(vm.Area.CityIds, ct);
            return View("~/Views/Admin/Areas/Create.cshtml", vm);
        }

        SetSuccess("Area created.");
        return RedirectToAction(nameof(Details), new { id = res.Data!.Id });
    }


    [HttpPost("/admin/areas/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, [FromForm] AreaDetailsVm vm, CancellationToken ct = default)
    {
        var dto = new UpsertAreaDto {
            Name = vm.Area.Name, 
            CityIds = vm.Area.CityIds?.ToList() ?? new List<Guid>(), 
            Description = vm.Area.Description
        };

        var res = await api.UpdateAreaAsync(id, dto, ct);

        if (!res.Ok)
        {
            SetError(res.Message ?? "Could not update area.");
            SetAdminTab("areas");
            var rebuiltVm = await BuildAreaEditVmAsync(null, vm.Area.CityIds, vm.Area.Name, vm.Area.Description, ct);
            return View("~/Views/Admin/Areas/Details.cshtml", rebuiltVm);
        }

        SetSuccess("Area updated.");
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost("/admin/areas/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await api.DeleteAreaAsync(id, ct);
        if (res.Ok)
        {
            SetSuccess("Area deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete area.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/areas/{id:guid}/images")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAreaImages(Guid id, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        if (files is null || !files.Any())
        {
            SetError("Please select at least one image.");
            return RedirectToAction(nameof(Details), new { id, tab = "images" });
        }

        var res = await api.UploadAreaImagesAsync(id, files, ct);
        if (res.Ok)
        {
            var uploadedCount = res.Data?.Count ?? 0;
            SetSuccess(uploadedCount > 0 ? $"Uploaded {uploadedCount} image(s)." : "No images were uploaded.");
        }
        else
        {
            SetError(res.Message ?? "Upload failed.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "images" });
    }

    [HttpPost("/admin/areas/{id:guid}/images/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAreaImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError("Invalid area ID.");
            return RedirectToAction(nameof(Details), new { id, tab = "images" });
        }

        var res = await api.DeleteAreaImageAsync(id, imageId, ct);
        if (res.Ok)
        {
            SetSuccess("Image deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete image.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "images" });
    }

    private async Task<AreaDetailsVm> BuildAreaEditVmAsync(
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
            var imagesRes = await api.GetAreaImagesAsync(areaId, ct);
            if (imagesRes.Ok && imagesRes.Data is not null)
            {
                images = imagesRes.Data.ToList();
            }
            else
            {
                images = area.Images?.Select(i => new ImageDto(i.Id, i.Url, null, "gallery")).ToList() ?? new List<ImageDto>();
                if (!imagesRes.Ok && !string.IsNullOrWhiteSpace(imagesRes.Message))
                {
                    SetError(imagesRes.Message);
                }
            }
        }

        return new AreaDetailsVm
        {
            Area = area ?? new AreaDetailsDto(Guid.Empty, name ?? string.Empty, description, images, cityIds.ToList()),
            Cities = cities,
        };
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadCityOptionsAsync(IReadOnlyCollection<Guid>? selectedCityIds, CancellationToken ct)
    {
        var citiesRes = await api.GetCitiesAsync(ct);
        if (citiesRes.Ok && citiesRes.Data is not null)
        {
            return citiesRes.Data.ToSelectList(selectedCityIds).ToList();
        }

        SetError(citiesRes.Message ?? "Could not load cities.");
        return new List<SelectListItem>();
    }
}
