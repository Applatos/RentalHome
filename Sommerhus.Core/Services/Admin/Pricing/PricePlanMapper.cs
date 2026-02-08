using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.Pricing;

public static class PricePlanMapper
{
    public static PricePlanDetailsDto ToDto(PricePlan plan)
    {
        var rates = plan.SeasonPrices
            .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SeasonPriceDto(
                s.Id,
                s.PricePlanId,
                s.Code,
                s.NightlyPrice))
            .ToList();

        return new PricePlanDetailsDto(
            plan.Id,
            plan.HouseId,
            plan.Name,
            plan.Currency,
            plan.IsActive,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            rates);
    }
}
