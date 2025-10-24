namespace Sommerhus.Pricing.Abstractions;

public interface IPriceRule
{
    Task ApplyAsync(PricingContext ctx, CancellationToken ct);
}

