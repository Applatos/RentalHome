using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sommerhus.Application.Pricing.Abstractions;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Pricing.Engine.Rules;

public sealed class CleaningFeeRule : IPriceRule
{
    private readonly decimal _fee;

    public CleaningFeeRule(IConfiguration configuration)
    {
        _fee = configuration.GetValue<decimal>("Pricing:CleaningFee");
    }

    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        if (_fee > 0)
        {
            ctx.Items.Add(new PriceQuoteLineItemDto("CLEAN", "Rengøring", _fee));
        }

        return Task.CompletedTask;
    }
}
