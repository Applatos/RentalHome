// 1) Base nightly price fra RateSeason
using Sommerhus.Api.Data;
using Sommerhus.Pricing.Abstractions;

public sealed class BaseNightlyRateRule : IPriceRule
{
    private readonly AppDbContext _db;
    public BaseNightlyRateRule(AppDbContext db) => _db = db;

    public async Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        var houseId = ctx.Request.HouseId;
        var nights = ctx.Nights.ToList();

        // Hent sæsoner der overlapper
        var from = nights.First();
        var to = nights.Last();
        var seasons = await _db.RateSeasons
            .Where(s => s.HouseId == houseId &&
                        s.StartDate <= to && s.EndDate >= from)
            .ToListAsync(ct);

        foreach (var d in nights)
        {
            var s = seasons.FirstOrDefault(x => x.StartDate <= d && d <= x.EndDate)
                    ?? throw new InvalidOperationException($"Ingen rate for {d}");
            ctx.Items.Add(new PriceLineItem("BASE", $"Nat {d} ({s.Name})", s.NightlyPrice));
        }
    }
}

