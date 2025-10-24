using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions;

    public interface IPriceRule
    {
        Task ApplyAsync(PricingContext ctx, CancellationToken ct);
    }

