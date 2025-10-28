using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Mvc.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Sommerhus.Mvc.Controllers.Admin;


public static class SelectListExtensions
{
    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items)
        => items.ToSelectList((IEnumerable<Guid>?)null);

    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items, Guid? selectedId)
        => items.ToSelectList(selectedId.HasValue ? new[] { selectedId.Value } : null);

    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<LookupItem> items, IEnumerable<Guid>? selectedIds)
    {
        var selected = selectedIds is null ? new HashSet<Guid>() : selectedIds.ToHashSet();

            return items.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Label,
                Selected = selected.Contains(i.Id)
            });
    }
}

public sealed class HouseEditVm
{
    public UpsertHouseDto House { get; set; } = new();
    public IEnumerable<SelectListItem> Cities { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Areas { get; set; } = Enumerable.Empty<SelectListItem>();
}

public sealed class AreaEditVm
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> CityIds { get; set; } = new List<Guid>();
    public string? Description { get; set; }
    public IReadOnlyList<ImageDto> Images { get; init; } = Array.Empty<ImageDto>();
    public IReadOnlyList<SelectListItem> Cities { get; init; } = Array.Empty<SelectListItem>();

    public bool IsNew => !Id.HasValue || Id == Guid.Empty;
}

public sealed record AreaGalleryVm(Guid AreaId, IReadOnlyList<ImageDto> Images, string? RedirectTo = null);

public class SeasonRow
{
    public Guid? Id { get; set; }
    [Required] public string Name { get; set; } = "";

    [DataType(DataType.Date)] public DateOnly StartDate { get; set; }
    [DataType(DataType.Date)] public DateOnly EndDate { get; set; }

    [Range(0.01, double.MaxValue)] public decimal NightlyPrice { get; set; }
    [Range(1, 365)] public int? MinStayNights { get; set; }

    // Markeret i UI, filtreres væk på serveren før mapping
    public bool IsDeleted { get; set; }
}

public class HousePricingForm
{
    public Guid? PlanId { get; set; }

    [Required] public string Name { get; set; } = "";
    [Required, StringLength(3)] public string Currency { get; set; } = "DKK";
    public bool IsActive { get; set; }

    public List<SeasonRow> Seasons { get; set; } = new();
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
        await PopulateAreasAsync(res.Data.AreaIds, ct);

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
        var areas = await _api.GetAreasLookupAsync(ct);

        var vm = new HouseEditVm
        {
            House = new UpsertHouseDto(),
            Cities = cities.Data.ToSelectList().ToList(),
            Areas = areas.Data.ToSelectList().ToList()
        };

        if (!cities.Ok)
        {
            TempData["Err"] ??= cities.Message ?? "Kunne ikke hente byer.";
        }

        if (!areas.Ok)
        {
            TempData["Err"] ??= areas.Message ?? "Kunne ikke hente områder.";
        }

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
            vm.Cities = (citiesRes.Data ?? Array.Empty<LookupItem>()).ToSelectList(vm.House.CityId).ToList();

