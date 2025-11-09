using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sommerhus.Application.Pricing.Abstractions;

namespace Sommerhus.Application.Pricing.Engine.Rules;

public sealed class TaxRule : IPriceRule
{
    private readonly decimal _rate;

    public TaxRule(IConfiguration configuration)
    {
        _rate = configuration.GetValue("Pricing:VatRate", 0m);
    }

    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        ctx.Tax = Math.Round(ctx.Subtotal * _rate, 2);
        return Task.CompletedTask;
    }
}
