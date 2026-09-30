using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Pricing.Engine;

public sealed class PricingPipeline : IPricingPipeline
{
    private readonly IEnumerable<IPriceRule> _rules;

    public PricingPipeline(IEnumerable<IPriceRule> rules)
    {
        _rules = rules;
    }

    public async Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct)
    {
        var ctx = new PricingContext { Request = req };
        foreach (var rule in _rules)
        {
            await rule.ApplyAsync(ctx, ct);
        }

        if (ctx.UnpricedNights.Count > 0)
        {
            var first = ctx.UnpricedNights.Min();
            var count = ctx.UnpricedNights.Count;
            var message = count == 1
                ? $"1 night of the stay has no season price ({first:yyyy-MM-dd})."
                : $"{count} nights of the stay have no season price, the first is {first:yyyy-MM-dd}.";

            return ServiceResult<PriceQuoteResponseDto>.Invalid(PricingErrors.UnpricedNights, message);
        }

        return ServiceResult<PriceQuoteResponseDto>.Success(new PriceQuoteResponseDto(
            ctx.Currency,
            ctx.Nights.Count(),
            ctx.Items,
            ctx.Subtotal,
            ctx.Tax,
            ctx.Total,
            ctx.VatIncluded));
    }
}