            var areasRes = await _api.GetAreasLookupAsync(ct);
            vm.Areas = (areasRes.Data ?? Array.Empty<LookupItem>()).ToSelectList(vm.House.AreaIds).ToList();

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
            AreaIds = dto.AreaIds?.ToList() ?? new List<Guid>()
        };

        // Repopulate cities for the dropdown with the selected value from dto
        await PopulateCitiesAsync(dto.CityId, ct);
        await PopulateAreasAsync(merged.AreaIds, ct);

        var selectedAreaLookups = new List<LookupItem>();
        if (ViewBag.Areas is IEnumerable<SelectListItem> areaOptions)
        {
            foreach (var option in areaOptions.Where(o => o.Selected))
            {
                if (Guid.TryParse(option.Value, out var Areaid))
                {
                    selectedAreaLookups.Add(new LookupItem(Areaid, option.Text));
                }
            }
        }

        merged = merged with { Areas = selectedAreaLookups };

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

    private async Task PopulateAreasAsync(IEnumerable<Guid>? selectedAreaIds, CancellationToken ct)
    {
        var areasRes = await _api.GetAreasLookupAsync(ct);
        if (areasRes.Ok && areasRes.Data is not null)
        {
            ViewBag.Areas = areasRes.Data.ToSelectList(selectedAreaIds?.ToList());
        }
        else
        {
            ViewBag.Areas = Enumerable.Empty<SelectListItem>();
            if (!areasRes.Ok && !string.IsNullOrWhiteSpace(areasRes.Message))
            {
                TempData["Err"] ??= areasRes.Message;
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
        if (id == Guid.Empty)
        {
            TempData["Err"] = "Ugyldigt hus-id.";
            return RedirectToAction(nameof(House), new { id, tab = "images" });
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
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

    public async Task<IActionResult> DeleteHouseImage(Guid id, Guid imageId, CancellationToken ct = default)
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

    // ===== Areas =====
    [HttpGet("/admin/areas")]
    public async Task<IActionResult> Areas(CancellationToken ct = default)
    {
        var res = await _api.GetAreasAsync(ct);
        ViewData["AdminTab"] = "areas";

        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] ??= res.Message ?? "Kunne ikke hente områder.";
            return View(Array.Empty<AreaListItemDto>());
        }

        return View(res.Data);
    }

    [HttpGet("/admin/areas/{id:guid}")]
    public async Task<IActionResult> Area(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "areas";
        var res = await _api.GetAreaAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] = res.Message ?? "Område ikke fundet.";
            return RedirectToAction(nameof(Areas));
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
    public async Task<IActionResult> NewArea(CancellationToken ct = default)
    {
        var vm = await BuildAreaEditVmAsync(null, Array.Empty<Guid>(), null, null, ct);
        ViewData["AdminTab"] = "areas";
        return View("EditArea", vm);
    }

    [HttpPost("/admin/areas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateArea([FromForm] UpsertAreaDto dto, CancellationToken ct = default)
    {
        var res = await _api.CreateAreaAsync(dto, ct);
        if (res.Ok && res.Data is not null)
        {
            TempData["Ok"] = "Area oprettet.";
            return RedirectToAction(nameof(Area), new { id = res.Data.Id });
        }
        TempData["Err"] = res.Message ?? "Kunne ikke oprette område.";
        var vm = await BuildAreaEditVmAsync(null, dto.CityIds, dto.Name, dto.Description, ct);
        ViewData["AdminTab"] = "areas";
        return View("EditArea", vm);
    }


    [HttpGet("/admin/areas/{id:guid}/edit")]
    public async Task<IActionResult> EditArea(Guid id, CancellationToken ct = default)
    {
        var res = await _api.GetAreaAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            TempData["Err"] = res.Message ?? "Område ikke fundet.";
            return RedirectToAction(nameof(Areas));
        }

        var dto = res.Data;
        var vm = await BuildAreaEditVmAsync(res.Data, dto.CityIds ?? res.Data.CityIds, dto.Name, dto.Description, ct);
        ViewData["AdminTab"] = "areas";
        return View(vm);
    }

    [HttpPost("/admin/areas/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateArea(Guid id, [FromForm] UpsertAreaDto dto, CancellationToken ct = default)
    {
        var res = await _api.UpdateAreaAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Område opdateret.";
            return RedirectToAction(nameof(Area), new { id });
        }
        TempData["Err"] = res.Message ?? "Kunne ikke opdatere område.";
        var areaRes = await _api.GetAreaAsync(id, ct);
        var vm = await BuildAreaEditVmAsync(areaRes.Data, dto.CityIds ?? areaRes.Data?.CityIds, dto.Name, dto.Description, ct); 
        ViewData["AdminTab"] = "areas";
        return View("EditArea", vm);
    }

    [HttpPost("/admin/areas/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteArea(Guid id, CancellationToken ct = default)
    {
        var res = await _api.DeleteAreaAsync(id, ct);
        TempData[res.Ok ? "Ok" : "Err"] = res.Ok
            ? "Area slettet."
            : res.Message ?? "Kunne ikke slette area.";

        return RedirectToAction(nameof(Areas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAreaImage(Guid id, IFormFile? file, string? redirectTo, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "Ugyldigt område.";
            return RedirectAfterAreaImageChange(id, redirectTo);
        }

        if (file is null || file.Length == 0)
        {
            TempData["Err"] = "Vælg et billede.";
            return RedirectAfterAreaImageChange(id, redirectTo);
        }

        var res = await _api.UploadAreaImageAsync(id, file, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Billede uploadet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke uploade billede.";
        }

        return RedirectAfterAreaImageChange(id, redirectTo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAreaImage(Guid id, Guid imageId, string? redirectTo, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            TempData["Err"] = "Ugyldigt område.";
            return RedirectAfterAreaImageChange(id, redirectTo);
        }

        var res = await _api.DeleteAreaImageAsync(id, imageId, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Billede slettet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke slette billede.";
        }

        return RedirectAfterAreaImageChange(id, redirectTo);
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

    private IActionResult RedirectAfterAreaImageChange(Guid id, string? redirectTo)
    {
        if (string.Equals(redirectTo, "edit", StringComparison.OrdinalIgnoreCase))
        {
            return id == Guid.Empty
                ? RedirectToAction(nameof(Areas))
                : RedirectToAction(nameof(EditArea), new { id });
        }

        return id == Guid.Empty
            ? RedirectToAction(nameof(Areas))
            : RedirectToAction(nameof(Area), new { id, tab = "images" });
    }


    private async Task<IReadOnlyList<SelectListItem>> LoadCityOptionsAsync(IReadOnlyCollection<Guid>? selectedCityIds, CancellationToken ct)
    {
        var citiesRes = await _api.GetCitiesAsync(ct);
        if (citiesRes.Ok && citiesRes.Data is not null)
        {
            return citiesRes.Data.ToSelectList(selectedCityIds).ToList();
        }

        TempData["Err"] ??= citiesRes.Message ?? "Kunne ikke hente byer.";
        return new List<SelectListItem>();
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHousePricing(Guid id, [FromForm] HousePricingForm form, CancellationToken ct = default)
    {
        var validSeasons = form.Seasons
            .Where(s => !s.IsDeleted)
            .Select(s => new UpsertRateSeasonDto(s.Id, s.Name, s.StartDate, s.EndDate, s.NightlyPrice, s.MinStayNights))
            .ToList();

        var dto = new UpsertRatePlanDto(form.PlanId, form.Name, form.Currency, form.IsActive, validSeasons);

        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige felter i prisplan.";
            return RedirectToAction(nameof(House), new { id, tab = "pricing" });
        }

        var res = await _api.PutHousePricingAsync(id, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Priser opdateret.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke gemme priser.";
        }

        return RedirectToAction(nameof(House), new { id, tab = "pricing" });
    }

    [HttpGet]
    public IActionResult NewSeasonRow(string currency = "DKK")
    {
        var idx = Guid.NewGuid().ToString("N"); // unikt token
        ViewData["Index"] = idx;
        ViewData["Currency"] = (currency ?? "DKK").ToUpperInvariant();
        return PartialView("_SeasonRow", model: null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRatePlan(Guid houseId, Guid ratePlanId, CancellationToken ct = default)
    {
        var res = await _api.DeleteHouseRatePlanAsync(houseId, ratePlanId, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Prisplan slettet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke slette prisplan.";
        }
        return RedirectToAction(nameof(House), new { id = houseId, tab = "pricing" });
    }

}