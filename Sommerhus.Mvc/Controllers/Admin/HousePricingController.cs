using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;
using Sommerhus.Mvc.ViewModels.Admin.Houses;

namespace Sommerhus.Mvc.Controllers.Admin;

public sealed class HousePricingController(AdminApiClient api, IStringLocalizer<SharedResource> localizer) : AdminControllerBase
{
    [HttpPost("/admin/houses/{id:guid}/pricing")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHousePricing(Guid id, [FromForm] HousePricingForm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            var messages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct();
            SetError(string.Join(" ", messages.Prepend(localizer["Pages.AdminHouse.Pricing.NotSaved"].Value)));
            return RedirectToDetails(id, "pricing");
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

        var res = await api.PutHousePricingAsync(id, dto, ct);
        if (res.Ok)
        {
            SetSuccess("Prices updated.");
        }
        else
        {
            // The API names what is wrong (a price of zero, a duplicate season code); say so rather than only that it failed.
            SetError($"{localizer["Pages.AdminHouse.Pricing.NotSaved"]} {ApiErrorText.Describe(res, string.Empty)}".TrimEnd());
        }

        return RedirectToDetails(id, "pricing");
    }

    [HttpPost("/admin/houses/{houseId:guid}/pricing/rate-plans/{ratePlanId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRatePlan(Guid houseId, Guid ratePlanId, CancellationToken ct = default)
    {
        var res = await api.DeleteHouseRatePlanAsync(houseId, ratePlanId, ct);
        if (res.Ok)
        {
            SetSuccess("Price plan deleted.");
        }
        else
        {
            SetError(ApiErrorText.Describe(res, "Could not delete price plan."));
        }
        return RedirectToDetails(houseId, "pricing");
    }
}
