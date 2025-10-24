using Sommerhus.Pricing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions;

public sealed class PricingContext
{
    public required PriceQuoteRequest Request { get; init; }
    public string Currency { get; set; } = "DKK";
    public Dictionary<DateOnly, decimal> NightlyRates { get; } = new();
    public RatePlan? RatePlan { get; set; }
    public List<PriceLineItem> Items { get; } = new();
    public decimal Subtotal => Items.Sum(i => i.Amount);
    public decimal Tax { get; set; }
    public decimal Total => Subtotal + Tax;
    public IEnumerable<DateOnly> Nights
    {
        get
        {
            var arrival = Request.Arrival;
            var count = Request.Departure.DayNumber - arrival.DayNumber;
            return Enumerable.Range(0, count).Select(i => arrival.AddDays(i)); // captures arrival
        }
    }
}
