using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Sommerhus.Mvc.Services;

namespace Sommerhus.Mvc.Controllers;

public class AdminController(ISommerhusApi api, AdminApiClient adminApi) : Controller
{
    // ---------- LANDING (master–detail shell) ----------
    [HttpGet("/admin")]
    public IActionResult Index(int page = 1, string? q = null)
    {
        ViewBag.Page = page;
        ViewBag.Query = q;
        return View();
    }

    // ---------- MASTER (paged partial) ----------
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

    // ---------- DETAIL (enkel – vi genbruger dine eksisterende DTO’er/services) ----------
    public sealed class DetailVm { public Services.HouseDetails House { get; set; } = default!; }

    [HttpGet("/admin/{id:guid}/detail")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var house = await api.GetHouseAsync(id, ct);
        if (house is null) return NotFound();
        return PartialView("~/Views/Admin/_Details.cshtml", new DetailVm { House = house });
    }

    // ---------- CREATE/EDIT/DELETE ----------
    public class CreateFormVM
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Address { get; set; }
        public Guid? CityId { get; set; }
        public string? Description { get; set; }
        public string? Facilities { get; set; }
    }

    private async Task PopulateCitiesAsync(Guid? selectedCityId, CancellationToken ct)
    {
        var cities = await api.GetCitiesAsync(ct);
        ViewBag.CityOptions = cities
            .OrderBy(c => c.City)
            .ThenBy(c => c.Zip)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(c.Zip) ? c.City : $"{c.Zip} {c.City}",
                Selected = selectedCityId.HasValue && c.Id == selectedCityId.Value
            })
            .ToList();
    }

    [HttpGet("/admin/create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var form = new CreateFormVM();
        await PopulateCitiesAsync(form.CityId, ct);
        return View(form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/create")]
    public async Task<IActionResult> CreatePost(CreateFormVM form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            ModelState.AddModelError(nameof(form.Title), "Titel er påkrævet");
        }
        if (!form.CityId.HasValue || form.CityId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(form.CityId), "By er påkrævet");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCitiesAsync(form.CityId, ct);
            return View("Create", form);
        }

        var id = await api.CreateHouseAsync(form, ct);
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
            Facilities = h.Facilities
        };

        ViewBag.HouseId = id;
        await PopulateCitiesAsync(form.CityId, ct);
        return View(form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/{id:guid}/edit")]
    public async Task<IActionResult> EditPost(Guid id, CreateFormVM form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            ModelState.AddModelError(nameof(form.Title), "Titel er påkrævet");
        }
        if (!form.CityId.HasValue || form.CityId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(form.CityId), "By er påkrævet");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.HouseId = id;
            await PopulateCitiesAsync(form.CityId, ct);
            return View("Edit", form);
        }

        await api.UpdateHouseAsync(id, form, ct);
        TempData["ok"] = "Gemte ændringer";
        return Redirect($"/admin/{id}/images");
    }

// ---------- IMAGES (klassisk side – bevares) ----------
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
        return Redirect($"/admin/{id}/images");
    }

    [HttpPost("/admin/{id:guid}/images/gallery")]
    public async Task<IActionResult> UploadGallery(Guid id, List<IFormFile> files, CancellationToken ct)
    {
        var list = files?.Where(f => f != null && f.Length > 0).Select(f => (f!.OpenReadStream(), f.FileName)).ToList() ?? new();
        if (list.Count > 0) await api.UploadGalleryAsync(id, list, ct);
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
        return Redirect($"/admin/{id}/images");
    }

    [HttpPost("/admin/{id:guid}/images/{imgId:guid}/setcover")]
    public async Task<IActionResult> SetCover(Guid id, Guid imgId, CancellationToken ct)
    {
        await api.SetCoverAsync(id, imgId, ct);
        return Redirect($"/admin/{id}/images");
    }

    [HttpPost("/admin/{id:guid}/images/{imgId:guid}/delete")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imgId, CancellationToken ct)
    {
        await api.DeleteImageAsync(id, imgId, ct);
        return Redirect($"/admin/{id}/images");
    }
}
