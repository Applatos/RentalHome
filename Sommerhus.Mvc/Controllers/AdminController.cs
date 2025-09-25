using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers;

public class AdminController(ISommerhusApi api, AdminApiClient adminApi) : Controller
{
    private static readonly string[] FeatureValueTypes = ["Bool", "Int", "Decimal", "Text"];
    private bool IsHtmx => Request.Headers.TryGetValue("HX-Request", out var hx) &&
                           string.Equals(hx, "true", StringComparison.OrdinalIgnoreCase);

    // ---------- Landing & tabs ----------
    [HttpGet("/admin")]
    public IActionResult Index(int page = 1, string? q = null)
    {
        ViewBag.Page = page;
        ViewBag.Query = q;
        return View();
    }

    [HttpGet("/admin/tab/houses")]
    public IActionResult HousesTab(int page = 1, string? q = null)
    {
        ViewBag.Page = page;
        ViewBag.Query = q;
        return PartialView("~/Views/Admin/Tabs/_Houses.cshtml");
    }

    [HttpGet("/admin/tab/areas")]
    public IActionResult AreasTab(string? q = null)
    {
        ViewBag.Query = q;
        return PartialView("~/Views/Admin/Tabs/_Areas.cshtml");
    }

    [HttpGet("/admin/tab/features")]
    public IActionResult FeaturesTab(string? q = null)
    {
        ViewBag.Query = q;
        return PartialView("~/Views/Admin/Tabs/_Features.cshtml");
    }

    // ---------- Houses (master/detail + CRUD) ----------
    public sealed record MasterListItem(Guid Id, string Title, string? City, string? Zip, string? Cover);

