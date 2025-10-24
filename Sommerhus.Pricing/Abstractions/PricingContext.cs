using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions;

public sealed class PricingContext
{
    public required PriceQuoteRequest Request { get; init; }
    public string Currency { get; init; } = "DKK";
    public List<PriceLineItem> Items { get; } = new();
    public decimal Subtotal => Items.Sum(i => i.Amount);
    public decimal Tax { get; set; }
    public decimal Total => Subtotal + Tax;
    public IEnumerable<DateOnly> Nights => Enumerable.Range(0, (Request.Departure.DayNumber - Request.Arrival.DayNumber)).Select(i => Request.Arrival.AddDays(i));
}
