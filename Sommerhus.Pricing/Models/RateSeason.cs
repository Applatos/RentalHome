using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

// RateSeason.cs
public class RateSeason
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // En sæson hører til en rateplan
    public Guid RatePlanId { get; set; }

    public string Name { get; set; } = "Sæson";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }            // inklusiv
    public decimal NightlyPrice { get; set; }        // DKK, 2 decimaler

    // Valgfrit i v1, nyttigt i v2:
    public int? MinStayNights { get; set; }          // fx 3 i højsæson

    // Navigation property
    public RatePlan? RatePlan { get; set; }
}
