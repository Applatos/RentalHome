using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.Extensions;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin;
using Sommerhus.Mvc.ViewModels.Admin.Houses;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HousesController(AdminApiClient api) : AdminControllerBase
{

    [HttpGet("admin")]
    public IActionResult AdminIndex()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/admin/houses")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] EntityStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        SetAdminTab("houses");
        var res = await api.GetHousesAsync(q, status, page, pageSize, ct);

        if (!res.Ok || res.Data is null)
        {
            SetError(res.Message ?? "Could not load house list.");
            return View("~/Views/Admin/Houses/Index.cshtml", new HouseListVm
            {
                Houses = new PageResult<AdminHouseListItemDto> { Items = [], Total = 0, Page = page, PageSize = pageSize, Query = q },
                StatusFilter = status
            });
        }

        return View("~/Views/Admin/Houses/Index.cshtml", new HouseListVm
        {
            Houses = res.Data,
            SearchQuery = q,
            StatusFilter = status
        });
    }

    [HttpGet("/admin/houses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken ct = default)
    {
        SetAdminTab("houses");
        var res = await api.GetHouseAsync(id, ct);
        if (!res.Ok || res.Data is null)
        {
            SetError(res.Message ?? "House not found.");
            return RedirectToAction(nameof(Index));
        }

        var vm = await BuildHouseDetailsVmAsync(res.Data, tab, ct);
        return View("~/Views/Admin/Houses/Details.cshtml", vm);
    }

    private async Task<HouseDetailsVm> BuildHouseDetailsVmAsync(AdminHouseDetailsDto house, string tab, CancellationToken ct)
    {
        var cities = await LoadCitiesSelectListAsync(house.CityId, ct);
        var areas = await LoadAreasSelectListAsync(house.AreaIds, ct);
        var houseGroups = await LoadHouseGroupsSelectListAsync(house.GroupId, ct);
        var availabilityFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var availabilityTo = availabilityFrom.AddMonths(6);

        var allFeatures = Array.Empty<FeatureDto>() as IReadOnlyList<FeatureDto>;
        string? featuresError = null;
        if (string.Equals(tab, "features", StringComparison.OrdinalIgnoreCase))
        {
            var featuresRes = await api.GetFeaturesAsync(ct);
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
            var codesRes = await api.GetSeasonCodesAsync(ct);
            if (codesRes.Ok && codesRes.Data is not null)
            {
                seasonCodes = codesRes.Data;
            }
            else
            {
                seasonCodesError = codesRes.Message ?? "Could not load season codes.";
            }
        }

        var auditEntries = Array.Empty<AuditEntryDto>() as IReadOnlyList<AuditEntryDto>;
        string? auditError = null;
        if (string.Equals(tab, "audit", StringComparison.OrdinalIgnoreCase))
        {
            var auditRes = await api.GetAuditEntriesAsync("VacationHouse", house.Id.ToString(), 1, 50, ct);
            if (auditRes.Ok && auditRes.Data is not null)
            {
                auditEntries = auditRes.Data.Items;
            }
            else
            {
                auditError = auditRes.Message ?? "Could not load audit history.";
            }
        }

        var availableCalendars = Array.Empty<CalendarDto>() as IReadOnlyList<CalendarDto>;
        if (string.Equals(tab, "calendar", StringComparison.OrdinalIgnoreCase))
        {
            var calRes = await api.GetCalendarsAsync(ct);
            if (calRes.Ok && calRes.Data is not null)
                availableCalendars = calRes.Data;
        }

        var availabilityBlocks = Array.Empty<AvailabilityBlockDto>() as IReadOnlyList<AvailabilityBlockDto>;
        string? availabilityError = null;
        if (string.Equals(tab, "availability", StringComparison.OrdinalIgnoreCase))
        {
            var availabilityRes = await api.GetHouseAvailabilityAsync(house.Id, availabilityFrom, availabilityTo, ct);
            if (availabilityRes.Ok && availabilityRes.Data is not null)
            {
                availabilityBlocks = availabilityRes.Data;
            }
            else
            {
                availabilityError = availabilityRes.Message ?? "Could not load availability blocks.";
            }
        }

        var pricingAuditEntries = Array.Empty<AuditEntryDto>() as IReadOnlyList<AuditEntryDto>;
        string? pricingAuditError = null;
        if (string.Equals(tab, "pricing", StringComparison.OrdinalIgnoreCase))
        {
            var planId = house.Pricing?.PlanId.ToString();
            if (!string.IsNullOrEmpty(planId))
            {
                var pricingAuditRes = await api.GetAuditEntriesAsync("PricePlan", planId, 1, 50, ct);
                if (pricingAuditRes.Ok && pricingAuditRes.Data is not null)
                {
                    pricingAuditEntries = pricingAuditRes.Data.Items;
                }
                else
                {
                    pricingAuditError = pricingAuditRes.Message ?? "Could not load price change history.";
                }
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
            SeasonCodesError = seasonCodesError,
            AvailabilityBlocks = availabilityBlocks,
            AvailabilityError = availabilityError,
            AvailabilityFrom = availabilityFrom,
            AvailabilityTo = availabilityTo,
            AuditEntries = auditEntries,
            AuditError = auditError,
            PricingAuditEntries = pricingAuditEntries,
            PricingAuditError = pricingAuditError,
            AvailableCalendars = availableCalendars
        };
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadCitiesSelectListAsync(Guid? selectedCityId, CancellationToken ct)
    {
        var citiesRes = await api.GetCitiesAsync(ct);
        if (citiesRes.Ok && citiesRes.Data is not null)
        {
            return citiesRes.Data.ToSelectList(selectedCityId).ToList();
        }
        return [];
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadAreasSelectListAsync(IEnumerable<Guid>? selectedAreaIds, CancellationToken ct)
    {
        var areasRes = await api.GetAreasLookupAsync(ct);
        if (areasRes.Ok && areasRes.Data is not null)
        {
            return areasRes.Data.ToSelectList(selectedAreaIds?.ToList()).ToList();
        }
        return [];
    }

    private async Task<IReadOnlyList<SelectListItem>> LoadHouseGroupsSelectListAsync(Guid? selectedGroupId, CancellationToken ct)
    {
        var groupsRes = await api.GetHouseGroupListAsync(ct);
        if (groupsRes.Ok && groupsRes.Data is not null)
        {
            var lookups = groupsRes.Data.Select(g => new LookupItem(g.Id, g.Name));
            return lookups.ToSelectList(selectedGroupId).ToList();
        }
        return [];
    }

    [HttpGet("/admin/houses/new")]
    public async Task<IActionResult> New(CancellationToken ct)
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

        var res = await api.PostHouseAsync(vm.House, ct);
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

        var res = await api.PutHouseAsync(id, dto, ct);
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
        var houseRes = await api.GetHouseAsync(id, ct);
        if (!houseRes.Ok || houseRes.Data is null)
        {
            SetError(houseRes.Message ?? "House not found.");
            return RedirectToAction(nameof(Index));
        }

        var read = houseRes.Data;
        var merged = read with
        {
            Title = dto.Title,
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

    [HttpPost("/admin/houses/{id:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromForm] EntityStatus target, CancellationToken ct = default)
    {
        var dto = new ChangeStatusDto { Target = target };
        var res = await api.ChangeHouseStatusAsync(id, dto, ct);
        if (res.Ok)
        {
            SetSuccess($"Status changed to {target}.");
        }
        else
        {
            SetError(res.Message ?? "Could not change status.");
        }

        return RedirectToAction(nameof(Details), new { id, tab = "overview" });
    }

    [HttpPost("/admin/houses/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var res = await api.DeleteHouseAsync(id, ct);
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
}
