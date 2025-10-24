using Sommerhus.Contracts.Dtos.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions
{
    public interface IPricingPipeline
    {
        Task<PriceQuoteResult> QuoteAsync(PriceQuoteRequest req, CancellationToken ct);
    }
}
