using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Pricing.Abstractions;

public interface IPricingPipeline
{
    /// <summary>
    /// Runs the price rules. Fails with <see cref="PricingErrors.UnpricedNights"/> when any night
    /// of the stay has no season price, so a partial price is never returned.
    /// </summary>
    Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct);
}
