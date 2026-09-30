using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sommerhus.Core.Services.Pricing.Abstractions;

namespace Sommerhus.Core.Services.Pricing.Engine.Rules;

/// <summary>
/// Prices include VAT (Danish consumer pricing): no tax is added on top, and the VAT share of the
/// total is recorded as <see cref="PricingContext.VatIncluded"/>. Must run after every rule that
/// adds line items.
/// Configuration:
///   Pricing:VatRate - VAT rate as a fraction (default: 0.25)
/// </summary>
public sealed class VatRule : IPriceRule
{
    public const decimal DefaultRate = 0.25m;

    private readonly decimal _rate;

    public VatRule(IConfiguration configuration)
    {
        _rate = ReadRate(configuration);
    }

    public static decimal ReadRate(IConfiguration configuration)
        => configuration.GetValue("Pricing:VatRate", DefaultRate);

    /// <summary>
    /// The VAT share of a VAT-inclusive total, rounded to two decimals.
    /// </summary>
    public static decimal IncludedIn(decimal total, decimal rate)
        => rate <= 0 ? 0m : Math.Round(total * rate / (1 + rate), 2, MidpointRounding.AwayFromZero);

    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        ctx.Tax = 0m;
        ctx.VatIncluded = IncludedIn(ctx.Total, _rate);
        return Task.CompletedTask;
    }
}
