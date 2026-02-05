using Microsoft.AspNetCore.Mvc;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin;

namespace Sommerhus.Mvc.Controllers.Admin;

public class PricesController : AdminControllerBase
{
    private readonly AdminApiClient _api;
    public PricesController(AdminApiClient api) => _api = api;


    [HttpGet("/admin/prices")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetAdminTab("prices");
        var vm = await BuildPricingVmAsync(null, null, ct);
        return View("~/Views/Admin/Prices/Index.cshtml", vm);
    }


    [HttpPost("/admin/prices/season-codes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSeasonCode([FromForm][Bind(Prefix = "SeasonCodeForm")] CreateSeasonCodeForm form, CancellationToken ct = default)
    {
        SetAdminTab("prices");

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPricingVmAsync(null, form, ct);
            return View("Index", invalidVm);
        }

        var dto = new SeasonCodeDto(form.Code, form.Label, form.Color, form.SortOrder);
        var res = await _api.CreateSeasonCodeAsync(dto, ct);

        if (res.Ok)
        {
            SetSuccess("Season code created.");
            return RedirectToAction(nameof(Index));
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
            ModelState.AddModelError("SeasonCodeForm.Code", res.Message ?? "Could not create season code.");
        }

        var vm = await BuildPricingVmAsync(null, form, ct);
        return View("~/Views/Admin/Prices/Index.cshtml", vm);
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
            GroupError = groupsRes.Ok ? null : groupsRes.Message ?? "Could not load groups.",
            SeasonError = seasonCodesRes.Ok ? null : seasonCodesRes.Message ?? "Could not load season codes."
        };

        if (codeForm is null && vm.SeasonCodes.Count > 0 && vm.SeasonCodeForm.SortOrder == 0)
        {
            vm.SeasonCodeForm.SortOrder = vm.SeasonCodes.Max(c => c.SortOrder) + 1;
        }

        return vm;
    }

}