    public sealed class MasterListVm
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Math.Max(0, Total) / Math.Max(1, PageSize));
        public List<MasterListItem> Items { get; set; } = new();
    }

    [HttpGet("/admin/master")]
    public async Task<IActionResult> Master(string? q, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var res = await adminApi.SearchHousesAsync(q, page, pageSize, ct);
        var vm = new MasterListVm
        {
            Query = res.Query,
            Page = res.Page,
            PageSize = res.PageSize,
            Total = res.Total,
            Items = res.Items.Select(x => new MasterListItem(x.Id, x.Title, x.City, x.Zip, x.Cover)).ToList()
        };
        return PartialView("~/Views/Admin/_MasterList.cshtml", vm);
    }

    [HttpGet("/admin/{id:guid}/detail")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        return await RenderHouseDetailAsync(id, ct);
    }

    [HttpGet("/admin/houses/detail/blank")]
    public IActionResult BlankDetail() => RenderEmptyHouseDetail();

    private async Task<IActionResult> RenderHouseDetailAsync(Guid id, CancellationToken ct, string? successMessage = null)
    {
        var house = await api.GetHouseAsync(id, ct);
        if (house is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(successMessage))
        {
            ViewData["Success"] = successMessage;
        }
        return PartialView("~/Views/Admin/Houses/_Detail.cshtml", house);
    }

    private IActionResult RenderEmptyHouseDetail(string? successMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(successMessage))
        {
            ViewData["Success"] = successMessage;
        }
        return PartialView("~/Views/Admin/Houses/_Detail.cshtml", model: null);
    }

    public class CreateFormVM
    {
        [Required(ErrorMessage = "Titel er påkrævet")]
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Address { get; set; }
        public Guid? CityId { get; set; }
        public string? CitySearch { get; set; }
        public string? Description { get; set; }
        public string? Facilities { get; set; }
    }

    [HttpGet("/admin/create")]
    public IActionResult Create()
    {
        var form = new CreateFormVM();
        if (IsHtmx)
        {
            ViewBag.HouseIsEdit = false;
            return PartialView("~/Views/Admin/Houses/_Form.cshtml", form);
        }

        return View(form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/create")]
    public async Task<IActionResult> CreatePost(CreateFormVM form, CancellationToken ct)
    {
        if (!form.CityId.HasValue || form.CityId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(form.CityId), "By er påkrævet");
        }

        if (!ModelState.IsValid)
        {
            if (IsHtmx)
            {
                ViewBag.HouseIsEdit = false;
                return PartialView("~/Views/Admin/Houses/_Form.cshtml", form);
            }

            return View("Create", form);
        }

        var id = await api.CreateHouseAsync(form, ct);

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Hus oprettet");
        }

        TempData["ok"] = "Hus oprettet";
        return Redirect($"/admin/{id}/images");
    }

    [HttpGet("/admin/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var h = await adminApi.GetHouseAsync(id, ct);
        if (h is null) return NotFound();

        var form = new CreateFormVM
        {
            Title = h.Title,
            Subtitle = h.Subtitle,
            Address = h.Address,
            CityId = h.CityId,
            Description = h.Description,
            Facilities = h.Facilities,
            CitySearch = await ResolveCityLabelAsync(h.CityId, ct)
        };

        ViewBag.HouseId = id;

        if (IsHtmx)
        {
            ViewBag.HouseIsEdit = true;
            return PartialView("~/Views/Admin/Houses/_Form.cshtml", form);
        }

        return View(form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/{id:guid}/edit")]
    public async Task<IActionResult> EditPost(Guid id, CreateFormVM form, CancellationToken ct)
    {
        if (!form.CityId.HasValue || form.CityId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(form.CityId), "By er påkrævet");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.HouseId = id;
            if (IsHtmx)
            {
                ViewBag.HouseIsEdit = true;
                return PartialView("~/Views/Admin/Houses/_Form.cshtml", form);
            }

            return View("Edit", form);
        }

        await api.UpdateHouseAsync(id, form, ct);

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Gemte ændringer");
        }

        TempData["ok"] = "Gemte ændringer";
        return Redirect($"/admin/{id}/images");
    }

    [HttpGet("/admin/{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var house = await api.GetHouseAsync(id, ct);
        if (house is null) return NotFound();

        if (IsHtmx)
        {
            return PartialView("~/Views/Admin/Houses/_Delete.cshtml", house);
        }

        return View("Delete", house);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/{id:guid}/delete")]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct)
    {
        await api.DeleteHouseAsync(id, ct);

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return RenderEmptyHouseDetail("Hus slettet");
        }

        TempData["ok"] = "Hus slettet";
        return Redirect("/admin");
    }

    // ---------- House image management ----------
    [HttpGet("/admin/{id:guid}/images")]
    public async Task<IActionResult> Images(Guid id, CancellationToken ct)
    {
        var h = await api.GetHouseAsync(id, ct);
        if (h is null) return NotFound();
        return View(h);
    }

    [HttpPost("/admin/{id:guid}/images/cover")]
    public async Task<IActionResult> UploadCover(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file != null && file.Length > 0)
        {
            using var s = file.OpenReadStream();
            await api.UploadCoverAsync(id, s, file.FileName, ct);
        }

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Cover opdateret");
        }

        return Redirect($"/admin/{id}/images");
    }

    [HttpPost("/admin/{id:guid}/images/gallery")]
    public async Task<IActionResult> UploadGallery(Guid id, List<IFormFile> files, CancellationToken ct)
    {
        if (files is { Count: > 0 })
        {
            foreach (var file in files.Where(f => f is { Length: > 0 }))
            {
                await using var stream = file.OpenReadStream();
                await api.UploadGalleryImageAsync(id, stream, file.FileName, ct);
            }
        }

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Galleri opdateret");
        }

        return Redirect($"/admin/{id}/images");
    }
    [HttpPost("/admin/{id:guid}/images/floorplan")]
    public async Task<IActionResult> UploadFloorplan(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file != null && file.Length > 0)
        {
            using var s = file.OpenReadStream();
            await api.UploadFloorplanAsync(id, s, file.FileName, ct);
        }

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Plantegning opdateret");
        }

        return Redirect($"/admin/{id}/images");
    }

    [HttpPost("/admin/{id:guid}/images/{imgId:guid}/setcover")]
    public async Task<IActionResult> SetCover(Guid id, Guid imgId, CancellationToken ct)
    {
        await api.SetCoverAsync(id, imgId, ct);

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Cover opdateret");
        }

        return Redirect($"/admin/{id}/images");
    }

    [HttpPost("/admin/{id:guid}/images/{imgId:guid}/delete")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imgId, CancellationToken ct)
    {
        await api.DeleteImageAsync(id, imgId, ct);

        if (IsHtmx)
        {
            Response.Headers["HX-Trigger"] = "admin-houses-updated";
            return await RenderHouseDetailAsync(id, ct, "Billede opdateret");
        }

        return Redirect($"/admin/{id}/images");
    }

    // ---------- City lookup ----------
    [HttpGet("/admin/cities/search")]
    public async Task<IActionResult> SearchCities(string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        var page = await adminApi.SearchZipcodesAsync(q.Trim(), 1, 8, ct);
        var items = (page.Items ?? new List<AdminApiClient.ZipListItem>())
            .Select(x => new { id = x.Id, label = x.Display })
            .ToArray();
        return Json(items);
    }

    private async Task<string?> ResolveCityLabelAsync(Guid? cityId, CancellationToken ct)
    {
        if (!cityId.HasValue || cityId == Guid.Empty) return null;
        var city = await adminApi.GetCityAsync(cityId.Value, ct);
        if (city is null) return null;
        var parts = new[] { city.Zip, city.Name }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(' ', parts);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ---------- Areas ----------
    public sealed record AreaMasterItem(Guid Id, string Name, string Slug, int HouseCount, int ImageCount);

    public sealed class AreaMasterVm
    {
        public string? Query { get; set; }
        public List<AreaMasterItem> Items { get; set; } = new();
    }

    public sealed class AreaDetailVm
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IReadOnlyList<AdminApiClient.AreaImage> Images { get; set; } = Array.Empty<AdminApiClient.AreaImage>();
    }

    public class AreaFormVm
    {
        public Guid? Id { get; set; }
        [Required(ErrorMessage = "Navn er påkrævet")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    [HttpGet("/admin/areas/master")]
    public async Task<IActionResult> AreasMaster(string? q, CancellationToken ct)
    {
        var list = await adminApi.GetAreasAsync(ct);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            list = list.Where(a =>
                    a.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(a.Slug) && a.Slug.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var vm = new AreaMasterVm
        {
            Query = q,
            Items = list
                .OrderBy(a => a.Name)
                .Select(a => new AreaMasterItem(a.Id, a.Name, a.Slug, a.HouseCount, a.ImageCount))
                .ToList()
        };

        return PartialView("~/Views/Admin/Areas/_MasterList.cshtml", vm);
    }

    [HttpGet("/admin/areas/{id:guid}/detail")]
    public async Task<IActionResult> AreaDetail(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return PartialView("~/Views/Admin/Areas/_Detail.cshtml", model: null);
        }

        var area = await adminApi.GetAreaAsync(id, ct);
        if (area is null) return NotFound();

        var vm = new AreaDetailVm
        {
            Id = area.Id,
            Slug = area.Slug ?? string.Empty,
            Name = area.Name,
            Description = area.Description,
            Images = area.Images ?? new List<AdminApiClient.AreaImage>()
        };

        return PartialView("~/Views/Admin/Areas/_Detail.cshtml", vm);
    }

    [HttpGet("/admin/areas/create")]
    public IActionResult AreaCreate()
    {
        ViewBag.AreaIsEdit = false;
        return PartialView("~/Views/Admin/Areas/_Form.cshtml", new AreaFormVm());
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/areas/create")]
    public async Task<IActionResult> AreaCreatePost(AreaFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AreaIsEdit = false;
            return PartialView("~/Views/Admin/Areas/_Form.cshtml", form);
        }

        var id = await adminApi.CreateAreaAsync(form.Name.Trim(), Clean(form.Description), null, ct);
        TempData["ok"] = "Område oprettet";
        Response.Headers["HX-Trigger"] = "admin-areas-updated";
        ViewData["Success"] = "Område oprettet";
        return await AreaDetail(id, ct);
    }

    [HttpGet("/admin/areas/{id:guid}/edit")]
    public async Task<IActionResult> AreaEdit(Guid id, CancellationToken ct)
    {
        var area = await adminApi.GetAreaAsync(id, ct);
        if (area is null) return NotFound();

        var form = new AreaFormVm
        {
            Id = area.Id,
            Name = area.Name,
            Description = area.Description
        };

        ViewBag.AreaIsEdit = true;
        return PartialView("~/Views/Admin/Areas/_Form.cshtml", form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/areas/{id:guid}/edit")]
    public async Task<IActionResult> AreaEditPost(Guid id, AreaFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AreaIsEdit = true;
            return PartialView("~/Views/Admin/Areas/_Form.cshtml", form);
        }

        await adminApi.UpdateAreaAsync(id, form.Name.Trim(), Clean(form.Description), null, ct);
        TempData["ok"] = "Område opdateret";
        Response.Headers["HX-Trigger"] = "admin-areas-updated";
        ViewData["Success"] = "Område opdateret";
        return await AreaDetail(id, ct);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/areas/{id:guid}/delete")]
    public async Task<IActionResult> AreaDelete(Guid id, CancellationToken ct)
    {
        await adminApi.DeleteAreaAsync(id, ct);
        TempData["ok"] = "Område slettet";
        Response.Headers["HX-Trigger"] = "admin-areas-updated";
        ViewData["Success"] = "Område slettet";
        return PartialView("~/Views/Admin/Areas/_Detail.cshtml", model: null);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/areas/{id:guid}/images")]
    public async Task<IActionResult> AreaUploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is { Length: > 0 })
        {
            using var stream = file.OpenReadStream();
            await adminApi.UploadAreaImageAsync(id, stream, file.FileName, ct);
        }

        ViewData["Success"] = "Billede uploadet";
        return await AreaDetail(id, ct);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/areas/{id:guid}/images/{imageId:guid}/delete")]
    public async Task<IActionResult> AreaDeleteImage(Guid id, Guid imageId, CancellationToken ct)
    {
        await adminApi.DeleteAreaImageAsync(id, imageId, ct);
        ViewData["Success"] = "Billede slettet";
        return await AreaDetail(id, ct);
    }

    // ---------- Features ----------
    public sealed record FeatureMasterItem(Guid Id, string Name, string Key, string ValueType, string? IconUrl);

    public sealed class FeatureMasterVm
    {
        public string? Query { get; set; }
        public List<FeatureMasterItem> Items { get; set; } = new();
    }

    public sealed class FeatureDetailVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; }
    }

    public class FeatureFormVm
    {
        public Guid? Id { get; set; }
        [Required(ErrorMessage = "Navn er påkrævet")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Nøgle er påkrævet")]
        public string Key { get; set; } = string.Empty;
        [Required(ErrorMessage = "Datatype er påkrævet")]
        public string ValueType { get; set; } = FeatureValueTypes[0];
        public string? Unit { get; set; }
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; }
    }

    [HttpGet("/admin/features/master")]
    public async Task<IActionResult> FeaturesMaster(string? q, CancellationToken ct)
    {
        var list = await adminApi.GetFeaturesAsync(ct) ?? new List<AdminApiClient.FeatureListItem>();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            list = list.Where(f =>
                    f.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    f.Key.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var vm = new FeatureMasterVm
        {
            Query = q,
            Items = list
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
                .Select(f => new FeatureMasterItem(f.Id, f.Name, f.Key, f.ValueType, f.IconUrl))
                .ToList()
        };

        return PartialView("~/Views/Admin/Features/_MasterList.cshtml", vm);
    }

    [HttpGet("/admin/features/{id:guid}/detail")]
    public async Task<IActionResult> FeatureDetail(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return PartialView("~/Views/Admin/Features/_Detail.cshtml", model: null);
        }

        var list = await adminApi.GetFeaturesAsync(ct) ?? new List<AdminApiClient.FeatureListItem>();
        var feature = list.FirstOrDefault(f => f.Id == id);
        if (feature is null) return NotFound();

        var vm = new FeatureDetailVm
        {
            Id = feature.Id,
            Name = feature.Name,
            Key = feature.Key,
            ValueType = feature.ValueType,
            Unit = feature.Unit,
            IconUrl = feature.IconUrl,
            SortOrder = feature.SortOrder
        };

        return PartialView("~/Views/Admin/Features/_Detail.cshtml", vm);
    }

    [HttpGet("/admin/features/create")]
    public IActionResult FeatureCreate()
    {
        ViewBag.FeatureIsEdit = false;
        ViewBag.FeatureValueTypes = FeatureValueTypes;
        return PartialView("~/Views/Admin/Features/_Form.cshtml", new FeatureFormVm());
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/features/create")]
    public async Task<IActionResult> FeatureCreatePost(FeatureFormVm form, CancellationToken ct)
    {
        if (!FeatureValueTypes.Contains(form.ValueType, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(form.ValueType), "Ugyldig datatype");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.FeatureIsEdit = false;
            ViewBag.FeatureValueTypes = FeatureValueTypes;
            return PartialView("~/Views/Admin/Features/_Form.cshtml", form);
        }

        var newId = await adminApi.CreateFeatureAsync(
            form.Name.Trim(),
            form.Key.Trim(),
            form.ValueType,
            Clean(form.Unit),
            Clean(form.IconUrl),
            form.SortOrder,
            ct);

        TempData["ok"] = "Feature oprettet";
        Response.Headers["HX-Trigger"] = "admin-features-updated";
        ViewData["Success"] = "Feature oprettet";
        return await FeatureDetail(newId, ct);
    }

    [HttpGet("/admin/features/{id:guid}/edit")]
    public async Task<IActionResult> FeatureEdit(Guid id, CancellationToken ct)
    {
        var list = await adminApi.GetFeaturesAsync(ct) ?? new List<AdminApiClient.FeatureListItem>();
        var feature = list.FirstOrDefault(f => f.Id == id);
        if (feature is null) return NotFound();

        var form = new FeatureFormVm
        {
            Id = feature.Id,
            Name = feature.Name,
            Key = feature.Key,
            ValueType = feature.ValueType,
            Unit = feature.Unit,
            IconUrl = feature.IconUrl,
            SortOrder = feature.SortOrder
        };

        ViewBag.FeatureIsEdit = true;
        ViewBag.FeatureValueTypes = FeatureValueTypes;
        return PartialView("~/Views/Admin/Features/_Form.cshtml", form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/features/{id:guid}/edit")]
    public async Task<IActionResult> FeatureEditPost(Guid id, FeatureFormVm form, CancellationToken ct)
    {
        if (!FeatureValueTypes.Contains(form.ValueType, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(form.ValueType), "Ugyldig datatype");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.FeatureIsEdit = true;
            ViewBag.FeatureValueTypes = FeatureValueTypes;
            return PartialView("~/Views/Admin/Features/_Form.cshtml", form);
        }

        await adminApi.UpdateFeatureAsync(
            id,
            form.Name.Trim(),
            form.Key.Trim(),
            form.ValueType,
            Clean(form.Unit),
            Clean(form.IconUrl),
            form.SortOrder,
            ct);

        TempData["ok"] = "Feature opdateret";
        Response.Headers["HX-Trigger"] = "admin-features-updated";
        ViewData["Success"] = "Feature opdateret";
        return await FeatureDetail(id, ct);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/features/{id:guid}/delete")]
    public async Task<IActionResult> FeatureDelete(Guid id, CancellationToken ct)
    {
        await adminApi.DeleteFeatureAsync(id, ct);
        TempData["ok"] = "Feature slettet";
        Response.Headers["HX-Trigger"] = "admin-features-updated";
        ViewData["Success"] = "Feature slettet";
        return PartialView("~/Views/Admin/Features/_Detail.cshtml", model: null);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/features/{id:guid}/icon")]
    public async Task<IActionResult> FeatureUploadIcon(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is { Length: > 0 })
        {
            using var stream = file.OpenReadStream();
            await adminApi.UploadFeatureIconAsync(id, stream, file.FileName, ct);
        }

        Response.Headers["HX-Trigger"] = "admin-features-updated";
        ViewData["Success"] = "Ikon opdateret";
        return await FeatureDetail(id, ct);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/features/{id:guid}/icon/delete")]
    public async Task<IActionResult> FeatureRemoveIcon(Guid id, CancellationToken ct)
    {
        await adminApi.RemoveFeatureIconAsync(id, ct);
        Response.Headers["HX-Trigger"] = "admin-features-updated";
        ViewData["Success"] = "Ikon fjernet";
        return await FeatureDetail(id, ct);
    }
}


