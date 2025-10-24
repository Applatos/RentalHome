using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

public class RatePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // En plan hører til et sommerhus
    public Guid HouseId { get; set; }

    public string Name { get; set; } = "Standard";
    public string Currency { get; set; } = "DKK";
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }

    public ICollection<RateSeason> Seasons { get; set; } = new List<RateSeason>();
    public ICollection<RateModifier> Modifiers { get; set; } = new List<RateModifier>();
}

