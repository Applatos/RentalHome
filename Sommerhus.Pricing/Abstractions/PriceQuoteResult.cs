namespace Sommerhus.Pricing.Abstractions;

//public sealed record PriceQuoteResponse(
//    Guid HouseId,
//    DateOnly Arrival,
//    DateOnly Departure,
//    int Guests,
//    decimal TotalPrice,
//    decimal DailyPrice);


public record PriceQuoteResult(string Currency, int Nights, IReadOnlyList<PriceLineItem> Items, decimal Subtotal, decimal Tax, decimal Total);
