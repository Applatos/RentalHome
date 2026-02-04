using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Mvc.Extensions;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin;
using System.Globalization;

namespace Sommerhus.Mvc.Controllers.Admin;

[Authorize]
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
        await PopulateHouseGroupsAsync(res.Data.GroupId, ct);

        if (string.Equals(tab, "features", StringComparison.OrdinalIgnoreCase))
        {
            await PopulateFeaturesAsync(ct);
        }
        else
        {
            ViewBag.AllFeatures ??= Array.Empty<FeatureDto>();
        }

        if (string.Equals(tab, "pricing") || string.Equals(tab, "calendar"))
        {
            await PopulateSeasonCodesAsync(ct);
        }
        else
        {
            ViewBag.SeasonCodes ??= Array.Empty<SeasonCodeDto>();
            ViewBag.SeasonCodesError ??= null;
        }


        ViewBag.Tab = tab;
        return View(res.Data);
    }

    private async Task PopulateSeasonCodesAsync(CancellationToken ct)
    {
        var codesRes = await _api.GetSeasonCodesAsync(ct);
        if (codesRes.Ok && codesRes.Data is not null)
        {
            ViewBag.SeasonCodes = codesRes.Data;
            ViewBag.SeasonCodesError = null;
        }
        else
        {
            ViewBag.SeasonCodes = Array.Empty<SeasonCodeDto>();
            ViewBag.SeasonCodesError = codesRes.Message ?? "Kunne ikke hente sæsonkoder.";
        }
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
        await PopulateHouseGroupsAsync(merged.GroupId, ct);

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

    private async Task PopulateHouseGroupsAsync(Guid? selectedGroupId, CancellationToken ct)
    {
        var groupsRes = await _api.GetHouseGroupsAsync(ct);
        if (groupsRes.Ok && groupsRes.Data is not null)
        {
            ViewBag.HouseGroups = groupsRes.Data.ToSelectList(selectedGroupId);
        }
        else
        {
            ViewBag.HouseGroups = Enumerable.Empty<SelectListItem>();
            if (!groupsRes.Ok && !string.IsNullOrWhiteSpace(groupsRes.Message))
            {
                TempData["Err"] ??= groupsRes.Message;
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
            ViewBag.AllFeatures = Array.Empty<FeatureDto>();
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

    // ===== Pricing =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHousePricing(Guid id, [FromForm] HousePricingForm form, CancellationToken ct = default)
    {

        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige felter i prisplan.";
            return RedirectToAction(nameof(House), new { id, tab = "pricing" });
        }

        var trimmedCurrency = (form.Currency ?? "DKK").Trim().ToUpperInvariant();
        var planName = string.IsNullOrWhiteSpace(form.Name) ? "Standard" : form.Name.Trim();

        var priceRows = form.SeasonPrices
            .Where(p => !string.IsNullOrWhiteSpace(p.Code) && p.NightlyPrice is not null)
            .Select(p => new SeasonPriceDto(
                p.Id ?? Guid.Empty,
                p.RatePlanId ?? form.PlanId ?? Guid.Empty,
                p.Code.Trim().ToUpperInvariant(),
                p.NightlyPrice!.Value))
            .ToList();

        var dto = new PricePlanDetailsDto(
            form.PlanId ?? Guid.Empty,
            id,
            planName,
            trimmedCurrency,
            form.IsActive,
            DateTime.UtcNow,
            DateTime.UtcNow,
            priceRows);


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


    // ===== Pricing setup =====
    [HttpGet("/admin/pricing")]
    public async Task<IActionResult> Pricing(CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "pricing";
        var vm = await BuildPricingVmAsync(null, null, ct);
        return View(vm);
    }

    [HttpPost("/admin/pricing/groups")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHouseGroup([FromForm][Bind(Prefix = "GroupForm")] CreateHouseGroupForm form, CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "pricing";

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPricingVmAsync(form, null, ct);
            return View("Pricing", invalidVm);
        }

        var res = await _api.CreateHouseGroupAsync(new HouseGroupDto(Guid.NewGuid(), form.Name), ct);

        if (res.Ok)
        {
            TempData["Ok"] = "Gruppe oprettet.";
            return RedirectToAction(nameof(Pricing));
        }

        if (res.Errors is { Count: > 0 })
        {
            foreach (var (key, errors) in res.Errors)
            {
                var targetKey = string.IsNullOrWhiteSpace(key) ? "GroupForm.Name" : $"GroupForm.{key}";
                foreach (var error in errors)
                {
                    ModelState.AddModelError(targetKey, error);
                }
            }
        }
        else
        {
            ModelState.AddModelError("GroupForm.Name", res.Message ?? "Kunne ikke oprette gruppe.");
        }

        var vm = await BuildPricingVmAsync(form, null, ct);
        return View("Pricing", vm);
    }

    [HttpPost("/admin/pricing/season-codes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSeasonCode([FromForm][Bind(Prefix = "SeasonCodeForm")] CreateSeasonCodeForm form, CancellationToken ct = default)
    {
        ViewData["AdminTab"] = "pricing";

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPricingVmAsync(null, form, ct);
            return View("Pricing", invalidVm);
        }

        var dto = new SeasonCodeDto(form.Code, form.Label, form.Color, form.SortOrder);
        var res = await _api.CreateSeasonCodeAsync(dto, ct);

        if (res.Ok)
        {
            TempData["Ok"] = "Sæsonkode oprettet.";
            return RedirectToAction(nameof(Pricing));
        }

        if (res.Errors is { Count: > 0 })
        {
            foreach (var (key, errors) in res.Errors)
            {
                var targetKey = string.IsNullOrWhiteSpace(key) ? "SeasonCodeForm.Code" : $"SeasonCodeForm.{key}";
                foreach (var error in errors)
                {
                    ModelState.AddModelError(targetKey, error);
                }
            }
        }
        else
        {
            ModelState.AddModelError("SeasonCodeForm.Code", res.Message ?? "Kunne ikke oprette sæsonkode.");
        }

        var vm = await BuildPricingVmAsync(null, form, ct);
        return View("Pricing", vm);
    }

    // House season span management
    [HttpPost("/admin/houses/{houseId:guid}/calendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHouseSeasonSpan(Guid houseId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige data for sæsonperiode.";
            return RedirectToAction(nameof(House), new { id = houseId, tab = "pricing" });
        }

        var res = await _api.AddHouseSeasonSpanAsync(houseId, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Sæsonperiode tilføjet.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke tilføje sæsonperiode.";
        }

        return RedirectToAction(nameof(House), new { id = houseId, tab = "pricing" });
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHouseSeasonSpan(Guid houseId, Guid spanId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Err"] = "Ugyldige data for sæsonperiode.";
            return RedirectToAction(nameof(House), new { id = houseId, tab = "pricing" });
        }

        var res = await _api.UpdateHouseSeasonSpanAsync(houseId, spanId, dto, ct);
        if (res.Ok)
        {
            TempData["Ok"] = "Sæsonperiode opdateret.";
        }
        else
        {
            TempData["Err"] = res.Message ?? "Kunne ikke opdatere sæsonperiode.";
        }

        return RedirectToAction(nameof(House), new { id = houseId, tab = "pricing" });
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHouseSeasonSpan(Guid houseId, Guid spanId, CancellationToken ct = default)
    {
        var res = await _api.DeleteHouseSeasonSpanAsync(houseId, spanId, ct);
        TempData[res.Ok ? "Ok" : "Err"] = res.Ok
            ? "Sæsonperiode slettet."
            : res.Message ?? "Kunne ikke slette sæsonperiode.";

        return RedirectToAction(nameof(House), new { id = houseId, tab = "pricing" });
    }

    private async Task<PricingAdminVm> BuildPricingVmAsync(
        CreateHouseGroupForm? groupForm,
        CreateSeasonCodeForm? codeForm,
        CancellationToken ct)
    {
        var groupsRes = await _api.GetHouseGroupsAsync(ct);
        var seasonCodesRes = await _api.GetSeasonCodesAsync(ct);

        var vm = new PricingAdminVm
        {
            Groups = groupsRes.Data ?? Array.Empty<LookupItem>(),
            SeasonCodes = seasonCodesRes.Data ?? Array.Empty<SeasonCodeDto>(),
            GroupForm = groupForm ?? new CreateHouseGroupForm(),
            SeasonCodeForm = codeForm ?? new CreateSeasonCodeForm(),
            GroupError = groupsRes.Ok ? null : groupsRes.Message ?? "Kunne ikke hente grupper.",
            SeasonError = seasonCodesRes.Ok ? null : seasonCodesRes.Message ?? "Kunne ikke hente sæsonkoder."
        };

        if (codeForm is null && vm.SeasonCodes.Count > 0 && vm.SeasonCodeForm.SortOrder == 0)
        {
            vm.SeasonCodeForm.SortOrder = vm.SeasonCodes.Max(c => c.SortOrder) + 1;
        }

        return vm;
    }


}
