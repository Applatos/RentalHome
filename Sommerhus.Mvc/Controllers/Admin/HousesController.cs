using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Mvc.Services;

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
    public async Task<IActionResult> House(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "houses";


        var res = await _api.GetHouseAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] = res.Message ?? "Hus ikke fundet.";
            return RedirectToAction(nameof(Houses));
        }
        await PopulateCitiesAsync(res.Data.CityId, ct);

        if (string.Equals(tab, "features", StringComparison.OrdinalIgnoreCase))
        {
            await PopulateFeaturesAsync(ct);
        }
        else
        {
            ViewBag.AllFeatures ??= Array.Empty<FeatureDetailsDto>();
        }
        ViewBag.Tab = tab;
        return View(res.Data);
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
            var citiesRes = await _api.GetCitiesAsync(ct);
            vm.Cities = citiesRes.Data.ToSelectList(vm.House.CityId).ToList();

            TempData["Err"] = "Ugyldige felter.";
            return View("NewHouse", vm);
        }

        var dto = vm.House;
        var res = await _api.PostHouseAsync(dto, ct);
        if (res.Ok && res.Data is Guid id)
        {
            TempData["Ok"] = "Hus oprettet.";
            return RedirectToAction(nameof(House), new { id });
        }
        return RedirectToAction(nameof(NewHouse));
    }


    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, UpsertHouseDto dto, CancellationToken ct = default)
    {
        // Binder nu direkte til write-DTO — konsistent med API (PUT /api/admin/houses/{id})
        if (!ModelState.IsValid)
        {
            // Repopulate cities & show view with user's input merged into the read model
            return await RenderHouseEditAsync(id, dto, ct);
        }

        var res = await _api.PutHouseAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Hus opdateret.";
            return RedirectToAction(nameof(House), new { id, tab = "overview" });
        }

        // API returned failure (could inspect res.Errors/Message)
        TempData["Err"] = res.Message ?? "Kunne ikke opdatere hus.";
        return await RenderHouseEditAsync(id, dto, ct);
    }

    private async Task<ActionResult> RenderHouseEditAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
    {
        // Get latest read-model from API (so we keep Images/Features/CreatedUtc etc.)
        var houseRes = await _api.GetHouseAsync(id, ct);
        if (!houseRes.Ok || houseRes.Data is null)
        {
            TempData["Err"] = houseRes.Message ?? "Hus ikke fundet.";
            return RedirectToAction(nameof(Houses));
        }

        // Merge incoming write DTO values into the read DTO so view displays the user's input
        var read = houseRes.Data;
        var merged = read with
        {
            Name = dto.Name,
            CityId = dto.CityId,
            Address = dto.Address,
            Description = dto.Description,
            AreaId = dto.AreaId
        };

        // Repopulate cities for the dropdown with the selected value from dto
        await PopulateCitiesAsync(dto.CityId, ct);

        ViewBag.Tab = "overview";
        ViewData["AdminTab"] = "houses";
        ViewBag.HouseId = id;
        return View("House", merged);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await _api.DeleteHouseAsync(id, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Hus slettet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke slette hus.";
        }

        return RedirectToAction(nameof(Houses));
    }

    private async Task PopulateCitiesAsync(Guid? selectedCityId, CancellationToken ct)
    {
        var citiesRes = await _api.GetCitiesAsync(ct);
        if (citiesRes.Ok && citiesRes.Data is not null)
        {
            ViewBag.Cities = citiesRes.Data.ToSelectList(selectedCityId);
        }
        else
        {
            ViewBag.Cities = Enumerable.Empty<SelectListItem>();
            if (!citiesRes.Ok && !string.IsNullOrWhiteSpace(citiesRes.Message))
            {
                TempData["Err"] ??= citiesRes.Message;
            }
        }
    }

    public async Task<IActionResult> UploadHouseImages(Guid id, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        if (files is null || !files.Any())
        {
            TempData["Err"] = "Vælg mindst ét billede.";
            return RedirectToAction(nameof(House), new { id, tab = "images" });
        }

        var res = await _api.UploadHouseImagesAsync(id, files, ct);


        if (res.Ok)
        {
            var uploadedCount = res.Data?.Count ?? 0;
            TempData["Ok"] = uploadedCount > 0 ? $"Uploadede {uploadedCount} billede(r)." : "Ingen billeder blev uploadet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Fejl ved upload.";
        }

         return RedirectToAction(nameof(House), new { id, tab = "images" });
    }

    public async Task<IActionResult> SetHouseImageKind(Guid id, Guid ImageId, string kind, CancellationToken ct = default)
    {
        if (id == Guid.Empty) { 
            TempData["Err"] = "Ugyldigt hus-id.";
            return RedirectToAction(nameof(House), new { id, tab = "images" });
        }

        if (string.IsNullOrWhiteSpace(kind)) {             
            TempData["Err"] = "Ugyldig billedetype.";
            return RedirectToAction(nameof(House), new { id, tab = "images" });
        }

        var res = await _api.SetHouseImageKindAsync(id, ImageId, kind, ct);
        if (res.Ok)
        {
            TempData["Ok"] = $"Sat til {kind}.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke opdatere billede.";
        }

        return RedirectToAction(nameof(House), new { id, tab = "images" });
    }

    public async Task<IActionResult> DeleteHouseImage(Guid id,  Guid imageId, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "Ugyldigt hus-id.";
            return RedirectToAction(nameof(House), new { id, tab = "images" });
        }
        var res = await _api.DeleteHouseImageAsync(id, imageId, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Billede slettet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke slette billede.";
        }
        return RedirectToAction(nameof(House), new { id, tab = "images" });
    }





    // ==== Features ====


    private async Task PopulateFeaturesAsync(CancellationToken ct)
    {
        var featuresRes = await _api.GetFeaturesAsync(ct);
        if (featuresRes.Ok && featuresRes.Data is not null)
        {
            ViewBag.AllFeatures = featuresRes.Data;
            ViewBag.FeaturesError = null;
        }
        else
        {
            ViewBag.AllFeatures = Array.Empty<FeatureDetailsDto>();
            ViewBag.FeaturesError = featuresRes.Message ?? "Kunne ikke hente features.";
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHouseFeatures(Guid id, CancellationToken ct = default)
    {
        var featuresRes = await _api.GetFeaturesAsync(ct);
        if (!featuresRes.Ok || featuresRes.Data is null)
        {
            TempData["Err"] = featuresRes.Message ?? "Kunne ikke hente features.";
            return RedirectToAction(nameof(House), new { id, tab = "features" });
        }

        var form = await Request.ReadFormAsync(ct);
        var values = new List<PostFeatureValueDto>();
        var errors = new List<string>();

        foreach (var feature in featuresRes.Data)
        {
            var key = $"feature_{feature.Id}";
            if (!form.TryGetValue(key, out var formValue) || formValue.Count == 0)
            {
                continue;
            }

            var raw = formValue[^1]?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                continue;
            }

            var type = feature.ValueType?.Trim() ?? string.Empty;
            switch (type.ToLowerInvariant())
            {
                case "bool":
                    if (IsTruthy(raw))
                    {
                        values.Add(new PostFeatureValueDto(feature.Id, "true"));
                    }
                    break;
                case "int":
                    if (!TryParseInt(raw, out var intValue))
                    {
                        errors.Add($"{feature.Name}: indtast et helt tal.");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, intValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                case "decimal":
                    if (!TryParseDecimal(raw, out var decValue))
                    {
                        errors.Add($"{feature.Name}: indtast et tal.");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, decValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                default:
                    if (raw.Length > 200)
                    {
                        errors.Add($"{feature.Name}: teksten er for lang (maks 200 tegn).");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, raw));
                    break;
            }
        }

        if (errors.Count > 0)
        {
            TempData["Err"] = string.Join(" ", errors);
            return RedirectToAction(nameof(House), new { id, tab = "features" });
        }

        var res = await _api.UpsertHouseFeaturesAsync(id, values, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Features opdateret.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke gemme features.";
        }

        return RedirectToAction(nameof(House), new { id, tab = "features" });
    }

    private static bool IsTruthy(string value)
        => value.Equals("true", StringComparison.OrdinalIgnoreCase)
           || value.Equals("on", StringComparison.OrdinalIgnoreCase)
           || value.Equals("1", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseInt(string input, out int value)
    {
        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            return true;
        return int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseDecimal(string input, out decimal value)
    {
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            return true;
        return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }


    [HttpGet("api/admin/features")]
    public async Task<IActionResult> Features(CancellationToken ct = default)
    {
        var res = await _api.GetFeaturesAsync(ct);
        if (!res.Ok)
        {
            TempData["Err"] = res.Message ?? "Kunne ikke hente features.";
            return View();
        }
        ViewData["AdminTab"] = "features";
        return View(res.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeature([FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            TempData["Err"] = "Navn, Key og Type er påkrævet.";
            return RedirectToAction(nameof(Features));
        }
        var res = await _api.CreateFeatureAsync(dto, ct);
        if (res.Ok && res.Data is Guid id)
        {
            TempData["Ok"] = $"Feature oprettet (#{id}).";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke oprette feature.";
        }
        return RedirectToAction(nameof(Features));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFeature(Guid id, [FromForm] UpsertFeatureDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.ValueType))
        {
            TempData["Err"] = "Navn, Key og Type er påkrævet.";
            return RedirectToAction(nameof(Features));
        }
        var res = await _api.UpdateFeatureAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Feature opdateret.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke opdatere feature.";
        }
        return RedirectToAction(nameof(Features));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeature(Guid id, CancellationToken ct = default)
    {
        var res = await _api.DeleteFeatureAsync(id, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Feature slettet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke slette feature. er i brug på et sommerhus.";
        }
        return RedirectToAction(nameof(Features));
    }

    [HttpPost("/admin/features/{id:guid}/icon")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFeatureIcon(Guid id, IFormFile file, CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "features";

        if (file is null || file.Length == 0)
        {
            TempData["Err"] = "Vælg en fil eller træk en fil ind i feltet.";
            return RedirectToAction(nameof(Features));
        }

        using var stream = file.OpenReadStream();
        var res = await _api.UploadFeatureIconAsync(id, stream, file.FileName, file.ContentType, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Ikon uploadet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke uploade ikon.";
        }

        return RedirectToAction(nameof(Features));
    }



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



}
