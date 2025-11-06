using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

public class PricePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // En plan hører til et sommerhus
    public Guid HouseId { get; set; }

    public string Name { get; set; } = "Standard";
    public string Currency { get; set; } = "DKK";
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }

    public ICollection<SeasonPrice> SeasonPrices { get; set; } = new List<SeasonPrice>();
}