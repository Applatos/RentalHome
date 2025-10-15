using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Mvc.Services;
using AdmAreas = Sommerhus.Contracts.Dtos.Admin.Areas;
using AdmFeats = Sommerhus.Contracts.Dtos.Admin.Features;
using AdmHouses = Sommerhus.Contracts.Dtos.Admin.Houses;

namespace Sommerhus.Mvc.Controllers.Admin;


public static class SelectListExtensions
{
    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items, Guid? selectedId = null)
    {
        return items.Select(i => new SelectListItem
        {
            Value = i.Id.ToString(),
            Text = i.Label,
            Selected = i.Id == selectedId
        });
    }
}

public sealed class HouseEditVm
{
    public UpsertHouseDto House { get; set; } = new();
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
}

public sealed class AdminController : Controller
{
    private readonly AdminApiClient _api;
    public AdminController(AdminApiClient api) => _api = api;
    // ======= HOUSES ======

    [HttpGet("/admin")]
    public IActionResult Index() => RedirectToAction(nameof(Houses));


    // HOUSES (master + pagination)
    [HttpGet("/admin/houses")]
    public async Task<IActionResult> Houses([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var res = await _api.GetHousesAsync(q, page, pageSize, ct);

        if (!res.Ok)
        {
            TempData["Err"] = res.Message ?? "could not find house list";
            return View();
        }

        ViewData["AdminTab"] = "houses";
        return View(res.Data);
    }



    [HttpGet("api/admin/houses/{id:guid}")]
    public async Task<IActionResult> House(Guid id, CancellationToken ct)
    {
        var res = await _api.GetHouseAsync(id, ct);
        if (res.Data is null) return NotFound();
        ViewData["AdminTab"] = "houses";
        return View(res.Data);
    }



     [HttpGet]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        var res = await _api.GetHouseAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] = res.Message ?? "Hus ikke fundet.";
            return RedirectToAction(nameof(Houses));
        }
        var cities = await _api.GetCitiesAsync(ct);
        ViewBag.Cities = cities.Data.ToSelectList(res.Data.CityId);
        ViewBag.Tab = tab;
        return View("House", res.Data);
    }


    // ========== Opret nyt hus ==========
    public async Task<IActionResult> NewHouse(CancellationToken ct)
    {
        var cities = await _api.GetCitiesAsync(ct);

        var vm = new HouseEditVm
        {
            House = new UpsertHouseDto(), // evt. defaults
            Cities = cities.Data.ToSelectList().ToList()
        };
        ViewData["AdminTab"] = "houses";
        return View(vm);
    }


    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(HouseEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige felter.";
            return View("NewHouse", vm);
        }

        var dto = vm.House;
        var res = await _api.PostHouseAsync(dto, ct);
        if (res.Ok && res.Data is Guid id)
        {
            TempData["Ok"] = "Hus oprettet.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(NewHouse));
    }

    // ========== Edit eksisterende hus ==========
    [HttpGet("api/admin/houses/{id:Guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var res = await _api.GetHouseAsync(id, ct);
        if (res.Ok!)
        {
            TempData["Err"] = res.Message ?? "hus kunne ikke findes";
            return RedirectToAction(nameof(Index));
        }
        ViewData["AdminTab"] = "houses";
        var h = res.Data;
        var cities = await _api.GetCitiesAsync(ct);

        var dto = new UpsertHouseDto
        {
            Name = h!.Name,
            CityId = h.CityId,
            Description = h.Description,
            Address = h.Address,
            AreaId = h.AreaId
        };

        var vm = new HouseEditVm
        {
            House = dto,
            Cities = cities.Data.ToSelectList(res.Data!.CityId)
        };
        ViewBag.HouseId = h.Id;
        return View(dto);
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, HouseDetailsDto dto, CancellationToken ct = default)
    {
        // Map HouseDetailsDto to UpsertHouseDto (or use a mapper)
        var updateDto = new UpsertHouseDto
        {
            Name = dto.Name,
            CityId = dto.CityId,
            Address = dto.Address ?? "",
            Description = dto.Description ?? "",
            AreaId = dto.AreaId
        };

        var res = await _api.PutHouseAsync(id, updateDto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Hus opdateret.";
            return RedirectToAction(nameof(Details), new { id, tab = "overview" });
        }

        TempData["Err"] = res.Message ?? "Kunne ikke opdatere hus.";
        // Re-populate cities for the dropdown
        var cities = await _api.GetCitiesAsync(ct);
        ViewBag.Cities = cities.Data.ToSelectList(dto.CityId);
        ViewBag.Tab = "overview";
        return View("House", dto);
    }




    //// POST: /admin/houses/{id}
    //[HttpPost("/admin/houses/{id:guid}")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> SaveHouse(Guid id, [FromForm] UpdateHouseDto dto, CancellationToken ct)
    //{
    //    if (!ModelState.IsValid)
    //    {
    //        TempData["Err"] = "Ugyldige felter.";
    //        return RedirectToAction(nameof(House), new { id });
    //    }

    //    var ok = await _api.UpdateHouseAsync(id, dto, ct);
    //    TempData[ok ? "Ok" : "Err"] = ok ? "Gemt." : "Kunne ikke gemme.";
    //    return RedirectToAction(nameof(House), new { id });
    //}

    //// POST: /admin/houses/{id}/images/upload
    //[HttpPost("/admin/houses/{id:guid}/images/upload")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> UploadHouseImages(Guid id, List<IFormFile> files, CancellationToken ct)
    //{
    //    if (files is null || files.Count == 0)
    //    {
    //        TempData["Err"] = "Vælg mindst ét billede.";
    //        return RedirectToAction(nameof(House), new { id, tab = "images" });
    //    }

    //    var (ok, imgs) = await _api.UploadHouseImagesAsync(id, files, ct);
    //    TempData[ok ? "Ok" : "Err"] = ok ? $"Uploadede {imgs?.Count ?? 0} billede(r)." : "Fejl ved upload.";
    //    return RedirectToAction(nameof(House), new { id, tab = "images" });
    //}

    //// POST: /admin/houses/{id}/images/{imageId}/set-kind
    //[HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/set-kind")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> SetHouseImageKind(Guid id, Guid imageId, [FromForm] string kind, CancellationToken ct)
    //{
    //    var ok = await _api.SetHouseImageKindAsync(id, imageId, kind, ct);
    //    TempData[ok ? "Ok" : "Err"] = ok ? $"Sat til {kind}." : "Kunne ikke opdatere billede.";
    //    return RedirectToAction(nameof(House), new { id, tab = "images" });
    //}

    //// AREAS
    //[HttpGet("/admin/areas")]
    //public async Task<IActionResult> Areas(CancellationToken ct)
    //{
    //    var list = await _api.GetAreasAsync(ct);
    //    ViewData["AdminTab"] = "areas";
    //    return View(list);
    //}

    //[HttpGet("/admin/areas/{id:guid}")]
    //public async Task<IActionResult> Area(Guid id, CancellationToken ct)
    //{
    //    var dto = await _api.GetAreaAsync(id, ct);
    //    if (dto is null) return NotFound();
    //    ViewData["AdminTab"] = "Areas";
    //    return View(dto);
    //}

    //// AREAS – Opret (GET) til ny form
    //[HttpGet("/admin/areas/new")]
    //public IActionResult NewArea()
    //{
    //    // Tom formular til opret
    //    return View("EditArea", new AdmAreas.AreaDetailsDto(
    //        Id: Guid.Empty,
    //        Name: string.Empty,
    //        CityId: null,
    //        CityName: null,
    //        Description: null,
    //        Images: new List<ImageDto>()
    //    ));
    //}

    //// AREAS – Opret (POST)
    //[ValidateAntiForgeryToken]
    //[HttpPost("/admin/areas")]
    //public async Task<IActionResult> CreateArea([FromForm] AdmAreas.UpsertAreaDto dto, CancellationToken ct = default)
    //{
    //    var result = await _api.CreateAreaAsync(dto, ct);

    //    if (result.Ok && result.Payload is not null)
    //    {
    //        TempData["Ok"] = "Area oprettet.";
    //        return RedirectToAction(nameof(Area), new { id = result.Payload.Id });
    //    }

    //    TempData["Err"] = result.ErrorMessage ?? "Kunne ikke oprette area.";
    //    return View("EditArea", dto);
    //}

    //// AREAS – Rediger (GET)
    //[HttpGet("/admin/areas/{id:guid}/edit")]
    //public async Task<IActionResult> EditArea(Guid id, CancellationToken ct = default)
    //{
    //    var area = await _api.GetAreaAsync(id, ct);
    //    if (area is null)
    //    {
    //        TempData["Err"] = "Area ikke fundet.";
    //        return RedirectToAction(nameof(Areas));
    //    }
    //    return View("EditArea", area);
    //}

    //// AREAS – Rediger (POST)
    //[ValidateAntiForgeryToken]
    //[HttpPost("/admin/areas/{id:guid}")]
    //public async Task<IActionResult> UpdateArea(
    //    Guid id,
    //    [FromForm] string Name,
    //    [FromForm] Guid? CityId,
    //    [FromForm] string? Description,
    //    CancellationToken ct = default)
    //{
    //    var dto = new AdmAreas.UpsertHouseDto(Name, CityId, Description, null);
    //    var res = await _api.UpdateAreaAsync(id, dto, ct);

    //    if (res.Ok)
    //    {
    //        TempData["Ok"] = "Area opdateret.";
    //        return RedirectToAction(nameof(Area), new { id });
    //    }

    //    TempData["Err"] = res.ErrorMessage ?? "Kunne ikke opdatere area.";
    //    // Hent aktuel for at vise formular igen med data
    //    var area = await _api.GetAreaAsync(id, ct);
    //    if (area is null)
    //        return RedirectToAction(nameof(Areas));

    //    // Merge de seneste indtastninger
    //    area = area with { Name = Name ?? area.Name, CityId = CityId, Description = Description ?? area.Description };
    //    return View("AreaEdit", area);
    //}

    //// AREAS – Slet (POST)
    //[ValidateAntiForgeryToken]
    //[HttpPost("/admin/areas/{id:guid}/delete")]
    //public async Task<IActionResult> DeleteArea(Guid id, CancellationToken ct = default)
    //{
    //    var res = await _api.DeleteAreaAsync(id, ct);
    //    if (res.Ok)
    //        TempData["Ok"] = "Area slettet.";
    //    else
    //        TempData["Err"] = res.ErrorMessage ?? "Kunne ikke slette area (tjek om den er i brug).";

    //    return RedirectToAction(nameof(Areas));
    //}


    //// FEATURES (master)
    //[HttpGet("/admin/features")]
    //public async Task<IActionResult> Features(CancellationToken ct)
    //{
    //    var list = await _api.GetFeaturesAsync(ct);
    //    ViewData["AdminTab"] = "features";
    //    return View(list);
    //}

    //// ===== CREATE FEATURE =====
    //[HttpPost("/admin/features/create")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> CreateFeature([FromForm] AdmFeats.UpsertFeatureDto dto, CancellationToken ct)
    //{
    //    // Simpel server-validering (kræver mindst Name, Key, ValueType)
    //    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
    //    {
    //        TempData["Err"] = "Navn, Key og Type er påkrævet.";
    //        return RedirectToAction(nameof(Features));
    //    }

    //    var (ok, id) = await _api.CreateFeatureAsync(dto, ct);
    //    TempData[ok ? "Ok" : "Err"] = ok ? $"Feature oprettet (#{id})." : "Kunne ikke oprette feature.";
    //    return RedirectToAction(nameof(Features));
    //}

    //// ===== DELETE FEATURE =====
    //[HttpPost("/admin/features/{id:guid}/delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteFeature(Guid id, CancellationToken ct)
    //{
    //    var ok = await _api.DeleteFeatureAsync(id, ct);
    //    TempData[ok ? "Ok" : "Err"] = ok ? "Feature slettet." : "Kunne ikke slette feature. er i brug på et sommerhus.";
    //    return RedirectToAction(nameof(Features));
    //}

    //[HttpPost("/admin/features/{id:guid}/icon")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> UploadFeatureIcon(Guid id, IFormFile file, CancellationToken ct)
    //{
    //    if (file is null || file.Length == 0)
    //    {
    //        TempData["Err"] = "Vælg en fil eller træk en fil ind i feltet.";
    //        return RedirectToAction(nameof(Features));
    //    }

    //    await using var s = file.OpenReadStream();
    //    var ok = await _api.UploadFeatureIconAsync(id, s, file.FileName, file.ContentType, ct);
    //    TempData[ok ? "Ok" : "Err"] = ok ? "Ikon uploadet." : "Kunne ikke uploade ikon.";
    //    return RedirectToAction(nameof(Features));
    //}

}
