using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Pricing.Abstractions;
using System.Threading;
using System.Threading.Tasks;

public sealed class PricingPipeline : IPricingPipeline
{
    private readonly IEnumerable<IPriceRule> _rules;
    public PricingPipeline(IEnumerable<IPriceRule> rules) => _rules = rules;

    public async Task<PriceQuoteResponseDto> QuoteAsync(PriceQuoteRequestDto req, CancellationToken ct)
    {

        var ctx = new PricingContext { Request = req };
        foreach (var r in _rules)
        {
            await r.ApplyAsync(ctx, ct);
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