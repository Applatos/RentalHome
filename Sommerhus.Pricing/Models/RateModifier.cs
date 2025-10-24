using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

// RateModifier.cs
public class RateModifier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RatePlanId { get; set; }

    public string Name { get; set; } = "Modifier";

    public PriceScope Scope { get; set; } = PriceScope.PerBooking; // fx rengøring = per booking
    public AdjustmentKind Kind { get; set; } = AdjustmentKind.Absolute;
    public decimal Value { get; set; }                             // Absolute: kr, Percent: 0.10m = 10%

    public ModifierTrigger Trigger { get; set; } = ModifierTrigger.Always;
    public int? ThresholdNights { get; set; } // bruges når Trigger=MinNights

    public bool IsActive { get; set; } = true;

    public RatePlan? RatePlan { get; set; }
}
