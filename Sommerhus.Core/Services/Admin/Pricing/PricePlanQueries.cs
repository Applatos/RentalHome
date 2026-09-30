using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.Pricing;

public static class PricePlanQueries
{
    /// <summary>
    /// A house's plans, the one the admin form edits first: the active plan, otherwise the most
    /// recently changed one. An inactive plan is still returned, so the form never loses it.
    /// </summary>
    public static IQueryable<PricePlan> ForEditing(this IQueryable<PricePlan> plans, Guid houseId)
        => plans
            .Where(p => p.HouseId == houseId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc);
}
