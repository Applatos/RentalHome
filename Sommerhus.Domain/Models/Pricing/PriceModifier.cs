using System;

namespace Sommerhus.Domain.Models.Pricing;

public class PriceModifier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RatePlanId { get; set; }

    public string Name { get; set; } = "Modifier";

    public PriceScope Scope { get; set; } = PriceScope.PerBooking;
    public AdjustmentKind Kind { get; set; } = AdjustmentKind.Absolute;
    public decimal Value { get; set; }

    public ModifierTrigger Trigger { get; set; } = ModifierTrigger.Always;
    public int? ThresholdNights { get; set; }

    public bool IsActive { get; set; } = true;

    public PricePlan? RatePlan { get; set; }
}
