using Microsoft.AspNetCore.Mvc;
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

    // ---------- CREATE/EDIT/DELETE (uændret) ----------
    public record CreateFormVM(string Title, string? Subtitle, string? Address, string? City, string? Zip, string? Description, string? Facilities);

    [HttpGet("/admin/create")]
    public IActionResult Create() => View(new CreateFormVM("", "", "", "", "", "", ""));

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/create")]
    public async Task<IActionResult> CreatePost(CreateFormVM form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            ModelState.AddModelError(nameof(form.Title), "Titel er påkrævet");
            return View("Create", form);
        }
        var id = await api.CreateHouseAsync(form, ct);
        TempData["ok"] = "Hus oprettet";
        return Redirect($"/admin/{id}/images");
    }

    [HttpGet("/admin/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var h = await api.GetHouseAsync(id, ct);
        if (h is null) return NotFound();
        var form = new CreateFormVM(h.Title, h.Subtitle, h.Address, h.City, h.Zip, h.Description, h.Facilities);
        ViewBag.HouseId = id;
        return View(form);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/{id:guid}/edit")]
    public async Task<IActionResult> EditPost(Guid id, CreateFormVM form, CancellationToken ct)
    {
        await api.UpdateHouseAsync(id, form, ct);
        TempData["ok"] = "Gemte ændringer";
        return Redirect($"/admin/{id}/images");
    }

    [HttpGet("/admin/{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var h = await api.GetHouseAsync(id, ct);
        if (h is null) return NotFound();
        return View(h);
    }

    [ValidateAntiForgeryToken]
    [HttpPost("/admin/{id:guid}/delete")]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct)
    {
        await api.DeleteHouseAsync(id, ct);
        TempData["ok"] = "Hus slettet";
        return Redirect("/admin");
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
