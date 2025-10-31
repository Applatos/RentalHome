using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

public class SeasonPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key på SeasonCode (opslagstabel med hver sæson type)
    public string Code { get; set; } = "A";


    public Guid PricePlanId { get; set; }
    // Navigation property
    public PricePlan? PricePlan { get; set; }


    public decimal NightlyPrice { get; set; }
}