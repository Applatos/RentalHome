namespace Sommerhus.Pricing.Abstractions;

public record PriceQuoteRequest(
    Guid HouseId, 
    DateOnly Arrival, 
    DateOnly Departure, 
    int Guests, 
    Guid? AreaId);
