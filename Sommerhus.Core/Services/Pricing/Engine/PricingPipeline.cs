using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Pricing.Engine;

public sealed class PricingPipeline : IPricingPipeline
{
    private readonly IEnumerable<IPriceRule> _rules;

    public PricingPipeline(IEnumerable<IPriceRule> rules)
    {
        _rules = rules;
    }

    public async Task<PriceQuoteResponseDto> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct)
    {
        var ctx = new PricingContext { Request = req };
        foreach (var rule in _rules)
        {
            await rule.ApplyAsync(ctx, ct);
        }

        return new PriceQuoteResponseDto(
            ctx.Currency,
            ctx.Nights.Count(),
            ctx.Items,
            ctx.Subtotal,
            ctx.Tax,
            ctx.Total);
    }
}
