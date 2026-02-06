using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Pricing;

public interface IPricingQuoteService
{
    Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto request, CancellationToken ct);
}
