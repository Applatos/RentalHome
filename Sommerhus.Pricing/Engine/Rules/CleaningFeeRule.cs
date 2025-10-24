//using Microsoft.Extensions.Configuration;
//using Sommerhus.Pricing.Abstractions;

//public sealed class CleaningFeeRule : IPriceRule
//{
//    private readonly decimal _fee;
//    public CleaningFeeRule(IConfiguration cfg) => _fee = cfg.GetValue("Pricing:CleaningFee", 0m);
//    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
//    {
//        if (_fee > 0) ctx.Items.Add(new PriceLineItem("CLEAN", "Rengøring", _fee));
//        return Task.CompletedTask;
//    }
//}