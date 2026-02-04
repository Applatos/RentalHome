using System;
using System.Collections.Generic;
using System.Linq;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Pricing.Abstractions;

public sealed class PricingContext
{
    public required PriceQuoteRequestDto Request { get; init; }
    public string Currency { get; set; } = "DKK";
    public Dictionary<DateOnly, decimal> NightlyRates { get; } = new();
    public PricePlan? RatePlan { get; set; }
    public List<PriceQuoteLineItemDto> Items { get; } = new();
    public decimal Subtotal => Items.Sum(i => i.Amount);
    public decimal Tax { get; set; }
    public decimal Total => Subtotal + Tax;
    public IEnumerable<DateOnly> Nights
    {
        get
        {
            var arrival = Request.Arrival;
            var count = Request.Departure.DayNumber - arrival.DayNumber;
            return Enumerable.Range(0, count).Select(i => arrival.AddDays(i));
        }
    }
}
