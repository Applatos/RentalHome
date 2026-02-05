using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Mvc.Extensions;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin;
using Sommerhus.Mvc.ViewModels.Admin.Houses;
using System.Globalization;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HousesController : AdminControllerBase
{
    private readonly AdminApiClient _api;
    public HousesController(AdminApiClient api) => _api = api;
    [HttpGet("/admin")]
    public IActionResult AdminIndex() => RedirectToAction(nameof(Index));

    [HttpGet("/admin/houses")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var res = await _api.GetHousesAsync(q, page, pageSize, ct);

        if (!res.Ok)
        {
            SetError(res.Message ?? "Could not load house list.");
            return View("~/Views/Admin/Houses/Index.cshtml", new HouseListVm
            {
                Houses = new PageResult<HouseListItemDto> { Items = [], Total = 0, Page = page, PageSize = pageSize, Query = q }
            });
        }

        SetAdminTab("houses");
        return View("~/Views/Admin/Houses/Index.cshtml", new HouseListVm
        {
            Houses = res.Data,
            SearchQuery = q
        });
    }



    [HttpGet("/admin/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        SetAdminTab("houses");

        var res = await _api.GetHouseAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            SetError(res.Message ?? "House not found.");
            return RedirectToAction(nameof(Index));
        }

        var vm = await BuildHouseDetailsVmAsync(res.Data, tab, ct);
        return View("~/Views/Admin/Houses/Details.cshtml", vm);
    }

    private async Task<HouseDetailsVm> BuildHouseDetailsVmAsync(HouseDetailsDto house, string tab, CancellationToken ct)
    {
        var cities = await LoadCitiesSelectListAsync(house.CityId, ct);
        var areas = await LoadAreasSelectListAsync(house.AreaIds, ct);
        var houseGroups = await LoadHouseGroupsSelectListAsync(house.GroupId, ct);

        var allFeatures = Array.Empty<FeatureDto>() as IReadOnlyList<FeatureDto>;
        string? featuresError = null;
        if (string.Equals(tab, "features", StringComparison.OrdinalIgnoreCase))
        {
            var featuresRes = await _api.GetFeaturesAsync(ct);
            if (featuresRes.Ok && featuresRes.Data is not null)
            {
                allFeatures = featuresRes.Data;
            }
            else
            {
                featuresError = featuresRes.Message ?? "Could not load features.";
            }
        }

        var seasonCodes = Array.Empty<SeasonCodeDto>() as IReadOnlyList<SeasonCodeDto>;
        string? seasonCodesError = null;
        if (string.Equals(tab, "pricing", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(tab, "calendar", StringComparison.OrdinalIgnoreCase))
        {
            var codesRes = await _api.GetSeasonCodesAsync(ct);
            if (codesRes.Ok && codesRes.Data is not null)
            {
                seasonCodes = codesRes.Data;
            }
            else
            {
                seasonCodesError = codesRes.Message ?? "Could not load season codes.";
            }
        }

        return new HouseDetailsVm
        {
            House = house,
            Cities = cities,
            Areas = areas,
            HouseGroups = houseGroups,
            ActiveTab = tab,
            AllFeatures = allFeatures,
            FeaturesError = featuresError,
            SeasonCodes = seasonCodes,
            SeasonCodesError = seasonCodesError
        };
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadCitiesSelectListAsync(Guid? selectedCityId, CancellationToken ct)
    {
        var citiesRes = await _api.GetCitiesAsync(ct);
        if (citiesRes.Ok && citiesRes.Data is not null)
        {
            return citiesRes.Data.ToSelectList(selectedCityId).ToList();
        }
        return [];
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadAreasSelectListAsync(IEnumerable<Guid>? selectedAreaIds, CancellationToken ct)
    {
        var areasRes = await _api.GetAreasLookupAsync(ct);
        if (areasRes.Ok && areasRes.Data is not null)
        {
            return areasRes.Data.ToSelectList(selectedAreaIds?.ToList()).ToList();
        }
        return [];
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadHouseGroupsSelectListAsync(Guid? selectedGroupId, CancellationToken ct)
    {
        var groupsRes = await _api.GetHouseGroupsAsync(ct);
        if (groupsRes.Ok && groupsRes.Data is not null)
        {
            return groupsRes.Data.ToSelectList(selectedGroupId).ToList();
        }
        return [];
    }

    [HttpGet("/admin/houses/new")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        SetAdminTab("houses");
        var cities = await LoadCitiesSelectListAsync(null, ct);
        var areas = await LoadAreasSelectListAsync(null, ct);

        var vm = new HouseCreateVm
        {
            House = new UpsertHouseDto(),
            Cities = cities,
            Areas = areas
        };

        return View("~/Views/Admin/Houses/Create.cshtml", vm);
    }

    [HttpPost("/admin/houses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HouseCreateVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            vm.Cities = await LoadCitiesSelectListAsync(vm.House.CityId, ct);
            vm.Areas = await LoadAreasSelectListAsync(vm.House.AreaIds, ct);
            SetError("Invalid fields.");
            return View("~/Views/Admin/Houses/Create.cshtml", vm);
        }

        var res = await _api.PostHouseAsync(vm.House, ct);
        if (res.Ok && res.Data is Guid id)
        {
            SetSuccess("House created.");
            return RedirectToAction(nameof(Details), new { id });
        }
        
        SetError(res.Message ?? "Could not create house.");
        return RedirectToAction(nameof(Create));
    }


    [HttpPost("/admin/houses/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, UpsertHouseDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return await RenderHouseEditAsync(id, dto, ct);
        }

        var res = await _api.PutHouseAsync(id, dto, ct);
        if (res.Ok)
        {
            SetSuccess("House updated.");
            return RedirectToAction(nameof(Details), new { id, tab = "overview" });
        }

        SetError(res.Message ?? "Could not update house.");
        return await RenderHouseEditAsync(id, dto, ct);
    }

    private async Task<ActionResult> RenderHouseEditAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
    {
        var houseRes = await _api.GetHouseAsync(id, ct);
        if (!houseRes.Ok || houseRes.Data is null)
        {
            SetError(houseRes.Message ?? "House not found.");
            return RedirectToAction(nameof(Index));
        }

        var read = houseRes.Data;
        var merged = read with
        {
            Name = dto.Name,
            CityId = dto.CityId,
            Address = dto.Address,
            Description = dto.Description,
            AreaIds = dto.AreaIds?.ToList() ?? []
        };

        var cities = await LoadCitiesSelectListAsync(dto.CityId, ct);
        var areas = await LoadAreasSelectListAsync(merged.AreaIds, ct);
        var houseGroups = await LoadHouseGroupsSelectListAsync(merged.GroupId, ct);

        var selectedAreaLookups = areas
            .Where(o => o.Selected && Guid.TryParse(o.Value, out _))
            .Select(o => new LookupItem(Guid.Parse(o.Value), o.Text))
            .ToList();

        merged = merged with { Areas = selectedAreaLookups };

        var vm = new HouseDetailsVm
        {
            House = merged,
            Cities = cities,
            Areas = areas,
            HouseGroups = houseGroups,
            ActiveTab = "overview"
        };

        SetAdminTab("houses");
        return View("~/Views/Admin/Houses/Details.cshtml", vm);
    }

    [HttpPost("/admin/houses/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await _api.DeleteHouseAsync(id, ct);
        if (res.Ok)
        {
            SetSuccess("House deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete house.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/houses/{id:guid}/images")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadHouseImages(Guid id, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        if (files is null || !files.Any())
        {
            SetError("Please select at least one image.");
            return RedirectToAction(nameof(Details), new { id, tab = "images" });
        }

        var res = await _api.UploadHouseImagesAsync(id, files, ct);
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

    [HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/set-kind")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetHouseImageKind(Guid id, Guid imageId, string kind, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError("Invalid house ID.");
            return RedirectToAction(nameof(Details), new { id, tab = "images" });
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            SetError("Invalid image type.");
            return RedirectToAction(nameof(Details), new { id, tab = "images" });
        }

        var res = await _api.SetHouseImageKindAsync(id, imageId, kind, ct);
        if (res.Ok)
        {
            SetSuccess($"Set to {kind}.");
        }
        else
        {
            SetError(res.Message ?? "Could not update image.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "images" });
    }

    [HttpPost("/admin/houses/{id:guid}/images/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHouseImage(Guid id, Guid imageId, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            SetError("Invalid house ID.");
            return RedirectToAction(nameof(Details), new { id, tab = "images" });
        }

        var res = await _api.DeleteHouseImageAsync(id, imageId, ct);
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





    [HttpPost("/admin/houses/{id:guid}/features")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHouseFeatures(Guid id, CancellationToken ct = default)
    {
        var featuresRes = await _api.GetFeaturesAsync(ct);
        if (!featuresRes.Ok || featuresRes.Data is null)
        {
            SetError(featuresRes.Message ?? "Could not load features.");
            return RedirectToAction(nameof(Details), new { id, tab = "features" });
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
                        errors.Add($"{feature.Name}: enter a whole number.");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, intValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                case "decimal":
                    if (!TryParseDecimal(raw, out var decValue))
                    {
                        errors.Add($"{feature.Name}: enter a number.");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, decValue.ToString(CultureInfo.InvariantCulture)));
                    break;
                default:
                    if (raw.Length > 200)
                    {
                        errors.Add($"{feature.Name}: text too long (max 200 characters).");
                        continue;
                    }
                    values.Add(new PostFeatureValueDto(feature.Id, raw));
                    break;
            }
        }

        if (errors.Count > 0)
        {
            SetError(string.Join(" ", errors));
            return RedirectToAction(nameof(Details), new { id, tab = "features" });
        }

        var res = await _api.UpsertHouseFeaturesAsync(id, values, ct);
        if (res.Ok)
        {
            SetSuccess("Features updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not save features.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "features" });
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

    [HttpPost("/admin/houses/{id:guid}/pricing")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHousePricing(Guid id, [FromForm] HousePricingForm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid fields in price plan.");
            return RedirectToAction(nameof(Details), new { id, tab = "pricing" });
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
            SetSuccess("Prices updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not save prices.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "pricing" });
    }

    [HttpPost("/admin/houses/{houseId:guid}/pricing/rate-plans/{ratePlanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRatePlan(Guid houseId, Guid ratePlanId, CancellationToken ct = default)
    {
        var res = await _api.DeleteHouseRatePlanAsync(houseId, ratePlanId, ct);
        if (res.Ok)
        {
            SetSuccess("Price plan deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete price plan.");
        }
        return RedirectToAction(nameof(Details), new { id = houseId, tab = "pricing" });
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHouseSeasonSpan(Guid houseId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid season span data.");
            return RedirectToAction(nameof(Details), new { id = houseId, tab = "calendar" });
        }

        var res = await _api.AddHouseSeasonSpanAsync(houseId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Season span added.");
        }
        else
        {
            SetError(res.Message ?? "Could not add season span.");
        }

        return RedirectToAction(nameof(Details), new { id = houseId, tab = "calendar" });
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHouseSeasonSpan(Guid houseId, Guid spanId, [FromForm] UpsertSeasonSpanDto dto, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Invalid season span data.");
            return RedirectToAction(nameof(Details), new { id = houseId, tab = "calendar" });
        }

        var res = await _api.UpdateHouseSeasonSpanAsync(houseId, spanId, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Season span updated.");
        }
        else
        {
            SetError(res.Message ?? "Could not update season span.");
        }

        return RedirectToAction(nameof(Details), new { id = houseId, tab = "calendar" });
    }

    [HttpPost("/admin/houses/{houseId:guid}/calendar/{spanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHouseSeasonSpan(Guid houseId, Guid spanId, CancellationToken ct = default)
    {
        var res = await _api.DeleteHouseSeasonSpanAsync(houseId, spanId, ct);
        if (res.Ok)
        {
            SetSuccess("Season span deleted.");
        }
        else
        {
            SetError(res.Message ?? "Could not delete season span.");
        }

        return RedirectToAction(nameof(Details), new { id = houseId, tab = "calendar" });
    }
}
