using Sommerhus.Contracts.Dtos.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions
{
    public interface IPricingPipeline
    {
        Task<PriceQuoteResponseDto> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct);
    }
}
