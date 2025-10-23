namespace Sommerhus.Contracts.Dtos.Pricing;

public record PriceQuoteRequest(Guid HouseId, DateOnly Arrival, DateOnly Departure, int Guests, Guid? AreaId);
public record PriceLineItem(string Code, string Text, decimal Amount);
public record PriceQuoteResult(string Currency, int Nights, IReadOnlyList<PriceLineItem> Items, decimal Subtotal, decimal Tax, decimal Total);
