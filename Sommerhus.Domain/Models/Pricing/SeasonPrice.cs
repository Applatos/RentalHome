using System;

namespace Sommerhus.Domain.Models.Pricing;

public class SeasonPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = "A";

    public Guid PricePlanId { get; set; }
    public PricePlan? PricePlan { get; set; }

    public decimal NightlyPrice { get; set; }
}
