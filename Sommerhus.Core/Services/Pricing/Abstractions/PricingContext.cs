using System;
using System.Collections.Generic;
using System.Linq;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Pricing.Abstractions;

public sealed class PricingContext
{
    public required PriceQuoteRequestDto Request { get; init; }
    public string Currency { get; set; } = "DKK";
    public Dictionary<DateOnly, decimal> NightlyRates { get; } = new();

    /// <summary>
    /// Nights no season price covers. A quote with any of these is invalid, never discounted.
    /// </summary>
    public List<DateOnly> UnpricedNights { get; } = new();

    public PricePlan? RatePlan { get; set; }
    public List<PriceQuoteLineItemDto> Items { get; } = new();
    public decimal Subtotal => Items.Sum(i => i.Amount);

    /// <summary>
    /// VAT added on top of the subtotal. Prices include VAT, so this stays zero.
    /// </summary>
    public decimal Tax { get; set; }

    /// <summary>
    /// The VAT share already included in <see cref="Total"/>.
    /// </summary>
    public decimal VatIncluded { get; set; }

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
