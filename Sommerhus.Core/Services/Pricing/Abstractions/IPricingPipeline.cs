using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Pricing.Abstractions;

public interface IPricingPipeline
{
    Task<PriceQuoteResponseDto> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct);
}
