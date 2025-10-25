using Microsoft.Extensions.Configuration;
using Sommerhus.Pricing.Abstractions;
using System.Threading;
using System.Threading.Tasks;

public sealed class CleaningFeeRule : IPriceRule
{
    private readonly decimal _fee;
    private readonly IConfiguration _cfg;

    public CleaningFeeRule(IConfiguration cfg)
    {
        _cfg = cfg;
        _fee = _cfg.GetValue<decimal>("Pricing:CleaningFee");
    }
    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        if (_fee > 0) ctx.Items.Add(new PriceLineItem("CLEAN", "Rengøring", _fee));
        return Task.CompletedTask;
    }
}