using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sommerhus.Application.Pricing.Abstractions;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Pricing.Engine.Rules;

/// <summary>
/// Applies an extra fee per guest above a base number.
/// Configuration:
///   Pricing:GuestFee:BaseGuests - Number of guests included in base price (default: 2)
///   Pricing:GuestFee:PerGuestPerNight - Extra fee per guest per night above base (default: 0)
/// </summary>
public sealed class GuestFeeRule : IPriceRule
{
    private readonly int _baseGuests;
    private readonly decimal _perGuestPerNight;

    public GuestFeeRule(IConfiguration configuration)
    {
        _baseGuests = configuration.GetValue("Pricing:GuestFee:BaseGuests", 2);
        _perGuestPerNight = configuration.GetValue("Pricing:GuestFee:PerGuestPerNight", 50m);
    }

    public Task ApplyAsync(PricingContext ctx, CancellationToken ct)
    {
        var extraGuests = ctx.Request.Guests - _baseGuests;
        if (extraGuests <= 0 || _perGuestPerNight <= 0)
        {
            return Task.CompletedTask;
        }

        var nights = ctx.NightlyRates.Count;
        if (nights == 0)
        {
            return Task.CompletedTask;
        }

        var totalGuestFee = extraGuests * _perGuestPerNight * nights;
        ctx.Items.Add(new PriceQuoteLineItemDto(
            "GUEST",
            $"Ekstra gæster ({extraGuests} x {nights} nætter)",
            totalGuestFee));

        return Task.CompletedTask;
    }
}
