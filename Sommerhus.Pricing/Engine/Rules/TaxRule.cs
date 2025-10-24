using Microsoft.Extensions.Configuration;
using Sommerhus.Pricing.Abstractions;

public sealed class TaxRule : IPriceRule
{
    private readonly decimal _rate; // fx 0.25m
    public TaxRule(IConfiguration cfg) => _rate = cfg.GetValue("Pricing:VatRate", 0m);
    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        ctx.Tax = Math.Round(ctx.Subtotal * _rate, 2);
        return Task.CompletedTask;
    }
}