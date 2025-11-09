using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Pricing.Abstractions;

public interface IPricingPipeline
{
    Task<PriceQuoteResponseDto> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct);
}
